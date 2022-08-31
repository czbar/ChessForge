using System;
using System.IO;
using System.Diagnostics;
using System.Timers;
using System.Diagnostics.Tracing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineService
{
    /// <summary>
    /// This class starts the engine service and provides a means of
    /// communicating with it.
    /// It is built as a DLL so that it can be easily tested independent of the main program.
    /// </summary>
    public class EngineProcess : IEngineService
    {
        /// <summary>
        /// Once the engine process has started and "readyok" was received, the engine will be
        /// available to receive commands.
        /// 
        /// The initial state is NOT_READY and it changes to IDLE after receiving "readyok".
        /// Once it receives a "go" command it enters CALCULATING state.
        /// During the CALCULATING state it will only accept a "stop" command.
        /// When the "best move" command is received, the engine goes back to IDLE.
        /// After receiving a "stop" command, the engine enters the STOPPING state
        /// during which it will not accept any commands. It will await a 
        /// "best move" command to then go back to IDLE.
        /// If the "best move" is not received within 1 second, the engine will enter
        /// UNEXPECTED state and won't be accepting any commands.
        /// It is up to the client to check for this state and restart the engine
        /// if appropariate.
        /// </summary>
        public enum State
        {
            NOT_READY,
            IDLE,
            CALCULATING,
            STOPPING,
            UNEXPECTED
        }

        /// <summary>
        /// The current state of the engine.
        /// It is readonly.
        /// </summary>
        public State CurrentState { get => _currentState; }

        /// <summary>
        /// True if the engine is running ready to accept requets 
        /// </summary>
        public bool IsEngineRunning = false;

        /// <summary>
        /// True if we have received "readyok" from the engine  
        /// </summary>
        public bool IsEngineReady = false;

        // Message polling interval in milliseconds
        private static readonly int POLL_INTERVAL = 50;

        // A number of polls in the STOPPING state after which we decide that the engine state is unhealthy
        private static readonly int MAX_POLL_COUNT_IN_STOPPING = (int)(1000 / POLL_INTERVAL);

        // A lock object to use when reading engine messages
        private static object _lockEngineMessage = new object();

        // A lock object for changing _currentState
        private static object _lockStateChange = new object();

        // helper counter for ReadEngineMessages
        private int _counter = 0;

        // reads engine process's STDOUT 
        private StreamReader _strmReader;

        // writes to engine process's STDIN 
        private StreamWriter _strmWriter;

        // the engine service process
        private Process _engineProcess;

        // counts the number of poll events while in the STOPPING state
        private int _pollCountInStopping;

        /// <summary>
        /// Action invoked by ReadEngineMessage().
        /// It is defined in EngineMessageProcessor as EngineMessageReceived().
        /// </summary>
        public event Action<string> EngineMessage;

        // the current state of the engine
        private State _currentState;

        // if true, engine's messages will be logged
        private bool _debugMode;

        // the timer polling for engine's messages
        private Timer _messagePollTimer = new Timer();

        // a "position" command queued while in STOPPING mode
        private string _queuedPositionCommand;

        // a "go" command queued while in STOPPING mode
        private string _queuedGoCommand;

        /// <summary>
        /// Creates the Engine Service object.
        /// </summary>
        /// <param name="debugMode"></param>
        public EngineProcess(bool debugMode, string appPath)
        {
            _debugMode = debugMode;
            if (_debugMode)
            {
                EngineLog.SetDebugMode(appPath);
            }
        }

        /// <summary>
        /// Starts the engine process.
        /// Initializes all relevant objects and variables.
        /// </summary>
        /// <returns></returns>
        public bool StartEngine(string enginePath)
        {
            try
            {
                EngineLog.Message("Starting engine: " + enginePath);
                _engineProcess = new Process();
                _engineProcess.StartInfo.FileName = enginePath;
                _engineProcess.StartInfo.UseShellExecute = false;
                _engineProcess.StartInfo.RedirectStandardInput = true;
                _engineProcess.StartInfo.RedirectStandardOutput = true;
                _engineProcess.StartInfo.RedirectStandardError = true;
                _engineProcess.StartInfo.CreateNoWindow = true;

                _engineProcess.Start();

                _strmWriter = _engineProcess.StandardInput;
                _strmReader = _engineProcess.StandardOutput;

                CreateTimer();

                // start the message polling timer
                // it will be stopped when ok is received
                StartMessagePollTimer();

                _strmWriter.WriteLine(UciCommands.ENG_UCI);
                _strmWriter.WriteLine(UciCommands.ENG_ISREADY);
                _strmWriter.WriteLine(UciCommands.ENG_UCI_NEW_GAME);

                IsEngineRunning = true;
                _currentState = State.NOT_READY;

                EngineLog.Message("Engine running.");

                return true;
            }
            catch (Exception ex)
            {
                EngineLog.Message("Failed to start engine: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Stops the engine process.
        /// </summary>
        public void StopEngine()
        {
            StopMessagePollTimer();

            if (_engineProcess != null && !_engineProcess.HasExited)
            {
                _strmReader.Close();
                _strmReader = null;
                _strmWriter.Close();
                _engineProcess.Close();
            }

            IsEngineRunning = false;
            IsEngineReady = false;

            _currentState = State.NOT_READY;
        }

        /// <summary>
        /// Sends a command to the engine by writing to its standard stream.
        /// Checks the type of the command versus the current state to determine
        /// whether it is ok to send the given command.
        /// </summary>
        /// <param name="command"></param>
        public void SendCommand(string command)
        {
            if (!IsEngineRunning || !IsEngineReady)
            {
                return;
            }

            lock (_lockStateChange)
            {
                if (_strmWriter != null && command != UciCommands.ENG_UCI)
                {
                    EngineLog.Message("Cmd: " + command);
                    bool accept = false;
                    switch (_currentState)
                    {
                        case State.NOT_READY:
                            break;
                        case State.IDLE:
                            if (!command.StartsWith(UciCommands.ENG_STOP))
                            {
                                accept = true;
                            }
                            if (command.StartsWith(UciCommands.ENG_GO))
                            {
                                // we may have a queued "position" command
                                if (!string.IsNullOrWhiteSpace(_queuedPositionCommand))
                                {
                                    _strmWriter.WriteLine(command);
                                    _queuedPositionCommand = null;
                                }
                                _queuedGoCommand = null;
                                _currentState = State.CALCULATING;
                            }
                            break;
                        case State.CALCULATING:
                            if (command.StartsWith(UciCommands.ENG_STOP))
                            {
                                accept = true;
                                _currentState = State.STOPPING;
                            }
                            break;
                        case State.STOPPING:
                            // queue the command to send once we are back in IDLE mode.
                            // we only allow one (latest) position and go command and 
                            if (command.StartsWith(UciCommands.ENG_GO))
                            {
                                _queuedGoCommand = command;
                            }
                            else if (command.StartsWith(UciCommands.ENG_POSITION))
                            {
                                _queuedPositionCommand = command;
                            }
                            break;
                    }

                    if (accept)
                    {
                        // we are sending a command so enable message polling, if not enabled
                        StartMessagePollTimer();
                        _strmWriter.WriteLine(command);
                    }
                    else
                    {
                        EngineLog.Message("Command rejected! : State=" + _currentState.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Starts the message poll timer.
        /// </summary>
        private void StartMessagePollTimer()
        {
            _messagePollTimer.Enabled = true;
        }

        /// <summary>
        /// Stops the message poll timer
        /// </summary>
        private void StopMessagePollTimer()
        {
            _messagePollTimer.Enabled = false;
        }

        /// <summary>
        /// Creates a timer to poll for engine messages.
        /// </summary>
        private void CreateTimer()
        {
            _messagePollTimer.Enabled = false;
            _messagePollTimer.Interval = POLL_INTERVAL;
            _messagePollTimer.Elapsed += new ElapsedEventHandler(ReadEngineMessages);
        }

        /// <summary>
        /// Called periodically in response to MessagePollTimer's elapse event
        /// to check on messages from the engine.
        /// Invokes the message handler to process the info.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void ReadEngineMessages(object source, ElapsedEventArgs e)
        {
            lock (_lockEngineMessage)
            {
                if (_strmReader == null)
                    return;

                if (_currentState == State.STOPPING)
                {
                    _pollCountInStopping++;
                    if (_pollCountInStopping >= MAX_POLL_COUNT_IN_STOPPING)
                    {
                        _currentState = State.UNEXPECTED;
                    }
                }
                else
                {
                    _pollCountInStopping = 0;
                }

                try
                {
                    string message;
                    while ((message = _strmReader.ReadLine()) != null)
                    {
                        if (message != null && !message.Contains("currmove"))
                        {
                            EngineLog.Message(message);
                            if (message.StartsWith(UciCommands.ENG_READY_OK))
                            {
                                HandleReadyOk();
                            }
                            else
                            {
                                if (message.StartsWith(UciCommands.ENG_BEST_MOVE))
                                {
                                    HandleBestMove(message);
                                }
                                EngineMessage?.Invoke(message);
                            }
                        }

                        // break out every now and then if it gets to tight so that GUI updates can happen
                        _counter++;
                        if (_counter % 10 == 0)
                        {
                            _counter = 0;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    EngineLog.Message("ReadEngineMessages():" + ex.Message);
                    throw new Exception("ReadEngineMessages():" + ex.Message);
                };
            }
        }

        /// <summary>
        /// Handles the "readyok" message.
        /// </summary>
        private void HandleReadyOk()
        {
            IsEngineReady = true;
            _currentState = State.IDLE;
            StopMessagePollTimer();
        }

        /// <summary>
        /// Handles the "bestmove" message
        /// </summary>
        /// <param name="message"></param>
        private void HandleBestMove(string message)
        {
            lock (_lockStateChange)
            {
                bool stopPoll = true;
                _currentState = State.IDLE;
                // if we have queued commands, send them now
                if (_queuedPositionCommand != null)
                {
                    SendCommand(_queuedPositionCommand);
                    _queuedPositionCommand = null;
                }
                if (_queuedGoCommand != null)
                {
                    SendCommand(_queuedGoCommand);
                    _queuedGoCommand = null;
                    stopPoll = false;
                }
                if (stopPoll)
                {
                    StopMessagePollTimer();
                }
            }
        }

    }
}


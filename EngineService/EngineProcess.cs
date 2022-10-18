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
    public class EngineProcess
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
        /// If the "best move" is not received within 5000 milliseconds, the engine will enter
        /// UNEXPECTED state and will require a restart.
        /// It is up to the client to check for this state and restart the engine
        /// if appropriate.
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

        /// <summary>
        /// The number of alternative lines to analyze
        /// </summary>
        public int Multipv { get => _multipv; set => _multipv = value; }

        // Message polling interval in milliseconds
        private static readonly int POLL_INTERVAL = 50;

        // A number of polls in the STOPPING state after which we decide that the engine will not send "bestmove". 
        // We allow 5000 ms which is rather generous.
        private static readonly int MAX_POLL_COUNT_IN_STOPPING = (int)(5000 / POLL_INTERVAL);

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

        // the timer polling for engine's messages
        private Timer _messagePollTimer = new Timer();

        // number of lines to analyze
        private int _multipv = 5;

        // a completed "go"+"position" command i.e. one for which we have receive a "bestmove" message
        private GoFenCommand _goFenCompleted;

        // a "go"+"position" command currently being calculated by the engine
        private GoFenCommand _goFenCurrent;

        // a "go"+"position" command that was received while there was another one being calculated.
        private GoFenCommand _goFenQueued;

        /// <summary>
        /// Creates the Engine Service object.
        /// </summary>
        /// <param name="debugMode"></param>
        public EngineProcess()
        {
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

                WriteOut(UciCommands.ENG_UCI);
                WriteOut(UciCommands.ENG_ISREADY);
                WriteOut(UciCommands.ENG_UCI_NEW_GAME);

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
        /// Depending on the current state of the engine,
        /// sends "go" and "position" commands or queues them.
        /// </summary>
        /// <param name="cmd"></param>
        public void SendFenGoCommand(GoFenCommand cmd)
        {
            if (_currentState == State.IDLE)
            {
                GoFenCommand gfc = cmd;
                // if there is a queued command send it first
                if (_goFenQueued != null)
                {
                    gfc = _goFenQueued;
                    _goFenQueued = cmd;
                }

                _goFenCurrent = gfc;
                int mpv = gfc.Mpv <= 0 ? _multipv : gfc.Mpv;

                WriteOut(UciCommands.ENG_SET_MULTIPV + " " + mpv.ToString());
                WriteOut(UciCommands.ENG_POSITION_FEN + " " + gfc.Fen);
                WriteOut(gfc.GoCommandString);
                StartMessagePollTimer();

                _currentState = State.CALCULATING;
            }
            else if (_currentState == State.CALCULATING)
            {
                // new request came in so queue it and stop the previous one
                _currentState = State.STOPPING;
                WriteOut(UciCommands.ENG_STOP);
                _goFenQueued = cmd;
            }
            else
            {
                _goFenQueued = cmd;
            }
        }

        /// <summary>
        /// Sends the "stop" command to the engine.
        /// </summary>
        public void SendStopCommand()
        {
            SendCommand(UciCommands.ENG_STOP);
        }

        /// <summary>
        /// Sends a command to the engine by writing to its standard input stream.
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
                    EngineLog.Message("Command requested: " + command + " : State=" + _currentState.ToString());
                    bool accept = false;
                    switch (_currentState)
                    {
                        case State.NOT_READY:
                            break;
                        case State.IDLE:
                            accept = !command.StartsWith(UciCommands.ENG_STOP);
                            break;
                        case State.CALCULATING:
                            if (command.StartsWith(UciCommands.ENG_STOP))
                            {
                                accept = true;
                                _currentState = State.STOPPING;
                            }
                            break;
                        case State.STOPPING:
                            break;
                    }

                    if (accept)
                    {
                        // we are sending a command so enable message polling, if not enabled
                        StartMessagePollTimer();
                        WriteOut(command);
                    }
                    else
                    {
                        EngineLog.Message("Command rejected: " + command + " : State=" + _currentState.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the Message Poll timer
        /// is currently enabled.
        /// </summary>
        /// <returns></returns>
        public bool IsMessagePollEnabled()
        {
            return _messagePollTimer.Enabled;
        }

        /// <summary>
        /// Writes directly to the engine.
        /// </summary>
        /// <param name="command"></param>
        private void WriteOut(string command)
        {
            _strmWriter.WriteLine(command);
            EngineLog.Message("Command sent: " + command + " : State=" + _currentState.ToString());
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
        /// The message handler is EngineMessageProcessor.EngineMessageReceived(string)
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
                        // we have not received "bestmove" message in a reasonable time,
                        // assume the engine is available but log the problem.
                        EngineLog.Message("ERROR: wait for bestmove timed out");
                        _currentState = State.UNEXPECTED;
                    }
                }
                else
                {
                    _pollCountInStopping = 0;
                }

                string message;
                try
                {
                    while (_strmReader != null && (message = _strmReader.ReadLine()) != null)
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
                                message = InsertIdsPrefix(message);
                                if (message.Contains(UciCommands.ENG_BEST_MOVE))
                                {
                                    HandleBestMove();
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
                    EngineLog.Message("ERROR: ReadEngineMessages():" + ex.Message);
                    throw new Exception("ReadEngineMessages():" + ex.Message);
                };
            }
        }

        /// <summary>
        /// Prefixes the message with the ids of the Tree and Node within
        /// the tree, for which the evaluation was performed.
        /// The EngineMessageReceived() handler will parse it and strip off before further
        /// processing.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private string InsertIdsPrefix(string message)
        {
            if (_goFenCurrent != null)
            {
                return UciCommands.CHF_TREE_ID_PREFIX + _goFenCurrent.TreeId.ToString() + " "
                     + UciCommands.CHF_NODE_ID_PREFIX + _goFenCurrent.NodeId.ToString()
                     + " " + message;
            }
            else
            {
                return message;
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
        /// Receiving the "bestmove" message (or timing out waiting for it)
        /// completes the evaluation process.
        /// We change state to IDLE and send the queued commands, if any.
        /// </summary>
        private void HandleBestMove()
        {
            _goFenCompleted = _goFenCurrent;
            _goFenCurrent = null;

            lock (_lockStateChange)
            {
                bool stopPoll = true;
                _currentState = State.IDLE;

                if (_goFenQueued != null)
                {
                    _goFenCurrent = _goFenQueued;
                    _goFenQueued = null;
                    SendFenGoCommand(_goFenCurrent);
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


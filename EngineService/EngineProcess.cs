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
        /// True if we have received "readyok" from the engine  
        /// </summary>
        public bool IsEngineReady
        {
            get => _isEngineReady;
        }

        /// <summary>
        /// The number of alternative lines to analyze
        /// </summary>
        public int Multipv { get => _multipv; set => _multipv = value; }

        /// <summary>
        /// True if the engine is running and ready to accept requets 
        /// </summary>
        private bool _isEngineRunning = false;

        // indicates engine's readiness
        private bool _isEngineReady = false;

        // whether the message reading loop is running
        private bool _isMessageRxLoopRunning = false;

        /// <summary>
        /// Action invoked by ReadEngineMessage().
        /// It is defined in EngineMessageProcessor as EngineMessageReceived().
        /// </summary>
        public event Action<string> EngineMessage;

        // Message polling interval in milliseconds
        private static readonly int POLL_INTERVAL = 200;

        // A lock object to use when reading engine messages
        private static object _lockEngineMessage = new object();

        // A lock object for changing _currentState
        private static object _lockStateChange = new object();

        // reads engine process's STDOUT 
        private StreamReader _strmReader;

        // writes to engine process's STDIN 
        private StreamWriter _strmWriter;

        // the engine service process
        private Process _engineProcess;

        // the current state of the engine
        private State _currentState;

        // the timer starting / re-starting the message reading loop
        private Timer _messageRxLoopTimer = new Timer();

        // number of lines to analyze
        private int _multipv = 5;

        // a completed "go"+"position" command i.e. one for which we have receive a "bestmove" message
        private GoFenCommand _goFenCompleted;

        // a "go"+"position" command currently being calculated by the engine
        private GoFenCommand _goFenCurrent;

        // a "go"+"position" command that was received while there was another one being calculated.
        private GoFenCommand _goFenQueued;

        // whether to ignore the next bestmove response (because the caller abandoned evaluation mode)
        private bool _ignoreNextBestMove = false;

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

                bool result = true;
                bool started = _engineProcess.Start();

                EngineLog.Message("StartEngine returned: " + started.ToString() + "; HasExited=" + _engineProcess.HasExited.ToString());
                if (!started && !_engineProcess.HasExited)
                {
                    result = false;
                }
                else
                {
                    _isEngineRunning = true;

                    CreateMessageRxLoopTimer();
                    StartMessageRxLoopTimer();

                    _strmWriter = _engineProcess.StandardInput;
                    _strmReader = _engineProcess.StandardOutput;

                    WriteOut(UciCommands.ENG_UCI);
                    WriteOut(UciCommands.ENG_ISREADY);
                    WriteOut(UciCommands.ENG_UCI_NEW_GAME);

                    _currentState = State.NOT_READY;

                    EngineLog.Message("Engine running.");
                }

                return result;
            }
            catch (Exception ex)
            {
                EngineLog.Message("Failed to start engine: " + ex.Message);
                _isEngineRunning = false;
                return false;
            }
        }

        /// <summary>
        /// Stops the engine process.
        /// </summary>
        public void StopEngine()
        {
            try
            {
                StopMessageRxLoopTimer();

                if (_engineProcess != null && _isEngineRunning)
                {
                    _strmReader.Close();
                    _strmReader = null;
                    _strmWriter.Close();
                    if (_engineProcess.ProcessName != null)
                    {
                        _engineProcess.Close();
                    }
                }

                _isEngineRunning = false;
                _isEngineReady = false;

                _currentState = State.NOT_READY;
            }
            catch
            {
            }
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
                EngineLog.Message("NodeId=" + gfc.NodeId.ToString() + " TreeId=" + gfc.TreeId.ToString() + " Mode=" + gfc.EvalMode);
                StartMessageRxLoopTimer();

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
        public void SendStopCommand(bool ignoreNextBestMove)
        {
            _ignoreNextBestMove = ignoreNextBestMove;
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
            if (!_isEngineRunning || !_isEngineReady)
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
                        StartMessageRxLoopTimer();
                        WriteOut(command);
                    }
                    else
                    {
                        _ignoreNextBestMove = false;
                        EngineLog.Message("Command rejected: " + command + " : State=" + _currentState.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the Message Rx Loop timer
        /// is currently enabled.
        /// </summary>
        /// <returns></returns>
        public bool IsMessageRxLoopEnabled()
        {
            return _messageRxLoopTimer.Enabled;
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
        /// Creates a timer to poll for engine messages.
        /// </summary>
        private void CreateMessageRxLoopTimer()
        {
            _messageRxLoopTimer.Enabled = false;
            _messageRxLoopTimer.Interval = POLL_INTERVAL;
            _messageRxLoopTimer.Elapsed += new ElapsedEventHandler(CheckMessageRxLoop);
        }

        /// <summary>
        /// Starts the message poll timer.
        /// </summary>
        private void StartMessageRxLoopTimer()
        {
            _messageRxLoopTimer.Enabled = true;
        }

        /// <summary>
        /// Stops the message poll timer
        /// </summary>
        private void StopMessageRxLoopTimer()
        {
            _messageRxLoopTimer.Enabled = false;
        }

        /// <summary>
        /// Invoked by the MessageRxLoopTimer event.
        /// Starts or re-starts the message loop if not currently running.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void CheckMessageRxLoop(object source, ElapsedEventArgs e)
        {
            if (_isEngineRunning && !_isMessageRxLoopRunning)
            {
                ReadEngineMessages();
            }
        }

        /// <summary>
        /// Called periodically in response to MessagePollTimer's elapse event
        /// to check on messages from the engine.
        /// Invokes the message handler to process the info.
        /// The message handler is EngineMessageProcessor.EngineMessageReceived(string)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void ReadEngineMessages()
        {
            _isMessageRxLoopRunning = true;

            lock (_lockEngineMessage)
            {
                if (_strmReader == null)
                {
                    _isMessageRxLoopRunning = false;
                    return;
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
                                message = InsertIdPrefixes(message);
                                if (message.Contains(UciCommands.ENG_BEST_MOVE))
                                {
                                    if (!HandleBestMove())
                                    {
                                        message = InsertBestMoveDelayedPrefix(message);
                                    }

                                    if (!_ignoreNextBestMove)
                                    {
                                        EngineMessage?.Invoke(message);
                                    }
                                }
                                else
                                {
                                    EngineMessage?.Invoke(message);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _isMessageRxLoopRunning = false;

                    EngineLog.Message("ERROR: ReadEngineMessages():" + ex.Message);
                    throw new Exception("ReadEngineMessages():" + ex.Message);
                };
            }

            _isMessageRxLoopRunning = false;
        }

        /// <summary>
        /// Prefixes the message with the ids of the Tree and Node within
        /// the tree, for which the evaluation was performed.
        /// The EngineMessageReceived() handler will parse it and strip off before further
        /// processing.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private string InsertIdPrefixes(string message)
        {
            if (_goFenCurrent != null)
            {
                return UciCommands.CHF_TREE_ID_PREFIX + _goFenCurrent.TreeId.ToString() + " "
                     + UciCommands.CHF_NODE_ID_PREFIX + _goFenCurrent.NodeId.ToString() + " "
                     + UciCommands.CHF_EVAL_MODE_PREFIX + ((int)_goFenCurrent.EvalMode).ToString() + " "
                     + message;
            }
            else
            {
                return message;
            }
        }

        /// <summary>
        /// Inserts a prefix indicating that this message was received AFTER another request came in.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private string InsertBestMoveDelayedPrefix(string message)
        {
            return UciCommands.CHF_DELAYED_PREFIX + " " + message;
        }

        /// <summary>
        /// Handles the "readyok" message.
        /// </summary>
        private void HandleReadyOk()
        {
            _isEngineReady = true;
            _currentState = State.IDLE;
        }

        /// <summary>
        /// Receiving the "bestmove" message (or timing out waiting for it)
        /// completes the evaluation process.
        /// We change state to IDLE and send the queued commands, if any.
        /// If there was an command currently being evaluated, this method returns
        /// false so that the caller does not invoke the processing in the app.
        /// Otherwise it could lead to serious problems with managing timing 
        /// of commands and respones.
        /// </summary>
        /// <returns></returns>
        private bool HandleBestMove()
        {
            bool result = true;

            _goFenCompleted = _goFenCurrent;
            _goFenCurrent = null;

            EngineLog.Message("Best Move rx: NodeId=" + _goFenCompleted.NodeId.ToString() + " TreeId=" + _goFenCompleted.TreeId.ToString());

            lock (_lockStateChange)
            {
                _currentState = State.IDLE;

                if (_goFenQueued != null)
                {
                    result = false;
                    EngineLog.Message("Discarding bestmove for NodeId=" + _goFenCompleted.NodeId.ToString());

                    _goFenCurrent = _goFenQueued;
                    _goFenQueued = null;
                    EngineLog.Message("Sending queued command for NodeId=" + _goFenCurrent.NodeId.ToString() + ", State = " + _currentState.ToString());
                    SendFenGoCommand(_goFenCurrent);
                }
            }

            return result;
        }
    }
}


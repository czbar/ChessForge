using System;
using System.IO;
using System.Diagnostics;
using System.Timers;
using System.Collections.Generic;

namespace EngineService
{
    /// <summary>
    /// This class starts the engine service and provides a means of
    /// communicating with it.
    /// It is built as a DLL so that it can be easily tested independent of the main program.
    /// </summary>
    public partial class EngineProcess
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
        /// 
        /// NOTE: ensure that sending commands, setting _goFen commands and setting State happens under _lockSendCommand
        /// </summary>
        /// 

        // lock object to prevent commands sent simultaneously and thus corrupting STATE
        private static object _lockSendCommand = new object();

        // number of basd messages seen in a row that may be an indicator of an engine issue
        private static int _badMessageCount = 0;

        /// <summary>
        /// Possible Engine states.
        /// </summary>
        public enum State
        {
            NOT_READY,
            IDLE,
            CALCULATING,
            STOPPING,
        }

        /// <summary>
        /// True if we have received "readyok" from the engine  
        /// </summary>
        public bool IsEngineReady
        {
            get => _isEngineReady;
        }

        /// <summary>
        /// Evaluation mode for the message currently being processed.
        /// </summary>
        public GoFenCommand.EvaluationMode ActiveEvaluationMode
        {
            get => _activeEvaluationMode;
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

        /// <summary>
        /// Action invoked by ProcessMessagesList().
        /// It is defined in EngineMessageProcessor as EngineInfoMessagesReceived().
        /// </summary>
        public event Action<List<string>> EngineInfoMessages;

        // path to the engine executable
        private string _enginePath;

        // A lock object to use when reading engine messages
        private static object _lockEngineMessage = new object();

        // reads engine process's STDOUT 
        private StreamReader _strmReader;

        // writes to engine process's STDIN 
        private StreamWriter _strmWriter;

        // the engine service process
        private Process _engineProcess;

        // the current state of the engine
        private State _currentState;

        // Message polling interval in milliseconds
        private static readonly int RX_LOOP_POLL_INTERVAL = 200;

        // the timer starting / re-starting the message reading loop
        private Timer _messageRxLoopTimer = new Timer();

        // maximum allowed time in the stopping state i.e. after
        // the Stop command was sent but BestMessage was not received.
        private static readonly int STOPPING_STATE_MAX_TIME = 500;

        // the timer for getting the process out of the STOPPING mode if stuck
        private Timer _stoppingStateTimer = new Timer();

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

        // evaluation mode for the message currently processed
        private GoFenCommand.EvaluationMode _activeEvaluationMode;

        /// <summary>
        /// Creates the Engine Service object.
        /// </summary>
        public EngineProcess()
        {
        }

        // engine options
        private List<KeyValuePair<string, string>> _options;

        //**************************************************
        //
        // PUBLIC METHODS TO START/TOP THE ENGINE
        //
        //**************************************************

        /// <summary>
        /// Starts the engine process.
        /// Initializes all relevant objects and variables.
        /// </summary>
        /// <returns></returns>
        public bool StartEngine(string enginePath, List<KeyValuePair<string, string>> options)
        {
            try
            {
                _isEngineRunning = false;
                _isEngineReady = false;
                _isMessageRxLoopRunning = false;

                _enginePath = enginePath;
                _options = options;

                EngineLog.Message("Starting the Engine: " + enginePath);
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

                    CreateStoppingStateTimer();

                    _strmWriter = _engineProcess.StandardInput;
                    _strmReader = _engineProcess.StandardOutput;

                    WriteOut(UciCommands.ENG_UCI);
                    WriteOut(UciCommands.ENG_ISREADY);
                    WriteOut(UciCommands.ENG_UCI_NEW_GAME);

                    _currentState = State.NOT_READY;

                    _activeEvaluationMode = GoFenCommand.EvaluationMode.NONE;
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
            EngineLog.Message("Stopping the Engine");
            try
            {
                StopMessageRxLoopTimer();

                if (_engineProcess != null && _isEngineRunning)
                {
                    if (_engineProcess.ProcessName != null)
                    {
                        _engineProcess.Kill();
                    }
                }

                _isEngineRunning = false;
                _isEngineReady = false;

                _currentState = State.NOT_READY;
                _activeEvaluationMode = GoFenCommand.EvaluationMode.NONE;

                _engineProcess.Kill();
                _engineProcess.Dispose();

                EngineLog.Message("Engine stopped.");
            }
            catch (Exception ex)
            {
                EngineLog.Message("EXCEPTION in StopEngine() " + ex.Message);
            }
        }

        /// <summary>
        /// Stops and restarts the engine
        /// </summary>
        public void RestartEngine()
        {
            StopEngine();
            StartEngine(_enginePath, _options);
        }

        //**************************************************
        //
        // PUBLIC METHODS FOR SENDING COMMANDS TO THE engine.
        // Only "Go Fen"  and stops commands are allowed.
        //
        //**************************************************

        /// <summary>
        /// Depending on the current state of the engine,
        /// sends the requested "go+position" command or queues it.
        /// Do not send an existed queued command. Discard it as there
        /// is nothing expeting it if we alreayd have a newer request.
        /// </summary>
        /// <param name="cmd"></param>
        public void SendFenGoCommand(GoFenCommand cmd)
        {
            lock (_lockSendCommand)
            {
                switch (_currentState)
                {
                    case State.IDLE:
                        GoFenCommand gfc = cmd;
                        _goFenCurrent = gfc;
                        int mpv = gfc.Mpv <= 0 ? _multipv : gfc.Mpv;

                        _activeEvaluationMode = cmd.EvalMode;

                        WriteOut(UciCommands.ENG_SET_MULTIPV + " " + mpv.ToString());
                        WriteOut(UciCommands.ENG_POSITION_FEN + " " + gfc.Fen);
                        WriteOut(gfc.GoCommandString);
                        EngineLog.Message("NodeId=" + gfc.NodeId.ToString() + " TreeId=" + gfc.TreeId.ToString() + " Mode=" + gfc.EvalMode);
                        StartMessageRxLoopTimer();

                        _currentState = State.CALCULATING;
                        break;
                    case State.CALCULATING:
                        // new request came in so queue it and stop the previous one
                        _currentState = State.STOPPING;
                        WriteOut(UciCommands.ENG_STOP);

                        _activeEvaluationMode = GoFenCommand.EvaluationMode.NONE;
                        _goFenQueued = cmd;
                        break;
                    case State.STOPPING:
                        _goFenQueued = cmd;
                        break;
                }
            }
        }

        /// <summary>
        /// For the STOP command it is unsafe to send it if BestMove message
        /// is expected. We may then confuse for which GoFen command the BestMove
        /// is when finally received.
        /// However, we also want to prevent hanging due to never receiving BestMove
        /// (although this should never happen) so need a safety exit.
        /// </summary>
        public void SendStopCommand(bool ignoreNextBestMove)
        {
            EngineLog.Message("STOP command requested.");
            lock (_lockSendCommand)
            {
                _ignoreNextBestMove = ignoreNextBestMove;
                switch (_currentState)
                {
                    case State.IDLE:
                        WriteOut(UciCommands.ENG_STOP);
                        _activeEvaluationMode = GoFenCommand.EvaluationMode.NONE;
                        _currentState = State.IDLE;
                        break;
                    case State.CALCULATING:
                        WriteOut(UciCommands.ENG_STOP);
                        _activeEvaluationMode = GoFenCommand.EvaluationMode.NONE;
                        _currentState = State.STOPPING;
                        break;
                    case State.STOPPING:
                        // start timer ensuring that we get out of the stopping state even if BestMove never comes
                        EngineLog.Message("WARNING: received STOP request while in STOPPING state.");
                        StartStoppingStateTimer();
                        break;
                }
            }
        }

        /// <summary>
        /// Sens a setoption command.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SendSetOptionCommand(string name, string value)
        {
            lock (_lockSendCommand)
            {
                string command = string.Format(UciCommands.ENG_SET_OPTION, name, value);
                EngineLog.Message("Sending SetOption command: " + command);
                WriteOut(command);
            }
        }

        /// <summary>
        /// Make sure there are no remnants blocking new messages.
        /// E.g. when we exit a game or a training session.
        /// </summary>
        public void ClearState()
        {
            SendStopCommand(false);
            _activeEvaluationMode = GoFenCommand.EvaluationMode.NONE;
            lock (_lockSendCommand)
            {
                _goFenQueued = null;
                _goFenCurrent = null;
            }
        }

        /// <summary>
        /// More than a few bad messages indicate a problem with the engine.
        /// The actual definition of "a few" is rather arbitrary.
        /// </summary>
        /// <returns></returns>
        public bool IsEngineHealthy()
        {
            return _badMessageCount < 10;
        }

        //**************************************************
        //
        // PRIVATE METHODS
        //
        //**************************************************

        /// <summary>
        /// Writes directly to the engine.
        /// </summary>
        /// <param name="command"></param>
        private void WriteOut(string command)
        {
            _strmWriter.WriteLine(command);
            EngineLog.Message("Command sent: " + command + " : State=" + _currentState.ToString());
        }


        //******************************************************
        //
        //        INTERNAL ENGINE PROCESS TIMERS
        //   for getting the process out of bad state.
        //
        //******************************************************

        //*** Message Receiving Loop Timer ***//

        /// <summary>
        /// Creates a timer to poll for engine messages.
        /// Used to monitor the health of the ReadEngineMessages()
        /// a.k.a. Rx Message Rx loop.
        /// </summary>
        private void CreateMessageRxLoopTimer()
        {
            _messageRxLoopTimer.Enabled = false;
            _messageRxLoopTimer.Interval = RX_LOOP_POLL_INTERVAL;
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
                EngineLog.Message("WARNING: ReadEngineMessages() was not running. Restarting...");
                ReadEngineMessages();
            }
        }

        //*** Message Receiving Loop Timer ***//

        /// <summary>
        /// Creates a timer to run in the stopping state
        /// making sure that it does not last too long.
        /// </summary>
        private void CreateStoppingStateTimer()
        {
            _stoppingStateTimer.Enabled = false;
            _stoppingStateTimer.Interval = STOPPING_STATE_MAX_TIME;
            _stoppingStateTimer.Elapsed += new ElapsedEventHandler(ExitStoppingState);
        }

        /// <summary>
        /// Starts the stopping state timer.
        /// </summary>
        private void StartStoppingStateTimer()
        {
            _stoppingStateTimer.Enabled = true;
        }

        /// <summary>
        /// Stops the stopping state timer.
        /// </summary>
        private void StopStoppingStateTimer()
        {
            _stoppingStateTimer.Enabled = false;
        }

        /// <summary>
        /// Invoked by the MessageRxLoopTimer event.
        /// Starts or re-starts the message loop if not currently running.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void ExitStoppingState(object source, ElapsedEventArgs e)
        {
            // take action if we are in the STOPPING state
            bool sendCommand = false;

            lock (_lockSendCommand)
            {
                if (_currentState == State.STOPPING)
                {
                    _currentState = State.IDLE;
                    sendCommand = true;
                }
            }

            if (sendCommand)
            {
                SendQueuedCommand();
            }

            StopStoppingStateTimer();
        }

        //******************************************************
        //
        //        MAIN MESSAGE LOOP
        //
        //******************************************************


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

            string message;
            lock (_lockEngineMessage)
            {
                if (_strmReader == null)
                {
                    _isMessageRxLoopRunning = false;
                    return;
                }

                try
                {
                    while (_strmReader != null)
                    {
                        message = _strmReader.ReadLine();
                        
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
                                    _activeEvaluationMode = GoFenCommand.EvaluationMode.NONE;

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

                        if (message == null)
                        {
                            // null message can be received during initialization or when there is an engine error.
                            // if the former, we need to exit the loop and allow regular processing,
                            // if the latter, we need to handle error situation
                            _badMessageCount++;
                            if (_isEngineReady)
                            {
                                EngineMessage?.Invoke(message);
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            _badMessageCount = 0;
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
            lock (_lockSendCommand)
            {
                _currentState = State.IDLE;
            }

            try
            {
                if (_options != null)
                {
                    foreach (var option in _options)
                    {
                        SendSetOptionCommand(option.Key, option.Value);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Receiving the "bestmove" message completes the evaluation process.
        /// We change state to IDLE and send the queued commands, if any.
        /// If there was a command currently being evaluated, this method returns
        /// false so that the caller does not invoke the processing in the app.
        /// Otherwise it could lead to serious problems with managing timing 
        /// of commands and respones.
        /// </summary>
        /// <returns></returns>
        private bool HandleBestMove()
        {
            lock (_lockSendCommand)
            {
                bool result = true;

                _goFenCompleted = _goFenCurrent;
                _goFenCurrent = null;

                if (_goFenCompleted != null)
                {
                    EngineLog.Message("Best Move rx: NodeId=" + _goFenCompleted.NodeId.ToString() + " TreeId=" + _goFenCompleted.TreeId.ToString() + ", State = " + _currentState.ToString());
                }

                _currentState = State.IDLE;

                if (_goFenQueued != null)
                {
                    result = false;
                    SendQueuedCommand();
                }

                return result;
            }
        }

        /// <summary>
        /// Sends a queued command if there is one.
        /// </summary>
        private void SendQueuedCommand()
        {
            bool sendQueued = false;
            lock (_lockSendCommand)
            {
                if (_goFenQueued != null)
                {
                    _goFenCurrent = _goFenQueued;
                    _goFenQueued = null;
                    sendQueued = true;
                }
            }

            if (sendQueued)
            {
                EngineLog.Message("Sending queued command for NodeId=" + _goFenCurrent.NodeId.ToString() + " TreeId=" + _goFenCurrent.TreeId.ToString() + ", State = " + _currentState.ToString());
                SendFenGoCommand(_goFenCurrent);
            }
        }


        //******************************************************
        //
        // DEBUG HELPERS
        //
        //******************************************************

        /// <summary>
        /// Sends a command without checking any pre-conditions.
        /// Use for debugging only.
        /// </summary>
        /// <param name="command"></param>
        [Conditional("DEBUG")]
        public void DebugSendCommand(string command)
        {
            EngineLog.Message("DebugSendCommand():" + command);
            WriteOut(command);
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

    }
}


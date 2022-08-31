using System;
using System.Collections.Generic;
using System.Timers;
using System.Text;
using System.Threading.Tasks;
using GameTree;
using ChessPosition;
using System.IO;
using EngineService;

namespace ChessForge
{
    /// <summary>
    /// Handles messages received from the chess engine
    /// and requests for evaluation in different contexts.
    /// There will be one instance of this class created 
    /// in the Main Window lasting the whole session. 
    /// </summary>
    public class EngineMessageProcessor
    {
        // an instance of the engine service
        public static EngineService.EngineProcess ChessEngineService;

        /// <summary>
        /// The list of candidates returned by the engine.
        /// </summary>
        public static List<MoveEvaluation> MoveCandidates = new List<MoveEvaluation>();

        /// <summary>
        /// Flags whether message processing is in progress.
        /// We won't process another message untile this flag
        /// is reset.
        /// </summary>
        public static bool IsInfoMessageProcessing = false;

        /// <summary>
        /// Lock object to use when accessing the list of candidate move.
        /// </summary>
        public static object MoveCandidatesLock = new object();

        /// <summary>
        /// Lock object to use when accessing the "info message processing" flag.
        /// </summary>
        private static object InfoMessageProcessLock = new object();

        // main application window
        private static MainWindow _mainWin;


        /// <summary>
        /// Set when we want to stop processing.
        /// We have to wait for the bestmove message though or
        /// strange things will happen.
        /// </summary>
        private static bool _pendingStop = false;

        /// <summary>
        /// Creates an instance of the Engine service.
        /// </summary>
        /// <param name="win"></param>
        /// <param name="debugMode"></param>
        public static void CreateEngineService(MainWindow mainWin, bool debugMode)
        {
            _mainWin = mainWin;

            ChessEngineService = new EngineService.EngineProcess(debugMode, App.AppPath);
            ChessEngineService.EngineMessage += EngineMessageReceived;
        }

        /// <summary>
        /// Stops the engine service e.g. when
        /// we want to switch the engines.
        /// </summary>
        public static void StopEngineService()
        {
            ChessEngineService.StopEngine();
        }

        /// <summary>
        /// Returns true if the engine service is running.
        /// </summary>
        /// <returns></returns>
        public static bool IsEngineAvailable()
        {
            return ChessEngineService.IsEngineReady;
        }

        /// <summary>
        /// Sends a command to the engine.
        /// </summary>
        /// <param name="cmd"></param>
        public static void SendCommand(string cmd)
        {
            AppLog.Message("Tx Command: " + cmd);
            ChessEngineService.SendCommand(cmd);
        }

        /// <summary>
        /// Starts the engine service.
        /// </summary>
        /// <returns></returns>
        public static bool Start()
        {
            string enginePath = Configuration.EngineExecutableFilePath();
            return ChessEngineService.StartEngine(enginePath);
        }

        /// <summary>
        /// Evaluation has finished. Tidy up and reset to prepare for the next evaluation request.
        /// This is called in the evaluation mode as well as during user vs engine game. 
        /// 
        /// This method may be invoked to evaluate a single move whether there is or there is not
        /// a game in progress i.e. AppState.Mode is or is not equal to GAME_VS_COMPUTER.
        /// To distinguish we need to check Evaluation.Mode.
        /// </summary>
        public static void MoveEvaluationFinished()
        {
            ClearMoveCandidates(false);
            if (_mainWin.Evaluation.CurrentMode == EvaluationState.EvaluationMode.ENGINE_GAME)
            {
                ProcessEngineGameMoveEvent();
                _mainWin.Evaluation.PrepareToContinue();

                _mainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                _mainWin.ResetEvaluationProgressBar();
                if (TrainingState.IsTrainingInProgress)
                {
                    _mainWin.EngineTrainingGameMoveMade();
                }
                AppStateManager.SetCurrentEvaluationMode(EvaluationState.EvaluationMode.IDLE);
            }
            else if (TrainingState.IsTrainingInProgress)
            {
                // stop the timer, apply training mode specific handling 
                // NOTE do not reset Evaluation.CurrentMode as this will be done 
                // later down the chain
                _mainWin.Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                _mainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                _mainWin.ResetEvaluationProgressBar();

                _mainWin.MoveEvaluationFinishedInTraining();
            }
            else
            {
                lock (LearningMode.EvalLock)
                {
                    AppLog.Message("Move evaluation finished for index " + _mainWin.Evaluation.PositionIndex.ToString());

                    string eval = _mainWin.Evaluation.PositionEvaluation;
                    if (!string.IsNullOrEmpty(eval))
                    {
                        // if this is not checkmate, check the sign (for checkmate it is already there)
                        if (eval.IndexOf('#') < 0)
                        {
                            eval = (eval[0] == '-' ? "" : "+") + eval;
                        }
                    }

                    bool isWhiteEval = (_mainWin.Evaluation.PositionIndex - 1) % 2 == 0;
                    int moveIndex = (_mainWin.Evaluation.PositionIndex - 1) / 2;
                    if (isWhiteEval)
                    {
                        _mainWin.ActiveLine.GetMoveAtIndex(moveIndex).WhiteEval = eval;
                        _mainWin.ActiveLine.GetNodeForMove(moveIndex, PieceColor.White).EngineEvaluation = eval;
                        AppStateManager.IsDirty = true;
                    }
                    else
                    {
                        _mainWin.ActiveLine.GetMoveAtIndex(moveIndex).BlackEval = eval;
                        _mainWin.ActiveLine.GetNodeForMove(moveIndex, PieceColor.Black).EngineEvaluation = eval;
                        AppStateManager.IsDirty = true;
                    }

                    _mainWin.Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                    _mainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);

                    // if the mode is not LINE or this is the last move in LINE
                    // evaluation we stop here
                    // otherwise we start the next move's evaluation
                    if (_mainWin.Evaluation.CurrentMode != EvaluationState.EvaluationMode.LINE
                        || _mainWin.Evaluation.PositionIndex == _mainWin.ActiveLine.GetPlyCount() - 1)
                    {
                        _mainWin.Evaluation.Reset();

                        AppStateManager.ResetEvaluationControls();
                        AppStateManager.ShowMoveEvaluationControls(false, true);
                    }
                    else
                    {
                        AppLog.Message("Continue eval next move after index " + _mainWin.Evaluation.PositionIndex.ToString());
                        ClearMoveCandidates(false);
                        _mainWin.Evaluation.PrepareToContinue();
                        _mainWin.Evaluation.PositionIndex++;
                        RequestMoveEvaluation(_mainWin.Evaluation.PositionIndex);

                        _mainWin.Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                    }
                }
            }
        }

        /// <summary>
        /// We have a response from the engine so need to choose
        /// the move from the list of candidates, show it on the board
        /// and display in the Engine Game Line view.
        /// </summary>
        public static void ProcessEngineGameMoveEvent()
        {
            TreeNode nd = null;
            BoardPosition pos = null;

            // NOTE: need to invoke from the Dispatcher here or the program
            // will crash when engine makes a White move (because
            // it will attempt to add an element to the MoveList ObservableCollection
            // from the "wrong" thread)
            _mainWin.Dispatcher.Invoke(() =>
            {
                pos = EngineGame.ProcessEngineGameMove(out nd);
                SoundPlayer.PlayMoveSound(nd.LastMoveAlgebraicNotation);
                _mainWin.BoardCommentBox.GameMoveMade(nd, false);
            });

            // update the GUI and finish
            // (the app will wait for the user's move)
            _mainWin.DisplayPosition(pos);
            _mainWin.ColorMoveSquares(nd.LastMoveEngineNotation);

            // check for mate and stalemate
            bool isMateCf = PositionUtils.IsCheckmate(nd.Position);
            bool isStalemate = false;
            if (!isMateCf)
            {
                isStalemate = PositionUtils.IsStalemate(nd.Position);
            }

            if (isMateCf)
            {
                EngineGame.CurrentState = EngineGame.GameState.IDLE;
                _mainWin.BoardCommentBox.ReportCheckmate(false);
            }
            else if (isStalemate)
            {
                EngineGame.CurrentState = EngineGame.GameState.IDLE;
                _mainWin.BoardCommentBox.ReportStalemate();
            }
            else
            {
                EngineGame.CurrentState = EngineGame.GameState.USER_THINKING;
                _mainWin.Timers.Start(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
            }
        }

        /// <summary>
        /// Starts move evaluation on user's request or when continuing line evaluation.
        /// NOTE: does not start evaluation when making a move during a user vs engine game.
        /// </summary>
        /// <param name="posIndex"></param>
        public static void RequestMoveEvaluation(int posIndex)
        {
            _mainWin.Evaluation.PositionIndex = posIndex;
            if (_mainWin.Evaluation.CurrentMode == EvaluationState.EvaluationMode.IDLE)
            {
                AppStateManager.SetCurrentEvaluationMode(EvaluationState.EvaluationMode.SINGLE_MOVE);
            }

            TreeNode nd = _mainWin.ActiveLine.GetNodeAtIndex(posIndex);
            _mainWin.Evaluation.Position = nd.Position;
            _mainWin.DisplayPosition(_mainWin.Evaluation.Position);

            _mainWin.Dispatcher.Invoke(() =>
            {
                if (AppStateManager.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW && _mainWin.Evaluation.CurrentMode == EvaluationState.EvaluationMode.LINE)
                {
                    _mainWin.ActiveLine.SelectPly((int)nd.Parent.Position.MoveNumber, nd.Parent.Position.ColorToMove);
                    _mainWin.SelectLineAndMoveInWorkbookViews(_mainWin.ActiveLine.GetLineId(), nd);
                }
            });

            AppStateManager.ShowMoveEvaluationControls(true);
            _mainWin.UpdateLastMoveTextBox(posIndex);

            string fen = AppStateManager.PrepareMoveEvaluation(_mainWin.Evaluation.Position, true);
            RequestEngineEvaluation(fen, Configuration.EngineMpv, Configuration.EngineEvaluationTime);
        }

        /// <summary>
        /// This method will be called when in Training mode to evaluate
        /// user's move or moves from the Workbook.
        /// </summary>
        /// <param name="nodeId"></param>
        public static void RequestMoveEvaluationInTraining(int nodeId)
        {
            TreeNode nd = _mainWin.Workbook.GetNodeFromNodeId(nodeId);
            RequestMoveEvaluationInTraining(nd);
        }

        /// <summary>
        /// Requests evaluation while in Training mode.
        /// </summary>
        /// <param name="nd"></param>
        public static void RequestMoveEvaluationInTraining(TreeNode nd)
        {
            _mainWin.Evaluation.Position = nd.Position;
            _mainWin.UpdateLastMoveTextBox(nd);
            AppStateManager.ShowMoveEvaluationControls(true, false);

            string fen = AppStateManager.PrepareMoveEvaluation(_mainWin.Evaluation.Position, true);
            int moveTime = AppStateManager.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME ?
                Configuration.EngineMoveTime : Configuration.EngineEvaluationTime;
            RequestEngineEvaluation(fen, Configuration.EngineMpv, moveTime);
        }

        /// <summary>
        /// Prepares controls, timers 
        /// and requests the engine to make move.
        /// </summary>
        /// <param name="position"></param>
        public static void RequestEngineMove(BoardPosition position)
        {
            AppStateManager.SetCurrentEvaluationMode(EvaluationState.EvaluationMode.ENGINE_GAME);

            string fen = AppStateManager.PrepareMoveEvaluation(position, false);
            RequestEngineEvaluation(fen, Configuration.EngineMpv, Configuration.EngineMoveTime);
        }

        /// <summary>
        /// Clears the list of candidate moves.
        /// Not in engine mode, though, (unless forced)
        /// because we will be choosing from the candidates a little later.
        /// </summary>
        public static void ClearMoveCandidates(bool force)
        {
            if (force || !(AppStateManager.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME))
                lock (MoveCandidatesLock)
                {
                    MoveCandidates.Clear();
                }
        }

        /// <summary>
        /// Sends a sequence of commands to the engine to request evaluation
        /// of the position.
        /// </summary>
        /// <param name="fen">The position to analyze in the FEN format.</param>
        /// <param name="mpv">Number of options to return.</param>
        /// <param name="movetime">Time to think per move (milliseconds)</param>
        public static void RequestEngineEvaluation(string fen, int mpv, int movetime)
        {
            SendCommand("setoption name multipv value " + mpv.ToString());
            SendCommand("position fen " + fen);
            SendCommand("go movetime " + movetime.ToString());
        }

        public static void RequestPositionEvaluation(TreeNode nd, int mpv, int movetime)
        {
            string fen = FenParser.GenerateFenFromPosition(nd.Position);
            SendCommand("setoption name multipv value " + mpv.ToString());
            SendCommand("position fen " + fen);

            if (movetime >= 0)
            {
                SendCommand("go movetime " + movetime.ToString());
            }
            else
            {
                SendCommand("go");
            }
            AppStateManager.ShowMoveEvaluationControls(true);

        }

        /// <summary>
        /// Stops engine evaluation. Sends the "stop"
        /// command and raises the "_pendingStop" flag.
        /// We want to wait for a "bestmove" command if it 
        /// is coming or time out, if it is not.
        /// </summary>
        public static void StopEngineEvaluation()
        {
            SendCommand("stop");
            _pendingStop = true;
            _mainWin.Timers.Start(AppTimers.TimerId.STOP_MESSAGE_POLL);
        }

        /// <summary>
        /// Stops the evaluation once the allowed time expired.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void StopEngineEvaluation(object source, ElapsedEventArgs e)
        {
            SendCommand("stop");
        }

        /// <summary>
        /// Called when STOP_MESSAGE_POLL timer elapsed.
        /// We let some time elapse between since sending
        /// the "stop" command so we are assuming that there
        /// are no messages pending.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void MessageQueueTimeout(object source, ElapsedEventArgs e)
        {
            _pendingStop = false;
            _mainWin.Timers.Stop(AppTimers.TimerId.STOP_MESSAGE_POLL);
            //StopMessagePollTimer();
        }

        /// <summary>
        /// Processes a response from the engine.
        /// We will be interested in messages of these forms:
        /// 1. ongoing evaluation update e.g.: 
        ///     "info depth 10 seldepth 15 multipv 1 score cp -59 nodes 60598 nps 126509 tbhits 0 time 479 pv d7d5 e4d5 d8d5 b1c3 d5a5 d2d4 e7e5 d4d5 a5b4"
        ///     where "multipv" indicates which move, in order of best to worst this is, "score cp" is score in centipawns and "pv" indicates the variation
        ///     examined.
        /// 2. best move message e.g. 
        ///    "bestmove d7d5 ponder e4d5"
        ///    where we will grab the "bestmove" value as the final answer and ignore the "ponder" part (for now, anyway)
        /// </summary>
        /// <param name="message"></param>
        private static void EngineMessageReceived(string message)
        {
            if (message == null)
                return;

            //AppLog.Message("Rx Msg: " + message);
            if (message.StartsWith("info"))
            {
                ProcessInfoMessage(message);
            }
            else if (message.StartsWith("bestmove"))
            {
                if (_pendingStop)
                {
                    _pendingStop = false;
                    //StopMessagePollTimer();
                }
                else
                {
                    ProcessBestMoveMessage(message);
                }
            }
            else if (message.StartsWith(UciCommands.ENG_ID_NAME))
            {
                string engineName = message.Substring(UciCommands.ENG_ID_NAME.Length).Trim();
                AppStateManager.EngineName = engineName;
            }
        }

        /// <summary>
        /// In response to the "bestmove" message,
        /// flags the completion of evaluation.
        /// </summary>
        /// <param name="message"></param>
        private static void ProcessBestMoveMessage(string message)
        {
            // make sure the last lines are shown before we stop the timer.
            _mainWin.EngineLinesGUI.ShowEngineLines(null, null);

            // tell the app that the evaluation has finished
            MoveEvaluationFinished();
        }

        /// <summary>
        /// Parses a text message from the engine and sets properties
        /// of the MoveEvaluation object stored in MoveCandidates list
        /// under the index corresponding to the value of multipv.
        /// </summary>
        /// <param name="message"></param>
        private static void ProcessInfoMessage(string message)
        {
            lock (InfoMessageProcessLock)
            {
                if (IsInfoMessageProcessing)
                    return;
                else
                    IsInfoMessageProcessing = true;
            }

            string[] tokens = message.Split(' ');
            int idx = 0;
            int? multipv = null;
            int? score = null;
            int? movesToMate = null;
            string moves = "";

            string STRING_PV = " pv ";

            while (idx < tokens.Length)
            {
                switch (tokens[idx])
                {
                    case "multipv":
                        idx++;
                        multipv = int.Parse(tokens[idx]);
                        break;
                    case "score":
                        // next token can be "cp" or "mate" followed by an int value
                        ProcessScoreTokens(tokens[idx + 1], tokens[idx + 2], out score, out movesToMate);
                        idx += 2;
                        break;
                    case "pv":
                        int pvIndex = message.IndexOf(STRING_PV);
                        moves = message.Substring(pvIndex + STRING_PV.Length);

                        // force loop exit
                        idx = tokens.Length;
                        break;
                }
                idx++;
            }

            lock (MoveCandidatesLock)
            {
                if (multipv != null && (score != null || movesToMate != null))
                {
                    // we have updated evaluation
                    // make sure we have the object to set
                    while (MoveCandidates.Count < multipv)
                    {
                        MoveCandidates.Add(new MoveEvaluation());
                    }

                    MoveCandidates[multipv.Value - 1].Line = moves;
                    if (score != null)
                    {
                        MoveCandidates[multipv.Value - 1].ScoreCp = score.Value;
                        MoveCandidates[multipv.Value - 1].IsMateDetected = false;
                    }
                    else
                    {
                        MoveCandidates[multipv.Value - 1].IsMateDetected = true;
                        MoveCandidates[multipv.Value - 1].MovesToMate = movesToMate.Value;
                    }
                }
                else if (multipv == null && movesToMate == 0) // special case where we have a check mate position
                {
                    if (MoveCandidates.Count == 0)
                    {
                        MoveCandidates.Add(new MoveEvaluation());
                    }
                    MoveCandidates[0].IsMateDetected = true;
                    MoveCandidates[0].MovesToMate = 0;
                }
                else if (multipv == null && score == 0) // special case where we have a stalemate
                {
                    if (MoveCandidates.Count == 0)
                    {
                        MoveCandidates.Add(new MoveEvaluation());
                    }
                    MoveCandidates[0].ScoreCp = score.Value;
                    MoveCandidates[0].IsMateDetected = false;
                    MoveCandidates[0].MovesToMate = 0;
                }
            }


            lock (InfoMessageProcessLock)
            {
                IsInfoMessageProcessing = false;
            }
        }

        /// <summary>
        /// Processes the 2 tokens following the "score" token.
        /// We will only process it if the value of the first token is "cp" or "mate".
        /// </summary>
        private static void ProcessScoreTokens(string firstToken, string secondToken, out int? score, out int? movesToMate)
        {
            score = null;
            movesToMate = null;

            int iVal;

            if (firstToken == "cp")
            {
                if (int.TryParse(secondToken, out iVal))
                {
                    score = iVal;
                }
            }
            else if (firstToken == "mate")
            {
                if (int.TryParse(secondToken, out iVal))
                {
                    movesToMate = iVal;
                }
            }
        }

    }
}

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
        // An instance of the engine service
        public static EngineProcess ChessEngineService;

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
        /// Creates an instance of the Engine service.
        /// </summary>
        /// <param name="win"></param>
        /// <param name="debugMode"></param>
        public static void CreateEngineService(MainWindow mainWin, bool debugMode)
        {
            _mainWin = mainWin;

            ChessEngineService = new EngineService.EngineProcess();
            ChessEngineService.EngineMessage += EngineMessageReceived;
            ChessEngineService.Multipv = Configuration.EngineMpv;
        }

        /// <summary>
        /// Starts the engine service.
        /// </summary>
        /// <returns></returns>
        public static bool StartEngineService()
        {
            string enginePath = Configuration.EngineExecutableFilePath();
            return ChessEngineService.StartEngine(enginePath);
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
        /// Returns true if the engine service is running.
        /// </summary>
        /// <returns></returns>
        public static bool IsEngineAvailable { get => ChessEngineService.IsEngineReady; }

        /// <summary>
        /// Returns true if the Message Poll timer
        /// is currently enabled.
        /// </summary>
        /// <returns></returns>
        public static bool IsMessagePollEnabled()
        {
            return ChessEngineService.IsMessagePollEnabled();
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
        /// Stops engine evaluation. Sends the "stop" command to the Engine.
        /// </summary>
        public static void StopEngineEvaluation()
        {
            SendCommand("stop");
        }

        //*********************************************************************************
        //
        //         PREPARING PROCESSING OF MESSAGES RECEIVED FROM THE ENGINE
        //
        //*********************************************************************************

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
            if (EvaluationManager.CurrentMode == EvaluationManager.Mode.ENGINE_GAME)
            {
                // we are in a game
                MoveEvaluationFinishedInGame();
            }
            else if (TrainingState.IsTrainingInProgress)
            {
                // eval request was made while in training (LearningMode can be GAME or TRAINING)
                MoveEvaluationFinishedInTraining();
            }
            else
            {
                // eval request in MANUAL_REVIEW (could be for COMTINUOUS or LINE)
                MoveEvaluationFinishedInManualReview();
            }
        }

        /// <summary>
        /// Move evaluation finished while in the ENGINE_GAME mode.
        /// This could be while playing a game against the engine that
        /// started in MANUAL_REVIEW learning mode,
        /// or during the training i.e. in TRAINING learning mode.
        /// </summary>
        private static void MoveEvaluationFinishedInGame()
        {
            if (EvaluationManager.CurrentMode == EvaluationManager.Mode.ENGINE_GAME)
            {
                ProcessEngineGameMove();
                _mainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                _mainWin.ResetEvaluationProgressBar();

                if (TrainingState.IsTrainingInProgress)
                {
                    _mainWin.EngineTrainingGameMoveMade();
                }
                _mainWin.Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);
            }
        }

        /// <summary>
        /// Move evaluation finished while in the TRAINING mode.
        /// Note that this is in manual TRAINING mode, not during a game.
        /// The engine was evaluating the move on user's request not responding
        /// to the user's move in a game.
        /// </summary>
        private static void MoveEvaluationFinishedInTraining()
        {
            // stop the timer, apply training mode specific handling 
            // NOTE do not reset Evaluation.CurrentMode as this will be done 
            // later down the chain
            EvaluationManager.SetPositionToEvaluate(null);
            _mainWin.Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
            _mainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
            _mainWin.ResetEvaluationProgressBar();

            _mainWin.MoveEvaluationFinishedInTraining();
        }

        /// <summary>
        /// Move evaluation finished while in the MANUAL_REVIEW.
        /// This is either CONTINUOUS evaluation (i.e. an "infinite"
        /// evaluation of the position that is currently selected 
        /// in Active Line or a LINE evaluation where we ask for 
        /// evaluation move by move automatically.
        /// </summary>
        private static void MoveEvaluationFinishedInManualReview()
        {
            lock (LearningMode.EvalLock)
            {
                AppLog.Message("Move evaluation finished for index " + EvaluationManager.PositionIndex.ToString());

                string eval = EvaluationManager.PositionEvaluation;
                if (!string.IsNullOrEmpty(eval))
                {
                    // if this is not checkmate, check the sign (for checkmate it is already there)
                    if (eval.IndexOf('#') < 0)
                    {
                        eval = (eval[0] == '-' ? "" : "+") + eval;
                    }
                }

                bool isWhiteEval = (EvaluationManager.PositionIndex - 1) % 2 == 0;
                int moveIndex = (EvaluationManager.PositionIndex - 1) / 2;

                if (EvaluationManager.PositionIndex <= 0)
                {
                    AppLog.Message("ERROR: MoveEvaluationFinishedInManualReview() - bad position index " + EvaluationManager.PositionIndex.ToString());
                }
                else
                {
                    AppStateManager.ActiveLine.SetEvaluation(moveIndex, isWhiteEval ? PieceColor.White : PieceColor.Black, eval);
                }

                if (EvaluationManager.CurrentMode != EvaluationManager.Mode.CONTINUOUS)
                {
                    _mainWin.Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                    _mainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                }

                if (ContinueLineEvaluation())
                {
                    AppLog.Message("Continue eval next move after index " + EvaluationManager.PositionIndex.ToString());
                    ClearMoveCandidates(false);
                    AppStateManager.MainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                    EvaluationManager.PositionIndex++;
                    RequestMoveEvaluation(EvaluationManager.PositionIndex);

                    _mainWin.Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                }
                else
                {
                    if (EvaluationManager.CurrentMode != EvaluationManager.Mode.CONTINUOUS)
                    {
                        EvaluationManager.Reset();

                        AppStateManager.ResetEvaluationControls();
                        AppStateManager.ShowMoveEvaluationControls(false, true);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if we should continue with LINE evaluation.
        /// Returns true if the mode is LINE and the evaluated position
        /// was not the last in the evaluated Line.
        /// </summary>
        /// <returns></returns>
        private static bool ContinueLineEvaluation()
        {
            return EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE && EvaluationManager.PositionIndex != _mainWin.ActiveLine.GetPlyCount() - 1;
        }

        /// <summary>
        /// We have a response from the engine so need to choose
        /// the move from the list of candidates, show it on the board
        /// and display in the Engine Game Line view.
        /// </summary>
        private static void ProcessEngineGameMove()
        {
            TreeNode nd = null;
            BoardPosition pos = null;

            // NOTE: need to invoke from the Dispatcher here or the program
            // will crash when engine makes a White move (because
            // it will attempt to add an element to the MoveList ObservableCollection
            // from the "wrong" thread)
            _mainWin.Dispatcher.Invoke(() =>
            {
                pos = EngineGame.ProcessEngineMove(out nd);
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
                EngineGame.ChangeCurrentState(EngineGame.GameState.IDLE);
                _mainWin.BoardCommentBox.ReportCheckmate(false);
            }
            else if (isStalemate)
            {
                EngineGame.ChangeCurrentState(EngineGame.GameState.IDLE);
                _mainWin.BoardCommentBox.ReportStalemate();
            }
            else
            {
                EngineGame.ChangeCurrentState(EngineGame.GameState.USER_THINKING);
                _mainWin.Timers.Start(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
            }
        }


        //*********************************************************************************
        //
        //         PREPARING REQUESTS/COMMANDS FOR THE ENGINE
        //
        //*********************************************************************************

        /// <summary>
        /// Starts move evaluation on user's request or when continuing line evaluation.
        /// NOTE: does not start evaluation when making a move during a user vs engine game.
        /// </summary>
        /// <param name="posIndex"></param>
        public static void RequestMoveEvaluation(int posIndex)
        {
            EvaluationManager.PositionIndex = posIndex;
            if (EvaluationManager.CurrentMode == EvaluationManager.Mode.IDLE)
            {
                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.CONTINUOUS);
            }

            TreeNode nd = _mainWin.ActiveLine.GetNodeAtIndex(posIndex);
            _mainWin.DisplayPosition(EvaluationManager.Position);

            _mainWin.Dispatcher.Invoke(() =>
            {
                if (AppStateManager.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW && EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE)
                {
                    _mainWin.ActiveLine.SelectPly((int)nd.Parent.Position.MoveNumber, nd.Parent.Position.ColorToMove);
                    _mainWin.SelectLineAndMoveInWorkbookViews(_mainWin.ActiveLine.GetLineId(), posIndex);
                }
            });

            AppStateManager.ShowMoveEvaluationControls(true);
            _mainWin.UpdateLastMoveTextBox(posIndex);

            string fen = AppStateManager.PrepareMoveEvaluation(EvaluationManager.Position, true);
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
            EvaluationManager.SetPositionToEvaluate(nd.Position);
            string fen = FenParser.GenerateFenFromPosition(nd.Position);
            _mainWin.UpdateLastMoveTextBox(nd);
            AppStateManager.ShowMoveEvaluationControls(true, false);

            AppStateManager.PrepareMoveEvaluation(fen, true);
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
            EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.ENGINE_GAME);

            string fen = AppStateManager.PrepareMoveEvaluation(position, false);
            RequestEngineEvaluation(fen, Configuration.EngineMpv, Configuration.EngineMoveTime);
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
            SendCommand("position fen " + fen);
            SendCommand("go movetime " + movetime.ToString());
        }

        /// <summary>
        /// Request evaluation of a position.
        /// </summary>
        /// <param name="index">Index of the position in the main line</param>
        /// <param name="mpv"></param>
        /// <param name="movetime"></param>
        public static void RequestPositionEvaluation(int index, int mpv, int movetime)
        {
            _mainWin.Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
            ClearMoveCandidates(true);

            string fen = FenParser.GenerateFenFromPosition(_mainWin.ActiveLine.GetNodeAtIndex(index).Position);
            SendCommand("position fen " + fen);

            EvaluationManager.PositionIndex = index;
            if (movetime > 0)
            {
                SendCommand(UciCommands.ENG_GO_MOVE_TIME + " " + movetime.ToString());
            }
            else
            {
                SendCommand(UciCommands.ENG_GO_INFINITE);
            }

            AppStateManager.ShowMoveEvaluationControls(true);
            _mainWin.Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
        }

        //*********************************************************************************
        //
        //         PROCESSING MESSAGES FROM THE ENGINE
        //
        //*********************************************************************************

        /// <summary>
        /// This function is subscribed to EngineMessageReceived
        /// in Engine Service's EngineMessage.
        /// 
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

            if (message.StartsWith("info"))
            {
                ProcessInfoMessage(message);
            }
            else if (message.StartsWith("bestmove"))
            {
                ProcessBestMoveMessage(message);
            }
            else if (message.StartsWith(UciCommands.ENG_ID_NAME))
            {
                string engineName = message.Substring(UciCommands.ENG_ID_NAME.Length).Trim();
                AppStateManager.EngineName = engineName;
            }
        }

        /// <summary>
        /// The "bestmove" message has been received which indicates
        /// the completion of evaluations.
        /// Invoke the final processing
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
        /// We will only process it if the value of the first token is "cp" (centipawn evaluation)
        /// or "mate" (a variation ending in checkmate).
        /// </summary>
        /// <param name="firstToken"></param>
        /// <param name="secondToken"></param>
        /// <param name="score"></param>
        /// <param name="movesToMate"></param>
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

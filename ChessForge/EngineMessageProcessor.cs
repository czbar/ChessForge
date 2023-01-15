﻿using System;
using System.Collections.Generic;
using System.Timers;
using System.Text;
using System.Threading.Tasks;
using GameTree;
using ChessPosition;
using System.IO;
using EngineService;
using System.Windows.Ink;
using System.Windows;
using static System.Net.Mime.MediaTypeNames;

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

        // node for which the last engine message was received
        private static TreeNode _lastMessageNode = null;

        /// <summary>
        /// The Node for whichthe last engine message was received.
        /// </summary>
        public static TreeNode LastMessageNode
        {
            get => _lastMessageNode;
        }

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
        /// Stops engine evaluation. Sends the "stop" command to the Engine.
        /// </summary>
        public static void StopEngineEvaluation(bool ignoreNextBestMove = false)
        {
            ChessEngineService.SendStopCommand(ignoreNextBestMove);
        }

        /// <summary>
        /// Stops engine evaluation and changes Evaluation Mode to IDLE.
        /// </summary>
        public static void ExitEngineEvaluationMode()
        {
            StopEngineEvaluation();
            EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);
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
        public static void MoveEvaluationFinished(TreeNode nd, int treeId, GoFenCommand.EvaluationMode mode, bool delayed)
        {
            ClearMoveCandidates(false);
            // it could be that we switched to a game mode while awaiting "normal" evaluation,
            // so double check to avoid complications
            if (EvaluationManager.CurrentMode == EvaluationManager.Mode.ENGINE_GAME && mode == GoFenCommand.EvaluationMode.GAME)
            {
                if (mode == GoFenCommand.EvaluationMode.GAME)
                {
                    MoveEvaluationFinishedInGame(nd);
                }
                else
                {
                }
            }
            else if (TrainingSession.IsTrainingInProgress)
            {
                // eval request was made while in training (LearningMode can be GAME or TRAINING)
                MoveEvaluationFinishedInTraining(nd, delayed);
            }
            else
            {
                // eval request in MANUAL_REVIEW (could be for CONTINUOUS or LINE)
                MoveEvaluationFinishedInManualReview(nd, treeId, delayed);
            }
        }

        /// <summary>
        /// Move evaluation finished while in the ENGINE_GAME mode.
        /// This could be while playing a game against the engine that
        /// started in MANUAL_REVIEW learning mode,
        /// or during the training i.e. in TRAINING learning mode.
        /// </summary>
        private static void MoveEvaluationFinishedInGame(TreeNode nd)
        {
            if (EvaluationManager.CurrentMode == EvaluationManager.Mode.ENGINE_GAME)
            {
                ProcessEngineGameMove(nd);
                _mainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                _mainWin.ResetEvaluationProgressBar();
                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);

                if (TrainingSession.IsTrainingInProgress)
                {
                    _mainWin.EngineTrainingGameMoveMade();
                }
            }
        }

        /// <summary>
        /// Move evaluation finished while in the TRAINING mode.
        /// Note that this is in manual TRAINING mode, not during a game.
        /// The engine was evaluating the move on user's request not responding
        /// to the user's move in a game.
        /// </summary>
        private static void MoveEvaluationFinishedInTraining(TreeNode nd, bool delayed)
        {
            // stop the timer, apply training mode specific handling 
            // NOTE do not reset Evaluation.CurrentMode as this will be done 
            // later down the chain
            _mainWin.ResetEvaluationProgressBar();
            _mainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);

            EvaluationManager.SetSingleNodeToEvaluate(null);

            _mainWin.MoveEvaluationFinishedInTraining(nd, delayed);
        }

        /// <summary>
        /// Move evaluation finished while in the MANUAL_REVIEW.
        /// This is either CONTINUOUS evaluation (i.e. an "infinite"
        /// evaluation of the position that is currently selected 
        /// in Active Line or a LINE evaluation where we ask for 
        /// evaluation move by move automatically.
        /// </summary>
        private static void MoveEvaluationFinishedInManualReview(TreeNode nd, int treeId, bool delayed)
        {
            int index = AppStateManager.ActiveLine.GetIndexForNode(nd);
            if (index >= 0)
            {
                lock (LearningMode.EvalLock)
                {
                    AppLog.Message("Move evaluation finished for index " + index.ToString());

                    string eval = nd.EngineEvaluation;

                    bool isWhiteEval = (index - 1) % 2 == 0;
                    int moveIndex = (index - 1) / 2;

                    AppStateManager.ActiveLine.SetEvaluation(nd, eval);

                    if (EvaluationManager.CurrentMode != EvaluationManager.Mode.CONTINUOUS)
                    {
                        _mainWin.Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                        _mainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                    }

                    if (ContinueLineEvaluation())
                    {
                        if (!delayed)
                        {
                            AppLog.Message("Continue evaluation next move after index " + index.ToString());
                            ClearMoveCandidates(false);
                            AppStateManager.MainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                            RequestMoveEvaluation(index + 1, EvaluationManager.GetNextLineNodeToEvaluate(), treeId);

                            _mainWin.Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                        }
                    }
                    else
                    {
                        if (EvaluationManager.CurrentMode != EvaluationManager.Mode.CONTINUOUS)
                        {
                            EvaluationManager.Reset();
                        }
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
            return EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE
                && !EvaluationManager.IsLastPositionIndex();
        }

        /// <summary>
        /// We have a response from the engine so need to choose
        /// the move from the list of candidates, show it on the board
        /// and display in the Engine Game Line view.
        /// </summary>
        private static void ProcessEngineGameMove(TreeNode nd)
        {
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
                nd.Position.IsCheckmate = true;
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
        public static void RequestMoveEvaluation(int nodeIndex, TreeNode nd, int treeId)
        {
            if (!IsEngineAvailable
                || WorkbookManager.SessionWorkbook == null
                || _mainWin.ActiveTreeView == null
                || nd == null
                || nd.Parent == null)
            {
                return;
            }

            _mainWin.Dispatcher.Invoke(() =>
            {
                if (EvaluationManager.CurrentMode == EvaluationManager.Mode.IDLE)
                {
                    EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.CONTINUOUS);
                }

                _mainWin.DisplayPosition(nd);

                if (AppStateManager.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW && EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE)
                {
                    _mainWin.ActiveLine.SelectPly((int)nd.Parent.Position.MoveNumber, nd.Parent.Position.ColorToMove);
                    _mainWin.SelectLineAndMoveInWorkbookViews(_mainWin.ActiveTreeView, _mainWin.ActiveLine.GetLineId(), nodeIndex);
                }
            });

            AppStateManager.ShowMoveEvaluationControls(true);

            string fen = AppStateManager.PrepareMoveEvaluation(EvaluationManager.GetEvaluatedNode(out _).Position, true);
            GoFenCommand.EvaluationMode mode = ConvertEvaluationManagertoFenEvaluationMode(EvaluationManager.CurrentMode);
            RequestEngineEvaluation(mode, nd, treeId, fen, Configuration.EngineMpv, Configuration.EngineEvaluationTime);
        }

        /// <summary>
        /// Converts EvaluationManager.Mode to GoFenCommand.EvaluationMode 
        /// </summary>
        /// <param name="mgrMode"></param>
        /// <returns></returns>
        public static GoFenCommand.EvaluationMode ConvertEvaluationManagertoFenEvaluationMode(EvaluationManager.Mode mgrMode)
        {
            switch (mgrMode)
            {
                case EvaluationManager.Mode.CONTINUOUS:
                    return GoFenCommand.EvaluationMode.CONTINUOUS;
                case EvaluationManager.Mode.LINE:
                    return GoFenCommand.EvaluationMode.LINE;
                case EvaluationManager.Mode.ENGINE_GAME:
                    return GoFenCommand.EvaluationMode.GAME;
                default:
                    return GoFenCommand.EvaluationMode.NONE;
            }
        }

        /// <summary>
        /// This method will be called when in Training mode to evaluate
        /// user's move or moves from the Workbook.
        /// </summary>
        /// <param name="nodeId"></param>
        public static void RequestMoveEvaluationInTraining(int nodeId, int treeId)
        {
            TreeNode nd = _mainWin.ActiveVariationTree.GetNodeFromNodeId(nodeId);
            RequestMoveEvaluationInTraining(nd, treeId);
        }

        /// <summary>
        /// Requests evaluation while in Training mode.
        /// </summary>
        /// <param name="nd"></param>
        public static void RequestMoveEvaluationInTraining(TreeNode nd, int treeId)
        {
            if (nd != null)
            {
                EvaluationManager.SetSingleNodeToEvaluate(nd);
                string fen = FenParser.GenerateFenFromPosition(nd.Position);

                AppStateManager.ShowMoveEvaluationControls(true, false);
                AppStateManager.PrepareMoveEvaluation(fen, true);

                int moveTime;
                switch (EvaluationManager.CurrentMode)
                {
                    case EvaluationManager.Mode.ENGINE_GAME:
                        moveTime = Configuration.EngineMoveTime;
                        break;
                    case EvaluationManager.Mode.LINE:
                        moveTime = Configuration.EngineEvaluationTime;
                        break;
                    case EvaluationManager.Mode.CONTINUOUS:
                        moveTime = 0;
                        break;
                    default:
                        AppLog.Message("ERROR: RequestMoveEvaluationInTraining() invalid mode: " + EvaluationManager.CurrentMode.ToString());
                        moveTime = Configuration.EngineEvaluationTime;
                        break;
                }

                GoFenCommand.EvaluationMode mode = ConvertEvaluationManagertoFenEvaluationMode(EvaluationManager.CurrentMode);
                RequestEngineEvaluation(mode, nd, treeId, fen, Configuration.EngineMpv, moveTime);
            }
        }

        /// <summary>
        /// Prepares controls, timers 
        /// and requests the engine to make move.
        /// </summary>
        /// <param name="node"></param>
        public static void RequestEngineMove(TreeNode node, int treeId)
        {
            if (!IsEngineAvailable)
            {
                MessageBox.Show("Chess Engine not available", "Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.ENGINE_GAME);

            string fen = AppStateManager.PrepareMoveEvaluation(node.Position, false);
            RequestEngineEvaluation(GoFenCommand.EvaluationMode.GAME, node, treeId, fen, Configuration.EngineMpv, Configuration.EngineMoveTime);
        }

        /// <summary>
        /// Sends a sequence of commands to the engine to request evaluation
        /// of the position.
        /// </summary>
        /// <param name="fen">The position to analyze in the FEN format.</param>
        /// <param name="mpv">Number of options to return.</param>
        /// <param name="movetime">Time to think per move (milliseconds)</param>
        private static void RequestEngineEvaluation(GoFenCommand.EvaluationMode evalMode, TreeNode nd, int treeId, string fen, int mpv, int movetime)
        {
            GoFenCommand gfc = new GoFenCommand();
            gfc.Fen = fen;
            gfc.Mpv = mpv;
            gfc.EvalMode = evalMode;
            gfc.GoCommandString = movetime > 0 ? UciCommands.ENG_GO_MOVE_TIME + " " + movetime.ToString() : UciCommands.ENG_GO;
            if (nd != null)
            {
                gfc.NodeId = nd.NodeId;
            }
            gfc.TreeId = treeId;
            ChessEngineService.SendFenGoCommand(gfc);
        }

        /// <summary>
        /// Request evaluation of a position.
        /// </summary>
        /// <param name="index">Index of the position in the main line</param>
        /// <param name="mpv"></param>
        /// <param name="movetime"></param>
        public static void RequestPositionEvaluation(TreeNode nd, int treeId, int mpv, int movetime)
        {
            _mainWin.Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
            ClearMoveCandidates(true);

            if (nd != null)
            {
                string fen = FenParser.GenerateFenFromPosition(nd.Position);

                GoFenCommand.EvaluationMode mode = ConvertEvaluationManagertoFenEvaluationMode(EvaluationManager.CurrentMode);
                RequestEngineEvaluation(mode, nd, treeId, fen, mpv, movetime);

                AppStateManager.ShowMoveEvaluationControls(true);
                _mainWin.Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
            }
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
            try
            {
                if (message != null)
                {
                    // Info and Best Move messages will begin with TreeId
                    message = ParseMessagePrefix(message, out int treeId, out int nodeId, out bool delayed, out GoFenCommand.EvaluationMode mode);

                    TreeNode evalNode = AppStateManager.GetNodeByIds(treeId, nodeId);
                    _lastMessageNode = evalNode;

                    if ((LearningMode.CurrentMode == LearningMode.Mode.ENGINE_GAME) || evalNode != null)
                    {
                        if (evalNode == null)
                        {
                            AppLog.Message("null evalNode in EngineMessageReceived()");
                        }
                        if (message.StartsWith(UciCommands.ENG_INFO))
                        {
                            ProcessInfoMessage(message);
                        }
                        else if (message.StartsWith(UciCommands.ENG_BEST_MOVE))
                        {
                            ProcessBestMoveMessage(message, evalNode, treeId, mode, delayed);
                        }
                    }
                    else
                    {
                        if (message.StartsWith(UciCommands.ENG_ID_NAME))
                        {
                            string engineName = message.Substring(UciCommands.ENG_ID_NAME.Length).Trim();
                            AppStateManager.EngineName = engineName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("ERROR: processing engine message: " + ex.Message);
                DebugUtils.ShowDebugMessage("Error processing engine message: " + ex.Message);
            }
        }

        /// <summary>
        /// Checks if the message comes with the prefix containing IDs
        /// and if so gets their values and removes from the message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="treeId"></param>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private static string ParseMessagePrefix(string message, out int treeId, out int nodeId, out bool delayed, out GoFenCommand.EvaluationMode mode)
        {
            treeId = -1;
            nodeId = -1;
            delayed = false;

            if (message != null && message.StartsWith(UciCommands.CHF_DELAYED_PREFIX))
            {
                delayed = true;
                message = message.Substring(UciCommands.CHF_DELAYED_PREFIX.Length + 1);
            }

            mode = GoFenCommand.EvaluationMode.NONE;

            if (message != null && message.StartsWith(UciCommands.CHF_TREE_ID_PREFIX))
            {
                int pos = FindThirdSpaceInString(message);
                if (pos > 0)
                {
                    string[] idTokens = message.Substring(0, pos).Split(' ');
                    treeId = GetIntValueFromPair(idTokens[0], '=');
                    nodeId = GetIntValueFromPair(idTokens[1], '=');
                    mode = (GoFenCommand.EvaluationMode)GetIntValueFromPair(idTokens[2], '=');
                    return message.Substring(pos + 1);
                }
                else
                {
                    // should never happen
                    return "";
                }
            }
            else
            {
                return message;
            }
        }

        /// <summary>
        /// Converts the second part of the string, as separated
        /// by the splitChar to an int.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="splitChar"></param>
        /// <returns></returns>
        private static int GetIntValueFromPair(string text, char splitChar)
        {
            string[] tokens = text.Split(splitChar);

            int id;
            if (int.TryParse(tokens[1], out id))
            {
                return id;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Finds the third occurence of the space character
        /// in the passed string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static int FindThirdSpaceInString(string str)
        {
            int thirdSpacePos = -1;

            bool firstSpaceFound = false;
            bool secondSpaceFound = false;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == ' ')
                {
                    if (firstSpaceFound && !secondSpaceFound)
                    {
                        secondSpaceFound = true;
                    }
                    else if (secondSpaceFound)
                    {
                        thirdSpacePos = i;
                        break;
                    }
                    else
                    {
                        firstSpaceFound = true;
                    }
                }
            }

            return thirdSpacePos;
        }

        /// <summary>
        /// The "bestmove" message has been received which indicates
        /// the completion of evaluations.
        /// Invoke the final processing
        /// </summary>
        /// <param name="message"></param>
        private static void ProcessBestMoveMessage(string message, TreeNode nd, int treeId, GoFenCommand.EvaluationMode mode, bool delayed)
        {
            try
            {
                // make sure the last lines are shown before we stop the timer.
                EngineLinesBox.ShowEngineLines(null, null);

                if (nd != null)
                {
                    if (EngineLinesBox.Lines.Count > 0)
                    {
                        nd.EngineEvaluation = EvaluationManager.BuildEvaluationText(EngineLinesBox.Lines[0], nd.Position.ColorToMove);
                    }
                }

                // tell the app that the evaluation has finished
                MoveEvaluationFinished(nd, treeId, mode, delayed);
            }
            catch (Exception ex)
            {
                AppLog.Message("ERROR: processing \"bestmove\" message: " + ex.Message);
                AppLog.TreeNodeDetails(nd);
                DebugUtils.ShowDebugMessage("Error processing \"bestmove\": " + ex.Message);
            }
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

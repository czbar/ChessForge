using ChessPosition;
using GameTree;
using System.Windows;
using System.Windows.Documents;

namespace ChessForge
{
    /// <summary>
    /// Partial class handling move requests and move received notifications.
    /// </summary>
    public partial class TrainingView
    {

        //**************************************************
        //
        // MOVES made while a TRAINING GAME is in progress
        //
        //**************************************************

        /// <summary>
        /// This is called from MainWindow.MoveEvaluationFinished()
        /// when engine produced a move while playing with the user.
        /// </summary>
        public void EngineGameMoveMade()
        {
            if (!(EngineMessageProcessor.ActiveEvaluationMode == EngineService.GoFenCommand.EvaluationMode.GAME))
            {
                TreeNode nd = EngineGame.GetLastGameNode();
                nd.IsNewTrainingMove = true;
                AddMoveToEngineGamePara(nd, false);
                _mainWin.UiRtbTrainingProgress.ScrollToEnd();
                if (TrainingSession.IsContinuousEvaluation)
                {
                    ShowEvaluationResult(nd, false);
                }

                if (TrainingSession.IsContinuousEvaluation)
                {
                    _lastClickedNode = nd;
                    // request evaluation while user is thinking
                    StartEvaluationInContinuousMode(true);
                }
            }
        }

        /// <summary>
        /// This is called from EngineGame.ProcessUserGameMove()
        /// after the user's move made on the chessboard was processed.
        /// </summary>
        public void UserGameMoveMade()
        {
            TrainingSession.IsTakebackAvailable = false;
            RemoveTakebackParagraph();

            TreeNode nd = EngineGame.GetLastGameNode();
            AddMoveToEngineGamePara(nd, true);
            _mainWin.UiRtbTrainingProgress.ScrollToEnd();
            if (TrainingSession.IsContinuousEvaluation)
            {
                ShowEvaluationResult(nd, false);
            }
        }

        /// <summary>
        /// This method is called directly when the user
        /// made their move and the training is in engine game mode
        /// (as opposed to the manual mode).
        /// This method requests the engine to make a move.
        /// </summary>
        public void StartTrainingEngineGame()
        {
            int nodeId = _userMove.NodeId;
            _mainWin.StartEngineGame(_userMove, true);
        }


        //**************************************************
        //
        // MOVES made while within the WORKBOOK
        //
        //**************************************************

        /// <summary>
        /// This method is invoked when user makes their move.
        /// 
        /// It gets the last ply from the EngineGame.Line (which is the
        /// move made by the user) and finds its parent in the Workbook.
        /// 
        /// NOTE: If the parent is not in the Workbook, this method should
        /// not have been invoked as the TrainingMode should know that
        /// we are "out of the book".
        /// 
        /// Having found the parent, checks if the user's move corresponds to any move
        /// in the Workbook (i.e. children of that parent) and reports accordingly.
        /// </summary>
        public void ReportLastMoveVsWorkbook()
        {
            AppLog.Message("ReportLastMoveVsWorkbook()");

            // if we got here via a rollback, the CHECK_FOR_USER_MOVE may not have been stopped.
            _mainWin.Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
            
            TrainingSession.IsTakebackAvailable = false;
            RemoveTakebackParagraph();

            // this method may be called after we exited training while exiting application
            // and there is no ActiveVariationTree.
            if (_mainWin.ActiveVariationTree == null)
            {
                return;
            }

            lock (TrainingSession.UserVsWorkbookMoveLock)
            {
                if (TrainingSession.CurrentState != TrainingSession.State.USER_MOVE_COMPLETED)
                {
                    return;
                }

                if (TrainingSession.IsContinuousEvaluation)
                {
                    // TODO strengthen the condition above?  (EngineMode != GAME))
                    RequestMoveEvaluation(_mainWin.ActiveVariationTreeId, true);
                }

                RemoveIntroParas();

                _otherMovesInWorkbook.Clear();

                _userMove = EngineGame.GetLastGameNode();
                _lastUserMoveNodeId = _userMove.NodeId;

                TreeNode parent = _userMove.Parent;

                TreeNode foundMove = null;
                foreach (TreeNode child in parent.Children)
                {
                    // we cannot use ArePositionsIdentical() because _userMove only has static position
                    if (child.LastMoveEngineNotation == _userMove.LastMoveEngineNotation && !_userMove.IsNewTrainingMove)
                    {
                        // replace the TreeNode with the one from the Workbook so that
                        // we stay with the workbook as long as the user does.
                        EngineGame.ReplaceLastPly(child);
                        foundMove = child;
                        _userMove = child;
                    }
                    else
                    {
                        if (!child.IsNewTrainingMove && (!child.IsNullMove || child.Children.Count > 0))
                        {
                            _otherMovesInWorkbook.Add(child);
                        }
                    }
                }

                if (PositionUtils.IsCheckmate(_userMove.Position, out _))
                {
                    _userMove.Position.IsCheckmate = true;
                    BuildMoveParagraph(_userMove, true);
                    if (foundMove == null)
                    {
                        InsertCommentIntoUserMovePara(false, _userMove);
                    }
                    BuildCheckmateParagraph(_userMove, true);
                }
                else if (PositionUtils.IsStalemate(_userMove.Position))
                {
                    BuildMoveParagraph(_userMove, true);
                    if (foundMove == null)
                    {
                        InsertCommentIntoUserMovePara(false, _userMove);
                    }
                    BuildStalemateParagraph(_userMove);
                }
                else if (PositionUtils.IsInsufficientMaterial(_userMove.Position))
                {
                    BuildMoveParagraph(_userMove, true);
                    if (foundMove == null)
                    {
                        InsertCommentIntoUserMovePara(false, _userMove);
                    }
                    BuildInsufficientMaterialParagraph(_userMove);
                }
                else
                {
                    // double check that we have the parent in our Workbook
                    if (_mainWin.ActiveVariationTree.GetNodeFromNodeId(parent.NodeId) == null)
                    {
                        // we are "out of the book" in our training so there is nothing to report
                        DebugUtils.ShowDebugMessage("ReportLastMoveVsWorkbook() : parent not found");
                        return;
                    }

                    BuildMoveParagraph(_userMove, true);
                    InsertCommentIntoUserMovePara(foundMove != null, _userMove);

                    // if we found a move and this is not the last move in the Workbbook, request response.
                    if (foundMove != null && TreeUtils.NonNullChildrenCount(foundMove, false) > 0)
                    {
                        // start the timer that will trigger a workbook response by RequestWorkbookResponse()
                        TrainingSession.ChangeCurrentState(TrainingSession.State.AWAITING_WORKBOOK_RESPONSE);
                        _mainWin.Timers.Start(AppTimers.TimerId.REQUEST_WORKBOOK_MOVE);
                    }
                    else
                    {
                        // delete if exists
                        Paragraph oldPara = FindParagraphByName(HostRtb.Document, _par_game_moves_, false);
                        if (oldPara != null)
                        {
                            HostRtb.Document.Blocks.Remove(oldPara);
                        }
                        _paraCurrentEngineGame = AddNewParagraphToDoc(HostRtb.Document, STYLE_ENGINE_GAME, "");
                        _paraCurrentEngineGame.Name = _par_game_moves_;
                        if (foundMove != null)
                        {
                            Run rWbEnded = new Run("\n" + Properties.Resources.TrnLineEnded + ".");
                            SoundPlayer.PlayTrainingSound(SoundPlayer.Sound.END_OF_LINE);
                            rWbEnded.Name = _run_wb_ended_;
                            _paraCurrentEngineGame.Inlines.Add(rWbEnded);
                        }

                        if (!EngineMessageProcessor.IsEngineAvailable)
                        {
                            MessageBox.Show(Properties.Resources.NoEngineForTraining, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        else
                        {
                            _paraCurrentEngineGame.Inlines.Add(new Run("\n" + Properties.Resources.TrnGameStarted + "\n"));
                            _engineGameRootNode = _userMove;

                            // call RequestEngineResponse() directly so it invokes PlayEngine
                            LearningMode.ChangeCurrentMode(LearningMode.Mode.ENGINE_GAME);
                            AppState.SetupGuiForCurrentStates();
                            StartTrainingEngineGame();
                        }
                    }
                }
            }
            _mainWin.UiRtbTrainingProgress.ScrollToEnd();
        }

        /// <summary>
        /// When the user made their move, and the training is in
        /// manual mode (as opposed to a game vs engine)
        /// a timer was started to invoke
        /// this method (via InvokeRequestWorkbookResponse).
        /// This method performs the move and starts the timer
        /// so that is gets picked up by EngineGame.CheckForTrainingWorkbookMoveMade.
        /// </summary>
        public void RequestWorkbookResponse()
        {
            try
            {
                int nodeId = _userMove.NodeId;
                _mainWin.Timers.Stop(AppTimers.TimerId.REQUEST_WORKBOOK_MOVE);

                // user may have chosen a different move to what we originally had
                // TODO: after the re-think of the GUI that probably cannot happen (?)
                EngineGame.ReplaceLastPly(nodeId);

                TreeNode userChoiceNode = _mainWin.ActiveVariationTree.GetNodeFromNodeId(nodeId);

                _mainWin.DisplayPosition(userChoiceNode);
                _mainWin.ColorMoveSquares(_userMove.LastMoveEngineNotation);

                // consult TrainingSession to find Workbook response
                TreeNode nd = TrainingSession.GetNextTrainingLineMove(userChoiceNode);

                // nd should never be null here, but if it is, we will use the first child of the user move
                if (nd == null && TreeUtils.NonNullChildrenCount(userChoiceNode) > 0)
                {
                    nd = TreeUtils.GetFirstNonNullChild(userChoiceNode);
                }

                EngineGame.AddPlyToGame(nd);

                // The move will be visualized in response to CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE timer's elapsed event
                EngineGame.IsTrainingWorkbookMoveMade = true;
                _mainWin.Timers.Start(AppTimers.TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE);
                AppState.SwapCommentBoxForEngineLines(TrainingSession.IsContinuousEvaluation);
                AppState.ConfigureMenusForTraining();
            }
            catch
            {
                MessageBox.Show(Properties.Resources.UnexpectedErrorInUserMove, Properties.Resources.UnexpectedError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //********************************************************
        //
        // ENGINE EVALUATION REQUESTS
        //
        //********************************************************

        /// <summary>
        /// Invoked from the Training context menu.
        /// Starts evaluation of the clicked move.
        /// Alternatively, can be called as part of line 
        /// evaluation.
        /// </summary>
        public void RequestMoveEvaluation(int treeId, bool lastMove = false)
        {
            if (!EngineMessageProcessor.IsEngineAvailable)
            {
                _mainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.EngineNotAvailable, CommentBox.HintType.ERROR);
                return;
            }

            if (EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE)
            {
                TreeNode nd = EvaluationManager.GetNextLineNodeToEvaluate();
                if (nd == null)
                {
                    // the LINE evaluation has finished
                    EvaluationManager.ResetLineNodesToEvaluate();
                    EvaluationManager.SetSingleNodeToEvaluate(null);

                    if (TrainingSession.IsContinuousEvaluation)
                    {
                        StartEvaluationInContinuousMode(true);
                    }
                    else
                    {
                        EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);
                    }
                }
                else
                {
                    AppLog.Message("TrainingView:RequestMoveEvaluation " + nd.LastMoveAlgebraicNotation);
                    EngineMessageProcessor.RequestMoveEvaluationInTraining(nd, treeId);
                }
            }
            else
            {
                // we could be in GAME mod
                if (EngineMessageProcessor.ActiveEvaluationMode != EngineService.GoFenCommand.EvaluationMode.GAME)
                {
                    StartEvaluationInContinuousMode(lastMove);
                }
            }
        }

        /// <summary>
        /// Requests evaluation of a  line.
        /// Checks if this is for the Main Line or an Engine Game,
        /// sets up a list of Nodes and Runs to evaluate 
        /// and calls RequestMoveEvaluation().
        /// Sets Evaluation.CurrentMode to TRAINING_LINE to ensure
        /// that evaluation does not stop after the first move.
        /// </summary>
        public void RequestLineEvaluation()
        {
            if (!EngineMessageProcessor.IsEngineAvailable)
            {
                AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.EngineNotAvailable, CommentBox.HintType.ERROR);
                return;
            }

            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                EvaluationManager.ResetLineNodesToEvaluate();

                // figure out whether this is for the Main Line or Engine Game
                Run firstRun = _lastClickedRun;
                Paragraph parentPara = firstRun.Parent as Paragraph;
                if (firstRun != null)
                {
                    string paraName = parentPara.Name;
                    if (paraName.StartsWith(_par_line_moves_))
                    {
                        EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.LINE, true, EvaluationManager.LineSource.TRAINING_LINE);
                        SetMainLineRunsToEvaluate(paraName, _lastClickedRun);
                        Paragraph gamePara = FindParagraphByName(HostRtb.Document, _par_game_moves_, true);
                        if (gamePara != null)
                        {
                            SetGameRunsToEvaluate(gamePara, null);
                        }
                        RequestMoveEvaluation(_mainWin.ActiveVariationTreeId);
                    }
                    else if (paraName.StartsWith(_par_game_moves_))
                    {
                        EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.LINE, true, EvaluationManager.LineSource.TRAINING_LINE);
                        SetGameRunsToEvaluate(parentPara, _lastClickedRun);
                        RequestMoveEvaluation(_mainWin.ActiveVariationTreeId);
                    }
                }
            });
        }

        /// <summary>
        /// Request evaluation of a move in the CONTNUOUS mode.
        /// </summary>
        /// <param name="lastMove"></param>
        private void StartEvaluationInContinuousMode(bool lastMove)
        {
            EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.CONTINUOUS);
            if (_lastClickedNode == null || lastMove)
            {
                _lastClickedNode = EngineGame.GetLastGameNode();
            }
            EngineMessageProcessor.RequestMoveEvaluationInTraining(_lastClickedNode, _mainWin.ActiveVariationTreeId);
        }

    }
}

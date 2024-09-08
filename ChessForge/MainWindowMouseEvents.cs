using ChessPosition;
using GameTree;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Mouse event handlers for the Main Window class.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Indicates whether the Main Window is currently processing the mouse up event.
        /// This would normally be after the user made their move and can be resource intensive. 
        /// Therefore, other processes should query this flag and limit their processing if it is up.
        /// </summary>
        public bool ProcessingMouseUp
        {
            get => _processingMouseUp;
        }

        // a flag to use to prevent processing MouseDown before MouseUp is finished.
        private bool _processingMouseUp = false;

        //**************************************************************
        //
        //  MAIN AREA mouse events 
        // 
        //**************************************************************

        /// <summary>
        /// The user pressed the mouse button over the board.
        /// If it is a left button it indicates the commencement of
        /// an intended move.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_processingMouseUp)
            {
                AppLog.Message("OnMouseDown rejected: processing MouseUp");
                e.Handled = true;
                return;
            }

            Point clickedPoint = e.GetPosition(UiImgMainChessboard);
            SquareCoords sq = MainChessBoard.ClickedSquare(clickedPoint);
            if (sq == null)
            {
                return;
            }

            if (e.ChangedButton == MouseButton.Right)
            {
                _lastRightClickedPoint = clickedPoint;

                // allow drawing shapes in MANUAL_REVIEW mode
                if (AppState.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW && AppState.IsTabAllowingBoardDraw)
                {
                    // if no special key was pressed we consider this tentative
                    // since we need to resolve between shape building and context menu
                    if (ActiveVariationTree != null)
                    {
                        bool isDrawTentative = !GuiUtilities.IsSpecialKeyPressed();
                        MainChessBoard.Shapes.StartShapeDraw(sq, "", isDrawTentative);
                    }
                }
            }
            else
            {
                // sanity check
                if (DraggedPiece.isDragInProgress)
                {
                    // if Drag is in progress there is something wrong
                    DebugUtils.ShowDebugMessage("Incomplete drag event. Dump all logs and report!");
                    DebugDumps.LogDraggedPiece();
                    // restore the dragged piece to its origin square
                    ReturnDraggedPiece(false);
                }

                _lastRightClickedPoint = null;
                if (EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE)
                {
                    if (EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE)
                    {
                        BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.InfoLineEvalInProgress, CommentBox.HintType.ERROR);
                        return;
                    }
                    else if (EvaluationManager.CurrentMode == EvaluationManager.Mode.ENGINE_GAME)
                    {
                        BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.InfoEngineThinking, CommentBox.HintType.ERROR);
                        return;
                    }
                }

                if (e.ChangedButton == MouseButton.Left)
                {
                    if (sq != null)
                    {
                        SquareCoords sqNorm = new SquareCoords(sq);
                        if (MainChessBoard.IsFlipped)
                        {
                            sqNorm.Flip();
                        }

                        if (MainChessBoard.GetPieceColor(sqNorm) == PieceColor.None)
                        {
                            MainChessBoard.Shapes.Reset(true);
                        }

                        if (CanMovePiece(sqNorm))
                        {
                            DraggedPiece.isDragInProgress = true;
                            DraggedPiece.OriginSquare = sq;

                            DraggedPiece.ImageControl = MainChessBoard.GetImageFromPoint(clickedPoint);
                            Point ptLeftTop = MainChessBoard.GetSquareTopLeftPointOffCanvas(sq);
                            DraggedPiece.PtDraggedPieceOrigin = ptLeftTop;

                            // for the remainder, we need absolute point
                            clickedPoint.X += UiImgMainChessboard.Margin.Left;
                            clickedPoint.Y += UiImgMainChessboard.Margin.Top;
                            DraggedPiece.PtStartDragLocation = clickedPoint;


                            Point ptCenter = MainChessBoard.GetSquareCenterPoint(sq);

                            Canvas.SetLeft(DraggedPiece.ImageControl, ptLeftTop.X + (clickedPoint.X - ptCenter.X));
                            Canvas.SetTop(DraggedPiece.ImageControl, ptLeftTop.Y + (clickedPoint.Y - ptCenter.Y));
                        }
                        else
                        {
                            AppLog.Message("OnMouseDown rejected: CanMovePiece returned false");
                            // if we can't move because we're in exercise hiding mode, try to help the user
                            if (AppState.ActiveTab == TabViewType.EXERCISE && ActiveArticle != null && ActiveArticle.Solver != null && !ActiveArticle.Solver.IsMovingAllowed())
                            {
                                if (_exerciseTreeView != null)
                                {
                                    if (MessageBox.Show(Properties.Resources.ViewExercise, Properties.Resources.Exercise, MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                    {
                                        _exerciseTreeView.EventShowHideButtonClicked(null, null);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// Depending on the Application and/or Training mode,
        /// this may have been the user completing a move.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_processingMouseUp)
            {
                AppLog.Message("OnMouseUp rejected: processing MouseUp");
                return;
            }

            _processingMouseUp = true;

            try
            {
                Point clickedPoint = e.GetPosition(UiImgMainChessboard);
                SquareCoords targetSquare = MainChessBoard.ClickedSquare(clickedPoint);

                if (e.ChangedButton == MouseButton.Right)
                {
                    HandleMouseUpRightButton(targetSquare, e);
                }
                else
                {
                    if (DraggedPiece.isDragInProgress)
                    {
                        try
                        {
                            if (targetSquare == null)
                            {
                                // just put the piece back
                                Canvas.SetLeft(DraggedPiece.ImageControl, DraggedPiece.PtDraggedPieceOrigin.X);
                                Canvas.SetTop(DraggedPiece.ImageControl, DraggedPiece.PtDraggedPieceOrigin.Y);
                            }
                            else
                            {
                                // double check that we are legitimately making a move
                                if (LearningMode.CurrentMode == LearningMode.Mode.ENGINE_GAME && EngineGame.CurrentState == EngineGame.GameState.USER_THINKING
                                    || LearningMode.CurrentMode == LearningMode.Mode.TRAINING && TrainingSession.CurrentState == TrainingSession.State.AWAITING_USER_TRAINING_MOVE
                                    || LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW)
                                {
                                    if (AppState.ActiveTab == TabViewType.INTRO)
                                    {
                                        HandleMoveOnIntroView(targetSquare);
                                    }
                                    else
                                    {
                                        AdjustEvaluationModeAfterUserMove();
                                        UserMoveProcessor.FinalizeUserMove(targetSquare);
                                    }
                                }
                                else
                                {
                                    ReturnDraggedPiece(false);
                                }
                            }
                            Canvas.SetZIndex(DraggedPiece.ImageControl, Constants.ZIndex_PieceOnBoard);
                        }
                        catch
                        {
                        }

                        DraggedPiece.isDragInProgress = false;
                    }
                }
            }
            catch
            {
            }

            if (AppState.MainWin.ActiveTreeView != null && AppState.ActiveTab != TabViewType.INTRO)
            {
                try
                {
                    AppState.DoEvents();
                    // it is possible that since the check above ActiveTreeView was set to null! Hence the try block.
                    AppState.MainWin.ActiveTreeView.BringSelectedRunIntoView();
                }
                catch
                {
                }
            }

            _processingMouseUp = false;
        }

        /// <summary>
        /// Handles moves made in the Intro View.
        /// </summary>
        /// <param name="targetSquare"></param>
        private void HandleMoveOnIntroView(SquareCoords targetSquare)
        {
            if (_introView != null)
            {
                TreeNode nd = new TreeNode(null, "", 0);
                nd.Position = new BoardPosition(MainChessBoard.DisplayedPosition);
                nd.LastMoveAlgebraicNotation = RepositionPieceProcessor.RepositionDraggedPiece(targetSquare, false, ref nd);

                _introView.InsertMove(nd);
            }
        }

        /// <summary>
        /// This is called when the user made their move.
        /// We want to make sure that any current evaluation is stopped if this is a GAME mode, in which case
        /// the caller is responsible to trigger a response and set evaluation to GAME...
        /// In the other modes we stop evaluations if not in CONTINUOUS mode.
        /// </summary>
        private void AdjustEvaluationModeAfterUserMove()
        {
            if (EvaluationManager.CurrentMode != EvaluationManager.Mode.IDLE)
            {
                if (LearningMode.CurrentMode == LearningMode.Mode.ENGINE_GAME)
                {
                    StopEvaluation(true);
                }
                else if (EvaluationManager.CurrentMode != EvaluationManager.Mode.CONTINUOUS)
                {
                    EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);
                }
            }
        }

        /// <summary>
        /// Handles move of the mouse.
        /// This may be the user dragging the mouse to make a move
        /// or to draw an arrow.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_processingMouseUp)
            {
                return;
            }

            Point mousePoint = e.GetPosition(UiImgMainChessboard);
            SquareCoords sq = MainChessBoard.ClickedSquare(mousePoint);

            // if right button is pressed we may be drawing an arrow
            if (e.RightButton == MouseButtonState.Pressed)
            {
                HandleMouseMoveRightButton(sq, mousePoint);
            }
            else
            {
                _lastRightClickedPoint = null;
                if (MainChessBoard.Shapes.IsShapeBuildInProgress)
                {
                    MainChessBoard.Shapes.CancelShapeDraw(true);
                }
                if (DraggedPiece.isDragInProgress)
                {
                    Canvas.SetZIndex(DraggedPiece.ImageControl, Constants.ZIndex_PieceInAnimation);
                    mousePoint.X += UiImgMainChessboard.Margin.Left;
                    mousePoint.Y += UiImgMainChessboard.Margin.Top;

                    Canvas.SetLeft(DraggedPiece.ImageControl, mousePoint.X - squareSize / 2);
                    Canvas.SetTop(DraggedPiece.ImageControl, mousePoint.Y - squareSize / 2);
                }
            }
        }

        /// <summary>
        /// Handles mouse move event with Right Button pressed
        /// A shape building would have been started by a right click
        /// with or without special key being pressed.
        /// </summary>
        private void HandleMouseMoveRightButton(SquareCoords sq, Point ptCurrent)
        {
            if (MainChessBoard.Shapes.IsShapeBuildInProgress)
            {
                bool proceed = true;

                // check if we are tentative
                if (MainChessBoard.Shapes.IsShapeBuildTentative)
                {
                    if (_lastRightClickedPoint == null)
                    {
                        MainChessBoard.Shapes.CancelShapeDraw(true);
                        proceed = false;
                    }
                    else
                    {
                        // check if we should proceed or let context menu show
                        if (Math.Abs(GuiUtilities.CalculateDistance(_lastRightClickedPoint.Value, ptCurrent)) > 5)
                        {
                            MainChessBoard.Shapes.IsShapeBuildTentative = false;
                            _lastRightClickedPoint = null;
                            proceed = true;
                        }
                        else
                        {
                            proceed = false;
                        }
                    }
                }

                if (proceed)
                {
                    MainChessBoard.Shapes.UpdateShapeDraw(sq);
                }
            }
        }

        /// <summary>
        /// Handles the release of the right mouse button
        /// if we were in the shape building mode.
        /// </summary>
        /// <param name="targetSquare"></param>
        /// <param name="e"></param>
        private void HandleMouseUpRightButton(SquareCoords targetSquare, MouseButtonEventArgs e)
        {
            if (MainChessBoard.Shapes.IsShapeBuildInProgress && !MainChessBoard.Shapes.IsShapeBuildTentative)
            {
                MainChessBoard.Shapes.FinalizeShape(targetSquare, true, true);
                _lastRightClickedPoint = null;

                if (AppState.ActiveTab == TabViewType.INTRO)
                {
                    if (_introView != null)
                    {
                        _introView.UpdateDiagramShapes(MainChessBoard.DisplayedNode);
                    }
                }

                // we have been building a shape so ensure context menu does not pop up
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the mouse wheel event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Configuration.AllowMouseWheelForMoves
                && LearningMode.CurrentMode != LearningMode.Mode.TRAINING
                && LearningMode.CurrentMode != LearningMode.Mode.ENGINE_GAME
                && (AppState.ActiveTab == TabViewType.STUDY || AppState.ActiveTab == TabViewType.MODEL_GAME || AppState.ActiveTab == TabViewType.EXERCISE))
            {
                if (e.Delta > 0)
                {
                    ActiveLine.HandleKeyDown(Key.Left);
                }
                else if (e.Delta < 0)
                {
                    ActiveLine.HandleKeyDown(Key.Right);
                }
            }
        }


        //**************************************************************
        //
        //  WORKBOOK/CHAPTER VIEW mouse events 
        // 
        //**************************************************************

        /// <summary>
        /// Handles a mouse click in the Workbook's grid. At this point
        /// we disable node specific menu items in case no node was clicked.
        /// If a node was clicked, it will be corrected when the event is handled
        /// in the Run's OnClick handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VariationTreeView_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            int lastClickedNode = -1;
            if (e.ChangedButton == MouseButton.Right)
            {
                AppState.EnableTabViewMenuItems(WorkbookManager.ActiveTab, lastClickedNode, false);
            }
        }

        /// <summary>
        /// Navigate back arrow clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgNavigateBack_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WorkbookLocationNavigator.MoveToPreviousLocation();
            if (e != null)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Navigate forward arrow clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgNavigateForward_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WorkbookLocationNavigator.MoveToNextLocation();
            if (e != null)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Left mouse button released in the Chapters View
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbChaptersView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _chaptersView?.MouseLeftButtonUp(sender, e);
        }

        /// <summary>
        /// Mouse left the Chapters View area
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbChaptersView_MouseLeave(object sender, MouseEventArgs e)
        {
            _chaptersView?.MouseLeave(sender, e);
        }

        /// <summary>
        /// Mouse entered the Chapters View area
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbChaptersView_MouseEnter(object sender, MouseEventArgs e)
        {
            _chaptersView?.MouseEnter(sender, e);
        }


        /// <summary>
        /// Mouse move within the Chapters View area.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbChaptersView_MouseMove(object sender, MouseEventArgs e)
        {
            _chaptersView?.MouseMove(sender, e);
        }


        /// <summary>
        /// DownArrow clicked in the Chapters view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgChapterArrowDown_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            UiRtbChaptersView.ScrollToEnd();
        }

        /// <summary>
        /// UpArrow clicked in the Chapters view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgChapterArrowUp_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            UiRtbChaptersView.ScrollToHome();
        }

        /// <summary>
        /// Chapters view's layout has been updated so we can check if it has a scrollbar
        /// and show/hide the up/down arrows.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbChaptersView_LayoutUpdated(object sender, EventArgs e)
        {
            bool showArrows = GuiUtilities.CheckVerticalScrollBarVisibility(UiRtbChaptersView);

            UiImgChapterArrowUp.Visibility = showArrows ? Visibility.Visible : Visibility.Collapsed;
            UiImgChapterArrowDown.Visibility = showArrows ? Visibility.Visible : Visibility.Collapsed;
        }


        //**************************************************************
        //
        //  TRAINING VIEW mouse events 
        // 
        //**************************************************************


        /// <summary>
        /// Hides the floating board if shown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrainingView_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            ShowTrainingFloatingBoard(false);
        }

        /// <summary>
        /// The user right clicked in the Training View RTB.
        /// Set LastClicked node to null.
        /// It will be adjusted if a run was clicked and the View's handlers will
        /// be invoked nex.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrainingView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //UiTrainingView.SetLastClickedNode(null);
            Dispatcher.Invoke(() =>
            {
                ContextMenu cm = UiMncTrainingView;
                foreach (object o in cm.Items)
                {
                    if (o is MenuItem)
                    {
                        MenuItem mi = o as MenuItem;
                        switch (mi.Name)
                        {
                            case "_mnTrainEvalMove":
                            case "_mnTrainEvalLine":
                            case "_mnTrainRestartGame":
                            case "_mnRollBackTraining":
                            case "_mnTrainSwitchToWorkbook":
                            case "UiMncTrainReplaceEngineMove":
                                mi.Visibility = Visibility.Collapsed;
                                break;
                            case "_mnTrainRestartTraining":
                            case "_mnTrainExitTraining":
                                mi.Visibility = Visibility.Visible;
                                break;
                            default:
                                break;
                        }
                    }

                    if (o is Separator)
                    {
                        ((Separator)o).Visibility = Visibility.Collapsed;
                    }
                }
            });
        }

        //**************************************************************
        //
        //  BOOKMARKS VIEW mouse events 
        // 
        //**************************************************************

        /// <summary>
        /// Handles a mouse click on any of the Bookmark chessboards.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Bookmarks_Chessboards_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Canvas canvas)
            {
                BookmarkManager.ChessboardClickedEvent(canvas.Name, UiMncBookmarks, e);
            }
        }

        /// <summary>
        /// The Bookmarks view was clicked somewhere.
        /// We disable the bookmark menu in case the click was not on a bookmark.
        /// The event is then handled by a bookmark handler, if the click was on
        /// a bookmark and the menus will be enabled accordingly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Bookmarks_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            BookmarkManager.ClickedIndex = -1;
            BookmarkManager.EnableBookmarkMenus(UiMncBookmarks, false);
        }

        /// <summary>
        /// The left paging arrow was clicked in the Bookmarks view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Bookmarks_PreviousPage_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            BookmarkManager.PageDown();
        }

        /// <summary>
        /// The right paging arrow was clicked in the Bookmarks view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Bookmarks_NextPage_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            BookmarkManager.PageUp();
        }


        //**************************************************************
        //
        //  ACTIVE LINE mouse events 
        // 
        //**************************************************************

        /// <summary>
        /// A mouse down event occurred in the Active Line data grid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ActiveLine_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ActiveLine.PreviewMouseDown(sender, e);
        }

        /// <summary>
        /// A mouse up event occurred in the Active Line data grid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ActiveLine_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            ActiveLine.PreviewMouseUp(sender, e);
        }

        /// <summary>
        /// A mouse double click event occurred in the Active Line data grid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ActiveLine_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ActiveLine.MouseDoubleClick(sender, e);
        }

        //**************************************************************
        //
        //  ENGINE GAME VIEW mouse events 
        // 
        //**************************************************************

        /// <summary>
        /// Disables handling of the mouse up event in the Game data grid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EngineGame_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Selection is disabled in DataGrid RowStyle but we need the scrolling event.
        /// So, we need to let this event through.  However, the whole program hangs
        /// if the header is clicked (??) so detect that and mark as handled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EngineGame_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            // iteratively traverse the visual tree
            while ((dep != null) && !(dep is DataGridCell) && !(dep is DataGridColumnHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
            {
                return;
            }

            if (dep is DataGridColumnHeader)
            {
                e.Handled = true;
            }
        }


        //**************************************************************
        //
        //  INTRO VIEW mouse events 
        // 
        //**************************************************************

        /// <summary>
        /// Mouse click received in the view.
        /// Configure the context menu according to what was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbIntroView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_introView != null)
            {
                if (e.ChangedButton == MouseButton.Right)
                {
                    _introView.EnableMenuItems(false, false, false, null);
                }

                IntroView.RestoreSelectionOpacity();
                UiImgMainChessboard.Source = Configuration.StudyBoardSet.MainBoard;
            }
        }

        //**************************************************************
        //
        //  FUNCTION TOGGLE mouse events 
        // 
        //**************************************************************

        /// <summary>
        /// Hides the Evaluation Chart.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EvaluationChartToggleOn(object sender, MouseButtonEventArgs e)
        {
            Configuration.ShowEvaluationChart = false;
            UiImgChartOff.Visibility = Visibility.Visible;
            UiImgChartOn.Visibility = Visibility.Collapsed;

            MultiTextBoxManager.ShowEvaluationChart(false);

            if (e != null)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Shows the Evaluation Chart.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EvaluationChartToggleOff(object sender, MouseButtonEventArgs e)
        {
            Configuration.ShowEvaluationChart = true;
            UiImgChartOff.Visibility = Visibility.Collapsed;
            UiImgChartOn.Visibility = Visibility.Visible;

            UiEvalChart.ReportIfCanShow();
            MultiTextBoxManager.ShowEvaluationChart(true);

            if (e != null)
            {
                e.Handled = true;
            }
        }


        /// <summary>
        /// Turns off the Explorers Toggle and stops Web queries.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ExplorersToggleOn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TurnExplorersOff(true);

            if (e != null)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Turns on the Explorers Toggle and allows Web queries.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ExplorersToggleOff_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (AppState.CurrentLearningMode != LearningMode.Mode.ENGINE_GAME && AppState.CurrentLearningMode != LearningMode.Mode.TRAINING)
            {
                TurnExplorersOn();
            }

            if (e != null)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// "Show Explorer" button clicked in lieu of the switching the toggle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnShowExplorer_Click(object sender, RoutedEventArgs e)
        {
            ExplorersToggleOff_PreviewMouseDown(sender, null);
        }

        /// <summary>
        /// Activates and shows the Explorers
        /// </summary>
        private void TurnExplorersOn()
        {
            Configuration.ShowExplorers = true;

            UiImgExplorersOff.Visibility = Visibility.Collapsed;
            UiImgExplorersOn.Visibility = Visibility.Visible;
            WebAccessManager.IsEnabledExplorerQueries = true;

            if (ActiveVariationTree != null && ActiveVariationTree.SelectedNode != null || AppState.ActiveTab == TabViewType.INTRO)
            {
                _openingStatsView.SetOpeningName();
                WebAccessManager.ExplorerRequest(AppState.ActiveTreeId, ActiveVariationTree.SelectedNode);
            }

            AppState.SetupGuiForCurrentStates();
        }

        /// <summary>
        /// Deactivates and hides explorers.
        /// </summary>
        public void TurnExplorersOff(bool userAction)
        {
            if (userAction)
            {
                // user clicked the toggle so update configuration
                Configuration.ShowExplorers = false;
            }

            UiImgExplorersOff.Visibility = Visibility.Visible;
            UiImgExplorersOn.Visibility = Visibility.Collapsed;
            WebAccessManager.IsEnabledExplorerQueries = false;

            AppState.ShowExplorers(false, ActiveTreeView != null && ActiveTreeView.HasEntities);
        }

        /// <summary>
        /// Updates the states of explorers based on the current configuration
        /// as opposed to being temporarily off e.g. due to being in the Training Mode.
        /// </summary>
        public void UpdateExplorersToggleState()
        {
            if (AppState.AreExplorersOn)
            {
                UiImgExplorersOff.Visibility = Visibility.Collapsed;
                UiImgExplorersOn.Visibility = Visibility.Visible;
                WebAccessManager.IsEnabledExplorerQueries = true;
            }
            else
            {
                UiImgExplorersOff.Visibility = Visibility.Visible;
                UiImgExplorersOn.Visibility = Visibility.Collapsed;
                WebAccessManager.IsEnabledExplorerQueries = false;
            }
        }

        /// <summary>
        /// Handles the Evaluation toggle being clicked while in the ON mode.
        /// Unless this is a game, any evaluation in progress will be stopped.
        /// While in a game it will have an effect of forcing the engine's move.
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EngineToggleOn_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            UiImgEngineOff.Visibility = Visibility.Visible;
            UiImgEngineOn.Visibility = Visibility.Collapsed;
            UiImgEngineOnGray.Visibility = Visibility.Collapsed;

            if (AppState.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
            {
                ForceEngineMove();
                TrainingSession.IsContinuousEvaluation = false;
                if (EvaluationManager.CurrentMode != EvaluationManager.Mode.ENGINE_GAME)
                {
                    EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE, false);
                }
            }
            else
            {
                StopEvaluation(false, false);
                TrainingSession.IsContinuousEvaluation = false;
            }
            AppState.SetupGuiForCurrentStates();

            if (e != null)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the Evaluation toggle being clicked while in the OFF mode.
        /// If in MANUAL REVIEW mode, sets the current evaluation mode
        /// to CONTINUOUS.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EngineToggleOff_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!EngineMessageProcessor.IsEngineAvailable)
            {
                BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.EngineNotAvailable, CommentBox.HintType.ERROR);
                if (e != null)
                {
                    e.Handled = true;
                }
                return;
            }

            // no eval in auto-replay
            if (ActiveLineReplay.IsReplayActive
                || AppState.ActiveTab == TabViewType.INTRO
                || AppState.ActiveTab == TabViewType.BOOKMARKS
                || AppState.ActiveTab == TabViewType.CHAPTERS)
            {
                if (e != null)
                {
                    e.Handled = true;
                }
                return;
            }

            if (AppState.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.CONTINUOUS);
                UiImgEngineOff.Visibility = Visibility.Collapsed;
                if (AppState.EngineEvaluationsUpdateble)
                {
                    UiImgEngineOn.Visibility = Visibility.Visible;
                    UiImgEngineOn.Visibility = Visibility.Collapsed;
                }
                else
                {
                    UiImgEngineOn.Visibility = Visibility.Collapsed;
                    UiImgEngineOn.Visibility = Visibility.Visible;
                }
                Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                EvaluateActiveLineSelectedPosition();
            }
            else if (AppState.CurrentLearningMode == LearningMode.Mode.TRAINING
                || AppState.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME && TrainingSession.IsTrainingInProgress)
            {
                TrainingSession.IsContinuousEvaluation = true;
                UiTrainingView.RequestMoveEvaluation(ActiveVariationTreeId, true);
            }

            if (e != null)
            {
                e.Handled = true;
            }
        }

        //**************************************************************
        //
        //  TAB VARIATION TREE VIEWS 
        // 
        //**************************************************************

        /// <summary>
        /// The Previous Chapter arrow was clicked.
        /// Go to the target chapter, keep the the current tab
        /// active and select the active article of the tab's type.
        /// If the Intro is the tab type and there is no Intro in the
        /// target chapter, select Study.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgChapterLeftArrow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (WorkbookManager.SessionWorkbook.ActiveChapterIndex > 0)
                {
                    if (!MouseClickMonitor.IsSeriesInProgress(MouseClickAction.PREVIOUS_CHAPTER) && !Keyboard.IsKeyDown(Key.LeftShift))
                    {
                        int targetChapterIndex = WorkbookManager.SessionWorkbook.ActiveChapterIndex - 1;
                        WorkbookLocationNavigator.GotoArticle(targetChapterIndex, AppState.ActiveTab);
                    }
                    MouseClickMonitor.RegisterClick(MouseClickAction.PREVIOUS_CHAPTER);
                }
            }
            catch
            {
                AppLog.Message("Exception in UiImgChapterLeftArrow_PreviewMouseLeftButtonDown()");
            }
        }

        /// <summary>
        /// The Next Chapter arrow was clicked.
        /// Go to the target chapter, keep the the current tab
        /// active and select the active article of the tab's type.
        /// If the Intro is the tab type and there is no Intro in the
        /// target chapter, select Study.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgChapterRightArrow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (WorkbookManager.SessionWorkbook.ActiveChapterIndex >= 0
                    && WorkbookManager.SessionWorkbook.ActiveChapterIndex < WorkbookManager.SessionWorkbook.Chapters.Count - 1)
                {
                    if (!MouseClickMonitor.IsSeriesInProgress(MouseClickAction.NEXT_CHAPTER) && !Keyboard.IsKeyDown(Key.LeftShift))
                    {
                        int targetChapterIndex = WorkbookManager.SessionWorkbook.ActiveChapterIndex + 1;
                        WorkbookLocationNavigator.GotoArticle(targetChapterIndex, AppState.ActiveTab);
                    }
                    MouseClickMonitor.RegisterClick(MouseClickAction.NEXT_CHAPTER);
                }
            }
            catch
            {
                AppLog.Message("Exception in UiImgChapterRightArrow_PreviewMouseLeftButtonDown()");
            }
        }

        /// <summary>
        /// In the Model Games view, the user requested the previous game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiPreviousModelGame_ButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex > 0)
                {
                    if (!MouseClickMonitor.IsSeriesInProgress(MouseClickAction.PREVIOUS_GAME) && !Keyboard.IsKeyDown(Key.LeftShift))
                    {
                        SelectModelGame(WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex - 1, true);
                    }
                    MouseClickMonitor.RegisterClick(MouseClickAction.PREVIOUS_GAME);
                }
            }
            catch
            {
                AppLog.Message("Exception in UiPreviousModelGame_ButtonDown()");
            }
        }

        /// <summary>
        /// In the Model Games view, the user requested the next game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiNextModelGame_ButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex < WorkbookManager.SessionWorkbook.ActiveChapter.GetModelGameCount() - 1)
                {
                    if (!MouseClickMonitor.IsSeriesInProgress(MouseClickAction.NEXT_GAME) && !Keyboard.IsKeyDown(Key.LeftShift))
                    {
                        SelectModelGame(WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex + 1, true);
                    }
                    MouseClickMonitor.RegisterClick(MouseClickAction.NEXT_GAME);
                }
            }
            catch
            {
                AppLog.Message("Exception in UiNextModelGame_ButtonDown()");
            }
        }

        /// <summary>
        /// In the Exercises view, the user requested the previous exercise
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgExerciseLeftArrow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (WorkbookManager.SessionWorkbook.ActiveChapter.ActiveExerciseIndex > 0)
                {
                    if (!MouseClickMonitor.IsSeriesInProgress(MouseClickAction.PREVIOUS_EXERCISE) && !Keyboard.IsKeyDown(Key.LeftShift))
                    {
                        SelectExercise(WorkbookManager.SessionWorkbook.ActiveChapter.ActiveExerciseIndex - 1, true);
                    }
                    MouseClickMonitor.RegisterClick(MouseClickAction.PREVIOUS_EXERCISE);
                }
            }
            catch
            {
                AppLog.Message("Exception in UiImgExerciseLeftArrow_PreviewMouseLeftButtonDown()");
            }
        }

        /// <summary>
        /// In the Exercises view, the user requested the next exercise
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgExerciseRightArrow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (WorkbookManager.SessionWorkbook.ActiveChapter.ActiveExerciseIndex < WorkbookManager.SessionWorkbook.ActiveChapter.GetExerciseCount() - 1)
                {
                    if (!MouseClickMonitor.IsSeriesInProgress(MouseClickAction.NEXT_EXERCISE) && !Keyboard.IsKeyDown(Key.LeftShift))
                    {
                        SelectExercise(WorkbookManager.SessionWorkbook.ActiveChapter.ActiveExerciseIndex + 1, true);
                    }
                    MouseClickMonitor.RegisterClick(MouseClickAction.NEXT_EXERCISE);
                }
            }
            catch
            {
                AppLog.Message("Exception in UiImgExerciseRightArrow_PreviewMouseLeftButtonDown()");
            }
        }


        //**************************************************************
        //
        //  TAB VIEW FOCUS 
        // 
        //**************************************************************

        /// <summary>
        /// The Chapter View got focus.
        /// Make sure it is sized properly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiTabChapters_GotFocus(object sender, RoutedEventArgs e)
        {
            if (AppState.ActiveTab == TabViewType.CHAPTERS)
            {
                return;
            }

            StopReplayIfActive();
            EngineMessageProcessor.StopEngineEvaluation();
            ResizeTabControl(UiTabCtrlManualReview, TabControlSizeMode.HIDE_ACTIVE_LINE);

            WorkbookManager.ActiveTab = TabViewType.CHAPTERS;
            AppState.ShowExplorers(false, false);

            WorkbookLocationNavigator.SaveNewLocation(TabViewType.CHAPTERS);

            // we may need to show/hide Intro headers if something has changed
            if (_chaptersView == null || AppState.Workbook == null)
            {
                _chaptersView = new ChaptersView(UiRtbChaptersView.Document, this);
                _chaptersView.IsDirty = true;
            }
            else
            {
                if (_chaptersView.IsDirty)
                {
                    _chaptersView.BuildFlowDocumentForChaptersView();
                }
                _chaptersView.HighlightActiveChapter();
                _chaptersView.BringActiveChapterIntoView();
                _chaptersView.UpdateIntroHeaders();
            }

            AppState.ConfigureMenusForManualReview();
            BoardCommentBox.ShowTabHints();
            try
            {
                if (KeepFocusOnGame() || WorkbookManager.SessionWorkbook == null)
                {
                    return;
                }

                UiImgMainChessboard.Source = Configuration.StudyBoardSet.MainBoard;
                if (WorkbookManager.SessionWorkbook != null)
                {
                    SetupGuiForChapters();
                    DisplayPosition(PositionUtils.SetupStartingPosition());
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// The Intro view got focus.
        /// Check if it is built for the current context,
        /// and if not create/re-build it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiTabIntro_GotFocus(object sender, RoutedEventArgs e)
        {
            StopReplayIfActive();
            try
            {
                if (WorkbookManager.SessionWorkbook == null && _introView != null)
                {
                    _introView.Clear();
                }

                if (AppState.ActiveTab == TabViewType.INTRO)
                {
                    return;
                }

                WorkbookManager.ActiveTab = TabViewType.INTRO;
                if (_introView == null || _introView.Document == null || _introView.Document.Blocks.Count == 0)
                {
                    SetupGuiForIntro(true);
                }
                else
                {
                    RebuildIntroView();
                }

                if (WorkbookManager.SessionWorkbook != null)
                {
                    WorkbookLocationNavigator.SaveNewLocation(WorkbookManager.SessionWorkbook.ActiveChapter, GameData.ContentType.INTRO, -1);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Rebuilds and shows the Intro view.
        /// </summary>
        private void RebuildIntroView()
        {
            AppState.ConfigureMenusForManualReview();
            WorkbookManager.SessionWorkbook.ActiveChapter.SetActiveVariationTree(GameData.ContentType.INTRO);
            AppState.ShowExplorers(AppState.AreExplorersOn, true);
            UiImgMainChessboard.Source = Configuration.StudyBoardSet.MainBoard;

            BoardCommentBox.ShowTabHints();
            // if _introView is not null and ParentChapter is the same, leave things as they are,
            // otherwise build the view.
            if (_introView == null || _introView.ParentChapter != WorkbookManager.SessionWorkbook.ActiveChapter)
            {
                UiRtbIntroView.IsDocumentEnabled = true;
                UiRtbIntroView.AllowDrop = false;
                _introView = new IntroView(UiRtbIntroView.Document, WorkbookManager.SessionWorkbook.ActiveChapter);
            }
            DisplayPosition(_introView.SelectedNode);

            AppState.ConfigureMainBoardContextMenu();
            ResizeTabControl(UiTabCtrlManualReview, TabControlSizeMode.HIDE_ACTIVE_LINE);

            PreviousNextViewBars.BuildPreviousNextBar(GameData.ContentType.INTRO);
        }

        /// <summary>
        /// We need to save the content of the view on LostFocus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabIntro_LostFocus(object sender, RoutedEventArgs e)
        {
            //TODO: this will be called too often (e.g. when loading), find some performance optimization
            if (_introView != null)
            {
                _introView.SaveXAMLContent(false);
            }
        }

        /// <summary>
        /// The Study Tree view got focus.
        /// Select the last selected line and move and display position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiTabStudyTree_GotFocus(object sender, RoutedEventArgs e)
        {
            if (AppState.ActiveTab == TabViewType.STUDY)
            {
                return;
            }


            WorkbookManager.ActiveTab = TabViewType.STUDY;
            if (_studyTreeView == null || _studyTreeView.Document == null || _studyTreeView.Document.Blocks.Count == 0)
            {
                SetupGuiForActiveStudyTree(true);
            }
            UiRtbStudyTreeView.Focus();

            StopReplayIfActive();

            UiImgEngineOn.IsEnabled = true;
            UiImgEngineOnGray.IsEnabled = true;
            UiImgEngineOff.IsEnabled = true;

            AppState.ShowExplorers(AppState.AreExplorersOn, true);

            BoardCommentBox.ShowTabHints();
            try
            {
                SetStudyStateOnFocus();
                AppState.ConfigureMainBoardContextMenu();
                if (WorkbookManager.SessionWorkbook != null)
                {
                    WorkbookLocationNavigator.SaveNewLocation(WorkbookManager.SessionWorkbook.ActiveChapter, GameData.ContentType.STUDY_TREE, -1);
                }
            }
            catch
            {
            }

        }

        /// <summary>
        /// Set the board and the active line as if thios was a study view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiTabBookmarks_GotFocus(object sender, RoutedEventArgs e)
        {
            StopReplayIfActive();

            if (AppState.ActiveTab == TabViewType.BOOKMARKS)
            {
                return;
            }

            DisplayPosition(PositionUtils.SetupStartingPosition());
            WorkbookManager.ActiveTab = TabViewType.BOOKMARKS;
            AppState.ConfigureMenusForManualReview();
            AppState.ShowExplorers(false, false);

            BoardCommentBox.ShowTabHints();
            try
            {
                if (KeepFocusOnGame())
                {
                    return;
                }
                UiImgMainChessboard.Source = Configuration.StudyBoardSet.MainBoard;
                ResizeTabControl(UiTabCtrlManualReview, TabControlSizeMode.HIDE_ACTIVE_LINE);
                BookmarkManager.BuildBookmarkList(true);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Moves selection in the ChaptersView up or down.
        /// </summary>
        /// <param name="upOrDown"></param>
        public void ChaptersViewSelectionMove(bool upOrDown)
        {
            try
            {
                _chaptersView.MoveSelection(upOrDown);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Acts on the current selection i.e. opens the selected
        /// game, exercise or study tree.
        /// 
        /// </summary>
        public void ChaptersViewActivateSelection()
        {
            try
            {
                _chaptersView.ActOnSelection();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Sets the board orientation and active line according
        /// the last StudyTree state.
        /// </summary>
        private void SetStudyStateOnFocus()
        {
            try
            {
                if (AppState.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
                {
                    UiImgMainChessboard.Source = ChessBoards.ChessBoardGreen;
                }
                else
                {
                    UiImgMainChessboard.Source = Configuration.StudyBoardSet.MainBoard;
                    ResizeTabControl(UiTabCtrlManualReview, TabControlSizeMode.SHOW_ACTIVE_LINE);
                    if (WorkbookManager.SessionWorkbook != null)
                    {
                        Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                        if (chapter != null)
                        {
                            chapter.SetActiveVariationTree(GameData.ContentType.STUDY_TREE);
                            // under some circumstamces we may be showing the tree from the wrong chapter so check...
                            if (chapter.ActiveVariationTree != _studyTreeView.ShownVariationTree)
                            {
                                // TODO we should not have to do all this here: can we prevent the special circumstances where
                                // this is not in ActiveChapter when we are calling ApplyStates() ??
                                _studyTreeView.BuildFlowDocumentForVariationTree();
                                string lineId = "1";
                                int nodeId = 0;
                                ObservableCollection<TreeNode> lineToSelect = _studyTreeView.ShownVariationTree.SelectLine(lineId);
                                SetActiveLine(lineToSelect, nodeId);
                                _studyTreeView.SelectLineAndMove(lineId, nodeId);
                            }
                            else
                            {
                                RestoreSelectedLineAndMoveInActiveView();
                            }
                        }
                        MainChessBoard.FlipBoard(EffectiveBoardOrientation(WorkbookManager.ItemType.STUDY));

                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// The Model Games view got focus.
        /// Select the last selected line and move and display position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiTabModelGames_GotFocus(object sender, RoutedEventArgs e)
        {
            if (AppState.ActiveTab == TabViewType.MODEL_GAME)
            {
                return;
            }

            StopReplayIfActive();

            WorkbookManager.ActiveTab = TabViewType.MODEL_GAME;
            AppState.ConfigureMenusForManualReview();
            RefreshGamesView(out Chapter chapter, out int articleIndex);
            WorkbookLocationNavigator.SaveNewLocation(chapter, GameData.ContentType.MODEL_GAME, articleIndex);
        }

        /// <summary>
        /// Rebuilds the Game view
        /// </summary>
        private void RefreshGamesView(out Chapter chapter, out int articleIndex)
        {
            chapter = null;
            articleIndex = -1;

            UiImgEngineOn.IsEnabled = true;
            UiImgEngineOnGray.IsEnabled = true;
            UiImgEngineOff.IsEnabled = true;

            WorkbookManager.ActiveTab = TabViewType.MODEL_GAME;
            if (AppState.ActiveChapterGamesCount > 0)
            {
                AppState.ShowExplorers(AppState.AreExplorersOn, true);
            }
            else
            {
                AppState.ShowExplorers(false, false);
            }

            BoardCommentBox.ShowTabHints();
            try
            {
                if (KeepFocusOnGame())
                {
                    return;
                }

                UiImgMainChessboard.Source = Configuration.GameBoardSet.MainBoard;

                if (WorkbookManager.SessionWorkbook != null)
                {
                    MainChessBoard.FlipBoard(EffectiveBoardOrientation(WorkbookManager.ItemType.MODEL_GAME));

                    chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                    if (chapter != null && chapter.GetModelGameCount() > 0)
                    {
                        if (chapter.ActiveModelGameIndex == -1)
                        {
                            chapter.ActiveModelGameIndex = 0;
                        }

                        SelectModelGame(chapter.ActiveModelGameIndex, false);
                        articleIndex = chapter.ActiveModelGameIndex;
                    }
                    else
                    {
                        MainChessBoard.SetStartingPosition();
                        ClearTreeView(_modelGameTreeView, GameData.ContentType.MODEL_GAME);
                        // SelectModelGame() does this in the branch above
                        WorkbookManager.SessionWorkbook.ActiveChapter.SetActiveVariationTree(GameData.ContentType.NONE);
                    }

                    AppState.ConfigureMainBoardContextMenu();
                    if (chapter != null && (chapter.ActiveModelGameIndex < 0 || chapter.ActiveModelGameIndex >= chapter.GetModelGameCount()))
                    {
                        chapter.CorrectActiveModelGameIndex();
                        if (chapter.ActiveModelGameIndex < 0)
                        {
                            ActiveLine.Clear();
                        }
                    }
                    ResizeTabControl(UiTabCtrlManualReview, TabControlSizeMode.SHOW_ACTIVE_LINE);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Persists the board's flipped state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabModelGames_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
            }
            catch
            {
            }
        }

        /// <summary>
        /// The Exercises view got focus.
        /// Select the last selected line and move and display position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiTabExercises_GotFocus(object sender, RoutedEventArgs e)
        {
            if (AppState.ActiveTab == TabViewType.EXERCISE)
            {
                return;
            }

            StopReplayIfActive();

            WorkbookManager.ActiveTab = TabViewType.EXERCISE;
            AppState.ConfigureMenusForManualReview();
            RefreshExercisesView(out Chapter chapter, out int articleIndex);
            WorkbookLocationNavigator.SaveNewLocation(chapter, GameData.ContentType.EXERCISE, articleIndex);
        }

        /// <summary>
        /// Rebuilds the Exercise view
        /// </summary>
        private void RefreshExercisesView(out Chapter chapter, out int articleIndex)
        {
            chapter = null;
            articleIndex = -1;

            UiImgEngineOn.IsEnabled = true;
            UiImgEngineOnGray.IsEnabled = true;
            UiImgEngineOff.IsEnabled = true;

            WorkbookManager.ActiveTab = TabViewType.EXERCISE;
            if (AppState.ActiveChapterExerciseCount > 0)
            {
                AppState.ShowExplorers(AppState.AreExplorersOn, true);
            }
            else
            {
                AppState.ShowExplorers(false, false);
            }

            BoardCommentBox.ShowTabHints();
            try
            {
                if (KeepFocusOnGame())
                {
                    return;
                }

                UiImgMainChessboard.Source = Configuration.ExerciseBoardSet.MainBoard;
                if (WorkbookManager.SessionWorkbook != null)
                {
                    MainChessBoard.FlipBoard(EffectiveBoardOrientation(WorkbookManager.ItemType.EXERCISE));

                    chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                    if (chapter != null && chapter.GetExerciseCount() > 0)
                    {
                        if (chapter.ActiveExerciseIndex == -1)
                        {
                            chapter.ActiveExerciseIndex = 0;
                        }

                        SelectExercise(chapter.ActiveExerciseIndex, false);
                        articleIndex = chapter.ActiveExerciseIndex;
                    }
                    else
                    {
                        MainChessBoard.SetStartingPosition();
                        ClearTreeView(_exerciseTreeView, GameData.ContentType.EXERCISE);
                        WorkbookManager.SessionWorkbook.ActiveChapter.SetActiveVariationTree(GameData.ContentType.NONE);
                        ActiveLine.Clear();
                    }

                    AppState.ConfigureMainBoardContextMenu();

                    if (ActiveVariationTree != null && ActiveVariationTree.ShowTreeLines)
                    {
                        ResizeTabControl(UiTabCtrlManualReview, TabControlSizeMode.SHOW_ACTIVE_LINE);
                    }
                    else
                    {
                        ResizeTabControl(UiTabCtrlManualReview, TabControlSizeMode.HIDE_ACTIVE_LINE);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Persists the board's flipped state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabExercises_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
            }
            catch
            {
            }
        }

        /// <summary>
        /// Invoked when user clicks a non-active tab while an engine game is in progress.
        /// Checks if there is a game in progress, displays a "flash announcement" and 
        /// returns focus to the view with the game.
        /// </summary>
        /// <returns></returns>
        private bool KeepFocusOnGame()
        {
            if (AppState.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
            {
                BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.InfoExitGameBeforeTabSwitch, CommentBox.HintType.ERROR);
                if (TrainingSession.IsTrainingInProgress)
                {
                    UiTabTrainingProgress.Focus();
                }
                else
                {
                    UiTabStudyTree.Focus();
                }
                return true;
            }
            else
            {
                return false;
            }
        }


        //**************************************************************
        //
        //  Previous/Next Bar  
        // 
        //**************************************************************


        /// <summary>
        /// Double click on the Chapter title in INTRO
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiIntroLblChapterTitle_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AppState.Workbook != null)
            {
                RenameChapter(WorkbookManager.SessionWorkbook.ActiveChapter);
                PreviousNextViewBars.BuildPreviousNextBar(GameData.ContentType.INTRO);
            }
        }

        /// <summary>
        /// Double click on the Chapter title in STUDY
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiStudyLblChapterTitle_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AppState.Workbook != null)
            {
                RenameChapter(WorkbookManager.SessionWorkbook.ActiveChapter);
                PreviousNextViewBars.BuildPreviousNextBar(GameData.ContentType.STUDY_TREE);
            }
        }

        /// <summary>
        /// Double click on the Chapter title in GAMES
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiGamesLblChapterTitle_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AppState.Workbook != null)
            {
                RenameChapter(WorkbookManager.SessionWorkbook.ActiveChapter);
                PreviousNextViewBars.BuildPreviousNextBar(GameData.ContentType.MODEL_GAME);
            }
        }

        /// <summary>
        /// Double click on the Chapter title in EXERCISES
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiExerciseLblChapterTitle_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AppState.Workbook != null)
            {
                RenameChapter(WorkbookManager.SessionWorkbook.ActiveChapter);
                PreviousNextViewBars.BuildPreviousNextBar(GameData.ContentType.EXERCISE);
            }
        }

    }
}

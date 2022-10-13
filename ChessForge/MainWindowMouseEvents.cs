using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Mouse event handlers for the Main Window class.
    /// </summary>
    public partial class MainWindow : Window
    {

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
            Point clickedPoint = e.GetPosition(UiImgMainChessboard);
            SquareCoords sq = MainChessBoardUtils.ClickedSquare(clickedPoint);
            if (sq == null)
            {
                return;
            }

            if (e.ChangedButton == MouseButton.Right)
            {
                _lastRightClickedPoint = clickedPoint;
                // if no special key was pressed we consider this tentative
                // since we need to resolve between shape building and context menu
                bool isDrawTentative = !GuiUtilities.IsSpecialKeyPressed();
                StartShapeDraw(sq, isDrawTentative);
            }
            else
            {
                _lastRightClickedPoint = null;
                if (EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE)
                {
                    if (EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE)
                    {
                        BoardCommentBox.ShowFlashAnnouncement("Line evaluation in progress!");
                        return;
                    }
                    else if (EvaluationManager.CurrentMode == EvaluationManager.Mode.ENGINE_GAME)
                    {
                        BoardCommentBox.ShowFlashAnnouncement("The engine is thinking!");
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
                            BoardShapesManager.Reset(true);
                        }

                        if (CanMovePiece(sqNorm))
                        {
                            DraggedPiece.isDragInProgress = true;
                            DraggedPiece.Square = sq;

                            DraggedPiece.ImageControl = MainChessBoardUtils.GetImageFromPoint(clickedPoint);
                            Point ptLeftTop = MainChessBoardUtils.GetSquareTopLeftPoint(sq);
                            DraggedPiece.ptDraggedPieceOrigin = ptLeftTop;

                            // for the remainder, we need absolute point
                            clickedPoint.X += UiImgMainChessboard.Margin.Left;
                            clickedPoint.Y += UiImgMainChessboard.Margin.Top;
                            DraggedPiece.ptStartDragLocation = clickedPoint;


                            Point ptCenter = MainChessBoardUtils.GetSquareCenterPoint(sq);

                            Canvas.SetLeft(DraggedPiece.ImageControl, ptLeftTop.X + (clickedPoint.X - ptCenter.X));
                            Canvas.SetTop(DraggedPiece.ImageControl, ptLeftTop.Y + (clickedPoint.Y - ptCenter.Y));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Depending on the Application and/or Training mode,
        /// this may have been the user completing a move.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            Point clickedPoint = e.GetPosition(UiImgMainChessboard);
            SquareCoords targetSquare = MainChessBoardUtils.ClickedSquare(clickedPoint);

            if (e.ChangedButton == MouseButton.Right)
            {
                HandleMouseUpRightButton(targetSquare, e);
            }
            else
            {
                if (DraggedPiece.isDragInProgress)
                {
                    DraggedPiece.isDragInProgress = false;
                    if (targetSquare == null)
                    {
                        // just put the piece back
                        Canvas.SetLeft(DraggedPiece.ImageControl, DraggedPiece.ptDraggedPieceOrigin.X);
                        Canvas.SetTop(DraggedPiece.ImageControl, DraggedPiece.ptDraggedPieceOrigin.Y);
                    }
                    else
                    {
                        // double check that we are legitimately making a move
                        if (LearningMode.CurrentMode == LearningMode.Mode.ENGINE_GAME && EngineGame.CurrentState == EngineGame.GameState.USER_THINKING
                            || LearningMode.CurrentMode == LearningMode.Mode.TRAINING && TrainingSession.CurrentState == TrainingSession.State.AWAITING_USER_TRAINING_MOVE
                            || LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW)
                        {
                            if (LearningMode.CurrentMode == LearningMode.Mode.ENGINE_GAME && EvaluationManager.CurrentMode != EvaluationManager.Mode.IDLE)
                            {
                                BoardCommentBox.ShowFlashAnnouncement("Stop evaluation before making your move.");
                                ReturnDraggedPiece(false);
                            }
                            else
                            {
                                // we made a move and we are not in a game,
                                // so we can switch off all evaluations except if we are in the CONTINUOUS
                                // mode in MANUAL REVIEW
                                if (EvaluationManager.CurrentMode != EvaluationManager.Mode.IDLE
                                    && (LearningMode.CurrentMode != LearningMode.Mode.MANUAL_REVIEW || EvaluationManager.CurrentMode != EvaluationManager.Mode.CONTINUOUS))
                                {
                                    EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);
                                }
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
            Point mousePoint = e.GetPosition(UiImgMainChessboard);
            SquareCoords sq = MainChessBoardUtils.ClickedSquare(mousePoint);

            // if right button is pressed we may be drawing an arrow
            if (e.RightButton == MouseButtonState.Pressed)
            {
                HandleMouseMoveRightButton(sq, mousePoint);
            }
            else
            {
                _lastRightClickedPoint = null;
                if (BoardShapesManager.IsShapeBuildInProgress)
                {
                    BoardShapesManager.CancelShapeDraw(true);
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
            if (BoardShapesManager.IsShapeBuildInProgress)
            {
                bool proceed = true;

                // check if we are tentative
                if (BoardShapesManager.IsShapeBuildTentative)
                {
                    if (_lastRightClickedPoint == null)
                    {
                        BoardShapesManager.CancelShapeDraw(true);
                        proceed = false;
                    }
                    else
                    {
                        // check if we should proceed or let context menu show
                        if (Math.Abs(GuiUtilities.CalculateDistance(_lastRightClickedPoint.Value, ptCurrent)) > 5)
                        {
                            BoardShapesManager.IsShapeBuildTentative = false;
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
                    BoardShapesManager.UpdateShapeDraw(sq);
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
            if (BoardShapesManager.IsShapeBuildInProgress && !BoardShapesManager.IsShapeBuildTentative)
            {
                BoardShapesManager.FinalizeShape(targetSquare, true, true);
                _lastRightClickedPoint = null;

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
            if (e.Delta > 0)
            {
                ActiveLine.HandleKeyDown(Key.Left);
            }
            else if (e.Delta < 0)
            {
                ActiveLine.HandleKeyDown(Key.Right);
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
        private void WorkbookView_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_studyTreeView != null)
            {
                _studyTreeView.LastClickedNodeId = -1;
                _studyTreeView.EnableWorkbookMenus(UiCmnWorkbookRightClick, false);
            }
        }

        /// <summary>
        /// Ensure that Workbook Tree's ListView allows
        /// mouse wheel scrolling.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WorkbookView_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
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
            ShowFloatingChessboard(false);
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
                BookmarkManager.ChessboardClickedEvent(canvas.Name, _cmBookmarks, e);
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
            BookmarkManager.EnableBookmarkMenus(_cmBookmarks, false);
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
        /// A mouse down events occurred in the Active Line data grid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ActiveLine_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ActiveLine.PreviewMouseDown(sender, e);
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
        /// Disables handling of the mouse down event in the Game data grid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EngineGame_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }


        //**************************************************************
        //
        //  EVALUATION TOGGLE mouse events 
        // 
        //**************************************************************

        /// <summary>
        /// Handles the Evaluation toggle being clicked while in the ON mode.
        /// Any evaluation in progress will be stopped.
        /// to CONTINUOUS.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EngineToggleOn_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            UiImgEngineOff.Visibility = Visibility.Visible;
            UiImgEngineOn.Visibility = Visibility.Collapsed;

            StopEvaluation();

            e.Handled = true;
        }

        /// <summary>
        /// Handles the Evaluation toggle being clicked while in the OFF mode.
        /// If in MANUAL REVIEW mode, sets the current evaluation mode
        /// to CONTINUOUS.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EngineToggleOff_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (AppStateManager.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.CONTINUOUS);
                UiImgEngineOff.Visibility = Visibility.Collapsed;
                UiImgEngineOn.Visibility = Visibility.Visible;
                Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                EvaluateActiveLineSelectedPosition();
            }

            e.Handled = true;
        }


        //**************************************************************
        //
        //  VIEW FOCUS 
        // 
        //**************************************************************

        /// <summary>
        /// The Study Tree view got focus.
        /// Select the last selected line and move and display position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabStudyTree_GotFocus(object sender, RoutedEventArgs e)
        {
            SetStudyStateOnFocus();
        }


        /// <summary>
        /// Persists the board's flipped state when the Study Tree view loses focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabStudyTree_LostFocus(object sender, RoutedEventArgs e)
        {
            WorkbookManager.SessionWorkbook.IsStudyBoardFlipped = MainChessBoard.IsFlipped;
        }

        /// <summary>
        /// Set the board and the active line as if thios was a study view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabBookmarks_GotFocus(object sender, RoutedEventArgs e)
        {
            SetStudyStateOnFocus();
        }

        /// <summary>
        /// Sets the board orientation and active line according
        /// the last StudyTree state.
        /// </summary>
        private void SetStudyStateOnFocus()
        {
            bool? boardFlipped = WorkbookManager.SessionWorkbook.IsStudyBoardFlipped;
            if (boardFlipped != null)
            {
                MainChessBoard.FlipBoard(boardFlipped.Value);
            }

            Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
            if (chapter != null)
            {
                chapter.SetActiveVariationTree(ChessPosition.GameTree.GameMetadata.ContentType.STUDY_TREE);
            }
            RestoreSelectedLineAndMoveInActiveView();
        }

        /// <summary>
        /// Persists the board's flipped state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabBookmarks_LostFocus(object sender, RoutedEventArgs e)
        {
            WorkbookManager.SessionWorkbook.IsStudyBoardFlipped = MainChessBoard.IsFlipped;
        }

        /// <summary>
        /// The Model Games view got focus.
        /// Select the last selected line and move and display position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabModelGames_GotFocus(object sender, RoutedEventArgs e)
        {
            Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
            if (chapter != null)
            {
                chapter.SetActiveVariationTree(ChessPosition.GameTree.GameMetadata.ContentType.MODEL_GAME, chapter.ActiveModelGameIndex);
            }
            RestoreSelectedLineAndMoveInActiveView();
        }

        /// <summary>
        /// The Exercises view got focus.
        /// Select the last selected line and move and display position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabExercises_GotFocus(object sender, RoutedEventArgs e)
        {
            Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
            if (chapter != null)
            {
                chapter.SetActiveVariationTree(ChessPosition.GameTree.GameMetadata.ContentType.EXERCISE, chapter.ActiveExerciseIndex);
            }
            RestoreSelectedLineAndMoveInActiveView();
        }

    }
}

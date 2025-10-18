using ChessPosition;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace ChessForge
{
    public partial class TrainingView
    {

        /// <summary>
        /// Based on the name of the clicked run, performs an action.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventRunClicked(object sender, MouseButtonEventArgs e)
        {
            Run r = (Run)e.Source;
            if (string.IsNullOrEmpty(r.Name))
            {
                e.Handled = true;
                return;
            }

            //on Right Button we invoke the the Context Menu, on Left we don't but will continue with CONTINUOUS evaluation 
            if (e.ChangedButton == MouseButton.Right || e.ChangedButton == MouseButton.Left)
            {
                bool found = false;
                if (r.Name.StartsWith(_run_line_move_))
                {
                    // a move in the main training line was clicked 
                    DetectLastClickedNode(r, _run_line_move_, e);
                    _moveContext = MoveContext.LINE;
                    found = true;
                }
                else if (r.Name.StartsWith(_run_wb_move_))
                {
                    // a workbook move in the comment was clicked 
                    DetectLastClickedNode(r, _run_wb_move_, e);
                    _moveContext = MoveContext.WORKBOOK_COMMENT;
                    found = true;
                }
                else if (r.Name.StartsWith(_run_engine_game_move_))
                {
                    // a move from a game against the engine was clicked,
                    // we take the game back to that move.
                    // If it is an engine move, the user will be required to respond,
                    // otherwise it will be engine's turn.
                    DetectLastClickedNode(r, _run_engine_game_move_, e);
                    _moveContext = MoveContext.GAME;
                    found = true;
                }

                if (found)
                {
                    if (e.ChangedButton == MouseButton.Right)
                    {
                        if (EvaluationManager.CurrentMode != EvaluationManager.Mode.IDLE)
                        {
                            EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);
                        }
                        _mainWin.Timers.Start(AppTimers.TimerId.SHOW_TRAINING_PROGRESS_POPUP_MENU);
                    }
                    else if (e.ChangedButton == MouseButton.Left)
                    {
                        _mainWin.ShowTrainingFloatingBoard(false);
                        if (_lastClickedNode != null)
                        {
                            // flip the visibility for the floating board
                            if (_nodeIdSuppressFloatingBoard == _lastClickedNode.NodeId)
                            {
                                _nodeIdSuppressFloatingBoard = -1;
                            }
                            else
                            {
                                _nodeIdSuppressFloatingBoard = _lastClickedNode.NodeId;
                            }

                            // if not the last move, ask if to restart
                            if (EngineGame.GetLastGameNode() != _lastClickedNode)
                            {
                                RestartFromClickedMove(_moveContext);
                            }
                        }
                    }
                }
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handler for the Next Line click event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventNextLineClicked(object sender, MouseEventArgs e)
        {
            AppState.MainWin.UiMnTrainNextLine_Click(null, null);
            e.Handled = true;
        }

        /// <summary>
        /// Handler for the Previous Line click event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventPreviousLineClicked(object sender, MouseEventArgs e)
        {
            AppState.MainWin.UiMnTrainPreviousLine_Click(null, null);
            e.Handled = true;
        }

        /// <summary>
        /// Handler for the Random Line click event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventRandomLineClicked(object sender, MouseEventArgs e)
        {
            AppState.MainWin.UiMnTrainRandomLine(null, null);
            e.Handled = true;
        }

        /// <summary>
        /// User requested takeback by clicking the takeback para
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventTakebackParaClicked(object sender, MouseEventArgs e)
        {
            RemoveTakebackParagraph();
            RestartFromLastUserWorkbookMove();
        }

        /// <summary>
        /// Handles a mouse move over a Run.
        /// Shows the floating chessboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventRunMoveOver(object sender, MouseEventArgs e)
        {
            if (_blockFloatingBoard)
                return;

            // check if we are over a move run
            Run r = (Run)e.Source;
            if (string.IsNullOrEmpty(r.Name))
            {
                return;
            }

            int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
            if (nodeId >= 0)
            {
                Point pt = e.GetPosition(_mainWin.UiRtbTrainingProgress);
                bool isStemMove = r.Name.StartsWith(_run_stem_move_);
                ShowFloatingBoard(nodeId, pt, isStemMove);
            }
        }
    }
}

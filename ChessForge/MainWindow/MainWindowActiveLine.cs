using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChessPosition;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Functions handling the Active Line view (DataGrid)
    /// </summary>
    public partial class MainWindow : Window
    {
        // column where White's plies are displayed
        private int _dgActiveLineWhitePlyColumn = 1;

        // column where Black's plies are displayed
        private int _dgActiveLineBlackPlyColumn = 3;

        /// <summary>
        /// Returns the index of the Node for the ply
        /// currently selected in the Single Line View. 
        /// </summary>
        /// <returns></returns>
        public int ViewSingleLine_GetSelectedPlyNodeIndex()
        {
            int row;
            int column;

            ViewActiveLine_GetSelectedRowColumn(out row, out column);
            return ViewActiveLine_GetPlyNodeIndexFromRowColumn(row, column);
        }

        /// <summary>
        /// TODO: we do not need both this and the next functions. Rationalize here.
        /// Returns the index of the Node for the ply
        /// at a given row and column. 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private int ViewActiveLine_GetPlyNodeIndexFromRowColumn(int row, int column)
        {
            if (row < 0 || column < 0)
                return -1;

            int moveIndex = (row * 2) + (column == _dgActiveLineWhitePlyColumn ? 0 : 1);
            return moveIndex;
        }

        private int ViewActiveLine_GetNodeIndexFromRowColumn(int row, int column)
        {
            if (row < 0 || column < 0)
                return -1;

            int nodeIndex = (row * 2) + (column == _dgActiveLineWhitePlyColumn ? 0 : 1) + 1;

            return (nodeIndex < ActiveLine.GetPlyCount()) ? nodeIndex : -1;
        }


        /// <summary>
        /// TODO: compare with GetColumnRowFromMouseClick()
        /// in MainWindowUtils to see if we can have one function
        /// for different controls (?)
        /// 
        /// Note: we are only allowing for single selection.
        /// If, somehow, there is more than 1 cell in the selection
        /// we will return the first one.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private bool ViewActiveLine_GetSelectedRowColumn(out int row, out int column)
        {
            row = -1;
            column = -1;

            if (_dgActiveLine.SelectedCells.Count > 0)
            {
                DataGridCellInfo cell = _dgActiveLine.SelectedCells[0];
                column = cell.Column.DisplayIndex;
                DataGridRow dr = (DataGridRow)(_dgActiveLine.ItemContainerGenerator.ContainerFromItem(cell.Item));
                if (dr != null)
                {
                    row = dr.GetIndex();
                }
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Selects the requested ply in relevant controls.
        /// </summary>
        /// <param name="moveNo">Number of the move</param>
        /// <param name="colorToMove">Side on move</param>
        private void ViewActiveLine_SelectPly(int moveNo, PieceColor colorToMove)
        {
            _dgActiveLine.SelectedCells.Clear();
            if (moveNo >= 0)
            {
                DataGridCellInfo cell;
                if (colorToMove == PieceColor.White && moveNo < _dgActiveLine.Items.Count)
                {
                    cell = new DataGridCellInfo(_dgActiveLine.Items[moveNo], _dgActiveLine.Columns[_dgActiveLineWhitePlyColumn]);
                    _dgActiveLine.ScrollIntoView(_dgActiveLine.Items[moveNo]);
                }
                else
                {
                    cell = new DataGridCellInfo(_dgActiveLine.Items[moveNo - 1], _dgActiveLine.Columns[_dgActiveLineBlackPlyColumn]);
                    _dgActiveLine.ScrollIntoView(_dgActiveLine.Items[moveNo - 1]);
                }

                var cellContent = cell.Column.GetCellContent(cell.Item);
                if (cellContent != null)
                {
                    DataGridCell mycell = (DataGridCell)cellContent.Parent;
                    _dgActiveLine.SelectedCells.Add(cell);
                }
            }
        }

        /// <summary>
        /// A double click triggers a replay animation from the currently
        /// selected Node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewActiveLine_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int column = -1;
            int row = -1;

            GuiUtilities.GetDataGridColumnRowFromMouseClick(_dgActiveLine, e, out row, out column);

            // if there is replay happening now, stop it
            if (gameReplay.IsReplayActive)
            {
                StopAnimation();
                _mainboardCommentBox.RestoreTitleMessage();            
            }

            if (row >= 0)
            {
                int moveIndex = (row * 2) + (column == _dgActiveLineWhitePlyColumn ? 0 : 1);
                gameReplay.SetupTreeLineToDisplay(ActiveLine.NodeList, moveIndex + 1);
                _mainboardCommentBox.GameReplayStart();
            }
        }

        /// <summary>
        /// Intercepts the MouseDown event when occuring within the Active Line view.
        /// If the click was on a non-selectable column,
        /// marks the event as handled so that the internal WPF handling is not invoked
        /// (since we don't want the cell to be selected).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewActiveLine_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            int column;
            int row;

            GuiUtilities.GetDataGridColumnRowFromMouseClick(_dgActiveLine, e, out row, out column);

            if (ViewActiveLine_IsSelectableCell(row, column))
            {
                int moveIndex = (row * 2) + (column == _dgActiveLineWhitePlyColumn ? 0 : 1);

                if (moveIndex + 1 < ActiveLine.GetPlyCount())
                {
                    TreeNode nd = ActiveLine.GetNodeAtIndex(moveIndex + 1);

                    if (gameReplay.IsReplayActive)
                    {
                        // request that the replay be stopped and the clicked
                        // position shown, unless this mouse down
                        // was part of a double click (in which case the double click
                        // handler will override this).
                        gameReplay.ShowPositionAndStop(nd);
                        _mainboardCommentBox.RestoreTitleMessage();

                        //StopAnimation();
                        //gameReplay.Stop();
                    }

                    MainChessBoard.DisplayPosition(nd.Position);
                    _workbookView.SelectLineAndMove(null, nd.NodeId);
                    _lvWorkbookTable_SelectLineAndMove(null, nd.NodeId);
                }
            }
            else
            {
                if (e.ChangedButton == MouseButton.Right)
                {
                    // this prevents selection of the cell that
                    // is not selectable but will open the context menu.
                    _dgActiveLine.ContextMenu.IsOpen = true;
                }
                
                // if row < 0 this could be a scrollbar so do not prevent WPF processing 
                if (row >= 0)
                {
                    e.Handled = true;
                }

            }
        }

        /// <summary>
        /// Determines whether the column at a given index is selectable.
        /// Only the columns showing plies are selectable.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private bool ViewActiveLine_IsSelectableColumn(int column)
        {
            return (column == _dgActiveLineWhitePlyColumn || column == _dgActiveLineBlackPlyColumn) ? true : false;
        }

        /// <summary>
        /// Determines whether the cell at a specified row and column is selectable.
        /// Returns true if the column is selectable and the cell contains a move/ply.
        /// Handles the edge case where the last row contains White's move only.
        /// Cells in this view are only selectable in MANUAL_REVIEW mode.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private bool ViewActiveLine_IsSelectableCell(int row, int column)
        {
            if (AppState.CurrentMode == AppState.Mode.MANUAL_REVIEW)
            {
                if (!ViewActiveLine_IsSelectableColumn(column))
                    return false;

                if (column == _dgActiveLineBlackPlyColumn
                    && row == ActiveLine.MoveList.Count - 1
                    && ActiveLine.MoveList[row].BlackPly == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Retruns the currently selected Node/Ply, if any.
        /// </summary>
        /// <returns></returns>
        private TreeNode ViewActiveLine_GetSelectedTreeNode()
        {
            int row, column;

            if (ViewActiveLine_GetSelectedRowColumn(out row, out column))
            {
                return ViewActiveLine_GetTreeNodeFromRowColumn(row, column);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns Node bound to the specified row and column.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private TreeNode ViewActiveLine_GetTreeNodeFromRowColumn(int row, int column)
        {
            int moveIndex = (row * 2) + (column == _dgActiveLineWhitePlyColumn ? 0 : 1);

            if (moveIndex + 1 < ActiveLine.GetPlyCount())
            {
                return ActiveLine.GetNodeAtIndex(moveIndex + 1);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Intercepts and handles key events in the Active Line view.
        /// Facilitates scrolling through the game using the keyboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewActiveLine_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            int row;
            int column;
            if (ViewActiveLine_GetSelectedRowColumn(out row, out column))
            {
                int selColumn = -1;
                int selRow = -1;

                int moveIndex;

                switch (e.Key)
                {
                    case Key.Left:
                        selColumn = column == _dgActiveLineWhitePlyColumn ? _dgActiveLineBlackPlyColumn : _dgActiveLineWhitePlyColumn;
                        selRow = column == _dgActiveLineWhitePlyColumn ? row - 1 : row;
                        break;
                    case Key.Right:
                        selColumn = column == _dgActiveLineWhitePlyColumn ? _dgActiveLineBlackPlyColumn : 1;
                        selRow = column == _dgActiveLineWhitePlyColumn ? row : row + 1;
                        // if we went beyond the last move (because it is White's and Black cell is empty.)
                        // switch back to the White column
                        moveIndex = (selRow * 2) + (selColumn == _dgActiveLineWhitePlyColumn ? 0 : 1);
                        if (moveIndex + 1 >= ActiveLine.GetPlyCount())
                        {
                            selColumn = _dgActiveLineWhitePlyColumn;
                        }
                        break;
                    case Key.Up:
                        selColumn = _dgActiveLineWhitePlyColumn;
                        selRow = 0;
                        break;
                    case Key.Down:
                        selRow = _dgActiveLine.Items.Count - 1;
                        selColumn = (ActiveLine.GetPlyCount() % 2) == 0 ? _dgActiveLineWhitePlyColumn : _dgActiveLineBlackPlyColumn;
                        break;
                }

                if (selRow >= 0 && selRow < _dgActiveLine.Items.Count)
                {
                    DataGridCellInfo cell = new DataGridCellInfo(_dgActiveLine.Items[selRow], _dgActiveLine.Columns[selColumn]);
                    _dgActiveLine.ScrollIntoView(_dgActiveLine.Items[selRow]);
                    _dgActiveLine.SelectedCells.Clear();
                    _dgActiveLine.SelectedCells.Add(cell);

                    moveIndex = (selRow * 2) + (selColumn == _dgActiveLineWhitePlyColumn ? 0 : 1);
                    TreeNode nd = ActiveLine.GetNodeAtIndex(moveIndex + 1);

                    if (gameReplay.IsReplayActive)
                    {
                        // request that the replay be stopped and the clicked
                        // position shown, unless this mouse down
                        // was part of double click (in which case the doble click
                        // handler will override this.
                        gameReplay.ShowPositionAndStop(nd);
                        //StopAnimation();
                        //gameReplay.Stop();
                    }
                    else
                    {
                        MainChessBoard.DisplayPosition(nd.Position);
                    }
                    _workbookView.SelectLineAndMove(null, nd.NodeId);
                    _lvWorkbookTable_SelectLineAndMove(null, nd.NodeId);
                }
                e.Handled = true;
            }
        }
    }
}


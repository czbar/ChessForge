using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using ChessPosition;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates functions that handle the Active Line view (DataGrid)
    /// </summary>
    public class ActiveLineManager
    {
        /// <summary>
        /// Holds all moves/plies of the active line.
        /// </summary>
        public ScoreSheet Line = new ScoreSheet();

        /// <summary>
        /// The DataGrid control visualizing the active line.
        /// </summary>
        private DataGrid _dgActiveLine;

        // column where White's plies are displayed
        private const int _dgActiveLineWhitePlyColumn = 1;

        // column where Black's plies are displayed
        private const int _dgActiveLineBlackPlyColumn = 3;

        // Application's Main Window
        private MainWindow _mainWin;

        /// <summary>
        /// Constructor.
        /// Sets reference to the DataGrid control
        /// visualizing the active line.
        /// </summary>
        /// <param name="dg"></param>
        public ActiveLineManager(DataGrid dg, MainWindow mainWin)
        {
            _mainWin = mainWin;
            _dgActiveLine = dg;
        }

        /// <summary>
        /// Clears all content and selection
        /// </summary>
        public void Clear()
        {
            _dgActiveLine.SelectedCells.Clear();
            Line.MoveList.Clear();
            Line.NodeList.Clear(); 
        }

        /// <summary>
        /// Figures out the node corresponding to 
        /// the selected cell and displays the position.
        /// </summary>
        public void DisplayPositionForSelectedCell()
        {
            int row, column;
            if (!GetSelectedRowColumn(out row, out column))
            {
                row = 0;
                column = 1;
            }
            SelectPly(row, column == 1 ? PieceColor.White : PieceColor.Black);
            int nodeIndex = GetNodeIndexFromRowColumn(row, column);
            TreeNode nd = GetNodeAtIndex(nodeIndex);
            if (nd != null)
            {
                _mainWin.DisplayPosition(nd.Position);
            }
        }

        /// <summary>
        /// Binds a new line to this object and DataGrid control.
        /// </summary>
        /// <param name="line"></param>
        public void SetNodeList(ObservableCollection<TreeNode> line)
        {
            Line.SetNodeList(line);
            _dgActiveLine.ItemsSource = Line.MoveList;
        }

        /// <summary>
        /// Gets the Node object from the Line
        /// given its id.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public TreeNode GetNodeFromId(int nodeId)
        {
            return Line.GetNodeFromId(nodeId);
        }

        /// <summary>
        /// Gets the Node object from the Line
        /// given its index on the Node list.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public TreeNode GetNodeAtIndex(int idx)
        {
            return Line.GetNodeAtIndex(idx);
        }

        /// <summary>
        /// Gets the Move object from the Line
        /// given its index in the Move list.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public MoveWithEval GetMoveAtIndex(int idx)
        {
            return Line.GetMoveAtIndex(idx);
        }

        /// <summary>
        /// Returns Node corresponding to a ply.
        /// </summary>
        /// <param name="moveIdx"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public TreeNode GetNodeForMove(int moveIdx, PieceColor color)
        {
            int nodeIdx = moveIdx * 2 + (color == PieceColor.White ? 0 : 1) + 1;
            return Line.GetNodeAtIndex(nodeIdx);
        }

        /// <summary>
        /// Gets the number of plies in the Line.
        /// </summary>
        /// <returns></returns>
        public int GetPlyCount()
        {
            return Line.GetPlyCount();
        }

        /// <summary>
        /// Returns the index of the Node for the ply
        /// currently selected in the Single Line View. 
        /// </summary>
        /// <returns></returns>
        public int GetSelectedPlyNodeIndex()
        {
            int row;
            int column;

            GetSelectedRowColumn(out row, out column);
            return GetPlyNodeIndexFromRowColumn(row, column);
        }

        /// <summary>
        /// Calculates the index of a node give the node's
        /// row and column.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        internal int GetNodeIndexFromRowColumn(int row, int column)
        {
            if (row < 0 || column < 0)
                return -1;

            int nodeIndex = (row * 2) + (column == _dgActiveLineWhitePlyColumn ? 0 : 1) + 1;

            return (nodeIndex < Line.GetPlyCount()) ? nodeIndex : -1;
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
        internal bool GetSelectedRowColumn(out int row, out int column)
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
        /// We get passed the move number of the parent of the node we want 
        /// to select and also parent's ColorToMove.
        /// We keep the color but adjust the move number if the ColorToMove
        /// is Black.
        /// </summary>
        /// <param name="moveNo">Number of the move</param>
        /// <param name="colorToMove">Side on move</param>
        internal void SelectPly(int moveNo, PieceColor colorToMove)
        {
            _dgActiveLine.SelectedCells.Clear();
            moveNo = Math.Max(moveNo, 0);

            if (moveNo == 0 && colorToMove != PieceColor.White)
            {
                return;
            }

            if (moveNo > _dgActiveLine.Items.Count || (moveNo == _dgActiveLine.Items.Count && colorToMove != PieceColor.Black))
            {
                return;
            }

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

        /// <summary>
        /// Returns the currently selected Node/Ply, if any.
        /// </summary>
        /// <returns></returns>
        public TreeNode GetSelectedTreeNode()
        {
            int row, column;

            if (GetSelectedRowColumn(out row, out column))
            {
                return GetTreeNodeFromRowColumn(row, column);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// A double click triggers a replay animation from the currently
        /// selected Node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int column = -1;
            int row = -1;

            GuiUtilities.GetDataGridColumnRowFromMouseClick(_dgActiveLine, e, out row, out column);

            ReplayLine(row, column);
        }

        public void ReplayLine(int row, int column = _dgActiveLineWhitePlyColumn)
        {
            // if there is replay happening now, stop it
            if (_mainWin.ActiveLineReplay.IsReplayActive)
            {
                _mainWin.StopMoveAnimation();
                _mainWin.BoardCommentBox.RestoreTitleMessage();
            }

            if (row >= 0)
            {
                int moveIndex = (row * 2) + (column == _dgActiveLineWhitePlyColumn ? 0 : 1);
                _mainWin.ActiveLineReplay.SetupTreeLineToDisplay(Line.NodeList, moveIndex + 1);
                _mainWin.BoardCommentBox.GameReplayStart();
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
        internal void PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            int column;
            int row;

            GuiUtilities.GetDataGridColumnRowFromMouseClick(_dgActiveLine, e, out row, out column);

            if (IsSelectableCell(row, column))
            {
                int moveIndex = (row * 2) + (column == _dgActiveLineWhitePlyColumn ? 0 : 1);

                if (moveIndex + 1 < Line.GetPlyCount())
                {
                    TreeNode nd = Line.GetNodeAtIndex(moveIndex + 1);

                    if (_mainWin.ActiveLineReplay.IsReplayActive)
                    {
                        // request that the replay be stopped and the clicked
                        // position shown, unless this mouse down
                        // was part of a double click (in which case the double click
                        // handler will override this).
                        _mainWin.ActiveLineReplay.ShowPositionAndStop(nd);
                        _mainWin.BoardCommentBox.RestoreTitleMessage();

                        //StopAnimation();
                        //gameReplay.Stop();
                    }

                    _mainWin.DisplayPosition(nd.Position);
                    _mainWin.SelectLineAndMoveInWorkbookViews(null, nd.NodeId);
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
        /// Intercepts and handles key events in the Active Line view.
        /// Facilitates scrolling through the game using the keyboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void PreviewKeyDown(object sender, KeyEventArgs e)
        {
            int row;
            int column;
            if (GetSelectedRowColumn(out row, out column))
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
                        if (moveIndex + 1 >= Line.GetPlyCount())
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
                        selColumn = (Line.GetPlyCount() % 2) == 0 ? _dgActiveLineWhitePlyColumn : _dgActiveLineBlackPlyColumn;
                        break;
                }

                if (selRow >= 0 && selRow < _dgActiveLine.Items.Count)
                {
                    DataGridCellInfo cell = new DataGridCellInfo(_dgActiveLine.Items[selRow], _dgActiveLine.Columns[selColumn]);
                    _dgActiveLine.ScrollIntoView(_dgActiveLine.Items[selRow]);
                    _dgActiveLine.SelectedCells.Clear();
                    _dgActiveLine.SelectedCells.Add(cell);

                    moveIndex = (selRow * 2) + (selColumn == _dgActiveLineWhitePlyColumn ? 0 : 1);
                    TreeNode nd = Line.GetNodeAtIndex(moveIndex + 1);

                    if (_mainWin.ActiveLineReplay.IsReplayActive)
                    {
                        // request that the replay be stopped and the clicked
                        // position shown, unless this mouse down
                        // was part of double click (in which case the doble click
                        // handler will override this.
                        _mainWin.ActiveLineReplay.ShowPositionAndStop(nd);
                    }
                    else
                    {
                        _mainWin.DisplayPosition(nd.Position);
                    }
                    _mainWin.SelectLineAndMoveInWorkbookViews(null, nd.NodeId);
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// TODO: we do not need both this and the next functions. Rationalize here.
        /// Returns the index of the Node for the ply
        /// at a given row and column. 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private int GetPlyNodeIndexFromRowColumn(int row, int column)
        {
            if (row < 0 || column < 0)
                return -1;

            int moveIndex = (row * 2) + (column == _dgActiveLineWhitePlyColumn ? 0 : 1);
            return moveIndex;
        }

        /// <summary>
        /// Determines whether the column at a given index is selectable.
        /// Only the columns showing plies are selectable.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private bool IsSelectableColumn(int column)
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
        private bool IsSelectableCell(int row, int column)
        {
            if (LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                if (!IsSelectableColumn(column))
                    return false;

                if (column == _dgActiveLineBlackPlyColumn
                    && row == Line.MoveList.Count - 1
                    && Line.MoveList[row].BlackPly == null)
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
        /// Returns Node bound to the specified row and column.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private TreeNode GetTreeNodeFromRowColumn(int row, int column)
        {
            int moveIndex = (row * 2) + (column == _dgActiveLineWhitePlyColumn ? 0 : 1);

            if (moveIndex + 1 < Line.GetPlyCount())
            {
                return Line.GetNodeAtIndex(moveIndex + 1);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Accessor to the list that is bound to the DataGrid control.
        /// </summary>
        private ObservableCollection<MoveWithEval> MoveList
        {
            get { return Line.MoveList; }
            set { Line.MoveList = value; }
        }
    }
}


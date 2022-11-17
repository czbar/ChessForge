using System;
using System.Collections.ObjectModel;
using System.Text;
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

        private int _selectedRow = -1;

        private int _selectedColumn = -1;

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
            ClearSelection();

            Line.MoveList.Clear();
            Line.NodeList.Clear();
        }

        /// <summary>
        /// Figures out the node corresponding to 
        /// the selected cell and displays the position.
        /// </summary>
        public void DisplayPositionForSelectedCell()
        {
            if (!GetSelectedRowColumn(out int row, out int column))
            {
                row = 0;
                column = 1;
            }
            SelectPly(row, column == 1 ? PieceColor.White : PieceColor.Black);
            int nodeIndex = GetNodeIndexFromRowColumn(row, column);
            TreeNode nd = GetNodeAtIndex(nodeIndex);
            if (nd != null)
            {
                _mainWin.DisplayPosition(nd);
            }
        }

        /// <summary>
        /// Sets evaluation string on the node and move objects
        /// </summary>
        /// <param name="moveIndex"></param>
        /// <param name="color"></param>
        /// <param name="eval"></param>
        public void SetEvaluation(TreeNode nd, string eval)
        {
            if (nd != null)
            {
                PieceColor color;
                MoveWithEval mev = Line.GetMoveFromNodeId(nd.NodeId, out color);

                if (mev != null)
                {
                    if (color == PieceColor.White)
                    {
                        mev.WhiteEval = eval;
                    }
                    else
                    {
                        mev.BlackEval = eval;
                    }
                }

                AppStateManager.IsDirty = true;
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
            if (Configuration.DebugLevel > 0)
            {
                if (WorkbookManager.ActiveTab == WorkbookManager.TabViewType.EXERCISE)
                {
                    AppLog.Message("_dgActiveLine.ItemsSource bound to Exercise, Line.MoveList.Count=" + Line.MoveList.Count.ToString());
                }
            }
            _selectedRow = -1;
            _selectedColumn = -1;
        }

        /// <summary>
        /// Returns the last Node of the Line.
        /// </summary>
        /// <returns></returns>
        public TreeNode GetLastNode()
        {
            return Line.GetLastNode();
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
        /// Returns the id of the line represented by this object.
        /// This is LineId of the last node in the list.
        /// </summary>
        /// <returns></returns>
        public string GetLineId()
        {
            return Line.GetLineId();
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
        /// If no ply selected, returns either 0 or -1
        /// depending on the bZeroOnNoSelection parameter
        /// </summary>
        /// <returns></returns>
        public int GetSelectedPlyNodeIndex(bool bZeroOnNoSelection)
        {
            GetSelectedRowColumn(out int row, out int column);
            int index = GetNodeIndexFromRowColumn(row, column);
            if (index >= 0)
            {
                return index;
            }
            else
            {
                return bZeroOnNoSelection ? 0 : -1;
            }
        }

        /// <summary>
        /// Finds index of a Node in the Node/Ply list.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public int GetIndexForNode(TreeNode nd)
        {
            return Line.GetIndexForNode(nd);
        }

        /// <summary>
        /// Finds index of a Node with the given NodeId in the Node/Ply list.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public int GetIndexForNode(int nodeId)
        {
            return Line.GetIndexForNode(nodeId);
        }

        /// <summary>
        /// Calculates the index of a node given the node's
        /// row and column.
        /// Allow for lines that start with a Black move (e.g. exercises)
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public int GetNodeIndexFromRowColumn(int row, int column)
        {
            if (row < 0 || column < 0)
                return -1;

            PieceColor startingColor = Line.NodeList[0].ColorToMove;
            int nodeIndex = (row * 2) + (column == _dgActiveLineWhitePlyColumn ? 0 : 1) + 1;
            if (startingColor != PieceColor.White)
            {
                nodeIndex--;
            }

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
        public bool GetSelectedRowColumn(out int row, out int column)
        {
            row = _selectedRow;
            column = _selectedColumn;

            return row >= 0 && column >= 0;
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
        public void SelectPly(int moveNo, PieceColor colorToMove)
        {
            // the under board message may not be relevant anymore
            _mainWin.BoardCommentBox.RestoreTitleMessage();

            ClearSelection();

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
                _selectedRow = moveNo;
                _selectedColumn = _dgActiveLineWhitePlyColumn;
            }
            else
            {
                _selectedRow = moveNo - 1;
                _selectedColumn = _dgActiveLineBlackPlyColumn;

            }
            cell = new DataGridCellInfo(_dgActiveLine.Items[_selectedRow], _dgActiveLine.Columns[_selectedColumn]);
            _dgActiveLine.ScrollIntoView(_dgActiveLine.Items[_selectedRow]);

            var cellContent = cell.Column.GetCellContent(cell.Item);
            if (cellContent == null)
            {
                _dgActiveLine.UpdateLayout();
                cellContent = cell.Column.GetCellContent(cell.Item);
            }

            try
            {
                _dgActiveLine.SelectedCells.Add(cell);
            }
            catch (Exception ex)
            {
                AppLog.Message("_dgActiveLine.SelectedCells.Add(cell) in SelectPly", ex);
            }

            if (cellContent == null)
            {
                string msg = "Cell content is null in SelectPly(): " + "row=" + _selectedRow.ToString() + " column=" + _selectedColumn.ToString();
                DebugUtils.ShowDebugMessage(msg);
                AppLog.Message(msg);
                AppLog.Message("_dgActiveLine.Items.Count=" + _dgActiveLine.Items.Count.ToString());
                AppLog.Message("Line.MoveList.Count=" + Line.MoveList.Count.ToString());
            }
        }

        /// <summary>
        /// Returns the currently selected Node/Ply, if any.
        /// </summary>
        /// <returns></returns>
        public TreeNode GetSelectedTreeNode()
        {
            if (GetPlyCount() == 1)
            {
                // game with no moves
                return _mainWin.ActiveVariationTree.Nodes[0];
            }

            if (GetSelectedRowColumn(out int row, out int column))
            {
                return GetTreeNodeFromRowColumn(row, column);
            }
            else
            {
                return _mainWin.ActiveVariationTree.Nodes[0];
            }
        }

        /// <summary>
        /// Checks if the currently selected item is last
        /// </summary>
        /// <returns></returns>
        public bool IsLastMoveSelected()
        {
            return GetSelectedPlyNodeIndex(true) == GetNodeCount() - 1;
        }

        /// <summary>
        /// Returns the number of nodes in the list.
        /// </summary>
        /// <returns></returns>
        public int GetNodeCount()
        {
            return Line.NodeList.Count;
        }

        /// <summary>
        /// A double click triggers a replay animation from the currently
        /// selected Node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            GuiUtilities.GetDataGridColumnRowFromMouseClick(_dgActiveLine, e, out int row, out int column);

            ReplayLine(row, column);
        }

        /// <summary>
        /// Automatically replays the currently selected line 
        /// on the main chessboard.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        public void ReplayLine(int row, int column = _dgActiveLineWhitePlyColumn)
        {
            if (EvaluationManager.IsRunning)
            {
                EngineMessageProcessor.StopEngineEvaluation();
                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);
            }

            AppStateManager.SwapCommentBoxForEngineLines(false);

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
        public void PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            GuiUtilities.GetDataGridColumnRowFromMouseClick(_dgActiveLine, e, out int row, out int column);

            _mainWin.BoardCommentBox.ShowWorkbookTitle();

            if (IsSelectableCell(row, column))
            {
                int moveIndex = (row * 2) + (column == _dgActiveLineWhitePlyColumn ? 0 : 1) + 1;

                if (moveIndex < Line.GetPlyCount())
                {
                    TreeNode nd = Line.GetNodeAtIndex(moveIndex);

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

                    if (EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE)
                    {
                        _mainWin.StopEvaluation();
                    }

                    _selectedRow = row;
                    _selectedColumn = column;

                    _mainWin.DisplayPosition(nd);
                    _mainWin.SelectLineAndMoveInWorkbookViews(_mainWin.ActiveTreeView, Line.GetLineId(), moveIndex);
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
        /// Bring the selected run in the Active View into view on mouse up. 
        /// It may not have happened in the mouse down handler if we were updating 
        /// the RTB Document as part of the handling.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _mainWin.BringSelectedRunIntoView();
        }

        /// <summary>
        /// Intercepts and handles key events in the Active Line view.
        /// Facilitates scrolling through the game using the keyboard.
        /// If the key modifier is CTRL do not mark as handled as we need
        /// keyboard shortcut to work.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                if (HandleKeyDown(e.Key))
                {
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Bring the selected run in the Active View into view on key up. 
        /// It may not have happened in the key down handler if we were updating 
        /// the RTB Document as part of the handling.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                if (HandleKeyUp(e.Key))
                {
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Handles the KeyDown event
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool HandleKeyDown(Key key)
        {
            // prevent "cheating" in exercises
            if (_mainWin.ActiveVariationTree == null || !_mainWin.ActiveVariationTree.ShowTreeLines || AppStateManager.CurrentSolvingMode == VariationTree.SolvingMode.GUESS_MOVE)
            {
                return true;
            }

            bool handled = false;

            GetSelectedRowColumn(out int currRow, out int currColumn);

            //if row and column == -1, it means there is no selection.
            // if we have any moves (except the 0/null move) we will "fake" selection, if not we bail
            if (currRow < 0 && currColumn < 0)
            {
                if (Line.GetPlyCount() == 1)
                {
                    return false;
                }
                else
                {
                    // an exercise my start from white or black
                    if (Line.NodeList[0].ColorToMove == PieceColor.White)
                    {
                        // fake that "0" move for the Black side is currently selected
                        currRow = -1;
                        currColumn = _dgActiveLineBlackPlyColumn;
                    }
                    else
                    {
                        currRow = 0;
                        currColumn = _dgActiveLineWhitePlyColumn;
                    }
                }
            }

            bool validKey = CalculatePostKeyDownSelection(key, currRow, currColumn, out int postKeyDownRow, out int postKeyDownColumn);

            if (validKey)
            {
                int plyIndex;
                if (postKeyDownRow < _dgActiveLine.Items.Count)
                {
                    if (postKeyDownRow >= 0)
                    {
                        _selectedRow = postKeyDownRow;
                        _selectedColumn = postKeyDownColumn;

                        DataGridCellInfo cell = new DataGridCellInfo(_dgActiveLine.Items[postKeyDownRow], _dgActiveLine.Columns[postKeyDownColumn]);
                        _dgActiveLine.ScrollIntoView(_dgActiveLine.Items[postKeyDownRow]);
                        _dgActiveLine.SelectedCells.Clear();
                        _dgActiveLine.SelectedCells.Add(cell);

                        plyIndex = (postKeyDownRow * 2) + (postKeyDownColumn == _dgActiveLineWhitePlyColumn ? 0 : 1) + 1;
                        // check for Exercise starting with Black move
                        if (plyIndex != 0 && Line.NodeList[0].ColorToMove == PieceColor.Black)
                        {
                            plyIndex--;
                        }
                    }
                    else
                    {
                        ClearSelection();
                        plyIndex = 0;
                    }

                    TreeNode nd = Line.GetNodeAtIndex(plyIndex);

                    if (nd != null)
                    {
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
                            _mainWin.DisplayPosition(nd);
                        }
                        _mainWin.SelectLineAndMoveInWorkbookViews(_mainWin.ActiveTreeView, Line.GetLineId(), plyIndex);
                    }
                }
                handled = true;
            }

            return handled;
        }

        public bool HandleKeyUp(Key key)
        {
            if (_mainWin.ActiveVariationTree == null || !_mainWin.ActiveVariationTree.ShowTreeLines || AppStateManager.CurrentSolvingMode == VariationTree.SolvingMode.GUESS_MOVE)
            {
                return true;
            }

            _mainWin.BringSelectedRunIntoView();

            return true;
        }

        /// <summary>
        /// Clears the selection in the DataGrid
        /// and tracking variables.
        /// </summary>
        private void ClearSelection()
        {
            _dgActiveLine.SelectedCells.Clear();
            _selectedRow = -1;
            _selectedColumn = -1;
        }

        /// <summary>
        /// Returns the new selected row and column based on the current selection 
        /// and the key the user pressed.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="currRow"></param>
        /// <param name="currColumn"></param>
        /// <param name="postKeyDownRow"></param>
        /// <param name="postKeyDownColumn"></param>
        /// <returns></returns>
        private bool CalculatePostKeyDownSelection(Key key, int currRow, int currColumn, out int postKeyDownRow, out int postKeyDownColumn)
        {
            bool validKey = true;

            postKeyDownColumn = -1;
            postKeyDownRow = -1;

            switch (key)
            {
                case Key.Left:
                    postKeyDownColumn = currColumn == _dgActiveLineWhitePlyColumn ? _dgActiveLineBlackPlyColumn : _dgActiveLineWhitePlyColumn;
                    postKeyDownRow = currColumn == _dgActiveLineWhitePlyColumn ? currRow - 1 : currRow;
                    // check for Exercise starting with Black move
                    if (Line.NodeList[0].ColorToMove == PieceColor.Black && postKeyDownRow == -1)
                    {
                        postKeyDownRow = 0;
                        postKeyDownColumn = _dgActiveLineWhitePlyColumn;
                    }
                    break;
                case Key.Right:
                    postKeyDownColumn = currColumn == _dgActiveLineWhitePlyColumn ? _dgActiveLineBlackPlyColumn : 1;
                    postKeyDownRow = currColumn == _dgActiveLineWhitePlyColumn ? currRow : currRow + 1;
                    // if we went beyond the last move (because it is White's and Black cell is empty.)
                    // switch back to the White column
                    int selectedPlyIndex = (postKeyDownRow * 2) + (postKeyDownColumn == _dgActiveLineWhitePlyColumn ? 0 : 1) + 1;

                    // check for Exercise starting with Black move
                    if (selectedPlyIndex != 0 && Line.NodeList[0].ColorToMove == PieceColor.Black)
                    {
                        selectedPlyIndex--;
                    }
                    if (selectedPlyIndex >= Line.GetPlyCount())
                    {
                        postKeyDownColumn = _dgActiveLineWhitePlyColumn;
                    }
                    break;
                case Key.Up:
                    postKeyDownColumn = _dgActiveLineWhitePlyColumn;
                    postKeyDownRow = 0;
                    break;
                case Key.Home:
                    postKeyDownRow = -1;
                    break;
                case Key.Down:
                case Key.End:
                    postKeyDownRow = _dgActiveLine.Items.Count - 1;
                    postKeyDownColumn = (Line.GetPlyCount() % 2) == 0 ? _dgActiveLineWhitePlyColumn : _dgActiveLineBlackPlyColumn;
                    break;
                default:
                    validKey = false;
                    break;
            }

            return validKey;
        }

        /// <summary>
        /// Determines whether the column at a given index is selectable.
        /// Only the columns showing plies are selectable.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private bool IsSelectableColumn(int column)
        {
            return (column == _dgActiveLineWhitePlyColumn || column == _dgActiveLineBlackPlyColumn);
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
        /// Returns the color of the side making the first move.
        /// It is always White except for exercises with Black 
        /// to move first.
        /// </summary>
        /// <returns></returns>
        public PieceColor GetStartingColor()
        {
            if (Line.NodeList.Count == 0)
            {
                return PieceColor.None;
            }
            else
            {
                return Line.NodeList[0].ColorToMove;
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
            int nodeIndex;
            nodeIndex = (row * 2) + (column == _dgActiveLineWhitePlyColumn ? 0 : 1);

            if (Line.NodeList[0].ColorToMove == PieceColor.Black)
            {
                nodeIndex--;
            }

            if (nodeIndex + 1 < Line.GetPlyCount())
            {
                return Line.GetNodeAtIndex(nodeIndex + 1);
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


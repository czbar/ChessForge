using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Windows.Input;
using static ChessForge.CommentBox;

namespace ChessForge
{
    public partial class VariationTreeView : RichTextBuilder    
    {

        /// <summary>
        /// Sets a line and move in the VariationTree view.
        /// This only sets and highlights the selected line and move, it does not update the ActiveLine.
        /// It is called when left/right arrow or Home/End keys are pressed or when a move is clicked
        /// in the Scoresheet or the Evaluation Chart (since those actions do not change the ActiveLine).
        /// </summary>
        /// <param name="lineId"></param>
        /// <param name="index"></param>
        public void SelectLineAndMoveInWorkbookViews(string lineId, int index, bool queryExplorer)
        {
            try
            {
                TreeNode nd = _mainWin.ActiveLine.GetNodeAtIndex(index);
                if (nd == null)
                {
                    // try the node at index 0
                    nd = _mainWin.ActiveLine.GetNodeAtIndex(0);
                }

                if (nd != null && WorkbookManager.SessionWorkbook.ActiveVariationTree != null)
                {
                    WorkbookManager.SessionWorkbook.ActiveVariationTree.SetSelectedLineAndMove(lineId, nd.NodeId);
                    HighlightLineAndMove(HostRtb.Document, lineId, nd.NodeId);
                    if (EvaluationManager.CurrentMode == EvaluationManager.Mode.CONTINUOUS && AppState.ActiveTab != TabViewType.CHAPTERS)
                    {
                        _mainWin.EvaluateActiveLineSelectedPosition(nd);
                    }
                    if (AppState.MainWin.UiEvalChart.Visibility == System.Windows.Visibility.Visible)
                    {
                        if (AppState.MainWin.UiEvalChart.IsDirty)
                        {
                            MultiTextBoxManager.ShowEvaluationChart(true);
                        }
                        AppState.MainWin.UiEvalChart.SelectMove(nd);
                    }
                    if (queryExplorer)
                    {
                        _mainWin.OpeningStatsView.SetOpeningName();
                        WebAccessManager.ExplorerRequest(AppState.ActiveTreeId, ShownVariationTree.SelectedNode);
                    }
                }
            }
            catch
            {
            }
        }


        /// <summary>
        /// Selects the passed node along with its line id.
        /// TODO: this should not be necessary, replace with a call to SelectNode(TreeNode);
        /// </summary>
        /// <param name="nodeId"></param>
        public void SelectNode(FlowDocument doc, int nodeId)
        {
            TreeNode node = ShownVariationTree.GetNodeFromNodeId(nodeId);
            if (node != null)
            {
                HighlightLineAndMove(doc, node.LineId, nodeId);
            }
        }

        /// <summary>
        /// Selects a line for the next/prev sibling if we are at fork.
        /// </summary>
        /// <param name="prevNext"></param>
        /// <returns></returns>
        public TreeNode SelectParallelLine(bool prevNext)
        {
            TreeNode node = null;

            try
            {
                TreeNode currNode = GetSelectedNode();
                node = TreeUtils.GetNextSibling(currNode, prevNext, true);
            }
            catch { }

            if (node != null)
            {
                SelectNode(node);
            }

            return node;
        }

        /// <summary>
        /// Selects the passed node.
        /// Selects the move, its line and the line in ActiveLine.
        /// </summary>
        /// <param name="node"></param>
        public void SelectNode(TreeNode node)
        {
            try
            {
                SelectRun(_dictNodeToRun[node.NodeId], 1, MouseButton.Left);
            }
            catch { }
        }

        /// <summary>
        /// Highlights the line with the passed lineId and the Move with the passed nodeId. 
        /// Does not touch or reset colors of any other nodes.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="lineId"></param>
        public void HighlightLineAndMove(FlowDocument doc, string lineId, int nodeId)
        {
            if (!IsSelectionEnabled())
            {
                return;
            }

            if (ShownVariationTree.ShowTreeLines)
            {
                if (doc == null)
                {
                    doc = HostRtb.Document;
                }

                if (nodeId == 0)
                {
                    HostRtb.ScrollToHome();
                }

                try
                {
                    BuildForkTable(doc, nodeId);

                    ObservableCollection<TreeNode> lineToSelect = ShownVariationTree.GetNodesForLine(lineId);

                    _selectedRun = null;
                    _dictNodeToRun.TryGetValue(nodeId, out _selectedRun);

                    if (!string.IsNullOrEmpty(lineId))
                    {
                        foreach (TreeNode nd in lineToSelect)
                        {
                            if (nd.NodeId != 0)
                            {
                                //we should always have this key, so report in the debug mode if not
                                if (_dictNodeToRun.ContainsKey(nd.NodeId))
                                {
                                    Run run = _dictNodeToRun[nd.NodeId];
                                    if (run.Background != ChessForgeColors.CurrentTheme.RtbSelectLineBackground)
                                    {
                                        run.Background = ChessForgeColors.CurrentTheme.RtbSelectLineBackground;
                                    }
                                    if (run.Foreground != ChessForgeColors.CurrentTheme.RtbSelectLineForeground)
                                    {
                                        run.Foreground = ChessForgeColors.CurrentTheme.RtbSelectLineForeground;
                                    }
                                }
                                else if (Configuration.DebugLevel != 0)
                                {
                                    //we should always have this key, so show debug message if not
                                    if (_debugSelectedBkgMsgCount < 2)
                                    {
                                        DebugUtils.ShowDebugMessage("WorkbookView:SelectLineAndMove()-BrushSelectedBkg nodeId=" + nd.NodeId.ToString() + " not in _dictNodeToRun");
                                        _debugSelectedBkgMsgCount++;
                                    }
                                    AppLog.Message("WorkbookView:SelectLineAndMove()-BrushSelectedBkg nodeId=" + nd.NodeId.ToString() + " not in _dictNodeToRun");
                                }
                            }
                        }
                    }

                    if (_selectedRun != null)
                    {
                        _selectedRun.Background = ChessForgeColors.CurrentTheme.RtbSelectRunBackground;
                        _selectedRun.Foreground = ChessForgeColors.CurrentTheme.RtbSelectRunForeground;

                        if (nodeId != 0)
                        {
                            _selectedRun.BringIntoView();
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Message("SelectLineAndMove()", ex);
                }
            }
        }

        /// <summary>
        /// Sets background for all moves in the currently selected line.
        /// </summary>
        /// <param name="lineId"></param>
        /// <param name="nodeId"></param>
        public void SetAndSelectActiveLine(string lineId, int nodeId)
        {
            // TODO: do not select line and therefore repaint everything if the clicked line is already selected
            // UNLESS there is "copy select" active
            ObservableCollection<TreeNode> lineToSelect = ShownVariationTree.GetNodesForLine(lineId);
            WorkbookManager.SessionWorkbook.ActiveVariationTree.SetSelectedLineAndMove(lineId, nodeId);
            _mainWin.SetActiveLine(lineToSelect, nodeId);
            HighlightActiveLine();
        }

        /// <summary>
        /// Unhighlights the active line including the selected run.
        /// </summary>
        public void UnhighlightActiveLine()
        {
            try
            {
                ObservableCollection<TreeNode> line = AppState.MainWin.GetActiveLine();
                foreach (TreeNode nd in line)
                {
                    if (nd.NodeId != 0)
                    {
                        if (_dictNodeToRun.TryGetValue(nd.NodeId, out Run run))
                        {
                            if (run.Background != ChessForgeColors.CurrentTheme.RtbBackground)
                            {
                                run.Background = ChessForgeColors.CurrentTheme.RtbBackground;
                            }
                            
                            if (run.Foreground != ChessForgeColors.CurrentTheme.RtbForeground)
                            {
                                run.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
                            }
                        }
                    }
                }                
            }
            catch { }
        }

        /// <summary>
        /// Highlights the active line.
        /// </summary>
        private void HighlightActiveLine()
        {
            // TODO: duplicates functionality of HighlightLineAndMove
            try
            {
                ObservableCollection<TreeNode> line = _mainWin.GetActiveLine();
                foreach (TreeNode nd in line)
                {
                    if (nd.NodeId != 0)
                    {
                        if (_dictNodeToRun.TryGetValue(nd.NodeId, out Run run))
                        {
                            if (run.Background != ChessForgeColors.CurrentTheme.RtbSelectLineBackground)
                            {
                                run.Background = ChessForgeColors.CurrentTheme.RtbSelectLineBackground;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Selects the entire subtree under the currently selected node.
        /// </summary>
        public void SelectSubtreeForCopy()
        {
            ClearCopySelect();

            TreeNode selectedNode = GetSelectedNode();
            if (selectedNode != null)
            {
                List<TreeNode> lstNodes = ShownVariationTree.BuildSubTreeNodeList(selectedNode, false);
                _selectedForCopy.AddRange(lstNodes);
                HighlightSelectedForCopy();
            }
        }

        /// <summary>
        /// Selects for copy the currently highlighted line.
        /// </summary>
        public void SelectActiveLineForCopy()
        {
            ClearCopySelect();

            ObservableCollection<TreeNode> lstNodes = _mainWin.GetActiveLine();
            _selectedForCopy.AddRange(lstNodes);
            HighlightSelectedForCopy();
        }

        /// <summary>
        /// Places a deep copy of the "selected for copy" nodes in the clipboard
        /// </summary>
        public void PlaceSelectedForCopyInClipboard()
        {
            if (_selectedForCopy.Count == 0)
            {
                TreeNode nd = GetSelectedNode();
                if (nd != null && nd.NodeId != 0)
                {
                    _selectedForCopy.Add(nd);
                }
            }

            if (_selectedForCopy.Count > 0)
            {
                List<TreeNode> lstNodes = TreeUtils.CopyNodeList(_selectedForCopy);
                SystemClipboard.CopyMoveList(lstNodes, ShownVariationTree.MoveNumberOffset);
                _mainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgCopiedMoves, HintType.INFO);
            }
        }

        /// <summary>
        /// Change background to the "Copy Select" color
        /// for all nodes between the selected node to the passed one. 
        /// </summary>
        /// <param name="r"></param>
        private void SetCopySelect(Run r)
        {
            try
            {
                if (_selectedForCopy.Count > 0)
                {
                    HighlightActiveLine();
                }

                _selectedForCopy.Clear();

                TreeNode currSelected = GetSelectedNode();
                TreeNode shiftClicked = null;
                if (r.Name != null && r.Name.StartsWith(_run_))
                {
                    int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
                    shiftClicked = ShownVariationTree.GetNodeFromNodeId(nodeId);
                }

                if (currSelected != null && shiftClicked != null)
                {
                    // check if there is a branch between the 2
                    TreeNode node_1 = shiftClicked;
                    TreeNode node_2 = currSelected;

                    if (currSelected.MoveNumber < shiftClicked.MoveNumber || currSelected.MoveNumber == shiftClicked.MoveNumber && currSelected.ColorToMove == PieceColor.Black)
                    {
                        node_1 = currSelected;
                        node_2 = shiftClicked;
                    }

                    bool found = false;
                    while (node_2.Parent != null && node_1.MoveNumber <= node_2.MoveNumber)
                    {
                        _selectedForCopy.Insert(0, node_2);
                        if (node_2.NodeId == node_1.NodeId)
                        {
                            found = true;
                            break;
                        }
                        node_2 = node_2.Parent;
                    }

                    if (found)
                    {
                        HighlightSelectedForCopy();

                        // Commented out as we do not want the "automatic selection" but want to 
                        // act only after CTRL+C.
                        // Not sure why it was done like this in the first place.
                        //
                        //PlaceSelectedForCopyInClipboard();
                    }
                    else
                    {
                        _selectedForCopy.Clear();
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Highlights the nodes selected for copy.
        /// </summary>
        private void HighlightSelectedForCopy()
        {
            try
            {
                TreeNode selectedNode = GetSelectedNode();
                foreach (TreeNode nd in _selectedForCopy)
                {
                    if (nd == selectedNode)
                    {
                        _dictNodeToRun[nd.NodeId].Foreground = ChessForgeColors.CurrentTheme.RtbSelectMoveWhileCopyForeground;
                        _dictNodeToRun[nd.NodeId].Background = ChessForgeColors.CurrentTheme.RtbSelectMoveWhileCopyBackground;
                    }
                    else
                    {
                        _dictNodeToRun[nd.NodeId].Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
                        _dictNodeToRun[nd.NodeId].Background = ChessForgeColors.CurrentTheme.RtbSelectMovesForCopyBackground;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("HighlightSelectedForCopy()", ex);
            }
        }

        /// <summary>
        /// Clears the "for Copy" selection.
        /// </summary>
        protected void ClearCopySelect()
        {
            try
            {
                // reset copy selection if any
                if (_selectedForCopy.Count > 0)
                {
                    HighlightActiveLine();

                    TreeNode selectedNode = GetSelectedNode();
                    foreach (TreeNode nd in _selectedForCopy)
                    {
                        _dictNodeToRun[nd.NodeId].Background = ChessForgeColors.CurrentTheme.RtbBackground; ;
                    }
                    HighlightActiveLine();
                    if (selectedNode != null)
                    {
                        _dictNodeToRun[selectedNode.NodeId].Foreground = ChessForgeColors.CurrentTheme.RtbSelectRunForeground;
                        _dictNodeToRun[selectedNode.NodeId].Background = ChessForgeColors.CurrentTheme.RtbSelectRunBackground;
                    }
                    _selectedForCopy.Clear();
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Select a Run.
        /// </summary>
        /// <param name="runToSelect"></param>
        /// <param name="clickCount"></param>
        /// <param name="changedButton"></param>
        protected void SelectRun(Run runToSelect, int clickCount, MouseButton changedButton)
        {
            if (!IsSelectionEnabled() || runToSelect == null)
            {
                return;
            }

            try
            {
                if (changedButton == MouseButton.Left)
                {
                    ClearCopySelect();
                }

                // restore colors of the currently selected Run that will be unselected below.
                if (runToSelect != _selectedRun)
                {
                    _selectedRun.Background = ChessForgeColors.CurrentTheme.RtbBackground;
                    _selectedRun.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
                }

                int nodeId = TextUtils.GetIdFromPrefixedString(runToSelect.Name);
                TreeNode nd = ShownVariationTree.GetNodeFromNodeId(nodeId);

                if (clickCount == 2)
                {
                    if (_mainWin.InvokeAnnotationsDialog(nd))
                    {
                        InsertOrUpdateCommentRun(nd);
                    }
                }
                else
                {
                    if (changedButton == MouseButton.Left && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
                    {
                        SetCopySelect(runToSelect);
                    }
                    else
                    {
                        if (EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE)
                        {
                            _mainWin.StopEvaluation(true);
                            AppState.SwapCommentBoxForEngineLines(false);
                        }

                        UnhighlightActiveLine();
                        _selectedRun = runToSelect;
                        BuildForkTable(HostRtb.Document, nodeId);

                        if (runToSelect.Name != null && runToSelect.Name.StartsWith(_run_))
                        {
                            // This should never be needed but protect against unexpected timoing issue with sync/async processing
                            if (!ShownVariationTree.HasLinesCalculated())
                            {
                                ShownVariationTree.BuildLines();
                            }

                            string lineId = ShownVariationTree.GetDefaultLineIdForNode(nodeId);

                            SetAndSelectActiveLine(lineId, nodeId);
                        }

                        _selectedRun.Background = ChessForgeColors.CurrentTheme.RtbSelectRunBackground;
                        _selectedRun.Foreground = ChessForgeColors.CurrentTheme.RtbSelectRunForeground;

                        // this is a right click offer the context menu
                        if (changedButton == MouseButton.Right)
                        {
                            _lastClickedNodeId = nodeId;
                            EnableActiveTreeViewMenus(changedButton, true);
                        }
                        else
                        {
                            _lastClickedNodeId = nodeId;
                        }

                        if (changedButton != MouseButton.Left)
                        {
                            // restore selection for copy
                            HighlightSelectedForCopy();
                        }
                    }
                }
            }
            catch { }
        }

    }
}

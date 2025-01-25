using ChessPosition;
using GameTree;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ChessForge
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// After a right click, present a dialog where the user can choose the lines
        /// to insert into the view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbEngineLines_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ProcessAndPasteEngineLines(false);
        }

        /// <summary>
        /// After a double click, grab the first engine line and add it to the current view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbEngineLines_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ProcessAndPasteEngineLines(true);
        }

        /// <summary>
        /// Prepares the engine lines and pastes them into the current view.
        /// </summary>
        /// <param name="singleLine"></param>
        private void ProcessAndPasteEngineLines(bool singleLine)
        {
            try
            {
                // only proceed if we are in manual review mode and not in line evaluation mode
                if (AppState.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW 
                    && AppState.CurrentEvaluationMode != EvaluationManager.Mode.LINE)
                {
                    BuildEngineLines(singleLine, out List<List<TreeNode>> treeNodeLines, out List<string> algebraicLines);

                    bool proceed = true;
                    if (!singleLine && algebraicLines.Count > 0)
                    {
                        if (!UserLineSelection(algebraicLines, treeNodeLines))
                        {
                            proceed = false;
                        }
                    }

                    if (proceed)
                    {
                        List<TreeNode> insertedNewNodes = new List<TreeNode>();
                        List<TreeNode> failedInsertions = new List<TreeNode>();

                        VariationTreeView targetView = AppState.MainWin.ActiveTreeView;
                        TreeNode insertAtNode = targetView.GetSelectedNode();
                        TreeNode firstInserted = PasteEngineLines(AppState.ActiveVariationTree, insertAtNode, treeNodeLines,
                                         insertedNewNodes, failedInsertions);

                        targetView.BuildFlowDocumentForVariationTree(false);
                        AppState.MainWin.SetActiveLine(firstInserted.LineId, firstInserted.NodeId);
                        targetView.SelectNode(firstInserted);
                        targetView.HighlightLineAndMove(null, firstInserted.LineId, firstInserted.NodeId);

                        if (insertedNewNodes.Count > 0)
                        {
                            string msg = Properties.Resources.FlMsgPastedMovesCount + ": " + insertedNewNodes.Count;
                            if (failedInsertions.Count > 0)
                            {
                                msg += " (" + Properties.Resources.FlMsgFailedInsertions + ": " + failedInsertions.Count + ")";
                            }
                            BoardCommentBox.ShowFlashAnnouncement(msg, CommentBox.HintType.INFO, 14);

                            // Prepare info for Undo
                            List<int> nodeIds = new List<int>();
                            foreach (TreeNode nd in insertedNewNodes)
                            {
                                nodeIds.Add(nd.NodeId);
                            }
                            EditOperation op = new EditOperation(EditOperation.EditType.PASTE_LINES, insertAtNode.NodeId, nodeIds, null);
                            AppState.ActiveVariationTree.OpsManager.PushOperation(op);
                        }
                        else
                        {
                            if (failedInsertions.Count > 0)
                            {
                                string msg = Properties.Resources.ErrClipboardLinePaste + " ("
                                    + MoveUtils.BuildSingleMoveText(failedInsertions[0], true, false, AppState.ActiveVariationTree.MoveNumberOffset) + ")";
                                AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(msg, CommentBox.HintType.ERROR, 14);
                            }
                            else
                            {
                                AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.VariationAlreadyExists, CommentBox.HintType.ERROR, 14);
                            }
                        }

                        // we seem to be losing focus in this procedure, so we need to restore it
                        targetView.HostRtb.Focus();
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Invokes the dialog to select the engine lines to insert into the view.
        /// </summary>
        /// <param name="algebraicLines"></param>
        /// <param name="treeNodeLines"></param>
        /// <returns></returns>
        private bool UserLineSelection(List<string> algebraicLines, List<List<TreeNode>> treeNodeLines)
        {
            List<int> indicesToRemove = new List<int>();

            ObservableCollection<SelectableString> lineList = new ObservableCollection<SelectableString>();
            foreach (var item in algebraicLines)
            {
                lineList.Add(new SelectableString(item, false));
            }

            SelectEngineLinesDialog dlg = new SelectEngineLinesDialog(lineList);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

            if (dlg.ShowDialog() == true)
            {
                for (int i = 0; i < lineList.Count; i++)
                {
                    if (!lineList[i].IsSelected)
                    {
                        indicesToRemove.Add(i);
                    }
                }

                for (int i = indicesToRemove.Count - 1; i >= 0; i--)
                {
                    treeNodeLines.RemoveAt(indicesToRemove[i]);
                }

                return treeNodeLines.Count > 0;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Builds the lists of engine lines: 
        /// one as a list of TreeNodes that can be inserted into a view
        /// and the second as a list of string in the algebraic notation
        /// that can be shown to the user for selection.
        /// </summary>
        /// <param name="singleLine"></param>
        /// <param name="treeNodeLines"></param>
        /// <param name="algebraicLines"></param>
        private void BuildEngineLines(bool singleLine, out List<List<TreeNode>> treeNodeLines, out List<string> algebraicLines)
        {
            List<string> engineLines = GatherEngineLines();
            CreateLineLists(engineLines, singleLine, out treeNodeLines, out algebraicLines);
        }

        /// <summary>
        /// Gathers the current engine lines from EngineMessageProcessor.
        /// </summary>
        /// <returns></returns>
        private List<string> GatherEngineLines()
        {
            List<string> engineLines = new List<string>();
            try
            {
                TreeNode evalNode;

                // copy the lines under the lock
                lock (EngineMessageProcessor.MoveCandidatesLock)
                {
                    evalNode = EngineMessageProcessor.EngineMoveCandidates.EvalNode;
                    if (evalNode != null)
                    {
                        foreach (MoveEvaluation moveEval in EngineMessageProcessor.EngineMoveCandidates.Lines)
                        {
                            engineLines.Add(moveEval.Line);
                        }
                    }
                }
            }
            catch
            {
            }

            return engineLines;
        }

        /// <summary>
        /// Creates the lists of TreeNodes and algebraic lines from the engine lines.
        /// </summary>
        /// <param name="engineLines"></param>
        /// <param name="singleLine"></param>
        /// <param name="treeNodeLines"></param>
        /// <param name="algebraicLines"></param>
        private void CreateLineLists(List<string> engineLines, bool singleLine,
                                     out List<List<TreeNode>> treeNodeLines, out List<string> algebraicLines)
        {
            treeNodeLines = new List<List<TreeNode>>();
            algebraicLines = new List<string>();

            //build lists of Nodes for every engine line requested
            foreach (string line in engineLines)
            {
                TreeNode selectedNode = AppState.MainWin.ActiveTreeView.GetSelectedNode();
                treeNodeLines.Add(EngineLineParser.ParseEngineLine(AppState.ActiveVariationTree, selectedNode, line));
                if (singleLine)
                {
                    break;
                }
            }

            if (!singleLine)
            {
                CreateAlgebraicLinesList(treeNodeLines, ref algebraicLines);
            }
        }

        /// <summary>
        /// Creates the list of algebraic lines from the list of TreeNodes.
        /// </summary>
        /// <param name="treeNodeLines"></param>
        /// <param name="algebraicLines"></param>
        private void CreateAlgebraicLinesList(List<List<TreeNode>> treeNodeLines, ref List<string> algebraicLines)
        {
            //build corresponding lists in the algebraic notation
            foreach (List<TreeNode> nodeList in treeNodeLines)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < nodeList.Count; i++)
                {
                    TreeNode node = nodeList[i];
                    sb.Append(MoveUtils.BuildSingleMoveText(node, i == 0 || node.ColorToMove == PieceColor.Black, false, 0));
                    sb.Append(" ");
                }
                algebraicLines.Add(sb.ToString());
            }
        }

        /// <summary>
        /// Inserts the passed engine lines into the current view.
        /// </summary>
        /// <param name="treeNodeLines"></param>
        private TreeNode PasteEngineLines(VariationTree targetTree, TreeNode insertAtNode, List<List<TreeNode>> treeNodeLines
                                      , List<TreeNode> insertedNewNodes, List<TreeNode> failedInsertions)
        {
            TreeNode firstInserted = null;

            foreach (List<TreeNode> nodeList in treeNodeLines)
            {
                TreeNode first = PasteOneEngineLine(targetTree, insertAtNode, nodeList, insertedNewNodes, failedInsertions);
                if (firstInserted == null)
                {
                    firstInserted = first;
                }
            }

            return firstInserted;
        }

        /// <summary>
        /// Pastes one line into the current VariationTree.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="targetTree"></param>
        /// <param name="targetView"></param>
        /// <param name="insertionCount"></param>
        /// <param name="failureCount"></param>
        private TreeNode PasteOneEngineLine(VariationTree targetTree, TreeNode insertAtNode, List<TreeNode> line,
                                        List<TreeNode> insertedNewNodes, List<TreeNode> failedInsertions)
        {
            TreeNode lastSharedNode = TreeUtils.GetLastSharedNode(insertAtNode, line, out int index);
            if (lastSharedNode != null)
            {
                insertAtNode = lastSharedNode;
                for (int i = 0; i <= index; i++)
                {
                    line.RemoveAt(0);
                }
            }

            TreeNode firstInserted = TreeUtils.InsertSubtreeMovesIntoTree(AppState.ActiveVariationTree, insertAtNode, line, ref insertedNewNodes, ref failedInsertions);

            // if we inserted an already existing line, do nothing
            if (insertedNewNodes.Count > 0)
            {
                // build lines to prepare for the next insertion
                targetTree.BuildLines();

                //TreeNode insertedRoot = targetTree.GetNodeFromNodeId(firstInserted.NodeId);
                firstInserted.CommentBeforeMove = AppState.EngineName + ": ";
            }

            return firstInserted;
        }
    }
}

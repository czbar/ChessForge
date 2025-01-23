using ChessPosition;
using GameTree;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ChessForge
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// After a right click, prsent a dialog where the user can choose the lines
        /// to insert into the view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbEngineLines_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                BuildEngineLines(false, out List<List<TreeNode>> treeNodeLines, out List<string> algebraicLines);
                
                VariationTreeView targetView = AppState.MainWin.ActiveTreeView;
                PasteEngineLines(treeNodeLines, targetView);
                targetView.BuildFlowDocumentForVariationTree(false);
            }
            catch
            {
            }
        }

        /// <summary>
        /// After a double click, grab the first engine line and add it to the current view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbEngineLines_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                BuildEngineLines(true, out List<List<TreeNode>> treeNodeLines, out _);
                
                VariationTreeView targetView = AppState.MainWin.ActiveTreeView;
                PasteEngineLines(treeNodeLines, targetView);
                targetView.BuildFlowDocumentForVariationTree(false);
            }
            catch
            {
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
        private void PasteEngineLines(List<List<TreeNode>> treeNodeLines, VariationTreeView targetView)
        {
            VariationTree targetTree = AppState.MainWin.ActiveVariationTree;

            int insertedNodesCount = 0;
            int failedInsertionsCount = 0;

            foreach (List<TreeNode> nodeList in treeNodeLines)
            {
                PasteOneEngineLine(nodeList, targetTree, targetView, out int insertions, out int failures);
                insertedNodesCount += insertions;
                failedInsertionsCount += failures;
            }

            targetView.BuildFlowDocumentForVariationTree(false);
        }

        /// <summary>
        /// Pastes one line into the current VariationTree.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="targetTree"></param>
        /// <param name="targetView"></param>
        /// <param name="insertionCount"></param>
        /// <param name="failureCount"></param>
        private void PasteOneEngineLine(List<TreeNode> line, 
                                        VariationTree targetTree, VariationTreeView targetView,
                                        out int insertionCount, out int failureCount)
        {
            List<TreeNode> insertedNewNodes = new List<TreeNode>();
            List<TreeNode> failedInsertions = new List<TreeNode>();

            //TODO: replace with TreeUtils.InsertSubtree
            TreeNode firstInserted = targetView.InsertSubtree(line, ref insertedNewNodes, ref failedInsertions);

            insertionCount = insertedNewNodes.Count;
            failureCount = failedInsertions.Count;

            // if we inserted an already existing line, do nothing
            if (insertedNewNodes.Count > 0)
            {
                // build lines to prepare for the next insertion
                targetTree.BuildLines();
                
                TreeNode insertedRoot = targetTree.GetNodeFromNodeId(firstInserted.NodeId);
                insertedRoot.CommentBeforeMove = AppState.EngineName + ": ";
            }
        }

    }
}

using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Utilities for copying moves/lines/variations
    /// </summary>
    public class CopyPasteMoves
    {
        /// <summary>
        /// Pastes the passed list of nodes into the active tree.
        /// </summary>
        /// <param name="lstNodes"></param>
        public static void PasteVariation(List<TreeNode> lstNodes)
        {
            try
            {
                if (lstNodes != null && lstNodes.Count > 0 && AppState.IsVariationTreeTabType)
                {
                    VariationTree targetTree = AppState.MainWin.ActiveVariationTree;
                    VariationTreeView targetView = AppState.MainWin.ActiveTreeView;
                    List<TreeNode> insertedNewNodes = new List<TreeNode>();
                    List<TreeNode> failedInsertions = new List<TreeNode>();
                    TreeNode firstInserted = targetView.InsertSubtree(lstNodes, ref insertedNewNodes, ref failedInsertions);
                    if (failedInsertions.Count == 0)
                    {
                        // if we inserted an already existing line, do nothing
                        if (insertedNewNodes.Count > 0)
                        {
                            targetTree.BuildLines();
                            targetView.BuildFlowDocumentForVariationTree();
                            TreeNode insertedRoot = targetTree.GetNodeFromNodeId(firstInserted.NodeId);
                            AppState.MainWin.SetActiveLine(insertedRoot.LineId, insertedRoot.NodeId);
                            targetView.SelectNode(firstInserted.NodeId);
                        }
                    }
                    else
                    {
                        if (insertedNewNodes.Count > 0)
                        {
                            // remove inserted nodes after first removing the inserted root from the parent's children list.
                            insertedNewNodes[0].Parent.Children.Remove(insertedNewNodes[0]);
                            foreach (TreeNode node in insertedNewNodes)
                            {
                                targetTree.Nodes.Remove(node);
                            }
                        }

                        string msg = Properties.Resources.ErrClipboardLinePaste + " ("
                            + MoveUtils.BuildSingleMoveText(failedInsertions[0], true, false, targetTree.MoveNumberOffset) + ")";
                        MessageBox.Show(msg, Properties.Resources.ClipboardOperation, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("PasteVariation()", ex);
            }
        }
    }
}

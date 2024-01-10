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
        /// Checks the type of the clipboard content and undertakes an appropriate action.
        /// </summary>
        public static void PasteMoveList()
        {
            try
            {
                List<TreeNode> lstNodes = null;

                IDataObject dataObject = Clipboard.GetDataObject();
                if (dataObject != null && dataObject.GetDataPresent(DataFormats.Serializable))
                {
                    lstNodes = dataObject.GetData(DataFormats.Serializable) as List<TreeNode>;
                }
                else
                {
                    // do we have plain text?
                    string clipText = Clipboard.GetText();
                    if (!string.IsNullOrEmpty(clipText) && AppState.MainWin.ActiveTreeView != null)
                    {
                        TreeNode startNode = AppState.MainWin.ActiveTreeView.GetSelectedNode();
                        lstNodes = BuildNodeListFromText(startNode, clipText);
                    }
                }

                if (lstNodes != null)
                {
                    PasteVariation(lstNodes);
                    AppState.IsDirty = true;
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("PasteMoveList()", ex);
            }
        }

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
                        else
                        {
                            AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.VariationAlreadyExists, System.Windows.Media.Brushes.Red, 14);
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
                        AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(msg, System.Windows.Media.Brushes.Red, 14);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("PasteVariation()", ex);
            }
        }

        /// <summary>
        /// Builds a list of Nodes given text (e.g. from the clipboard)
        /// and a starting position.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static List<TreeNode> BuildNodeListFromText(TreeNode startPos, string clipText)
        {
            List<TreeNode> lstNodes  = new List<TreeNode>();
            PieceColor sideToMove = ColorToMoveFromAlgNotation(clipText);
            VariationTree tree = new VariationTree(GameData.ContentType.GENERIC);
            if (sideToMove != PieceColor.None && AppState.MainWin.ActiveTreeView != null)
            {
                try
                {
                    if (startPos != null && startPos.ColorToMove == sideToMove)
                    {
                        new PgnGameParser(clipText, tree, FenParser.GenerateFenFromPosition(startPos.Position), false);
                    }
                    else
                    {
                        new PgnGameParser(clipText, tree, FenParser.GenerateFenFromPosition(startPos.Parent.Position), false);
                    }
                }
                catch (Exception ex)
                {
                    if (ex is ParserException parserException)
                    {
                        AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(GuiUtilities.TranslateParseException(parserException), System.Windows.Media.Brushes.Red, 14);
                    }
                    else
                    {
                        AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.Error + ": " + ex.Message,
                            System.Windows.Media.Brushes.Red, 14);
                    }
                }

                lstNodes = tree.Nodes;
            }
            else
            {
                AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.InvalidPgn, System.Windows.Media.Brushes.Red, 14);
            }

            return lstNodes;
        }

        /// <summary>
        /// Based on the first token in text (assumed to contain game notation) 
        /// determine if this is white or black to move.
        /// </summary>
        /// <param name="move"></param>
        /// <returns></returns>
        public static PieceColor ColorToMoveFromAlgNotation(string text)
        {
            PieceColor pieceColor = PieceColor.None;

            if (!string.IsNullOrEmpty(text))
            {
                // the first move must start with number followed by a single dot (if this is a white move)
                // or multiple dots (of black)
                int dotPos = text.IndexOf('.');
                if (dotPos > 0 && text.Length > dotPos + 1)
                {
                    string sNum = text.Substring(0, dotPos).TrimStart();
                    if (uint.TryParse(sNum, out _))
                    {
                        pieceColor = text[dotPos + 1] == '.' ? PieceColor.Black : PieceColor.White;
                    }
                }
            }

            return pieceColor;
        }
    }
}

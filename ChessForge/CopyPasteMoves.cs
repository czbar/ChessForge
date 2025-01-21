using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

                IDataObject dataObject = SystemClipboard.GetDataObject();
                if (dataObject != null && dataObject.GetDataPresent(DataFormats.Serializable))
                {
                    lstNodes = dataObject.GetData(DataFormats.Serializable) as List<TreeNode>;
                }
                else
                {
                    // do we have plain text?
                    string clipText = SystemClipboard.GetText();
                    if (!string.IsNullOrEmpty(clipText))
                    {
                        lstNodes = ProcessClipboardText(clipText);
                    }
                }

                if (lstNodes != null)
                {
                    PasteVariation(lstNodes);
                    AppState.IsDirty = true;
                    MultiTextBoxManager.ShowEvaluationChart(true);
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
                            targetView.BuildFlowDocumentForVariationTree(false);
                            TreeNode insertedRoot = targetTree.GetNodeFromNodeId(firstInserted.NodeId);
                            AppState.MainWin.SetActiveLine(insertedRoot.LineId, insertedRoot.NodeId);
                            targetView.SelectNode(targetView.HostRtb.Document, firstInserted.NodeId);

                            string msg = Properties.Resources.FlMsgPastedMovesCount + ": " + insertedNewNodes.Count;
                            AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(msg, CommentBox.HintType.INFO, 14);
                        }
                        else
                        {
                            AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.VariationAlreadyExists, CommentBox.HintType.ERROR, 14);
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
                        AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(msg, CommentBox.HintType.ERROR, 14);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("PasteVariation()", ex);
            }
        }

        /// <summary>
        /// Processes the clipoards text looking for a list of moves
        /// in algebraic notation or a PGN content.
        /// </summary>
        /// <param name="clipText"></param>
        /// <returns></returns>
        private static List<TreeNode> ProcessClipboardText(string clipText)
        {
            List<TreeNode> lstNodes = null;

            PieceColor sideToMove = ColorToMoveFromAlgNotation(clipText);

            // if sideToMove is not NONE it means that the first token may be a move and this is not PGN.
            if (sideToMove != PieceColor.None)
            {
                lstNodes = ProcessPlainTextFromClipboard(clipText, sideToMove);
                if (AppState.MainWin.ActiveTreeView != null)
                {
                    TreeNode startNode = AppState.MainWin.ActiveTreeView.GetSelectedNode();
                    lstNodes = BuildNodeListFromText(startNode, clipText, sideToMove);
                }
            }
            else
            {
                // this could be PGN
                if (AppState.ActiveChapter != null)
                {
                    lstNodes = null;
                    ProcessPgnFromClipboard(clipText);
                }
            }

            return lstNodes;
        }

        /// <summary>
        /// Processed the passed string assuming it is an algebraic notations
        /// of moves.
        /// </summary>
        /// <param name="clipText"></param>
        /// <param name="sideToMove"></param>
        /// <returns></returns>
        private static List<TreeNode> ProcessPlainTextFromClipboard(string clipText, PieceColor sideToMove)
        {
            List<TreeNode> lstNodes = null;

            if (AppState.MainWin.ActiveTreeView != null)
            {
                TreeNode startNode = AppState.MainWin.ActiveTreeView.GetSelectedNode();
                lstNodes = BuildNodeListFromText(startNode, clipText, sideToMove);
            }

            return lstNodes;
        }

        /// <summary>
        /// Builds a list of Nodes given text (e.g. from the clipboard)
        /// and a starting position.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static List<TreeNode> BuildNodeListFromText(TreeNode startPos, string clipText, PieceColor sideToMove)
        {
            List<TreeNode> lstNodes = new List<TreeNode>();
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
                        AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(GuiUtilities.TranslateParseException(parserException), CommentBox.HintType.ERROR, 14);
                    }
                    else
                    {
                        AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.Error + ": " + ex.Message,
                            CommentBox.HintType.ERROR, 14);
                    }
                }

                lstNodes = tree.Nodes;
            }
            else
            {
                AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.InvalidPgn, CommentBox.HintType.ERROR, 14);
            }

            return lstNodes;
        }

        /// <summary>
        /// Based on the first token in text (assumed to contain game notation) 
        /// determine if this is white or black to move.
        /// </summary>
        /// <param name="move"></param>
        /// <returns></returns>
        private static PieceColor ColorToMoveFromAlgNotation(string text)
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

        /// <summary>
        /// Process PGN from the clipboard if found.
        /// </summary>
        /// <param name="clipText"></param>
        private static void ProcessPgnFromClipboard(string clipText)
        {
            bool addedChapters;
            bool cancelled;

            // if this is a legitimate PGN, try to process it 
            int articleCount = PgnArticleUtils.PasteArticlesFromPgn(clipText, out addedChapters, out cancelled);
            if (articleCount == 0)
            {
                AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.InvalidPgn, CommentBox.HintType.ERROR, 14);
            }
            else
            {
                // there was some valid content added but check if the user cancelled the proceedings
                if (!cancelled)
                {
                    AppState.MainWin.ChaptersView.IsDirty = true;
                    if (addedChapters)
                    {
                        GuiUtilities.RefreshChaptersView(null);
                        AppState.MainWin.UiTabChapters.Focus();
                        PulseManager.ChapterIndexToBringIntoView = AppState.Workbook.Chapters.Count - 1;
                    }
                    else
                    {
                        if (AppState.ActiveTab == TabViewType.CHAPTERS)
                        {
                            GuiUtilities.RefreshChaptersView(AppState.ActiveChapter);
                        }
                        else
                        {
                            AppState.MainWin.ChaptersView.IsDirty = true;
                        }
                    }
                }
            }
        }


    }
}

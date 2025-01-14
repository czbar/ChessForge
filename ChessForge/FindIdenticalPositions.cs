using ChessForge;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Text;
using ChessPosition;
using System.Windows.Input;
using System.Windows.Shapes;

namespace ChessForge
{
    /// <summary>
    /// Utilities for handling Finding Identical Positions
    /// </summary>
    public class FindIdenticalPositions
    {
        /// <summary>
        /// Search mode.
        /// </summary>
        public enum Mode
        {
            FIND_AND_REPORT,
            CHECK_IF_ANY,
        }

        /// <summary>
        /// Finds positions identical to the one in the current node.
        /// Returns true if any such position found.
        /// If mode is set to CHECK_IF_ANY, this is all it does,
        /// otherwise it pops up an appropriate message or dialog for the user.
        /// </summary>
        /// <param name="searchNode"></param>
        /// <param name="mode"></param>
        /// <param name="externalSearch">whether this was invoked from the view that is not subject to search e.g. INTRO</param>
        /// <returns></returns>
        public static bool Search(bool editableSearch, TreeNode searchNode, Mode mode, bool externalSearch, bool reportNoFind, out bool searchAgain)
        {
            searchAgain = false;

            bool anyFound = false;
            ObservableCollection<ArticleListItem> lstIdenticalPositions;
            try
            {
                Mouse.SetCursor(Cursors.Wait);
                lstIdenticalPositions = ArticleListBuilder.BuildIdenticalPositionsList(searchNode, mode == Mode.CHECK_IF_ANY, false, !externalSearch);

                anyFound = lstIdenticalPositions.Count > 0;

                if (mode == Mode.FIND_AND_REPORT)
                {
                    // if externalSearch, a single match is good (as it is not in the invoking view)
                    if (!ChapterUtils.HasAtLeastNArticles(lstIdenticalPositions, externalSearch ? 1 : 2 ))
                    {
                        if (reportNoFind)
                        {
                            // we only have 1 result which is the current position
                            MessageBox.Show(Properties.Resources.MsgNoIdenticalPositions, Properties.Resources.ChessForge, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        FoundArticlesDialog dlgEx = new FoundArticlesDialog(searchNode,
                                                            FoundArticlesDialog.Mode.IDENTICAL_ARTICLES,
                                                            ref lstIdenticalPositions,
                                                            editableSearch);
                        GuiUtilities.PositionDialog(dlgEx, AppState.MainWin, 100);

                        if (dlgEx.ShowDialog() == true)
                        {
                            if (dlgEx.Request == FoundArticlesDialog.Action.CopyOrMoveArticles)
                            {
                                ChapterUtils.RequestCopyMoveArticles(searchNode, false, lstIdenticalPositions, ArticlesAction.COPY_OR_MOVE, true);
                            }
                            else if (dlgEx.ArticleIndexId >= 0 && dlgEx.ArticleIndexId < lstIdenticalPositions.Count)
                            {
                                ProcessSelectedPosition(lstIdenticalPositions, dlgEx.Request, dlgEx.ArticleIndexId);
                            }
                        }
                        else
                        {
                            searchAgain = dlgEx.Request == FoundArticlesDialog.Action.SearchAgain;
                        }
                    }
                }
            }
            finally
            {
                Mouse.SetCursor(Cursors.Arrow);
            }

            return anyFound;
        }

        /// <summary>
        /// Processes a response from the dialog, where the user requested
        /// action on a specific item
        /// </summary>
        /// <param name="lstIdenticalPositions"></param>
        /// <param name="request"></param>
        /// <param name="articleIndexId"></param>
        private static void ProcessSelectedPosition(ObservableCollection<ArticleListItem> lstIdenticalPositions, FoundArticlesDialog.Action request, int articleIndexId)
        {
            ArticleListItem item = lstIdenticalPositions[articleIndexId];
            uint moveNumberOffset = 0;
            if (item.Article != null && item.Article.Tree != null)
            {
                moveNumberOffset = item.Article.Tree.MoveNumberOffset;
            }
            List<TreeNode> nodelList = null;
            switch (request)
            {
                case FoundArticlesDialog.Action.CopyLine:
                    nodelList = TreeUtils.CopyNodeList(item.TailLine);
                    CopyPasteMoves.PasteVariation(nodelList);
                    AppState.IsDirty = true;
                    break;
                case FoundArticlesDialog.Action.CopyTree:
                    foreach (TreeNode node in item.TailLine[0].Parent.Children)
                    {
                        nodelList = TreeUtils.CopySubtree(node);
                        CopyPasteMoves.PasteVariation(nodelList);
                    }
                    AppState.IsDirty = true;
                    break;
                case FoundArticlesDialog.Action.OpenView:
                    WorkbookLocationNavigator.GotoArticle(item.ChapterIndex, item.Article.Tree.ContentType, item.ArticleIndex);
                    if (item.Article.Tree.ContentType == GameData.ContentType.STUDY_TREE)
                    {
                        AppState.MainWin.SetupGuiForActiveStudyTree(true);
                    }
                    AppState.MainWin.SetActiveLine(item.Node.LineId, item.Node.NodeId);
                    if (AppState.MainWin.ActiveTreeView is StudyTreeView view)
                    {
                        if (view.UncollapseMove(item.Node))
                        {
                            view.BuildFlowDocumentForVariationTree(false);
                        }
                    }
                    AppState.MainWin.SelectLineAndMoveInWorkbookViews(AppState.MainWin.ActiveTreeView, item.Node.LineId,
                        AppState.MainWin.ActiveLine.GetSelectedPlyNodeIndex(false), true);
                    break;
            }
        }

    }
}
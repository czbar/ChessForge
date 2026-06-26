using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

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
            // Finds positions identical to the passed one.
            IDENTICAL,

            // Finds postions fully matching the specified position.
            POSITION_MATCH,
        }

        /// <summary>
        /// Timer tick handler for the timer that is used to delay search 
        /// for the last searched position until the workbook is fully loaded and ready.
        /// The timer is started when a workbook is being opened due to a user selecting it
        /// in a multi-file search. We wait for the workbook to be fully loaded
        /// before performing the search for the last searched position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void OpenPositionSearchTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (WorkbookManager.SessionWorkbook == null)
                {
                    AppState.MainWin.Timers.OpenPositionSearchTimer.Stop();
                }
                else if (WorkbookManager.SessionWorkbook.IsReady)
                {
                    AppState.MainWin.Timers.OpenPositionSearchTimer.Stop();
                    SearchLastSearchedPosition();
                }
            }
            catch 
            {
                AppState.MainWin.Timers.OpenPositionSearchTimer.Stop();
            }
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
        public static bool Search(bool editableSearch, SearchPositionCriteria crits, out bool searchAgain)
        {
            searchAgain = false;

            bool anyFound = false;
            ObservableCollection<ArticleListItem> lstIdenticalPositions;
            try
            {
                Mouse.SetCursor(Cursors.Wait);
                lstIdenticalPositions = ArticleListBuilder.BuildIdenticalPositionsList(crits);

                // the list may contain items for both chapters and articles
                // so we need to call the special method rather than simply checking size. 
                anyFound = ChapterUtils.HasAtLeastNArticles(lstIdenticalPositions, 1);

                if (crits.FindMode == Mode.IDENTICAL)
                {
                    if (anyFound)
                    {
                        ShowFoundPositions(crits.SearchNode, lstIdenticalPositions, editableSearch, out searchAgain);
                    }
                    else
                    {
                        if (crits.ReportNoFind)
                        {
                            MessageBox.Show(Properties.Resources.MsgNoIdenticalPositions, Properties.Resources.ChessForge, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                else if (crits.FindMode == Mode.POSITION_MATCH)
                {
                    if (anyFound)
                    {
                        ShowFoundPositions(crits.SearchNode, lstIdenticalPositions, editableSearch, out searchAgain);
                    }
                    else
                    {
                        if (crits.ReportNoFind)
                        {
                            if (MessageBox.Show(Properties.Resources.MsgEditPositionSearch, Properties.Resources.MsgTitlePositionSearch, MessageBoxButton.YesNoCancel
                                          , MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                searchAgain = true;
                            }
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
        /// Searches for the last searched position.
        /// This method is invoked started when a workbook is being opened due to a user selecting it
        /// in a multi-file search. 
        /// (We use a time to wait for the workbook to be fully loaded before calling this method, see OpenPositionSearchTimer_Tick).
        /// </summary>
        /// <returns></returns>
        public static bool SearchLastSearchedPosition()
        {
            TreeNode searchNode = new TreeNode();
            searchNode.Position = new BoardPosition(SearchPosition.LastSearchPosition);
            
            SearchPositionCriteria crits = new SearchPositionCriteria(searchNode);
            crits.FindMode = FindIdenticalPositions.Mode.POSITION_MATCH;
            crits.IsPartialSearch = Configuration.PartialSearch;
            crits.SetCheckDynamicAttrs(false);
            crits.ExcludeCurrentNode = false;
            crits.ReportNoFind = false;

            // check whether we search in the current files or in multiple files
            return Search(false, crits, out _);
        }

        /// <summary>
        /// Shows the found positions in a dialog.
        /// </summary>
        /// <param name="searchNode"></param>
        /// <param name="lstIdenticalPositions"></param>
        /// <param name="editableSearch"></param>
        /// <param name="searchAgain"></param>
        private static void ShowFoundPositions(TreeNode searchNode, ObservableCollection<ArticleListItem> lstIdenticalPositions, bool editableSearch, out bool searchAgain)
        {
            searchAgain = false;

            FoundArticlesDialog dlgEx = new FoundArticlesDialog(searchNode,
                                                FoundArticlesDialog.Mode.IDENTICAL_ARTICLES,
                                                ref lstIdenticalPositions,
                                                editableSearch);
            GuiUtilities.PositionDialog(dlgEx, AppState.MainWin, 100);

            if (dlgEx.ShowDialog() == true)
            {
                if (dlgEx.Request == FoundArticlesDialog.Action.CopyOrMoveArticles)
                {
                    ChapterUtils.RequestCopyMoveArticles(searchNode, false, lstIdenticalPositions, ArticlesAction.COPY_OR_MOVE_FOUND_POSITIONS, true);
                }
                else if (dlgEx.ArticleIndexId >= 0 && dlgEx.ArticleIndexId < lstIdenticalPositions.Count)
                {
                    ProcessSelectedPosition(searchNode, lstIdenticalPositions, dlgEx.Request, dlgEx.ArticleIndexId);
                }
            }
            else
            {
                searchAgain = dlgEx.Request == FoundArticlesDialog.Action.SearchAgain;
            }
        }

        /// <summary>
        /// Processes a response from the dialog, where the user requested
        /// action on a specific item
        /// </summary>
        /// <param name="lstIdenticalPositions"></param>
        /// <param name="request"></param>
        /// <param name="articleIndexId"></param>
        private static void ProcessSelectedPosition(TreeNode searchNode, ObservableCollection<ArticleListItem> lstIdenticalPositions, FoundArticlesDialog.Action request, int articleIndexId)
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
                    if (item.Article != null && nodelList != null && nodelList.Count > 0)
                    {
                        string guid = item.Article.Guid;
                        ReferenceUtils.AddReferenceToNode(nodelList.Last(), guid);
                    }
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
                    VariationTreeViewUtils.SetSelectionsForNode(item.Article.Tree.ContentType, item.Node);
                    break;
            }
        }

    }
}
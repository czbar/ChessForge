using ChessForge;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;
using System.Text;
using ChessPosition;
using System.Windows.Input;
using System.Text.RegularExpressions;

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
        /// <returns></returns>
        public static bool Search(TreeNode searchNode, Mode mode)
        {
            bool anyFound = false;
            ObservableCollection<ArticleListItem> lstIdenticalPositions;
            try
            {
                Mouse.SetCursor(Cursors.Wait);
                lstIdenticalPositions = ArticleListBuilder.BuildIdenticalPositionsList(searchNode, mode == Mode.CHECK_IF_ANY, false);

                anyFound = lstIdenticalPositions.Count > 0;

                if (mode == Mode.FIND_AND_REPORT)
                {
                    if (!HasAtLeastNArticles(lstIdenticalPositions, 2))
                    {
                        // we only have 1 result which is the current position
                        MessageBox.Show(Properties.Resources.MsgNoIdenticalPositions, Properties.Resources.ChessForge, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        IdenticalPositionsExDialog dlgEx = new IdenticalPositionsExDialog(searchNode, ref lstIdenticalPositions)
                        {
                            Left = AppState.MainWin.ChessForgeMain.Left + 100,
                            Top = AppState.MainWin.ChessForgeMain.Top + 100,
                            Topmost = false,
                            Owner = AppState.MainWin
                        };

                        if (dlgEx.ShowDialog() == true)
                        {
                            if (dlgEx.Request == IdenticalPositionsExDialog.Action.CopyArticles
                                || dlgEx.Request == IdenticalPositionsExDialog.Action.MoveArticles)
                            {
                                ProcessCopyMoveArticlesRequest(searchNode, lstIdenticalPositions, dlgEx.Request);
                            }
                            else if (dlgEx.ArticleIndexId >= 0 && dlgEx.ArticleIndexId < lstIdenticalPositions.Count)
                            {
                                ProcessSelectedPosition(lstIdenticalPositions, dlgEx.Request, dlgEx.ArticleIndexId);
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
        /// Processes a response from the dialog, where the user requested to copy/move articles
        /// </summary>
        /// <param name="lstIdenticalPositions"></param>
        /// <param name="request"></param>
        private static void ProcessCopyMoveArticlesRequest(TreeNode searchNode, ObservableCollection<ArticleListItem> lstIdenticalPositions, IdenticalPositionsExDialog.Action request)
        {
            // make a list with only games and exercises
            RemoveStudies(lstIdenticalPositions);
            string title = request == IdenticalPositionsExDialog.Action.MoveArticles ?
                           Properties.Resources.SelectItemsToMove : Properties.Resources.SelectItemsToCopy;
            SelectArticlesDialog dlg = new SelectArticlesDialog(null, title, ref lstIdenticalPositions, true)
            {
                Left = AppState.MainWin.ChessForgeMain.Left + 100,
                Top = AppState.MainWin.ChessForgeMain.Top + 100,
                Topmost = false,
                Owner = AppState.MainWin
            };

            if (dlg.ShowDialog() == true)
            {
                RemoveUnselectedArticles(lstIdenticalPositions);

                if (HasAtLeastNArticles(lstIdenticalPositions, 1))
                {
                    int index = AppState.MainWin.InvokeSelectSingleChapterDialog(out bool newChapter);
                    if (index >= 0)
                    {
                        RemoveChapters(lstIdenticalPositions);
                        bool emptyEntryList = lstIdenticalPositions.Count == 0;

                        Chapter targetChapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(index);
                        if (newChapter)
                        {
                            SetChapterTitle(searchNode, targetChapter);
                            List<TreeNode> stem = TreeUtils.GetStemLine(searchNode, true);
                            stem[stem.Count - 1].IsThumbnail = true;
                            targetChapter.StudyTree.Tree.Nodes = TreeUtils.CopyNodeList(stem);

                        }
                        if (targetChapter != null)
                        {
                            List<ArticleListItem> articlesToInsert = CreateListToMoveOrCopy(lstIdenticalPositions, request, targetChapter);
                            if (articlesToInsert.Count > 0)
                            {
                                // place the articles in the target chapter
                                foreach (ArticleListItem ali in articlesToInsert)
                                {
                                    if (ali.Article != null)
                                    {
                                        if (ali.ContentType == GameData.ContentType.MODEL_GAME)
                                        {
                                            targetChapter.AddModelGame(ali.Article);
                                        }
                                        else if (ali.ContentType == GameData.ContentType.EXERCISE)
                                        {
                                            targetChapter.AddExercise(ali.Article);
                                        }
                                    }
                                }

                                // remove from the articles from their source chapters,
                                // note that we could be removing the Active article so the GUI must be handled
                                // accordingly
                                if (request == IdenticalPositionsExDialog.Action.MoveArticles)
                                {
                                    foreach (ArticleListItem ali in articlesToInsert)
                                    {
                                        Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(ali.ChapterIndex);
                                        chapter?.DeleteArticle(ali.Article);
                                    }
                                }

                                targetChapter.IsViewExpanded = true;
                                targetChapter.IsModelGamesListExpanded = true;
                                targetChapter.IsExercisesListExpanded = true;

                                AppState.MainWin.ChaptersView.IsDirty = true;

                                AppState.MainWin.UiTabChapters.Focus();
                                PulseManager.ChaperIndexToBringIntoView = targetChapter.Index;
                            }
                            else
                            {
                                if (!emptyEntryList)
                                {
                                    AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.ItemsAlreadyInChapter);
                                }
                            }
                        }
                        else
                        {
                            AppLog.Message("Unexpected error in ProcessCopyMoveArticlesRequest() - target chapter is null");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the chapter's title based on the passed move ECO code
        /// and the move notation.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="chapter"></param>
        private static void SetChapterTitle(TreeNode nd, Chapter chapter)
        {
            if (nd != null && chapter != null)
            {
                string title = "";
                if (nd.Eco != null)
                {
                    title = nd.Eco + ": ";
                }
                title += MoveUtils.BuildSingleMoveText(nd, true, true, 0);
                chapter.SetTitle(title);
            }
        }

        /// <summary>
        /// Prepares a list of articles that will be inserted in the target chapter.
        /// In case of Copy, we need copies of the articles and the originals will not be touched.
        /// In case of Move, the articles will be removed from the source chapter.
        /// </summary>
        /// <param name="sourceArticles"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private static List<ArticleListItem> CreateListToMoveOrCopy(ObservableCollection<ArticleListItem> sourceArticles
            , IdenticalPositionsExDialog.Action request
            , Chapter targetChapter)
        {
            List<ArticleListItem> articlesToInsert = new List<ArticleListItem>();
            int targetChapterIndex = targetChapter.Index;

            if (request == IdenticalPositionsExDialog.Action.CopyArticles)
            {
                // make copies and give them new GUIDs
                foreach (ArticleListItem ali in sourceArticles)
                {
                    // defensive check, it should never be null here
                    if (ali.Article != null)
                    {
                        Article newArticle = ali.Article.CloneMe();
                        newArticle.Tree.Header.SetNewTreeGuid();
                        ArticleListItem article = new ArticleListItem(ali.Chapter, ali.ChapterIndex, newArticle, ali.ArticleIndex);
                        // if target chapter is the same as source, do not add it. 
                        if (ali.ChapterIndex != targetChapterIndex)
                        {
                            articlesToInsert.Add(article);
                        }
                    }
                }
            }
            else
            {
                // we are moving so store just the references, not copies
                foreach (ArticleListItem ali in sourceArticles)
                {
                    ArticleListItem article = new ArticleListItem(ali.Chapter, ali.ChapterIndex, ali.Article, ali.ArticleIndex);
                    // if target chapter is the same as source, do not add it. 
                    if (ali.ChapterIndex != targetChapterIndex)
                    {
                        articlesToInsert.Add(article);
                    }
                }
            }
            return articlesToInsert;
        }

        /// <summary>
        /// Processes a response from the dialog, where the user requested
        /// action on a specific item
        /// </summary>
        /// <param name="lstIdenticalPositions"></param>
        /// <param name="request"></param>
        /// <param name="articleIndexId"></param>
        private static void ProcessSelectedPosition(ObservableCollection<ArticleListItem> lstIdenticalPositions, IdenticalPositionsExDialog.Action request, int articleIndexId)
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
                case IdenticalPositionsExDialog.Action.CopyLine:
                    nodelList = TreeUtils.CopyNodeList(item.TailLine);
                    ChfClipboard.HoldNodeList(nodelList, moveNumberOffset);
                    AppState.MainWin.PasteChfClipboard();
                    AppState.IsDirty = true;
                    break;
                case IdenticalPositionsExDialog.Action.CopyTree:
                    foreach (TreeNode node in item.TailLine[0].Parent.Children)
                    {
                        nodelList = TreeUtils.CopySubtree(node);
                        ChfClipboard.HoldNodeList(nodelList, moveNumberOffset);
                        AppState.MainWin.PasteChfClipboard();
                    }
                    AppState.IsDirty = true;
                    break;
                case IdenticalPositionsExDialog.Action.OpenView:
                    WorkbookLocationNavigator.GotoArticle(item.ChapterIndex, item.Article.Tree.ContentType, item.ArticleIndex);
                    if (item.Article.Tree.ContentType == GameData.ContentType.STUDY_TREE)
                    {
                        AppState.MainWin.SetupGuiForActiveStudyTree(true);
                    }
                    AppState.MainWin.SetActiveLine(item.Node.LineId, item.Node.NodeId);
                    AppState.MainWin.ActiveTreeView.SelectLineAndMove(item.Node.LineId, item.Node.NodeId);
                    break;
            }
        }

        /// <summary>
        /// Removes all Articles that are not selected.
        /// </summary>
        /// <param name="lstIdenticalPositions"></param>
        private static void RemoveUnselectedArticles(ObservableCollection<ArticleListItem> lstIdenticalPositions)
        {
            List<ArticleListItem> itemsToRemove = new List<ArticleListItem>();

            foreach (ArticleListItem item in lstIdenticalPositions)
            {
                if (item.Article != null && !item.IsSelected)
                {
                    itemsToRemove.Add(item);
                }
            }

            foreach (ArticleListItem item in itemsToRemove)
            {
                lstIdenticalPositions.Remove(item);
            }
        }

        /// <summary>
        /// Removes lines for studies and chapters that only contain studies.
        /// This is for display in the list of articles to select.
        /// </summary>
        /// <param name="lstIdenticalPositions"></param>
        private static void RemoveStudies(ObservableCollection<ArticleListItem> lstIdenticalPositions)
        {
            List<ArticleListItem> itemsToRemove = new List<ArticleListItem>();

            ArticleListItem lastChapterItem = null;
            foreach (ArticleListItem item in lstIdenticalPositions)
            {
                if (item.Article == null)
                {
                    if (lastChapterItem != null)
                    {
                        itemsToRemove.Add(lastChapterItem);
                    }
                    lastChapterItem = item;
                }
                else if (item.ContentType == GameData.ContentType.STUDY_TREE)
                {
                    itemsToRemove.Add(item);
                }
                else
                {
                    lastChapterItem = null;
                }
            }

            if (lastChapterItem != null)
            {
                itemsToRemove.Add(lastChapterItem);
            }

            foreach (ArticleListItem item in itemsToRemove)
            {
                lstIdenticalPositions.Remove(item);
            }
        }

        /// <summary>
        /// Removes lines for chapters.
        /// This is for copying/moving articles.
        /// </summary>
        /// <param name="lstIdenticalPositions"></param>
        private static void RemoveChapters(ObservableCollection<ArticleListItem> lstIdenticalPositions)
        {
            List<ArticleListItem> itemsToRemove = new List<ArticleListItem>();

            foreach (ArticleListItem item in lstIdenticalPositions)
            {
                if (item.Article == null)
                {
                    itemsToRemove.Add(item);
                }
            }

            foreach (ArticleListItem item in itemsToRemove)
            {
                lstIdenticalPositions.Remove(item);
            }
        }

        /// <summary>
        /// Checks if we have at least 2 articles i.e. 1 in addition to the currently viewed.
        /// We have to check the type of the item so that we don't count chapter items.
        /// </summary>
        /// <param name="lstPos"></param>
        /// <returns></returns>
        private static bool HasAtLeastNArticles(ObservableCollection<ArticleListItem> lstPos, int n)
        {
            int count = 0;
            foreach (ArticleListItem item in lstPos)
            {
                if (item.ContentType == GameData.ContentType.STUDY_TREE
                    || item.ContentType == GameData.ContentType.MODEL_GAME
                    || item.ContentType == GameData.ContentType.EXERCISE)
                {
                    count++;
                    if (count >= n)
                    {
                        break;
                    }
                }
            }

            return count >= 2;
        }

    }
}
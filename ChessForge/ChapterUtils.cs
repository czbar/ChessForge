using ChessForge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameTree;
using ChessPosition;
using System.Collections.ObjectModel;
using System.Windows;
using ChessPosition.GameTree;

namespace ChessForge
{
    /// <summary>
    /// Utilities to manipulate chapter objects.
    /// </summary>
    public class ChapterUtils
    {
        /// <summary>
        /// Invokes dialog for managing some aspects of the active chapter.
        /// </summary>
        /// <param name="chapter"></param>
        public static void ManageChapter(Chapter chapter)
        {
            if (chapter != null)
            {
                ManageChapterDialog dlg = new ManageChapterDialog();
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        if (dlg.CallSplitChapterDialog)
                        {
                            SplitChapterUtils.InvokeSplitChapterDialog(chapter);
                        }
                        else
                        {
                            bool regenerateStudy = dlg.RegenerateStudy;
                            bool sortGames = dlg.SortGamesBy != GameSortCriterion.SortItem.NONE && chapter.ModelGames.Count > 1;
                            if (dlg.RegenerateStudy)
                            {
                                RegenerateStudy(chapter);
                                if (!sortGames)
                                {
                                    AppState.MainWin.SetupGuiForActiveStudyTree(true);
                                }
                            }

                            if (sortGames)
                            {
                                SortGames(chapter, dlg.SortGamesBy, dlg.SortGamesDirection);

                                GuiUtilities.RefreshChaptersView(null);
                                AppState.MainWin.UiTabChapters.Focus();
                                PulseManager.ChaperIndexToBringIntoView = chapter.Index;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLog.Message("ManageChapter()", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Processes a response from the dialog, where the user requested to copy/move articles
        /// </summary>
        /// <param name="lstArticleItems"></param>
        /// <param name="request"></param>
        public static void RequestCopyMoveArticles(TreeNode searchNode,
                                bool allChaptersCheckbox,
                                ObservableCollection<ArticleListItem> lstArticleItems,
                                ArticlesAction action,
                                bool showAllChapters)
        {
            // make a list with only games and exercises
            RemoveStudies(lstArticleItems);

            string title = "";
            switch (action)
            {
                case ArticlesAction.COPY:
                    title = Properties.Resources.SelectItemsToCopy;
                    break;
                case ArticlesAction.MOVE:
                    title = Properties.Resources.SelectItemsToMove;
                    break;
                case ArticlesAction.COPY_OR_MOVE:
                    title = Properties.Resources.SelectItemsToCopyOrMove;
                    break;
            }

            SelectArticlesDialog dlg = new SelectArticlesDialog(null, allChaptersCheckbox, title, ref lstArticleItems, showAllChapters, action);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

            if (action == ArticlesAction.COPY_OR_MOVE)
            {
                dlg.SetupGuiForGamesCopyOrMove();
            }

            if (dlg.ShowDialog() == true)
            {
                action = dlg.ActionOnArticles;
                RemoveUnselectedArticles(lstArticleItems);

                if (HasAtLeastNArticles(lstArticleItems, 1))
                {
                    ProcessCopyOrMoveArticles(searchNode, lstArticleItems, action);
                }
            }
        }

        /// <summary>
        /// Performs copy/move operations. It can be invoked from the FindIdenticalPositions context
        /// or via the menu item handlers. 
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="lstArticles"></param>
        /// <param name="action"></param>
        public static void ProcessCopyOrMoveArticles(TreeNode startNode, ObservableCollection<ArticleListItem> lstArticles, ArticlesAction action)
        {
            int index = InvokeSelectSingleChapterDialog(out bool newChapter);
            if (index >= 0)
            {
                RemoveChapters(lstArticles);
                bool emptyEntryList = lstArticles.Count == 0;

                Chapter targetChapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(index);
                if (newChapter && startNode != null)
                {
                    SetChapterTitle(startNode, targetChapter);
                    List<TreeNode> stem = TreeUtils.GetStemLine(startNode, true);
                    targetChapter.StudyTree.Tree.Nodes = TreeUtils.CopyNodeList(stem);
                    targetChapter.StudyTree.Tree.Nodes[stem.Count - 1].IsThumbnail = true;
                }

                if (targetChapter != null)
                {
                    List<ArticleListItem> articlesToInsert = CreateListToMoveOrCopy(lstArticles, action, targetChapter);
                    if (articlesToInsert.Count > 0)
                    {
                        GameData.ContentType contentType = CopyOrMoveArticles(targetChapter, articlesToInsert, action);

                        targetChapter.IsViewExpanded = true;
                        targetChapter.IsModelGamesListExpanded = true;
                        targetChapter.IsExercisesListExpanded = true;

                        AppState.IsDirty = true;

                        UpdateViewAfterCopyMoveArticles(targetChapter, action, contentType);
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

        /// <summary>
        /// After articles have been copied or moved there may be a need to refresh
        /// the current view, depending on what that view is and what operation
        /// was performed.
        /// </summary>
        public static void UpdateViewAfterCopyMoveArticles(Chapter targetChapter, ArticlesAction action, GameData.ContentType contentType)
        {
            try
            {
                AppState.MainWin.ChaptersView.IsDirty = true;
                Chapter chapter = AppState.Workbook.ActiveChapter;

                // if we are in the Chapters view, we want to refresh the view
                // and bring the target chapter into view.
                if (AppState.ActiveTab == TabViewType.CHAPTERS)
                {
                    GuiUtilities.RefreshChaptersView(null);
                    AppState.MainWin.UiTabChapters.Focus();
                    PulseManager.ChaperIndexToBringIntoView = targetChapter.Index;
                }
                else if (action == ArticlesAction.COPY)
                {
                    // if the action was COPY and we are not in the chapters view,
                    // at most, it may be necessary to update the prev/next bar
                    PreviousNextViewBars.BuildPreviousNextBar(GameData.ContentType.MODEL_GAME);
                    PreviousNextViewBars.BuildPreviousNextBar(GameData.ContentType.EXERCISE);
                }
                else if (action == ArticlesAction.MOVE)
                {
                    if (contentType == GameData.ContentType.MODEL_GAME || contentType == GameData.ContentType.GENERIC)
                    {
                        UpdateModelGamesView(chapter);
                    }
                    if (contentType == GameData.ContentType.EXERCISE || contentType == GameData.ContentType.GENERIC)
                    {
                        UpdateExercisesView(chapter);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UpdateViewAfterCopyMoveArticles", ex);
            }
        }

        /// <summary>
        /// Updates the Model Games View when the game list has changed
        /// </summary>
        /// <param name="chapter"></param>
        public static void UpdateModelGamesView(Chapter chapter)
        {
            PreviousNextViewBars.BuildPreviousNextBar(GameData.ContentType.MODEL_GAME);
            int updatedIndex = chapter.VerifyGameIndex(chapter.ActiveModelGameIndex);
            chapter.ActiveModelGameIndex = updatedIndex;
            AppState.MainWin.SelectModelGame(chapter.ActiveModelGameIndex, AppState.ActiveTab == TabViewType.MODEL_GAME);
        }

        /// <summary>
        /// Updates the Exercises View when the exercise list has changed
        /// </summary>
        /// <param name="chapter"></param>
        public static void UpdateExercisesView(Chapter chapter)
        {
            PreviousNextViewBars.BuildPreviousNextBar(GameData.ContentType.EXERCISE);
            int updatedIndex = chapter.VerifyExerciseIndex(chapter.ActiveExerciseIndex);
            chapter.ActiveExerciseIndex = updatedIndex;
            AppState.MainWin.SelectExercise(chapter.ActiveExerciseIndex, AppState.ActiveTab == TabViewType.EXERCISE);
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
            , ArticlesAction action
            , Chapter targetChapter)
        {
            List<ArticleListItem> articlesToInsert = new List<ArticleListItem>();
            int targetChapterIndex = targetChapter.Index;

            if (action == ArticlesAction.COPY)
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
        /// Performs the copy/move operation.
        /// </summary>
        /// <param name="targetChapter"></param>
        /// <param name="articlesToInsert"></param>
        /// <param name="copy"></param>
        /// <returns>whether we are copying Games or Exercise; GENERIC if both types</returns>
        private static GameData.ContentType CopyOrMoveArticles(Chapter targetChapter, List<ArticleListItem> articlesToInsert, ArticlesAction action)
        {
            GameData.ContentType contentType = GameData.ContentType.NONE;

            // place the articles in the target chapter
            foreach (ArticleListItem ali in articlesToInsert)
            {
                if (ali.Article != null)
                {
                    if (ali.ContentType == GameData.ContentType.MODEL_GAME)
                    {
                        targetChapter.AddModelGame(ali.Article);
                        if (contentType == GameData.ContentType.EXERCISE || contentType == GameData.ContentType.GENERIC)
                        {
                            contentType = GameData.ContentType.GENERIC;
                        }
                        else
                        {
                            contentType = GameData.ContentType.MODEL_GAME;
                        }
                    }
                    else if (ali.ContentType == GameData.ContentType.EXERCISE)
                    {
                        targetChapter.AddExercise(ali.Article);
                        if (contentType == GameData.ContentType.MODEL_GAME || contentType == GameData.ContentType.GENERIC)
                        {
                            contentType = GameData.ContentType.GENERIC;
                        }
                        else
                        {
                            contentType = GameData.ContentType.EXERCISE;
                        }
                    }
                }
            }

            // remove from the articles from their source chapters,
            // note that we could be removing the Active article so the GUI must be handled
            // accordingly
            if (action != ArticlesAction.COPY)
            {
                foreach (ArticleListItem ali in articlesToInsert)
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(ali.ChapterIndex);
                    chapter?.DeleteArticle(ali.Article);
                }
            }

            // collect info for the Undo operation
            WorkbookOperationType typ = action == ArticlesAction.COPY ? WorkbookOperationType.COPY_ARTICLES : WorkbookOperationType.MOVE_ARTICLES;
            WorkbookOperation op = new WorkbookOperation(typ, targetChapter, (object)articlesToInsert);
            WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);

            return contentType;
        }

        /// <summary>
        /// Invokes the InvokeSelectSingleChapter dialog
        /// and returns the selected index.
        /// </summary>
        /// <returns></returns>
        public static int InvokeSelectSingleChapterDialog(out bool newChapter)
        {
            newChapter = false;

            try
            {
                int chapterIndex = -1;

                SelectSingleChapterDialog dlg = new SelectSingleChapterDialog();
                {
                };
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                if (dlg.ShowDialog() == true)
                {
                    if (dlg.CreateNew)
                    {
                        chapterIndex = WorkbookManager.SessionWorkbook.CreateNewChapter().Index;
                        newChapter = true;
                    }
                    else
                    {
                        chapterIndex = dlg.SelectedIndex;
                    }
                }

                return chapterIndex;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Undoes the copy articles operation.
        /// </summary>
        /// <param name="targetChapter"></param>
        /// <param name="lst"></param>
        public static void UndoCopyArticles(Chapter targetChapter, object lst)
        {
            try
            {
                Chapter firstChapter = null;

                List<ArticleListItem> articles = lst as List<ArticleListItem>;

                // delete from the copy target chapter
                foreach (var item in articles)
                {
                    targetChapter.DeleteArticle(item.Article);
                    if (firstChapter == null)
                    {
                        firstChapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(item.ChapterIndex);
                    }
                }

                GuiUtilities.RefreshChaptersView(firstChapter);
                AppState.MainWin.UiTabChapters.Focus();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Undoes the move articles operation.
        /// </summary>
        /// <param name="targetChapter"></param>
        /// <param name="lst"></param>
        public static void UndoMoveArticles(Chapter targetChapter, object lst)
        {
            Chapter firstChapter = null;

            try
            {
                List<ArticleListItem> articles = lst as List<ArticleListItem>;

                // first return the articles to where they were copied from
                foreach (ArticleListItem item in articles)
                {
                    Chapter sourceChapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(item.ChapterIndex);
                    // since this the same order we were removing the articles in.
                    // the indexes should be valid as we ,ove down the list
                    sourceChapter.InsertArticle(item.Article, item.ArticleIndex);
                    if (firstChapter == null)
                    {
                        firstChapter = sourceChapter;
                    }
                }

                // delete from the move target chapter
                foreach (var item in articles)
                {
                    targetChapter.DeleteArticle(item.Article);
                }

                GuiUtilities.RefreshChaptersView(firstChapter);
                AppState.MainWin.UiTabChapters.Focus();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Checks if we have at least 2 articles i.e. 1 in addition to the currently viewed.
        /// We have to check the type of the item so that we don't count chapter items.
        /// </summary>
        /// <param name="lstPos"></param>
        /// <returns></returns>
        public static bool HasAtLeastNArticles(ObservableCollection<ArticleListItem> lstPos, int n)
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

            return count >= n;
        }

        /// <summary>
        /// Sorts games in the chapter per specified criteria.
        /// Keep the original order if items cannot be separated by those criteria.
        /// This method is superior to the standard QuickSort because it does keep 
        /// the original order.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="sortBy"></param>
        /// <param name="direction"></param>
        private static void SortGames(Chapter chapter, GameSortCriterion.SortItem sortBy, GameSortCriterion.SortItem direction)
        {
            IComparer<Article> comparer = new ArticleComparer(sortBy, direction);
            chapter.ModelGames = chapter.ModelGames
                .Select((item, index) => new { item, index })
                .OrderBy(z => z.item, comparer)
                .ThenBy(z => z.index)
                .Select(z => z.item)
                .ToList();

            AppState.MainWin.ChaptersView.IsDirty = true;
            AppState.IsDirty = true;
        }

        /// <summary>
        /// Sorts exercises in the chapter per specified criteria.
        /// Keep the original order if items cannot be separated by those criteria.
        /// This method is superior to the standard QuickSort because it does keep 
        /// the original order.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="sortBy"></param>
        /// <param name="direction"></param>
        private static void SortExercises(Chapter chapter, GameSortCriterion.SortItem sortBy, GameSortCriterion.SortItem direction)
        {
            IComparer<Article> comparer = new ArticleComparer(sortBy, direction);
            chapter.Exercises = chapter.Exercises
                .Select((item, index) => new { item, index })
                .OrderBy(z => z.item, comparer)
                .ThenBy(z => z.index)
                .Select(z => z.item)
                .ToList();

            AppState.MainWin.ChaptersView.IsDirty = true;
            AppState.IsDirty = true;
        }

        /// <summary>
        /// Regenerates the Study Tree from the Games.
        /// </summary>
        /// <param name="chapter"></param>
        private static void RegenerateStudy(Chapter chapter)
        {
            if (chapter.StudyTree.Tree.Nodes.Count <= 1 ||
                MessageBox.Show(Properties.Resources.MsgThisOverwritesStudy,
                    Properties.Resources.Information,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                List<VariationTree> trees = new List<VariationTree>();
                foreach (Article game in chapter.ModelGames)
                {
                    trees.Add(game.Tree);
                }

                // TODO: trimming while merging would be more effective; implement MergeWithTrim()
                VariationTree tree = TreeMerge.MergeVariationTrees(trees);
                if (tree != null)
                {
                    TreeUtils.TrimTree(ref tree, Configuration.AutogenTreeDepth, PieceColor.Black);
                    tree.ContentType = GameData.ContentType.STUDY_TREE;
                    chapter.StudyTree.Tree = tree;
                    AppState.IsDirty = true;
                }
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

    }
}

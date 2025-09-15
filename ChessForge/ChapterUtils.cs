using ChessPosition;
using ChessPosition.GameTree;
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
    /// Utilities to manipulate chapter objects.
    /// </summary>
    public class ChapterUtils
    {
        /// <summary>
        /// Invoked when a new chapter was created and needs a name.
        /// Calls the Chapter Title dialog and lets the user edit the name/author attributes.
        /// If the user cancels, discards the new dialog and restore the status quo.
        /// </summary>
        /// <param name="targetChapter"></param>
        /// <param name="activeChapter"></param>
        /// <returns></returns>
        public static bool NameNewChapter(Chapter targetChapter, Chapter activeChapter)
        {
            bool proceed = true;

            ChapterTitleDialog dlg = new ChapterTitleDialog(targetChapter);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);
            if (dlg.ShowDialog() == true)
            {
                targetChapter.SetTitle(dlg.ChapterTitle);
                targetChapter.SetAuthor(dlg.Author);
                AppState.Workbook.ActiveChapter = targetChapter;
            }
            else
            {
                AppState.Workbook.Chapters.Remove(targetChapter);
                AppState.Workbook.ActiveChapter = activeChapter;
                proceed = false;
            }

            return proceed;
        }

        /// <summary>
        /// Returns true if all chapters in the workbook have ShowSolutionsOnOpen set to true
        /// </summary>
        /// <returns></returns>
        public static bool IsAllChaptersShowSolutionsOnOpen()
        {
            bool all = true;

            try
            {
                foreach(Chapter ch in AppState.Workbook.Chapters)
                {
                    if (!ch.ShowSolutionsOnOpen)
                    {
                        all = false;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                all = false;
                AppLog.Message("IsAllChaptersShowSolutionsOnOpen()", ex);
            }

            return all;
        }

        /// <summary>
        /// Through some loose programming we may end up with some garbage in the Study Tree header
        /// (e.g. after regenerating) which in turn may affect things like the title of an exercise
        /// when created from a position in the Study.
        /// </summary>
        /// <param name="tree"></param>
        public static void ClearStudyTreeHeader(VariationTree tree)
        {
            tree.Header.SetHeaderValue(PgnHeaders.KEY_WHITE_ELO, "");
            tree.Header.SetHeaderValue(PgnHeaders.KEY_BLACK_ELO, "");
            tree.Header.SetHeaderValue(PgnHeaders.KEY_DATE, "");
            tree.Header.SetHeaderValue(PgnHeaders.KEY_EVENT, "");
        }

        /// <summary>
        /// Invokes the dialog for specifying regeneration's depth.
        /// </summary>
        /// <param name="chapter"></param>
        public static void InvokeRegenerateStudyDialog(Chapter chapter)
        {
            if (chapter != null)
            {
                RegenerateStudyDialog dlg = new RegenerateStudyDialog(chapter);
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        if (RegenerateStudy(dlg.ApplyToAllChapters ? null : chapter))
                        {
                            AppState.IsDirty = true;
                        }

                        // go to the appropriate view
                        AppState.MainWin.SetupGuiForActiveStudyTree(true);
                    }
                    catch (Exception ex)
                    {
                        AppLog.Message("InvokeRegenerateStudyDialog()", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Invokes the dialog for sorting games in a chapter/workbook.
        /// </summary>
        /// <param name="chapter"></param>
        public static void InvokeSortGamesDialog(Chapter chapter)
        {
            if (chapter != null)
            {
                try
                {
                    SortGamesDialog dlg = new SortGamesDialog(chapter);
                    GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                    if (dlg.ShowDialog() == true && dlg.SortGamesBy != GameSortCriterion.SortItem.NONE)
                    {
                        if (dlg.ApplyToAllChapters)
                        {
                            SortGames(null, dlg.SortGamesBy, dlg.SortGamesDirection);
                        }
                        else
                        {
                            SortGames(chapter, dlg.SortGamesBy, dlg.SortGamesDirection);
                        }

                        AppState.IsDirty = true;
                        GuiUtilities.RefreshChaptersView(null);
                        AppState.MainWin.UiTabChapters.Focus();
                        PulseManager.ChapterIndexToBringIntoView = chapter.Index;
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Message("InvokeSortGamesDialog()", ex);
                }
            }
        }

        /// <summary>
        /// Invokes the dialog for configuring the exercise view.
        /// </summary>
        /// <param name="chapter"></param>
        public static void InvokeExerciseViewConfigDialog(Chapter chapter)
        {
            if (chapter != null)
            {
                ExerciseSolutionsDialog dlg = new ExerciseSolutionsDialog(chapter);
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        if (dlg.ShowSolutionOnOpen != chapter.ShowSolutionsOnOpen || dlg.ApplyToAllChapters)
                        {
                            if (dlg.ApplyToAllChapters)
                            {
                                foreach (Chapter ch in AppState.Workbook.Chapters)
                                {
                                    ch.ShowSolutionsOnOpen = dlg.ShowSolutionOnOpen;
                                    UpdateShowSolutions(ch, dlg.ShowSolutionOnOpen);
                                }
                            }
                            else
                            {
                                chapter.ShowSolutionsOnOpen = dlg.ShowSolutionOnOpen;
                                UpdateShowSolutions(chapter, dlg.ShowSolutionOnOpen);
                            }
                            AppState.MainWin.UpdateShowSolutionInExerciseView(dlg.ShowSolutionOnOpen);
                            AppState.IsDirty = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLog.Message("InvokeExerciseViewConfigDialog()", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Invokes the dialog for setting thumbnails in a chapter/workbook.
        /// </summary>
        /// <param name="chapter"></param>
        public static void InvokeSetThumbnailsDialog(Chapter chapter)
        {
            if (chapter != null)
            {
                SetThumbnailsDialog dlg = new SetThumbnailsDialog(chapter);
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        if (dlg.ThumbnailMove > 0)
                        {
                            if (dlg.ApplyToAllChapters)
                            {
                                SetThumbnails(null, dlg.ThumbnailMove, dlg.ThumbnailMoveColor, dlg.OverwriteThumbnails);
                            }
                            else
                            {
                                SetThumbnails(chapter, dlg.ThumbnailMove, dlg.ThumbnailMoveColor, dlg.OverwriteThumbnails);
                            }
                            AppState.IsDirty = true;
                            AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgGamesThumbnailsSet, CommentBox.HintType.INFO);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLog.Message("InvokeSetThumbnailsDialog()", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Updates show/hide solutions on open flag in a chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="show"></param>
        public static void UpdateShowSolutionsInChapter(Chapter chapter, bool show)
        {
            if (chapter != null)
            {
                foreach (Article exc in chapter.Exercises)
                {
                    exc.Tree.ShowTreeLines = show;
                }
            }
        }

        /// <summary>
        /// Updates show/hide solutions on open flag in a chapter
        /// or entire workbook.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="show"></param>
        public static void UpdateShowSolutions(Chapter chapter, bool show)
        {
            if (chapter != null)
            {
                UpdateShowSolutionsInChapter(chapter, show);
            }
            else
            {
                if (AppState.Workbook != null)
                {
                    foreach (Chapter ch in AppState.Workbook.Chapters)
                    {
                        UpdateShowSolutionsInChapter(ch, show);
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
            // preserve/restore the active chapter as the dialog may change it if new chapter was requested
            Chapter currActiveChapter = AppState.ActiveChapter;
            int index = InvokeSelectSingleChapterDialog(-1, out bool newChapter);
            WorkbookManager.SessionWorkbook.ActiveChapter = currActiveChapter;

            if (index >= 0)
            {
                Chapter targetChapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(index);

                if (newChapter)
                {
                    targetChapter.SetTitle(SuggestChapterTitle(startNode));
                    ChapterTitleDialog dlg = new ChapterTitleDialog(targetChapter);
                    GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);
                    if (dlg.ShowDialog() == true)
                    {
                        targetChapter.SetTitle(dlg.ChapterTitle);
                        targetChapter.SetAuthor(dlg.Author);
                    }
                }

                RemoveChapters(lstArticles);
                bool emptyEntryList = lstArticles.Count == 0;

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

                        if (AppState.ActiveTab != TabViewType.CHAPTERS)
                        {
                            bool gotoChaptersView = false;

                            // check if we have a request "not to ask" in this session
                            if (Configuration.PostCopyMoveNavigation == 0)
                            {
                                // no saved request so ask the user whether to "stay here" or show the target chapter
                                PostCopyMoveDialog dlg = new PostCopyMoveDialog(targetChapter, action, articlesToInsert.Count);
                                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);
                                if (dlg.ShowDialog() == true)
                                {
                                    gotoChaptersView = true;
                                }

                                // persist the answer if requested
                                if (dlg.UiCbDontAsk.IsChecked == true)
                                {
                                    Configuration.PostCopyMoveNavigation = (uint)(gotoChaptersView ? 1 : 2);
                                }
                            }
                            else
                            {
                                if (Configuration.PostCopyMoveNavigation == 1)
                                {
                                    gotoChaptersView = true;
                                }
                            }

                            if (gotoChaptersView)
                            {
                                AppState.Workbook.ActiveChapter = targetChapter;
                                targetChapter.IsViewExpanded = true;
                                // show chapter view with the target chapter in the view and expanded
                                AppState.MainWin.ChaptersView.IsDirty = true;
                                AppState.MainWin.UiTabChapters.Focus();
                                PulseManager.ChapterIndexToBringIntoView = targetChapter.Index;
                            }
                        }
                    }
                    else
                    {
                        if (!emptyEntryList)
                        {
                            AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.ItemsAlreadyInChapter, CommentBox.HintType.ERROR);
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
                Chapter activeChapter = AppState.Workbook.ActiveChapter;

                // if we are in the Chapters view, we want to refresh the view
                // and bring the target chapter into view.
                if (AppState.ActiveTab == TabViewType.CHAPTERS)
                {
                    // if in Chapters view then change active chapter to target
                    AppState.Workbook.ActiveChapter = targetChapter;
                    GuiUtilities.RefreshChaptersView(null);
                    AppState.MainWin.UiTabChapters.Focus();
                    PulseManager.ChapterIndexToBringIntoView = targetChapter.Index;
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
                    if (AppState.ActiveTab == TabViewType.MODEL_GAME && (contentType == GameData.ContentType.MODEL_GAME || contentType == GameData.ContentType.GENERIC))
                    {
                        UpdateModelGamesView(activeChapter);
                    }
                    if (AppState.ActiveTab == TabViewType.EXERCISE && (contentType == GameData.ContentType.EXERCISE || contentType == GameData.ContentType.GENERIC))
                    {
                        UpdateExercisesView(activeChapter);
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
                string title = SuggestChapterTitle(nd);
                chapter.SetTitle(title);
            }
        }

        /// <summary>
        /// Suggests the chapter title based on the passed position.
        /// This is useful after copying/moving games that were find after
        /// search for identical positions.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private static string SuggestChapterTitle(TreeNode nd)
        {
            string title = "";

            if (nd != null)
            {
                if (nd.Eco != null)
                {
                    title = nd.Eco + ": ";
                }
                title += MoveUtils.BuildSingleMoveText(nd, true, true, 0);
            }

            return title;
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
        public static int InvokeSelectSingleChapterDialog(int chapterIndex, out bool newChapter)
        {
            newChapter = false;

            try
            {
                SelectSingleChapterDialog dlg = new SelectSingleChapterDialog(chapterIndex);
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
                else
                {
                    chapterIndex = -1;
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
        /// Undoes the insert articles operation.
        /// The list of articles can come from import or pasting.
        /// Some of the articles may represent a Chapters.
        /// When removing the articles we need to make our way from
        /// the end of the list to the front so we delete chapter content
        /// before deleting the chapter itself.
        /// </summary>
        /// <param name="targetChapter"></param>
        /// <param name="lst"></param>
        public static void UndoInsertArticles(Chapter targetChapter, object lst)
        {
            List<ArticleListItem> articles = lst as List<ArticleListItem>;

            try
            {
                for (int i = articles.Count - 1; i >= 0; i--)
                {
                    ArticleListItem item = articles[i];
                    if (item.Article == null)
                    {
                        // this is a chapter
                        AppState.Workbook.DeleteChapter(item.Chapter);
                    }
                    else
                    {
                        item.Chapter.DeleteArticle(item.Article);
                    }
                }

                GuiUtilities.RefreshChaptersView(null);
                AppState.MainWin.UiTabChapters.Focus();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Undoes the import of chapters(s).
        /// </summary>
        /// <param name="targetChapter"></param>
        /// <param name="lst"></param>
        public static void UndoImportChapters(object lst)
        {
            List<ArticleListItem> articles = lst as List<ArticleListItem>;

            try
            {
                foreach (ArticleListItem item in articles)
                {
                    if (item.Chapter != null)
                    {
                        AppState.Workbook.DeleteChapter(item.Chapter);
                    }
                }

                GuiUtilities.RefreshChaptersView(null);
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
                    // the indexes should be valid as we move down the list
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
        /// Undoes the move articles from a single chapter
        /// to multiple chapters (e.g. when moving by ECO).
        /// </summary>
        /// <param name="sourceChapter"></param>
        /// <param name="items"></param>
        public static void UndoMoveMultiChapterArticles(Chapter sourceChapter, object items)
        {
            try
            {
                ObservableCollection<ArticleListItem> articles = items as ObservableCollection<ArticleListItem>;
                // add the articles to the source chapter
                foreach (ArticleListItem item in articles)
                {
                    AppState.Workbook.DeleteArticle(item.Article);
                    sourceChapter.InsertArticle(item.Article, item.ArticleIndex);
                }

                GuiUtilities.RefreshChaptersView(sourceChapter);
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
        public static void SortGames(Chapter chapter, GameSortCriterion.SortItem sortBy, GameSortCriterion.SortItem direction, bool showMsg = true)
        {
            Mouse.SetCursor(Cursors.Wait);

            try
            {
                if (chapter != null)
                {
                    SortGamesInChapter(chapter, sortBy, direction);
                }
                else
                {
                    foreach (Chapter ch in AppState.Workbook.Chapters)
                    {
                        SortGamesInChapter(ch, sortBy, direction);
                    }
                }

                AppState.MainWin.ChaptersView.IsDirty = true;
                AppState.IsDirty = true;

                if (AppState.ActiveTab == TabViewType.CHAPTERS)
                {
                    AppState.MainWin.ChaptersView.BuildFlowDocumentForChaptersView(false);
                }

                if (showMsg)
                {
                    AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgGamesSorted, CommentBox.HintType.INFO);
                }
            }
            catch
            {
            }

            Mouse.SetCursor(Cursors.Arrow);
        }

        /// <summary>
        /// Sorts games in a single chapter per specified criteria.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="sortBy"></param>
        /// <param name="direction"></param>
        private static void SortGamesInChapter(Chapter chapter, GameSortCriterion.SortItem sortBy, GameSortCriterion.SortItem direction)
        {
            IComparer<Article> comparer = new ArticleComparer(sortBy, direction);
            chapter.ModelGames = chapter.ModelGames
                .Select((item, index) => new { item, index })
                .OrderBy(z => z.item, comparer)
                .ThenBy(z => z.index)
                .Select(z => z.item)
                .ToList();
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
        /// Sets thumbnails at the requested move in all games of a chapter
        /// or of all chapters.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="moveNo"></param>
        /// <param name="color"></param>
        /// <param name="overwrite"></param>
        private static void SetThumbnails(Chapter chapter, int moveNo, PieceColor color, bool overwrite)
        {
            try
            {
                if (chapter != null)
                {
                    SetThumbnailsInChapter(chapter, moveNo, color, overwrite);
                }
                else
                {
                    foreach (Chapter ch in AppState.Workbook.Chapters)
                    {
                        SetThumbnailsInChapter(ch, moveNo, color, overwrite);
                    }
                }

                AppState.MainWin.ActiveTreeView?.UpdateThumbnail();
            }
            catch { }
        }

        /// <summary>
        /// Sets thumbnails at the requested move in all games of a chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="moveNo"></param>
        /// <param name="color"></param>
        /// <param name="overwrite"></param>
        private static void SetThumbnailsInChapter(Chapter chapter, int moveNo, PieceColor color, bool overwrite)
        {
            try
            {
                foreach (Article game in chapter.ModelGames)
                {
                    // find current thumbnail
                    TreeNode thumbCurrent = null;
                    foreach (TreeNode node in game.Tree.Nodes)
                    {
                        if (node.IsThumbnail)
                        {
                            thumbCurrent = node;
                            break;
                        }
                    }

                    // walk the main line to find a candidate
                    TreeNode thumbCandidate = game.Tree.RootNode;
                    if (overwrite || thumbCurrent == null)
                    {
                        while (thumbCandidate.Children.Count > 0)
                        {
                            thumbCandidate = thumbCandidate.Children[0];
                            if (thumbCandidate.MoveNumber == moveNo && color != thumbCandidate.ColorToMove)
                            {
                                break;
                            }
                        }
                    }

                    // if there is a candidate chack other condition before setting
                    if (thumbCandidate.NodeId != 0)
                    {
                        if (overwrite)
                        {
                            thumbCandidate.IsThumbnail = true;
                            if (thumbCurrent != null && thumbCurrent != thumbCandidate)
                            {
                                thumbCurrent.IsThumbnail = false;
                            }
                        }
                        else
                        {
                            if (thumbCurrent == null)
                            {
                                thumbCandidate.IsThumbnail = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("SetThumbnailsInChapter()", ex);
            }
        }

        /// <summary>
        /// Regenerates the Study Tree from the Games.
        /// </summary>
        /// <param name="chapter"></param>
        private static bool RegenerateStudy(Chapter chapter)
        {
            bool done = false;
            bool overwriteWarningIssued = false;

            Mouse.SetCursor(Cursors.Wait);

            List<Chapter> chapters = new List<Chapter>();
            List<VariationTree> replacedTrees = new List<VariationTree>();

            try
            {

                if (chapter != null)
                {
                    done = RegenerateChapterStudy(chapter, ref overwriteWarningIssued, chapters, replacedTrees);
                }
                else
                {
                    foreach (Chapter ch in AppState.Workbook.Chapters)
                    {
                        bool cont = RegenerateChapterStudy(ch, ref overwriteWarningIssued, chapters, replacedTrees);
                        if (!cont)
                        {
                            break;
                        }
                        else
                        {
                            done = true;
                        }
                    }
                }
                if (replacedTrees.Count > 0)
                {
                    WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.REGENERATE_STUDIES, null, -1, chapters, replacedTrees, null);
                    WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
                }


            }
            catch (Exception ex)
            {
                AppLog.Message("RegenerateStudy()", ex);
            }

            Mouse.SetCursor(Cursors.Arrow);
            return done;
        }

        /// <summary>
        /// Regenerates the Study Tree in a single chapter.
        /// </summary>
        /// <param name="chapter"></param>
        private static bool RegenerateChapterStudy(Chapter chapter, ref bool overwriteWarningIssued, List<Chapter> chapters, List<VariationTree> replacedTrees)
        {
            if (chapter.StudyTree.Tree.Nodes.Count > 1 && !overwriteWarningIssued)
            {
                overwriteWarningIssued = true;

                if (MessageBox.Show(Properties.Resources.MsgThisOverwritesStudy,
                    Properties.Resources.Warning,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    return false;
                }
            }

            if (chapter.StudyTree.Tree.Nodes.Count >= 1)
            {
                Mouse.SetCursor(Cursors.Wait);
                try
                {
                    List<VariationTree> trees = new List<VariationTree>();
                    foreach (Article game in chapter.ModelGames)
                    {
                        trees.Add(game.Tree);
                    }

                    VariationTree tree = TreeMerge.MergeVariationTreeListEx(trees, Configuration.AutogenTreeDepth, true);
                    if (tree != null)
                    {
                        // data for Undo
                        chapters.Add(chapter);
                        replacedTrees.Add(chapter.StudyTree.Tree);

                        TreeUtils.TrimTree(ref tree, Configuration.AutogenTreeDepth, PieceColor.Black);
                        tree.ContentType = GameData.ContentType.STUDY_TREE;
                        tree.BuildLines();
                        ClearStudyTreeHeader(tree);

                        // preserve the preamble when replacing the study tree
                        var preamble = chapter.StudyTree.Tree.Header.GetPreamble();

                        chapter.StudyTree.Tree = tree;
                        tree.Header.SetPreamble(preamble);

                        // if we are in the study tab, must set the new tree as active tree (does not happen automatically)
                        if (AppState.ActiveTab == TabViewType.STUDY)
                        {
                            AppState.Workbook.ActiveChapter.SetActiveVariationTree(GameData.ContentType.STUDY_TREE);
                        }
                        chapter.SetAuthor("");
                        AppState.IsDirty = true;
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Message("RegenerateChapterStudy()", ex);
                }
                Mouse.SetCursor(Cursors.Arrow);
            }

            return true;
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

using ChessForge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameTree;
using ChessPosition;
using System.Collections.ObjectModel;

namespace ChessForge
{
    /// <summary>
    /// Utilities to manipulate chapter objects.
    /// </summary>
    public class ChapterUtils
    {
        /// <summary>
        /// Processes a response from the dialog, where the user requested to copy/move articles
        /// </summary>
        /// <param name="lstIdenticalPositions"></param>
        /// <param name="request"></param>
        public static void RequestCopyMoveArticles(TreeNode searchNode, bool allChaptersCheckbox, ObservableCollection<ArticleListItem> lstIdenticalPositions, bool copy, bool showAllChapters)
        {
            // make a list with only games and exercises
            RemoveStudies(lstIdenticalPositions);
            string title = copy ? Properties.Resources.SelectItemsToCopy : Properties.Resources.SelectItemsToMove;
            SelectArticlesDialog dlg = new SelectArticlesDialog(null, allChaptersCheckbox, title, ref lstIdenticalPositions, showAllChapters)
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
                    ProcessCopyOrMoveArticles(searchNode, lstIdenticalPositions, copy);
                }
            }
        }

        /// <summary>
        /// Performs copy/move operations. It can be invoked from the FindIdenticalPositions context
        /// or via the menu item handlers. 
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="lstArticles"></param>
        /// <param name="copy"></param>
        public static void ProcessCopyOrMoveArticles(TreeNode startNode, ObservableCollection<ArticleListItem> lstArticles, bool copy)
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
                    List<ArticleListItem> articlesToInsert = CreateListToMoveOrCopy(lstArticles, copy, targetChapter);
                    if (articlesToInsert.Count > 0)
                    {
                        CopyOrMoveArticles(targetChapter, articlesToInsert, copy);

                        targetChapter.IsViewExpanded = true;
                        targetChapter.IsModelGamesListExpanded = true;
                        targetChapter.IsExercisesListExpanded = true;

                        AppState.MainWin.ChaptersView.IsDirty = true;
                        AppState.IsDirty = true;

                        GuiUtilities.RefreshChaptersView(null);
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

        /// <summary>
        /// Moves a game between chapters after invoking a dialog
        /// to select the target chapter
        /// </summary>
        /// <returns></returns>
        public static int MoveGameBetweenChapters(Chapter sourceChapter)
        {
            int targetChapterIndex = -1;
            if (sourceChapter == null)
            {
                return -1;
            }

            try
            {
                int sourceChapterIndex = sourceChapter.Index;
                int gameIndex = sourceChapter.ActiveModelGameIndex;
                Article game = sourceChapter.GetModelGameAtIndex(gameIndex);

                targetChapterIndex = InvokeSelectSingleChapterDialog(out _);

                if (game != null && targetChapterIndex >= 0 && targetChapterIndex != sourceChapterIndex)
                {
                    Chapter targetChapter = WorkbookManager.SessionWorkbook.Chapters[targetChapterIndex];

                    targetChapter.ModelGames.Add(game);
                    sourceChapter.ModelGames.Remove(game);

                    WorkbookManager.SessionWorkbook.ActiveChapter = targetChapter;
                    targetChapter.IsModelGamesListExpanded = true;
                    targetChapter.ActiveModelGameIndex = targetChapter.GetModelGameCount() - 1;

                    AppState.IsDirty = true;
                    AppState.MainWin.ChaptersView.IsDirty = true;

                    GuiUtilities.RefreshChaptersView(null);
                    AppState.MainWin.UiTabChapters.Focus();
                    PulseManager.ChaperIndexToBringIntoView = targetChapter.Index;
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("MoveGameBetweenChapters()", ex);
            }

            return targetChapterIndex;
        }

        /// <summary>
        /// Moves an exercise between chapters after invoking a dialog
        /// to select the target chapter
        /// </summary>
        /// <returns></returns>
        public static int MoveExerciseBetweenChapters(Chapter sourceChapter)
        {
            int targetChapterIndex = -1;
            if (sourceChapter == null)
            {
                return -1;
            }

            try
            {
                int sourceChapterIndex = sourceChapter.Index;
                int exerciseIndex = sourceChapter.ActiveExerciseIndex;
                Article exercise = sourceChapter.GetExerciseAtIndex(exerciseIndex);

                targetChapterIndex = InvokeSelectSingleChapterDialog(out _);

                if (exercise != null && targetChapterIndex >= 0 && targetChapterIndex != sourceChapterIndex)
                {
                    Chapter targetChapter = WorkbookManager.SessionWorkbook.Chapters[targetChapterIndex];

                    targetChapter.Exercises.Add(exercise);
                    sourceChapter.Exercises.Remove(exercise);

                    WorkbookManager.SessionWorkbook.ActiveChapter = targetChapter;
                    targetChapter.IsExercisesListExpanded = true;
                    targetChapter.ActiveExerciseIndex = targetChapter.GetExerciseCount() - 1;

                    AppState.IsDirty = true;
                    AppState.MainWin.ChaptersView.IsDirty = true;

                    GuiUtilities.RefreshChaptersView(null);
                    AppState.MainWin.UiTabChapters.Focus();
                    PulseManager.ChaperIndexToBringIntoView = targetChapter.Index;
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("MoveExerciseBetweenChapters()", ex);
            }

            return targetChapterIndex;
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
            , bool copy
            , Chapter targetChapter)
        {
            List<ArticleListItem> articlesToInsert = new List<ArticleListItem>();
            int targetChapterIndex = targetChapter.Index;

            if (copy)
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
        /// 
        /// </summary>
        /// <param name="targetChapter"></param>
        /// <param name="articlesToInsert"></param>
        /// <param name="copy"></param>
        private static void CopyOrMoveArticles(Chapter targetChapter, List<ArticleListItem> articlesToInsert, bool copy)
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
            if (!copy)
            {
                foreach (ArticleListItem ali in articlesToInsert)
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(ali.ChapterIndex);
                    chapter?.DeleteArticle(ali.Article);
                }
            }

            // collect info for the Undo operation
            WorkbookOperationType typ = copy ? WorkbookOperationType.COPY_ARTICLES : WorkbookOperationType.MOVE_ARTICLES;
            WorkbookOperation op = new WorkbookOperation(typ, targetChapter, (object)articlesToInsert);
            WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
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

                SelectSingleChapterDialog dlg = new SelectSingleChapterDialog()
                {
                    //TODO: if maximized, ChessForgeMain will be wrong!
                    Left = AppState.MainWin.ChessForgeMain.Left + 100,
                    Top = AppState.MainWin.Top + 100,
                    Topmost = false,
                    Owner = AppState.MainWin
                };

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

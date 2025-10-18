using ChessPosition;
using GameTree;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChessForge
{
    /// <summary>
    /// Utilities for deletion of articles.
    /// </summary>
    public class DeleteArticlesUtils
    {
        /// <summary>
        /// Deletes the Game at the requested index from the list of games.
        /// </summary>
        /// <param name="index"></param>
        public static void DeleteModelGame(int index)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                int gameCount = chapter.GetModelGameCount();
                if (index >= 0 && index < gameCount)
                {
                    Article article = chapter.GetModelGameAtIndex(index);
                    var itemList = new List<ArticleListItem>();
                    itemList.Add(new ArticleListItem(chapter, chapter.Index, article, index));
                    DeleteArticles(itemList, GameData.ContentType.MODEL_GAME);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Deletes the Exercise at the requested index from the list of games.
        /// </summary>
        /// <param name="index"></param>
        public static void DeleteExercise(int index)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                int exerciseCount = chapter.GetExerciseCount();
                if (index >= 0 && index < exerciseCount)
                {
                    Article article = chapter.GetExerciseAtIndex(index);
                    var itemList = new List<ArticleListItem>();
                    itemList.Add(new ArticleListItem(chapter, chapter.Index, article, index));
                    DeleteArticles(itemList, GameData.ContentType.EXERCISE);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Deletes a list of articles of type MODEL_GAME or EXERCISE.
        /// Creates an Undo operation.
        /// </summary>
        /// <param name="articleList"></param>
        /// <param name="articleType"></param>
        public static void DeleteArticles(List<ArticleListItem> articlesToDelete, GameData.ContentType articleType = GameData.ContentType.GENERIC)
        {
            List<int> indicesToDelete = new List<int>();
            foreach (ArticleListItem item in articlesToDelete)
            {
                indicesToDelete.Add(item.ArticleIndex);
            }

            List<ArticleListItem> deletedArticles = new List<ArticleListItem>();
            List<int> deletedIndices = new List<int>();
            List<List<FullNodeId>> refNodes = new List<List<FullNodeId>>();
            for (int i = 0; i < articlesToDelete.Count; i++)
            {
                ArticleListItem item = articlesToDelete[i];
                Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(item.ChapterIndex);
                if (chapter != null && item.Article != null)
                {
                    // NOTE: we only calculate the index here, after the previous item was deleted.
                    // That way in the UNDO the games will be restored to their correct places 
                    // as in the undo we insert them in the reverse order/
                    int index = chapter.GetArticleIndex(item.Article);
                    bool res = chapter.DeleteArticle(item.Article);
                    if (res)
                    {
                        deletedArticles.Add(item);
                        deletedIndices.Add(index);
                        refNodes.Add(WorkbookManager.RemoveArticleReferences(item.Article.Guid));
                    }
                }
            }

            if (deletedArticles.Count > 0)
            {
                WorkbookOperationType wot = GetDeleteOpType(articleType);
                WorkbookOperation op = new WorkbookOperation(wot, null, -1, deletedArticles, deletedIndices, refNodes);
                WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);

                PostArticleDeleteSetup(deletedArticles.Count);
            }
        }

        /// <summary>
        /// Deletes a list of articles of type MODEL_GAME or EXERCISE.
        /// Creates an Undo operation.
        /// </summary>
        /// <param name="articleList"></param>
        /// <param name="articleType"></param>
        public static void DeleteArticleListItems(ObservableCollection<ArticleListItem> articleList, GameData.ContentType articleType = GameData.ContentType.GENERIC)
        {
            List<ArticleListItem> articlesToDelete = GetArticlesToDelete(articleList);
            DeleteArticles(articlesToDelete, articleType);
        }

        /// <summary>
        /// Overloaded method for deleting articles given a list of DuplicateListItem.
        /// </summary>
        /// <param name="dupeList"></param>
        public static void DeleteDupeArticles(ObservableCollection<DuplicateListItem> dupeList)
        {
            ObservableCollection<ArticleListItem> articleList = new ObservableCollection<ArticleListItem>();

            foreach (DuplicateListItem item in dupeList)
            {
                if (item.ArticleItem != null)
                {
                    articleList.Add(item.ArticleItem);
                }
            }

            DeleteArticleListItems(articleList);
        }

        /// <summary>
        /// Update the GUI appropriately after the deletion.
        /// </summary>
        /// <param name="deletedCount"></param>
        private static void PostArticleDeleteSetup(int deletedCount)
        {
            AppState.MainWin.ChaptersView.IsDirty = true;
            AppState.IsDirty = true;

            if (AppState.ActiveVariationTree == null || AppState.CurrentEvaluationMode != EvaluationManager.Mode.CONTINUOUS)
            {
                AppState.MainWin.StopEvaluation(true);
                AppState.MainWin.BoardCommentBox.ShowTabHints();
            }

            // don't call it earlier so that ShowTabHints() above doesn't overrite the announcement
            AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(
                Properties.Resources.FlMsgItemsRemoved + " (" + deletedCount.ToString() + ")", CommentBox.HintType.INFO);

            AppState.MainWin.ChaptersView.IsDirty = true;
            if (AppState.ActiveTab == TabViewType.CHAPTERS)
            {
                GuiUtilities.RefreshChaptersView(null);
                AppState.SetupGuiForCurrentStates();
                AppState.MainWin.UiTabChapters.Focus();
            }
            else if (AppState.ActiveTab == TabViewType.MODEL_GAME)
            {
                ChapterUtils.UpdateModelGamesView(AppState.Workbook.ActiveChapter);
            }
            else if (AppState.ActiveTab == TabViewType.EXERCISE)
            {
                ChapterUtils.UpdateExercisesView(AppState.Workbook.ActiveChapter);
            }
        }

        /// <summary>
        /// Build a "clean" list of articles to delete from the supplied list of ArticleListItems
        /// </summary>
        /// <param name="articleList"></param>
        /// <returns></returns>
        private static List<ArticleListItem> GetArticlesToDelete(ObservableCollection<ArticleListItem> articleList)
        {
            List<ArticleListItem> articlesToDelete = new List<ArticleListItem>();
            foreach (ArticleListItem item in articleList)
            {
                if (item.IsSelected == true && item.Article != null)
                {
                    articlesToDelete.Add(item);
                }
            }

            // sort "normally" as in some scenarios the sorting would have been messed up (e.g. Remove Duplicates)
            articlesToDelete.Sort(CompareArticlesNormal);

            return articlesToDelete;
        }

        /// <summary>
        /// Converts content Type to Delete operation type.
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private static WorkbookOperationType GetDeleteOpType(GameData.ContentType contentType)
        {
            WorkbookOperationType wot;

            switch (contentType)
            {
                case GameData.ContentType.MODEL_GAME:
                    wot = WorkbookOperationType.DELETE_MODEL_GAMES;
                    break;
                case GameData.ContentType.EXERCISE:
                    wot = WorkbookOperationType.DELETE_EXERCISES;
                    break;
                default:
                    wot = WorkbookOperationType.DELETE_ARTICLES;
                    break;
            }

            return wot;
        }

        /// <summary>
        /// Compares items for sorting as they would be placed in the Workbook.
        /// </summary>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <returns></returns>
        private static int CompareArticlesNormal(ArticleListItem item1, ArticleListItem item2)
        {
            int res = 0;

            if (item1 == null && item2 == null)
            {
                return 0;
            }
            if (item1 == null && item2 != null)
            {
                return -1;
            }
            if (item1 != null && item2 == null)
            {
                return 1;
            }

            if (item1.ChapterIndex != item2.ChapterIndex)
            {
                res = item1.ChapterIndex - item2.ChapterIndex;
            }
            else if (item1.ContentType == GameData.ContentType.MODEL_GAME && item2.ContentType != GameData.ContentType.MODEL_GAME)
            {
                res = -1;
            }
            else if (item1.ContentType != GameData.ContentType.MODEL_GAME && item2.ContentType == GameData.ContentType.MODEL_GAME)
            {
                res = 1;
            }

            else if (item1.ArticleIndex != item2.ArticleIndex)
            {
                res = item1.ArticleIndex - item2.ArticleIndex;
            }

            return res;
        }
    }
}

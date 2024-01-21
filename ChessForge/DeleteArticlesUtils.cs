using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ChessForge
{
    /// <summary>
    /// Utilities for deletion of articles.
    /// </summary>
    public class DeleteArticlesUtils
    {
        /// <summary>
        /// Deletes a list of articles of type MODEL_GAME or EXERCISE.
        /// Creates an Undo operation.
        /// 
        /// TODO: articleType is not needed here as DELETE_ARTICLES
        /// provides a method for deletions and undo of both kinds
        /// automatically. Same goes for Undo.
        /// </summary>
        /// <param name="articleList"></param>
        /// <param name="articleType"></param>
        public static void DeleteArticles(ObservableCollection<ArticleListItem> articleList, GameData.ContentType articleType = GameData.ContentType.GENERIC)
        {
            List<ArticleListItem> articlesToDelete = new List<ArticleListItem>();
            List<int> indicesToDelete = new List<int>();
            foreach (ArticleListItem item in articleList)
            {
                if (item.IsSelected && item.Article != null)
                {
                    articlesToDelete.Add(item);
                    indicesToDelete.Add(item.ArticleIndex);
                }
            }

            List<ArticleListItem> deletedArticles = new List<ArticleListItem>();
            List<int> deletedIndices = new List<int>();
            for (int i = 0; i < articlesToDelete.Count; i++)
            {
                ArticleListItem item = articlesToDelete[i];
                Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(item.ChapterIndex);
                if (chapter != null && item.Article != null)
                {
                    int index = chapter.GetArticleIndex(item.Article);
                    bool res = chapter.DeleteArticle(item.Article);
                    if (res)
                    {
                        deletedArticles.Add(item);
                        deletedIndices.Add(indicesToDelete[i]);
                    }
                }
            }

            if (deletedArticles.Count > 0)
            {
                WorkbookOperationType wot;

                switch (articleType)
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
                WorkbookOperation op = new WorkbookOperation(wot, null, -1, deletedArticles, deletedIndices);
                WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);

                AppState.MainWin.ChaptersView.IsDirty = true;
                AppState.IsDirty = true;

                if (AppState.ActiveVariationTree == null || AppState.CurrentEvaluationMode != EvaluationManager.Mode.CONTINUOUS)
                {
                    AppState.MainWin.StopEvaluation(true);
                    AppState.MainWin.BoardCommentBox.ShowTabHints();
                }

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
        }
    }
}

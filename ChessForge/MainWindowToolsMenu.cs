using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace ChessForge
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Invokes the Search Games dialog, collects user specified criteria
        /// executes the search and opens the Found Articles dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnFindGames_Click(object sender, RoutedEventArgs e)
        {
            FindGames.SearchForGames();
        }

        /// <summary>
        /// Deletes all comments and NAGs in the scope that the user will select. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiDeleteComments_Click(object sender, RoutedEventArgs e)
        {
            UiDeleteMoveAttributes(sender, e, MoveAttribute.COMMENT_AND_NAGS);
        }

        /// <summary>
        /// Deletes all engine evaluations in the scope that the user will select. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiDeleteEngineEvals_Click(object sender, RoutedEventArgs e)
        {
            UiDeleteMoveAttributes(sender, e, MoveAttribute.ENGINE_EVALUATION);
        }

        /// <summary>
        /// Deletes all comments from the currently shown tree. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiDeleteMoveAttributes(object sender, RoutedEventArgs e, MoveAttribute attrType)
        {
            string dlgTitle = "";
            switch (attrType)
            {
                case MoveAttribute.COMMENT_AND_NAGS:
                    dlgTitle = Properties.Resources.ScopeForDeleteComments;
                    break;
                case MoveAttribute.ENGINE_EVALUATION:
                    dlgTitle = Properties.Resources.ScopeForDeleteEvals;
                    break;
            }

            OperationScopeDialog dlg = new OperationScopeDialog(dlgTitle);
            GuiUtilities.PositionDialog(dlg, this, 100);
            if (dlg.ShowDialog() == true)
            {
                Dictionary<Article, List<MoveAttributes>> dictUndoData = new Dictionary<Article, List<MoveAttributes>>();

                if (dlg.ApplyScope == OperationScope.ACTIVE_ITEM)
                {
                    if (ActiveTreeView != null && AppState.IsTreeViewTabActive() && AppState.Workbook.ActiveArticle != null)
                    {
                        var list = DeleteMoveAttributesInArticle(AppState.Workbook.ActiveArticle, attrType);
                        if (list.Count > 0)
                        {
                            dictUndoData[AppState.Workbook.ActiveArticle] = list;
                        }
                    }
                }
                else if (dlg.ApplyScope == OperationScope.CHAPTER)
                {
                    DeleteMoveAttributesInChapter(attrType, AppState.ActiveChapter, dlg.ApplyToStudies, dlg.ApplyToGames, dlg.ApplyToExercises, dictUndoData);
                }
                else if (dlg.ApplyScope == OperationScope.WORKBOOK)
                {
                    foreach (Chapter chapter in AppState.Workbook.Chapters)
                    {
                        DeleteMoveAttributesInChapter(attrType, chapter, dlg.ApplyToStudies, dlg.ApplyToGames, dlg.ApplyToExercises, dictUndoData);
                    }
                }

                if (ActiveTreeView != null && AppState.IsTreeViewTabActive())
                {
                    switch (attrType)
                    {
                        case MoveAttribute.COMMENT_AND_NAGS:
                            ActiveTreeView.BuildFlowDocumentForVariationTree();
                            break;
                        case MoveAttribute.ENGINE_EVALUATION:
                            // there may have been "assessments" so need to refresh this
                            ActiveTreeView.BuildFlowDocumentForVariationTree();
                            ActiveLine.RefreshNodeList();
                            break;
                    }
                }

                if (dictUndoData.Keys.Count > 0)
                {
                    WorkbookOperationType wot = WorkbookOperationType.NONE;
                    switch (attrType)
                    {
                        case MoveAttribute.COMMENT_AND_NAGS:
                            wot = WorkbookOperationType.DELETE_COMMENTS;    
                            break;
                        case MoveAttribute.ENGINE_EVALUATION:
                            wot = WorkbookOperationType.DELETE_ENGINE_EVALS;
                            break;
                    }
                    WorkbookOperation op = new WorkbookOperation(wot, dictUndoData);
                    AppState.Workbook.OpsManager.PushOperation(op);

                    AppState.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Deletes move attributes of the specified type from the Article.
        /// Returns the list of removed comments for the Undo operation.
        /// </summary>
        /// <param name="article"></param>
        /// <returns></returns>
        private List<MoveAttributes> DeleteMoveAttributesInArticle(Article article, MoveAttribute attrType)
        {
            List<MoveAttributes> attrsList = new List<MoveAttributes>();

            switch (attrType)
            {
                case MoveAttribute.COMMENT_AND_NAGS:
                    attrsList = TreeUtils.BuildNagsAndCommentsList(article.Tree);
                    article.Tree.DeleteCommentsAndNags();
                    break;
                case MoveAttribute.ENGINE_EVALUATION:
                    attrsList = TreeUtils.BuildEngineEvalList(article.Tree);
                    article.Tree.DeleteEvalsAndAssessments();
                    break;
            }

            return attrsList;
        }

        /// <summary>
        /// Deletes move attributes of the specified type from all articles in a chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="study"></param>
        /// <param name="games"></param>
        /// <param name="exercises"></param>
        private void DeleteMoveAttributesInChapter(MoveAttribute attrType, 
            Chapter chapter, 
            bool study, 
            bool games, 
            bool exercises, 
            Dictionary<Article, List<MoveAttributes>> dict)
        {
            if (chapter != null)
            {
                if (study)
                {
                    var list = DeleteMoveAttributesInArticle(chapter.StudyTree, attrType);
                    if (list.Count > 0)
                    {
                        dict[chapter.StudyTree] = list;
                    }
                }
                if (games)
                {
                    foreach (Article game in chapter.ModelGames)
                    {
                        var list = DeleteMoveAttributesInArticle(game, attrType);
                        if (list.Count > 0)
                        {
                            dict[game] = list;
                        }
                    }
                }
                if (exercises)
                {
                    foreach (Article exercise in chapter.Exercises)
                    {
                        var list = DeleteMoveAttributesInArticle(exercise, attrType);
                        if (list.Count > 0)
                        {
                            dict[exercise] = list;
                        }
                    }
                }
            }
        }


        //########################################################################################


        /// <summary>
        /// Opens the dialog for selecting and evaluating games
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnEvaluateGames_Click(object sender, RoutedEventArgs e)
        {
            if (TrainingSession.IsTrainingInProgress)
            {
                GuiUtilities.ShowExitTrainingInfoMessage();
            }
            else
            {
                // do not track location changes as we evaluate game after game 
                WorkbookLocationNavigator.IsNavigationTrackingOn = false;

                try
                {
                    ObservableCollection<ArticleListItem> gameList = WorkbookManager.SessionWorkbook.GenerateArticleList(null, GameData.ContentType.MODEL_GAME);

                    string title = Properties.Resources.EvaluateGames;
                    SelectArticlesDialog dlg = new SelectArticlesDialog(null, true, title, ref gameList, false, ArticlesAction.NONE, GameData.ContentType.MODEL_GAME);
                    GuiUtilities.PositionDialog(dlg, this, 100);
                    dlg.SetupGuiForGamesEval();

                    if (dlg.ShowDialog() == true)
                    {
                        if (double.TryParse(dlg.UiTbEngEvalTime.Text, out double dval))
                        {
                            Configuration.EngineEvaluationTime = (int)(dval * 1000);
                        }

                        ObservableCollection<ArticleListItem> gamesToEvaluate = new ObservableCollection<ArticleListItem>();
                        foreach (ArticleListItem item in gameList)
                        {
                            if (item.ContentType == GameData.ContentType.MODEL_GAME && item.IsSelected)
                            {
                                gamesToEvaluate.Add(item);
                            }
                        }
                        if (gamesToEvaluate.Count > 0)
                        {
                            GamesEvaluationManager.InitializeProcess(gamesToEvaluate);
                        }
                    }
                }
                catch { }
            }

            WorkbookLocationNavigator.IsNavigationTrackingOn = true;
            e.Handled = true;
        }

        /// <summary>
        /// Invokes the dialog to select Games to delete and deletes them. 
        /// The initial state of the dialog will be to show Games from the active chapter only.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDeleteGames_Click(object sender, RoutedEventArgs e)
        {
            DeleteArticles(GameData.ContentType.MODEL_GAME, false);
        }

        /// <summary>
        /// Invokes the dialog to select Games to delete and deletes them. 
        /// The initial state of the dialog will be to show Games from all chapters.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnToolsDeleteGames_Click(object sender, RoutedEventArgs e)
        {
            DeleteArticles(GameData.ContentType.MODEL_GAME, false);
        }

        /// <summary>
        /// Invokes the dialog to select Exercises to delete and deletes them. 
        /// The initial state of the dialog will be to show Exercises from the active chapter only.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDeleteExercises_Click(object sender, RoutedEventArgs e)
        {
            DeleteArticles(GameData.ContentType.EXERCISE, false);
        }

        /// <summary>
        /// Invokes the dialog to select Exercises to delete and deletes them. 
        /// The initial state of the dialog will be to show Exercises from all chapters.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnToolsDeleteExercises_Click(object sender, RoutedEventArgs e)
        {
            DeleteArticles(GameData.ContentType.EXERCISE, false);
        }

        /// <summary>
        /// Deletes articles passed in the article list
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="articleType"></param>
        private void DeleteArticles(GameData.ContentType articleType, bool allChapters)
        {
            try
            {
                ObservableCollection<ArticleListItem> articleList = WorkbookManager.SessionWorkbook.GenerateArticleList(null, articleType);

                string title = null;
                if (articleType == GameData.ContentType.MODEL_GAME)
                {
                    title = Properties.Resources.SelectGamesForDeletion;
                }
                else if (articleType == GameData.ContentType.EXERCISE)
                {
                    title = Properties.Resources.SelectExercisesForDeletion;
                }

                SelectArticlesDialog dlg = new SelectArticlesDialog(null, true, title, ref articleList, allChapters, ArticlesAction.NONE, articleType);
                GuiUtilities.PositionDialog(dlg, this, 100);
                if (dlg.ShowDialog() == true)
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
                        WorkbookOperationType wot =
                            articleType == GameData.ContentType.MODEL_GAME ? WorkbookOperationType.DELETE_MODEL_GAMES : WorkbookOperationType.DELETE_EXERCISES;
                        WorkbookOperation op = new WorkbookOperation(wot, null, -1, deletedArticles, deletedIndices);
                        WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);

                        AppState.MainWin.ChaptersView.IsDirty = true;
                        AppState.IsDirty = true;

                        if (ActiveVariationTree == null || AppState.CurrentEvaluationMode != EvaluationManager.Mode.CONTINUOUS)
                        {
                            StopEvaluation(true);
                            BoardCommentBox.ShowTabHints();
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
            catch { }
        }
    }
}

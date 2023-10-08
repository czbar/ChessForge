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
        /// Deletes all comments from the currently shown tree. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiDeleteComments_Click(object sender, RoutedEventArgs e)
        {
            OperationScopeDialog dlg = new OperationScopeDialog(Properties.Resources.ScopeForDeleteComments);
            if (dlg.ShowDialog() == true)
            {
                Dictionary<Article, List<NagsAndComment>> dictUndoData = new Dictionary<Article, List<NagsAndComment>>();

                if (dlg.ApplyScope == OperationScope.ACTIVE_ITEM)
                {
                    if (ActiveTreeView != null && AppState.IsTreeViewTabActive() && AppState.Workbook.ActiveArticle != null)
                    {
                        var list = DeleteCommentsInArticle(AppState.Workbook.ActiveArticle);
                        if (list.Count > 0)
                        {
                            dictUndoData[AppState.Workbook.ActiveArticle] = list;
                        }
                    }
                }
                else if (dlg.ApplyScope == OperationScope.CHAPTER)
                {
                    DeleteCommentsInChapter(AppState.ActiveChapter, dlg.ApplyToStudies, dlg.ApplyToGames, dlg.ApplyToExercises, dictUndoData);
                }
                else if (dlg.ApplyScope == OperationScope.WORKBOOK)
                {
                    foreach (Chapter chapter in AppState.Workbook.Chapters)
                    {
                        DeleteCommentsInChapter(chapter, dlg.ApplyToStudies, dlg.ApplyToGames, dlg.ApplyToExercises, dictUndoData);
                    }
                }

                if (ActiveTreeView != null && AppState.IsTreeViewTabActive())
                {
                    ActiveTreeView.BuildFlowDocumentForVariationTree();
                }

                if (dictUndoData.Keys.Count > 0)
                {
                    WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.DELETE_COMMENTS, dictUndoData);
                    AppState.Workbook.OpsManager.PushOperation(op);

                    AppState.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Deletes comments from an article.
        /// Returns the list of removed comments for the Undo operation.
        /// </summary>
        /// <param name="article"></param>
        /// <returns></returns>
        private List<NagsAndComment> DeleteCommentsInArticle(Article article)
        {
            List<NagsAndComment> comments = TreeUtils.BuildNagsAndCommentsList(article.Tree);
            article.Tree.DeleteCommentsAndNags();
            return comments;
        }

        /// <summary>
        /// Deletes comments from all articles in a chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="study"></param>
        /// <param name="games"></param>
        /// <param name="exercises"></param>
        private void DeleteCommentsInChapter(Chapter chapter, bool study, bool games, bool exercises, Dictionary<Article, List<NagsAndComment>> dict)
        {
            if (chapter != null)
            {
                if (study)
                {
                    var list = DeleteCommentsInArticle(chapter.StudyTree);
                    if (list.Count > 0)
                    {
                        dict[chapter.StudyTree] = list;
                    }
                }
                if (games)
                {
                    foreach (Article game in chapter.ModelGames)
                    {
                        var list = DeleteCommentsInArticle(game);
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
                        var list = DeleteCommentsInArticle(exercise);
                        if (list.Count > 0)
                        {
                            dict[exercise] = list;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Deletes all engine evaluations from the currently shown tree. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiDeleteEngineEvals_Click(object sender, RoutedEventArgs e)
        {
            OperationScopeDialog dialog = new OperationScopeDialog(Properties.Resources.ScopeForDeleteEvals);
            dialog.ShowDialog();

            if (ActiveTreeView != null && AppState.IsTreeViewTabActive())
            {
                if (MessageBox.Show(Properties.Resources.MsgConfirmDeleteEngineEvals, Properties.Resources.Confirm,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    VariationTree tree = ActiveTreeView.ShownVariationTree;

                    // get data for the Undo operation first
                    List<EvalAndAssessment> evals = TreeUtils.BuildEngineEvalList(tree);
                    EditOperation op = new EditOperation(EditOperation.EditType.DELETE_ENGINE_EVALS, evals, null);
                    tree.OpsManager.PushOperation(op);

                    tree.DeleteEvalsAndAssessments();
                    ActiveLine.DeleteEngineEvaluations();
                    AppState.IsDirty = true;

                    ActiveTreeView.BuildFlowDocumentForVariationTree();
                }
            }
        }

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
                try
                {
                    ObservableCollection<ArticleListItem> gameList = WorkbookManager.SessionWorkbook.GenerateArticleList(null, GameData.ContentType.MODEL_GAME);

                    string title = Properties.Resources.EvaluateGames;
                    SelectArticlesDialog dlg = new SelectArticlesDialog(null, false, title, ref gameList, true, GameData.ContentType.MODEL_GAME)
                    {
                        Left = ChessForgeMain.Left + 100,
                        Top = ChessForgeMain.Top + 100,
                        Topmost = false,
                        Owner = this
                    };
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
                            if (item.IsSelected)
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
            DeleteArticles(GameData.ContentType.MODEL_GAME, true);
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
            DeleteArticles(GameData.ContentType.EXERCISE, true);
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

                SelectArticlesDialog dlg = new SelectArticlesDialog(null, true, title, ref articleList, allChapters, articleType)
                {
                    Left = ChessForgeMain.Left + 100,
                    Top = ChessForgeMain.Top + 100,
                    Topmost = false,
                    Owner = this
                };
                if (dlg.ShowDialog() == true)
                {
                    List<ArticleListItem> articlesToDelete = new List<ArticleListItem>();
                    List<int> indicesToDelete = new List<int>();
                    foreach (ArticleListItem item in articleList)
                    {
                        if (item.IsSelected)
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
                        if (chapter != null)
                        {
                            int index = chapter.GetArticleIndex(item.Article);
                            bool res = chapter.DeleteArticle(item.Article);
                            if (res)
                            {
                                deletedArticles.Add(item);
                                deletedIndices.Add(indicesToDelete[index]);
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
                        GuiUtilities.RefreshChaptersView(null);
                        AppState.MainWin.UiTabChapters.Focus();
                    }
                }
            }
            catch { }
        }
    }
}

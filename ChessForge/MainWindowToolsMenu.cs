using ChessPosition;
using GameTree;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace ChessForge
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Sets the font size for the main menu and all context menus.
        /// </summary>
        /// <param name="size"></param>
        private void SetMenuFontSize(double size)
        {
            UiMainMenu.FontSize = size;

            UiMncMainBoard.FontSize = size;
            UiMncTrainingView.FontSize = size;
            UiMncEngineGame.FontSize = size;
            UiMncChapters.FontSize = size;
            UiMncIntro.FontSize = size;
            UiMncStudyTree.FontSize = size;
            UiMncModelGames.FontSize = size;
            UiMncExercises.FontSize = size;
            UiMncBookmarks.FontSize = size;
            UiMncTopGames.FontSize = size;

            ContextMenus.SetMenuFontSize(size);

            UiLblAutoSave.FontSize = size;
            UiLblExplorers.FontSize = size;
            UiLblEngine.FontSize = size;
        }

        /// <summary>
        /// Invokes the Search Games dialog, collects user specified criteria
        /// executes the search and opens the Found Articles dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnFindGames_Click(object sender, RoutedEventArgs e)
        {
            FindGames.SearchForGames();
        }

        /// <summary>
        /// Deletes user-selected items: comments, engine evaluations, sidelines. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCleanLinesAndComments_Click(object sender, RoutedEventArgs e)
        {
            UiCleanLinesAndComments(sender, e);
        }

        /// <summary>
        /// Lets the user the select the scope and figure out ECOs in that scope
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiAssignEco_Click(object sender, RoutedEventArgs e)
        {
            Tools.UiAssignEcoToArticles();
        }

        /// <summary>
        /// Invokes a dialog allowing the user to select the scope 
        /// and type of notes to delete.
        /// The "notes" can be comments (before and after moves), engine evaluations
        /// and sidelines.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCleanLinesAndComments(object sender, RoutedEventArgs e)
        {
            CleanSidelinesCommentsDialog dlg = new CleanSidelinesCommentsDialog();
            GuiUtilities.PositionDialog(dlg, this, 100);

            if (dlg.ShowDialog() == true && (dlg.MoveAttrsFlags != 0 || dlg.ArticleAttrsFlags != 0))
            {
                CleanSidelinesComments.CleanLinesAndComments(dlg.Scope, dlg.MoveAttrsFlags, dlg.ArticleAttrsFlags,
                                                             dlg.ApplyToStudies, dlg.ApplyToGames, dlg.ApplyToExercises);
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
                    SelectArticlesDialog dlg = new SelectArticlesDialog(null, true, title, ref gameList, false, ArticlesAction.EVALUATE, GameData.ContentType.MODEL_GAME);
                    GuiUtilities.PositionDialog(dlg, this, 100);
                    dlg.SetupGuiForGamesEval();

                    if (dlg.ShowDialog() == true)
                    {
                        if (double.TryParse(dlg.UiTbEngEvalTime.Text, out double dval))
                        {
                            Configuration.EngineEvaluationTime = (int)(dval * 1000);
                        }

                        int ivalFrom;
                        if (int.TryParse(dlg.UiTbFromMove.Text, out ivalFrom))
                        {
                            Configuration.EvalMoveRangeStart = ivalFrom;
                        }
                        else if (string.IsNullOrEmpty(dlg.UiTbFromMove.Text))
                        {
                            Configuration.EvalMoveRangeStart = 0;
                        }

                        int ivalTo;
                        if (int.TryParse(dlg.UiTbToMove.Text, out ivalTo))
                        {
                            Configuration.EvalMoveRangeEnd = ivalTo;
                        }
                        else if (string.IsNullOrEmpty(dlg.UiTbToMove.Text))
                        {
                            Configuration.EvalMoveRangeEnd = 0;
                        }

                        ObservableCollection<ArticleListItem> gamesToEvaluate = new ObservableCollection<ArticleListItem>();
                        foreach (ArticleListItem item in gameList)
                        {
                            if (item.ContentType == GameData.ContentType.MODEL_GAME && item.IsSelected == true)
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
        /// Calls the function to identify and select duplicates for removal.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnRemoveDuplicates_Click(object sender, RoutedEventArgs e)
        {
            FindDuplicates.FindDuplicateArticles(null);
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

                SelectArticlesDialog dlg = new SelectArticlesDialog(null, true, title, ref articleList, allChapters, ArticlesAction.DELETE, articleType);
                GuiUtilities.PositionDialog(dlg, this, 100);
                if (dlg.ShowDialog() == true)
                {
                    DeleteArticlesUtils.DeleteArticleListItems(articleList, articleType);
                }
            }
            catch { }
        }
    }
}

using ChessPosition;
using GameTree;
using System.Windows;

namespace ChessForge
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// This method is called from the Main Menu 
        /// and it will offer to import both Games and Exercises.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnMainImportFromPgn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chapter startChapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                int origGameCount = startChapter.GetModelGameCount();
                int origExerciseCount = startChapter.GetExerciseCount();

                int importedArticles = ImportFromPgn.ImportArticlesFromPgn(GameData.ContentType.GENERIC, GameData.ContentType.ANY, out int gameCount, out int exerciseCount);
                if (importedArticles > 0)
                {
                    AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(
                        Properties.Resources.FlMsgArticlesImported + " (" + importedArticles.ToString() + ")", CommentBox.HintType.INFO);

                    Chapter currChapter = WorkbookManager.SessionWorkbook.ActiveChapter;

                    // if we did not create a new chapter, adjust index to the first imported game.
                    if (currChapter == startChapter)
                    {
                        if (startChapter.GetModelGameCount() > origGameCount)
                        {
                            startChapter.ActiveModelGameIndex = origGameCount;
                        }
                        if (startChapter.GetExerciseCount() > origExerciseCount)
                        {
                            startChapter.ActiveExerciseIndex = origExerciseCount;
                        }

                        if (AppState.ActiveTab == TabViewType.MODEL_GAME && gameCount > 0)
                        {
                            SelectModelGame(startChapter.ActiveModelGameIndex, false);
                        }
                        else if (AppState.ActiveTab == TabViewType.EXERCISE && exerciseCount > 0)
                        {
                            SelectExercise(startChapter.ActiveExerciseIndex, false);
                        }
                    }
                    else
                    // we have created a new chapter
                    {
                        // refresh the current view if we are in the GAMES or EXERCISES tab
                        if (AppState.ActiveTab == TabViewType.MODEL_GAME)
                        {
                            RefreshGamesView(out _, out _);
                        }
                        else if (AppState.ActiveTab == TabViewType.EXERCISE)
                        {
                            RefreshExercisesView(out _, out _);
                        }
                        else if (AppState.ActiveTab == TabViewType.STUDY || AppState.ActiveTab == TabViewType.INTRO || AppState.ActiveTab == TabViewType.CHAPTERS)
                        {
                            // select the chapter if in STUDY, INTRO or CHAPTERS tab
                            SelectChapterByIndex(currChapter.Index, true);
                            if (gameCount > 0)
                            {
                                SelectModelGame(currChapter.ActiveModelGameIndex, true);
                            }
                            else if (exerciseCount > 0)
                            {
                                SelectExercise(currChapter.ActiveExerciseIndex, true);
                            }
                        }
                    }

                    int firstImportedGameIndex = -1;
                    if (gameCount > 0)
                    {
                        firstImportedGameIndex = currChapter.ActiveModelGameIndex;
                    }
                    else if (exerciseCount > 0)
                    {
                        firstImportedGameIndex = currChapter.ActiveExerciseIndex;
                    }

                    RefreshChaptersViewAfterImport(gameCount, exerciseCount, currChapter, firstImportedGameIndex);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Invoked from the chapters context menus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnImportModelGames_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                int count = chapter.GetModelGameCount();

                int importedGames = ImportFromPgn.ImportArticlesFromPgn(GameData.ContentType.MODEL_GAME, GameData.ContentType.MODEL_GAME, out _, out _);
                if (importedGames > 0)
                {
                    if (chapter != WorkbookManager.SessionWorkbook.ActiveChapter)
                    {
                        chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                        SelectChapterByIndex(chapter.Index, false, false);
                    }

                    AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(
                        Properties.Resources.FlMsgGamesImported + " (" + importedGames.ToString() + ")", CommentBox.HintType.INFO);
                    if (chapter.GetModelGameCount() > count)
                    {
                        chapter.ActiveModelGameIndex = count;
                    }
                    else
                    {
                        chapter.ActiveModelGameIndex = count - 1;
                    }

                    if (AppState.ActiveTab != TabViewType.CHAPTERS)
                    {
                        SelectModelGame(chapter.ActiveModelGameIndex, false);
                    }

                    RefreshChaptersViewAfterImport(importedGames, 0, chapter, chapter.ActiveModelGameIndex);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Invoked from the context menus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnImportExercises_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                int count = chapter.GetExerciseCount();

                int importedExercises = ImportFromPgn.ImportArticlesFromPgn(GameData.ContentType.EXERCISE, GameData.ContentType.EXERCISE, out _, out _);
                if (importedExercises > 0)
                {
                    if (chapter != WorkbookManager.SessionWorkbook.ActiveChapter)
                    {
                        chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                        SelectChapterByIndex(chapter.Index, false, false);
                    }

                    AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(
                        Properties.Resources.FlMsgExercisesImported + " (" + importedExercises.ToString() + ")", CommentBox.HintType.INFO);
                    if (chapter.GetExerciseCount() > count)
                    {
                        chapter.ActiveExerciseIndex = count;
                    }
                    else
                    {
                        chapter.ActiveExerciseIndex = count - 1;
                    }

                    if (AppState.ActiveTab != TabViewType.CHAPTERS)
                    {
                        SelectExercise(chapter.ActiveExerciseIndex, false);
                    }

                    RefreshChaptersViewAfterImport(importedExercises, 0, chapter, chapter.ActiveExerciseIndex);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Sets the list in the Chapters View in the correct Expand/Collapse state.
        /// Rebuilds the Paragraph for the chapter.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="chapter"></param>
        private void RefreshChaptersViewAfterImport(int gameCount, int exerciseCount, Chapter chapter, int gameUinitIndex)
        {
            chapter.IsViewExpanded = true;
            if (gameCount > 0)
            {
                chapter.IsModelGamesListExpanded = true;
            }
            if (exerciseCount > 0)
            {
                chapter.IsExercisesListExpanded = true;
            }

            GameData.ContentType contentType = GameData.ContentType.MODEL_GAME;
            if (gameCount == 0 && exerciseCount > 0)
            {
                contentType = GameData.ContentType.EXERCISE;
            }

            if (AppState.ActiveTab == TabViewType.CHAPTERS)
            {
                _chaptersView.BuildFlowDocumentForChaptersView(false);
                _chaptersView.BringArticleIntoView(_chaptersView.HostRtb.Document, chapter.Index, contentType, gameUinitIndex);
            }
            else
            {
                _chaptersView.IsDirty = true;
            }
        }

    }
}

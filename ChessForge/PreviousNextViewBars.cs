using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Sets up values and visibility of the elements in the Previous/Next bar
    /// for different views.
    /// </summary>
    public class PreviousNextViewBars
    {
        /// <summary>
        /// Populates or hides the Previous/Next game/exercise bar above the tree view
        /// as appropriate.
        /// </summary>
        /// <param name="contentType"></param>
        public static void BuildPreviousNextBar(GameData.ContentType contentType)
        {
            try
            {
                switch (contentType)
                {
                    case GameData.ContentType.STUDY_TREE:
                        BuildPreviousNextChapterBar();
                        break;
                    case GameData.ContentType.INTRO:
                        //BuildPreviousNextIntroBar();
                        BuildPreviousNextChapterBar();
                        break;
                    case GameData.ContentType.MODEL_GAME:
                        BuildPreviousNextModelGameBar();
                        BuildPreviousNextChapterBar();
                        break;
                    case GameData.ContentType.EXERCISE:
                        BuildPreviousNextExerciseBar();
                        BuildPreviousNextChapterBar();
                        break;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Builds the Previous/Next bar for Chapter/Study Tree view.
        /// </summary>
        private static void BuildPreviousNextChapterBar()
        {
            MainWindow mainWin = AppState.MainWin;

            int chapterCount = 0;
            int chapterIndex = -1;

            if (WorkbookManager.SessionWorkbook != null)
            {
                chapterCount = WorkbookManager.SessionWorkbook.GetChapterCount();
                chapterIndex = WorkbookManager.SessionWorkbook.ActiveChapterIndex;
            }

            if (chapterCount >= 1)
            {
                SetupElements(mainWin.UiImgChapterLeftArrow,
                              mainWin.UiImgChapterRightArrow,
                              mainWin.UiLblExerciseCounter,
                              "Chapter",
                              chapterIndex,
                              chapterCount);
            }
        }

        /// <summary>
        /// Builds the Previous/Next bar for Model Games view.
        /// </summary>
        private static void BuildPreviousNextModelGameBar()
        {
            MainWindow mainWin = AppState.MainWin;

            int gameCount = 0;
            int gameIndex = -1;

            if (WorkbookManager.SessionWorkbook != null && WorkbookManager.SessionWorkbook.ActiveChapter != null)
            {
                gameCount = WorkbookManager.SessionWorkbook.ActiveChapter.GetModelGameCount();
                gameIndex = WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex;
            }

            if (gameCount > 0)
            {
                SetupElements(mainWin.UiImgModelGameLeftArrow,
                              mainWin.UiImgModelGameRightArrow,
                              mainWin.UiLblExerciseCounter,
                              "Game",
                              gameIndex,
                              gameCount);
            }
        }

        /// <summary>
        /// Builds the Previous/Next bar for the Exercises view.
        /// </summary>
        private static void BuildPreviousNextExerciseBar()
        {
            MainWindow mainWin = AppState.MainWin;

            int exerciseCount = 0;
            int exerciseIndex = -1;

            if (WorkbookManager.SessionWorkbook != null && WorkbookManager.SessionWorkbook.ActiveChapter != null)
            {
                exerciseCount = WorkbookManager.SessionWorkbook.ActiveChapter.GetExerciseCount();
                exerciseIndex = WorkbookManager.SessionWorkbook.ActiveChapter.ActiveExerciseIndex;
            }

            if (exerciseCount > 0)
            {
                SetupElements(mainWin.UiImgExerciseLeftArrow,
                              mainWin.UiImgExerciseRightArrow,
                              mainWin.UiLblExerciseCounter,
                              "Exercise",
                              exerciseIndex,
                              exerciseCount);
            }
        }

        /// <summary>
        /// Set up values and visibility of the GUI elements
        /// </summary>
        /// <param name="imgLeftArrow"></param>
        /// <param name="imgRightArrow"></param>
        /// <param name="lblCounter"></param>
        /// <param name="itemType"></param>
        /// <param name="itemIndex"></param>
        /// <param name="itemCount"></param>
        private static void SetupElements(Image imgLeftArrow,
                                          Image imgRightArrow,
                                          Label lblCounter,
                                          string itemType,
                                          int itemIndex,
                                          int itemCount)
        {
            if (itemCount > 0)
            {
                string counter = ResourceUtils.GetCounterBarText(itemType, itemIndex, itemCount);
                lblCounter.Content = counter;

                imgRightArrow.Visibility = Visibility.Visible;
                imgLeftArrow.Visibility = Visibility.Visible;

                if (itemIndex == 0)
                {
                    imgLeftArrow.Visibility = Visibility.Hidden;
                }

                if (itemIndex == itemCount - 1)
                {
                    imgRightArrow.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                lblCounter.Visibility = Visibility.Collapsed;
                imgRightArrow.Visibility = Visibility.Collapsed;
                imgLeftArrow.Visibility = Visibility.Collapsed;
            }
        }
    }
}

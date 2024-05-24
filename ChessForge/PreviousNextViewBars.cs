using GameTree;
using System;
using System.Collections.Generic;
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
        /// Populates the Previous/Next bar above the main view
        /// as appropriate.
        /// </summary>
        /// <param name="contentType"></param>
        public static void BuildPreviousNextBar(GameData.ContentType contentType)
        {
            try
            {
                Chapter chapter = AppState.ActiveChapter;
                switch (contentType)
                {
                    case GameData.ContentType.STUDY_TREE:
                    case GameData.ContentType.INTRO:
                        break;
                    case GameData.ContentType.MODEL_GAME:
                        int gameIndex = chapter == null ? -1 : chapter.ActiveModelGameIndex;
                        SetModelGameCounterControls(gameIndex);
                        break;
                    case GameData.ContentType.EXERCISE:
                        int exerciseIndex = chapter == null ? -1 : chapter.ActiveExerciseIndex;
                        SetExerciseCounterControls(exerciseIndex);
                        break;
                }

                int chapterIndex = chapter == null ? -1 : WorkbookManager.SessionWorkbook.ActiveChapterIndex;
                SetChapterCounterControls(contentType, chapterIndex);
            }
            catch
            {
            }
        }


        /// <summary>
        /// Sets the chapter counter controls
        /// </summary>
        /// <param name="contentType"></param>
        public static void SetChapterCounterControls(GameData.ContentType contentType, int chapterIndex)
        {
            int chapterCount = 0;

            if (WorkbookManager.SessionWorkbook != null)
            {
                chapterCount = WorkbookManager.SessionWorkbook.GetChapterCount();
            }

            if (GetChapterCounterControls(contentType, out Label lblTitle, out Image imgLeftArrow, out Image imgRightArrow, out Label lblCounter))
            {
                SetupElements(lblTitle,
                              imgLeftArrow,
                              imgRightArrow,
                              lblCounter,
                              "Chapter",  //TODO: using a string here is weird, switch to using ContentType
                              chapterIndex,
                              chapterCount);
            }
        }

        /// <summary>
        /// Builds the Previous/Next bar for Model Games view.
        /// </summary>
        public static void SetModelGameCounterControls(int gameIndex)
        {
            MainWindow mainWin = AppState.MainWin;

            int gameCount = 0;

            if (WorkbookManager.SessionWorkbook != null && WorkbookManager.SessionWorkbook.ActiveChapter != null)
            {
                gameCount = WorkbookManager.SessionWorkbook.ActiveChapter.GetModelGameCount();
            }

            SetupElements(mainWin.UiGamesLblChapterTitle,
                          mainWin.UiImgModelGameLeftArrow,
                          mainWin.UiImgModelGameRightArrow,
                          mainWin.UiLblGameCounter,
                          "Game",
                          gameIndex,
                          gameCount);
        }

        /// <summary>
        /// Builds the Previous/Next bar for the Exercises view.
        /// </summary>
        public static void SetExerciseCounterControls(int exerciseIndex)
        {
            MainWindow mainWin = AppState.MainWin;

            int exerciseCount = 0;

            if (WorkbookManager.SessionWorkbook != null && WorkbookManager.SessionWorkbook.ActiveChapter != null)
            {
                exerciseCount = WorkbookManager.SessionWorkbook.ActiveChapter.GetExerciseCount();
            }

            SetupElements(mainWin.UiExerciseLblChapterTitle,
                          mainWin.UiImgExerciseLeftArrow,
                          mainWin.UiImgExerciseRightArrow,
                          mainWin.UiLblExerciseCounter,
                          "Exercise",
                          exerciseIndex,
                          exerciseCount);
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
        private static void SetupElements(Label lblTitle,
                                          Image imgLeftArrow,
                                          Image imgRightArrow,
                                          Label lblCounter,
                                          string itemType,
                                          int itemIndex,
                                          int itemCount)
        {
            string fullTitle = AppState.ActiveChapter == null ? "" : AppState.ActiveChapter.GetTitle();
            string titleForLabel = GuiUtilities.AdjustTextToFit(lblTitle, fullTitle);
            lblTitle.Content = titleForLabel;
            lblTitle.ToolTip = fullTitle;
            lblCounter.ToolTip = fullTitle;

            if (itemCount > 0)
            {
                string counter = ResourceUtils.GetCounterBarText(itemType, itemIndex, itemCount);
                lblCounter.Content = counter;

                imgRightArrow.Visibility = Visibility.Visible;
                imgLeftArrow.Visibility = Visibility.Visible;
                lblCounter.Visibility = Visibility.Visible;

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


        /// <summary>
        /// Returns the controls to use for the chapter counter.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="imgLeftArrow"></param>
        /// <param name="imgRightArrow"></param>
        /// <param name="lblCounter"></param>
        /// <returns></returns>
        private static bool GetChapterCounterControls(GameData.ContentType contentType, out Label lblTitle, out Image imgLeftArrow, out Image imgRightArrow, out Label lblCounter)
        {
            bool res = true;
            imgLeftArrow = null;
            imgRightArrow = null;
            lblCounter = null;
            lblTitle = null;

            MainWindow mainWin = AppState.MainWin;

            switch (contentType)
            {
                case GameData.ContentType.INTRO:
                    imgLeftArrow = mainWin.UiIntroImgLeftArrowChapter;
                    imgRightArrow = mainWin.UiIntroImgRightArrowChapter;
                    lblCounter = mainWin.UiIntroLblCounterChapter;
                    lblTitle = mainWin.UiIntroLblChapterTitle;
                    break;
                case GameData.ContentType.STUDY_TREE:
                    imgLeftArrow = mainWin.UiImgChapterLeftArrow;
                    imgRightArrow = mainWin.UiImgChapterRightArrow;
                    lblCounter = mainWin.UiLblChapterCounter;
                    lblTitle = mainWin.UiStudyLblChapterTitle;
                    break;
                case GameData.ContentType.MODEL_GAME:
                    imgLeftArrow = mainWin.UiGamesImgLeftArrowChapter;
                    imgRightArrow = mainWin.UiGamesImgRightArrowChapter;
                    lblCounter = mainWin.UiGamesLblCounterChapter;
                    lblTitle = mainWin.UiGamesLblChapterTitle;
                    break;
                case GameData.ContentType.EXERCISE:
                    imgLeftArrow = mainWin.UiExerciseImgLeftArrowChapter;
                    imgRightArrow = mainWin.UiExerciseImgRightArrowChapter;
                    lblCounter = mainWin.UiExerciseLblCounterChapter;
                    lblTitle = mainWin.UiExerciseLblChapterTitle;
                    break;
                default:
                    res = false;
                    break;
            }

            return res;
        }
    }
}

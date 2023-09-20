using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Utilities to manipulate chapter objects.
    /// </summary>
    public class ChapterUtils
    {
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

                    AppState.MainWin.UiTabChapters.Focus();
                    AppState.MainWin.ChaptersView.BringChapterIntoViewByIndex(targetChapterIndex);
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

                    AppState.MainWin.UiTabChapters.Focus();
                    AppState.MainWin.ChaptersView.BringChapterIntoViewByIndex(targetChapterIndex);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("MoveExerciseBetweenChapters()", ex);
            }

            return targetChapterIndex;
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


    }
}

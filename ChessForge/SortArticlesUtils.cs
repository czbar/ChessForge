using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace ChessForge
{
    public class SortArticlesUtils
    {

        /// <summary>
        /// Invokes the dialog for sorting games in a chapter/workbook.
        /// </summary>
        /// <param name="chapter"></param>
        public static void InvokeSortGamesDialog(Chapter chapter)
        {
            if (chapter != null)
            {
                try
                {
                    SortGamesDialog dlg = new SortGamesDialog(chapter);
                    GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                    if (dlg.ShowDialog() == true && dlg.SortGamesBy != GameSortCriterion.SortItem.NONE)
                    {
                        var presortOrder = new Dictionary<int, List<Article>>();

                        if (dlg.ApplyToAllChapters)
                        {
                            WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.SORT_GAMES, null, (object)presortOrder);
                            foreach (Chapter ch in AppState.Workbook.Chapters)
                            {
                                StorePresortGamesOrder(ch, presortOrder);
                            }
                            SortGames(null, dlg.SortGamesBy, dlg.SortGamesDirection);
                            WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
                        }
                        else
                        {
                            WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.SORT_GAMES, chapter, (object)presortOrder);
                            StorePresortGamesOrder(chapter, presortOrder);
                            SortGames(chapter, dlg.SortGamesBy, dlg.SortGamesDirection);
                            WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
                        }

                        AppState.IsDirty = true;
                        GuiUtilities.RefreshChaptersView(null);
                        AppState.MainWin.UiTabChapters.Focus();
                        PulseManager.ChapterIndexToBringIntoView = chapter.Index;
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Message("InvokeSortGamesDialog()", ex);
                }
            }
        }

        /// <summary>
        /// Sorts games in the chapter per specified criteria.
        /// Keep the original order if items cannot be separated by those criteria.
        /// This method is superior to the standard QuickSort because it does keep 
        /// the original order.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="sortBy"></param>
        /// <param name="direction"></param>
        public static void SortGames(Chapter chapter, GameSortCriterion.SortItem sortBy, GameSortCriterion.SortItem direction, bool showMsg = true)
        {
            Mouse.SetCursor(Cursors.Wait);

            try
            {
                if (chapter != null)
                {
                    SortGamesInChapter(chapter, sortBy, direction);
                }
                else
                {
                    foreach (Chapter ch in AppState.Workbook.Chapters)
                    {
                        SortGamesInChapter(ch, sortBy, direction);
                    }
                }

                AppState.MainWin.ChaptersView.IsDirty = true;
                AppState.IsDirty = true;

                if (AppState.ActiveTab == TabViewType.CHAPTERS)
                {
                    AppState.MainWin.ChaptersView.BuildFlowDocumentForChaptersView(false);
                }

                if (showMsg)
                {
                    AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgGamesSorted, CommentBox.HintType.INFO);
                }
            }
            catch
            {
            }

            Mouse.SetCursor(Cursors.Arrow);
        }

        /// <summary>
        /// Saves the current order of games in the chapter
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="presortOrder"></param>
        private static void StorePresortGamesOrder(Chapter chapter, Dictionary<int, List<Article>> presortOrder)
        {
            presortOrder.Add(chapter.Index, new List<Article>());
            foreach (var game in chapter.ModelGames)
            {
                presortOrder[chapter.Index].Add(game);
            }
        }

        /// <summary>
        /// Sorts games in a single chapter per specified criteria.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="sortBy"></param>
        /// <param name="direction"></param>
        private static void SortGamesInChapter(Chapter chapter, GameSortCriterion.SortItem sortBy, GameSortCriterion.SortItem direction)
        {
            IComparer<Article> comparer = new ArticleComparer(sortBy, direction);
            chapter.ModelGames = chapter.ModelGames
                .Select((item, index) => new { item, index })
                .OrderBy(z => z.item, comparer)
                .ThenBy(z => z.index)
                .Select(z => z.item)
                .ToList();
        }

        /// <summary>
        /// Sorts exercises in the chapter per specified criteria.
        /// Keep the original order if items cannot be separated by those criteria.
        /// This method is superior to the standard QuickSort because it does keep 
        /// the original order.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="sortBy"></param>
        /// <param name="direction"></param>
        private static void SortExercises(Chapter chapter, GameSortCriterion.SortItem sortBy, GameSortCriterion.SortItem direction)
        {
            IComparer<Article> comparer = new ArticleComparer(sortBy, direction);
            chapter.Exercises = chapter.Exercises
                .Select((item, index) => new { item, index })
                .OrderBy(z => z.item, comparer)
                .ThenBy(z => z.index)
                .Select(z => z.item)
                .ToList();

            AppState.MainWin.ChaptersView.IsDirty = true;
            AppState.IsDirty = true;
        }

    }
}

using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Manages the process of requesting and downloading games from web sites.
    /// </summary>
    public class DownloadWebGamesManager
    {
        // max number of games to download
        public static int MAX_DOWNLOAD_GAME_COUNT = 1000;

        /// <summary>
        /// Invokes the dialog for requesting the download.
        /// Manages the return set of games.
        /// </summary>
        public static void DownloadGames()
        {
            DownloadWebGamesDialog dlg = new DownloadWebGamesDialog();
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 150);

            if (dlg.ShowDialog() == true)
            {
                int gameCount = 0;
                int exerciseCount = 0;

                int selected = GetSelectedGamesCount(dlg.Games);
                if (dlg.Games != null && selected > 0)
                {
                    bool buildRepertoireChapters;
                    DownloadedGamesActionDialog.Action action = SelectSaveOption(selected, out buildRepertoireChapters);
                    if (action != DownloadedGamesActionDialog.Action.None)
                    {
                        switch (action)
                        {
                            case DownloadedGamesActionDialog.Action.CurrentChapter:
                                AddGamesToCurrentChapter(dlg.Games, out gameCount, out exerciseCount, null, true);
                                break;
                            case DownloadedGamesActionDialog.Action.NewChapter:
                                AddGamesToNewChapter(dlg.Games, dlg.UserNick, buildRepertoireChapters, true, out gameCount, out exerciseCount);
                                break;
                            case DownloadedGamesActionDialog.Action.NewWorkbook:
                                AddGamesToNewWorkbook(dlg.Games, dlg.UserNick, buildRepertoireChapters, out gameCount, out exerciseCount);
                                break;
                        }

                        if (exerciseCount > 0)
                        {
                            string msg = Properties.Resources.DownloadedGamesAndExercises.Replace("$0", gameCount.ToString()).Replace("$1", exerciseCount.ToString());
                            MessageBox.Show(msg, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Information);
                        }

                        AppState.IsDirty = true;
                    }
                }
                else
                {
                    MessageBox.Show(Properties.Resources.ErrNoGamesSelected, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        /// <summary>
        /// Counts the games with the IsSelected flag set
        /// </summary>
        /// <param name="games"></param>
        /// <returns></returns>
        private static int GetSelectedGamesCount(ObservableCollection<GameData> games)
        {
            int count = 0;

            foreach (GameData game in games)
            {
                if (game.IsSelected)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Adds selected games to the current chapter
        /// </summary>
        /// <param name="games"></param>
        private static void AddGamesToCurrentChapter(ObservableCollection<GameData> games, out int addedGames, out int addedExercises, string errMsgPrefix, bool reportErrors)
        {
            addedGames = 0;
            addedExercises = 0;

            try
            {
                int selected = GetSelectedGamesCount(games);
                if (selected > 0)
                {
                    int copied = AppState.MainWin.CopySelectedItemsToChapter(AppState.ActiveChapter, true, out string error, games, out addedExercises);

                    addedGames = copied - addedExercises;
                    if (copied == 0)
                    {
                        // no games were copied
                        string message = Properties.Resources.ErrNoGamesCopied;
                        if (!string.IsNullOrEmpty(errMsgPrefix))
                        {
                            message = errMsgPrefix + ": " + message;
                        }
                        MessageBox.Show(message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        if (copied < selected)
                        {
                            // only some games were copied
                            string message = Properties.Resources.ErrNotAllGamesCopied + " (" + copied.ToString() + "/" + selected.ToString() + ")";
                            if (!string.IsNullOrEmpty(errMsgPrefix))
                            {
                                message = errMsgPrefix + ": " + message;
                            }
                            MessageBox.Show(message, Properties.Resources.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        AppState.MainWin.RebuildChaptersView(addedGames > 0, addedExercises > 0);
                        AppState.MainWin.FocusOnChapterView();
                    }

                    if (reportErrors && !string.IsNullOrEmpty(error))
                    {
                        TextBoxDialog tbDlg = new TextBoxDialog(Properties.Resources.PgnErrors, error);
                        //{
                        //    Left = AppState.MainWin.ChessForgeMain.Left + 100,
                        //    Top = AppState.MainWin.ChessForgeMain.Top + 100,
                        //    Topmost = false,
                        //    Owner = AppState.MainWin
                        //};
                        GuiUtilities.PositionDialog(tbDlg, AppState.MainWin, 100);
                        tbDlg.ShowDialog();
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Closes the current workbook, opens a new one and adds games to it.
        /// If buildRepertoireChapters==true, we will create 2 chapters, 
        /// one for White and one for Black games of the player.
        /// </summary>
        /// <param name="games"></param>
        private static void AddGamesToNewWorkbook(ObservableCollection<GameData> games, string player, bool buildRepertoireChapters, out int addedGames, out int addedExercises)
        {
            addedGames = 0;
            addedExercises = 0;

            try
            {
                if (WorkbookManager.AskToSaveWorkbookOnClose())
                {
                    AppState.MainWin.CreateNewWorkbook();
                    AddGamesToNewChapter(games, player, buildRepertoireChapters, false, out addedGames, out addedExercises);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Creates a new chapter in the current workbook and adds games to it.
        /// If buildRepertoireChapters==true, we will create 2 chapters, 
        /// one for White and one for Black games of the player.
        /// </summary>
        /// <param name="games"></param>
        private static void AddGamesToNewChapter(
            ObservableCollection<GameData> games,
            string player, 
            bool buildRepertoireChapters,
            bool createWhiteChapter, 
            out int addedGames, 
            out int addedExercises)
        {
            addedGames = 0;
            addedExercises = 0;

            // split games if we need separate chapter for White and separate for Black
            if (buildRepertoireChapters)
            {
                GameUtils.SplitGamesByColor(games, player, out ObservableCollection<GameData> whiteGames, out ObservableCollection<GameData> blackGames);
                if (whiteGames.Count > 0)
                {
                    if (createWhiteChapter)
                    {
                        WorkbookManager.SessionWorkbook.CreateNewChapter();
                    }
                    WorkbookManager.SessionWorkbook.ActiveChapter.SetTitle(player + ": " + Properties.Resources.GamesWithWhite);
                    AddGamesToCurrentChapter(whiteGames, out int addGames, out int addExercises, Properties.Resources.GamesWithWhite, false);
                    addedGames += addGames;
                    addedExercises += addExercises;
                    WorkbookManager.MergeGames(ref WorkbookManager.SessionWorkbook.ActiveChapter.StudyTree.Tree, ref whiteGames);
                    if (Configuration.AutogenTreeDepth > 0)
                    {
                        TreeUtils.TrimTree(ref WorkbookManager.SessionWorkbook.ActiveChapter.StudyTree.Tree, Configuration.AutogenTreeDepth, PieceColor.Black);
                    }
                    WorkbookManager.SessionWorkbook.ActiveChapter.StudyTree.Tree.BuildLines();
                }
                if (blackGames.Count > 0)
                {
                    if (createWhiteChapter || buildRepertoireChapters)
                    {
                        // create a new chapter for Black games if createWhiteChapter is true
                        WorkbookManager.SessionWorkbook.CreateNewChapter();
                    }
                    WorkbookManager.SessionWorkbook.ActiveChapter.SetTitle(player + ": " + Properties.Resources.GamesWithBlack);
                    AddGamesToCurrentChapter(blackGames, out int addGames, out int addExercises, Properties.Resources.GamesWithBlack, false);
                    addedGames += addGames;
                    addedExercises += addExercises;
                    WorkbookManager.MergeGames(ref WorkbookManager.SessionWorkbook.ActiveChapter.StudyTree.Tree, ref blackGames);
                    if (Configuration.AutogenTreeDepth > 0)
                    {
                        TreeUtils.TrimTree(ref WorkbookManager.SessionWorkbook.ActiveChapter.StudyTree.Tree, Configuration.AutogenTreeDepth, PieceColor.Black);
                    }
                    WorkbookManager.SessionWorkbook.ActiveChapter.StudyTree.Tree.BuildLines();
                }
            }
            else
            {
                Chapter currentActiveChapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                Chapter addedChapter = null;
                if (createWhiteChapter)
                {
                    addedChapter = WorkbookManager.SessionWorkbook.CreateNewChapter();
                }
                WorkbookManager.SessionWorkbook.ActiveChapter.SetTitle(player + ": " + Properties.Resources.DownloadedGames);
                AddGamesToCurrentChapter(games, out addedGames, out addedExercises, null, true);
                if (addedGames + addedExercises == 0)
                {
                    // undo creation of the chapter
                    WorkbookManager.SessionWorkbook.Chapters.Remove(addedChapter);
                    WorkbookManager.SessionWorkbook.ActiveChapter = currentActiveChapter;
                }
            }
        }

        /// <summary>
        /// Shows a dialog for selecting the Save option.
        /// </summary>
        /// <returns></returns>
        private static DownloadedGamesActionDialog.Action SelectSaveOption(int gameCount, out bool repertoireChapters)
        {
            repertoireChapters = false;

            DownloadedGamesActionDialog.Action action = DownloadedGamesActionDialog.Action.None;

            DownloadedGamesActionDialog dlgAct = new DownloadedGamesActionDialog(gameCount)
            {
                Left = AppState.MainWin.ChessForgeMain.Left + 150,
                Top = AppState.MainWin.Top + 150,
                Topmost = false,
                Owner = AppState.MainWin
            };

            if (dlgAct.ShowDialog() == true)
            {
                action = dlgAct.SaveOption;
                repertoireChapters = dlgAct.BuildRepertoireChapters;
            }

            return action;
        }
    }
}

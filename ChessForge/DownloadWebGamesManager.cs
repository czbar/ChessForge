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
        public static int MAX_DOWNLOAD_GAME_COUNT = 500;

        /// <summary>
        /// Invokes the dialog for requesting the download.
        /// Manages the return set of games.
        /// </summary>
        public static void DownloadGames()
        {
            DownloadWebGamesDialog dlg = new DownloadWebGamesDialog()
            {
                Left = AppState.MainWin.ChessForgeMain.Left + 150,
                Top = AppState.MainWin.Top + 150,
                Topmost = false,
                Owner = AppState.MainWin
            };

            if (dlg.ShowDialog() == true)
            {
                int selected = GetSelectedGamesCount(dlg.Games);
                if (dlg.Games != null && selected > 0)
                {
                    DownloadedGamesActionDialog.Action action = SelectSaveOption(selected);
                    switch (action)
                    {
                        case DownloadedGamesActionDialog.Action.CurrentChapter:
                            AddGamesToCurrentChapter(dlg.Games, selected);
                            break;
                        case DownloadedGamesActionDialog.Action.NewChapter:
                            AddGamesToNewChapter(dlg.Games, selected);
                            break;
                        case DownloadedGamesActionDialog.Action.NewWorkbook:
                            AddGamesToNewWorkbook(dlg.Games, selected);
                            break;
                    }
                    // if there were any exercises, display info
                    int gameCount = 0;
                    int exerciseCount = 0;
                    try
                    {
                        foreach (GameData gd in dlg.Games)
                        {
                            if (gd.GetContentType() == GameData.ContentType.MODEL_GAME || gd.GetContentType() == GameData.ContentType.GENERIC)
                            {
                                gameCount++;
                            }
                            else if (gd.GetContentType() == GameData.ContentType.EXERCISE)
                            {
                                exerciseCount++;
                            }
                        }
                        if (exerciseCount > 0)
                        {
                            string msg = Properties.Resources.DownloadedGamesAndExercises.Replace("$0", gameCount.ToString()).Replace("$1", exerciseCount.ToString());
                            MessageBox.Show(msg, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch { }
                    AppState.IsDirty = true;
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
        /// Closes the current workbook, opens a new one and adds games to it.
        /// </summary>
        /// <param name="games"></param>
        private static void AddGamesToNewWorkbook(ObservableCollection<GameData> games, int gameCount)
        {
            try
            {
                if (WorkbookManager.AskToSaveWorkbookOnClose())
                {
                    AppState.MainWin.CreateNewWorkbook();
                    AddGamesToCurrentChapter(games, gameCount);

                    WorkbookManager.SessionWorkbook.ActiveChapter.SetTitle(Properties.Resources.DownloadedGames);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Adds selected games to the current chapter
        /// </summary>
        /// <param name="games"></param>
        private static void AddGamesToCurrentChapter(ObservableCollection<GameData> games, int gameCount)
        {
            try
            {
                int selected = GetSelectedGamesCount(games);
                int copied = AppState.MainWin.CopySelectedItemsToChapter(AppState.ActiveChapter, true, out string error, games);
                if (copied == 0)
                {
                    // no games were copied
                    MessageBox.Show(Properties.Resources.ErrNoGamesCopied, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    if (copied < selected)
                    {
                        // only some games were copied
                        string message = Properties.Resources.ErrNotAllGamesCopied + " (" + copied.ToString() + "/" + selected.ToString() + ")";
                        MessageBox.Show(Properties.Resources.ErrNotAllGamesCopied, Properties.Resources.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    AppState.MainWin.RebuildChaptersView(true);
                    AppState.MainWin.FocusOnChapterView();
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Creates a new chapter in the current workbook and adds games to it.
        /// </summary>
        /// <param name="games"></param>
        private static void AddGamesToNewChapter(ObservableCollection<GameData> games, int gameCount)
        {
            VariationTree tree = new VariationTree(GameData.ContentType.STUDY_TREE);
            tree.AddNode(new TreeNode(null, "", 0));

            Chapter chapter = WorkbookManager.SessionWorkbook.CreateNewChapter(tree, true);
            chapter.SetTitle(Properties.Resources.DownloadedGames);

            AddGamesToCurrentChapter(games, gameCount);
        }

        /// <summary>
        /// Shows a dialog for selecting the Save option.
        /// </summary>
        /// <returns></returns>
        private static DownloadedGamesActionDialog.Action SelectSaveOption(int gameCount)
        {
            DownloadedGamesActionDialog.Action action = DownloadedGamesActionDialog.Action.None;

            if (WorkbookManager.SessionWorkbook == null)
            {
                // we only have one option i.e. to create a new workbook
                action = DownloadedGamesActionDialog.Action.NewWorkbook;
            }
            else
            {
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
                }
            }

            return action;
        }
    }
}

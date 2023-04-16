using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Manages the process of requesting and downloading games from web sites.
    /// </summary>
    public class DownloadWebGamesManager
    {
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

            if (dlg.ShowDialog() == true && dlg.Games != null && dlg.Games.Count > 0)
            {
                DownloadedGamesActionDialog.Action action = SelectSaveOption();
                switch (action)
                {
                    case DownloadedGamesActionDialog.Action.CurrentChapter:
                        AddGamesToCurrentChapter(dlg.Games);
                        break;
                    case DownloadedGamesActionDialog.Action.NewChapter:
                        AddGamesToNewChapter(dlg.Games);
                        break;
                    case DownloadedGamesActionDialog.Action.NewWorkbook:
                        AddGamesToNewWorkbook(dlg.Games);
                        break;
                }
                AppState.IsDirty = true;
            }
        }

        /// <summary>
        /// Closes the current workbook, opens a new one and adds games to it.
        /// </summary>
        /// <param name="games"></param>
        private static void AddGamesToNewWorkbook(ObservableCollection<GameData> games)
        {
            try
            {
                if (WorkbookManager.AskToSaveWorkbookOnClose())
                {
                    AppState.MainWin.CreateNewWorkbook();
                    AddGamesToCurrentChapter(games);

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
        private static void AddGamesToCurrentChapter(ObservableCollection<GameData> games)
        {
            try
            {
                AppState.MainWin.CopySelectedItemsToChapter(AppState.ActiveChapter, true, out string error, games);
                AppState.MainWin.RebuildChaptersView(true);
                AppState.MainWin.FocusOnChapterView();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Creates a new chapter in the current workbook and adds games to it.
        /// </summary>
        /// <param name="games"></param>
        private static void AddGamesToNewChapter(ObservableCollection<GameData> games)
        {
            VariationTree tree = new VariationTree(GameData.ContentType.STUDY_TREE);
            tree.AddNode(new TreeNode(null, "", 0));

            Chapter chapter = WorkbookManager.SessionWorkbook.CreateNewChapter(tree, true);
            chapter.SetTitle(Properties.Resources.DownloadedGames);

            AddGamesToCurrentChapter(games);
        }

        /// <summary>
        /// Shows a dialog for selecting the Save option.
        /// </summary>
        /// <returns></returns>
        private static DownloadedGamesActionDialog.Action SelectSaveOption()
        {
            DownloadedGamesActionDialog.Action action = DownloadedGamesActionDialog.Action.None;

            if (WorkbookManager.SessionWorkbook == null)
            {
                // we only have one option i.e. to create a new workbook
                action = DownloadedGamesActionDialog.Action.NewWorkbook;
            }
            else
            {
                DownloadedGamesActionDialog dlgAct = new DownloadedGamesActionDialog()
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

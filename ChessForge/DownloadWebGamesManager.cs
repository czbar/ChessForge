using System;
using System.Collections.Generic;
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
                if (WorkbookManager.SessionWorkbook == null)
                {
                    // we only have one option i.e. to create a new workbook
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
                    dlgAct.ShowDialog();
                }
            }
        }
    }
}

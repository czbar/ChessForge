namespace ChessForge
{
    public class SearchPositionInFiles
    {
        /// <summary>
        /// Invoke a dialog to search for a position in PGN files. 
        /// The dialog may return the name of the PGN file where the position is found
        /// and user requested to open it.
        /// </summary>
        /// <param name="crits"></param>
        public static void Search(SearchPositionCriteria crits)
        {
            SearchPgnFilesDialog dlg = new SearchPgnFilesDialog(crits);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

            if (dlg.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(dlg.SelectedPgnFile))
                {
                    // Open a PGN file marked as selected.
                    if (dlg.SelectedPgnFile != AppState.WorkbookFilePath)
                    {
                        AppState.MainWin.OpenWorkbook(dlg.SelectedPgnFile, true);
                    }
                }
            }
        }

    }
}

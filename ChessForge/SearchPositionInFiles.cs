using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    public class SearchPositionInFiles
    {
        public static void Search(SearchPositionCriteria crits)
        {
            SearchPgnFilesDialog dlg = new SearchPgnFilesDialog(crits);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

            if (dlg.ShowDialog() == true)
            {
            // Open a PGN file marked as selected.
            // TODO: implement
            }
        }
    }
}

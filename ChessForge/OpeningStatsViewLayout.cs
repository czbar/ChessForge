using System.Windows;

namespace ChessForge
{
    public class OpeningStatsViewLayout
    {
        /// <summary>
        /// Initial width of the view area in the Opening Stats View.
        /// </summary>
        private static double INITIAL_VIEW_AREA_WIDTH = 560;

        /// <summary>
        /// Width of the view area in the Opening Stats View.
        /// The initial value would not be used as it will be
        /// recalculated from the ActualWidth.
        /// </summary>
        public static double ViewAreaWidth
        {
            get
            {
                return INITIAL_VIEW_AREA_WIDTH - AppState.MainWin.ABSOLUTE_ADJUSTMENT;
            }
        }

        /// <summary>
        /// Total width of the stats labels column.
        /// </summary>
        public static GridLength StatsColumnWidth
        {
            get
            {
                return new GridLength(StatsLabelsWidth + StatsColumnIndent + 1);
            }
        }

        //*********************************************
        //
        // Width of the columns in the Opening Stats View
        // data table.
        //
        //*********************************************

        /// <summary>
        /// Width of the first column that contains the move.
        /// </summary>
        public static double MoveColumnWidth = 70;

        /// <summary>
        /// Width of the second column that contains the number of games.
        /// </summary>
        public static double TotalGamesColumnWidth = 70;

        /// <summary>
        /// Width of the combined labels in the third column that contains the stats labels.
        /// It is obtained by subtracting widths of other columns and the stats column indent
        /// from the total view area width.
        /// </summary>
        public static double StatsLabelsWidth
        {
            get
            {
                return ViewAreaWidth - (MoveColumnWidth + TotalGamesColumnWidth + StatsColumnIndent);
            }
        }

        /// <summary>
        /// Indent for the stats column labels.
        /// </summary>
        public static double StatsColumnIndent = 35;


        //*********************************************
        //
        // Width of the columns in the Opening Stats View
        // opening name table.
        //
        //*********************************************


        /// <summary>
        /// Width of the column that contains the ECO code.
        /// </summary>
        public static double EcoColumnWidth = 70;

        /// <summary>
        /// Width of the column that contains the opening name.
        /// It is the total view area width minus the ECO column width.
        /// </summary>
        public static double OpeningNameColumnWidth
        {
            get
            {
                return ViewAreaWidth - EcoColumnWidth;
            }
        }

        //*********************************************
        //
        // Width of the columns the Tablebase View
        //
        //*********************************************

        /// <summary>
        /// Width of the first column that contains the move.
        /// </summary>
        public static double TablebaseMoveColumnWidth = 210;

        /// <summary>
        /// Width of the second column that contains the DTZ value.
        /// </summary>
        public static double DtzColumnWidth = 140;

        /// <summary>
        /// Width of the second column that contains the DTM value.
        /// </summary>
        public static double DtmColumnWidth = 140;

    }
}

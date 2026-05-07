namespace ChessForge
{
    /// <summary>
    /// Holds the state of the layout of the main window.
    /// </summary>
    public class LayoutState
    {
        /// <summary>
        /// The adjustment factor for the chessboard size.
        /// It affects the widths of the Chessboard and Tab Control columns as their
        /// widths are adjusted by the same amount in the opposite directions.
        /// It does not affect the heights.
        /// The value is determined by the user when using the Vertical Splitter.
        /// </summary>
        public static double ChessboardSizeAdjustment { get; set; }

        /// <summary>
        /// The adjustment factor for the row height in the Explorer row.
        /// It affects the height of both the Chessboard and the Explorer rows as their 
        /// heights are adjusted by the same amount in the opposite directions.
        /// This value is determined by the user when using the Horizontal Splitter.
        /// </summary>
        public static double ExplorerRowHeightAdjustment { get; set; }

        /// <summary>
        /// The adjustment factor for the column width in the Scoresheet column.
        /// It affects the widths of the Tab Control and Scoresheet columns as their
        /// widths are adjusted by the same amount in the opposite directions.
        /// It does not affect the heights.
        /// The value is determined by the current state/mode of the application
        /// e.g. it the tab control is showing Chapters/Intro/Bookmarks there is no
        /// Scoresheet at all and the adjustment will reduce the width of the Scoresheet
        /// column to zero while widening the Tab Control column.
        /// </summary>
        public static double ScoresheetWidthAdjustment { get; set; }

        /// <summary>
        /// The width adjustment for the shape of the app windows
        /// i.e. the width/height ratio.
        /// </summary>
        public static double WidthCorrectionForShape { get; set; }

        /// <summary>
        /// The height adjustment for the shape of the app windows
        /// i.e. the width/height ratio.
        /// </summary>
        public static double HeightCorrectionForShape { get; set; }

        /// <summary>
        /// Initializes the layout state with the current values 
        /// from the configuration.
        /// </summary>
        /// <param name="mainWindow"></param>
        public static void Initialize(MainWindow mainWindow)
        {
            ChessboardSizeAdjustment = Configuration.ChessboardSizeAdjustment;
            ExplorerRowHeightAdjustment = Configuration.ExplorerRowHeightAdjustment;
            ScoresheetWidthAdjustment = 0;
        }
    }
}

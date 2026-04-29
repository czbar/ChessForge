using ChessPosition;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    public class LayoutUtils
    {
        /// <summary>
        /// The default margins for controls in the Explorer row.
        /// </summary>
        public static double EXPLORER_ROW_LEFT_MARGIN = 4;
        public static double EXPLORER_ROW_TOP_MARGIN = 10;
        public static double EXPLORER_ROW_RIGHT_MARGIN = 4;
        public static double EXPLORER_ROW_BOTTOM_MARGIN = 0;

        // Index of the bottom half row in the main grid.
        public static int EXPLORER_ROW_INDEX = 2;

        // Index of the column in the main grid where the chessboard is located.
        public static int CHESSBOARD_COLUMN_INDEX = 0;

        // The default thickness for controls in the explorer row.
        public static double DEFAULT_BORDER_THICKNESS = 1;

        /// <summary>
        /// Sets the default thickness for controls in the explorer row. 
        /// This is used to ensure that all controls in the explorer row have the same margins.
        /// </summary>
        /// <param name="ctrl"></param>
        public static void SetExplorerRowDefaultControlThickness(Control ctrl)
        {
            ThicknessUtils.SetControlThickness(ctrl, new Thickness(
                EXPLORER_ROW_LEFT_MARGIN,
                EXPLORER_ROW_TOP_MARGIN,
                EXPLORER_ROW_RIGHT_MARGIN,
                EXPLORER_ROW_BOTTOM_MARGIN));
        }

        /// <summary>
        /// Returns the height available for controls in the explorer row, after subtracting the margins.
        /// The height will vary as the user resizes the window.
        /// </summary>
        /// <returns></returns>
        public static double AvailableHeightInExplorerRow()
        {
            return AppState.MainWin._gridMain.RowDefinitions[EXPLORER_ROW_INDEX].Height.Value
                                      - (EXPLORER_ROW_TOP_MARGIN + EXPLORER_ROW_BOTTOM_MARGIN);
        }

        public static double AvailableWidthInChessboardColumn()
        {
            return AppState.MainWin._gridMain.ColumnDefinitions[CHESSBOARD_COLUMN_INDEX].Width.Value
                                      - (EXPLORER_ROW_LEFT_MARGIN + EXPLORER_ROW_RIGHT_MARGIN);
        }

        /// <summary>
        /// Sets the margins and font size for the engine lines text box, depending on whether it is in full size mode or not.
        /// In the half size mode, we remove the top margin so that it does not merge or overlap with the eval chart
        /// which could be ugly (thick).
        /// </summary>
        /// <param name="fullSize"></param>
        public static void SetEngineLinesBoxMargins(bool fullSize)
        {
            double availableHeight = AvailableHeightInExplorerRow();

            TextBox tb = AppState.MainWin.UiTbEngineLines;
            if (fullSize == true)
            {
                ThicknessUtils.SetControlTopMargin(tb, EXPLORER_ROW_TOP_MARGIN);
                tb.FontSize = Constants.BASE_ENGINE_LINES_FONT_SIZE + Configuration.FontSizeDiff;
                tb.BorderThickness = new Thickness(1, 1, 1, 1);

            }
            else
            {
                ThicknessUtils.SetControlTopMargin(tb, (availableHeight / 2) + (10 - 1));
                tb.FontSize = (Constants.BASE_ENGINE_LINES_FONT_SIZE + Configuration.FontSizeDiff) - 2;
                tb.BorderThickness = new Thickness(1, 0, 1, 1);
            }
        }
    }
}

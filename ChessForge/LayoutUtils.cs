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

        // The maximum width adjustment that the user can apply to the main tab control.
        public static int MAX_USER_WIDTH_ADJUSTMENT = 400;

        // The maximum height adjustment that the user can apply to the main tab control.
        public static int MAX_USER_HEIGHT_ADJUSTMENT = 400;

        // The minimum height adjustment that the user can apply to the main tab control.
        public static int MIN_USER_HEIGHT_ADJUSTMENT = -100;

        // Default width (and height) of the main chessboard.
        public static double CHESSBOARD_DEFAULT_WIDTH = 680;

        // The margin for the chessboard in the main grid.
        public static double MAIN_CHESSBOARD_MARGIN = 20;

        // Index of the bottom half row in the main grid.
        public static int EXPLORER_ROW_INDEX = 2;

        // Index of the column in the main grid where the chessboard is located.
        public static int CHESSBOARD_COLUMN_INDEX = 0;

        // Index of the row in the main grid where the chessboard is located.
        public static int CHESSBOARD_ROW_INDEX = 1;

        // The default thickness for controls in the explorer row.
        public static double DEFAULT_BORDER_THICKNESS = 1;


        // original grid row/column height/width definitions for the main grid.
        public static double[] MAIN_GRID_ROWS = { 1.0, 680.0, 160.0, 10.0 };
        public static double[] MAIN_GRID_COLUMNS = { 680.0, 600.0, 270.0, 1.0 };

        /// <summary>
        /// Coordinates the adjustments to the chessboard and explorer row heights, depending on the user input and the priority.
        /// Note that the non-prioritized adjustment must have the value of 0 on entry, it may be changed if required.
        /// </summary>
        /// <param name="chessboardAdjustment"></param>
        /// <param name="explorerRowAdjustment"></param>
        /// <param name="prioritizeBoard"></param>
        public static void CoordinateChessboardExplorerRowAdjustments(ref double chessboardAdjustment, ref double explorerRowAdjustment, bool prioritizeBoard)
        {
            LimitChessboardAdjustment(ref chessboardAdjustment);
            LimitExplorerRowAdjustment(ref explorerRowAdjustment);

            Grid mainGrid = AppState.MainWin._gridMain;

            // depending on the priority, we adjust the chessboard or the explorer row first.
            if (prioritizeBoard)
            {
                // if the chessboard would not fit in the available space, we adjust the explorer row to create more space for the chessboard.
                if (mainGrid.RowDefinitions[CHESSBOARD_ROW_INDEX].Height.Value < CHESSBOARD_DEFAULT_WIDTH + chessboardAdjustment)
                {
                    // need room below in the explorer row
                    explorerRowAdjustment = -1 * chessboardAdjustment;
                }
            }
            else
            {
                // squeeze the chessboard if needed.
                if (explorerRowAdjustment > MAIN_GRID_ROWS[CHESSBOARD_ROW_INDEX] - AppState.MainWin.MainBoard.Width) //mainGrid.RowDefinitions[CHESSBOARD_ROW_INDEX].Height.Value)
                {
                    chessboardAdjustment = -1 * explorerRowAdjustment;
                }
            }
        }

        /// <summary>
        /// Limits the chessboard adjustment to ensure that the chessboard does not become too small or too large.
        /// </summary>
        /// <param name="chessboardAdjustment"></param>
        public static void LimitChessboardAdjustment(ref double chessboardAdjustment)
        {
            if (chessboardAdjustment > 0)
            {
                chessboardAdjustment = 0;
            }
            else if (chessboardAdjustment < -MAX_USER_WIDTH_ADJUSTMENT)
            {
                chessboardAdjustment = -MAX_USER_WIDTH_ADJUSTMENT;
            }
        }

        /// <summary>
        /// Limits the explorer row adjustment to ensure that the explorer row does not become too small or too large.
        /// </summary>
        /// <param name="explorerRowAdjustment"></param>
        public static void LimitExplorerRowAdjustment(ref double explorerRowAdjustment)
        {    
            if (explorerRowAdjustment > MAX_USER_HEIGHT_ADJUSTMENT)
            {
                explorerRowAdjustment = MAX_USER_HEIGHT_ADJUSTMENT;
            }
            else if (explorerRowAdjustment < MIN_USER_HEIGHT_ADJUSTMENT)
            {
                explorerRowAdjustment = MIN_USER_HEIGHT_ADJUSTMENT;
            }
        }

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

        /// <summary>
        /// Returns the width available for controls in the chessboard column, after subtracting the margins.
        /// </summary>
        /// <returns></returns>
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

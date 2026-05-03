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

        /// <summary>
        /// The default margins for the tab items in the main tab control.
        /// </summary>
        public static double TAB_ITEM_LEFT_MARGIN = 2;
        public static double TAB_ITEM_TOP_MARGIN = 2;
        public static double TAB_ITEM_RIGHT_MARGIN = 2;
        public static double TAB_ITEM_BOTTOM_MARGIN = 22;

        // The maximum width adjustment that the user can apply to the main tab control.
        public static int MAX_USER_WIDTH_ADJUSTMENT = 400;

        // The minimum width adjustment that the user can apply to the main tab control.
        public static int MIN_USER_WIDTH_ADJUSTMENT = 0;

        // The maximum height adjustment that the user can apply to the main tab control.
        public static int MAX_USER_HEIGHT_ADJUSTMENT = 200;

        // The minimum height adjustment that the user can apply to the main tab control.
        public static int MIN_USER_HEIGHT_ADJUSTMENT = 0;

        // Default width (and height) of the main chessboard.
        public static double CHESSBOARD_DEFAULT_WIDTH = 680;

        // The margin for the chessboard in the main grid.
        public static double MAIN_CHESSBOARD_MARGIN = 20;


        //**********************************************************
        // Indexes of the rows and columns in the main grid, used for resizing calculations.
        // These indexes are used to identify the specific rows and columns in the main grid where the chessboard, tab control, scoresheet,
        // and explorer row are located.
        //

        // Index of the column in the main grid where the chessboard is located.
        public static int CHESSBOARD_COLUMN_INDEX = 0;

        // Index of the column in the main grid where the tab control is located.
        public static int TAB_CTRL_COLUMN_INDEX = 1;

        // Index of the column in the main grid where the scoresheet is located.
        public static int SCORESHEET_COLUMN_INDEX = 2;

        // Index of the row in the main grid where the chessboard is located.
        public static int CHESSBOARD_ROW_INDEX = 1;

        // Index of the bottom half row in the main grid.
        public static int EXPLORER_ROW_INDEX = 2;

        // default grid row/column height/width definitions for the main grid.
        public static double[] MAIN_GRID_ROWS = { 1.0, 680.0, 160.0, 10.0 };
        public static double[] MAIN_GRID_COLUMNS = { 680.0, 600.0, 270.0, 1.0 };



        // Default width of the main grid, used for resizing calculations.
        public static double DEFAULT_GRID_WIDTH;

        // Default height of the main grid, used for resizing calculations.
        public static double DEFAULT_GRID_HEIGHT;

        // Default width/height ratio of the main grid, used for resizing calculations.
        public static double DEFAULT_GRID_WIDTH_HEIGHT_RATIO;

        // The default thickness for controls in the explorer row.
        public static double DEFAULT_BORDER_THICKNESS = 1;

        /// <summary>
        /// Adjusts the bottom margin of the tab items in the main tab control.
        /// </summary>
        /// <param name="rtb"></param>
        /// <param name="bottomMargin"></param>
        public static void AdjustTabItemBottomMargin(RichTextBox rtb, double bottomMargin)
        {
            rtb.Margin = new Thickness(TAB_ITEM_LEFT_MARGIN, TAB_ITEM_TOP_MARGIN, TAB_ITEM_RIGHT_MARGIN, bottomMargin);
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
            return AppState.MainWin.UiMainGrid.RowDefinitions[EXPLORER_ROW_INDEX].Height.Value
                                      - (EXPLORER_ROW_TOP_MARGIN + EXPLORER_ROW_BOTTOM_MARGIN);
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

        /// <summary>
        /// Calculates the extra width of the chessboard row compared to what the default ratio would demand.
        /// The value can be negative. 
        /// </summary>
        /// <param name="actualWidthHeightRatio"></param>
        /// <returns></returns>
        public static double CalcExtraGridWidth(double actualWidthHeightRatio)
        {
            return actualWidthHeightRatio * DEFAULT_GRID_HEIGHT - DEFAULT_GRID_WIDTH;
        }

        /// <summary>
        /// Calculates the extra height of the chessboard row compared to what the default ratio would demand.
        /// The value can be negative.
        /// </summary>
        /// <param name="actualWidthHeightRatio"></param>
        /// <returns></returns>
        public static double CalcExtraGridHeight(double actualWidthHeightRatio)
        {
            return DEFAULT_GRID_WIDTH / actualWidthHeightRatio - DEFAULT_GRID_HEIGHT;
        }

        /// <summary>
        /// Adjusts the column widths of the main grid to maintain the chessboard size and the overall width of the main grid, 
        /// depending on the extra width calculated from the actual width/height ratio.
        /// If the extra width is positive, we give it to the tab control column, 
        /// if it is negative, we keep the default width as we do not want the tab control to become too narrow.
        /// </summary>
        /// <param name="extraWidth"></param>
        public static void AdjustColumnWidths(double extraWidth)
        {
            Grid mainGrid = AppState.MainWin.UiMainGrid;
            double adj = Configuration.ChessboardSizeAdjustment;

            if (extraWidth > 0)
            {
                // add the extraWidth to the tab control column
                mainGrid.ColumnDefinitions[CHESSBOARD_COLUMN_INDEX].Width = new GridLength(MAIN_GRID_COLUMNS[CHESSBOARD_COLUMN_INDEX] + adj);
                mainGrid.ColumnDefinitions[TAB_CTRL_COLUMN_INDEX].Width = new GridLength(MAIN_GRID_COLUMNS[TAB_CTRL_COLUMN_INDEX] - adj + extraWidth);
                mainGrid.ColumnDefinitions[SCORESHEET_COLUMN_INDEX].Width = new GridLength(MAIN_GRID_COLUMNS[SCORESHEET_COLUMN_INDEX]);
            }
            else
            {
                // we use the default widths with the chessboard size adjustment
                mainGrid.ColumnDefinitions[CHESSBOARD_COLUMN_INDEX].Width = new GridLength(MAIN_GRID_COLUMNS[CHESSBOARD_COLUMN_INDEX] + adj);
                mainGrid.ColumnDefinitions[TAB_CTRL_COLUMN_INDEX].Width = new GridLength(MAIN_GRID_COLUMNS[TAB_CTRL_COLUMN_INDEX] - adj);
                mainGrid.ColumnDefinitions[SCORESHEET_COLUMN_INDEX].Width = new GridLength(MAIN_GRID_COLUMNS[SCORESHEET_COLUMN_INDEX]);
            }
        }

        /// <summary>
        /// Adjusts the row heights of the main grid to maintain the chessboard size and the overall height of the main grid,
        /// </summary>
        /// <param name="extraHeight"></param>
        public static void AdjustRowHeights(double extraHeight)
        {
            Grid mainGrid = AppState.MainWin.UiMainGrid;
            double adj = Configuration.ExplorerRowHeightAdjustment;

            if (extraHeight > 0)
            {
                double extraChessboardRowHeight = 0;
                if (extraHeight > MAX_USER_HEIGHT_ADJUSTMENT)
                {
                    extraChessboardRowHeight = extraHeight - MAX_USER_HEIGHT_ADJUSTMENT;
                    extraHeight = MAX_USER_HEIGHT_ADJUSTMENT;
                }

                // add the extraHeight to the explorer row
                mainGrid.RowDefinitions[CHESSBOARD_ROW_INDEX].Height = new GridLength(MAIN_GRID_ROWS[CHESSBOARD_ROW_INDEX] - adj + extraChessboardRowHeight);
                mainGrid.RowDefinitions[EXPLORER_ROW_INDEX].Height = new GridLength(MAIN_GRID_ROWS[EXPLORER_ROW_INDEX] + adj + extraHeight);
            }
            else
            {
                // we use the default heights with the explorer row height adjustment
                mainGrid.RowDefinitions[CHESSBOARD_ROW_INDEX].Height = new GridLength(MAIN_GRID_ROWS[CHESSBOARD_ROW_INDEX] - adj);
                mainGrid.RowDefinitions[EXPLORER_ROW_INDEX].Height = new GridLength(MAIN_GRID_ROWS[EXPLORER_ROW_INDEX] + adj);
            }
        }
    }
}

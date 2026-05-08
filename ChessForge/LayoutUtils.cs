using ChessPosition;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    public class LayoutUtils
    {
        //**********************************************************
        // Default margins for rows and columns.
        //

        // Top and bottom margins for controls in the Explorer row.
        // We set the bottom margin to 0 so that the size of the empty space below the Explorer row
        // is controlled by the size of the fourth (dummy) row.
        public static double EXPLORER_ROW_TOP_MARGIN = 4;
        public static double EXPLORER_ROW_BOTTOM_MARGIN = 0;

        // Left and right margins of the Scoresheet column.
        public static int SCORESHEET_COL_LEFT_MARGIN = 2;
        public static int SCORESHEET_COL_RIGHT_MARGIN = 4;

        // Left and right margins of the tab control column.
        public static double TAB_CTRL_COL_LEFT_MARGIN = 4;
        public static double TAB_CTRL_COL_RIGHT_MARGIN = 2;

        // The bottom margin for the tab control column.
        // Note that there is no common top margin for the controls in the tab control row.
        public static double TAB_CTRL_ROW_BOTTOM_MARGIN = 0;


        //**************************************************
        // Margins specific to the controls rather than rows and columns.
        //

        // Top margin for the Tab Control.
        public static double TAB_CTRL_TOP_MARGIN = 5;

        // The margin for the chessboard in the main grid.
        public static double MAIN_CHESSBOARD_MARGIN = 20;

        // The left margin for the comment box in the explorer row.
        public static double COMMENT_BOX_LEFT_MARGIN = 1;

        // Top margin for the Scoresheet / Game DataGrid.
        // It is greater than the top margin for the tab control column because we want to leave some space
        // between the top of the scoresheet and the bottom of the scoresheet label, which is above it.
        public static int SCORESHEET_TOP_MARGIN = 27;

        //
        // The default margins for the tab items within the Tab Control.
        //
        public static double TAB_ITEM_LEFT_MARGIN = 2;
        public static double TAB_ITEM_TOP_MARGIN = 2;
        public static double TAB_ITEM_RIGHT_MARGIN = 2;
        // the bottom margin for the tab items in the main tab control, when leaving room for nags and evals.
        public static double TAB_ITEM_BOTTOM_MARGIN = 22;
        // the bottom margin for the tab items in the main tab control, when not showing nags and evals.
        public static double TAB_ITEM_BOTTOM_MARGIN_NO_NAGS = 2;


        //**********************************************************
        // Limits on adjustments that the user can apply
        // to the widths and heights of the main grid rows and columns.
        //

        // The maximum width adjustment that the user can apply to the main tab control.
        public static int MAX_USER_WIDTH_ADJUSTMENT = 400;

        // The minimum width adjustment that the user can apply to the main tab control.
        public static int MIN_USER_WIDTH_ADJUSTMENT = 0;

        // The maximum height adjustment that the user can apply to the main tab control.
        public static int MAX_USER_HEIGHT_ADJUSTMENT = 200;

        // The minimum height adjustment that the user can apply to the main tab control.
        public static int MIN_USER_HEIGHT_ADJUSTMENT = 0;


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


        //**********************************************************
        //
        // Default widths and heights for the rows and columns in the main grid.
        //

        // Default width and height of the main chessboard.
        public static double DEFAULT_CHESSBOARD_SIZE = 680;

        // Default width of the tab control column in the main grid.
        public static double DEFAULT_TAB_CTRL_ROW_WIDTH = 600;

        // Default width of the scoresheet column in the main grid.
        public static double DEFAULT_SCORESHEET_COL_WIDTH = 270;
        
        // Default height of the explorer row in the main grid.
        public static double DEFAULT_EXPLORER_ROW_HEIGHT = 160;

        /// <summary>
        /// Array of default widths of the columns in the main grid.
        /// </summary>
        public static double[] DEFAULT_COLUMN_WIDTHS = 
        {
          DEFAULT_CHESSBOARD_SIZE,
          DEFAULT_TAB_CTRL_ROW_WIDTH,
          DEFAULT_SCORESHEET_COL_WIDTH, 
          1.0 
        };

        /// <summary>
        /// The default heights of the rows in the main grid.
        /// </summary>
        public static double[] DEFAULT_ROW_HEIGHTS =
        {   
          1.0,
          DEFAULT_CHESSBOARD_SIZE,
          DEFAULT_EXPLORER_ROW_HEIGHT,
          5.0
        };

        // The extra width to subtract from the scoresheet column if not showing evals.
        // The scoresheet/game data grid will be narrower when not showing evals, so we can give some of its width to the tab control column.
        public static int SCORESHEET_COL_EXTRA_WIDTH_FOR_EVALS = 80;


        //**************************************************
        // Default total width and height of the main grid.
        // These values are calculated from the default widths
        // and heights of the columns and rows of the main grid
        // in MainWindowLayout.InitializeLayoutConstants()
        // which must be called early in the application startup,
        // before any resizing calculations are made.

        // Default width of the main grid, used for resizing calculations.
        public static double DEFAULT_GRID_WIDTH;

        // Default height of the main grid, used for resizing calculations.
        public static double DEFAULT_GRID_HEIGHT;

        // Default width/height ratio of the main grid, used for resizing calculations.
        public static double DEFAULT_GRID_WIDTH_HEIGHT_RATIO;


        //**************************************************
        // Additional constants for the layout of controls.
        //

        // The default thickness for controls in the explorer row.
        public static double DEFAULT_BORDER_THICKNESS = 1;

        // The default width of the empty space in the Openings Explorer.
        public static double EMPTY_WIDTH_IN_OPENINGS_EXPLORER = 12;

        /// <summary>
        /// Sets the default positions and margins for the controls in the scoresheet and explorer rows.
        /// </summary>
        public static void SetDefaultControlPositions()
        {
            MainWindow mainWin = AppState.MainWin;

            // DataGrid for active line
            mainWin.UiDgActiveLine.HorizontalAlignment = HorizontalAlignment.Stretch;
            mainWin.UiDgActiveLine.Margin = new Thickness(SCORESHEET_COL_LEFT_MARGIN, SCORESHEET_TOP_MARGIN, SCORESHEET_COL_RIGHT_MARGIN, 0);

            // Scoresheet label
            mainWin.UiLblScoresheet.HorizontalAlignment = HorizontalAlignment.Left;
            mainWin.UiLblScoresheet.Margin = new Thickness(SCORESHEET_COL_LEFT_MARGIN, 0, SCORESHEET_COL_RIGHT_MARGIN, 0);

            // DataGrid for engine game
            mainWin.UiDgEngineGame.HorizontalAlignment = HorizontalAlignment.Stretch;
            mainWin.UiDgEngineGame.Margin = new Thickness(SCORESHEET_COL_LEFT_MARGIN, SCORESHEET_TOP_MARGIN, SCORESHEET_COL_RIGHT_MARGIN, 0);

            mainWin.UiTbEngineLines.Margin = new Thickness(COMMENT_BOX_LEFT_MARGIN, EXPLORER_ROW_TOP_MARGIN, 0, 0);
            mainWin.UiRtbBoardComment.Margin = new Thickness(COMMENT_BOX_LEFT_MARGIN, EXPLORER_ROW_TOP_MARGIN, 0, 0);
            mainWin.UiEvalChart.Margin = new Thickness(COMMENT_BOX_LEFT_MARGIN, EXPLORER_ROW_TOP_MARGIN, 0, 0);

            mainWin.UiRtbOpenings.Margin = new Thickness(TAB_CTRL_COL_LEFT_MARGIN, EXPLORER_ROW_TOP_MARGIN, TAB_CTRL_COL_RIGHT_MARGIN, 0);
            mainWin.UiTrainingSessionBox.Margin = new Thickness(TAB_CTRL_COL_LEFT_MARGIN, EXPLORER_ROW_TOP_MARGIN, TAB_CTRL_COL_RIGHT_MARGIN, 0);
            mainWin.UiRectDummyOpenings.Margin = new Thickness(TAB_CTRL_COL_LEFT_MARGIN, EXPLORER_ROW_TOP_MARGIN, TAB_CTRL_COL_RIGHT_MARGIN, 0);

            mainWin.UiRtbTopGames.HorizontalAlignment = HorizontalAlignment.Stretch;
            mainWin.UiRtbTopGames.Margin = new Thickness(SCORESHEET_COL_LEFT_MARGIN, EXPLORER_ROW_TOP_MARGIN, SCORESHEET_COL_RIGHT_MARGIN, 0);
            mainWin.UiRectDummyTopGames.Margin = new Thickness(SCORESHEET_COL_LEFT_MARGIN, EXPLORER_ROW_TOP_MARGIN, SCORESHEET_COL_RIGHT_MARGIN, 0);

            SetDefaultTabCtrlMargins(mainWin.UiTabCtrlManualReview);
            SetDefaultTabCtrlMargins(mainWin.UiTabCtrlTraining);
            SetDefaultTabCtrlMargins(mainWin.UiTabCtrlEngineGame);

            SetDefaultTabItemMargins(mainWin.UiRtbChaptersView, false);
            SetDefaultTabItemMargins(mainWin.UiRtbIntroView, false);
            SetDefaultTabItemMargins(mainWin.UiRtbStudyTreeView, true);
            SetDefaultTabItemMargins(mainWin.UiRtbModelGamesView, true);
            SetDefaultTabItemMargins(mainWin.UiRtbExercisesView, true);
        }

        /// <summary>
        /// Sets the default margins for the tab controls in the main grid.
        /// </summary>
        /// <param name="tabCtrl"></param>
        private static void SetDefaultTabCtrlMargins(TabControl tabCtrl)
        {
            tabCtrl.Margin = new Thickness(TAB_CTRL_COL_LEFT_MARGIN, TAB_CTRL_TOP_MARGIN, TAB_CTRL_COL_RIGHT_MARGIN, 0);
        }

        /// <summary>
        /// Sets the default margins for the tab items in the main tab control.
        /// </summary>
        /// <param name="rtb"></param>
        /// <param name="isVariationTreeView"></param>
        private static void SetDefaultTabItemMargins(RichTextBox rtb, bool isVariationTreeView)
        {
            rtb.Margin = new Thickness(TAB_ITEM_LEFT_MARGIN, TAB_ITEM_TOP_MARGIN, TAB_ITEM_RIGHT_MARGIN,
                isVariationTreeView ? TAB_ITEM_BOTTOM_MARGIN : TAB_ITEM_BOTTOM_MARGIN_NO_NAGS);
        }

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
        public static void AdjustColumnWidths()
        {
            double extraWidth = LayoutState.WidthCorrectionForShape;

            Grid mainGrid = AppState.MainWin.UiMainGrid;
            double chessboardAdj = LayoutState.ChessboardSizeAdjustment;
            double scoresheetAdj = LayoutState.ScoresheetWidthAdjustment;

            if (extraWidth > 0)
            {
                // add the extraWidth to the tab control column
                mainGrid.ColumnDefinitions[CHESSBOARD_COLUMN_INDEX].Width = new GridLength(DEFAULT_COLUMN_WIDTHS[CHESSBOARD_COLUMN_INDEX] + chessboardAdj);
                mainGrid.ColumnDefinitions[TAB_CTRL_COLUMN_INDEX].Width = new GridLength(DEFAULT_COLUMN_WIDTHS[TAB_CTRL_COLUMN_INDEX] - chessboardAdj + extraWidth - scoresheetAdj);
                mainGrid.ColumnDefinitions[SCORESHEET_COLUMN_INDEX].Width = new GridLength(DEFAULT_COLUMN_WIDTHS[SCORESHEET_COLUMN_INDEX] + scoresheetAdj);
            }
            else
            {
                // we use the default widths with the chessboard size adjustment
                mainGrid.ColumnDefinitions[CHESSBOARD_COLUMN_INDEX].Width = new GridLength(DEFAULT_COLUMN_WIDTHS[CHESSBOARD_COLUMN_INDEX] + chessboardAdj);
                mainGrid.ColumnDefinitions[TAB_CTRL_COLUMN_INDEX].Width = new GridLength(DEFAULT_COLUMN_WIDTHS[TAB_CTRL_COLUMN_INDEX] - chessboardAdj - scoresheetAdj);
                mainGrid.ColumnDefinitions[SCORESHEET_COLUMN_INDEX].Width = new GridLength(DEFAULT_COLUMN_WIDTHS[SCORESHEET_COLUMN_INDEX] + scoresheetAdj);
            }
        }

        /// <summary>
        /// Adjusts the row heights of the main grid to maintain the chessboard size and the overall height of the main grid,
        /// </summary>
        public static void AdjustRowHeights()
        {
            double extraHeight = LayoutState.HeightCorrectionForShape;

            Grid mainGrid = AppState.MainWin.UiMainGrid;
            double adj = LayoutState.ExplorerRowHeightAdjustment;

            if (extraHeight > 0)
            {
                double extraChessboardRowHeight = 0;
                if (extraHeight > MAX_USER_HEIGHT_ADJUSTMENT)
                {
                    extraChessboardRowHeight = extraHeight - MAX_USER_HEIGHT_ADJUSTMENT;
                    extraHeight = MAX_USER_HEIGHT_ADJUSTMENT;
                }

                // add the extraHeight to the explorer row
                mainGrid.RowDefinitions[CHESSBOARD_ROW_INDEX].Height = new GridLength(DEFAULT_ROW_HEIGHTS[CHESSBOARD_ROW_INDEX] - adj + extraChessboardRowHeight);
                mainGrid.RowDefinitions[EXPLORER_ROW_INDEX].Height = new GridLength(DEFAULT_ROW_HEIGHTS[EXPLORER_ROW_INDEX] + adj + extraHeight);
            }
            else
            {
                // we use the default heights with the explorer row height adjustment
                mainGrid.RowDefinitions[CHESSBOARD_ROW_INDEX].Height = new GridLength(DEFAULT_ROW_HEIGHTS[CHESSBOARD_ROW_INDEX] - adj);
                mainGrid.RowDefinitions[EXPLORER_ROW_INDEX].Height = new GridLength(DEFAULT_ROW_HEIGHTS[EXPLORER_ROW_INDEX] + adj);
            }
        }

        /// <summary>
        /// Adjusts the column width of the Tab Control and Scoresheet columns
        /// depending on the size mode of the tab control.
        /// </summary>
        /// <param name="sizeMode"></param>
        public static void AdjustScoresheetColumnWidth(TabControlSizeMode sizeMode)
        {
            switch (sizeMode)
            {
                case TabControlSizeMode.SHOW_ACTIVE_LINE:
                    LayoutState.ScoresheetWidthAdjustment = 0;
                    break;
                case TabControlSizeMode.HIDE_ACTIVE_LINE:
                    LayoutState.ScoresheetWidthAdjustment = -DEFAULT_COLUMN_WIDTHS[SCORESHEET_COLUMN_INDEX];
                    break;
                case TabControlSizeMode.SHOW_ACTIVE_LINE_NO_EVAL:
                case TabControlSizeMode.SHOW_ENGINE_GAME_LINE:
                    LayoutState.ScoresheetWidthAdjustment = -SCORESHEET_COL_EXTRA_WIDTH_FOR_EVALS;
                    break;
                default:
                    LayoutState.ScoresheetWidthAdjustment = 0;
                    break;
            }

            Grid mainGrid = AppState.MainWin.UiMainGrid;
            double chessboardAdj = LayoutState.ChessboardSizeAdjustment;
            double scoresheetAdj = LayoutState.ScoresheetWidthAdjustment;
            double extraWidth = Math.Max(0, LayoutState.WidthCorrectionForShape);

            mainGrid.ColumnDefinitions[TAB_CTRL_COLUMN_INDEX].Width = new GridLength(DEFAULT_COLUMN_WIDTHS[TAB_CTRL_COLUMN_INDEX] + extraWidth - chessboardAdj - scoresheetAdj);
            mainGrid.ColumnDefinitions[SCORESHEET_COLUMN_INDEX].Width = new GridLength(DEFAULT_COLUMN_WIDTHS[SCORESHEET_COLUMN_INDEX] + scoresheetAdj);
        }
    }
}

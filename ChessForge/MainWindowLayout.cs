using ChessPosition;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Manages some aspects of the panels layout in the main window.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Padding around the main tab control.
        private const int MAIN_TAB_PAD = 5;

        // Left and right padding of the CommentBox / EngineLines / EvalChart controls.
        private const int COMMENT_BOX_HORIZ_PAD = 4;

        // Left and right padding of the Scoresheet / Game Line control.
        private const int SCORESHEET_HORIZ_PAD = 4;

        // Right margin of the main tab control in the presence of the scoresheet.
        private const int RIGHT_MARGIN_WITH_SCORESHEET = 275;

        // Default width of the main window, used for resizing calculations.
        private double DEFAULT_MAIN_WIN_WIDTH;

        // Default height of the main window, used for resizing calculations.
        private double DEFAULT_MAIN_WIN_HEIGHT;

        // Default width/height ratio of the main window, used for resizing calculations.
        private double DEFAULT_MAIN_WIN_WIDTH_HEIGHT_RATIO;

        // how far to move the scoresheet to the right when it has no evals and
        // therefore the control to the left (e.g. Training Tab Conbtrol) is made wider.
        public double SCORESHEET_NO_EVALS_LEFT_MARGIN = 90;

        // Width of the scoresheet in the presence of evals.
        // TODO: get rid of this and use margins instead.
        public double SCORESHEET_WIDTH_WITH_EVALS = 260;

        // Width of the scoresheet in the absence of evals.
        public double SCORESHEET_WIDTH_NO_EVALS = 160;

        // Index of the main tab control column in the main grid.
        private int TAB_CONTROL_COLUMN_INDEX = 1;

        // The current adjustment applied to the chessboard size.
        private double chessboardSizeAdjustment = 0;

        /// <summary>
        /// Right margin of the main tab control in the presence of the scoresheet without evals.
        /// The scoresheet to the right of the main tab control is narrower here so we need
        /// a greater right margin.
        /// </summary>
        private const int RIGHT_MARGIN_WITH_SCORESHEET_NO_EVALS = 195;

        /// <summary>
        /// Default right margin of the main tab control.
        /// NOTE: it seems spurious as this method is never called 
        ///       with an unknown sizeMode.
        /// </summary>
        private const int RIGHT_MARGIN_DEFAULT = 190;

        // Default margins for the Openings control.
        private Thickness _splitterDefaultThickness;

        /// <summary>
        /// Handler for the main window SizeChanged event.
        /// Updates the widths and heights of the main window controls according to the new size of the main window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTabControlWidthHeight(e.NewSize);
            RefreshAffectedControls();
        }

        /// <summary>
        /// Sets and stores the default sizes and margins of the main window controls.
        /// </summary>
        public void InitializeLayoutConstants()
        {
            _splitterDefaultThickness = new Thickness(0, 0, 0, 0);

            // calculate the default main window width and height based on the grid definitions.
            DEFAULT_MAIN_WIN_WIDTH = LayoutUtils.MAIN_GRID_COLUMNS.Sum();
            DEFAULT_MAIN_WIN_HEIGHT = LayoutUtils.MAIN_GRID_ROWS.Sum();

            DEFAULT_MAIN_WIN_WIDTH_HEIGHT_RATIO = DEFAULT_MAIN_WIN_WIDTH / DEFAULT_MAIN_WIN_HEIGHT;
        }

        /// <summary>
        /// Initializes configurable entities.
        /// </summary>
        private void ApplyLayoutConfiguration()
        {
            if (Configuration.IsMainWinPosValid())
            {
                this.Left = Configuration.MainWinPos.Left;
                this.Top = Configuration.MainWinPos.Top;
                this.Width = Configuration.MainWinPos.Right - Configuration.MainWinPos.Left;
                this.Height = Configuration.MainWinPos.Bottom - Configuration.MainWinPos.Top;
            }

            DebugUtils.DebugLevel = Configuration.DebugLevel;

            MainBoard.Width = LayoutUtils.CHESSBOARD_DEFAULT_WIDTH;
            MainBoard.Height = LayoutUtils.CHESSBOARD_DEFAULT_WIDTH;

            // set the main grid's row and column definitions
            UiMainGrid.RowDefinitions[0].Height = new GridLength(LayoutUtils.MAIN_GRID_ROWS[0]);
            UiMainGrid.RowDefinitions[1].Height = new GridLength(LayoutUtils.MAIN_GRID_ROWS[1]);
            UiMainGrid.RowDefinitions[2].Height = new GridLength(LayoutUtils.MAIN_GRID_ROWS[2]);
            UiMainGrid.RowDefinitions[3].Height = new GridLength(LayoutUtils.MAIN_GRID_ROWS[3]);

            UiMainGrid.ColumnDefinitions[0].Width = new GridLength(LayoutUtils.MAIN_GRID_COLUMNS[0]);
            UiMainGrid.ColumnDefinitions[1].Width = new GridLength(LayoutUtils.MAIN_GRID_COLUMNS[1]);
            UiMainGrid.ColumnDefinitions[2].Width = new GridLength(LayoutUtils.MAIN_GRID_COLUMNS[2]);
            UiMainGrid.ColumnDefinitions[3].Width = new GridLength(LayoutUtils.MAIN_GRID_COLUMNS[3]);

            // setup control positions
            UiDgActiveLine.HorizontalAlignment = HorizontalAlignment.Left;
            UiDgActiveLine.Margin = new Thickness(SCORESHEET_HORIZ_PAD, 27, SCORESHEET_HORIZ_PAD, 0);

            UiLblScoresheet.HorizontalAlignment = HorizontalAlignment.Left;
            UiLblScoresheet.Margin = new Thickness(COMMENT_BOX_HORIZ_PAD, 0, COMMENT_BOX_HORIZ_PAD, 0);

            UiDgEngineGame.HorizontalAlignment = HorizontalAlignment.Left;
            UiDgEngineGame.Margin = new Thickness(COMMENT_BOX_HORIZ_PAD, 27, COMMENT_BOX_HORIZ_PAD, 0);

            SetupMenuBarControls();
        }

        /// <summary>
        /// Resizes the main tab control to show/hide ActiveLine/GameLine controls.
        /// 
        /// The main tab control exists in the following learning modes:
        /// - Manual Review : UiTabControlManualReview
        /// - Engine Game : UiTabCtrlEngineGame
        /// - Training Game : UiTabCtrlTraining
        /// </summary>
        /// <param name="tabControl"></param>
        /// <param name="sizeMode"></param>
        public void ResizeTabControl(TabControl tabControl, TabControlSizeMode sizeMode)
        {
            ThicknessUtils.SetControlLeftMargin(UiDgActiveLine, 0);
            ThicknessUtils.SetControlLeftMargin(UiDgEngineGame, SCORESHEET_NO_EVALS_LEFT_MARGIN);

            UiTrainingSessionBox.Margin = new Thickness(MAIN_TAB_PAD, LayoutUtils.EXPLORER_ROW_TOP_MARGIN, RIGHT_MARGIN_WITH_SCORESHEET_NO_EVALS, 0);

            switch (sizeMode)
            {
                case TabControlSizeMode.SHOW_ACTIVE_LINE:
                    tabControl.Margin = new Thickness(MAIN_TAB_PAD, MAIN_TAB_PAD, RIGHT_MARGIN_WITH_SCORESHEET, MAIN_TAB_PAD);

                    UiDgActiveLine.Visibility = Visibility.Visible;
                    UiLblScoresheet.Visibility = Visibility.Visible;
                    break;
                case TabControlSizeMode.HIDE_ACTIVE_LINE:
                    tabControl.Margin = new Thickness(MAIN_TAB_PAD, MAIN_TAB_PAD, MAIN_TAB_PAD, MAIN_TAB_PAD);
                    UiDgActiveLine.Visibility = Visibility.Hidden;
                    UiLblScoresheet.Visibility = Visibility.Hidden;
                    break;
                case TabControlSizeMode.SHOW_ACTIVE_LINE_NO_EVAL:
                    tabControl.Margin = new Thickness(MAIN_TAB_PAD, MAIN_TAB_PAD, RIGHT_MARGIN_WITH_SCORESHEET_NO_EVALS, MAIN_TAB_PAD);
                    UiDgActiveLine.Visibility = Visibility.Visible;
                    UiLblScoresheet.Visibility = Visibility.Visible;
                    ThicknessUtils.SetControlLeftMargin(UiDgActiveLine, SCORESHEET_NO_EVALS_LEFT_MARGIN);
                    ThicknessUtils.SetControlLeftMargin(UiDgEngineGame, SCORESHEET_NO_EVALS_LEFT_MARGIN);
                    break;
                case TabControlSizeMode.SHOW_ENGINE_GAME_LINE:
                    tabControl.Margin = new Thickness(MAIN_TAB_PAD, MAIN_TAB_PAD, RIGHT_MARGIN_WITH_SCORESHEET_NO_EVALS, MAIN_TAB_PAD);
                    UiDgActiveLine.Visibility = Visibility.Hidden;
                    UiLblScoresheet.Visibility = Visibility.Visible;
                    UiDgEngineGame.Visibility = Visibility.Visible;
                    ThicknessUtils.SetControlLeftMargin(UiDgActiveLine, SCORESHEET_NO_EVALS_LEFT_MARGIN);
                    ThicknessUtils.SetControlLeftMargin(UiDgEngineGame, SCORESHEET_NO_EVALS_LEFT_MARGIN);
                    break;
                case TabControlSizeMode.HIDE_ENGINE_GAME_LINE:
                    tabControl.Margin = new Thickness(MAIN_TAB_PAD, MAIN_TAB_PAD, MAIN_TAB_PAD, MAIN_TAB_PAD);
                    UiDgActiveLine.Visibility = Visibility.Hidden;
                    UiLblScoresheet.Visibility = Visibility.Hidden;
                    break;
                default:
                    tabControl.Margin = new Thickness(MAIN_TAB_PAD, MAIN_TAB_PAD, RIGHT_MARGIN_DEFAULT, MAIN_TAB_PAD);
                    UiDgActiveLine.Visibility = Visibility.Visible;
                    UiLblScoresheet.Visibility = Visibility.Hidden;
                    break;
            }
        }

        /// <summary>
        /// Adjusts the widths of the main main chessboard control and the main view window
        /// according to the currently selected adjustment value.
        /// When the chessboard is made wider, the main view window is made narrower so that the overall layout remains balanced.
        /// </summary>
        /// <param name="adjustment">The adjustment value to be applied to the widths.</param>
        private void UpdateMainChessboardWidths(double adjustment, bool isAbsoluteAdjustment = false)
        {
            double absoluteAdjustment = adjustment;
            if (!isAbsoluteAdjustment)
            {
                // calculate the adjustment relative to the default boundary between the first and second column.
                absoluteAdjustment = adjustment + (UiMainGrid.ColumnDefinitions[0].Width.Value - LayoutUtils.MAIN_GRID_COLUMNS[0]);
            }

            if (absoluteAdjustment > 0 || absoluteAdjustment < -LayoutUtils.MAX_USER_WIDTH_ADJUSTMENT)
            {
                // An invalid adjustment should have been caught earlier so this is just a defensive measure.
                return;
            }

            Configuration.ChessboardSizeAdjustment = (int)absoluteAdjustment;

            UiMainGrid.ColumnDefinitions[0].Width = new GridLength(UiMainGrid.ColumnDefinitions[0].Width.Value + adjustment);
            UiMainGrid.ColumnDefinitions[1].Width = new GridLength(UiMainGrid.ColumnDefinitions[1].Width.Value - adjustment);

            chessboardSizeAdjustment = absoluteAdjustment;

            MainBoard.Width = LayoutUtils.CHESSBOARD_DEFAULT_WIDTH + absoluteAdjustment;
            MainBoard.Height = LayoutUtils.CHESSBOARD_DEFAULT_WIDTH + absoluteAdjustment;
            return;
        }

        /// <summary>
        /// Adjusts the heights of the controls in the bottom half (explorer row) of the main window
        /// </summary>
        /// <param name="adjustment"></param>
        private void SetExplorerRowHeights(double adjustment)
        {
            adjustment = Math.Min(adjustment, LayoutUtils.MAX_USER_HEIGHT_ADJUSTMENT);
            adjustment = Math.Max(adjustment, LayoutUtils.MIN_USER_HEIGHT_ADJUSTMENT);

            //UiMainGrid.RowDefinitions[LayoutUtils.EXPLORER_ROW_INDEX].Height = new GridLength(MAIN_GRID_ROWS[LayoutUtils.EXPLORER_ROW_INDEX] + adjustment);
            UiMainGrid.RowDefinitions[LayoutUtils.EXPLORER_ROW_INDEX].Height = new GridLength(UiMainGrid.RowDefinitions[LayoutUtils.EXPLORER_ROW_INDEX].Height.Value + adjustment);
            UiMainGrid.RowDefinitions[LayoutUtils.CHESSBOARD_ROW_INDEX].Height = new GridLength(UiMainGrid.RowDefinitions[LayoutUtils.CHESSBOARD_ROW_INDEX].Height.Value - adjustment);

            if (UiMainGrid.RowDefinitions[LayoutUtils.CHESSBOARD_ROW_INDEX].Height.Value < (LayoutUtils.CHESSBOARD_DEFAULT_WIDTH + Configuration.ChessboardSizeAdjustment))
            {
                double absoluteAdjustment = UiMainGrid.RowDefinitions[LayoutUtils.CHESSBOARD_ROW_INDEX].Height.Value - LayoutUtils.CHESSBOARD_DEFAULT_WIDTH;
                UpdateMainChessboardWidths(absoluteAdjustment, true);
            }
        }

        /// <summary>
        /// Updates the widths and heights of the main window controls according to the current size of the main window.
        /// </summary>
        private void UpdateTabControlWidthHeight(Size windowSize)
        {
            try
            {
                // calculate the current width/height ratio of the main window client area.
                // the width is straight forward , but the height is taken as the height of the second row of the main grid,
                // so that we skip the menus.
                double actualWidthHeightRatio = windowSize.Width / _gridUber.RowDefinitions[1].ActualHeight;

                // using actualWidthHightRatio update the sizes so that the client area remains fully utilized.
                UpdateTabControlWidth(actualWidthHeightRatio);
                UpdateExplorerRowHeights(actualWidthHeightRatio);
            }
            catch { }
        }

        /// <summary>
        /// Updates the width of the main tab control according to the current size of the main window.
        /// </summary>
        /// <param name="actualWidthHeightRatio"></param>
        private void UpdateTabControlWidth(double actualWidthHeightRatio)
        {
            double defaultWidth = DEFAULT_MAIN_WIN_HEIGHT * actualWidthHeightRatio;

            // calculate the total width currently defined for the main window,
            // excluding the main tab control, and add the default width of the main tab control.
            double currentDefinedWidth = 0;
            for (int i = 0; i < UiMainGrid.ColumnDefinitions.Count; i++)
            {
                if (i != TAB_CONTROL_COLUMN_INDEX)
                {
                    currentDefinedWidth += UiMainGrid.ColumnDefinitions[i].Width.Value;
                }
            }
            currentDefinedWidth += (LayoutUtils.MAIN_GRID_COLUMNS[TAB_CONTROL_COLUMN_INDEX]);

            // by how much to adjust the defined width of the main tab control.
            double widthGapScaled = defaultWidth - currentDefinedWidth;
            if (actualWidthHeightRatio <= DEFAULT_MAIN_WIN_WIDTH_HEIGHT_RATIO)
            {
                // in this case we need to adjust the heights so don't adjust the width of the main tab control.
                widthGapScaled = -1 * chessboardSizeAdjustment;
            }

            UiMainGrid.ColumnDefinitions[TAB_CONTROL_COLUMN_INDEX].Width = new GridLength(LayoutUtils.MAIN_GRID_COLUMNS[TAB_CONTROL_COLUMN_INDEX] + widthGapScaled);
        }

        /// <summary>
        /// Updates the heights of the controls in the Explorer row according to the current size of the main window.
        /// </summary>
        /// <param name="actualWidthHeightRatio"></param>
        private void UpdateExplorerRowHeights(double actualWidthHeightRatio)
        {
            double defaultHeight = DEFAULT_MAIN_WIN_WIDTH / actualWidthHeightRatio;

            double currentDefinedHeight = 0;
            for (int i = 0; i < UiMainGrid.RowDefinitions.Count; i++)
            {
                if (i != LayoutUtils.EXPLORER_ROW_INDEX)
                {
                    currentDefinedHeight += UiMainGrid.RowDefinitions[i].Height.Value;
                }
            }

            currentDefinedHeight += UiMainGrid.RowDefinitions[LayoutUtils.EXPLORER_ROW_INDEX].Height.Value;
            //currentDefinedHeight += MAIN_GRID_ROWS[LayoutUtils.EXPLORER_ROW_INDEX];

            double heightGapScaled = defaultHeight - currentDefinedHeight;
            if (actualWidthHeightRatio >= DEFAULT_MAIN_WIN_WIDTH_HEIGHT_RATIO)
            {
                heightGapScaled = 0;
            }

            UiMainGrid.RowDefinitions[LayoutUtils.EXPLORER_ROW_INDEX].Height = new GridLength(UiMainGrid.RowDefinitions[LayoutUtils.EXPLORER_ROW_INDEX].Height.Value + heightGapScaled);
            //UiMainGrid.RowDefinitions[LayoutUtils.EXPLORER_ROW_INDEX].Height = new GridLength(MAIN_GRID_ROWS[LayoutUtils.EXPLORER_ROW_INDEX] + heightGapScaled);
        }

        /// <summary>
        /// Refreshes the controls affected by a change in the main chessboard width, which are:
        /// - The board comment RichTextBox
        /// - The opening stats view (if explorers are on)
        /// - The evaluation chart
        /// </summary>
        private void RefreshAffectedControls()
        {
            UiRtbBoardComment.Document.PageWidth = UiMainGrid.ColumnDefinitions[0].Width.Value;
            if (_openingStatsView != null && AppState.AreExplorersOn)
            {
                _openingStatsView.RebuildView();
            }
            UiEvalChart.InitSizes();
            UiEvalChart.Refresh();

            EngineLinesBox.InitSizes();

            ManualSplitterVertical.Height = UiMainGrid.RowDefinitions[1].Height.Value + UiMainGrid.RowDefinitions[2].Height.Value;
            ManualSplitterHorizontal.Width = UiMainGrid.ColumnDefinitions[0].Width.Value + UiMainGrid.ColumnDefinitions[1].Width.Value + UiMainGrid.ColumnDefinitions[2].Width.Value;
        }

        //******************************************************************************************
        //
        // Event handlers for the Vertical Splitter between the chessboard and the main tab control.
        //
        //******************************************************************************************

        // whether resizing is in progress
        private bool _isResizingTab = false;

        // position of the splitter when resizing started (equals the width of the first column)
        private double _resizeStartPointX;

        // The adjustment implied by the current position of the mouse cursor.
        // This is the position of the mouse cursor minus the position of the splitter.
        // It needs to be tracked across the mouse events as if the mouse goes too far
        // we want to keep to the last valid adjustment.
        private double _runningHorizontalAdjustment = 0;

        /// <summary>
        /// A mouse click event occured over the splitter.
        /// The resizing starts here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ManualSplitterVertical_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _runningHorizontalAdjustment = 0;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // flag that resizing is in progress
                _isResizingTab = true;
                // set the initial position of the splitter
                _resizeStartPointX = UiMainGrid.ColumnDefinitions[0].Width.Value;

                ManualSplitterVertical.CaptureMouse();
            }
        }

        /// <summary>
        /// If the mouse is moved while resizing is in progress,
        /// update the position of the splitter and the _runningAdjustment value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ManualSplitterVertical_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizingTab && e.LeftButton == MouseButtonState.Pressed)
            {
                double currX = e.GetPosition(UiMainGrid).X;

                // make sure that the user cannot move the splitter beyond the allowed limits.
                if (currX <= LayoutUtils.MAIN_GRID_COLUMNS[0] - LayoutUtils.MAX_USER_WIDTH_ADJUSTMENT)
                {
                    currX = LayoutUtils.MAIN_GRID_COLUMNS[0] - LayoutUtils.MAX_USER_WIDTH_ADJUSTMENT;
                }
                else if (currX > LayoutUtils.MAIN_GRID_COLUMNS[0])
                {
                    currX = LayoutUtils.MAIN_GRID_COLUMNS[0];
                }

                ManualSplitterVertical.Fill = Brushes.Gray;
                ManualSplitterVertical.Opacity = 0.8;

                _runningHorizontalAdjustment = currX - _resizeStartPointX;

                ManualSplitterVertical.Margin = new Thickness(
                    _splitterDefaultThickness.Left + _runningHorizontalAdjustment,
                    _splitterDefaultThickness.Top,
                    _splitterDefaultThickness.Right - _runningHorizontalAdjustment,
                    _splitterDefaultThickness.Bottom);
            }
        }

        /// <summary>
        /// Mouse button released while resizing is in progress.
        /// Tidy up and resize the affected controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ManualSplitterVertical_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizingTab)
            {
                ManualSplitterVertical.Fill = Brushes.Transparent;
                ManualSplitterVertical.Opacity = 0;

                _isResizingTab = false;
                ManualSplitterVertical.ReleaseMouseCapture();
                ManualSplitterVertical.Margin = _splitterDefaultThickness;

                double absoluteAdjustmentWidth = UiMainGrid.ColumnDefinitions[LayoutUtils.CHESSBOARD_COLUMN_INDEX].Width.Value
                                  - LayoutUtils.MAIN_GRID_COLUMNS[LayoutUtils.CHESSBOARD_COLUMN_INDEX] + _runningHorizontalAdjustment;

                double absoluteAdjustmentHeight =
                    (UiMainGrid.RowDefinitions[LayoutUtils.EXPLORER_ROW_INDEX].Height.Value - LayoutUtils.MAIN_GRID_ROWS[LayoutUtils.EXPLORER_ROW_INDEX]);

                LayoutUtils.CoordinateChessboardExplorerRowAdjustments(ref absoluteAdjustmentWidth, ref absoluteAdjustmentHeight, false);

                AdjustAllControlSizes(absoluteAdjustmentWidth, absoluteAdjustmentHeight);

                RefreshAffectedControls();
            }
        }


        //******************************************************************************************
        //
        // Event handlers for the Horizontal Splitter between the chessboard and the main tab control.
        //
        //******************************************************************************************

        // whether resizing is in progress
        private bool _isHorizontalResizing = false;

        // position of the splitter when resizing started (equals the height of the first row)
        private double _resizeStartPointY;

        // The adjustment implied by the current position of the mouse cursor.
        // This is the position of the mouse cursor minus the position of the splitter.
        // It needs to be tracked across the mouse events as if the mouse goes too far
        // we want to keep to the last valid adjustment.
        private double _runningVerticalAdjustment = 0;

        /// <summary>
        /// A mouse click event occured over the splitter.
        /// The resizing starts here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ManualSplitterHorizontal_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _runningVerticalAdjustment = 0;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // flag that resizing is in progress
                _isHorizontalResizing = true;
                // set the initial position of the splitter
                _resizeStartPointY = UiMainGrid.RowDefinitions[0].Height.Value + UiMainGrid.RowDefinitions[1].Height.Value;

                ManualSplitterHorizontal.CaptureMouse();
            }
        }

        /// <summary>
        /// If the mouse is moved while resizing is in progress,
        /// update the position of the splitter and the _runningHorizontalAdjustment value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ManualSplitterHorizontal_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isHorizontalResizing && e.LeftButton == MouseButtonState.Pressed)
            {
                double currY = e.GetPosition(UiMainGrid).Y;

                double topRowsCombinedHeight = LayoutUtils.MAIN_GRID_ROWS[0] + LayoutUtils.MAIN_GRID_ROWS[1];

                // make sure that the user cannot move the splitter beyond the allowed limits.
                if (currY <= topRowsCombinedHeight - LayoutUtils.MAX_USER_HEIGHT_ADJUSTMENT)
                {
                    currY = topRowsCombinedHeight - LayoutUtils.MAX_USER_HEIGHT_ADJUSTMENT;
                }
                else if (currY > topRowsCombinedHeight - LayoutUtils.MIN_USER_HEIGHT_ADJUSTMENT)
                {
                    currY = topRowsCombinedHeight - LayoutUtils.MIN_USER_HEIGHT_ADJUSTMENT;
                }

                ManualSplitterHorizontal.Fill = Brushes.Gray;
                ManualSplitterHorizontal.Opacity = 0.8;

                _runningVerticalAdjustment = _resizeStartPointY - currY;

                ManualSplitterHorizontal.Margin = new Thickness(
                    _splitterDefaultThickness.Left,
                    _splitterDefaultThickness.Top - _runningVerticalAdjustment,
                    _splitterDefaultThickness.Right,
                    _splitterDefaultThickness.Bottom + _runningVerticalAdjustment);
            }
        }

        /// <summary>
        /// Mouse button released while resizing is in progress.
        /// Tidy up and resize the affected controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ManualSplitterHorizontal_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isHorizontalResizing)
            {
                ManualSplitterHorizontal.Fill = Brushes.Transparent;
                ManualSplitterHorizontal.Opacity = 0;

                _isHorizontalResizing = false;
                ManualSplitterHorizontal.ReleaseMouseCapture();
                ManualSplitterHorizontal.Margin = _splitterDefaultThickness;

                double absoluteAdjustmentWidth = UiMainGrid.ColumnDefinitions[LayoutUtils.CHESSBOARD_COLUMN_INDEX].Width.Value
                                                  - LayoutUtils.MAIN_GRID_COLUMNS[LayoutUtils.CHESSBOARD_COLUMN_INDEX];

                double absoluteAdjustmentHeight = 
                    (UiMainGrid.RowDefinitions[LayoutUtils.EXPLORER_ROW_INDEX].Height.Value - LayoutUtils.MAIN_GRID_ROWS[LayoutUtils.EXPLORER_ROW_INDEX])
                    + _runningVerticalAdjustment;

                LayoutUtils.CoordinateChessboardExplorerRowAdjustments(ref absoluteAdjustmentWidth, ref absoluteAdjustmentHeight, false);

                AdjustAllControlSizes(absoluteAdjustmentWidth, absoluteAdjustmentHeight);
                RefreshAffectedControls();
            }
        }

        /// <summary>
        /// Adjusts the widths and heights of the main window controls according to the given adjustments.
        /// </summary>
        /// <param name="chessboardAdjustmentWidth"></param>
        /// <param name="explorerRowAdjustmentHeight"></param>
        private void AdjustAllControlSizes(double chessboardAdjustmentWidth, double explorerRowAdjustmentHeight)
        {
            UiMainGrid.RowDefinitions[LayoutUtils.EXPLORER_ROW_INDEX].Height = new GridLength(LayoutUtils.MAIN_GRID_ROWS[LayoutUtils.EXPLORER_ROW_INDEX] + explorerRowAdjustmentHeight);
            UiMainGrid.RowDefinitions[LayoutUtils.CHESSBOARD_ROW_INDEX].Height = new GridLength(LayoutUtils.MAIN_GRID_ROWS[LayoutUtils.CHESSBOARD_ROW_INDEX] - explorerRowAdjustmentHeight);

            UiMainGrid.ColumnDefinitions[0].Width = new GridLength(LayoutUtils.MAIN_GRID_COLUMNS[LayoutUtils.CHESSBOARD_COLUMN_INDEX] + chessboardAdjustmentWidth);
            UiMainGrid.ColumnDefinitions[1].Width = new GridLength(LayoutUtils.MAIN_GRID_COLUMNS[1] - chessboardAdjustmentWidth);

            MainBoard.Width = Math.Min(UiMainGrid.ColumnDefinitions[0].Width.Value, UiMainGrid.RowDefinitions[1].Height.Value);
            MainBoard.Height = MainBoard.Width;

            UpdateTabControlWidthHeight(new Size(this.Width, this.Height));
        }
    }
}

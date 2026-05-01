using ChessPosition;
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
        // Default width (and height) of the main chessboard.
        private const double CHESSBOARD_DEFAULT_WIDTH = 680;

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

        // original grid row/column height/width definitions for the main grid.
        private double[] MAIN_GRID_ROWS = { 1.0, 680.0, 160.0, 0 };
        private double[] MAIN_GRID_COLUMNS = { 680.0, 600.0, 270.0, 1.0 };

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

        /// <summary>
        /// The maximum width adjustment that the user can apply to the main tab control.
        /// </summary>
        private int MAX_USER_WIDTH_ADJUSTMENT = 400;

        /// <summary>
        /// The maximum height adjustment that the user can apply to the main tab control.
        /// </summary>
        private int MAX_USER_HEIGHT_ADJUSTMENT = 400;

        // Default margins for the main window controls (Manual Review, Training, Game).
        private Thickness _mainTabCtrlDefaultThickness;

        // Default margins for the CommentBox, EngineLines and EvalChart controls.
        private Thickness _wndCommentBoxDefaultThickness;

        // Default margins for the Openings control.
        private Thickness _rtbOpeningsDefaultThickness;

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
            DEFAULT_MAIN_WIN_WIDTH = MAIN_GRID_COLUMNS.Sum();
            DEFAULT_MAIN_WIN_HEIGHT = MAIN_GRID_ROWS.Sum();

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

            MainBoard.Width = CHESSBOARD_DEFAULT_WIDTH;
            MainBoard.Height = CHESSBOARD_DEFAULT_WIDTH;

            // set the main grid's row and column definitions
            _gridMain.RowDefinitions[0].Height = new GridLength(MAIN_GRID_ROWS[0]);
            _gridMain.RowDefinitions[1].Height = new GridLength(MAIN_GRID_ROWS[1]);
            _gridMain.RowDefinitions[2].Height = new GridLength(MAIN_GRID_ROWS[2]);
            _gridMain.RowDefinitions[3].Height = new GridLength(MAIN_GRID_ROWS[3]);

            _gridMain.ColumnDefinitions[0].Width = new GridLength(MAIN_GRID_COLUMNS[0]);
            _gridMain.ColumnDefinitions[1].Width = new GridLength(MAIN_GRID_COLUMNS[1]);
            _gridMain.ColumnDefinitions[2].Width = new GridLength(MAIN_GRID_COLUMNS[2]);
            _gridMain.ColumnDefinitions[3].Width = new GridLength(MAIN_GRID_COLUMNS[3]);

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
        private void UpdateMainChessboardWidths(double adjustment)
        {
            // calculate the adjustment relative to the default boundary between the first and second column.
            double absoluteAdjustment = adjustment + (_gridMain.ColumnDefinitions[0].Width.Value - MAIN_GRID_COLUMNS[0]);
            if (absoluteAdjustment > 0 || absoluteAdjustment < -MAX_USER_WIDTH_ADJUSTMENT)
            {
                // An invalid adjustment should have been caught earlier so this is just a defensive measure.
                return;
            }

            Configuration.ChessboardSizeAdjustment = (int)absoluteAdjustment;

            _gridMain.ColumnDefinitions[0].Width = new GridLength(_gridMain.ColumnDefinitions[0].Width.Value + adjustment);
            _gridMain.ColumnDefinitions[1].Width = new GridLength(_gridMain.ColumnDefinitions[1].Width.Value - adjustment);

            chessboardSizeAdjustment = absoluteAdjustment;

            MainBoard.Width = CHESSBOARD_DEFAULT_WIDTH + absoluteAdjustment;
            MainBoard.Height = CHESSBOARD_DEFAULT_WIDTH + absoluteAdjustment;
            return;
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
                double actualWidthHightRatio = windowSize.Width / _gridUber.RowDefinitions[1].ActualHeight;

                // using actualWidthHightRatio update the sizes so that the client area remains fully utilized.
                UpdateTabControlWidth(actualWidthHightRatio);
                UpdateBottomHalfControlHeights(actualWidthHightRatio);
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
            for (int i = 0; i < _gridMain.ColumnDefinitions.Count; i++)
            {
                if (i != TAB_CONTROL_COLUMN_INDEX)
                {
                    currentDefinedWidth += _gridMain.ColumnDefinitions[i].Width.Value;
                }
            }
            currentDefinedWidth += (MAIN_GRID_COLUMNS[TAB_CONTROL_COLUMN_INDEX]);

            // by how much to adjust the defined width of the main tab control.
            double widthGapScaled = defaultWidth - currentDefinedWidth;
            if (actualWidthHeightRatio <= DEFAULT_MAIN_WIN_WIDTH_HEIGHT_RATIO)
            {
                // in this case we need to adjust the heights so don't adjust the width of the main tab control.
                widthGapScaled = -1 * chessboardSizeAdjustment;
            }

            _gridMain.ColumnDefinitions[TAB_CONTROL_COLUMN_INDEX].Width = new GridLength(MAIN_GRID_COLUMNS[TAB_CONTROL_COLUMN_INDEX] + widthGapScaled);
        }

        /// <summary>
        /// Updates the heights of the controls in the bottom half of the main window according to the current size of the main window.
        /// </summary>
        /// <param name="actualWidthHightRatio"></param>
        private void UpdateBottomHalfControlHeights(double actualWidthHightRatio)
        {
            double defaultHeight = DEFAULT_MAIN_WIN_WIDTH / actualWidthHightRatio;

            double currentDefinedHeight = 0;
            for (int i = 0; i < _gridMain.RowDefinitions.Count; i++)
            {
                if (i != LayoutUtils.EXPLORER_ROW_INDEX)
                {
                    currentDefinedHeight += _gridMain.RowDefinitions[i].Height.Value;
                }
            }
            currentDefinedHeight += MAIN_GRID_ROWS[LayoutUtils.EXPLORER_ROW_INDEX];

            double heightGapScaled = defaultHeight - currentDefinedHeight;
            if (actualWidthHightRatio >= DEFAULT_MAIN_WIN_WIDTH_HEIGHT_RATIO)
            {
                heightGapScaled = 0;
            }

            _gridMain.RowDefinitions[LayoutUtils.EXPLORER_ROW_INDEX].Height = new GridLength(MAIN_GRID_ROWS[LayoutUtils.EXPLORER_ROW_INDEX] + heightGapScaled);
        }

        /// <summary>
        /// Refreshes the controls affected by a change in the main chessboard width, which are:
        /// - The board comment RichTextBox
        /// - The opening stats view (if explorers are on)
        /// - The evaluation chart
        /// </summary>
        private void RefreshAffectedControls()
        {
            UiRtbBoardComment.Document.PageWidth = _gridMain.ColumnDefinitions[0].Width.Value;
            if (_openingStatsView != null && AppState.AreExplorersOn)
            {
                _openingStatsView.RebuildView();
            }
            UiEvalChart.InitSizes();
            UiEvalChart.Refresh();

            EngineLinesBox.InitSizes();

            ManualSplitterVertical.Height = _gridMain.RowDefinitions[1].Height.Value + _gridMain.RowDefinitions[2].Height.Value;
            ManualSplitterHorizontal.Width = _gridMain.ColumnDefinitions[0].Width.Value + _gridMain.ColumnDefinitions[1].Width.Value + _gridMain.ColumnDefinitions[2].Width.Value;
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
        private double _runningAdjustment = 0;

        /// <summary>
        /// A mouse click event occured over the splitter.
        /// The resizing starts here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ManualSplitterVertical_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _runningAdjustment = 0;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // flag that resizing is in progress
                _isResizingTab = true;
                // set the initial position of the splitter
                _resizeStartPointX = _gridMain.ColumnDefinitions[0].Width.Value;

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
                double currX = e.GetPosition(_gridMain).X;

                // make sure that the user cannot move the splitter beyond the allowed limits.
                if (currX <= MAIN_GRID_COLUMNS[0] - MAX_USER_WIDTH_ADJUSTMENT)
                {
                    currX = MAIN_GRID_COLUMNS[0] - MAX_USER_WIDTH_ADJUSTMENT;
                }
                else if (currX > MAIN_GRID_COLUMNS[0])
                {
                    currX = MAIN_GRID_COLUMNS[0];
                }

                ManualSplitterVertical.Fill = Brushes.Gray;
                ManualSplitterVertical.Opacity = 0.8;

                _runningAdjustment = currX - _resizeStartPointX;

                ManualSplitterVertical.Margin = new Thickness(
                    _splitterDefaultThickness.Left + _runningAdjustment,
                    _splitterDefaultThickness.Top,
                    _splitterDefaultThickness.Right - _runningAdjustment,
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

                UpdateMainChessboardWidths(_runningAdjustment);
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
                _resizeStartPointY = _gridMain.RowDefinitions[0].Height.Value + _gridMain.RowDefinitions[1].Height.Value;

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
                double currY = e.GetPosition(_gridMain).Y;

                double topRowsCombinedHeight = MAIN_GRID_ROWS[0] + MAIN_GRID_ROWS[1];

                // make sure that the user cannot move the splitter beyond the allowed limits.
                if (currY <= topRowsCombinedHeight - MAX_USER_HEIGHT_ADJUSTMENT)
                {
                    currY = topRowsCombinedHeight - MAX_USER_HEIGHT_ADJUSTMENT;
                }
                else if (currY > topRowsCombinedHeight + 100)
                {
                    currY = topRowsCombinedHeight + 100;
                }

                ManualSplitterHorizontal.Fill = Brushes.Gray;
                ManualSplitterHorizontal.Opacity = 0.8;

                _runningVerticalAdjustment = currY - _resizeStartPointY;

                ManualSplitterHorizontal.Margin = new Thickness(
                    _splitterDefaultThickness.Left,
                    _splitterDefaultThickness.Top + _runningVerticalAdjustment,
                    _splitterDefaultThickness.Right,
                    _splitterDefaultThickness.Bottom - _runningVerticalAdjustment);
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

                //UpdateMainChessboardWidths(_runningAdjustment);
                //RefreshAffectedControls();
            }
        }
    }
}

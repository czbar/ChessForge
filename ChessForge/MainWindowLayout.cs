using ChessPosition;
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
        public double MainChessBoardWidthAdjustment = 0;

        // Default width (and height) of the main chessboard.
        private const double CHESSBOARD_DEFAULT_WIDTH = 680;

        // Padding around the main tab control.
        private const int MAIN_TAB_PAD = 5;

        // Left and right padding of the CommentBox / EngineLines / EvalChart controls.
        private const int COMMENT_BOX_HORIZ_PAD = 4;

        // Left and right padding of the Scoresheet / Game Line control.
        private const int SCORESHEET_HORIZ_PAD = 4;

        // Top padding of the CommentBox / EngineLines / EvalChart and Explorer controls.
        private const int SECOND_ROW_TOP_PAD = 10;

        // Right margin of the main tab control in the presence of the scoresheet.
        private const int RIGHT_MARGIN_WITH_SCORESHEET = 275;

        // original grid row/column height/width definitions for the main grid.
        private double[] MAIN_GRID_ROWS = { 1.0, 680.0, 160.0, 20.0 };
        private double[] MAIN_GRID_COLUMNS = { 680.0, 600.0, 270.0, 1.0 };

        // Adjustment of the width of the second column of the main grid compared to its default size.
        public double ABSOLUTE_ADJUSTMENT = 0;

        // how far to move the scoresheet to the right when it has no evals and
        // therefore the control to the left (e.g. Training Tab Conbtrol) is made wider.
        public double SCORESHEET_NO_EVALS_LEFT_MARGIN = 90;

        // Width of the scoresheet in the presence of evals.
        // TODO: get rid of this and use margins instead.
        public double SCORESHEET_WIDTH_WITH_EVALS = 260;

        // Width of the scoresheet in the absence of evals.
        public double SCORESHEET_WIDTH_NO_EVALS = 160;

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
        /// The maximum adjustment that the user can apply to the main tab control.
        /// </summary>
        private int MAX_USER_WIDTH_ADJUSTMENT = 400;

        // Default margins for the main window controls (Manual Review, Training, Game).
        private Thickness _mainTabCtrlDefaultThickness;

        // Default margins for the CommentBox, EngineLines and EvalChart controls.
        private Thickness _wndCommentBoxDefaultThickness;

        // Default margins for the Openings control.
        private Thickness _rtbOpeningsDefaultThickness;

        // Default margins for the Openings control.
        private Thickness _splitterDefaultThickness;

        /// <summary>
        /// Sets and stores the default sizes and margins of the main window controls.
        /// </summary>
        public void InitializeLayout()
        {
            // The main tab control in all learning modes, has the width and height determined
            // by the margins. The width and the height are not set explicitly.
            _mainTabCtrlDefaultThickness = new Thickness(MAIN_TAB_PAD, MAIN_TAB_PAD, MAIN_TAB_PAD, MAIN_TAB_PAD);
            _wndCommentBoxDefaultThickness = new Thickness(COMMENT_BOX_HORIZ_PAD, SECOND_ROW_TOP_PAD, COMMENT_BOX_HORIZ_PAD, 0);
            _rtbOpeningsDefaultThickness = new Thickness(MAIN_TAB_PAD, SECOND_ROW_TOP_PAD, RIGHT_MARGIN_WITH_SCORESHEET, 0);

            _splitterDefaultThickness = new Thickness(0, 0, 0, 0);
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
        /// Return the default top margin (padding) for the controls in the second row.
        /// </summary>
        /// <returns></returns>
        public double GetSecondRowTopPad()
        {
            return SECOND_ROW_TOP_PAD;
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
        /// Adjusts the widths of the main tab control and the controls
        /// according to the currently selected adjustment value.
        /// </summary>
        /// <param name="adjustment"></param>
        private void AdjustPanelWidths(double adjustment)
        {
            // calculate the adjustment relative to the default boundary between the first and second column.
            double absoluteAdjustment = _runningAdjustment + (_gridMain.ColumnDefinitions[0].Width.Value - MAIN_GRID_COLUMNS[0]);
            if (absoluteAdjustment > 0 || absoluteAdjustment < -MAX_USER_WIDTH_ADJUSTMENT)
            {
                // An invalid adjustment should have been caught earlier so this is just a defensive measure.
                return;
            }

            ABSOLUTE_ADJUSTMENT = absoluteAdjustment;

            _gridMain.ColumnDefinitions[0].Width = new GridLength(_gridMain.ColumnDefinitions[0].Width.Value + adjustment);
            _gridMain.ColumnDefinitions[1].Width = new GridLength(_gridMain.ColumnDefinitions[1].Width.Value - adjustment);

            UiRtbBoardComment.Document.PageWidth = _gridMain.ColumnDefinitions[0].Width.Value;

            MainBoard.Width = CHESSBOARD_DEFAULT_WIDTH + absoluteAdjustment;
            MainBoard.Height = CHESSBOARD_DEFAULT_WIDTH + absoluteAdjustment;

            if (_openingStatsView != null && AppState.AreExplorersOn)
            {
                _openingStatsView.RebuildView(absoluteAdjustment);
            }

            return;
        }


        //**************************************************
        //
        // Manual splitter event handlers
        //
        //**************************************************

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
        private void ManualSplitter_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _runningAdjustment = 0;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // flag that resizing is in progress
                _isResizingTab = true;
                // set the initial position of the splitter
                _resizeStartPointX = _gridMain.ColumnDefinitions[0].Width.Value;

                ManualSplitter.CaptureMouse();
            }
        }

        /// <summary>
        /// If the mouse is moved while resizing is in progress,
        /// update the position of the splitter and the _runningAdjustment value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ManualSplitter_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizingTab)
            {
                Point current = e.GetPosition(_gridMain);
                // check if the mouse is within the limits of the allowed resizing
                if (current.X > MAIN_GRID_COLUMNS[0] - MAX_USER_WIDTH_ADJUSTMENT && current.X <= MAIN_GRID_COLUMNS[0])
                {
                    ManualSplitter.Fill = Brushes.Gray;
                    ManualSplitter.Opacity = 0.8;

                    _runningAdjustment = current.X - _resizeStartPointX;

                    ManualSplitter.Margin = new Thickness(
                        _splitterDefaultThickness.Left + _runningAdjustment,
                        _splitterDefaultThickness.Top,
                        _splitterDefaultThickness.Right - _runningAdjustment,
                        _splitterDefaultThickness.Bottom);
                }
            }
        }

        /// <summary>
        /// Mouse button released while resizing is in progress.
        /// Tidy up and resize the affected controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ManualSplitter_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizingTab)
            {
                ManualSplitter.Fill = Brushes.Transparent;
                ManualSplitter.Opacity = 0;

                _isResizingTab = false;
                ManualSplitter.ReleaseMouseCapture();
                ManualSplitter.Margin = _splitterDefaultThickness;

                AdjustPanelWidths(_runningAdjustment);
            }
        }
    }
}

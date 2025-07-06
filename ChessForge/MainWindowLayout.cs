using ChessPosition;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Manages some aspects of the panels layout in the main window.
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double CHESSBOARD_DEFAULT_WIDTH = 680;

        // Padding around the main tab control.
        private const int MAIN_TAB_PAD = 5;

        // Left and right padding of the CommentBox / EngineLines / EvalChart controls.
        private const int COMMENT_BOX_PAD = 4;

        // Top padding of the CommentBox / EngineLines / EvalChart and Explorer controls.
        private const int SECOND_ROW_TOP_PAD = 10;

        // Right margin of the main tab control in the presence of the scoresheet.
        private const int RIGHT_MARGIN_WITH_SCORESHEET = 275;

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
        /// The adjustment that the user manually performed in the GUI.
        /// It must be a positive value and it is reflected in the value of the left margin
        /// of the main tab control.
        /// Therefore, the user can make it wider (and the main board will be narrowed accordingly).
        /// </summary>
        private int USER_WIDTH_ADJUSTMENT = 0;

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

        // Default width and height of the main board.
        private double _mainBoardDefaultWidth;

        /// <summary>
        /// Sets and stores the default sizes and margins of the main window controls.
        /// </summary>
        public void InitializeLayout()
        {
            // The main tab control in all learning modes, has the width and height determined
            // by the margins. The width and the height are not set explicitly.
            _mainTabCtrlDefaultThickness = new Thickness(MAIN_TAB_PAD, MAIN_TAB_PAD, MAIN_TAB_PAD, MAIN_TAB_PAD);
            _wndCommentBoxDefaultThickness = new Thickness(COMMENT_BOX_PAD, SECOND_ROW_TOP_PAD, COMMENT_BOX_PAD, 0);
            _rtbOpeningsDefaultThickness = new Thickness(MAIN_TAB_PAD, SECOND_ROW_TOP_PAD, RIGHT_MARGIN_WITH_SCORESHEET, 0);

            _mainBoardDefaultWidth = CHESSBOARD_DEFAULT_WIDTH;
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
            int leftPad = MAIN_TAB_PAD - USER_WIDTH_ADJUSTMENT;

            switch (sizeMode)
            {
                case TabControlSizeMode.SHOW_ACTIVE_LINE:
                    tabControl.Margin = new Thickness(leftPad, MAIN_TAB_PAD, RIGHT_MARGIN_WITH_SCORESHEET, MAIN_TAB_PAD);

                    UiDgActiveLine.Visibility = Visibility.Visible;
                    PositionScoresheetLabel(UiDgActiveLine);
                    UiLblScoresheet.Visibility = Visibility.Visible;
                    PositionScoresheetLabel(UiDgActiveLine);
                    //UiLblScoresheet.Margin = new Thickness(0, 0, 10 + (UiDgActiveLine.Width - UiLblScoresheet.Width), 0);
                    break;
                case TabControlSizeMode.HIDE_ACTIVE_LINE:
                    tabControl.Margin = new Thickness(leftPad, MAIN_TAB_PAD, MAIN_TAB_PAD, MAIN_TAB_PAD);
                    UiDgActiveLine.Visibility = Visibility.Hidden;
                    UiLblScoresheet.Visibility = Visibility.Hidden;
                    break;
                case TabControlSizeMode.SHOW_ACTIVE_LINE_NO_EVAL:
                    tabControl.Margin = new Thickness(leftPad, MAIN_TAB_PAD, RIGHT_MARGIN_WITH_SCORESHEET_NO_EVALS, MAIN_TAB_PAD);
                    UiDgActiveLine.Visibility = Visibility.Visible;
                    PositionScoresheetLabel(UiDgActiveLine);
                    UiLblScoresheet.Visibility = Visibility.Visible;
                    break;
                case TabControlSizeMode.SHOW_ENGINE_GAME_LINE:
                    tabControl.Margin = new Thickness(leftPad, MAIN_TAB_PAD, RIGHT_MARGIN_WITH_SCORESHEET_NO_EVALS, MAIN_TAB_PAD);
                    UiDgActiveLine.Visibility = Visibility.Hidden;
                    PositionScoresheetLabel(UiDgEngineGame);
                    UiLblScoresheet.Visibility = Visibility.Visible;
                    UiDgEngineGame.Visibility = Visibility.Visible;
                    break;
                case TabControlSizeMode.HIDE_ENGINE_GAME_LINE:
                    tabControl.Margin = new Thickness(leftPad, MAIN_TAB_PAD, MAIN_TAB_PAD, MAIN_TAB_PAD);
                    UiDgActiveLine.Visibility = Visibility.Hidden;
                    UiLblScoresheet.Visibility = Visibility.Hidden;
                    break;
                default:
                    tabControl.Margin = new Thickness(leftPad, MAIN_TAB_PAD, RIGHT_MARGIN_DEFAULT, MAIN_TAB_PAD);
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
        private void AdjustPanelWidths(int adjustment)
        {
            if (adjustment < 0 || adjustment > MAX_USER_WIDTH_ADJUSTMENT)
            {
                // Invalid adjustment, ignore it.
                return; 
            }

            USER_WIDTH_ADJUSTMENT = adjustment;

            ThicknessUtils.AdjustControlLeftMargin(UiTabCtrlManualReview, -USER_WIDTH_ADJUSTMENT);
            ThicknessUtils.AdjustControlLeftMargin(UiTabCtrlEngineGame, -USER_WIDTH_ADJUSTMENT);
            ThicknessUtils.AdjustControlLeftMargin(UiTabCtrlTraining, -USER_WIDTH_ADJUSTMENT);

            ThicknessUtils.AdjustControlRightMargin(UiTbEngineLines, USER_WIDTH_ADJUSTMENT);
            ThicknessUtils.AdjustControlRightMargin(UiEvalChart, USER_WIDTH_ADJUSTMENT);
            ThicknessUtils.AdjustControlRightMargin(UiRtbBoardComment, USER_WIDTH_ADJUSTMENT);

            ThicknessUtils.AdjustControlLeftMargin(UiRtbOpenings, -USER_WIDTH_ADJUSTMENT);

            MainBoard.Width -= USER_WIDTH_ADJUSTMENT;
            MainBoard.Height -= USER_WIDTH_ADJUSTMENT;
        }

        /// <summary>
        /// Adjusts the position of the "Scoresheet" label in relation
        /// to the Scoresheet (DataGrid) control it associated with
        /// </summary>
        /// <param name="dgControl"></param>
        private void PositionScoresheetLabel(DataGrid dgControl)
        {
            UiLblScoresheet.Margin = new Thickness(0, 0, 10 + (dgControl.Width - UiLblScoresheet.Width), 0);
        }

    }
}

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
        // Padding around the main tab control.
        private const int PADDING = 5;

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

        /// <summary>
        /// Sets and stores the default sizes and margins of the main window controls.
        /// </summary>
        private void InitializeLayout()
        {
            // TODO: Implement
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
            int leftPad = PADDING - USER_WIDTH_ADJUSTMENT;

            switch (sizeMode)
            {
                case TabControlSizeMode.SHOW_ACTIVE_LINE:
                    tabControl.Margin = new Thickness(leftPad, PADDING, RIGHT_MARGIN_WITH_SCORESHEET, PADDING);

                    UiDgActiveLine.Visibility = Visibility.Visible;
                    PositionScoresheetLabel(UiDgActiveLine);
                    UiLblScoresheet.Visibility = Visibility.Visible;
                    PositionScoresheetLabel(UiDgActiveLine);
                    //UiLblScoresheet.Margin = new Thickness(0, 0, 10 + (UiDgActiveLine.Width - UiLblScoresheet.Width), 0);
                    break;
                case TabControlSizeMode.HIDE_ACTIVE_LINE:
                    tabControl.Margin = new Thickness(leftPad, PADDING, PADDING, PADDING);
                    UiDgActiveLine.Visibility = Visibility.Hidden;
                    UiLblScoresheet.Visibility = Visibility.Hidden;
                    break;
                case TabControlSizeMode.SHOW_ACTIVE_LINE_NO_EVAL:
                    tabControl.Margin = new Thickness(leftPad, PADDING, RIGHT_MARGIN_WITH_SCORESHEET_NO_EVALS, PADDING);
                    UiDgActiveLine.Visibility = Visibility.Visible;
                    PositionScoresheetLabel(UiDgActiveLine);
                    UiLblScoresheet.Visibility = Visibility.Visible;
                    break;
                case TabControlSizeMode.SHOW_ENGINE_GAME_LINE:
                    tabControl.Margin = new Thickness(leftPad, PADDING, RIGHT_MARGIN_WITH_SCORESHEET_NO_EVALS, PADDING);
                    UiDgActiveLine.Visibility = Visibility.Hidden;
                    PositionScoresheetLabel(UiDgEngineGame);
                    UiLblScoresheet.Visibility = Visibility.Visible;
                    UiDgEngineGame.Visibility = Visibility.Visible;
                    break;
                case TabControlSizeMode.HIDE_ENGINE_GAME_LINE:
                    tabControl.Margin = new Thickness(leftPad, PADDING, PADDING, PADDING);
                    UiDgActiveLine.Visibility = Visibility.Hidden;
                    UiLblScoresheet.Visibility = Visibility.Hidden;
                    break;
                default:
                    tabControl.Margin = new Thickness(leftPad, PADDING, RIGHT_MARGIN_DEFAULT, PADDING);
                    UiDgActiveLine.Visibility = Visibility.Visible;
                    UiLblScoresheet.Visibility = Visibility.Hidden;
                    break;
            }
        }

        private void AdjustPanelWidths(int adjustment)
        {
            if (adjustment < 0 || adjustment > MAX_USER_WIDTH_ADJUSTMENT)
            {
                return; // Invalid adjustment
            }

            USER_WIDTH_ADJUSTMENT = adjustment;

            AdjustTabControlWidth(UiTabCtrlManualReview, USER_WIDTH_ADJUSTMENT);
            AdjustTabControlWidth(UiTabCtrlEngineGame, USER_WIDTH_ADJUSTMENT);
            AdjustTabControlWidth(UiTabCtrlTraining, USER_WIDTH_ADJUSTMENT);

            UiTbEngineLines.Margin = new Thickness(UiTbEngineLines.Margin.Left, UiTbEngineLines.Margin.Top, UiTbEngineLines.Margin.Right + USER_WIDTH_ADJUSTMENT, UiTbEngineLines.Margin.Bottom);
            UiEvalChart.Margin = new Thickness(UiEvalChart.Margin.Left, UiEvalChart.Margin.Top, UiEvalChart.Margin.Right + USER_WIDTH_ADJUSTMENT, UiEvalChart.Margin.Bottom);
            UiRtbBoardComment.Margin = new Thickness(UiRtbBoardComment.Margin.Left, UiRtbBoardComment.Margin.Top, UiRtbBoardComment.Margin.Right + USER_WIDTH_ADJUSTMENT, UiRtbBoardComment.Margin.Bottom);
        
            UiRtbOpenings.Margin = new Thickness(UiRtbOpenings.Margin.Left - USER_WIDTH_ADJUSTMENT, UiRtbOpenings.Margin.Top, UiRtbOpenings.Margin.Right, UiRtbOpenings.Margin.Bottom);
            UiRtbOpenings.Width += USER_WIDTH_ADJUSTMENT;

            MainBoard.Width -= USER_WIDTH_ADJUSTMENT;
            MainBoard.Height -= USER_WIDTH_ADJUSTMENT;
        }

        private void AdjustTabControlWidth(TabControl tabControl, int adjustment)
        {
            tabControl.Margin = new Thickness(PADDING - USER_WIDTH_ADJUSTMENT, tabControl.Margin.Top, tabControl.Margin.Right, tabControl.Margin.Bottom);
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

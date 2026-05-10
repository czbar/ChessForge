using ChessPosition;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Manages some aspects of the panels layout in the main window.
    /// </summary>
    public partial class MainWindow : Window
    {
        // The new size of the main window after resizing is completed.
        private static Size _newAppWindowSize = new Size(0, 0);

        /// <summary>
        /// Handler for the main window SizeChanged event.
        /// Updates the widths and heights of the main window controls according to the new size of the main window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            vbMainWindow.Stretch = System.Windows.Media.Stretch.Uniform;
            Timers.AppWindowSizeChangedTimer.Stop();
            
            _newAppWindowSize.Width = e.NewSize.Width;
            _newAppWindowSize.Height = e.NewSize.Height;

            Timers.AppWindowSizeChangedTimer.Start();
        }

        /// <summary>
        /// Handler for the AppWindowSizeChangedTimer Tick event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ResizeTimer_Tick(object sender, EventArgs e)
        {
            Timers.AppWindowSizeChangedTimer.Stop();

            // Perform the resizing operations after a short delay to avoid doing them multiple times during a single resizing action by the user.
            ProcessFinalWindowSize();
            vbMainWindow.Stretch = System.Windows.Media.Stretch.Fill;
        }

        /// <summary>
        /// Performs the resizing operations.
        /// </summary>
        private void ProcessFinalWindowSize()
        {
            UpdateGridElementSizes(_newAppWindowSize);
            RefreshAffectedControls();
        }

        /// <summary>
        /// Sets and stores the default sizes and margins of the main window controls.
        /// </summary>
        public void InitializeLayoutConstants()
        {
            // calculate the default main window width and height based on the grid definitions.
            LayoutUtils.DEFAULT_GRID_WIDTH = LayoutUtils.DEFAULT_COLUMN_WIDTHS.Sum();
            LayoutUtils.DEFAULT_GRID_HEIGHT = LayoutUtils.DEFAULT_ROW_HEIGHTS.Sum();

            LayoutUtils.DEFAULT_GRID_WIDTH_HEIGHT_RATIO = LayoutUtils.DEFAULT_GRID_WIDTH / LayoutUtils.DEFAULT_GRID_HEIGHT;
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

            MainBoard.Width = LayoutUtils.DEFAULT_CHESSBOARD_SIZE;
            MainBoard.Height = LayoutUtils.DEFAULT_CHESSBOARD_SIZE;

            // set the main grid's row and column definitions
            UiMainGrid.RowDefinitions[0].Height = new GridLength(LayoutUtils.DEFAULT_ROW_HEIGHTS[0]);
            UiMainGrid.RowDefinitions[1].Height = new GridLength(LayoutUtils.DEFAULT_ROW_HEIGHTS[1]);
            UiMainGrid.RowDefinitions[2].Height = new GridLength(LayoutUtils.DEFAULT_ROW_HEIGHTS[2]);
            UiMainGrid.RowDefinitions[3].Height = new GridLength(LayoutUtils.DEFAULT_ROW_HEIGHTS[3]);

            UiMainGrid.ColumnDefinitions[0].Width = new GridLength(LayoutUtils.DEFAULT_COLUMN_WIDTHS[0]);
            UiMainGrid.ColumnDefinitions[1].Width = new GridLength(LayoutUtils.DEFAULT_COLUMN_WIDTHS[1]);
            UiMainGrid.ColumnDefinitions[2].Width = new GridLength(LayoutUtils.DEFAULT_COLUMN_WIDTHS[2]);
            UiMainGrid.ColumnDefinitions[3].Width = new GridLength(LayoutUtils.DEFAULT_COLUMN_WIDTHS[3]);

            LayoutUtils.SetDefaultControlPositions();
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
            switch (sizeMode)
            {
                case TabControlSizeMode.SHOW_ACTIVE_LINE:
                    UiDgActiveLine.Visibility = Visibility.Visible;
                    UiLblScoresheet.Visibility = Visibility.Visible;
                    break;
                case TabControlSizeMode.HIDE_ACTIVE_LINE:
                    UiDgActiveLine.Visibility = Visibility.Hidden;
                    UiLblScoresheet.Visibility = Visibility.Hidden;
                    break;
                case TabControlSizeMode.SHOW_ACTIVE_LINE_NO_EVAL:
                    UiDgActiveLine.Visibility = Visibility.Visible;
                    UiLblScoresheet.Visibility = Visibility.Visible;
                    break;
                case TabControlSizeMode.SHOW_ENGINE_GAME_LINE:
                    UiDgActiveLine.Visibility = Visibility.Hidden;
                    UiLblScoresheet.Visibility = Visibility.Visible;
                    UiDgEngineGame.Visibility = Visibility.Visible;
                    break;
                default:
                    UiDgActiveLine.Visibility = Visibility.Visible;
                    UiLblScoresheet.Visibility = Visibility.Hidden;
                    break;
            }

            LayoutUtils.AdjustScoresheetColumnWidth(sizeMode);
        }

        /// <summary>
        /// Updates the widths and heights of the main window controls according to the current size of the main window.
        /// </summary>
        public void UpdateGridElementSizes(Size windowSize)
        {
            try
            {
                // calculate the current width/height ratio of the main window client area.
                // the width is straight forward , but the height is taken as the height of the second row of the main grid,
                // so that we skip the menus.
                double actualWidthHeightRatio = windowSize.Width / _gridUber.RowDefinitions[1].ActualHeight;

                LayoutUtils.CalcExtraGridWidthAndHeight(actualWidthHeightRatio, out double extraWidth, out double extraHeight);

                LayoutState.WidthCorrectionForShape = extraWidth;
                LayoutState.HeightCorrectionForShape = extraHeight;

                LayoutUtils.AdjustColumnWidths();
                LayoutUtils.AdjustRowHeights();

                LayoutUtils.CorrectRowHeights();

                MainBoard.Width = Math.Min(UiMainGrid.ColumnDefinitions[0].Width.Value, UiMainGrid.RowDefinitions[1].Height.Value);
                MainBoard.Height = MainBoard.Width;
            }
            catch { }
        }

        /// <summary>
        /// Refreshes the controls affected by a change in the main chessboard width, which are:
        /// - The board comment RichTextBox
        /// - The opening stats view (if explorers are on)
        /// - The evaluation chart
        /// </summary>
        public void RefreshAffectedControls()
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
        // Redirects for the mouse events of the vertical splitter.
        //
        //******************************************************************************************

        private void ManualSplitterVertical_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LayoutVerticalSplitter.MouseDown(sender, e);
        }

        private void ManualSplitterVertical_MouseMove(object sender, MouseEventArgs e)
        {
            LayoutVerticalSplitter.MouseMove(sender, e);
        }

        private void ManualSplitterVertical_MouseUp(object sender, MouseButtonEventArgs e)
        {
            LayoutVerticalSplitter.MouseUp(sender, e);
        }


        //******************************************************************************************
        //
        // Redirects for the mouse events of the horizontal splitter.
        //
        //******************************************************************************************

        private void ManualSplitterHorizontal_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LayoutHorizontalSplitter.MouseDown(sender, e);
        }

        private void ManualSplitterHorizontal_MouseMove(object sender, MouseEventArgs e)
        {
            LayoutHorizontalSplitter.MouseMove(sender, e);
        }

        private void ManualSplitterHorizontal_MouseUp(object sender, MouseButtonEventArgs e)
        {
            LayoutHorizontalSplitter.MouseUp(sender, e);
        }
    }
}

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Handles the events related to the vertical splitter between the Chessboard and the Tab Control columns.
    /// </summary>
    public class LayoutVerticalSplitter
    {
        // whether resizing is in progress
        private static bool _isResizingTab = false;

        // position of the splitter when resizing started (equals the width of the first column)
        private static double _resizeStartPointX;

        // The adjustment implied by the current position of the mouse cursor.
        // This is the position of the mouse cursor minus the position of the splitter.
        // It needs to be tracked across the mouse events as if the mouse goes too far
        // we want to keep to the last valid adjustment.
        private static double _runningHorizontalAdjustment = 0;

        /// <summary>
        /// A mouse click event occured over the splitter.
        /// The resizing starts here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow win = AppState.MainWin;

            _runningHorizontalAdjustment = 0;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // flag that resizing is in progress
                _isResizingTab = true;
                // set the initial position of the splitter
                _resizeStartPointX = win.UiMainGrid.ColumnDefinitions[0].Width.Value;

                win.ManualSplitterVertical.CaptureMouse();
            }
        }

        /// <summary>
        /// If the mouse is moved while resizing is in progress,
        /// update the position of the splitter and the _runningAdjustment value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void MouseMove(object sender, MouseEventArgs e)
        {
            MainWindow win = AppState.MainWin;

            if (_isResizingTab && e.LeftButton == MouseButtonState.Pressed)
            {
                double currX = e.GetPosition(win.UiMainGrid).X;

                // make sure that the user cannot move the splitter beyond the allowed limits.
                if (currX <= LayoutUtils.MAIN_GRID_COLUMNS[0] - LayoutUtils.MAX_USER_WIDTH_ADJUSTMENT)
                {
                    currX = LayoutUtils.MAIN_GRID_COLUMNS[0] - LayoutUtils.MAX_USER_WIDTH_ADJUSTMENT;
                }
                else if (currX > LayoutUtils.MAIN_GRID_COLUMNS[0])
                {
                    currX = LayoutUtils.MAIN_GRID_COLUMNS[0];
                }

                win.ManualSplitterVertical.Fill = Brushes.Gray;
                win.ManualSplitterVertical.Opacity = 0.8;

                _runningHorizontalAdjustment = currX - _resizeStartPointX;

                win.ManualSplitterVertical.Margin = new Thickness(_runningHorizontalAdjustment, 0, -1 * _runningHorizontalAdjustment, 0);
            }
        }

        /// <summary>
        /// Mouse button released while resizing is in progress.
        /// Tidy up and resize the affected controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void MouseUp(object sender, MouseButtonEventArgs e)
        {
            MainWindow win = AppState.MainWin;

            if (_isResizingTab)
            {
                win.ManualSplitterVertical.Fill = Brushes.Transparent;
                win.ManualSplitterVertical.Opacity = 0;

                _isResizingTab = false;
                win.ManualSplitterVertical.ReleaseMouseCapture();
                win.ManualSplitterVertical.Margin = new Thickness(0, 0, 0, 0);

                LayoutState.ChessboardSizeAdjustment = (int)_runningHorizontalAdjustment + LayoutState.ChessboardSizeAdjustment;

                win.UpdateGridElementSizes(new Size(win.Width, win.Height));
                win.RefreshAffectedControls();
            }
        }

    }
}

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Handles the events related to the horizontal splitter between the Chessboard and the Explorer rows.
    /// </summary>
    public class LayoutHorizontalSplitter
    {
        // whether resizing is in progress
        private static bool _isHorizontalResizing = false;

        // position of the splitter when resizing started (equals the height of the first row)
        private static double _resizeStartPointY;

        // The adjustment implied by the current position of the mouse cursor.
        // This is the position of the mouse cursor minus the position of the splitter.
        // It needs to be tracked across the mouse events as if the mouse goes too far
        // we want to keep to the last valid adjustment.
        private static double _runningVerticalAdjustment = 0;

        /// <summary>
        /// A mouse click event occured over the splitter.
        /// The resizing starts here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow win = AppState.MainWin;

            _runningVerticalAdjustment = 0;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // flag that resizing is in progress
                _isHorizontalResizing = true;
                // set the initial position of the splitter
                _resizeStartPointY = win.UiMainGrid.RowDefinitions[0].Height.Value + win.UiMainGrid.RowDefinitions[1].Height.Value;

                win.ManualSplitterHorizontal.CaptureMouse();
            }
        }

        /// <summary>
        /// If the mouse is moved while resizing is in progress,
        /// update the position of the splitter and the _runningHorizontalAdjustment value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void MouseMove(object sender, MouseEventArgs e)
        {
            MainWindow win = AppState.MainWin;

            if (_isHorizontalResizing && e.LeftButton == MouseButtonState.Pressed)
            {
                double currY = e.GetPosition(win.UiMainGrid).Y;

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

                win.ManualSplitterHorizontal.Fill = Brushes.Gray;
                win.ManualSplitterHorizontal.Opacity = 0.8;

                _runningVerticalAdjustment = _resizeStartPointY - currY;

                win.ManualSplitterHorizontal.Margin = new Thickness(0, -1 * _runningVerticalAdjustment, 0, _runningVerticalAdjustment);
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

            if (_isHorizontalResizing)
            {
                win.ManualSplitterHorizontal.Fill = Brushes.Transparent;
                win.ManualSplitterHorizontal.Opacity = 0;

                _isHorizontalResizing = false;
                win.ManualSplitterHorizontal.ReleaseMouseCapture();
                win.ManualSplitterHorizontal.Margin = new Thickness(0, 0, 0, 0);

                LayoutState.ExplorerRowHeightAdjustment = (int)_runningVerticalAdjustment + LayoutState.ExplorerRowHeightAdjustment;

                win.UpdateGridElementSizes(new Size(win.Width, win.Height));
                win.RefreshAffectedControls();
            }
        }
    }
}

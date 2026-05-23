using System;
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

        // The last position of the mouse cursor during resizing.
        private static double _lastMousePosition;

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
                _resizeStartPointY = LayoutUtils.GetCurrentExplorerRowTop();

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
                _lastMousePosition = e.GetPosition(win.UiMainGrid).Y;

                double minAllowedY = GetMinAllowedMouseY();
                double maxAllowedY = GetMaxAllowedMouseY();

                // make sure that the user cannot move the splitter beyond the allowed limits.
                _lastMousePosition = Math.Max(_lastMousePosition, minAllowedY);
                _lastMousePosition = Math.Min(_lastMousePosition, maxAllowedY);

                win.ManualSplitterHorizontal.Fill = Brushes.Gray;
                win.ManualSplitterHorizontal.Opacity = 0.8;

                _runningVerticalAdjustment = LayoutUtils.GetCurrentExplorerRowTop() - _lastMousePosition;

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

                double currentHeightAdjustment = LayoutUtils.GetExplorerRowCurrentHeight() - LayoutUtils.DEFAULT_ROW_HEIGHTS[2];
                double currentMouseDelta = LayoutUtils.GetCurrentExplorerRowTop() - _lastMousePosition;
                LayoutState.ExplorerRowHeightUserAdjustment = Math.Max(currentMouseDelta + currentHeightAdjustment - LayoutState.HeightCorrectionForShape, 0);

                win.UpdateGridElementSizes(new Size(win.ActualWidth, win.ActualHeight));
                win.RefreshAffectedControls();
            }
        }

        /// <summary>
        /// The maximum allowed height of the Explorer row is the default height 
        /// plus the maximum adjustment plus the correction for the shape.
        /// </summary>
        /// <returns></returns>
        private static double GetMaxAllowedCurrentExplorerRowHeight()
        {
            return LayoutUtils.DEFAULT_ROW_HEIGHTS[2] + LayoutUtils.MAX_EXPLORER_ROW_HEIGHT_ADJUSTMENT + LayoutState.HeightCorrectionForShape;
        }

        /// <summary>
        /// The minimum allowed height of the Explorer row is the default height.
        /// </summary>
        /// <returns></returns>
        private static double GetMinAllowedCurrentExplorerRowHeight()
        {
            return LayoutUtils.DEFAULT_ROW_HEIGHTS[2] + LayoutState.HeightCorrectionForShape;
        }

        /// <summary>
        /// The minimum allowed position of the mouse cursor is the bottom of the Explorer row minus the maximum allowed height of the Explorer row.
        /// </summary>
        /// <returns></returns>
        private static double GetMinAllowedMouseY()
        {
            return LayoutUtils.GetCurrentExplorerRowBottom() - GetMaxAllowedCurrentExplorerRowHeight();
        }

        /// <summary>
        /// The maximum allowed position of the mouse cursor is the bottom of the Explorer row minus the minimum allowed height of the Explorer row.
        /// </summary>
        /// <returns></returns>
        private static double GetMaxAllowedMouseY()
        {
            return LayoutUtils.GetCurrentExplorerRowBottom() - GetMinAllowedCurrentExplorerRowHeight();
        }

    }
}

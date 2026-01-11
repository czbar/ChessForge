using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ChessForge
{
    public partial class VariationTreeView
    {
        // Cache the ScrollViewer reference for efficiency
        private ScrollViewer _scrollViewer = null;

        // Keep track of the last known vertical offset
        private double _lastKnownVerticalOffset = 0.0;

        /// <summary>
        /// Saves the current scroll position of the RichTextBox.
        /// </summary>
        public void SaveScrollPosition()
        {
            if (_scrollViewer == null)
            {
                _scrollViewer = GuiUtilities.GetVisualChild<ScrollViewer>(HostRtb);
            }
            if (_scrollViewer != null)
            {
                _lastKnownVerticalOffset = _scrollViewer.VerticalOffset;
            }
        }

        /// <summary>
        /// Restores the saved scroll position of the RichTextBox.
        /// </summary>
        public void RestoreScrollPosition()
        {
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollToVerticalOffset(_lastKnownVerticalOffset);
            }
        }

        /// <summary>
        /// Brings the specified Run into view within the RichTextBox.
        /// It is better than the built-in BringIntoView because it avoids
        /// jumping fron the bottom to the top when the run is being brought into view.
        /// </summary>
        /// <param name="run"></param>
        private void BringRunIntoView(Run run)
        {
            if (_scrollViewer == null)
            {
                _scrollViewer = GuiUtilities.GetVisualChild<ScrollViewer>(HostRtb);
            }

            // Get the rectangle of the Run relative to the RichTextBox
            Rect runRect = run.ContentStart.GetCharacterRect(LogicalDirection.Forward);

            double runTopYInViewport = runRect.Top;      
            double runBottomYInViewport = runRect.Bottom;

            double viewportHeight = _scrollViewer.ViewportHeight;      // visible height (DIP)
            double currentOffset = _scrollViewer.VerticalOffset;       // current vertical scroll offset (DIP)

            // Estimate line height (based on font size)
            double lineHeight = HostRtb.FontSize * 1.3; // typical line height multiplier

            // how many lines from top or bottom to start scrolling
            const double linesThreshold = 2.0;

            // If the run's bottom is within 2 lines of the bottom of the viewport, scroll up.
            double distanceFromBottom = viewportHeight - runBottomYInViewport;

            if (distanceFromBottom < linesThreshold * lineHeight)
            {
                // Move the run to the middle of the viewport
                double desiredRunTopInViewport = viewportHeight / 2.0 - (lineHeight / 2.0);
                double delta = runTopYInViewport - desiredRunTopInViewport;

                double newOffset = currentOffset + delta;
                if (newOffset < 0) newOffset = 0;

                // clamp to scrollable range:
                if (newOffset > _scrollViewer.ExtentHeight - _scrollViewer.ViewportHeight)
                    newOffset = _scrollViewer.ExtentHeight - _scrollViewer.ViewportHeight;

                _scrollViewer.ScrollToVerticalOffset(newOffset);
            }
            else
            {
                // If the run's top is within 2 lines of the top of the viewport, scroll down.
                double distanceFromTop = runTopYInViewport;
                if (distanceFromTop < linesThreshold * lineHeight)
                {
                    // Move the run to the middle of the viewport
                    double desiredRunTopInViewport = viewportHeight / 2.0 - (lineHeight / 2.0);
                    double delta = runTopYInViewport - desiredRunTopInViewport;

                    double newOffset = currentOffset + delta;
                    if (newOffset < 0) newOffset = 0;

                    // clamp to scrollable range:
                    if (newOffset > _scrollViewer.ExtentHeight - _scrollViewer.ViewportHeight)
                        newOffset = _scrollViewer.ExtentHeight - _scrollViewer.ViewportHeight;

                    _scrollViewer.ScrollToVerticalOffset(newOffset);
                }
            }
        }
    }
}

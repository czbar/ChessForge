using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Holds the placement information for a GUI control, including margins and dimensions.
    /// </summary>
    public class GuiControlPlacement
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public GuiControlPlacement() { }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="placement"></param>
        public GuiControlPlacement(GuiControlPlacement placement) 
        {
            LeftMargin = placement.LeftMargin;
            TopMargin = placement.TopMargin;
            RightMargin = placement.RightMargin;
            BottomMargin = placement.BottomMargin;

            Width = placement.Width;
            Height = placement.Height;
        }

        /// <summary>
        /// Constructor with parameters for margins and dimensions.
        /// </summary>
        /// <param name="leftMargin"></param>
        /// <param name="topMargin"></param>
        /// <param name="rightMargin"></param>
        /// <param name="bottomMargin"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public GuiControlPlacement(double leftMargin, double topMargin, double rightMargin, double bottomMargin, double width = -1, double height = -1)
        {
            LeftMargin = leftMargin;
            TopMargin = topMargin;
            RightMargin = rightMargin;
            BottomMargin = bottomMargin;

            Width = width;
            Height = height;
        }

        /// <summary>
        /// Creates a Thickness object from the margins defined in this placement.
        /// </summary>
        /// <returns></returns>
        public Thickness ToThickness()
        {
            return new Thickness(LeftMargin, TopMargin, RightMargin, BottomMargin);
        }

        /// <summary>
        /// Left margin of the control.
        /// </summary>
        public double LeftMargin { get; set; }

        /// <summary>
        /// Top margin of the control.
        /// </summary>
        public double TopMargin { get; set; }

        /// <summary>
        /// Right margin of the control.
        /// </summary>
        public double RightMargin { get; set; }

        /// <summary>
        /// Bottom margin of the control.
        /// </summary>
        public double BottomMargin { get; set; }

        /// <summary>
        /// Width of the control.
        /// Can be set to -1 to indicate that the width is not specified.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Height of the control.
        ///  Can be set to -1 to indicate that the height is not specified.
        /// </summary>
        public double Height { get; set; }
    }
}

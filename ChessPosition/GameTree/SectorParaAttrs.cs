using System.Windows;
using System.Windows.Media;

namespace GameTree
{
    /// <summary>
    /// Attributes of a paragraph represented by the Sector.
    /// </summary>
    public class SectorParaAttrs
    {
        /// <summary>
        /// Extra margin to add to the top/bottom for the first/last paragraph at a given level
        /// </summary>
        public static int EXTRA_MARGIN = 10;

        /// <summary>
        /// Display level of the paragraph.
        /// </summary>
        public int DisplayLevel { get; set; }

        /// <summary>
        /// Level group of the paragraph.
        /// </summary>
        public int LevelGroup { get; set; }

        /// <summary>
        /// Font size of the paragraph.
        /// </summary>
        public double FontSize { get; set; }

        /// <summary>
        /// Font weight of the paragraph.
        /// </summary>
        public FontWeight FontWeight { get; set; }

        /// <summary>
        /// Text alignment of the paragraph.
        /// </summary>
        public TextAlignment TextAlignment { get; set; }

        /// <summary>
        /// Foreground color of the paragraph.
        /// </summary>
        public Brush Foreground { get; set; }

        /// <summary>
        /// Background color of the paragraph.
        /// </summary>
        public string Background { get; set; }

        /// <summary>
        /// Color to use for the first node in the sector, if any
        /// </summary>
        public Brush FirstNodeColor { get; set; }

        /// <summary>
        /// Color to use for the last node in the sector, if any
        /// </summary>
        public Brush LastNodeColor { get; set; }

        /// <summary>
        /// Margin of the paragraph.
        /// </summary>
        public Thickness Margin { get; set; }

        /// <summary>
        /// Extra margin to add to the top for the first/last paragraph at a given level
        /// </summary>
        public int TopMarginExtra { get; set; }

        /// <summary>
        /// Extra margin to add to the bottom for the first/last paragraph at a given level
        /// </summary>
        public int BottomMarginExtra { get; set; }

        /// <summary>
        /// Creates a new instance of SectorParaAttrs.
        /// </summary>
        public SectorParaAttrs()
        {
        }

        /// <summary>
        /// Returns the font size for a given display level
        /// (if using variable font size).
        /// </summary>
        /// <param name="displayLevel"></param>
        /// <returns></returns>
        public static int GetVariableFontSizeForLevel(int displayLevel)
        {
            int fontSize;

            if (displayLevel <= 1)
            {
                fontSize = 16;
            }
            else if (displayLevel == 2)
            {
                fontSize = 14;
            }
            else if (displayLevel <= 4)
            {
                fontSize = 14;
            }
            else
            {
                fontSize = 11;
            }

            return fontSize;
        }

    }
}

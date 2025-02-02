using System.Windows;
using System.Windows.Media;

namespace ChessForge.TreeViewManagement
{
    /// <summary>
    /// Attributes of a paragraph represented by the Sector.
    /// </summary>
    public class SectorParaAttrs
    {
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
        
        public string Background { get; set; }

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
    }
}

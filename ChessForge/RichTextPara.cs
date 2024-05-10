using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Settings that are specific to paragraphs shown in 
    /// RichTextBoxes
    /// </summary>
    public class RichTextPara
    {
        /// <summary>
        /// Constructs a set of Paragraph attributes.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="bottom"></param>
        /// <param name="font_size"></param>
        /// <param name="font_weight"></param>
        /// <param name="align"></param>
        /// <param name="foregroundColor"></param>
        public RichTextPara(int left, int bottom, int font_size, FontWeight font_weight,
                            TextAlignment align, SolidColorBrush foregroundColor = null)
        {
            LeftIndent = left;
            BottomMargin = bottom;
            FontSize = font_size;
            FontWeight = font_weight;
            TextAlign = align;
            ForegroundColor = foregroundColor;
        }

        /// <summary>
        /// Copy constructor. 
        /// </summary>
        /// <param name="rtp"></param>
        public RichTextPara(RichTextPara rtp)
        {
            LeftIndent = rtp.LeftIndent;
            BottomMargin = rtp.BottomMargin;
            FontSize = rtp.FontSize;
            FontWeight = rtp.FontWeight;
            TextAlign = rtp.TextAlign;
            ForegroundColor = rtp.ForegroundColor;
        }

        /// <summary>
        /// Clones this object
        /// </summary>
        /// <returns></returns>
        public RichTextPara CloneMe()
        {
            return this.MemberwiseClone() as RichTextPara;
        }

        /// <summary>
        /// Left indent of the paragraph.
        /// </summary>
        public int LeftIndent { get; set; }

        /// <summary>
        /// Bottom margin of the paragraph.
        /// </summary>
        public int BottomMargin { get; set; }

        /// <summary>
        /// Default font size to use.
        /// </summary>
        public int FontSize { get; set; }

        /// <summary>
        /// Default font weight to use.
        /// </summary>
        public FontWeight FontWeight { get; set; }

        /// <summary>
        /// Text alignment in the paragraph.
        /// </summary>
        public TextAlignment TextAlign { get; set; }

        /// <summary>
        /// The font color to use.
        /// </summary>
        public SolidColorBrush ForegroundColor { get; set; }
    }
}

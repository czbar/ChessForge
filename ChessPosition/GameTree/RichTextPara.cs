using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace GameTree
{
    /// <summary>
    /// Settings that are specific to paragraphs shown in 
    /// RichTextBoxes
    /// </summary>
    public class RichTextPara
    {
        public RichTextPara(int left, int bottom, int font_size, FontWeight font_weight, SolidColorBrush color)
        {
            LeftIndent = left;
            BottomMargin = bottom;
            FontSize = font_size;
            FontWeight = font_weight;
            FirstCharColor = color;
        }

        public RichTextPara(RichTextPara rtp)
        {
            LeftIndent = rtp.LeftIndent;
            BottomMargin = rtp.BottomMargin;
            FontSize = rtp.FontSize;
            FontWeight = rtp.FontWeight;
            FirstCharColor = rtp.FirstCharColor;
        }

        public int LeftIndent { get; set; }

        public int BottomMargin { get; set; }

        public int FontSize { get; set; }

        public FontWeight FontWeight    {get; set; }    

        public SolidColorBrush FirstCharColor { get; set; }
    }
}

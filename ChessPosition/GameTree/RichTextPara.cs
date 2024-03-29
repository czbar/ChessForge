﻿using System;
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
        public RichTextPara(int left, int bottom, int font_size, FontWeight font_weight, 
                            SolidColorBrush firstCharColor, TextAlignment align, SolidColorBrush foregroundColor = null)
        {
            LeftIndent = left;
            BottomMargin = bottom;
            FontSize = font_size;
            FontWeight = font_weight;
            FirstCharColor = firstCharColor;
            TextAlign = align;
            ForegroundColor = foregroundColor == null ? Brushes.Black : foregroundColor;
        }

        public RichTextPara(RichTextPara rtp)
        {
            LeftIndent = rtp.LeftIndent;
            BottomMargin = rtp.BottomMargin;
            FontSize = rtp.FontSize;
            FontWeight = rtp.FontWeight;
            FirstCharColor = rtp.FirstCharColor;
            TextAlign=rtp.TextAlign;
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

        public int LeftIndent { get; set; }

        public int BottomMargin { get; set; }

        public int FontSize { get; set; }

        public FontWeight FontWeight    {get; set; }    

        public SolidColorBrush FirstCharColor { get; set; }

        public TextAlignment TextAlign { get; set; }

        public SolidColorBrush ForegroundColor { get; set; }
    }
}

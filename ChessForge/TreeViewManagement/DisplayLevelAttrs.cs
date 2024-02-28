using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Manages various attributes of the Study layout
    /// per Display Level
    /// </summary>
    public class DisplayLevelAttrs
    {
        // additional margin to add to the top/bottom for the first/last paragraph at a given level
        private static int EXTRA_MARGIN = 10;

        /// <summary>
        /// Color for the last node at the given level. 
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static Brush GetBrushForLastMove(int level)
        {
            Brush brush;

            int modLevel = level % 4;

            switch (modLevel)
            {
                case 0:
                    brush = Brushes.Blue;
                    break;
                case 1:
                    brush = Brushes.Green;
                    break;
                case 2:
                    brush = Brushes.Purple;
                    break;
                case 3:
                    brush = Brushes.Chocolate;
                    break;
                default:
                    brush = Brushes.Black;
                    break;
            }

            return brush;
        }

        /// <summary>
        /// Increases the top or bottom margin of a paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="topBottom"></param>
        public static void AdjustParagraphMargin(Paragraph para, bool topBottom)
        {
            Thickness margin = para.Margin;
            if (topBottom)
            {
                para.Margin = new Thickness(margin.Left, margin.Top + EXTRA_MARGIN, margin.Right, margin.Bottom);
            }
            else
            {
                para.Margin = new Thickness(margin.Left, margin.Top, margin.Right, margin.Bottom + EXTRA_MARGIN);
            }
        }
    }
}

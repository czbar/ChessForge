using ChessPosition;
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
        /// <summary>
        /// Extra margin to add to the top/bottom for the first/last paragraph at a given level
        /// </summary>
        public static int EXTRA_MARGIN = 10;

        /// <summary>
        /// Creates a paragraph for the main Study View area.
        /// </summary>
        /// <param name="displayLevel"></param>
        /// <param name="topMarginExtra"></param>
        /// <param name="bottomMarginExtra"></param>
        /// <returns></returns>
        public static Paragraph CreateStudyParagraph(int displayLevel, int topMarginExtra = 0, int bottomMarginExtra = 0)
        {
            // figure out the font size.
            int fontSize = Constants.BASE_FIXED_FONT_SIZE;
            if (!Configuration.UseFixedFont)
            {
                fontSize = GetVariableFontSizeForLevel(displayLevel);
            }
            fontSize = GuiUtilities.AdjustFontSize(fontSize);

            // create the paragraph.
            Paragraph para = new Paragraph
            {
                Margin = new Thickness(20 * displayLevel, topMarginExtra, 0, 5 + bottomMarginExtra),
                FontSize = fontSize,
                FontWeight = displayLevel == 0 ? FontWeights.DemiBold : FontWeights.Normal,
                TextAlignment = TextAlignment.Left,
                Foreground = Brushes.Black
            };

            return para;
        }

        /// <summary>
        /// Color for the last node at the given level. 
        /// </summary>
        /// <param name="level"></param>
        /// <param name="levelGroup"></param>
        /// <returns></returns>
        public static Brush GetBrushForLastMove(int level, int levelGroup)
        {
            Brush brush;

            int modLevel = (level + levelGroup) % 4;

            switch (modLevel)
            {
                case 0:
                    brush = Brushes.Blue;
                    break;
                case 1:
                    brush = Brushes.DarkGreen;
                    break;
                case 2:
                    brush = Brushes.Purple;
                    break;
                case 3:
                    brush = Brushes.Firebrick;
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

        /// <summary>
        /// Returns the font size for a given display level
        /// (if using variable font size).
        /// </summary>
        /// <param name="displayLevel"></param>
        /// <returns></returns>
        private static int GetVariableFontSizeForLevel(int displayLevel)
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

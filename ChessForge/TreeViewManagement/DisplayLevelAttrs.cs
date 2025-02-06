using ChessPosition;
using GameTree;
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
                Foreground = ChessForgeColors.CurrentTheme.RtbForeground
            };

            return para;
        }

        /// <summary>
        /// Creates a SectorParagraphAttributes object for a Sector.
        /// </summary>
        /// <param name="displayLevel"></param>
        /// <param name="topMarginExtra"></param>
        /// <param name="bottomMarginExtra"></param>
        /// <returns></returns>
        public static SectorParaAttrs CreateParagraphAttrs(int displayLevel, int topMarginExtra = 0, int bottomMarginExtra = 0)
        {
            int fontSize = Constants.BASE_FIXED_FONT_SIZE;
            if (!Configuration.UseFixedFont)
            {
                fontSize = GetVariableFontSizeForLevel(displayLevel);
            }
            fontSize = GuiUtilities.AdjustFontSize(fontSize);

            SectorParaAttrs attrs = new SectorParaAttrs
            {
                FontSize = fontSize,
                FontWeight = displayLevel == 0 ? FontWeights.DemiBold : FontWeights.Normal,
                TextAlignment = TextAlignment.Left,
                Foreground = ChessForgeColors.CurrentTheme.RtbForeground,
                Margin = new Thickness(20 * displayLevel, topMarginExtra, 0, 5 + bottomMarginExtra),
                TopMarginExtra = topMarginExtra,
                BottomMarginExtra = bottomMarginExtra
            };

            return attrs;
        }

        /// <summary>
        /// Resets the last move color selection.
        /// </summary>
        public static void ResetLastMoveBrush()
        {
            _lastMoveBrushIndex = -1;
            _lastLevelCombo = 0;
        }

        // last move color selection index
        private static int _lastMoveBrushIndex = -1;

        // last level + levelGroup combination for which color was requested
        private static int _lastLevelCombo = 0;

        /// <summary>
        /// Color for the last node at the given level. 
        /// </summary>
        /// <param name="level"></param>
        /// <param name="levelGroup"></param>
        /// <returns></returns>
        public static Brush GetBrushForLastMove(int level, int levelGroup)
        {
            Brush brush;

            // increment the index if this is not the last combination.
            if (level + levelGroup != _lastLevelCombo)
            {
                _lastLevelCombo = level + levelGroup;
                _lastMoveBrushIndex++;
            }
            else
            {
                if (_lastMoveBrushIndex < 0)
                {
                    _lastMoveBrushIndex = 0;
                }
            }

            int modLevel = _lastMoveBrushIndex % 4;

            switch (modLevel)
            {
                case 0:
                    brush = ChessForgeColors.CurrentTheme.ModuloColor_0;
                    break;
                case 1:
                    brush = ChessForgeColors.CurrentTheme.ModuloColor_1;
                    break;
                case 2:
                    brush = ChessForgeColors.CurrentTheme.ModuloColor_2;
                    break;
                case 3:
                    brush = ChessForgeColors.CurrentTheme.ModuloColor_3;
                    break;
                default:
                    brush = ChessForgeColors.CurrentTheme.RtbForeground;
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

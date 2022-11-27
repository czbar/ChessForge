using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Custom Brushes
    /// </summary>
    public class ChessForgeColors
    {
        /// <summary>
        /// Creates required objects.
        /// </summary>
        public static void Initialize()
        {
            WhiteWinLinearBrush = CreateGradientBrushForResult(EXPLORER_PCT_WHITE_GRAD.Color, EXPLORER_PCT_WHITE.Color);
            DrawLinearBrush = CreateGradientBrushForResult(EXPLORER_PCT_DRAW_GRAD.Color, EXPLORER_PCT_DRAW.Color);
            BlackWinLinearBrush = CreateGradientBrushForResult(EXPLORER_PCT_BLACK_GRAD.Color, EXPLORER_PCT_BLACK.Color);

            ExitButtonLinearBrush = CreateGradientBrushForResult(EXIT_BUTTON_GREEN.Color, EXIT_BUTTON_BLUE.Color);
        }

        public static SolidColorBrush WORKBOOK_TABLE_HILITE_FORE = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        public static SolidColorBrush WORKBOOK_TABLE_REGULAR_FORE = new SolidColorBrush(Color.FromRgb(0, 0, 0));

        public static SolidColorBrush TABLE_ROW_LIGHT_GRAY = new SolidColorBrush(Color.FromRgb(247, 246, 245));
        public static SolidColorBrush TABLE_HEADER_GREEN = new SolidColorBrush(Color.FromRgb(192, 214, 167));

        public static SolidColorBrush EXPLORER_PCT_BORDER = new SolidColorBrush(Color.FromRgb(225, 225, 225));
        public static SolidColorBrush EXPLORER_PCT_WHITE = new SolidColorBrush(Color.FromRgb(238, 238, 238));
        public static SolidColorBrush EXPLORER_PCT_WHITE_GRAD = new SolidColorBrush(Color.FromRgb(218, 218, 218));
        public static SolidColorBrush EXPLORER_PCT_DRAW = new SolidColorBrush(Color.FromRgb(164, 164, 164));
        public static SolidColorBrush EXPLORER_PCT_DRAW_GRAD = new SolidColorBrush(Color.FromRgb(134, 134, 134));
        public static SolidColorBrush EXPLORER_PCT_BLACK = new SolidColorBrush(Color.FromRgb(95, 95, 95));
        public static SolidColorBrush EXPLORER_PCT_BLACK_GRAD = new SolidColorBrush(Color.FromRgb(65, 65, 65));

        public static SolidColorBrush RTB_GRAY_FOREGROUND = new SolidColorBrush(Color.FromRgb(0x8f, 0x8f, 0x8f));

        public static SolidColorBrush EXIT_BUTTON_GREEN = new SolidColorBrush(Color.FromRgb(100, 248, 188));
        public static SolidColorBrush EXIT_BUTTON_BLUE = new SolidColorBrush(Color.FromRgb(100, 130, 226));

        public static LinearGradientBrush WhiteWinLinearBrush;
        public static LinearGradientBrush DrawLinearBrush;
        public static LinearGradientBrush BlackWinLinearBrush;
        public static LinearGradientBrush ExitButtonLinearBrush;

        /// <summary>
        /// Creates gradient brushes for the result and percentage labels in the Explorers.
        /// </summary>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <returns></returns>
        private static LinearGradientBrush CreateGradientBrushForResult(Color color1, Color color2)
        {
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(1, 1);
            brush.GradientStops.Add(new GradientStop(color1, 0.0));
            brush.GradientStops.Add(new GradientStop(color2, 0.8));

            return brush;
        }
    }
}

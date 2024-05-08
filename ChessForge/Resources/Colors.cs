using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        public static void Initialize(string theme)
        {
            InitLightModeSet();
            InitDarkModeSet();

            if (theme.ToUpper() == "DARK")
            {
                CurrentTheme = DarkMode;
            }
            else
            {
                CurrentTheme = LightMode;
            }

            WhiteWinLinearBrush = CreateGradientBrushForResult(EXPLORER_PCT_WHITE_GRAD.Color, EXPLORER_PCT_WHITE.Color);
            DrawLinearBrush = CreateGradientBrushForResult(EXPLORER_PCT_DRAW_GRAD.Color, EXPLORER_PCT_DRAW.Color);
            BlackWinLinearBrush = CreateGradientBrushForResult(EXPLORER_PCT_BLACK_GRAD.Color, EXPLORER_PCT_BLACK.Color);

            ExitButtonLinearBrush = CreateGradientBrushForResult(EXIT_BUTTON_GREEN.Color, EXIT_BUTTON_BLUE.Color);
            ShowExplorerLinearBrush = CreateGradientBrushForResult(TABLE_HEADER_GREEN.Color, TABLE_HIGHLIGHT_GREEN.Color);
        }

        /// <summary>
        /// Sets background and foreground colors on the main controls.
        /// </summary>
        public static void SetMainControlColors()
        {
            MainWindow win = AppState.MainWin;

            Brush rtbFg = CurrentTheme.RtbForeground;
            Brush rtbBg = CurrentTheme.RtbBackground;

            SetRichTextBoxColors(win.UiRtbChaptersView, rtbFg, rtbBg);
            SetRichTextBoxColors(win.UiRtbIntroView, rtbFg, rtbBg);
            SetRichTextBoxColors(win.UiRtbStudyTreeView, rtbFg, rtbBg);
            SetRichTextBoxColors(win.UiRtbModelGamesView, rtbFg, rtbBg);
            SetRichTextBoxColors(win.UiRtbExercisesView, rtbFg, rtbBg);
            SetRichTextBoxColors(win.UiRtbTrainingProgress, rtbFg, rtbBg);
            SetRichTextBoxColors(win.UiRtbBoardComment, rtbFg, rtbBg);

            win.Background = CurrentTheme.RtbBackground;
            win.UiTabCtrlManualReview.Background = CurrentTheme.RtbBackground;
            win.UiTabCtrlTraining.Background = CurrentTheme.RtbBackground;
            win.UiTabCtrlEngineGame.Background = CurrentTheme.RtbBackground;

            win.UiRtbBoardComment.BorderBrush = CurrentTheme.BorderBrush;
            win.UiTbEngineLines.BorderBrush = CurrentTheme.BorderBrush;
            win.UiEvalChart.BorderBrush = CurrentTheme.BorderBrush;

            win.UiTbEngineLines.Foreground = CurrentTheme.RtbForeground;
            win.UiTbEngineLines.Background = CurrentTheme.EngineLinesBackground;

            win.UiLblScoresheet.Foreground = CurrentTheme.RtbForeground;

            if (CurrentTheme.DarkShadeOpacity == 0)
            {
                win.UiCnvDarkShade.Visibility = Visibility.Collapsed;
            }
            else
            {
                win.UiCnvDarkShade.Visibility = Visibility.Visible;
                win.UiCnvDarkShade.Opacity = CurrentTheme.DarkShadeOpacity;
            }
        }

        /// <summary>
        /// Sets the foreground and background color on the RichTextBox. 
        /// </summary>
        /// <param name="rtb"></param>
        /// <param name="foreground"></param>
        /// <param name="background"></param>
        private static void SetRichTextBoxColors(RichTextBox rtb, Brush foreground, Brush background)
        {
            try
            {
                rtb.Foreground = foreground;
                rtb.Background = background;
            }
            catch { }
        }


        public static ThemeColorSet CurrentTheme;

        public static ThemeColorSet LightMode = new ThemeColorSet();
        public static ThemeColorSet DarkMode = new ThemeColorSet();

        public static SolidColorBrush WORKBOOK_TABLE_HILITE_FORE = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        public static SolidColorBrush WORKBOOK_TABLE_REGULAR_FORE = new SolidColorBrush(Color.FromRgb(0, 0, 0));

        public static SolidColorBrush TABLE_ROW_LIGHT_GRAY = new SolidColorBrush(Color.FromRgb(247, 246, 245));
        public static SolidColorBrush TABLE_HEADER_GREEN = new SolidColorBrush(Color.FromRgb(192, 214, 167));
        public static SolidColorBrush TABLE_HIGHLIGHT_GREEN = new SolidColorBrush(Color.FromRgb(224, 235, 211));

        public static SolidColorBrush INTRO_MOVE_BACKGROUND = new SolidColorBrush(Color.FromRgb(225, 225, 225));

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
        public static LinearGradientBrush ShowExplorerLinearBrush;

        /// <summary>
        /// Returns color to use for a given hint type
        /// in the comment box.
        /// </summary>
        /// <param name="typ"></param>
        /// <returns></returns>
        public static SolidColorBrush GetHintForeground(CommentBox.HintType typ)
        {
            switch (typ)
            {
                case CommentBox.HintType.ERROR:
                    return CurrentTheme.HintErrorForeground;
                case CommentBox.HintType.INFO:
                    return CurrentTheme.HintInfoForeground;
                case CommentBox.HintType.PROGRESS:
                    return CurrentTheme.HintProgressForeground;
                default:
                    return CurrentTheme.RtbForeground;
            }
        }

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

        /// <summary>
        /// Sets colors for the Light Mode.
        /// </summary>
        private static void InitLightModeSet()
        {
            LightMode.RtbForeground = Brushes.Black;
            LightMode.RtbBackground = Brushes.White;

            LightMode.RtbSelectRunForeground = Brushes.White;
            LightMode.RtbSelectRunBackground = Brushes.Black;

            LightMode.RtbSelectLineForeground = Brushes.Black;
            LightMode.RtbSelectLineBackground = new SolidColorBrush(Color.FromRgb(255, 255, 206));

            LightMode.IntroMoveForeground = Brushes.Blue;
            LightMode.IntroDiagBackground = Brushes.Black;
            LightMode.IntroDiagSideCanvasBackground = Brushes.White;

            LightMode.IndexPrefixForeground = new SolidColorBrush(Color.FromRgb(18, 55, 97));

            LightMode.ModuloColor_0 = Brushes.Blue;
            LightMode.ModuloColor_1 = Brushes.Green;
            LightMode.ModuloColor_2 = Brushes.Magenta;
            LightMode.ModuloColor_3 = Brushes.Firebrick;

            LightMode.BorderBrush = Brushes.Black;

            LightMode.EngineLinesBackground = new SolidColorBrush(Color.FromRgb(0xF2, 0xF5, 0xF3));

            LightMode.DarkShadeOpacity = 0;

            LightMode.HintErrorForeground = Brushes.Red;
            LightMode.HintInfoForeground = Brushes.Green;
            LightMode.HintProgressForeground = Brushes.Blue;

            LightMode.RtbSelectMoveWhileCopyForeground = Brushes.White;
            LightMode.RtbSelectMoveWhileCopyBackground = Brushes.Blue;
            LightMode.RtbSelectMovesForCopyBackground = Brushes.LightBlue;
    }

    /// <summary>
    /// Sets colors for the Dark Mode.
    /// </summary>
    private static void InitDarkModeSet()
        {
            DarkMode.RtbForeground = Brushes.White;
            DarkMode.RtbBackground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));

            DarkMode.RtbSelectRunForeground = Brushes.Black;
            DarkMode.RtbSelectRunBackground = Brushes.White;

            DarkMode.RtbSelectLineForeground = Brushes.White;
            DarkMode.RtbSelectLineBackground = new SolidColorBrush(Color.FromRgb(100, 100, 0));

            DarkMode.IntroMoveForeground = Brushes.LightBlue;
            DarkMode.IntroDiagBackground = Brushes.Black;
            DarkMode.IntroDiagSideCanvasBackground = DarkMode.RtbBackground;

            DarkMode.IndexPrefixForeground = Brushes.LightBlue;

            DarkMode.ModuloColor_0 = Brushes.LightCyan;
            DarkMode.ModuloColor_1 = Brushes.LightGreen;
            DarkMode.ModuloColor_2 = Brushes.LightPink;
            DarkMode.ModuloColor_3 = Brushes.Yellow;

            DarkMode.BorderBrush = Brushes.White;

            DarkMode.EngineLinesBackground = DarkMode.RtbBackground;

            DarkMode.DarkShadeOpacity = 0.1;

            DarkMode.HintErrorForeground = Brushes.OrangeRed;
            DarkMode.HintInfoForeground = Brushes.LightGreen;
            DarkMode.HintProgressForeground = Brushes.LightBlue;

            DarkMode.RtbSelectMoveWhileCopyForeground = Brushes.Black;
            DarkMode.RtbSelectMoveWhileCopyBackground = Brushes.LightBlue;
            DarkMode.RtbSelectMovesForCopyBackground = Brushes.Blue;
        }
    }
}

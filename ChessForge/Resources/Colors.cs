using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        public static void Initialize(ColorThemes theme)
        {
            InitLightModeSet();
            InitDarkModeSet();

            if (theme == ColorThemes.DARK_MODE)
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

            if (CurrentTheme == DarkMode)
            {
                win.UiImgChapterArrowUp.Source = ImageSources.ChaptersUpArrowDarkMode;
                win.UiImgChapterArrowDown.Source = ImageSources.ChaptersDnArrowDarkMode;
            }
            else
            {
                win.UiImgChapterArrowUp.Source = ImageSources.ChaptersUpArrow;
                win.UiImgChapterArrowDown.Source = ImageSources.ChaptersDnArrow;
            }

            Brush rtbFg = CurrentTheme.RtbForeground;
            Brush rtbBg = CurrentTheme.RtbBackground;

            SetRichTextBoxColors(win.UiRtbChaptersView, rtbFg, rtbBg);
            SetRichTextBoxColors(win.UiRtbIntroView, rtbFg, rtbBg);
            SetRichTextBoxColors(win.UiRtbStudyTreeView, rtbFg, rtbBg);
            SetRichTextBoxColors(win.UiRtbModelGamesView, rtbFg, rtbBg);
            SetRichTextBoxColors(win.UiRtbExercisesView, rtbFg, rtbBg);
            SetRichTextBoxColors(win.UiRtbTrainingProgress, rtbFg, rtbBg);
            SetRichTextBoxColors(win.UiRtbBoardComment, rtbFg, rtbBg);

            win.UiTrainingSessionBox.ApplyColorTheme(CurrentTheme);

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

            SetupDataGrid(win.UiDgActiveLine);
            SetupDataGrid(win.UiDgEngineGame);

            win.UiRtbTopGames.Background = CurrentTheme.RtbBackground;

            win.UiRtbOpenings.Background = CurrentTheme.RtbBackground;

            win.UiGridBookmarks.Background = CurrentTheme.BookmarksBackground;
            win.UiLblBmChapters.Foreground = CurrentTheme.RtbForeground;
            win.UiLblBmCContent.Foreground = CurrentTheme.RtbForeground;
            win.UiLblBookmarkPage.Foreground = CurrentTheme.RtbForeground;
            BookmarkManager.HighlightBookmark(null);
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

        /// <summary>
        /// Sets up colors for a DataGrid
        /// </summary>
        /// <param name="dg"></param>
        private static void SetupDataGrid(DataGrid dg)
        {
            dg.Foreground = CurrentTheme.RtbForeground;
            dg.Background = CurrentTheme.RtbBackground;
            dg.RowBackground = CurrentTheme.RtbBackground;

            dg.ColumnHeaderStyle = CurrentTheme.DataGridHeaderStyle;
        }

        public static ThemeColorSet CurrentTheme;

        public static ThemeColorSet LightMode = new ThemeColorSet();
        public static ThemeColorSet DarkMode = new ThemeColorSet();

        public static SolidColorBrush TABLE_ROW_LIGHT_GRAY = new SolidColorBrush(Color.FromRgb(247, 246, 245));
        public static SolidColorBrush TABLE_HEADER_GREEN = new SolidColorBrush(Color.FromRgb(192, 214, 167));
        public static SolidColorBrush TABLE_HIGHLIGHT_GREEN = new SolidColorBrush(Color.FromRgb(224, 235, 211));

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
        /// Returns brush for the passed level using
        /// the circular selection of colors.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static SolidColorBrush GetForegroundForLevel(int level)
        {
            SolidColorBrush brush;

            switch (level)
            {
                case 0:
                    brush = CurrentTheme.ModuloColor_0;
                    break;
                case 1:
                    brush = CurrentTheme.ModuloColor_1;
                    break;
                case 2:
                    brush = CurrentTheme.ModuloColor_2;
                    break;
                case 3:
                    brush = CurrentTheme.ModuloColor_3;
                    break;
                default:
                    brush = CurrentTheme.RtbForeground;
                    break;
            }

            return brush;
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
            if (LightMode.DataGridHeaderStyle == null)
            {
                LightMode.DataGridHeaderStyle = new Style(typeof(DataGridColumnHeader));
                LightMode.DataGridHeaderStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Black));
            }

            LightMode.RtbForeground = Brushes.Black;
            LightMode.RtbBackground = Brushes.White;
            LightMode.RtbForeground.Freeze();
            LightMode.RtbBackground.Freeze();

            LightMode.RtbSelectRunForeground = Brushes.White;
            LightMode.RtbSelectRunBackground = Brushes.Black;
            LightMode.RtbSelectRunForeground.Freeze();
            LightMode.RtbSelectRunBackground.Freeze();

            LightMode.RtbSelectLineForeground = Brushes.Black;
            LightMode.RtbSelectLineBackground = new SolidColorBrush(Color.FromRgb(255, 255, 206));
            LightMode.RtbSelectLineForeground.Freeze();
            LightMode.RtbSelectLineBackground.Freeze();

            LightMode.IntroMoveForeground = Brushes.Blue;
            LightMode.IntroDiagBackground = Brushes.Black;
            LightMode.IntroDiagSideCanvasBackground = Brushes.White;
            LightMode.IntroMoveForeground.Freeze();
            LightMode.IntroDiagBackground.Freeze();
            LightMode.IntroDiagSideCanvasBackground.Freeze();

            LightMode.IndexPrefixForeground = new SolidColorBrush(Color.FromRgb(18, 55, 97));
            LightMode.IndexPrefixForeground.Freeze();

            LightMode.ModuloColor_0 = Brushes.Blue;
            LightMode.ModuloColor_1 = Brushes.Green;
            LightMode.ModuloColor_2 = Brushes.Magenta;
            LightMode.ModuloColor_3 = Brushes.Firebrick;
            LightMode.ModuloColor_0.Freeze();
            LightMode.ModuloColor_1.Freeze();
            LightMode.ModuloColor_2.Freeze();
            LightMode.ModuloColor_3.Freeze();

            LightMode.BorderBrush = Brushes.Black;
            LightMode.BorderBrush.Freeze();

            LightMode.EngineLinesBackground = new SolidColorBrush(Color.FromRgb(0xF2, 0xF5, 0xF3));
            LightMode.EngineLinesBackground.Freeze();

            LightMode.DarkShadeOpacity = 0;

            LightMode.HintErrorForeground = Brushes.Red;
            LightMode.HintInfoForeground = Brushes.Green;
            LightMode.HintProgressForeground = Brushes.Blue;
            LightMode.HintErrorForeground.Freeze();
            LightMode.HintInfoForeground.Freeze();
            LightMode.HintProgressForeground.Freeze();

            LightMode.RtbSelectMoveWhileCopyForeground = Brushes.White;
            LightMode.RtbSelectMoveWhileCopyBackground = Brushes.Blue;
            LightMode.RtbSelectMovesForCopyBackground = Brushes.LightBlue;
            LightMode.RtbSelectMoveWhileCopyBackground.Freeze();
            LightMode.RtbSelectMoveWhileCopyBackground.Freeze();
            LightMode.RtbSelectMovesForCopyBackground.Freeze();

            LightMode.TrainingCheckmateForeground = Brushes.Navy;
            LightMode.TrainingTakebackForeground = Brushes.DarkOrange;
            LightMode.TrainingEngineGameForeground = Brushes.Brown;
            LightMode.TrainingCheckmateForeground.Freeze();
            LightMode.TrainingTakebackForeground.Freeze();
            LightMode.TrainingEngineGameForeground.Freeze();

            LightMode.ChaptersCreateIntroForeground = Brushes.Gray;
            LightMode.ChaptersCreateIntroForeground.Freeze();

            LightMode.BookmarksBackground = new SolidColorBrush(Color.FromRgb(229, 229, 229));
            LightMode.BookmarksBackground.Freeze();

            LightMode.HyperlinkForeground = new SolidColorBrush(Color.FromRgb(0, 153, 213));
            LightMode.HyperlinkHoveredForeground = new SolidColorBrush(Color.FromRgb(79, 102, 165));
            LightMode.HyperlinkForeground.Freeze();
            LightMode.HyperlinkHoveredForeground.Freeze();

            LightMode.GameExerciseRefForeground = new SolidColorBrush(Color.FromRgb(0, 132, 132));
            LightMode.GameExerciseRefHoveredForeground = new SolidColorBrush(Color.FromRgb(0, 106, 106));
            LightMode.GameExerciseRefForeground.Freeze();
            LightMode.GameExerciseRefHoveredForeground.Freeze();

            LightMode.ChapterRefForeground = new SolidColorBrush(Color.FromRgb(191, 0, 96));
            LightMode.ChapterRefHoveredForeground = new SolidColorBrush(Color.FromRgb(157, 0, 79));
            LightMode.ChapterRefForeground.Freeze();
            LightMode.ChapterRefHoveredForeground.Freeze();
        }

        /// <summary>
        /// Sets colors for the Dark Mode.
        /// </summary>
        private static void InitDarkModeSet()
        {
            if (DarkMode.DataGridHeaderStyle == null)
            {
                DarkMode.DataGridHeaderStyle = new Style(typeof(DataGridColumnHeader));
                DarkMode.DataGridHeaderStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Black));
            }

            DarkMode.RtbForeground = Brushes.White;
            DarkMode.RtbBackground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));
            DarkMode.RtbForeground.Freeze();
            DarkMode.RtbBackground.Freeze();

            DarkMode.RtbSelectRunForeground = Brushes.Black;
            DarkMode.RtbSelectRunBackground = Brushes.White;
            DarkMode.RtbSelectRunForeground.Freeze();
            DarkMode.RtbSelectRunBackground.Freeze();

            DarkMode.RtbSelectLineForeground = Brushes.White;
            DarkMode.RtbSelectLineBackground = new SolidColorBrush(Color.FromRgb(100, 100, 0));
            DarkMode.RtbSelectLineForeground.Freeze();
            DarkMode.RtbSelectLineBackground.Freeze();

            DarkMode.IntroMoveForeground = Brushes.LightBlue;
            DarkMode.IntroDiagBackground = Brushes.Black;
            DarkMode.IntroDiagSideCanvasBackground = DarkMode.RtbBackground;
            DarkMode.IntroMoveForeground.Freeze();
            DarkMode.IntroDiagBackground.Freeze();
            DarkMode.IntroDiagSideCanvasBackground.Freeze();

            DarkMode.IndexPrefixForeground = Brushes.LightBlue; 
            DarkMode.IndexPrefixForeground.Freeze();

            DarkMode.ModuloColor_0 = Brushes.LightCyan;
            DarkMode.ModuloColor_1 = Brushes.LightGreen;
            DarkMode.ModuloColor_2 = Brushes.LightPink;
            DarkMode.ModuloColor_3 = Brushes.Yellow;
            DarkMode.ModuloColor_0.Freeze();
            DarkMode.ModuloColor_1.Freeze();
            DarkMode.ModuloColor_2.Freeze();
            DarkMode.ModuloColor_3.Freeze();

            DarkMode.BorderBrush = Brushes.White;
            DarkMode.BorderBrush.Freeze();

            DarkMode.EngineLinesBackground = DarkMode.RtbBackground;
            DarkMode.EngineLinesBackground.Freeze();

            DarkMode.DarkShadeOpacity = 0.1;

            DarkMode.HintErrorForeground = Brushes.OrangeRed;
            DarkMode.HintInfoForeground = Brushes.LightGreen;
            DarkMode.HintProgressForeground = Brushes.LightBlue;
            DarkMode.HintErrorForeground.Freeze();
            DarkMode.HintInfoForeground.Freeze();
            DarkMode.HintProgressForeground.Freeze();

            DarkMode.RtbSelectMoveWhileCopyForeground = Brushes.Black;
            DarkMode.RtbSelectMoveWhileCopyBackground = Brushes.LightBlue;
            DarkMode.RtbSelectMovesForCopyBackground = Brushes.Blue;
            DarkMode.RtbSelectMoveWhileCopyForeground.Freeze();
            DarkMode.RtbSelectMoveWhileCopyBackground.Freeze();
            DarkMode.RtbSelectMovesForCopyBackground.Freeze();

            DarkMode.TrainingCheckmateForeground = Brushes.LightBlue;
            DarkMode.TrainingTakebackForeground = Brushes.Yellow;
            DarkMode.TrainingEngineGameForeground = Brushes.Gold;
            DarkMode.TrainingCheckmateForeground.Freeze();
            DarkMode.TrainingTakebackForeground.Freeze();
            DarkMode.TrainingEngineGameForeground.Freeze();

            DarkMode.ChaptersCreateIntroForeground = Brushes.LightGray;
            DarkMode.BookmarksBackground = DarkMode.RtbBackground;
            DarkMode.ChaptersCreateIntroForeground.Freeze();
            DarkMode.BookmarksBackground.Freeze();

            DarkMode.HyperlinkForeground = new SolidColorBrush(Color.FromRgb(9, 147, 189));
            DarkMode.HyperlinkHoveredForeground = new SolidColorBrush(Color.FromRgb(0, 244, 255));
            DarkMode.HyperlinkForeground.Freeze();
            DarkMode.HyperlinkHoveredForeground.Freeze();

            DarkMode.GameExerciseRefForeground = new SolidColorBrush(Color.FromRgb(29, 167, 209));
            DarkMode.GameExerciseRefHoveredForeground = new SolidColorBrush(Color.FromRgb(0, 244, 255));
            DarkMode.GameExerciseRefForeground.Freeze();
            DarkMode.GameExerciseRefHoveredForeground.Freeze();

            DarkMode.ChapterRefForeground = new SolidColorBrush(Color.FromRgb(255, 98, 176));
            DarkMode.ChapterRefHoveredForeground = new SolidColorBrush(Color.FromRgb(255, 147, 201));
            DarkMode.ChapterRefForeground.Freeze();
            DarkMode.ChapterRefHoveredForeground.Freeze();
        }
    }
}

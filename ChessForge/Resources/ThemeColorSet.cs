using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Supported color themes
    /// </summary>
    public enum ColorThemes
    {
        DARK_MODE,
        LIGHT_MODE,
    }

    /// <summary>
    /// Colors that must be defined for a color theme/mode.
    /// </summary>
    public class ThemeColorSet
    {
        /// <summary>
        /// Foreground color for the RichTextBoxes
        /// </summary>
        public SolidColorBrush RtbForeground;

        /// <summary>
        /// Background color for the RichTextBoxes
        /// </summary>
        public SolidColorBrush RtbBackground;


        /// <summary>
        /// Foreground color for the selected run.
        /// </summary>
        public SolidColorBrush RtbSelectRunForeground;

        /// <summary>
        /// Background color for the selected run.
        /// </summary>
        public SolidColorBrush RtbSelectRunBackground;


        /// <summary>
        /// Color to use for the foreground of the highlighted line.
        /// </summary>
        public SolidColorBrush RtbSelectLineForeground;

        /// <summary>
        /// Color to use for the background of the highlighted line.
        /// </summary>
        public SolidColorBrush RtbSelectLineBackground;


        /// <summary>
        /// Foreground color of the selected move
        /// while there is a copy selection
        /// e.g. for copy/paste
        /// </summary>
        public SolidColorBrush RtbSelectMoveWhileCopyForeground;

        /// <summary>
        /// Background color of the selected move
        /// while there is a copy selection
        /// e.g. for copy/paste
        /// </summary>
        public SolidColorBrush RtbSelectMoveWhileCopyBackground;

        /// <summary>
        /// Background color for the multi-move selection
        /// e.g. for copy/paste
        /// </summary>
        public SolidColorBrush RtbSelectMovesForCopyBackground;


        /// <summary>
        /// Forground of the nonselected DataGrid cell
        /// </summary>
        public SolidColorBrush DataGridForeground;

        /// <summary>
        /// Background of the nonselected DataGrid cell
        /// </summary>
        public SolidColorBrush DataGridBackground;

        /// <summary>
        /// Foreground for the Create Intro text in the Chapters view.
        /// </summary>
        public SolidColorBrush ChaptersCreateIntroForeground;

        /// <summary>
        /// Foreground of the selected DataGrid cell
        /// </summary>
        public SolidColorBrush DataGridSelectForeground;

        /// <summary>
        /// Background of the selected DataGrid cell
        /// </summary>
        public SolidColorBrush DataGridSelectBackground;

        /// <summary>
        /// Background of the Bookmarks View
        /// </summary>
        public SolidColorBrush BookmarksBackground;

        /// <summary>
        /// Foreground of the move in Intro
        /// </summary>
        public SolidColorBrush IntroMoveForeground;

        /// <summary>
        /// Background of the diagram in Intro
        /// </summary>
        public SolidColorBrush IntroDiagBackground;

        /// <summary>
        /// Background of the diagram's side canvas in Intro.
        /// </summary>
        public SolidColorBrush IntroDiagSideCanvasBackground;

        /// <summary>
        /// Foreground of index prefixes.
        /// </summary>
        public SolidColorBrush IndexPrefixForeground;

        /// <summary>
        /// Background of the Engine Lines TextBox.
        /// </summary>
        public SolidColorBrush EngineLinesBackground;

        /// <summary>
        /// Color to be used for the border of CommentBox,
        /// Engine Lines and Eval Chart.
        /// </summary>
        public SolidColorBrush BorderBrush;

        /// <summary>
        /// Foreground to ask the user about the takeback
        /// </summary>
        public SolidColorBrush TrainingTakebackForeground;

        /// <summary>
        /// Foreground to announce checkmate or stalemate
        /// in Training View
        /// </summary>
        public SolidColorBrush TrainingCheckmateForeground;

        /// <summary>
        /// Foreground for engine game moves
        /// </summary>
        public SolidColorBrush TrainingEngineGameForeground;

        /// <summary>
        /// Colors for various Hint Types in the Comment Box
        /// </summary>
        public SolidColorBrush HintErrorForeground;
        public SolidColorBrush HintInfoForeground;
        public SolidColorBrush HintProgressForeground;

        /// <summary>
        /// Colors to rotate through in the tree views.
        /// </summary>
        public SolidColorBrush ModuloColor_0;
        public SolidColorBrush ModuloColor_1;
        public SolidColorBrush ModuloColor_2;
        public SolidColorBrush ModuloColor_3;

        /// <summary>
        /// Opacity of the shade over the main chessboard
        /// </summary>
        public double DarkShadeOpacity;

        /// <summary>
        /// Style to use for the DataGerid Header
        /// </summary>
        public System.Windows.Style DataGridHeaderStyle;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace ChessForge
{
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
        /// Background color for the multi-move selection
        /// e.g. for copy/paste
        /// </summary>
        public SolidColorBrush RtbSelectMovesBackground;


        /// <summary>
        /// Forground of the nonselected DataGrid cell
        /// </summary>
        public SolidColorBrush DataGridForeground;

        /// <summary>
        /// Background of the nonselected DataGrid cell
        /// </summary>
        public SolidColorBrush DataGridBackground;


        /// <summary>
        /// Foreground of the selected DataGrid cell
        /// </summary>
        public SolidColorBrush DataGridSelectForeground;

        /// <summary>
        /// Background of the selected DataGrid cell
        /// </summary>
        public SolidColorBrush DataGridSelectBackground;

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
        /// Colors to rotate through in the tree views.
        /// </summary>
        public SolidColorBrush ModuloColor_0;
        public SolidColorBrush ModuloColor_1;
        public SolidColorBrush ModuloColor_2;
        public SolidColorBrush ModuloColor_3;
    }
}

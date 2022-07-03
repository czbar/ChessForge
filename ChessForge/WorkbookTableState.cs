using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChessForge
{
    internal class WorkbookTableState
    {
        /// <summary>
        /// The number of plies in the stem of the game
        /// i.e. before the first fork.
        /// </summary>
        public static int StemLength;

        /// <summary>
        /// The maximum number of moves to show in the table.
        /// </summary>
        public static int MaxMoves;

        public static bool IsInitialized;

        /// <summary>
        /// Selected TextBox (cell in the table)
        /// </summary>
        public static TextBlock SelectedTextBlock
        {
            get { return _selectedTextBlock; }
            set { _selectedTextBlock = value; }
        }

        /// <summary>
        /// Color of the foreground for the selected move.
        /// </summary>
        //public static SolidColorBrush HighlightForeground
        //{
        //    get { return _highlightForeground; }
        //    set { _highlightForeground = value; }
        //}

        public static int SelectedRow { get => _selectedRow; set => _selectedRow = value; }

        public static int SelectedColumn { get => _selectedColumn; set => _selectedColumn = value; }

        /// <summary>
        /// Resets selections and highlights.
        /// </summary>
        public static void Reset()
        {
            _selectedTextBlock = null;
//            _highlightForeground = null;
            IsInitialized = false;
        }

        private static TextBlock _selectedTextBlock;
//        private static SolidColorBrush _highlightForeground;

        private static int _selectedRow = -1;
        private static int _selectedColumn = -1;
    }
}

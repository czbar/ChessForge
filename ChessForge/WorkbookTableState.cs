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
#if false // suppressing "never assigned to" warning while the WorkbookTable code is commented out
        /// <summary>
        /// The number of plies in the stem of the game
        /// i.e. before the first fork.
        /// </summary>
        public static int StemLength;

        /// <summary>
        /// The maximum number of moves to show in the table.
        /// </summary>
        public static int MaxMoves;
#endif

        /// <summary>
        /// Indicates whether the Table has been initialized
        /// </summary>
        public static bool IsInitialized;

        /// <summary>
        /// Selected TextBox (cell in the table).
        /// </summary>
        public static TextBlock SelectedTextBlock
        {
            get { return _selectedTextBlock; }
            set { _selectedTextBlock = value; }
        }

        /// <summary>
        /// Returns the selected row
        /// </summary>
        public static int SelectedRow { get => _selectedRow; set => _selectedRow = value; }

        /// <summary>
        /// Returns the selected column.
        /// </summary>
        public static int SelectedColumn { get => _selectedColumn; set => _selectedColumn = value; }

        /// <summary>
        /// Resets selections and highlights.
        /// </summary>
        public static void Reset()
        {
            _selectedTextBlock = null;
            IsInitialized = false;
        }

        private static TextBlock _selectedTextBlock;

        private static int _selectedRow = -1;
        private static int _selectedColumn = -1;
    }
}

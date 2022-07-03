using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// A class specifically for use in the Tree Table view (WPF ListView).
    /// Each row displays moves (plies) of one side, White or Black. A row
    /// of Black's moves follows a row of White's moves aligned so that line's
    /// moves are comprised of entries in two consecutive row.
    /// For readability, we shade each pairs of rows interchangably so that it is easier 
    /// to read.
    /// Therefore, we end up with 4 row "types": White/Black combined with Odd/Even.
    /// </summary>
    public class TreeTableRow
    {
        public List<TreeTableCell> Cells
        {
            get { return _cells; }
            set { }
        }

        /// <summary>
        /// The list of plies that we will bind to the ItemsSource of the hosting ListView.
        /// </summary>
        //public List<string> Moves
        //{
        //    get { return _moves; }
        //    set { }
        //}

        /// <summary>
        /// Holds attributes of the row to display.
        /// It parallels the Moves list in that attributes at a certain index here correspond to 
        /// the move in Moves. TODO: we should combine move and attrs in one object once
        /// all the binding issues have been resolved.
        /// </summary>
        //public List<int> PlieAttrs
        //{
        //    get { return _plieAttrs; }
        //    set { }
        //}

        //public List<int> NodeIds
        //{
        //    get { return _nodeIds; }
        //    set { }
        //}

        /// <summary>
        /// Tool tips that will be displayed for the row under conditions
        /// specified in data binding triggers, and created externally.
        /// </summary>
        //public List<string> ToolTips
        //{
        //    get { return _toolTips; }
        //    set {}
        //}

        public int HasToolTip 
        {
            get { return 1; }
//            get { return string.IsNullOrWhiteSpace(_toolTip) ? 0 : 1; } 
        }

        /// <summary>
        /// Determines whether the row represents White's or Black's 
        /// plies.
        /// </summary>
        public string RowColor
        {
            get { return _rowColor; }
            set { _rowColor = value; }
        }

        public int VariationLineNumber
        {
            get { return _variationLineNumber; }
            set { _variationLineNumber = value; }
        }

        /// <summary>
        /// A value in the range of 0-3 representing the type of the row White/Black
        /// combined with Odd/Even.
        /// </summary>
        public int RowType
        {
            get { return ((_variationLineNumber * 2) + (_rowColor == Strings.ROW_COLOR_WHITE ? 0 : 1)) % 4; }
        }

        //private List<string> _moves = new List<string>();
        //private List<string> _toolTips = new List<string>();
        //private List<int> _plieAttrs = new List<int>();
//        private List<int> _nodeIds = new List<int>();

        private List<TreeTableCell> _cells = new List<TreeTableCell>();  

        private string _rowColor;
        private int _variationLineNumber;
    }
}

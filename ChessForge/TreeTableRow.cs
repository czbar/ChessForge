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

        public int HasToolTip 
        {
            get { return 1; }
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

        private List<TreeTableCell> _cells = new List<TreeTableCell>();  

        private string _rowColor;
        private int _variationLineNumber;
    }
}

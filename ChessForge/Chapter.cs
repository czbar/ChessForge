using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// A chapter within a Workbook.
    /// A chapter comprises one or more VariationTrees. 
    /// </summary>
    public class Chapter
    {
        // number of this chapter
        private int _number;

        // Variation Trees constituting this chapter.
        private List<WorkbookTree> _variationTrees = new List<WorkbookTree>();

        /// <summary>
        /// Number of this chapter.
        /// </summary>
        public int Number
        {
            get => _number; set => _number = value;
        }
    }
}

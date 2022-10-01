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

        /// <summary>
        /// Number of this chapter.
        /// </summary>
        public int Number
        {
            get => _number; set => _number = value;
        }

        /// <summary>
        /// The analysis tree of the chapter. There is exactly one
        /// analysis tree in a chapter.
        /// </summary>
        public VariationTree AnalysisTree = new VariationTree();

        /// <summary>
        /// The list of Model Games
        /// </summary>
        public List<VariationTree> ModelGames = new List<VariationTree>();

        /// <summary>
        /// The list of combinations.
        /// </summary>
        public List<VariationTree> Combinations = new List<VariationTree>();

    }
}

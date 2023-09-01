using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// A class of objects that will be passed to and from the background worker
    /// </summary>
    public class BackgroundPgnParserData
    {
        /// <summary>
        /// Identifies the processor using this objects
        /// </summary>
        public int ArticleIndex;

        /// <summary>
        /// Text to parse.
        /// </summary>
        public string ArticleText;

        /// <summary>
        /// Variation Tree to be populated.
        /// </summary>
        public VariationTree Tree;

        /// <summary>
        /// Fen of the initial position,
        /// if different from the starting position.
        /// </summary>
        public string Fen;
    }
}

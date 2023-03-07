using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates info about a diagram within the Intro view.
    /// </summary>
    public class IntroViewDiagram
    {
        /// <summary>
        /// The Chessboard object.
        /// </summary>
        public ChessBoardSmall Chessboard;

        /// <summary>
        /// Name of the paragraph the diagram is in.
        /// </summary>
        public string ParagraphName;

        /// <summary>
        /// The position represented by the diagram.
        /// </summary>
        public TreeNode Node;
    }
}

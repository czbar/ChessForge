using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Holds attributes of the diagram to generate an image for.
    /// </summary>
    public class RtfDiagram
    {
        /// <summary>
        /// An ID uniquely identifying the diagram while
        /// the document is being built.
        /// </summary>
        public int DiagramId;

        /// <summary>
        /// TreeNode represented by the diagram.
        /// </summary>
        public TreeNode Node;

        /// <summary>
        /// Whether the diagram should be flipped.
        /// </summary>
        public bool IsFlipped;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="isFlipped"></param>
        public RtfDiagram(int diagramId, TreeNode node, bool isFlipped)
        {
            DiagramId = diagramId;
            Node = node;
            IsFlipped = isFlipped;
        }
    }
}

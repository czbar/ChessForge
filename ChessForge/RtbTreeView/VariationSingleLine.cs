using GameTree;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChessForge
{
    /// <summary>
    /// A list of nodes forming a single path from one node to another
    /// without any branches included.
    /// </summary>
    public class VariationSingleLine
    {
        // the list of nodes forming a single linear path.
        private List<TreeNode> _nodes = new List<TreeNode>();

        // whether the line has any forks
        private bool _hasForks;

        /// <summary>
        /// The list of nodes in this line.
        /// </summary>
        public List<TreeNode> Nodes
        {
            get => _nodes;
            set => _nodes = value;
        }

        /// <summary>
        /// Whether any node in this line is a fork.
        /// If not, it may be aggregated into a single VariationDisplayLine
        /// with other VariationSingleLines.
        /// </summary>
        public bool HasForks
        {
            get => _hasForks;
            set => _hasForks = value;
        }
    }
}

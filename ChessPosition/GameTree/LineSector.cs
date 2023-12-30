using System;
using System.Collections.Generic;
using System.Text;

namespace GameTree
{
    /// <summary>
    /// Type of LineSectors.
    /// A LineSector is of type LEAF, if its last TreeNode is a leaf i.e. has no children.
    /// A LineSector is of type FORKING, if the last node is a fork i.e. has 2 children or more.
    /// Note that the last TreeNode in a LineSector cannot have exactly one child because
    /// that one child would have to be a member of such a LineSector.
    /// </summary>
    public enum LineSectorType
    {
        UNKNOWN,
        LEAF,
        FORKING
    }

    /// <summary>
    /// A list of nodes forming a single path from one node to another
    /// without any branches included.
    /// All LineSectors begin at a child of the root node (LineSectorId = 0) or at a child of a fork.
    /// They end at a leaf node (a node with no children) or at a fork.
    /// </summary>
    public class LineSector
    {
        // the list of nodes forming a single linear path.
        private List<TreeNode> _nodes = new List<TreeNode>();

        // level at which to display the sector
        private int _branchLevel = 0;

        /// <summary>
        /// Id of this LineSector.
        /// </summary>
        public int LineSectorId= 0;

        /// <summary>
        /// Type of this sector.
        /// </summary>
        public LineSectorType SectorType;

        /// <summary>
        /// List of child LineSectors.
        /// </summary>
        public List<LineSector> Children = new List<LineSector>();

        /// <summary>
        /// Parent of this LineSector.
        /// </summary>
        public LineSector Parent;

        /// <summary>
        /// Adds a child LineSector.
        /// </summary>
        /// <param name="sector"></param>
        public void AddChild(LineSector sector)
        {
            Children.Add(sector);
            sector.Parent = this;
        }

        /// <summary>
        /// The list of nodes in this line.
        /// </summary>
        public List<TreeNode> Nodes
        {
            get => _nodes;
            set => _nodes = value;
        }

        /// <summary>
        /// Level at which to display the sector
        /// </summary>
        public int BranchLevel
        {
            get => _branchLevel;
            set => _branchLevel = value;
        }
    }
}

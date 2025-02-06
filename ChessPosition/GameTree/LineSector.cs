using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;

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
    /// They end at a leaf node (a node with no children) or at a fork (which is included).
    /// </summary>
    public class LineSector
    {
        /// <summary>
        /// Color to use for the first node in the sector, if any
        /// </summary>
        public Brush FirstNodeColor = null;

        // the list of nodes forming a single linear path.
        private List<TreeNode> _nodes = new List<TreeNode>();

        // branch depth at which the sector is placed
        private int _branchLevel = 0;

        // display level at whihc to show the sector
        private int _displayLevel = 0;

        /// <summary>
        /// Attributes of the paragraph represented by the Sector.
        /// </summary>
        public SectorParaAttrs ParaAttrs;

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
        /// Paragraph hosting this LineSector
        /// </summary>
        public Paragraph HostPara;

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
        /// Branch depth level at which the sector belongs.
        /// </summary>
        public int BranchLevel
        {
            get => _branchLevel;
            set => _branchLevel = value;
        }

        /// <summary>
        /// Level at which to display the sector
        /// </summary>
        public int DisplayLevel
        {
            get => _displayLevel;
            set => _displayLevel = value;
        }

        /// <summary>
        /// Whether this sector should be shown as collapsed.
        /// It is determined by the IsCollapsed state of the first Node
        /// of the sector.
        /// </summary>
        public bool IsCollapsed
        {
            get
            {
                return (Nodes.Count > 0 && Nodes[0].IsCollapsed);
            }
        }

        /// <summary>
        /// NodeId code for open parenthesis
        /// </summary>
        public static readonly int OPEN_BRACKET = -100;

        /// <summary>
        /// NodeId code for close parenthesis
        /// </summary>
        public static readonly int CLOSE_BRACKET = -101;

        /// <summary>
        /// Inserts a Node representing an open parenthesis
        /// at the specified index in the Nodes list.
        /// </summary>
        /// <param name="index"></param>
        public void InsertOpenBracketNode(int index)
        {
            TreeNode node = new TreeNode();
            node.NodeId = OPEN_BRACKET;
            node.LastMoveAlgebraicNotation = "(";
            Nodes.Insert(index, node);
        }

        /// <summary>
        /// Inserts a Node representing a close parenthesis
        /// at the specified index in the Nodes list.
        /// </summary>
        /// <param name="index"></param>
        public void InsertCloseBracketNode(int index)
        {
            TreeNode node = new TreeNode();
            node.NodeId = CLOSE_BRACKET;
            node.LastMoveAlgebraicNotation = ")";
            Nodes.Insert(index, node);
        }

    }
}

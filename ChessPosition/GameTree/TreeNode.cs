using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessPosition;

namespace GameTree
{
    /// <summary>
    /// A TreeNode contains precisely 1 Position, 
    /// 1 pointer to the parent TreeNode (null for the root Node.
    /// and 0 or more pointers to child nodes (i.e. half-moves / plies that follow this one).
    /// 
    /// Position holds "LastMove" is the move that immediately led to the position in this node.
    /// If that was a White's move, the color to move in the position will be Black
    /// and vice versa.
    /// </summary>
    public class TreeNode
    {
        /// <summary>
        /// Pointer to the parent node,
        /// null if this node is
        /// at the root of the tree
        /// </summary>
        public TreeNode Parent;

        /// <summary>
        /// Id of the node which is unique across the tree (a.k.a. Workbook)
        /// </summary>
        public int NodeId;

        /// <summary>
        /// Child nodes.
        /// </summary>
        public List<TreeNode> Children = new List<TreeNode>();

        /// <summary>
        /// List of Chess Forge commands associated with the leadup move,
        /// if any.
        /// </summary>
        public List<string> UnprocessedChfCommands = new List<string>();

        /// <summary>
        /// A text comment associated with the leadup move
        /// </summary>
        public string Comment = null;

        // the move leading to this position (algebraic notation)
        private string _lastMoveAlg;

        // the move leading to this position (algebraic notation with NAG symbols)
        private string _lastMoveAlgWithNag;

        /// <summary>
        /// Numeric Annotation Glyphs associated with this
        /// move, if any.
        /// </summary>
        public string Nags = "";

        /// <summary>
        /// The color of the side on move in this position.
        /// </summary>
        public PieceColor ColorToMove => Position.ColorToMove;

        /// <summary>
        /// The move leading to this psoition in algebraic notation (e.g. "d4", "Nbd2", "e8Q")
        /// </summary>
        public string LastMoveAlgebraicNotation
        {
            get { return _lastMoveAlg; }
            set
            {
                _lastMoveAlg = value;
                _lastMoveAlgWithNag = value;
            }
        }

        public string LastMoveAlgebraicNotationWithNag
        {
            get { return _lastMoveAlgWithNag; }
            set { _lastMoveAlgWithNag = value; }
        }

        /// <summary>
        /// Adds a nag string to the NAG string for this node.
        /// </summary>
        /// <param name="nag"></param>
        public void AddNag(string nag)
        {
            // double check that this is a nag
            if (nag.Length < 2 || nag[0] != '$')
                return;

            // insert space between NAGs so they re ready for writing out without further parsing
            Nags += " " + nag;

            int res;
            if (int.TryParse(nag.Substring(1), out res))
            {
                string nagStr;
                if (Constants.NagsDict.TryGetValue(res, out nagStr))
                {
                    if (string.IsNullOrEmpty(LastMoveAlgebraicNotationWithNag))
                    {
                        LastMoveAlgebraicNotationWithNag = LastMoveAlgebraicNotation + nagStr;
                    }
                    else
                    {
                        LastMoveAlgebraicNotationWithNag += nagStr;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a string to the list of unprocessed CHF commands.
        /// This will be called from the parser.
        /// </summary>
        /// <param name="cmd"></param>
        public void AddUnprocessedChfCommand(string cmd)
        {
            UnprocessedChfCommands.Add(cmd);
        }

        /// <summary>
        /// LastMove in engine notation (e.g. "d2d4", "b1d2", "e7e8q")
        /// </summary>
        public string LastMoveEngineNotation
        {
            get
            {
                return Position.LastMove.GetEngineNotation();
            }
        }

        /// <summary>
        /// If the line id consists of 1 and dots only
        /// this node belongs to the main line.
        /// </summary>
        /// <returns></returns>
        public bool IsMainLine()
        {
            foreach (char c in LineId)
            {
                if (c != '.' && c != '1')
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Id of the line this move belongs to.
        /// It is in the form of integers separated by dots e.g. "1.2.1.3".
        /// The "main" line will be of the form "1.1.1.1.1"
        /// </summary>
        public string LineId = "";

        /// <summary>
        /// The number of Nodes to traverse before arriving at the next fork/branch/inode.
        /// Special values:
        /// -1  - means there is no next fork i.e. the branch leads directly to a leaf
        ///  0  - an invalid / uninitialized value.
        /// </summary>
        public int DistanceToNextFork = 0;

        /// <summary>
        /// The number of Nodes to traverse before arriving at the leaf.
        /// Special values.
        ///  0 - this node is a leaf
        /// -1 - there is a fork before we reach a leaf.
        /// </summary>
        public int DistanceToLeaf;

        /// <summary>
        /// Index of the first character of this move
        /// in the string shown in the game notation window.
        /// </summary>
        public int TextStart;

        /// <summary>
        /// Index of the last character of this move
        /// in the string shown in the game notation window.
        /// </summary>
        public int TextEnd;

        /// <summary>
        /// Position for this node.
        /// </summary>
        public BoardPosition Position = new BoardPosition();

        /// <summary>
        /// Indicates whether this position is a bookmark.
        /// </summary>
        public bool IsBookmark = false;

        /// <summary>
        /// Creates a new object as a child of the passed parent.
        /// Initializes the IheritedEnPassant square and the
        /// DynamicProperties based on the parent's properties.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="alg"></param>
        /// <param name="nodeId"></param>
        public TreeNode(TreeNode parent, string alg, int nodeId)
        {
            Parent = parent;
            LastMoveAlgebraicNotation = alg;
            if (parent != null)
            {
                Position.InheritedEnPassantSquare = parent.Position.EnPassantSquare;
                Position.DynamicProperties = parent.Position.DynamicProperties;
            }
            NodeId = nodeId;
        }

        /// <summary>
        /// Adds a node to this node's Children list.
        /// </summary>
        /// <param name="node"></param>
        public void AddChild(TreeNode node)
        {
            Children.Add(node);
        }

        /// <summary>
        /// Move number property exposing
        /// the MoveNumber property from Position.
        /// </summary>
        public uint MoveNumber
        {
            get { return Position.MoveNumber; }
            set { Position.MoveNumber = value; }
        }

        /// <summary>
        /// Text to show if the move is displayed somewhere.
        /// </summary>
        /// <returns></returns>
        public string GetPlyText(bool standalone)
        {
            StringBuilder sb = new StringBuilder();
            if (ColorToMove == PieceColor.Black)
            {
                sb.Append(MoveNumber.ToString() + ".");
            }
            else if (standalone)
            {
                sb.Append(MoveNumber.ToString() + "...");
            }
            sb.Append(LastMoveAlgebraicNotationWithNag);

            return sb.ToString();
        }
    }
}

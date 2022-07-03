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
        /// Child nodes.
        /// </summary>
        public List<TreeNode> Children = new List<TreeNode>();

        /// <summary>
        /// LastMove in algebraic notation (e.g. "d4", "Nbd2", "e8Q")
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

        private string _lastMoveAlg;

        public string LastMoveAlgebraicNotationWithNag
        {
            get { return _lastMoveAlgWithNag; }
            set { _lastMoveAlgWithNag = value; }
        }

        private string _lastMoveAlgWithNag;

        /// <summary>
        /// Numeric Annotation Glyphs associated with this
        /// move, if any.
        /// </summary>
        private string Nags = "";

        public void AddNag(string nag)
        {
            // double check that this is a nag
            if (nag.Length < 2 || nag[0] != '$')
                return;

            Nags += nag;

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
        /// Id of the node which is unique across the tree (a.k.a. Workbook)
        /// </summary>
        public int NodeId;

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

        public void AddChild(TreeNode node)
        {
            Children.Add(node);
        }

        public PieceColor ColorToMove()
        {
            return Position.ColorToMove;
        }

        public uint MoveNumber()
        {
            return Position.MoveNumber;
        }

        /// <summary>
        /// Text to show if the move is displayed somewhere.
        /// </summary>
        /// <returns></returns>
        public string GetPlyText(bool standalone)
        {
            StringBuilder sb = new StringBuilder();
            if (ColorToMove() == PieceColor.Black)
            {
                sb.Append(MoveNumber().ToString() + ".");
            }
            else if (standalone)
            {
                sb.Append(MoveNumber().ToString() + "...");
            }
            sb.Append(LastMoveAlgebraicNotationWithNag);

            return sb.ToString();
        }
    }
}

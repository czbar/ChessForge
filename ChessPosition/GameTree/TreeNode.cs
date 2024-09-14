using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
    [Serializable()]
    public class TreeNode
    {
        /// <summary>
        /// Pointer to the parent node,
        /// null if this node is
        /// at the root of the tree
        /// </summary>
        public TreeNode Parent;

        /// <summary>
        /// Id of the node which is unique across the tree
        /// </summary>
        public int NodeId;

        /// <summary>
        /// Used in some context to keep track of selection
        /// e.g. in Training.
        /// </summary>
        public int SelectedChildIndex = -1;

        /// <summary>
        /// Encyclopaedia of chess openings code
        /// </summary>
        public string Eco;

        /// <summary>
        /// Opening's name
        /// </summary>
        public string OpeningName;

        /// <summary>
        /// For special data e.g. encoded XAML string.
        /// </summary>
        public string Data;

        /// <summary>
        /// Child nodes.
        /// </summary>
        public List<TreeNode> Children = new List<TreeNode>();

        /// <summary>
        /// Id of the line this move belongs to.
        /// It is in the form of integers separated by dots e.g. "1.2.1.3".
        /// The "main" line will be of the form "1.1.1.1.1"
        /// </summary>
        public string LineId = "";

        /// <summary>
        /// Position for this node.
        /// </summary>
        public BoardPosition Position = new BoardPosition();

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
        /// Holds engine evaluation if available.
        /// This is read only.
        /// Setting must be done via SetEngineEvaluation()
        /// method as we need to do some extra work there.
        /// </summary>
        public string EngineEvaluation
        {
            get => _engEval;
        }

        /// <summary>
        /// Points awarded in the analysis solving mode.
        /// </summary>
        public int QuizPoints
        {
            get => _quizPoints;
            set => _quizPoints = value;
        }

        /// <summary>
        /// The best response in to the move 
        /// contained in this position as determined by the engine
        /// when evaluating the previuos move.
        /// </summary>
        public string BestResponse
        {
            get => _bestResponse;
            set => _bestResponse = value;
        }

        /// <summary>
        /// Coded assessment of the move.
        /// </summary>
        public uint Assessment
        {
            get => _assessment;
            set => _assessment = value;
        }

        /// <summary>
        /// Whether this node has siblings.
        /// We check that the parent has more than one child.
        /// </summary>
        public bool HasSiblings(bool excludeTrainingMoves = true)
        {
            if (Parent == null || Parent.Children.Count <= 1)
            {
                return false;
            }

            int childrenCount = 0;
            foreach (TreeNode child in Parent.Children)
            {
                if (!excludeTrainingMoves || !child.IsNewTrainingMove)
                {
                    childrenCount++;
                }
            }

            return childrenCount > 1;
        }

        /// <summary>
        /// Returns the number of children for this node.
        /// </summary>
        /// <param name="inclTrainingMoves"></param>
        /// <returns></returns>
        public int GetChildrenCount(bool inclTrainingMoves = true)
        {
            int count = 0;
            if (inclTrainingMoves)
            {
                count = Children.Count;
            }
            else
            {
                foreach (TreeNode child in Children)
                {
                    if (!child.IsNewTrainingMove)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Sets engine evaluation of the node.
        /// </summary>
        /// <param name="eval"></param>
        /// <returns></returns>
        public ChfCommands.Assessment SetEngineEvaluation(string eval)
        {
            _engEval = eval;
            return ChfCommands.Assessment.NONE;
        }

        /// <summary>
        /// Whether a position from this node is used as a thumbnail.
        /// </summary>
        public bool IsThumbnail = false;

        /// <summary>
        /// Whether a position from this node should be shown on the inline diagram.
        /// </summary>
        public bool IsDiagram = false;

        /// <summary>
        /// References to Games or Exercises
        /// in the form of GUID|GUID|...|GUID
        /// </summary>
        public string ArticleRefs;

        /// <summary>
        /// General purpose property to assist certain
        /// processing scenarios e.g. analysing a submitted
        /// solution.
        /// Not persisted with the tree.
        /// </summary>
        public bool IsProcessed = false;

        /// <summary>
        /// Indicates whether this position is a bookmark.
        /// </summary>
        public bool IsBookmark = false;

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
        public int DistanceToLeaf = -1;

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
        /// List of Chess Forge commands associated with the leadup move
        /// that we are not handling.
        /// We will preserve them in this list so that we will write them out.
        /// </summary>
        public List<string> UnprocessedChfCommands = new List<string>();

        /// <summary>
        /// Marks a node that was not in the tree when the training started.
        /// Depending on user choices, it may be later on removed from the tree
        /// or added permanently.
        /// </summary>
        public bool IsNewTrainingMove = false;

        /// <summary>
        // Expanded/collapsed state of the branch starting at this node.
        /// </summary>
        public bool IsCollapsed
        {
            get => _isCollapsed;
            set => _isCollapsed = value;
        }

        /// <summary>
        /// Marks a node that was not in the tree when it was open.
        /// This is an edit by the user.
        /// Depending on user choices, it may be later on removed from the tree
        /// or added permanently.
        /// </summary>
        public bool IsNewUserMove = false;

        /// <summary>
        /// A text comment associated with the leadup move
        /// </summary>
        public string Comment = null;

        /// <summary>
        /// A text comment to insert before the move
        /// </summary>
        public string CommentBeforeMove = null;

        // the move leading to this position (algebraic notation)
        private string _lastMoveAlg;

        // the move leading to this position (algebraic notation with NAG symbols)
        private string _lastMoveAlgWithNag;

        // engine evaluation
        private string _engEval;

        // best response in this position as determined by the engine
        private string _bestResponse;

        // points awarded in the solving analysis mode
        private int _quizPoints;

        // coded assessment of the move
        private uint _assessment;

        // expanded/collapsed state of the branch starting at this node.
        private bool _isCollapsed;

        /// <summary>
        /// Numeric Annotation Glyphs associated with this
        /// move, if any.
        /// </summary>
        public string Nags = "";

        /// <summary>
        /// Arrows drawn by the user in this position
        /// separated by a comma.
        /// Encoded as string: eg. Gd1d8,Gh3g2 (G for green)
        /// </summary>
        public string Arrows = "";

        /// <summary>
        /// Circles drawn by the user indicating squares of interest.
        /// Encoded as string: e.g. Rf3,Re2 (R for red).
        /// </summary>
        public string Circles = "";

        /// <summary>
        /// The color of the side on move in this position.
        /// </summary>
        public PieceColor ColorToMove => Position.ColorToMove;

        /// <summary>
        /// The move leading to this position in algebraic notation (e.g. "d4", "Nbd2", "e8Q")
        /// </summary>
        public string LastMoveAlgebraicNotation
        {
            get { return _lastMoveAlg; }
            set
            {
                _lastMoveAlg = value;
                if (string.IsNullOrEmpty(_lastMoveAlgWithNag))
                {
                    _lastMoveAlgWithNag = value;
                }
            }
        }

        /// <summary>
        /// The move leading to this position in algebraic notation 
        /// inluding NAG (e.g. "d4!", "Nbd2?!", "e8Q+-")
        /// </summary>
        public string LastMoveAlgebraicNotationWithNag
        {
            get { return _lastMoveAlgWithNag; }
            set { _lastMoveAlgWithNag = value; }
        }

        /// <summary>
        /// Adds a nag string to the NAG string for this node.
        /// Updates LastMoveAlgebraicNotationWithNag. 
        /// </summary>
        /// <param name="nag"></param>
        public void AddNag(string nag)
        {
            // double check that this is a nag
            if (nag.Length < 2 || nag[0] != '$')
                return;

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

                // insert space between NAGs so they are ready for writing out without further parsing
                Nags += " " + nag;
            }
        }

        /// <summary>
        /// Sets a new value for the NAGs string
        /// Updates LastMoveAlgebraicNotationWithNag. 
        /// </summary>
        /// <param name="nags"></param>
        public void SetNags(string nags)
        {
            LastMoveAlgebraicNotationWithNag = LastMoveAlgebraicNotation;

            Nags = "";
            if (!string.IsNullOrWhiteSpace(nags))
            {
                string[] tokens = nags.Trim().Split(' ');
                foreach (string token in tokens)
                {
                    AddNag(token);
                }
            }
        }

        /// <summary>
        /// Inserts a new Article Reference
        /// </summary>
        /// <param name="artref"></param>
        public void AddArticleReference(string artref)
        {
            if (!string.IsNullOrEmpty(artref))
            {
                if (!string.IsNullOrEmpty(ArticleRefs))
                {
                    ArticleRefs += "|" + artref;
                }
                else
                {
                    ArticleRefs += artref;
                }
            }
        }

        /// <summary>
        /// Removes the passed Article Reference.
        /// Returns true if the reference was found.
        /// </summary>
        /// <param name="artref"></param>
        /// <returns></returns>
        public bool RemoveArticleReference(string artref)
        {
            if (string.IsNullOrEmpty(ArticleRefs) || string.IsNullOrEmpty(artref))
            {
                return false;
            }

            int pos = ArticleRefs.IndexOf(artref);
            if (pos >= 0)
            {
                ArticleRefs = ArticleRefs.Remove(pos, artref.Length);
                // there may be double separation signs after the removal
                ArticleRefs = ArticleRefs.Replace("||", "|");
                return true;
            }
            else
            {
                return false;
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
        /// Returns true if this node is the first child of its parent
        /// </summary>
        /// <returns></returns>
        public bool IsFirstChild()
        {
            if (Parent == null)
            {
                return true;
            }

            if (Parent.Children[0].NodeId == NodeId)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Default constructor need for serialization.
        /// </summary>
        public TreeNode()
        {
        }

        /// <summary>
        /// Creates a new object as a child of the passed parent.
        /// Initializes the InheritedEnPassant square and the
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
                Position.HalfMove50Clock = parent.Position.HalfMove50Clock;
            }
            NodeId = nodeId;
        }

        /// <summary>
        /// Makes a deep copy of this TreeNode.
        /// NOTE: does not setTreeNode.Parent references.
        /// If clearChildren == false it will also make deep copies of the Children list.
        /// </summary>
        /// <param name="clearChildren"></param>
        public TreeNode CloneMe(bool clearChildren)
        {
            // shallow copy first
            TreeNode clone = this.MemberwiseClone() as TreeNode;
            BoardPosition pos = new BoardPosition(this.Position);
            clone.Position = pos;

            clone.Children = new List<TreeNode>();

            if (!clearChildren)
            {
                foreach (TreeNode child in Children)
                {
                    TreeNode childClone = child.CloneMe(false);
                    childClone.Parent = clone;
                    clone.Children.Add(childClone);
                }
            }

            return clone;
        }

        /// <summary>
        /// Makes a clone of the Node but not of the children.
        /// </summary>
        /// <returns></returns>
        public TreeNode CloneJustMe()
        {
            TreeNode clone = this.MemberwiseClone() as TreeNode;
            BoardPosition pos = new BoardPosition(this.Position);
            clone.Position = pos;
            return clone;
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
        /// Find a child of this node with a position identical
        /// to that in the passed Node.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public TreeNode FindChildIdenticalTo(TreeNode nd)
        {
            TreeNode ret = null;

            string fen = FenParser.GenerateFenFromPosition(nd.Position);

            foreach (TreeNode child in Children)
            {
                if (FenParser.GenerateFenFromPosition(child.Position) == fen)
                {
                    ret = child;
                    break;
                }
            }

            return ret;
        }

        /// <summary>
        /// Localized text for the ply to show without the move number
        /// with check / mate symbol and optionally with NAGs
        /// TODO: move this to some utilities class
        /// </summary>
        /// <returns></returns>
        public string GetGuiPlyText(bool withNags)
        {
            string res;

            if (!Position.IsCheckmate && !Position.IsCheck)
            {
                res = withNags ? LastMoveAlgebraicNotationWithNag : LastMoveAlgebraicNotation;
            }
            else
            {
                if (withNags)
                {
                    res = LastMoveAlgebraicNotation + GetCheckOrMateSign() + GetNagSubstring();
                }
                else
                {
                    res = LastMoveAlgebraicNotation + GetCheckOrMateSign();
                }
            }

            res = Languages.MapPieceSymbols(res, MoveUtils.ReverseColor(Position.ColorToMove));

            return res;
        }

        /// <summary>
        /// Obtains the NAG string if present by comparing the move string with and without the NAGs.
        /// </summary>
        /// <returns></returns>
        private string GetNagSubstring()
        {
            if (LastMoveAlgebraicNotationWithNag == null || LastMoveAlgebraicNotation == null
                || LastMoveAlgebraicNotationWithNag.Length <= LastMoveAlgebraicNotation.Length)
            {
                return "";
            }

            return LastMoveAlgebraicNotationWithNag.Substring(LastMoveAlgebraicNotation.Length);
        }

        /// <summary>
        /// Returns the check.mate/empty sign string appropriate for the position.
        /// </summary>
        /// <returns></returns>
        private string GetCheckOrMateSign()
        {
            if (Position.IsCheckmate)
            {
                return "#";
            }
            else if (Position.IsCheck)
            {
                return "+";
            }
            else
            {
                return "";
            }
        }
    }
}

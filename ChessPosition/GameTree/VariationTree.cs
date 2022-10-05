using System;
using System.Text;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChessPosition;
using System.Xml.Linq;

namespace GameTree
{
    /// <summary>
    /// This object is used to for storing analysis trees, games and combinations. 
    /// </summary>
    public class VariationTree
    {
        /// <summary>
        /// The complete list of Nodes for the current Workbook.
        /// </summary>
        public List<TreeNode> Nodes = new List<TreeNode>();

        /// <summary>
        /// Workbook headers as Name/Value pair.
        /// </summary>
        public Dictionary<string, string> Headers = new Dictionary<string, string>();

        // Headers dictionary keys
        public readonly string HEADER_TRAINING_SIDE = "TrainingSide";
        public readonly string HEADER_TITLE = "Title";
        public readonly string HEADER_WHITE = "White";
        public readonly string HEADER_BLACK = "Black";
        public readonly string HEADER_DATE = "Date";
        public readonly string HEADER_RESULT = "Result";
        public readonly string HEADER_EVENT = "Event";

        // a list of nodes from a subtree
        private List<TreeNode> _subTree = new List<TreeNode>();

        /// <summary>
        /// Title of this Workbook to show in the GUI
        /// </summary>
        public string Title
        {
            get
            {
                string title;
                Headers.TryGetValue(HEADER_TITLE, out title);
                return title ?? "";
            }
            set
            {
                Headers[HEADER_TITLE] = value;
            }
        }

        /// <summary>
        /// Default training side for the Workbook
        /// </summary>
        public PieceColor TrainingSide
        {
            get
            {
                string trainingSide;
                if (Headers.TryGetValue(HEADER_TRAINING_SIDE, out trainingSide))
                {
                    return (trainingSide.Trim().ToLower() == "black" ? PieceColor.Black : PieceColor.White);
                }
                else
                {
                    return PieceColor.None;
                };
            }
            set
            {
                if (value == PieceColor.White)
                {
                    Headers[HEADER_TRAINING_SIDE] = "white";
                }
                else if (value == PieceColor.Black)
                {
                    Headers[HEADER_TRAINING_SIDE] = "black";
                }
                else
                {
                    Headers[HEADER_TRAINING_SIDE] = "none";
                }
            }
        }

        /// <summary>
        /// "Stem" of this tree i.e., the starting moves up until the first fork.
        /// </summary>
        public List<TreeNode> Stem;

        /// <summary>
        /// References to bookmarked psoitions.
        /// </summary>
        public List<Bookmark> Bookmarks = new List<Bookmark>();

        /// <summary>
        /// The complete Workbook flattened into individual lines.
        /// Unlike in the Nodes list, the same moves may be included here multiple
        /// times as they may appear in multiple lines.  Essentially, all moves  
        /// that appear anywhere in the tree before a fork (a node with multiple children)
        /// will be included more than ones.
        /// </summary>
        public ObservableCollection<VariationLine> VariationLines = new ObservableCollection<VariationLine>();

        /// <summary>
        /// Creates a new WorkbookTree.
        /// Creates a root node and inserts in the tree. 
        /// </summary>
        public void CreateNew()
        {
            VariationLines.Clear();
            Nodes.Clear();

            TreeNode root = new TreeNode(null, "", 0);
            root.Position = PositionUtils.SetupStartingPosition();
            AddNode(root);
            BuildLines();
        }

        /// <summary>
        /// Walks the tree and assigns line id to each node
        /// in the form of N.N.N (e.g. "1.2.1.3")
        /// The line with the first child selected at each point 
        /// will have the id of 1.1.(...) , the line with the second child selected
        /// at the second fork and then all first children will have the id of 1.2.1.(...)
        /// </summary>
        public void BuildLines()
        {
            VariationLines.Clear();
            TreeNode root = Nodes[0];
            root.LineId = "1";
            BuildLine(root);
        }

        /// <summary>
        /// Processes a CfhCommand received from the parser.
        /// If possible, applies the command to this node's
        /// properties.
        /// If not, stores in the list of unprocessed commands.
        /// </summary>
        /// <param name="command"></param>
        public void AddChfCommand(TreeNode nd, string command)
        {
            if (!string.IsNullOrEmpty(command))
            {
                string[] tokens = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string cmdPrefix = tokens[0];

                switch (ChfCommands.GetCommand(cmdPrefix))
                {
                    case ChfCommands.Command.BOOKMARK:
                    case ChfCommands.Command.BOOKMARK_V2:
                        AddBookmark(nd);
                        break;
                    case ChfCommands.Command.ENGINE_EVALUATION:
                    case ChfCommands.Command.ENGINE_EVALUATION_V2:
                        if (tokens.Length > 1)
                        {
                            nd.EngineEvaluation = tokens[1];
                        }
                        break;
                    case ChfCommands.Command.ARROWS:
                        if (tokens.Length > 1)
                        {
                            nd.Arrows = tokens[1];
                        }
                        break;
                    case ChfCommands.Command.CIRCLES:
                        if (tokens.Length > 1)
                        {
                            nd.Circles = tokens[1];
                        }
                        break;
                    default:
                        nd.AddUnprocessedChfCommand(command);
                        break;
                }
            }
        }


        /// <summary>
        /// If there are no bookmarks, we will generate some
        /// for the user.
        /// In the bookmarked position, the training side
        /// should be on move.
        /// 
        /// Hence, we look for forks on the moves by the side
        /// opposite to the side doing the training.
        /// If the fork is for the training side, the parent
        /// may be a good candidate for bookmarking.
        /// </summary>
        public void GenerateBookmarks()
        {
            if (Nodes.Count == 0)
                return;

            int MAX_BOOKMARKS = 9;

            // find the first, highest level, fork
            TreeNode fork = FindNextFork(Nodes[0]);
            if (fork == null)
            {
                return;
            }

            // bookmark children of the first fork
            if (fork.ColorToMove != TrainingSide)
            {
                BookmarkChildren(fork, MAX_BOOKMARKS);
            }
            else if (fork.Parent != null && fork.Parent.NodeId != 0)
            {
                BookmarkChildren(fork.Parent, MAX_BOOKMARKS);
            }

            // look for the next fork in each child
            foreach (TreeNode nd in fork.Children)
            {
                TreeNode nextFork = FindNextFork(nd);
                if (nextFork != null)
                {
                    if (nextFork.ColorToMove != TrainingSide)
                    {
                        BookmarkChildren(nextFork, MAX_BOOKMARKS);
                    }
                    else
                    {
                        BookmarkChildren(nextFork.Parent, MAX_BOOKMARKS);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the list of Nodes from the starting position to the
        /// last position before the fork.
        /// </summary>
        /// <returns></returns>
        public List<TreeNode> BuildStem()
        {
            Stem = new List<TreeNode>();

            foreach (TreeNode nd in Nodes)
            {
                if (nd.Children.Count > 1)
                {
                    break;
                }
                else
                {
                    Stem.Add(nd);
                }
            }

            return Stem;
        }

        /// <summary>
        /// Removes all nodes that follow the passed
        /// node.
        /// </summary>
        /// <param name="nd"></param>
        public void RemoveTailAfter(TreeNode nd)
        {
            for (int i = nd.Children.Count - 1; i >= 0; i--)
            {
                Nodes.Remove(nd.Children[i]);
                RemoveTailAfter(nd.Children[i]);
                nd.Children.Remove(nd.Children[i]);
            }
        }

        /// <summary>
        /// Adds a bookmark to the list
        /// and sets the Bookmark flag on the
        /// bookmark's TreeNode.
        /// </summary>
        /// <param name="nd"></param>
        public int AddBookmark(TreeNode nd, bool inFront = false)
        {
            if (FindBookmarkIndex(nd) == -1)
            {
                if (inFront)
                {
                    Bookmarks.Insert(0, new Bookmark(nd));
                }
                else
                {
                    Bookmarks.Add(new Bookmark(nd));
                }
                nd.IsBookmark = true;
                return 0;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Returns true if the passed NodeId
        /// has already been bookmarked.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public bool IsBookmarked(int nodeId)
        {
            return FindBookmarkIndex(nodeId) >= 0;
        }

        /// <summary>
        /// Deletes a bookmark from the list of bookmarks
        /// and removes the bookmark flag from the node.
        /// </summary>
        /// <param name="nd"></param>
        public void DeleteBookmark(TreeNode nd)
        {
            nd.IsBookmark = false;
            for (int i = 0; i < Bookmarks.Count; i++)
            {
                if (Bookmarks[i].Node.NodeId == nd.NodeId)
                {
                    Bookmarks.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Removes a node from the list of bookmarks. 
        /// </summary>
        /// <param name="nd"></param>
        public void RemoveBookmark(TreeNode nd)
        {
            if (nd == null) return;

            nd.IsBookmark = false;
            int idx = FindBookmarkIndex(nd);
            if (idx >= 0)
            {
                Bookmarks.RemoveAt(idx);
            }
        }

        /// <summary>
        /// Clears the list of bookmarks.
        /// </summary>
        public void RemoveAllBookmarks()
        {
            foreach (Bookmark bookmark in Bookmarks)
            {
                bookmark.Node.IsBookmark = false;
            }
            Bookmarks.Clear();
        }

        /// <summary>
        /// Finds index of a passed node in the 
        /// Bookmarks list.
        /// </summary>
        /// <param name="nd">Index in the bookmark list or -1 if not found</param>
        /// <returns></returns>
        public int FindBookmarkIndex(TreeNode nd)
        {
            int idx = -1;
            int nodeId = nd.NodeId;

            for (int i = 0; i < Bookmarks.Count; i++)
            {
                if (Bookmarks[i].Node.NodeId == nodeId)
                {
                    idx = i;
                    break;
                }
            }

            return idx;
        }

        /// <summary>
        /// Finds index of a passed Node Id in the 
        /// Bookmarks list.
        /// </summary>
        /// <param name="nodeId">Index in the bookmark list or -1 if not found</param>
        /// <returns></returns>
        public int FindBookmarkIndex(int nodeId)
        {
            int idx = -1;

            for (int i = 0; i < Bookmarks.Count; i++)
            {
                if (Bookmarks[i].Node.NodeId == nodeId)
                {
                    idx = i;
                    break;
                }
            }

            return idx;
        }

        /// <summary>
        /// Returns a new NodeId that can be used by the caller in a newly
        /// created Node.  
        /// This is the id of the node currently last in the list of nodes
        /// incremented by one.
        /// </summary>
        /// <returns></returns>
        public int GetNewNodeId()
        {
            return Nodes[Nodes.Count - 1].NodeId + 1;
        }

        /// <summary>
        /// Adds a node to the Workbook tree.
        /// </summary>
        /// <param name="node"></param>
        public void AddNode(TreeNode node)
        {
            Nodes.Add(node);
        }

        /// <summary>
        /// Inserts an external subtree into this tree.
        /// Recursively clones the nodes from the external tree,
        /// assigns NodeIds, sets parent object, adds to the parent's
        /// list of children and finally adds to this Tree.
        /// </summary>
        /// <param name="nodeToInsertAt"></param>
        /// <param name="extSubtreeRoot"></param>
        public void InsertSubtree(TreeNode nodeToInsertAt, TreeNode extSubtreeRoot)
        {
            for (int i = 0; i < extSubtreeRoot.Children.Count; i++)
            {
                TreeNode subtreeNode = extSubtreeRoot.Children[i];

                TreeNode newNode = subtreeNode.CloneMe(true);
                newNode.NodeId = GetNewNodeId();
                newNode.Parent = nodeToInsertAt;
                nodeToInsertAt.AddChild(newNode);
                AddNode(newNode);

                InsertSubtree(newNode, subtreeNode);
            }
        }

        /// <summary>
        /// Creates a new child node for the passed node.
        /// </summary>
        /// <returns></returns>
        public TreeNode CreateNewChildNode(TreeNode nd)
        {
            BoardPosition pos = new BoardPosition(nd.Position);
            TreeNode newChild = new TreeNode(nd, "", GetNewNodeId());

            // preserve InheritedEnPassent and Dynamic Properities
            pos.InheritedEnPassantSquare = newChild.Position.InheritedEnPassantSquare;
            pos.DynamicProperties = newChild.Position.DynamicProperties;
            pos.EnPassantSquare = 0;

            newChild.Position = pos;

            return newChild;
        }

        /// <summary>
        /// Adds a new Node to the Workbook
        /// and to its parent node 
        /// </summary>
        /// <param name="node"></param>
        public void AddNodeToParent(TreeNode node)
        {
            Nodes.Add(node);
            node.Parent.AddChild(node);
        }

        /// <summary>
        /// Returns a sibling move of found to represent the same
        /// move as the passed node.
        /// Otherwise returns null.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public TreeNode GetIdenticalSibling(TreeNode nd, string engMove = null)
        {
            TreeNode ret = null;

            string engNotation = engMove == null ? nd.LastMoveEngineNotation : engMove;

            if (nd.Parent != null)
            {
                foreach (TreeNode sib in nd.Parent.Children)
                {
                    if (sib.LastMoveEngineNotation == engNotation)
                    {
                        ret = sib;
                        break;
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// Returns true if the specified node has at least one sibling.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public bool NodeHasSiblings(int nodeId)
        {
            bool ret;
            TreeNode nd = GetNodeFromNodeId(nodeId);
            if (nd != null && nd.Parent != null && nd.Parent.Children.Count > 1)
            {
                ret = true;
            }
            else
            {
                ret = false;
            }

            return ret;
        }

        /// <summary>
        /// Checks if the node already exists in the tree 
        /// by checking if it has the same position as one of the children.
        /// If so returns the already exisiting. Otherwise, returns null 
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public TreeNode FindExistingNode(TreeNode nd)
        {
            if (nd.Parent == null)
                return null;

            foreach (TreeNode child in nd.Parent.Children)
            {
                if (ArePositionsIdentical(child.Position, nd.Position))
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true if the two passed positions are identical.
        /// Note that this means "identical" in both static and dynamic terms
        /// as encapsulated in the FEN encoding.
        /// </summary>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        /// <returns></returns>
        public bool ArePositionsIdentical(BoardPosition pos1, BoardPosition pos2)
        {
            if (pos1 == null || pos2 == null)
                return false;

            return FenParser.GenerateFenFromPosition(pos1) == FenParser.GenerateFenFromPosition(pos2);
        }

        /// <summary>
        /// Checks if there are any moves marked
        /// as training in the tree.
        /// </summary>
        /// <returns></returns>
        public bool HasTrainingMoves()
        {
            foreach (TreeNode nd in Nodes)
            {
                if (nd.IsNewTrainingMove)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if there are any moves with
        /// engine evaluations in the tree.
        /// </summary>
        /// <returns></returns>
        public bool HasMovesWithEvaluations()
        {
            foreach (TreeNode nd in Nodes)
            {
                if (!string.IsNullOrWhiteSpace(nd.EngineEvaluation))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Clears any training flags found.
        /// </summary>
        public void ClearTrainingFlags()
        {
            foreach (TreeNode nd in Nodes)
            {
                nd.IsNewTrainingMove = false;
            }
        }

        /// <summary>
        /// Removes all training moves from the tree
        /// </summary>
        public void RemoveTrainingMoves()
        {
            // identify the nodes first and remove them
            // from parents'  children lists
            List<TreeNode> nodesToRemove = new List<TreeNode>();
            foreach (TreeNode nd in Nodes)
            {
                if (nd.IsNewTrainingMove)
                {
                    nodesToRemove.Add(nd);
                    if (nd.Parent != null)
                    {
                        nd.Parent.Children.Remove(nd);
                    }
                }
            }

            foreach (TreeNode nd in nodesToRemove)
            {
                Nodes.Remove(nd);
            }
        }

        /// <summary>
        /// Clears all engine evaluations.
        /// </summary>
        public void ClearEngineEvaluations()
        {
            foreach (TreeNode nd in Nodes)
            {
                nd.EngineEvaluation = null;
            }
        }

        /// <summary>
        /// Checks if there are any bookmarks
        /// as training in the tree.
        /// </summary>
        /// <returns></returns>
        public bool HasBookmarks()
        {
            return Bookmarks.Count > 0;
        }

        /// <summary>
        /// Returns a line identified by the passed prefix.
        /// First add all moves that have prefix that begins
        /// with the passed prefix.
        /// Then all moves whose LineId begins with this prefix
        /// and then only contain 1's and dots will be 
        /// selected.
        /// </summary>
        /// <param name="lineId"></param>
        /// <returns></returns>
        public ObservableCollection<TreeNode> SelectLine(string lineId)
        {
            var singleLine = new ObservableCollection<TreeNode>();

            foreach (TreeNode nd in Nodes)
            {
                if (lineId.StartsWith(nd.LineId))
                {
                    singleLine.Add(nd);
                }
                else if (nd.LineId.StartsWith(lineId))
                {
                    string rem = nd.LineId.Substring(lineId.Length);
                    bool include = true;
                    for (int i = 0; i < rem.Length; i++)
                    {
                        if (i % 2 == 0)
                        {
                            if (rem[i] != '.')
                            {
                                include = false;
                                break;
                            }
                        }
                        else
                        {
                            if (rem[i] != '1')
                            {
                                include = false;
                                break;
                            }
                        }
                    }
                    if (include)
                    {
                        singleLine.Add(nd);
                    }
                }
            }

            return singleLine;
        }

        /// <summary>
        /// Gets the default line id for the node.
        /// The "default" line id is the one in the last
        /// node (leaf) of a line as it uniquely identifies 
        /// the line.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public string GetDefaultLineIdForNode(int nodeId)
        {
            TreeNode nd = GetNodeFromNodeId(nodeId);
            // go to the last node
            while (nd.Children.Count > 0)
            {
                nd = nd.Children[0];
            }

            return nd.LineId ?? "";
        }

        /// <summary>
        /// Returns the TreeNode with a given id.
        /// Returns null if node not found.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public TreeNode GetNodeFromNodeId(int nodeId)
        {
            return Nodes.FirstOrDefault(x => x.NodeId == nodeId);
        }

        /// <summary>
        /// Selects random child of a given parent.
        /// This is needed to randomize training.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public TreeNode SelectRandomChild(int nodeId)
        {
            TreeNode par = GetNodeFromNodeId(nodeId);
            int sel = PositionUtils.GlobalRnd.Next(0, par.Children.Count);
            return par.Children[sel];
        }

        /// <summary>
        /// Each invocation of this method builds a Line for 
        /// the flattened view of the Workbook.
        /// The method calls itself recursively to build
        /// the complete set of lines.
        /// </summary>
        /// <param name="nd"></param>
        public void BuildLine(TreeNode nd)
        {
            if (nd.Children.Count == 0)
            {
                // this is the end of the line
                // so create a line and add to the
                // list of lines
                List<VariationLine> vls = VariationLine.BuildVariationLine(nd);
                VariationLines.Add(vls[0]);
                VariationLines.Add(vls[1]);
                return;
            }

            if (nd.Children.Count == 1)
            {
                nd.Children[0].LineId = nd.LineId;
                BuildLine(nd.Children[0]);
            }
            else if (nd.Children.Count > 1)
            {
                for (int i = 0; i < nd.Children.Count; i++)
                {
                    nd.Children[i].LineId = nd.LineId + "." + (i + 1).ToString();
                    BuildLine(nd.Children[i]);
                }
            }
        }

        /// <summary>
        /// A new node has been created; rebuild the lines
        /// from the parent.
        /// </summary>
        /// <param name="nd"></param>
        public void SetLineIdForNewNode(TreeNode nd)
        {
            BuildLine(nd.Parent);
        }

        /// <summary>
        /// Returns the first move of the main line i.e. the move
        /// we want selected when we open a workbook.
        /// The first move can be identified as the one that has its
        /// LineId=="1" (if no fork on the very first move) or LineId=="1.1".
        /// Alternatively, we could just return the first child of the root node.
        /// </summary>
        /// <returns></returns>
        public TreeNode GetFirstNodeInMainLine()
        {
            foreach (TreeNode child in Nodes[0].Children)
            {
                if (child.LineId == "1" || child.LineId == "1.1")
                    return child;
            }

            return null;
        }

        /// <summary>
        /// Promotes a line by moving it one level up.
        /// Starting from the passed node, this methods walks
        /// up the branch until it encounters a fork where this
        /// line is not a child 0. Then it moves up to the 0 position
        /// and calls a rebuild of the tree.
        /// The caller is then responsible for rebuilding the GUIs.
        /// </summary>
        /// <param name="nd"></param>
        public bool PromoteLine(TreeNode nd)
        {
            bool changed = false;

            if (nd == null || nd.IsMainLine())
            {
                return changed;
            }

            TreeNode currNode = nd;
            while (currNode.Parent != null)
            {
                if (currNode.Parent.Children[0].NodeId != currNode.NodeId)
                {
                    bool onthemove = false;
                    for (int i = currNode.Parent.Children.Count - 1; i > 0; i--)
                    {
                        if (onthemove || currNode.Parent.Children[i].NodeId == currNode.NodeId)
                        {
                            currNode.Parent.Children[i] = currNode.Parent.Children[i - 1];
                            onthemove = true;
                        }
                    }
                    changed = true;
                    currNode.Parent.Children[0] = currNode;
                    break;
                }
                currNode = currNode.Parent;
            }

            if (changed)
            {
                BuildLines();
            }

            return changed;
        }

        /// <summary>
        /// Deletes the passed node and all of its subtree.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public bool DeleteRemainingMoves(TreeNode nd)
        {
            // identify moves to delete
            _subTree.Clear();
            nd.Parent.Children.Remove(nd);

            GetSubTree(nd);

            foreach (TreeNode node in _subTree)
            {
                Nodes.Remove(node);
                DeleteBookmark(node);
            }

            return _subTree.Count > 0;
        }

        /// <summary>
        /// Identifies all nodes of a subtree with the root
        /// at the passed Node. Saves them in the
        /// _subTree list.
        /// </summary>
        /// <param name="nd"></param>
        private void GetSubTree(TreeNode nd)
        {
            _subTree.Add(nd);
            if (nd.Children.Count == 0)
            {
                return;
            }
            else
            {
                for (int i = 0; i < nd.Children.Count; i++)
                {
                    GetSubTree(nd.Children[i]);
                }
            }
        }

        /// <summary>
        /// Adds children of a node to the list of Bookmarks.
        /// </summary>
        /// <param name="fork">Node whose children to bookmark.</param>
        /// <param name="maxCount">Max allowed number of bookmarked positions.</param>
        private void BookmarkChildren(TreeNode fork, int maxCount)
        {
            if (fork == null)
                return;

            foreach (TreeNode nd in fork.Children)
            {
                if (Bookmarks.Count >= maxCount)
                {
                    break;
                }
                AddBookmark(nd);
            }
        }

        /// <summary>
        /// Find next fork after the given node.
        /// </summary>
        /// <param name="fromNode"></param>
        /// <returns></returns>
        private TreeNode FindNextFork(TreeNode fromNode)
        {
            TreeNode nd = fromNode;

            while (nd.Children.Count != 0)
            {
                if (nd.Children.Count > 1)
                {
                    return nd;
                }
                nd = nd.Children[0];
            }

            return null;
        }

    }
}


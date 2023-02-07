using System;
using System.Text;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChessPosition;
using System.Xml.Linq;
using ChessPosition.GameTree;
using ChessForge;
using System.Windows.Controls;

namespace GameTree
{
    /// <summary>
    /// This object stores study trees, games and exercises. 
    /// </summary>
    public class VariationTree
    {
        /// <summary>
        /// A secondary tree associated with this tree. 
        /// For example, it will be a Tree with user's solution while
        /// this Tree holds the Exercise.
        /// </summary>
        public VariationTree AssociatedSecondary { get; set; }

        /// <summary>
        /// The Primary team in an association where this tree is secondary.
        /// </summary>
        public VariationTree AssociatedPrimary { get; set; }

        /// <summary>
        /// Tree Id assigned uniquely assigned for the current session only.
        /// </summary>
        public int TreeId { get; set; }

        /// <summary>
        /// Available Exercise Solving modes.
        /// </summary>
        public enum SolvingMode
        {
            // No solving in progress. Lines are hidden. 
            NONE,
            // No solving in progress. Lines are shown and can be edited.
            EDITING,
            // "Guess move" solving in progress
            GUESS_MOVE,
            // "Analysis" solving in progress.
            ANALYSIS
        }

        /// <summary>
        /// Indicates whether the Associated Tree is active. 
        /// </summary>
        public bool IsAssociatedTreeActive
        {
            get => _isAssociatedTreeActive;
            set => _isAssociatedTreeActive = value;
        }

        // associated OperationsManager
        public EditOperationsManager OpsManager;

        /// <summary>
        /// If the user changed the board orientation and it is not the same
        /// as Workbook settings then store it here.
        /// Otherwise the value should be NONE.
        /// It is responsibiliy of the caller to maintain that logic
        /// as this object does not know app settings.
        /// </summary>
        public PieceColor CustomBoardOrientation
        {
            get => _customBoardOrientation;
            set => _customBoardOrientation = value;
        }

        // whether the Associated Tree is active
        private bool _isAssociatedTreeActive = false;

        // currently the highest NodeId in the tree (can be not set so NodeId of the last node must ne checked too)
        private int _maxNodeId = 0;

        // board orientation if not per Workbook settings
        private PieceColor _customBoardOrientation = PieceColor.None;

        /// <summary>
        /// Constructor. Creates a VariationTree of the requested type.
        /// </summary>
        /// <param name="contentType"></param>
        public VariationTree(GameData.ContentType contentType, TreeNode root = null)
        {
            TreeId = TreeManager.GetNewTreeId();
            OpsManager = new EditOperationsManager(this);
            Header.SetContentType(contentType);

            // create GUID (it will be overwritten when parsing entities from a file)
            Header.SetHeaderValue(PgnHeaders.KEY_GUID, Guid.NewGuid().ToString());

            if (contentType == GameData.ContentType.EXERCISE)
            {
                ShowTreeLines = false;
            }
            else
            {
                ShowTreeLines = true;
            }

            if (root != null)
            {
                Nodes.Add(root);
            }
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
        /// Sets a new thumbnail for the tree.
        /// Clears the previous thumbnail.
        /// </summary>
        /// <param name="nd"></param>
        public void SetThumbnail(TreeNode nd)
        {
            foreach (TreeNode node in Nodes)
            {
                if (nd.NodeId == node.NodeId)
                {
                    node.IsThumbnail = true;
                }
                else
                {
                    node.IsThumbnail = false;
                }
            }
        }

        /// <summary>
        /// Returns the Thumbnail node if marked.
        /// </summary>
        /// <returns></returns>
        public TreeNode GetThumbnail()
        {
            foreach (TreeNode nd in Nodes)
            {
                if (nd.IsThumbnail)
                {
                    return nd;
                }
            }

            return null;
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
        /// Accessor for the root node.
        /// </summary>
        public TreeNode RootNode
        {
            get
            {
                if (Nodes.Count > 0)
                {
                    return Nodes[0];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Returns the last position of the tree i.e. the leaf of the main line.
        /// </summary>
        /// <returns></returns>
        public TreeNode GetFinalPosition()
        {
            if (RootNode == null)
            {
                return null;
            }

            TreeNode lastNode = RootNode;
            while (lastNode.Children.Count > 0)
            {
                lastNode = lastNode.Children[0];
            }

            return lastNode;
        }

        /// <summary>
        /// Accessors to the ContentType value kept
        /// in the Header.
        /// </summary>
        public GameData.ContentType ContentType
        {
            get
            {
                return Header.GetContentType(out _);
            }
            set
            {
                Header.SetContentType(value);
                ShowTreeLines = (value != GameData.ContentType.EXERCISE);
            }
        }

        /// <summary>
        /// The complete list of Nodes for the current Workbook.
        /// </summary>
        public List<TreeNode> Nodes = new List<TreeNode>();

        /// <summary>
        /// Header lines of the game/tree
        /// </summary>
        public GameHeader Header = new GameHeader();

        /// <summary>
        /// The solving mode that the view for this tree is currently in.
        /// </summary>
        public SolvingMode CurrentSolvingMode
        {
            get => _solvingMode;
            set => _solvingMode = value;
        }

        // current solving mode
        private SolvingMode _solvingMode = SolvingMode.NONE;

        // a list of nodes from a subtree
        private List<TreeNode> _subTree = new List<TreeNode>();

        // selected Line Id
        private string _selectedLineId = "";

        // selected Node Id
        private int _selectedNodeId = -1;

        /// <summary>
        /// Selected Line Id as set from the interface. 
        /// </summary>
        public string SelectedLineId
        {
            get => _selectedLineId;
            set => _selectedLineId = value;
        }

        /// <summary>
        /// Selected Node Id as set from the interface. 
        /// </summary>
        public int SelectedNodeId
        {
            get => _selectedNodeId;
            set => _selectedNodeId = value;
        }

        /// <summary>
        /// Selected node.
        /// </summary>
        public TreeNode SelectedNode
        {
            get
            {
                return GetNodeFromNodeId(SelectedNodeId);
            }
        }

        // whethet to show the tree lines in the hosting view
        private bool _showTreeLines = true;

        /// <summary>
        /// Determines whether to show the tree lines in the hosting view.
        /// This will apply e.g. when we are showing the exercise and the user
        /// can click a button to show or hide the solution,
        /// </summary>
        public bool ShowTreeLines
        {
            get { return _showTreeLines; }
            set { _showTreeLines = value; }
        }

        /// <summary>
        /// Sets the line and mode selected in the GUI.
        /// This is to persist the state while the user switches between views.
        /// </summary>
        /// <param name="lineId"></param>
        /// <param name="nodeId"></param>
        public void SetSelectedLineAndMove(string lineId, int nodeId)
        {
            _selectedLineId = lineId;
            _selectedNodeId = nodeId;
        }

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
        /// Creates a new Tree with the root node at the starting position. 
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
        /// Creates a new Tree given the list of Nodes.
        /// </summary>
        /// <param name="nodes"></param>
        public void CreateNew(List<TreeNode> nodes)
        {
            VariationLines.Clear();
            Nodes = nodes;

            BuildLines();
        }

        /// <summary>
        /// Creates a new Tree starting from the passed position
        /// </summary>
        /// <param name="position"></param>
        public void CreateNew(BoardPosition position)
        {
            VariationLines.Clear();
            Nodes.Clear();

            TreeNode root = new TreeNode(null, "", 0);
            root.Position = new BoardPosition(position);
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
                    case ChfCommands.Command.THUMBNAIL:
                        nd.IsThumbnail = true;
                        break;
                    case ChfCommands.Command.ARTICLE_REFS:
                        if (tokens.Length > 1)
                        {
                            nd.ArticleRefs = tokens[1];
                        }
                        break;
                    case ChfCommands.Command.ENGINE_EVALUATION:
                    case ChfCommands.Command.ENGINE_EVALUATION_V2:
                        if (tokens.Length > 1)
                        {
                            nd.EngineEvaluation = tokens[1];
                        }
                        break;
                    case ChfCommands.Command.QUIZ_POINTS:
                        if (tokens.Length > 1)
                        {
                            if (int.TryParse(tokens[1], out int pts))
                            {
                                nd.QuizPoints = pts;
                            }
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
        public void GenerateBookmarks(PieceColor _trainingSide)
        {
            if (Nodes.Count == 0)
                return;

            int MAX_BOOKMARKS = 9;

            // find the first, highest level, fork
            TreeNode fork = FindNextFork(Nodes[0]);
            if (fork != null)
            {
                // bookmark children of the first fork
                if (fork.ColorToMove != _trainingSide)
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
                        if (nextFork.ColorToMove != _trainingSide)
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

            if (Bookmarks.Count == 0)
            {
                BookmarkAnything(_trainingSide);
            }
        }

        /// <summary>
        /// Returns the list of Nodes from the starting position to the
        /// last node before the passed node or to the last position before the first fork  
        /// if the passed node is null.
        /// </summary>
        /// <returns></returns>
        public List<TreeNode> BuildStem()
        {
            List<TreeNode> stem = new List<TreeNode>();
            foreach (TreeNode nd in Nodes)
            {
                if (nd.Children.Count > 1)
                {
                    break;
                }
                else
                {
                    stem.Add(nd);
                }
            }

            return stem;
        }

        /// <summary>
        /// Builds a pseudo-stem i.e. the single line
        /// leading from node 0 to the passed node.
        /// </summary>
        /// <param name="ndLast"></param>
        /// <returns></returns>
        public List<TreeNode> BuildPseudoStem(TreeNode ndLast)
        {
            List<TreeNode> stem = new List<TreeNode>();

            TreeNode origNode = ndLast;
            // make shallow copies down to Node 0
            while (ndLast.Parent != null)
            {
                TreeNode ndToInsert = ndLast.Parent.CloneMe(true);
                ndToInsert.Children.Clear();
                stem.Insert(0, ndToInsert);
                ndLast = ndLast.Parent;
            }

            // set children and parents
            for (int i = 0; i < stem.Count; i++)
            {
                if (i == stem.Count - 1)
                {
                    stem[i].AddChild(origNode);
                }
                else
                {
                    stem[i].AddChild(stem[i + 1]);
                }

                if (i == 0)
                {
                    stem[i].Parent = null;
                }
                else
                {
                    stem[i].Parent = stem[i - 1];
                }
            }

            return stem;
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
        public Bookmark AddBookmark(TreeNode nd, bool inFront = false)
        {
            Bookmark bm = null;
            if (FindBookmarkIndex(nd) == -1)
            {
                bm = new Bookmark(nd);
                if (inFront)
                {
                    Bookmarks.Insert(0, bm);
                }
                else
                {
                    Bookmarks.Add(bm);
                }
                nd.IsBookmark = true;
            }
            return bm;
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
        /// Sets Article Refererences on the Node.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="refGuids"></param>
        public void SetArticleRefs(TreeNode nd, List<String> refGuids)
        {
            TreeNode node = GetNodeFromNodeId(nd.NodeId);
            node.ArticleRefs = string.Empty;
            foreach (string s in refGuids)
            {
                node.AddArticleReference(s);
            }
        }

        /// <summary>
        /// Removes all reference to the passed guid in this Tree.
        /// Returns the number of affected nodes and adds them to the passed list.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="affectedNodes"></param>
        /// <returns></returns>
        public int RemoveArticleReferences(string guid, ref List<FullNodeId> affectedNodes)
        {
            int count = 0;

            foreach (TreeNode nd in Nodes)
            {
                if (nd.RemoveArticleReference(guid))
                {
                    count++;
                    affectedNodes.Add(new FullNodeId(TreeId, nd.NodeId));
                }
            }

            return count;
        }

        /// <summary>
        /// Returns a new NodeId that can be used by the caller in a newly
        /// created Node.  
        /// This should be the value of _maxNodeId incremented by one
        /// but it may not be set so needs to be checked against the node currently last in the list of nodes
        /// incremented by one or.
        /// </summary>
        /// <returns></returns>
        public int GetNewNodeId()
        {
            int currentMax = Math.Max(_maxNodeId, Nodes[Nodes.Count - 1].NodeId);
            _maxNodeId = currentMax + 1;
            return _maxNodeId;
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
        /// Gets the index of the node in its parent's children list
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public int GetChildIndex(TreeNode nd)
        {
            if (nd.Parent == null)
            {
                return -1;
            }

            int idx = -1;
            for (int i = 0; i < nd.Parent.Children.Count; i++)
            {
                if (nd.NodeId == nd.Parent.Children[i].NodeId)
                {
                    idx = i;
                    break;
                }
            }

            return idx;
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
        /// Checks if the node already exists in the tree. 
        /// It looks for a node with identical position
        /// optionally checking the move number as well.
        /// If so returns the already exisiting. Otherwise, returns null 
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public TreeNode FindIdenticalNode(TreeNode nd, bool compareMoveNumber)
        {
            TreeNode ret = null;

            foreach (TreeNode node in Nodes)
            {
                if (!compareMoveNumber || nd.MoveNumber == node.MoveNumber)
                {
                    if (ArePositionsIdentical(node.Position, nd.Position))
                    {
                        ret = node;
                        break;
                    }
                }
            }

            return ret;
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
            if (nd == null)
            {
                return "";
            }

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
        /// Selects the next child of the given node
        /// based on the last stored index.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public TreeNode SelectNextChild(int nodeId)
        {
            TreeNode nd = GetNodeFromNodeId(nodeId);
            int childCount = nd.Children.Count;
            if (childCount == 0)
            {
                return null;
            }
            else
            {
                nd.SelectedChildIndex++;
                if (nd.SelectedChildIndex > childCount - 1)
                {
                    nd.SelectedChildIndex = 0;
                }
                return nd.Children[nd.SelectedChildIndex];
            }
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

            int childIndex = GetChildIndex(nd);
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

            // Store info about this promotion for a possible undo
            EditOperation op = new EditOperation(EditOperation.EditType.PROMOTE_LINE, nd, childIndex);
            OpsManager.PushOperation(op);

            if (changed)
            {
                BuildLines();
            }

            return changed;
        }

        /// <summary>
        /// Undoes promotion of a line.
        /// Moves the root of promotion to its original position
        /// on the parent's child list.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="origIndex"></param>
        public void UndoPromoteLine(TreeNode nd, int origIndex)
        {
            if (nd != null && nd.Parent != null && origIndex >= 0 && origIndex < nd.Parent.GetChildrenCount())
            {
                nd.Parent.Children.Remove(nd);
                nd.Parent.Children.Insert(origIndex, nd);
            }
        }

        /// <summary>
        /// Deletes the passed node and all of its subtree.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public bool DeleteRemainingMoves(TreeNode nd)
        {
            // need child index for unod
            int childIndex = GetChildIndex(nd);

            // identify moves to delete
            _subTree.Clear();
            nd.Parent.Children.Remove(nd);
            GetSubTree(nd);

            // Store info about this deletion for a possible undo
            EditOperation op = new EditOperation(EditOperation.EditType.DELETE_LINE, nd, CopySubtree(_subTree), childIndex);
            OpsManager.PushOperation(op);

            foreach (TreeNode node in _subTree)
            {
                Nodes.Remove(node);
                DeleteBookmark(node);
            }

            return _subTree.Count > 0;
        }

        /// <summary>
        /// Restore the subtree that was removed e.g. by the DeleteRemainingMoves() call.
        /// Inserts the start node at its original index and then simply adds all other
        /// nodes to the tree as all parent and children references will be good there.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="nodeList"></param>
        /// <param name="childIndex"></param>
        public void UndoDeleteSubtree(TreeNode start, List<TreeNode> nodeList, int childIndex)
        {
            try
            {
                start.Parent.Children.Insert(childIndex, start);
                if (nodeList != null)
                {
                    foreach (TreeNode nd in nodeList)
                    {
                        Nodes.Add(nd);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Restores annotation from before the last edit.
        /// </summary>
        /// <param name="dummyNode"></param>
        public void UndoUpdateAnnotation(TreeNode dummyNode)
        {
            try
            {
                TreeNode nd = GetNodeFromNodeId(dummyNode.NodeId);
                nd.SetNags(dummyNode.Nags);
                nd.Comment = dummyNode.Comment;
                nd.QuizPoints = dummyNode.QuizPoints;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Undoes the merge of trees by removing all added nodes.
        /// The passed argument is the list of of nodes that the 
        /// original tree had.
        /// We remove all added nodes with children and from the parent's list
        /// </summary>
        /// <param name="opData"></param>
        public void UndoAddedNodeList(object opData)
        {
            try
            {
                List<int> nodeIds = opData as List<int>;
                RemoveNodesFromTree(nodeIds);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Removes the passed node from the Tree.
        /// In case of some issues with the Undo system, only removes
        /// the node if there are no children.
        /// </summary>
        /// <param name="nd"></param>
        public void UndoAddMove(TreeNode nd)
        {
            try
            {
                if (nd != null && nd.Parent != null && nd.GetChildrenCount() == 0)
                {
                    nd.Parent.Children.Remove(nd);
                    Nodes.Remove(nd);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Builds a list of Nodes belonging to a subtree
        /// identified by the passed node.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeStem"></param>
        /// <returns></returns>
        public List<TreeNode> BuildSubTreeNodeList(TreeNode nd, bool includeStem = false)
        {
            _subTree.Clear();
            return GetSubTree(nd, includeStem);
        }

        /// <summary>
        /// Returns a list of NodeIds.
        /// This will be used e.g. when undoing tree merge.
        /// </summary>
        /// <returns></returns>
        public List<int> GetListOfNodeIds(bool includeTrainingMoves)
        {
            List<int> nodeIds = new List<int>();
            foreach (TreeNode nd in Nodes)
            {
                if (includeTrainingMoves || !nd.IsNewTrainingMove)
                {
                    nodeIds.Add(nd.NodeId);
                }
            }

            return nodeIds;
        }

        /// <summary>
        /// Removes nodes with the passed Ids from the tree.
        /// </summary>
        /// <param name="nodeIds"></param>
        private void RemoveNodesFromTree(List<int> nodeIds)
        {
            try
            {
                // loop until no node to delete found
                while (true)
                {
                    bool found = false;
                    foreach (TreeNode nd in Nodes)
                    {
                        // 0 (default) returned can be ignored because that would be the root node
                        // that we always want to keep.
                        if (nodeIds.Find(x => x == nd.NodeId) == 0 && nd.NodeId != 0)
                        {
                            RemoveTailAfter(nd);
                            nd.Parent.Children.Remove(nd);
                            Nodes.Remove(nd);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        break;
                    }
                }
            }
            catch
            {
            }
        }


        /// <summary>
        /// Makes a copy of a list of nodes for later use.
        /// </summary>
        /// <param name="subTree"></param>
        /// <returns></returns>
        private List<TreeNode> CopySubtree(List<TreeNode> subTree)
        {
            List<TreeNode> copy = new List<TreeNode>();
            foreach (TreeNode node in subTree)
            {
                copy.Add(node);
            }

            return copy;
        }

        /// <summary>
        /// Identifies all nodes of a subtree with the root
        /// at the passed Node. Saves them in the
        /// _subTree list.
        /// </summary>
        /// <param name="nd"></param>
        private List<TreeNode> GetSubTree(TreeNode nd, bool includeStem = false)
        {
            _subTree.Add(nd);
            if (nd.Children.Count == 0)
            {
                return _subTree;
            }
            else
            {
                for (int i = 0; i < nd.Children.Count; i++)
                {
                    GetSubTree(nd.Children[i]);
                }
            }

            if (includeStem)
            {
                List<TreeNode> stem = BuildPseudoStem(nd);
                _subTree.InsertRange(0, stem);
            }

            return _subTree;
        }

        /// <summary>
        /// Bookmarks the last Node that fits the Training Side
        /// or the root node if nothing found.
        /// Called when user called Generate Bookmarks if there is
        /// no fork whose children can be reasonably bookmarked.
        /// </summary>
        private void BookmarkAnything(PieceColor _trainingSide)
        {
            for (int i = Nodes.Count - 1; i >= 0; i--)
            {
                if (Nodes[i].ColorToMove == _trainingSide || i == 0)
                {
                    AddBookmark(Nodes[i]);
                    break;
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


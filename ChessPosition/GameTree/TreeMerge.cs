using GameTree;
using System.Collections.Generic;

namespace ChessPosition.GameTree
{
    /// <summary>
    /// Manages merging of Workbooks
    /// </summary>
    public class TreeMerge
    {
        // the VariationTree that will be built and returned
        private static VariationTree _mergedTree;

        // depth to which we track position frequency
        private static int FREQ_TRACK_DEPTH = 15;


        /// <summary>
        /// Merges a list of trees into one and returns a deep copy.
        /// To start with makes a copy of the first Tree in the list and then
        /// merges copies of the rest.
        /// </summary>
        /// <param name="treeList"></param>
        /// <returns></returns>
        public static VariationTree MergeVariationTreeListEx(List<VariationTree> treeList, uint maxDepth, bool reorderLines)
        {
            Dictionary<string, int> fenCounts = new Dictionary<string, int>();

            // calculate FENs for all nodes in al trees.
            foreach (VariationTree tree in treeList)
            {
                foreach (TreeNode nd in tree.Nodes)
                {
                    if (nd.MoveNumber <= maxDepth + 1)
                    {
                        nd.Fen = FenParser.GenerateFenFromPosition(nd.Position);
                        if (fenCounts.ContainsKey(nd.Fen))
                        {
                            fenCounts[nd.Fen]++;
                        }
                        else
                        {
                            fenCounts[nd.Fen] = 1;
                        }
                    }
                }
            }

            VariationTree mergedTree = null;
            if (treeList != null && treeList.Count != 0)
            {
                // make a deep copy of the first tree up to the maxDepth level. 
                mergedTree = TreeUtils.CopyVariationTree(treeList[0], maxDepth);
                if (treeList.Count > 1)
                {
                    for (int i = 1; i < treeList.Count; i++)
                    {
                        MergeVariationTreesEx(mergedTree, treeList[i], maxDepth);
                    }

                    if (fenCounts != null && reorderLines)
                    {
                        OrderChildrenByOccurence(mergedTree, fenCounts);
                    }

                }
            }
            return mergedTree;
        }

        /// <summary>
        /// Starting at the root node, calls MergeNodeIntoTree
        /// recursively on the tree.
        /// </summary>
        /// <param name="mergedTree"></param>
        /// <param name="secondTree"></param>
        /// <param name="maxDepth"></param>
        public static void MergeVariationTreesEx(VariationTree mergedTree, VariationTree secondTree, uint maxDepth)
        {
            TreeNode nd = secondTree.RootNode;
            MergeNodeIntoTree(mergedTree, nd, maxDepth);
        }

        /// <summary>
        /// The merging method that is called recursively
        /// </summary>
        /// <param name="mergedTree"></param>
        /// <param name="secondTreeNode"></param>
        private static void MergeNodeIntoTree(VariationTree mergedTree, TreeNode secondTreeNode, uint maxDepth)
        {
            if (secondTreeNode.MoveNumber > maxDepth)
            {
                return;
            }

            // find out if secondTreeNode is a dupe i.e. an "original" node in the mergedTree.
            TreeNode orig = TreeUtils.FindTreeNodeByFen(mergedTree, secondTreeNode.Fen);
            if (orig != null)
            {
                // this is a dupe so insert nothing and keep iterating if there are any children
                foreach (TreeNode child in secondTreeNode.Children)
                {
                    if (!string.IsNullOrEmpty(secondTreeNode.Comment))
                    {
                        orig.Comment += ("; " + secondTreeNode.Comment);
                    }
                    MergeNodeIntoTree(mergedTree, child, maxDepth);
                }
            }
            else
            {
                // this is no duplicate so find the "original" of the parent in the mergedtree
                // and insert the subtree starting from secondTreeNode.
                var origParent = TreeUtils.FindTreeNodeByFen(mergedTree, secondTreeNode.Parent.Fen);
                mergedTree.InsertSubtree(origParent, secondTreeNode.Parent, maxDepth);
            }
        }

        /// <summary>
        /// Merges a list of trees into one and returns a deep copy
        /// of the merged product.
        /// </summary>
        /// <param name="treeList"></param>
        /// <returns></returns>
        public static VariationTree MergeVariationTreeList(List<VariationTree> treeList, uint maxDepth, bool reorderLines)
        {
            VariationTree returnTree = null;

            if (treeList != null && treeList.Count != 0)   
            {
                returnTree = TreeUtils.CopyVariationTree(treeList[0], maxDepth);
                if (treeList.Count > 1)
                {
                    Dictionary<string, int> fenCounts = null;
                    if (reorderLines)
                    {
                        fenCounts = new Dictionary<string, int>();
                        // add the first tree to the Counts directory
                        CountFenOccurences(returnTree, fenCounts);
                    }
                    for (int i = 1; i < treeList.Count; i++)
                    {
                        returnTree = MergeVariationTrees(returnTree, treeList[i], maxDepth, fenCounts);
                    }

                    if (fenCounts != null)
                    {
                        OrderChildrenByOccurence(returnTree, fenCounts);
                    }

                }
            }

            return returnTree;
        }

        /// <summary>
        /// Performs merging of 2 Variation Trees returns a new, merged tree as the result.
        /// </summary>
        /// <param name="tree1"></param>
        /// <param name="tree2"></param>
        /// <returns></returns>
        public static VariationTree MergeVariationTrees(VariationTree tree1, VariationTree tree2, uint maxDepth = 0, Dictionary<string,int> fenCounts = null)
        {
            if (fenCounts != null)
            {
                // only add nodes from the tree2. Nodes from tree1 already accounted for
                CountFenOccurences(tree2, fenCounts);
            }

            // create a new Variation Tree and create a root Node
            _mergedTree = new VariationTree(GameData.ContentType.STUDY_TREE);
            _mergedTree.CreateNew();

            MergeSubtrees(tree1.Nodes[0], tree2.Nodes[0], _mergedTree.Nodes[0], maxDepth);
            return _mergedTree;
        }

        /// <summary>
        /// Merges 2 subtrees beginning from the passed Nodes.
        /// A single Node representing those passed Nodes must already be in the
        /// final tree (a single node because these 2 nodes represent the same position
        /// in the 2 input trees).
        /// 
        /// Gets all children of each node and adds them the final tree
        /// making sure we don't add duplicates.
        /// 
        /// Where the node is found in only one of the trees, adds the entire subtree,
        /// otherwise adds the node and calls this method recursively on all such nodes.
        /// </summary>
        /// <param name="tn1"></param>
        /// <param name="tn2"></param>
        private static void MergeSubtrees(TreeNode tn1, TreeNode tn2, TreeNode outParent, uint maxDepth)
        {
            Dictionary<string, TreeNode> _dict1 = new Dictionary<string, TreeNode>();
            Dictionary<string, TreeNode> _dict2 = new Dictionary<string, TreeNode>();

            List<TreeNode> _dupes1 = new List<TreeNode>();
            List<TreeNode> _dupes2 = new List<TreeNode>();
            List<string> _dupeFens = new List<string>();

            _dict1.Clear();
            _dict2.Clear();

            // add all children of tn1 to the dictionary
            foreach (TreeNode nd in tn1.Children)
            {
                if (maxDepth == 0 || nd.MoveNumber <= maxDepth)
                {
                    _dict1[FenParser.GenerateFenFromPosition(nd.Position)] = nd;
                }
                else
                {
                    break;
                }
            }

            // add all children of tn2 to the dictionary
            foreach (TreeNode nd in tn2.Children)
            {
                if (maxDepth == 0 || nd.MoveNumber <= maxDepth)
                {
                    _dict2[FenParser.GenerateFenFromPosition(nd.Position)] = nd;
                }
                else
                {
                    break;
                }
            }

            _dupes1.Clear();
            _dupes2.Clear();
            _dupeFens.Clear();

            // place the positions found in both dictionaries in separate lists, and remove them from the dictionaries
            foreach (string fen in _dict1.Keys)
            {
                if (_dict2.ContainsKey(fen))
                {
                    _dupes1.Add(_dict1[fen]);
                    _dupes2.Add(_dict2[fen]);
                    _dupeFens.Add(fen);
                }
            }
            foreach (string fen in _dupeFens)
            {
                _dict1.Remove(fen);
                _dict2.Remove(fen);
            }

            // now the dictionaries contain unique nodes,
            // insert them into the final tree
            foreach (TreeNode nd in _dict1.Values)
            {
                TreeNode outNode = InsertNode(nd, outParent);
                _mergedTree.InsertSubtree(outNode, nd);
            }
            foreach (TreeNode nd in _dict2.Values)
            {
                TreeNode outNode = InsertNode(nd, outParent);
                _mergedTree.InsertSubtree(outNode, nd);
            }

            // for the dupes, add new nodes to the output tree 
            // and call this method recursively
            for (int i = 0; i < _dupes1.Count; i++)
            {
                // combine comments if any
                if (_dupes1[i].Comment != null)
                {
                    if (_dupes2[i].Comment != null)
                    {
                        _dupes1[i].Comment = _dupes1[i].Comment + "; " + _dupes2[i].Comment;
                    }
                }
                else if (_dupes2[i].Comment != null)
                {
                    _dupes1[i].Comment = _dupes2[i].Comment;
                }

                TreeNode outNode = InsertNode(_dupes1[i], outParent);
                MergeSubtrees(_dupes1[i], _dupes2[i], outNode, maxDepth);
            }
        }

        /// <summary>
        /// Inserts the passed node into the final tree with 
        /// outParent as the Parent.
        /// </summary>
        /// <param name="nodeToInsert"></param>
        /// <param name="finalTreeParent"></param>
        /// <returns></returns>
        private static TreeNode InsertNode(TreeNode nodeToInsert, TreeNode finalTreeParent)
        {
            TreeNode newNodeForFinalTree = nodeToInsert.CloneMe(true);
            newNodeForFinalTree.Parent = finalTreeParent;
            finalTreeParent.AddChild(newNodeForFinalTree);
            newNodeForFinalTree.NodeId = _mergedTree.GetNewNodeId();

            _mergedTree.AddNode(newNodeForFinalTree);

            return newNodeForFinalTree;
        }

        /// <summary>
        /// Updates occurence counts for the positions in the tree.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="fenCounts"></param>
        private static void CountFenOccurences(VariationTree tree, Dictionary<string, int> fenCounts)
        {
            if (tree == null || fenCounts == null)
            {
                return;
            }

            foreach (TreeNode nd in tree.Nodes)
            {
                if (nd.MoveNumber > FREQ_TRACK_DEPTH)
                {
                    break;
                }
                else
                {
                    string fen = FenParser.GenerateFenFromPosition(nd.Position);
                    if (fenCounts.ContainsKey(fen))
                    {
                        fenCounts[fen]++;
                    }
                    else
                    {
                        fenCounts[fen] = 1;
                    }
                }
            }
        }

        /// <summary>
        /// Orders children (lines) at each fork per frequency counts
        /// by calling the recursive function OrderChildrenNodes()
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="fenCounts"></param>
        private static void OrderChildrenByOccurence(VariationTree tree, Dictionary<string, int> fenCounts)
        {
            OrderChildrenNodes(tree.RootNode, fenCounts);
        }

        /// <summary>
        /// Recursively identifies siblings and orders them per frequency
        /// registered in fenCounts.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="fenCounts"></param>
        private static void OrderChildrenNodes(TreeNode nd, Dictionary<string, int> fenCounts)
        {
            if (nd.MoveNumber >= FREQ_TRACK_DEPTH)
            {
                return;
            }

            List<FenFrequency> lstFenFreq = new List<FenFrequency>();

            foreach (TreeNode child in nd.Children)
            {
                string fen = FenParser.GenerateFenFromPosition(child.Position);
                if (fenCounts.ContainsKey(fen))
                {
                    lstFenFreq.Add(CreateFenFrequencyObject(child, fen, fenCounts[fen]));
                    if (child.Children.Count > 0)
                    {
                        OrderChildrenNodes(child, fenCounts);
                    }
                }
            }

            lstFenFreq.Sort(CompareFenFrequency);
            if (nd.Children.Count == lstFenFreq.Count)
            {
                for (int i = 0; i < lstFenFreq.Count; i++)
                {
                    nd.Children[i] = lstFenFreq[i].Node;
                }
            }
        }

        /// <summary>
        /// Compares 2 FenFrequency objects based
        /// on the value of Occurences
        /// </summary>
        /// <param name="f1"></param>
        /// <param name="f2"></param>
        /// <returns></returns>
        private static int CompareFenFrequency(FenFrequency f1, FenFrequency f2)
        {
            return f2.Occurences - f1.Occurences;
        }

        /// <summary>
        /// Creates a FenFrequency object.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="fen"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static FenFrequency CreateFenFrequencyObject(TreeNode nd, string fen, int count)
        {
            return new FenFrequency(nd, fen, count);
        }
    }

    /// <summary>
    /// Structure to use when sorting nodes by frequency
    /// </summary>
    struct FenFrequency
    {
        public FenFrequency(TreeNode nd, string fen, int count)
        {
            Node = nd;
            Fen = fen;
            Occurences = count;
        }

        public TreeNode Node;
        public string Fen;
        public int Occurences;
    }
}

using GameTree;
using System.Collections.Generic;

namespace ChessPosition.GameTree
{
    /// <summary>
    /// Manages merging of Workbooks
    /// </summary>
    public class TreeMerge
    {
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

            // calculate FENs for all nodes in all trees.
            foreach (VariationTree tree in treeList)
            {
                foreach (TreeNode nd in tree.Nodes)
                {
                    if ((maxDepth == 0 || nd.MoveNumber <= maxDepth + 1) && nd.MoveNumber <= FREQ_TRACK_DEPTH)
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
        private static void MergeVariationTreesEx(VariationTree mergedTree, VariationTree secondTree, uint maxDepth)
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
            if (maxDepth != 0 && secondTreeNode.MoveNumber > maxDepth)
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

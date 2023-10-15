using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessPosition.GameTree
{
    /// <summary>
    /// Manages merging of Workbooks
    /// </summary>
    public class TreeMerge
    {
        // the VariationTree that will be built and returned
        private static VariationTree _mergedTree;

        /// <summary>
        /// Merges a list of trees into one and returns a deep copy
        /// of the merged product.
        /// </summary>
        /// <param name="treeList"></param>
        /// <returns></returns>
        public static VariationTree MergeVariationTrees(List<VariationTree> treeList)
        {
            VariationTree returnTree = null;

            if (treeList != null && treeList.Count != 0)   
            {
                returnTree = TreeUtils.CopyVariationTree(treeList[0]);
                if (treeList.Count > 1)
                {
                    for (int i = 1; i < treeList.Count; i++)
                    {
                        returnTree = MergeVariationTrees(returnTree, treeList[i]);
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
        public static VariationTree MergeVariationTrees(VariationTree tree1, VariationTree tree2)
        {
            // create a new Variation Tree and create a root Node
            _mergedTree = new VariationTree(GameData.ContentType.STUDY_TREE);
            _mergedTree.CreateNew();

            MergeTrees(tree1.Nodes[0], tree2.Nodes[0], _mergedTree.Nodes[0]);

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
        private static void MergeTrees(TreeNode tn1, TreeNode tn2, TreeNode outParent)
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
                _dict1[FenParser.GenerateFenFromPosition(nd.Position)] = nd;
            }

            // add all children of tn2 to the dictionary
            foreach (TreeNode nd in tn2.Children)
            {
                _dict2[FenParser.GenerateFenFromPosition(nd.Position)] = nd;
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
                MergeTrees(_dupes1[i], _dupes2[i], outNode);
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
    }
}

using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessPosition
{
    public class TreeUtils
    {
        /// <summary>
        /// Creates a new VariationTree object given a single TreeNode
        /// that will be considered the RootNode.
        /// The node and its children will be copied and the root node's id
        /// changed to 0.
        /// </summary>
        /// <param name="node"></param>
        /// <param name=""></param>
        /// <returns></returns>
        public static VariationTree CreateNewTreeFromNode(TreeNode node, GameData.ContentType contentType)
        {
            if (node == null)
            {
                return null;
            }

            VariationTree tree = new VariationTree(contentType);

            TreeNode root = node.CloneMe(false);
            int origRootId = node.NodeId;
            root.NodeId = 0;
            root.Parent = null;
            tree.AddNode(root);
            AddChildrenToTree(root, tree);
            RestartMoveNumbering(tree);
            return tree;
        }

        /// <summary>
        /// Renumbers the moves so that the first move is "1".
        /// This is used e.g. when an Exercise is created out of a study/game.
        /// </summary>
        /// <param name="tree"></param>
        public static void RestartMoveNumbering(VariationTree tree)
        {
            if (tree.Nodes.Count == 0)
            {
                return;
            }

            int rootNodeMoveNumber = (int)tree.Nodes[0].MoveNumber;
            int decrement = tree.Nodes[0].ColorToMove == PieceColor.White ? rootNodeMoveNumber : rootNodeMoveNumber - 1;
            foreach (TreeNode node in tree.Nodes)
            {
                node.MoveNumber = (uint)((int)node.MoveNumber - decrement);
            }
        }

        /// <summary>
        /// Adds children recursively to a tree
        /// </summary>
        /// <param name="node"></param>
        /// <param name="tree"></param>
        private static void AddChildrenToTree(TreeNode node, VariationTree tree)
        {
            if (node.Children.Count > 0)
            {
                foreach (TreeNode child in node.Children)
                {
                    child.Parent = node;
                    tree.AddNode(child);
                    AddChildrenToTree(child, tree);
                }
            }
        }

    }
}

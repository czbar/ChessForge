using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChessPosition
{
    public class TreeUtils
    {
        /// <summary>
        /// Sets check and mate status in psoitions as they may not have been
        /// correctly marked in the PGN.
        /// </summary>
        public static void SetCheckAndMates(ref VariationTree _tree)
        {
            foreach (TreeNode nd in _tree.Nodes)
            {
                if (PositionUtils.IsKingInCheck(nd.Position, nd.ColorToMove))
                {
                    nd.Position.IsCheck = true;
                    if (PositionUtils.IsCheckmate(nd.Position))
                    {
                        nd.Position.IsCheck = false;
                        nd.Position.IsCheckmate = true;
                    }
                }
                else
                {
                    nd.Position.IsCheck = false;
                    nd.Position.IsCheckmate = false;
                }
            }
        }

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
        /// Copies the passed tree and cerifies validity of the .
        /// If not cuts the subtree off and reports the number of removed
        /// Nodes.
        /// Returns a new Tree object contatining only the good nodes. 
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static VariationTree ValidateTree(VariationTree tree, ref List<TreeNode> nodesToRemove)
        {
            VariationTree fixedTree = tree;
            ValidateNode(fixedTree.RootNode, fixedTree, ref nodesToRemove);

            foreach (TreeNode nd in nodesToRemove)
            {
                fixedTree.DeleteRemainingMoves(nd);
            }
            SetCheckAndMates(ref fixedTree);

            return fixedTree;
        }

        /// <summary>
        /// Recursively validates the passed tree.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="fixedTree"></param>
        /// <param name="nodesToRemove"></param>
        /// <returns></returns>
        private static bool ValidateNode(TreeNode nd, VariationTree fixedTree, ref List<TreeNode> nodesToRemove)
        {
            try
            {
                if (nd.NodeId != 0)
                {
                    TreeNode newNode = MoveUtils.ProcessAlgMove(nd.LastMoveAlgebraicNotation, nd.Parent, nd.NodeId);
                    nd.Position = new BoardPosition(newNode.Position);
                }
                foreach (TreeNode child in nd.Children)
                {
                    ValidateNode(child, fixedTree, ref nodesToRemove);
                }
            }
            catch
            {
                nodesToRemove.Add(nd);
                return false;
            }

            return true;
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

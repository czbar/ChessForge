using ChessForge;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessPosition
{
    public class TreeUtils
    {
        /// <summary>
        /// Determines if the first passed line id starts with the second passed line id.
        /// Note that full partial numbers must be checked 
        /// so that we do not consider that 1.6.11 starts with 1.6.1 
        /// </summary>
        /// <param name="lineId"></param>
        /// <param name="subLineId"></param>
        /// <returns></returns>
        public static bool LineIdStartsWith(string lineId, string subLineId)
        {
            bool res = false;

            try
            {
                if (lineId.StartsWith(subLineId))
                {
                    // check that it is not subline 1.6.1 "matching" 1.6.11
                    int subLen = subLineId.Length;
                    if (lineId.Length <= subLen || lineId[subLen] == '.')
                    {
                        res = true;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("lineId=" + lineId.ToString() + "subLineId=" + subLineId.ToString(), ex);
            }

            return res;
        }

        /// <summary>
        /// Puts the subtree starting at the passed node into a list of nodes.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static List<TreeNode> NodeToNodeList(TreeNode node)
        {
            if (node == null)
            {
                return null;
            }

            List<TreeNode> lstNodes = new List<TreeNode>();
            EnlistNodeAndChildren(node, ref lstNodes);
            return lstNodes;
        }

        /// <summary>
        /// Makes a copy of a subtree starting at the passed node.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public static List<TreeNode> CopySubtree(TreeNode nd)
        {
            if (nd != null)
            {
                TreeNode clonedRoot = nd.CloneMe(false);
                return TreeUtils.NodeToNodeList(clonedRoot);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Makes a deep copy of each node in the list
        /// dropping references to children that are not in this list.
        /// </summary>
        /// <param name="nodesToCopy"></param>
        /// <returns></returns>
        public static List<TreeNode> CopyNodeList(List<TreeNode> nodesToCopy)
        {
            List<TreeNode> copiedList = new List<TreeNode>();
            foreach (TreeNode nd in nodesToCopy)
            {
                copiedList.Add(nd.CloneMe(true));
            }

            // set children
            for (int i = 0; i < copiedList.Count; i++)
            {
                TreeNode source = nodesToCopy[i];
                TreeNode target = copiedList[i];
                for (int j = 0; j < source.Children.Count; j++)
                {
                    TreeNode found = copiedList.Find(x => x.NodeId == source.Children[j].NodeId);
                    if (found != null)
                    {
                        target.Children.Add(found);
                    }
                }
            }

            return copiedList;
        }

        /// <summary>
        /// Removes all moves after the specified move/ply.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="lastMoveNo"></param>
        /// <param name="lastMoveColor"></param>
        public static void TrimTree(ref VariationTree tree, int lastMoveNo, PieceColor lastMoveColor)
        {
            // find all moves that meet the "last ply" criterion and trim the subtree
            List<TreeNode> markedForDeletion = new List<TreeNode>();
            List<TreeNode> leaves = new List<TreeNode>();

            foreach (TreeNode nd in tree.Nodes)
            {
                if (nd.MoveNumber == lastMoveNo && nd.ColorToMove == MoveUtils.ReverseColor(lastMoveColor))
                {
                    leaves.Add(nd);
                }
                else if (nd.MoveNumber > lastMoveNo || nd.MoveNumber == lastMoveNo && nd.ColorToMove == PieceColor.White && lastMoveColor == PieceColor.Black)
                {
                    markedForDeletion.Add(nd);
                }
            }

            foreach (TreeNode nd in leaves)
            {
                nd.Children.Clear(); 
            }
            foreach (TreeNode nd in markedForDeletion)
            {
                tree.Nodes.Remove(nd);
            }
        }

        /// <summary>
        /// Finds nodes featuring the passed Position.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="refBoard"></param>
        /// <param name="checkSideToMove"></param>
        /// <param name="checkEnpassant"></param>
        /// <param name="checkCastleRights"></param>
        /// <returns></returns>
        public static List<TreeNode> FindNodesWithPosition(VariationTree tree, BoardPosition refBoard, bool checkSideToMove, bool checkEnpassant, bool checkCastleRights)
        {
            List<TreeNode> nodeList = new List<TreeNode>();

            foreach (TreeNode nd in tree.Nodes)
            {
                if (refBoard.Board.Cast<byte>().SequenceEqual(nd.Position.Board.Cast<byte>()))
                {
                    if ((!checkEnpassant || IsSameEnpassantPossibilities(refBoard, nd.Position))
                        && (!checkSideToMove || refBoard.ColorToMove == nd.Position.ColorToMove)
                        && (!checkCastleRights || refBoard.CastlingRights == nd.Position.CastlingRights))
                    {
                        if (nodeList == null)
                        {
                            nodeList = new List<TreeNode>();
                        }
                        nodeList.Add(nd);
                    }
                }
            }

            return nodeList;
        }

        /// <summary>
        /// Returns the list of positions from the start (no including position 0)
        /// until the passed node.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public static List<TreeNode> GetStemLine(TreeNode nd)
        {
            List<TreeNode> line = new List<TreeNode>();

            while (nd != null && nd.Parent != null)
            {
                line.Insert(0, nd);
                nd = nd.Parent;
            }

            return line;
        }

        /// <summary>
        /// Returns the list of positions from after the passed node start 
        /// until the last node.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public static List<TreeNode> GetTailLine(TreeNode nd)
        {
            List<TreeNode> line = new List<TreeNode>();

            if (nd != null && nd.Children.Count > 0)
            {
                nd = nd.Children[0];

                while (nd != null)
                {
                    line.Add(nd);
                    if (nd.Children.Count > 0)
                    {
                        nd = nd.Children[0];
                    }
                    else
                    {
                        nd = null;
                    }
                }
            }

            return line;
        }

        /// <summary>
        /// Walks the tree and inserts all nodes into the passed list.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="lstNodes"></param>
        private static void EnlistNodeAndChildren(TreeNode node, ref List<TreeNode> lstNodes)
        {
            lstNodes.Add(node);
            foreach (TreeNode child in node.Children)
            {
                EnlistNodeAndChildren(child, ref lstNodes);
            }
        }

        /// <summary>
        /// <summary>
        /// Inserts a Subtree into a tree.
        /// If nodeToInsertAt has the opposite ColorToMove to the root node of the subtree, 
        /// the insertion will occur with nodeToInsertAt as Parent.
        /// Otherwise we will assume that the intent was to insert at the parent of nodeToInsertAt.
        /// </summary>
        /// </summary>
        /// <param name="targetTree"></param>
        /// <param name="nodeToInsertAt"></param>
        /// <param name="nodesToInsert"></param>
        /// <param name="insertedInsertions"></param>
        /// <param name="failedInsertions"></param>
        /// <returns></returns>
        public static TreeNode InsertSubtreeMovesIntoTree(VariationTree targetTree, TreeNode nodeToInsertAt, List<TreeNode> nodesToInsertOrig, ref List<TreeNode> insertedInsertions, ref List<TreeNode> failedInsertions)
        {
            try
            {
                List<TreeNode> nodesToInsert;
                if (nodesToInsertOrig[0].NodeId == 0)
                {
                    nodesToInsert = new List<TreeNode>();
                    nodesToInsert.AddRange(nodesToInsertOrig);
                    nodesToInsert.RemoveAt(0);
                }
                else
                {
                    nodesToInsert = nodesToInsertOrig;
                }

                TreeNode subtreeRoot = nodesToInsert[0];
                TreeNode updatedRoot = null;
                if (subtreeRoot != null && nodesToInsert.Count > 0 && nodeToInsertAt != null && targetTree != null)
                {
                    if (subtreeRoot.ColorToMove == nodeToInsertAt.ColorToMove)
                    {
                        nodeToInsertAt = nodeToInsertAt.Parent;
                    }
                    if (nodeToInsertAt != null)
                    {
                        updatedRoot = InsertMoveAndChildrenIntoTree(targetTree, nodeToInsertAt, subtreeRoot, ref insertedInsertions, ref failedInsertions);
                    }
                }

                return updatedRoot;
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        /// Checks if possible enpassant moves are same in both positions. 
        /// </summary>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        /// <returns></returns>
        private static bool IsSameEnpassantPossibilities(BoardPosition pos1, BoardPosition pos2)
        {
            if (pos1.EnPassantSquare == pos2.EnPassantSquare)
            {
                return true;
            }

            int epCount1 = PossibleEnpassantCapturesCount(pos1);
            int epCount2 = PossibleEnpassantCapturesCount(pos2);

            // since we know the enpassant squares are different, we will true only if there are 0 pawns available to perform the capture
            return epCount1 == 0 && epCount2 == 0;
        }

        /// <summary>
        /// How many pawns are there to take advantage of enpassant.
        /// The result will be between 0 and 2.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="epSquare"></param>
        /// <returns></returns>
        private static int PossibleEnpassantCapturesCount(BoardPosition pos)
        {
            if (pos.EnPassantSquare == 0)
            {
                return 0;
            }

            int xPos = pos.EnPassantSquare >> 4;
            int yPos = pos.EnPassantSquare & 0x0F;

            int count = 0;

            int yIncrement = pos.ColorToMove == PieceColor.White ? -1 : 1;
            if (xPos - 1 >= 0)
            {
                if (PositionUtils.GetPieceType(pos.Board[xPos - 1, yPos + yIncrement]) == PieceType.Pawn
                  && PositionUtils.GetPieceColor(pos.Board[xPos - 1, yPos + yIncrement]) == pos.ColorToMove)
                {
                    count++;
                }
            }

            if (xPos + 1 <= 7)
            {
                if (PositionUtils.GetPieceType(pos.Board[xPos + 1, yPos + yIncrement]) == PieceType.Pawn
                  && PositionUtils.GetPieceColor(pos.Board[xPos + 1, yPos + yIncrement]) == pos.ColorToMove)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Finds nodes featuring the same Position as the passed node.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="node"></param>
        /// <param name="checkSideToMove"></param>
        /// <param name="checkEnpassant"></param>
        /// <param name="checkCastleRights"></param>
        /// <returns></returns>
        public static List<TreeNode> FindIdenticalNodes(VariationTree tree, TreeNode node, bool checkSideToMove = true, bool checkEnpassant = true, bool checkCastleRights = true)
        {
            return FindNodesWithPosition(tree, node.Position, checkSideToMove, checkEnpassant, checkCastleRights);
        }

        /// <summary>
        /// Sets check and mate status in positions as they may not have been
        /// correctly marked in the PGN.
        /// </summary>
        public static void SetCheckAndMates(ref VariationTree tree)
        {
            foreach (TreeNode nd in tree.Nodes)
            {
                if (PositionUtils.IsKingInCheck(nd.Position, nd.ColorToMove))
                {
                    nd.Position.IsCheck = true;
                    if (PositionUtils.IsCheckmate(nd.Position, out _))
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
            tree.CalculateMaxNodeId();
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
        /// Builds a list of NagsAndComments.
        /// Only includes those that have non empty Nags and Comments.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static List<NagsAndComment> BuildNagsAndCommentsList(VariationTree tree)
        {
            List<NagsAndComment> lst = new List<NagsAndComment>();

            foreach (TreeNode nd in tree.Nodes)
            {
                if (!string.IsNullOrEmpty(nd.Comment) || !string.IsNullOrEmpty(nd.Nags))
                {
                    lst.Add(new NagsAndComment(nd.NodeId, nd.Comment, nd.Nags));
                }
            }

            return lst;
        }

        /// <summary>
        /// Builds a list of EvalsAndAssessments.
        /// Only includes those that have non empty EngineEval or Assessments.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static List<EvalAndAssessment> BuildEngineEvalList(VariationTree tree)
        {
            List<EvalAndAssessment> lst = new List<EvalAndAssessment>();

            foreach (TreeNode nd in tree.Nodes)
            {
                if (!string.IsNullOrEmpty(nd.EngineEvaluation) || nd.Assessment > 0)
                {
                    lst.Add(new EvalAndAssessment(nd.NodeId, nd.EngineEvaluation, nd.Assessment));
                }
            }

            return lst;
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
        /// Recursively inserts a node and its children into a Tree.
        /// </summary>
        /// <param name="targetTree"></param>
        /// <param name="insertAtNode"></param>
        /// <param name="moveToInsert"></param>
        /// <param name="failedInsertions"></param>
        private static TreeNode InsertMoveAndChildrenIntoTree(VariationTree targetTree, TreeNode insertAtNode, TreeNode moveToInsert, ref List<TreeNode> insertedNodes, ref List<TreeNode> failedInsertions)
        {
            TreeNode insertedNode = InsertMoveIntoTree(targetTree, insertAtNode, moveToInsert, ref insertedNodes);

            if (insertedNode == null)
            {
                failedInsertions.Add(moveToInsert);
                return null;
            }

            foreach (TreeNode child in moveToInsert.Children)
            {
                InsertMoveAndChildrenIntoTree(targetTree, insertedNode, child, ref insertedNodes, ref failedInsertions);
            }

            return insertedNode;
        }

        /// <summary>
        /// Inserts a move given by its algebraic notation in an existing variation tree.
        /// If the move already exists in the tree, that existing Node will be returned.
        /// We only add NEW nodes to the insertedNodes. They are subject to deletion if the
        /// operation fails or when undo is called.
        /// </summary>
        /// <param name="targetTree"></param>
        /// <param name="insertAtNode"></param>
        /// <param name="algMove"></param>
        /// <returns></returns>
        private static TreeNode InsertMoveIntoTree(VariationTree targetTree, TreeNode insertAtNode, TreeNode nodeToInsert, ref List<TreeNode> insertedNodes)
        {
            TreeNode retNode = null;

            string algMove = nodeToInsert.LastMoveAlgebraicNotation;
            // if the target tree already has a node with the passed algMove, return it
            foreach (TreeNode nd in insertAtNode.Children)
            {
                if (MoveUtils.AreAlgMovesIdentical(nd.LastMoveAlgebraicNotation, algMove))
                {
                    retNode = nd;
                    break;
                }
            }

            // if not found above, create a new node
            if (retNode == null)
            {
                try
                {
                    int nodeId = targetTree.GetNewNodeId();
                    retNode = MoveUtils.ProcessAlgMove(algMove, insertAtNode, nodeId);
                    // copy some fields from the original one to the one created here
                    retNode.Comment = nodeToInsert.Comment;
                    retNode.Nags = nodeToInsert.Nags;
                    retNode.LastMoveAlgebraicNotationWithNag = nodeToInsert.LastMoveAlgebraicNotationWithNag;
                    retNode.Position.IsCheckmate = PositionUtils.IsCheckmate(retNode.Position, out bool isCheck);
                    retNode.Position.IsCheck = isCheck;
                }
                catch
                {
                    retNode = null;
                }

                if (retNode != null)
                {
                    targetTree.AddNode(retNode);
                    insertAtNode.AddChild(retNode);
                    insertedNodes.Add(retNode);
                }
            }

            return retNode;
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

using ChessForge;
using GameTree;
using System;
using System.Collections.Generic;

namespace ChessPosition
{
    public class TreeUtils
    {
        /// <summary>
        /// Finds a node with a given FEN in a tree.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="fen"></param>
        /// <returns></returns>
        public static TreeNode FindTreeNodeByFen(VariationTree tree, string fen)
        {
            return tree.Nodes.Find(x => x.Fen == fen);
        }

        /// <summary>
        /// Checks if the first passed node has the second node as its ancestor (at any lavel).
        /// </summary>
        /// <param name="node"></param>
        /// <param name="ancestor"></param>
        /// <returns></returns>
        public static bool IsAncestor(TreeNode node, TreeNode ancestor)
        {
            bool result = false;

            if (node != null && ancestor != null)
            {
                while (node.Parent != null)
                {
                    if (node.Parent == ancestor)
                    {
                        result = true;
                        break;
                    }
                    else
                    {
                        node = node.Parent;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the closest ancestor that is not collapsed.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static TreeNode GetFirstExpandedAncestor(TreeNode node)
        {
            TreeNode ancestor = null;

            if (node != null)
            {
                while (node.Parent != null)
                {
                    if (!node.Parent.IsCollapsed)
                    {
                        ancestor = node.Parent;
                        break;
                    }
                    else
                    {
                        node = node.Parent;
                    }
                }
            }

            return ancestor;
        }

        /// <summary>
        /// Get the adjacent sibling of the passed node.
        /// If there are no siblings returns null.
        /// It there are no more siblings in the requested direction (prev/next)
        /// wraps around if wrap is set to true.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="prevNext"></param>
        /// <returns></returns>
        public static TreeNode GetNextSibling(TreeNode nd, bool prevNext, bool wrap)
        {
            TreeNode sib = nd;

            if (nd != null && nd.Parent != null && nd.Parent.Children.Count > 1)
            {
                for (int i = 0; i < nd.Parent.Children.Count; i++)
                {
                    if (nd.Parent.Children[i] == nd)
                    {
                        if (prevNext)
                        {
                            if (i > 0)
                            {
                                sib = nd.Parent.Children[i - 1];
                            }
                            else if (wrap)
                            {
                                sib = nd.Parent.Children[nd.Parent.Children.Count - 1];
                            }
                        }
                        else
                        {
                            if (i < nd.Parent.Children.Count - 1)
                            {
                                sib = nd.Parent.Children[i + 1];
                            }
                            else if (wrap)
                            {
                                sib = nd.Parent.Children[0];
                            }
                        }
                    }
                }
            }

            return sib;
        }

        /// <summary>
        /// Returns the last node in the line that matches a node 
        /// in the subtree under insertAtNode.
        /// We are looking for matches at the same level only i.e.
        /// children for line[0], grandchildren for line[1] etc.
        /// This is how we find the "real" insertion point when pasting a line.
        /// </summary>
        /// <param name="insertAtNode"></param>
        /// <param name="line"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static TreeNode GetLastSharedNode(TreeNode insertAtNode, List<TreeNode> line, out int index)
        {
            index = -1;
            TreeNode lastSharedNode = null;

            if (insertAtNode != null && line != null && line.Count > 0)
            {
                TreeNode node = insertAtNode;
                for (int i = 0; i < line.Count; i++)
                {
                    TreeNode matchedChild = GetChildMatchingNode(node, line[i]);
                    if (matchedChild != null)
                    {
                        // continue checking for matches at further levels.
                        node = matchedChild;
                        lastSharedNode = node;
                        index = i;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return lastSharedNode;
        }

        /// <summary>
        /// Finds the main line move in the passed tree with the passed move number and color.
        /// The passed color is the side "to move" i.e. White if the text is of the Black's move.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="moveNumber"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static TreeNode FindMainLineMove(VariationTree tree, int moveNumber, PieceColor color)
        {
            return tree.Nodes.Find(x => x.IsMainLine() && x.MoveNumber == moveNumber && x.ColorToMove != color);
        }

        /// <summary>
        /// Makes a deep copy of the passed variation tree.
        /// </summary>
        /// <param name="source">VariationTree to copy</param>
        /// <param name="maxDepth">The last move number which to copy. No limit if set to 0.</param>
        /// <returns></returns>
        public static VariationTree CopyVariationTree(VariationTree source, uint maxDepth = 0)
        {
            try
            {
                VariationTree copy = new VariationTree(source.ContentType);
                copy.Header = source.Header.CloneMe(true);
                copy.Nodes.Clear();
                copy.ShowTreeLines = source.ShowTreeLines;

                Dictionary<int, int> dictNodeIdToIndex = new Dictionary<int, int>();
                List<int> parentIds = new List<int>();
                Dictionary<int, List<int>> dictChildrenAtIndex = new Dictionary<int, List<int>>();

                int copyIndex = -1;

                // build a list of Nodes with dictionaries for NodeIds, parent and childre node ids
                for (int i = 0; i < source.Nodes.Count; i++)
                {
                    if (maxDepth != 0 && source.Nodes[i].MoveNumber > maxDepth)
                    {
                        // skip if outside the specified depth
                        continue;
                    }

                    copyIndex++;
                    TreeNode node = source.Nodes[i].CloneJustMe();
                    node.Children = new List<TreeNode>();

                    copy.Nodes.Add(node);

                    dictNodeIdToIndex[node.NodeId] = copyIndex;
                    if (source.Nodes[i].Parent == null)
                    {
                        parentIds.Add(-1);
                    }
                    else
                    {
                        parentIds.Add(source.Nodes[i].Parent.NodeId);
                    }
                    dictChildrenAtIndex[copyIndex] = new List<int>();
                    foreach (TreeNode child in source.Nodes[i].Children)
                    {
                        // only register children if they are within the specified depth.
                        if (maxDepth == 0 || child.MoveNumber <= maxDepth)
                        {
                            dictChildrenAtIndex[copyIndex].Add(child.NodeId);
                        }
                        else
                        {
                            // other children would have the same move number so break out
                            break;
                        }
                    }
                }

                // now populate Nodes' parents and children with object references
                for (int i = 0; i < copy.Nodes.Count; i++)
                {
                    TreeNode nd = copy.Nodes[i];
                    if (parentIds[i] == -1)
                    {
                        nd.Parent = null;
                    }
                    else
                    {
                        nd.Parent = copy.Nodes[dictNodeIdToIndex[parentIds[i]]];
                    }

                    foreach (int nodeId in dictChildrenAtIndex[i])
                    {
                        nd.Children.Add(copy.Nodes[dictNodeIdToIndex[nodeId]]);
                    }
                }

                return copy;
            }
            catch (Exception ex)
            {
                AppLog.Message("CopyVariationTree()", ex);
                return null;
            }
        }

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
                clonedRoot.Parent = null;
                return NodeToNodeList(clonedRoot);
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

            // make sure parent of the first node points to nowhere
            if (copiedList.Count > 0)
            {
                copiedList[0].Parent = null;
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
                        found.Parent = target;
                    }
                }
            }

            return copiedList;
        }

        /// <summary>
        /// Removes all moves after the specified move/ply.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="lastMoveNo">If lastMoveNo is passed as 0, no trimming is to be performed.</param>
        /// <param name="lastMoveColor"></param>
        public static void TrimTree(ref VariationTree tree, uint lastMoveNo, PieceColor lastMoveColor)
        {
            if (lastMoveNo == 0)
            {
                return;
            }

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
        /// Returns the move number of the last move in the main line
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static uint GetLastMoveNumberInMainLine(VariationTree tree)
        {
            uint moveNo = 0;

            if (tree != null || tree.Nodes.Count == 0)
            {

                TreeNode node = tree.RootNode;
                while (node.Children.Count > 0)
                {
                    node = node.Children[0];
                    moveNo = node.MoveNumber;
                }
            }

            return moveNo;
        }

        /// <summary>
        /// Returns the list of positions from the start until the passed node.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeRoot">whether to include the root node</param>
        /// <returns></returns>
        public static List<TreeNode> GetStemLine(TreeNode nd, bool includeRoot = false)
        {
            List<TreeNode> line = new List<TreeNode>();

            while (nd != null && (nd.Parent != null || includeRoot))
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
        /// Returns the main line of the tree.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static List<TreeNode> GetMainLine(VariationTree tree)
        {
            return GetTailLine(tree.RootNode);
        }

        /// <summary>
        /// Returns the branch level represented by the passed string.
        /// "1" will return level 1,
        /// "1.1" will return level 2 and so on.
        /// Null/empty string will return -1.
        /// </summary>
        /// <param name="lineId"></param>
        /// <returns></returns>
        public static int GetBranchLevel(string lineId)
        {
            int level = -1;
            if (!string.IsNullOrEmpty(lineId))
            {
                level = lineId.Split('.').Length;
            }

            return level;
        }

        /// <summary>
        /// Returns the number of nodes with some kind of comment 
        /// entered for them.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static int GetCommentsCount(VariationTree tree)
        {
            int count = 0;

            foreach (TreeNode node in tree.Nodes)
            {
                if (!string.IsNullOrEmpty(node.Comment)
                    || !string.IsNullOrEmpty(node.CommentBeforeMove)
                    || !string.IsNullOrEmpty(node.Circles)
                    || !string.IsNullOrEmpty(node.Arrows)
                    || (node.Assessment != 0))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Number of nodes with engine evaluation.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static int GetNodesWithEvalCount(VariationTree tree)
        {
            int count = 0;

            foreach (TreeNode node in tree.Nodes)
            {
                if (!string.IsNullOrEmpty(node.EngineEvaluation))
                {
                    count++;
                }
            }

            return count;
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
        public static TreeNode InsertSubtreeMovesIntoTree(VariationTree targetTree, TreeNode nodeToInsertAt, List<TreeNode> nodesToInsertOrig, ref List<TreeNode> insertedInsertions, ref List<TreeNode> failedInsertions, bool allowDupe = false)
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
                        updatedRoot = InsertMoveAndChildrenIntoTree(targetTree, nodeToInsertAt, subtreeRoot, ref insertedInsertions, ref failedInsertions, allowDupe);
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
        /// Inserts comments and nags from the list of move attributes into a tree.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="lstAttrs"></param>
        public static void InsertCommentsAndNags(VariationTree tree, List<MoveAttributes> lstAttrs)
        {
            foreach (MoveAttributes attrs in lstAttrs)
            {
                TreeNode nd = tree.GetNodeFromNodeId(attrs.NodeId);
                if (nd != null)
                {
                    nd.Comment = attrs.Comment;
                    nd.CommentBeforeMove = attrs.CommentBeforeMove;
                    nd.Nags = attrs.Nags;
                    nd.References = attrs.References;
                    nd.IsDiagram = attrs.IsDiagram;
                    nd.IsDiagramFlipped = attrs.IsDiagramFlipped;
                    nd.IsDiagramPreComment = attrs.IsDiagramPreComment;
                }
            }
        }

        /// <summary>
        /// Inserts engine evaluations from the list of move attributes into a tree.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="lstAttrs"></param>
        public static void InsertEngineEvals(VariationTree tree, List<MoveAttributes> lstAttrs)
        {
            foreach (MoveAttributes attrs in lstAttrs)
            {
                TreeNode nd = tree.GetNodeFromNodeId(attrs.NodeId);
                if (nd != null)
                {
                    nd.SetEngineEvaluation(attrs.EngineEval);
                    nd.BestResponse = attrs.BestResponse;
                    nd.Assessment = attrs.Assessment;
                }
            }
        }

        /// <summary>
        /// Removes ECO codes and Opening Names (that btw are not persisted) from the tree.
        /// We will use this when creating an Exercise from a game.
        /// </summary>
        /// <param name="tree"></param>
        public static void RemoveOpeningInfo(VariationTree tree)
        {
            foreach (TreeNode nd in tree.Nodes)
            {
                nd.Eco = "";
                nd.OpeningName = "";
            }
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
        /// Builds a list of Nodes' attributes of the type passed in attrTypes.
        /// Only includes those that any of the specified attributes.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static List<MoveAttributes> BuildMoveAttributesList(VariationTree tree, int attrTypes)
        {
            List<MoveAttributes> lst = new List<MoveAttributes>();

            foreach (TreeNode nd in tree.Nodes)
            {
                if ((attrTypes & (int)MoveAttribute.COMMENT_AND_NAGS) != 0 &&
                        (!string.IsNullOrEmpty(nd.Comment)
                        || !string.IsNullOrEmpty(nd.CommentBeforeMove)
                        || !string.IsNullOrEmpty(nd.Nags)
                        || !string.IsNullOrEmpty(nd.References)
                        || nd.IsDiagram
                        )
                    ||
                        (attrTypes & (int)MoveAttribute.ENGINE_EVALUATION) != 0 &&
                        !string.IsNullOrEmpty(nd.EngineEvaluation)
                    ||
                        (attrTypes & (int)MoveAttribute.BAD_MOVE_ASSESSMENT) != 0 &&
                        nd.Assessment > 0
                    ||
                        (attrTypes & (int)MoveAttribute.SIDELINE) != 0 &&
                        nd.IsMainLine() == false
                    )
                {

                    MoveAttributes moveAttrs = new MoveAttributes(nd);

                    bool isDeleted = (attrTypes & (int)MoveAttribute.SIDELINE) != 0 && nd.IsMainLine() == false;
                    if (isDeleted)
                    {
                        moveAttrs.IsDeleted = true;
                        moveAttrs.Node = nd;
                        if (nd.Parent != null)
                        {
                            moveAttrs.ParentId = nd.Parent.NodeId;
                            moveAttrs.ChildIndexInParent = nd.Parent.Children.IndexOf(nd);
                        }
                    }

                    lst.Add(moveAttrs);
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
        public static List<MoveAttributes> BuildEngineEvalList(VariationTree tree)
        {
            List<MoveAttributes> lst = new List<MoveAttributes>();

            foreach (TreeNode nd in tree.Nodes)
            {
                if (!string.IsNullOrEmpty(nd.EngineEvaluation) || nd.Assessment > 0)
                {
                    lst.Add(new MoveAttributes(nd.NodeId, nd.EngineEvaluation, nd.Assessment, nd.BestResponse));
                }
            }

            return lst;
        }

        /// <summary>
        /// Copies the passed tree and cerifies validity of the .
        /// If not cuts the subtree off and reports the number of removed
        /// Nodes.
        /// Returns a new Tree object containing only the good nodes. 
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
        /// Checks if two lists of TreeNodes are identical.
        /// The nodes must be in the same order in both lists.
        /// </summary>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        public static bool AreNodeListsIdentical(List<TreeNode> list1, List<TreeNode> list2)
        {
            // in our case, the lists will always have the same size so this is only a defensive check
            if (list1.Count != list2.Count)
            {
                return false;
            }
            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i] != list2[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns the child of the passed parent that has the same move as the passed node.
        /// This is useful e.g. when we want to insert a subtree into a tree and we want to find
        /// the "real" insertion point.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private static TreeNode GetChildMatchingNode(TreeNode parent, TreeNode node)
        {
            foreach (TreeNode child in parent.Children)
            {
                if (MoveUtils.AreAlgMovesIdentical(child.LastMoveAlgebraicNotation, node.LastMoveAlgebraicNotation))
                {
                    return child;
                }
            }
            return null;
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
                    if (newNode == null)
                    {
                        throw new Exception("Invalid move");
                    }
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
        private static TreeNode InsertMoveAndChildrenIntoTree(VariationTree targetTree, TreeNode insertAtNode, TreeNode moveToInsert, ref List<TreeNode> insertedNodes, ref List<TreeNode> failedInsertions, bool allowDupe = false)
        {
            TreeNode insertedNode = InsertMoveIntoTree(targetTree, insertAtNode, moveToInsert, ref insertedNodes, allowDupe);

            if (insertedNode == null)
            {
                failedInsertions.Add(moveToInsert);
                return null;
            }

            foreach (TreeNode child in moveToInsert.Children)
            {
                InsertMoveAndChildrenIntoTree(targetTree, insertedNode, child, ref insertedNodes, ref failedInsertions, allowDupe);
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
        private static TreeNode InsertMoveIntoTree(VariationTree targetTree, TreeNode insertAtNode, TreeNode nodeToInsert, ref List<TreeNode> insertedNodes, bool allowDupe = false)
        {
            TreeNode retNode = null;

            string algMove = nodeToInsert.LastMoveAlgebraicNotation;
            // if the target tree already has a node with the passed algMove, return it
            if (!allowDupe)
            {
                foreach (TreeNode nd in insertAtNode.Children)
                {
                    if (MoveUtils.AreAlgMovesIdentical(nd.LastMoveAlgebraicNotation, algMove))
                    {
                        retNode = nd;
                        break;
                    }
                }
            }

            // if not found above, create a new node
            if (retNode == null)
            {
                try
                {
                    int nodeId = targetTree.GetNewNodeId();
                    retNode = MoveUtils.ProcessAlgMove(algMove, insertAtNode, nodeId);
                    if (retNode != null)
                    {
                        // copy some fields from the original one to the one created here
                        retNode.Comment = nodeToInsert.Comment;
                        retNode.CommentBeforeMove = nodeToInsert.CommentBeforeMove;
                        retNode.Nags = nodeToInsert.Nags;
                        retNode.LastMoveAlgebraicNotationWithNag = nodeToInsert.LastMoveAlgebraicNotationWithNag;
                        retNode.Position.IsCheckmate = PositionUtils.IsCheckmate(retNode.Position, out bool isCheck);
                        retNode.Position.IsCheck = isCheck;
                    }
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

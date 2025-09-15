using System.Collections.Generic;
using System.Linq;

namespace GameTree
{
    public partial class VariationTree
    {
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
        /// Undoes reordering of the lines.
        /// </summary>
        /// <param name="oParent"></param>
        /// <param name="oChildren"></param>
        public void UndoReorderLines(object oNode, object oChildren)
        {
            try
            {
                if (oNode is TreeNode node && oChildren is List<TreeNode> oldChildren)
                {
                    TreeNode oldParent = node.Parent;
                    // there may have been operations in the meantime so we need to check that the tree
                    // is in the same state as before the operation so get the nodes by NodeId 
                    TreeNode newParent = GetNodeFromNodeId(oldParent.NodeId);

                    // Be careful here, as more often than not the old/new parents/children will be the same objects!

                    // extra checks
                    if (newParent.LastMoveAlgebraicNotation == oldParent.LastMoveAlgebraicNotation
                        && newParent.MoveNumber == oldParent.MoveNumber
                        && newParent.Children.Count == oldParent.Children.Count)
                    {
                        bool proceed = true;
                        // get old node ids and compare to the current ones
                        foreach (TreeNode child in oldChildren)
                        {
                            if (newParent.Children.FirstOrDefault(x => x.NodeId == child.NodeId) == null)
                            {
                                proceed = false;
                                break;
                            }
                        }

                        if (proceed)
                        {
                            for (int i = 0; i < oldChildren.Count; i++)
                            {
                                TreeNode newChild = GetNodeFromNodeId(oldChildren[i].NodeId);
                                newParent.Children[i] = newChild;
                            }
                        }
                    }
                }
            }
            catch { }
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
                nd.CommentBeforeMove = dummyNode.CommentBeforeMove;
                nd.QuizPoints = dummyNode.QuizPoints;
                nd.Assessment = dummyNode.Assessment;
                nd.BestResponse = dummyNode.BestResponse;
                nd.References = dummyNode.References;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Restores comment-before-move from before the last edit.
        /// </summary>
        /// <param name="dummyNode"></param>
        public void UndoUpdateCommentBeforeMove(TreeNode dummyNode)
        {
            try
            {
                TreeNode nd = GetNodeFromNodeId(dummyNode.NodeId);
                nd.CommentBeforeMove = dummyNode.CommentBeforeMove;
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
        /// Undoes the pasting of multiple lines (from the engine).
        /// The passed argument is the list of of nodes that the 
        /// original tree had.
        /// We remove all added nodes with children and from the parent's list
        /// </summary>
        /// <param name="opData"></param>
        public void UndoAddedLines(object opData)
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
        /// Deletes a previously inserted diagram.
        /// </summary>
        /// <param name="nd"></param>
        public void UndoInsertDiagram(TreeNode nd)
        {
            if (nd != null)
            {
                nd.IsDiagram = false;
            }
        }

        /// <summary>
        /// Re-inserts a previously deleted diagram.
        /// </summary>
        /// <param name="nd"></param>
        public void UndoDeleteDiagram(TreeNode nd)
        {
            if (nd != null)
            {
                nd.IsDiagram = true;
            }
        }

        /// <summary>
        /// Swaps diagram with comment.
        /// </summary>
        /// <param name="nd"></param>
        public void UndoSwapDiagramComment(TreeNode nd)
        {
            if (nd != null)
            {
                nd.IsDiagramPreComment = !nd.IsDiagramPreComment;
            }
        }

        /// <summary>
        /// Restores stripped comments and nags.
        /// </summary>
        /// <param name="opData"></param>
        public void UndoStripComments(object opData)
        {
            try
            {
                List<MoveAttributes> lst = opData as List<MoveAttributes>;
                foreach (MoveAttributes nac in lst)
                {
                    TreeNode nd = GetNodeFromNodeId(nac.NodeId);
                    nd.Comment = nac.Comment;
                    nd.Nags = nac.Nags;
                    nd.SetNags(nac.Nags);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Removes the current thumbnail and restores the previous one.
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        public void UndoMarkThumbnail(object currThumb, object prevThumb)
        {
            if (currThumb != null && currThumb is TreeNode currNode)
            {
                ClearThumbnail(currNode);
            }

            if (prevThumb != null && prevThumb is TreeNode prevNode)
            {
                SetThumbnail(prevNode);
            }
        }

        /// <summary>
        /// Restores deleted engine evaluations and assessments.
        /// </summary>
        /// <param name="opData"></param>
        public void UndoDeleteEngineEvals(object opData)
        {
            try
            {
                List<MoveAttributes> lst = opData as List<MoveAttributes>;
                foreach (MoveAttributes nac in lst)
                {
                    TreeNode nd = GetNodeFromNodeId(nac.NodeId);
                    nd.SetEngineEvaluation(nac.EngineEval);
                    nd.Assessment = nac.Assessment;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Restores deleted reference.
        /// </summary>
        /// <param name="oNode"></param>
        /// <param name="oRefGuid"></param>
        /// <returns></returns>
        public int UndoDeleteReference(object oNode, object oRefGuid, out HashSet<int> nodesToUpdate)
        {
            nodesToUpdate = new HashSet<int>();

            int nodeId = -1;
            if (oNode is TreeNode node && oRefGuid is string refGuid)
            {
                node.AddArticleReference(refGuid);
                nodeId = node.NodeId;
                nodesToUpdate.Add(nodeId);
            }

            return nodeId;
        }

        /// <summary>
        /// Restores original positions of references.
        /// </summary>
        /// <param name="oPreOpNodes">a list of node id / references to restore</param>
        /// <param name="nodesToUpdate">returns list of nodes that were affected.</param>
        /// <returns></returns>
        public int UndoRepositionReferences(object oPreOpNodes, out HashSet<int> nodesToUpdate)
        {
            int selectedNodeId = -1;

            nodesToUpdate = new HashSet<int>();
            if (oPreOpNodes is List<MoveAttributes> preOpNodes)
            {
                selectedNodeId = preOpNodes[0].NodeId;
                foreach (MoveAttributes nac in preOpNodes)
                {
                    TreeNode nd = GetNodeFromNodeId(nac.NodeId);
                    nd.References = nac.References;
                    nodesToUpdate.Add(nac.NodeId);
                }

            }

            return selectedNodeId;
        }

        /// <summary>
        /// Restores deleted assessments.
        /// </summary>
        /// <param name="oPreOpAssessments"></param>
        /// <param name="nodesToUpdate"></param>
        public void UndoDeleteAssessments(object oPreOpAssessments, out HashSet<int> nodesToUpdate)
        {
            nodesToUpdate = new HashSet<int>();

            var nodeAssessments = oPreOpAssessments as Dictionary<int, uint>;
            foreach (var kvp in nodeAssessments)
            {
                TreeNode node = GetNodeFromNodeId(kvp.Key);
                if (node != null)
                {
                    node.Assessment = kvp.Value;
                    nodesToUpdate.Add(node.NodeId);
                }
            }
        }

    }
}

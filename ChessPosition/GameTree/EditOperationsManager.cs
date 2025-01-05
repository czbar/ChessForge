using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameTree
{
    public class EditOperationsManager : OperationsManager
    {
        // parent tree
        private VariationTree _owningTree;

        /// <summary>
        /// Contructor for OperationsManager created in a VariationTree
        /// </summary>
        /// <param name="tree"></param>
        public EditOperationsManager(VariationTree tree)
        {
            _owningTree = tree;
        }

        /// <summary>
        /// Performs the undo of the Operation in the queue.
        /// </summary>
        public void Undo(out EditOperation.EditType tp, out string selectedLineId, out int selectedNodeId)
        {
            tp = EditOperation.EditType.NONE;
            selectedLineId = "";
            selectedNodeId = -1;
            if (_operations.Count == 0)
            {
                return;
            }

            try
            {
                EditOperation op = _operations.Pop() as EditOperation;
                tp = op.OpType;

                switch (tp)
                {
                    case EditOperation.EditType.DELETE_LINE:
                        // restore line
                        _owningTree.UndoDeleteSubtree(op.Node, op.NodeList, op.ChildIndex);
                        selectedNodeId = op.Node.NodeId;
                        break;
                    case EditOperation.EditType.PROMOTE_LINE:
                        _owningTree.UndoPromoteLine(op.Node, op.ChildIndex);
                        if (op.OpData_1 is TreeNode selNode)
                        {
                            selectedNodeId = selNode.NodeId;
                        }
                        break;
                    case EditOperation.EditType.REORDER_LINES:
                        _owningTree.UndoReorderLines(op.OpData_1, op.OpData_2);
                        break;
                    case EditOperation.EditType.UPDATE_ANNOTATION:
                        _owningTree.UndoUpdateAnnotation(op.Node);
                        selectedNodeId = op.Node.NodeId;
                        break;
                    case EditOperation.EditType.UPDATE_COMMENT_BEFORE_MOVE:
                        _owningTree.UndoUpdateCommentBeforeMove(op.Node);
                        selectedNodeId = op.Node.NodeId;
                        break;
                    case EditOperation.EditType.MERGE_TREE:
                        _owningTree.UndoAddedNodeList(op.OpData_1);
                        break;
                    case EditOperation.EditType.PASTE_MOVES:
                        _owningTree.UndoAddedNodeList(op.OpData_1);
                        selectedNodeId = op.NodeId;
                        break;
                    case EditOperation.EditType.SAVE_TRAINING_MOVES:
                        _owningTree.UndoAddedNodeList(op.OpData_1);
                        break;
                    case EditOperation.EditType.ADD_MOVE:
                        _owningTree.UndoAddMove(op.Node);
                        selectedNodeId= op.Node.Parent.NodeId;
                        selectedLineId= op.Node.Parent.LineId;
                        break;
                    case EditOperation.EditType.INSERT_DIAGRAM:
                        _owningTree.UndoInsertDiagram(op.Node);
                        selectedNodeId = op.Node.NodeId;
                        break;
                    case EditOperation.EditType.DELETE_DIAGRAM:
                        _owningTree.UndoDeleteDiagram(op.Node);
                        selectedNodeId = op.Node.NodeId;
                        break;
                    case EditOperation.EditType.SWAP_DIAGRAM_COMMENT:
                        _owningTree.UndoSwapDiagramComment(op.Node);
                        selectedNodeId = op.Node.NodeId;
                        break;
                    case EditOperation.EditType.MARK_THUMBNAIL:
                        _owningTree.UndoMarkThumbnail(op.OpData_1, op.OpData_2);
                        if (op.OpData_1 is TreeNode tnNode)
                        {
                            selectedNodeId = tnNode.NodeId;
                        }
                        break;
                    case EditOperation.EditType.DELETE_REFERENCE:
                        selectedNodeId = _owningTree.UndoDeleteReference(op.OpData_1, op.OpData_2);
                        break;
                    case EditOperation.EditType.REPOSITION_REFERENCES:
                        selectedNodeId =  _owningTree.UndoRepositionReferences(op.OpData_1, op.OpData_2);
                        break;
                }
            }
            catch
            {
            }
        }
    }
}

﻿using System;
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
                        break;
                    case EditOperation.EditType.PROMOTE_LINE:
                        _owningTree.UndoPromoteLine(op.Node, op.ChildIndex);
                        break;
                    case EditOperation.EditType.UPDATE_ANNOTATION:
                        _owningTree.UndoUpdateAnnotation(op.Node);
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
                    case EditOperation.EditType.STRIP_COMMENTS:
                        _owningTree.UndoStripComments(op.OpData_1);
                        break;
                }
            }
            catch
            {
            }
        }
    }
}

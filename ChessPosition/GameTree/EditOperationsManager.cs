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
        public void Undo()
        {
            if (_operations.Count == 0)
            {
                return;
            }

            EditOperation op = _operations.Dequeue() as EditOperation;
            switch (op.OpType)
            {
                case EditOperation.EditType.DELETE_LINE:
                    // restore line
                    _owningTree.RestoreSubtree(op.Node, op.NodeList, op.ChildIndex);
                    break;
            }
        }
    }
}

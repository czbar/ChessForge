using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameTree
{
    public class EditOperationsManager
    {
        // queue of operations
        private Queue<EditOperation> _operations = new Queue<EditOperation>();

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
        /// Clears the Operations queue
        /// </summary>
        public void Reset()
        {
            _operations.Clear();
        }
    }
}

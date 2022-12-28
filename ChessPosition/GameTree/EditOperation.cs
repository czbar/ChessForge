using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace GameTree
{
    public class EditOperation : Operation
    {
        /// <summary>
        /// Types of supported operations.
        /// </summary>
        public enum EditType
        {
            NONE,
            DELETE_LINE,
            PROMOTE_LINE,
            ADD_MOVE,
        }

        /// <summary>
        /// List of nodes to operate on.
        /// </summary>
        public List<TreeNode> NodeList { get { return _nodeList; } }

        /// <summary>
        /// The TreeNode operated no.
        /// </summary>
        public TreeNode Node { get { return _node; } }

        /// <summary>
        /// Child index in its parent's children list that will be required e.g. when promoting
        /// a line it will be the original index from which the line was promoted.
        /// </summary>
        public int ChildIndex { get { return _childIndex; } }

        /// <summary>
        /// Operation type.
        /// </summary>
        public EditType OpType { get { return _opType; } }

        /// <summary>
        /// Type of this operation.
        /// </summary>
        private EditType _opType;

        /// <summary>
        /// List of nodes that were operated on.
        /// </summary>
        private List<TreeNode> _nodeList;

        /// <summary>
        /// The node operated on
        /// </summary>
        private TreeNode _node;

        // Child index 
        private int _childIndex;

        /// <summary>
        /// Constructor for DELETE_LINE.
        /// </summary>
        public EditOperation(EditType tp, TreeNode deletionRoot, List<TreeNode> deletedNodes, int childIndex) : base()
        {
            _opType = tp;
            _node = deletionRoot;
            _nodeList = deletedNodes;
            _childIndex = childIndex;
        }

        /// <summary>
        /// Constructor for PROMOTE_LINE.
        /// </summary>
        public EditOperation(EditType tp, TreeNode promotionRoot, int originalChildIndex) : base()
        {
            _opType = tp;
            _node = promotionRoot;
            _childIndex = originalChildIndex;
        }

        /// <summary>
        /// Constructor for ADD_MOVE.
        /// </summary>
        public EditOperation(EditType tp, TreeNode move) : base()
        {
            _opType = tp;
            _node = move;
        }
    }
}

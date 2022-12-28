using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameTree
{
    public class EditOperation
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
        /// Type of this operation.
        /// </summary>
        private EditType _opType;

        /// <summary>
        /// List of nodes that were operated on.
        /// </summary>
        private List<TreeNode> _nodeList;

        /// <summary>
        /// The node oparated on
        /// </summary>
        private TreeNode _node;

        /// <summary>
        /// Child index will be required e.g. when promoting
        /// a line it will be the original index from which the line was promoted.
        /// </summary>
        private int _childIndex;

        /// <summary>
        /// Constructor for DELETE_LINE.
        /// </summary>
        public EditOperation(EditType tp, TreeNode deletionRoot, List<TreeNode> deletedNodes)
        {
            _opType = tp;
            _node = deletionRoot;
            _nodeList = deletedNodes;
        }

        /// <summary>
        /// Constructor for PROMOTE_LINE.
        /// </summary>
        public EditOperation(EditType tp, TreeNode promotionRoot, int originalChildIndex)
        {
            _opType = tp;
            _node = promotionRoot;
            _childIndex = originalChildIndex;
        }

        /// <summary>
        /// Constructor for ADD_MOVE.
        /// </summary>
        public EditOperation(EditType tp, TreeNode move)
        {
            _opType = tp;
            _node = move;
        }
    }
}

using System.Collections.Generic;

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
            REORDER_LINES,
            ADD_MOVE,
            UPDATE_ANNOTATION,
            UPDATE_COMMENT_BEFORE_MOVE,
            MERGE_TREE,
            PASTE_MOVES,
            PASTE_LINES,
            SAVE_TRAINING_MOVES,
            INSERT_DIAGRAM,
            DELETE_DIAGRAM,
            SWAP_DIAGRAM_COMMENT,
            MARK_THUMBNAIL,
            DELETE_REFERENCE,
            REPOSITION_REFERENCES,
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
        /// Id of the TreeNode operated no.
        /// </summary>
        public int NodeId { get { return _nodeId; } }

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

        /// <summary>
        /// Id of the node being operated on.
        /// </summary>
        private int _nodeId;

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
        public EditOperation(EditType tp, TreeNode promotionRoot, int originalChildIndex, TreeNode selectedNode) : base()
        {
            _opType = tp;
            _node = promotionRoot;
            _childIndex = originalChildIndex;
            _opData_1 = selectedNode;
        }

        /// <summary>
        /// Constructor for ADD_MOVE and UPDATE_ANNOTATION.
        /// </summary>
        public EditOperation(EditType tp, TreeNode nd) : base()
        {
            _opType = tp;
            if (tp == EditType.UPDATE_ANNOTATION || tp == EditType.UPDATE_COMMENT_BEFORE_MOVE)
            {
                // create a dummy node to keep the data of relevance
                _node = new TreeNode(null, "", nd.NodeId);
                if (tp == EditType.UPDATE_ANNOTATION)
                {
                    _node.Nags = nd.Nags;
                    _node.Comment = nd.Comment;
                    _node.QuizPoints = nd.QuizPoints;
                    _node.Assessment = nd.Assessment;
                    _node.BestResponse = nd.BestResponse;
                    _node.References = nd.References;
                }
                else if (tp == EditType.UPDATE_COMMENT_BEFORE_MOVE)
                {
                    _node.CommentBeforeMove = nd.CommentBeforeMove;
                }
            }
            else
            {
                _node = nd;
            }
        }

        /// <summary>
        /// Constructor for "other" operations.
        /// </summary>
        public EditOperation(EditType tp, int nodeId, object data_1, object data_2) : base()
        {
            _opType = tp;
            _nodeId = nodeId;
            _opData_1 = data_1;
            _opData_2 = data_2;
        }

        /// <summary>
        /// Constructor for "other" operations.
        /// 
        /// REORDER_LINES: data_1 is the parent node, data_2 is the list of children
        ///     in the original order List<TreeNde> 
        ///     
        /// </summary>
        public EditOperation(EditType tp, object data_1, object data_2) : base()
        {
            _opType = tp;
            _opData_1 = data_1;
            _opData_2 = data_2;
        }
    }
}

namespace GameTree
{
    /// <summary>
    /// Holds node Id along with some move attribures..
    /// This has been created to support Undo for DeleteComments / EngineEvals
    /// </summary>
    public class MoveAttributes
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public MoveAttributes(TreeNode nd) 
        {
            NodeId = nd.NodeId;
            Comment = nd.Comment;
            CommentBeforeMove = nd.CommentBeforeMove;
            Nags = nd.Nags;
            References = nd.References;
            IsDiagram = nd.IsDiagram;
            IsDiagramFlipped = nd.IsDiagramFlipped;
            IsDiagramPreComment = nd.IsDiagramPreComment;
            IsDiagramBeforeMove = nd.IsDiagramBeforeMove;
            EngineEval = nd.EngineEvaluation;
            Assessment = nd.Assessment;
            BestResponse = nd.BestResponse;
        }

        /// <summary>
        /// Constructor setting the comment and Nags properties.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="comment"></param>
        /// <param name="nags"></param>
        /// <param name="references"></param>
        /// <param name="isDiagram"></param>
        /// <param name="isDiagramFlipped"></param>
        /// <param name="isDiagramPreComment"></param>
        /// <param name="isDiagramBeforeMove"></param>
        public MoveAttributes(int nodeId, string comment, string commentBeforeMove, string nags, string references, bool isDiagram, bool isDiagramFlipped, bool isDiagramPreComment, bool isDiagramBeforeMove) 
        { 
            NodeId = nodeId;
            Comment = comment;
            CommentBeforeMove = commentBeforeMove;
            Nags = nags;
            References = references;
            IsDiagram = isDiagram;
            IsDiagramFlipped = isDiagramFlipped;
            IsDiagramPreComment = isDiagramPreComment;
            IsDiagramBeforeMove = isDiagramBeforeMove;
        }

        /// <summary>
        /// Constructor setting the engine eval and assessment properties.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="engineEval"></param>
        /// <param name="assessment"></param>
        public MoveAttributes(int nodeId, string engineEval, uint assessment, string bestResponse)
        {
            NodeId = nodeId;
            EngineEval = engineEval;
            BestResponse = bestResponse;
            Assessment = assessment;
        }

        /// <summary>
        /// Constructor setting the references property.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="references"></param>
        public MoveAttributes(int nodeId, string references)
        {
            NodeId = nodeId;
            References = references;
        }

        /// <summary>
        /// Node id.
        /// </summary>
        public int NodeId;

        /// <summary>
        /// IsDeleted flag.
        /// </summary>
        public bool IsDeleted;

        /// <summary>
        /// Comment.
        /// </summary>
        public string Comment;

        /// <summary>
        /// Comment before move 
        /// </summary>
        public string CommentBeforeMove;

        /// <summary>
        /// Nags string.
        /// </summary>
        public string Nags;

        /// <summary>
        /// Refernces string
        /// </summary>
        public string References;

        /// <summary>
        /// The IsDiagram flag.
        /// </summary>
        public bool IsDiagram;

        /// <summary>
        /// The diagram flipped flag.
        /// </summary>
        public bool IsDiagramFlipped;

        /// <summary>
        /// If IsDiagram==true, whether the diagram
        /// comes before the comment.
        /// </summary>
        public bool IsDiagramPreComment;

        /// <summary>
        /// Indicates whether the diagram is placed before the move.
        /// </summary>
        public bool IsDiagramBeforeMove;

        /// <summary>
        /// Engine Evaluation.
        /// </summary>
        public string EngineEval;

        /// <summary>
        /// Best response.
        /// </summary>
        public string BestResponse;

        /// <summary>
        /// Coded move assessment (e.g. BLUNDER).
        /// </summary>
        public uint Assessment;

        /// <summary>
        /// This is only set when we need a node for an undo delete operation.
        /// </summary>
        public TreeNode Node { get; set; }

        /// <summary>
        /// Parent id.
        /// This is only set when we need a node for an undo delete operation.
        /// </summary>
        public int ParentId;

        /// <summary>
        /// Index on the child list in the parent.
        /// This is only set when we need a node for an undo delete operation.
        /// </summary>
        public int ChildIndexInParent;

    }
}

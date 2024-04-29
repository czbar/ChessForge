using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public MoveAttributes() 
        { 
        }

        /// <summary>
        /// Constructor setting the comment and Nags properties.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="comment"></param>
        /// <param name="nags"></param>
        public MoveAttributes(int nodeId, string comment, string commentBeforeMove, string nags) 
        { 
            NodeId = nodeId;
            Comment = comment;
            CommentBeforeMove = commentBeforeMove;
            Nags = nags;
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
        /// Node id.
        /// </summary>
        public int NodeId;
        
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
    }
}

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
        /// Constructor setting the properties.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="comment"></param>
        /// <param name="nags"></param>
        public MoveAttributes(int nodeId, string comment, string nags) 
        { 
            NodeId = nodeId;
            Comment = comment;
            Nags = nags;
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
        /// Nags string.
        /// </summary>
        public string Nags;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameTree
{
    /// <summary>
    /// Holds node Id along with the Comment and Nags.
    /// This was created to supported Undo for StripComments
    /// </summary>
    public class NagsAndComment
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public NagsAndComment() 
        { 
        }

        /// <summary>
        /// Constructor setting the properties.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="comment"></param>
        /// <param name="nags"></param>
        public NagsAndComment(int nodeId, string comment, string nags) 
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

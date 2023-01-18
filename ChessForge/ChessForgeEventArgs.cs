using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Custom Event Args
    /// </summary>
    public class ChessForgeEventArgs : EventArgs
    {
        /// <summary>
        /// Whether event's result was success
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Id of the tree to which the handled Node belongs
        /// </summary>
        public int TreeId { get; set; }

        /// <summary>
        /// Id of the Node being handled.
        /// </summary>
        public int NodeId { get; set; }

        /// <summary>
        /// Index of the Chapter
        /// </summary>
        public int ChapterIndex { get; set; }

        /// <summary>
        /// Index of the Article (Game or Exercise)
        /// </summary>
        public int ArticleIndex {get; set; }

        /// <summary>
        /// Type of the Article (Game or Exercise)
        /// </summary>
        public GameData.ContentType ContentType { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessPosition;

namespace GameTree
{
    /// <summary>
    /// A bookmark is a single position from the Workbook
    /// selected for quick access.
    /// </summary>
    public class Bookmark
    {
        /// <summary>
        /// Bookmark's position
        /// </summary>
        public TreeNode Node;

        /// <summary>
        /// Any comment associated with the bookmark
        /// </summary>
        public string Comment;

        /// <summary>
        /// Creates the Bookmark objects and sets the Node.
        /// </summary>
        /// <param name="nd"></param>
        public Bookmark(TreeNode nd)
        {
            Node = nd;
        }

    }
}

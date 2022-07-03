using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameTree
{
    /// <summary>
    /// A bookmark is a single position from the Workbook
    /// selected for quick access.
    /// </summary>
    public class Bookmark
    {
        public TreeNode Node;
        public string Comment;

        public Bookmark(TreeNode nd)
        {
            Node = nd;
        }
    }
}

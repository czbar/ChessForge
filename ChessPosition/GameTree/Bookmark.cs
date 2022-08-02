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
    public class Bookmark : IComparable<Bookmark>
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

        /// <summary>
        /// Comparator to use when sorting by move number and color.
        /// </summary>
        /// <param name="bm"></param>
        /// <returns></returns>
        public int CompareTo(Bookmark bm)
        {
            if (bm == null)
                return -1;

            int moveNoDiff = (int)this.Node.MoveNumber - (int)bm.Node.MoveNumber;
            if (moveNoDiff != 0)
            {
                return moveNoDiff;
            }
            else
            {
                if (this.Node.ColorToMove == PieceColor.Black)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }
    }
}

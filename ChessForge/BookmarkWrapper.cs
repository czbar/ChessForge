using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Holds a Bookmark object together with external information regarding
    /// the bookmark's parent tree as well as tree's parent Chapter and
    /// position index.
    /// </summary>
    public class BookmarkWrapper : IComparable<BookmarkWrapper>
    {
        /// <summary>
        /// The Bookmark object shown in this view.
        /// </summary>
        public Bookmark Bookmark;

        /// <summary>
        /// Index of a Chapter in which the bookmark belongs.
        /// </summary>
        public int ChapterIndex;

        /// <summary>
        /// Variation Tree in which the bookmark belongs.
        /// </summary>
        public VariationTree Tree;

        /// <summary>
        /// Index of the article in which this bookmark belongs.
        /// It is only needed for GUI display and orgerening
        /// </summary>
        public int ArticleIndex;

        /// <summary>
        /// Bookmark's node.
        /// </summary>
        public TreeNode Node
        {
            get => Bookmark.Node;
        }

        /// <summary>
        /// Type of Tree this bookmark is in.
        /// </summary>
        public GameData.ContentType ContentType
        {
            get => Tree.Header.GetContentType(out _);
        }

        /// <summary>
        /// Creates this object.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="bm"></param>
        /// <param name="articleIndex"></param>
        public BookmarkWrapper(int chapterIndex, VariationTree tree, Bookmark bm, int articleIndex)
        {
            ChapterIndex = chapterIndex;
            Tree = tree;
            Bookmark = bm;
            ArticleIndex = articleIndex;
        }

        /// <summary>
        /// Comparator to use when sorting by move number and color.
        /// </summary>
        /// <param name="bm"></param>
        /// <returns></returns>
        public int CompareTo(BookmarkWrapper bm)
        {
            if (bm == null)
                return -1;

            if (this.ContentType == GameData.ContentType.STUDY_TREE && bm.ContentType != GameData.ContentType.STUDY_TREE)
            {
                return -1;
            }
            else if (this.ContentType == GameData.ContentType.MODEL_GAME)
            {
                if (bm.ContentType == GameData.ContentType.STUDY_TREE)
                {
                    return 1;
                }
                else if (bm.ContentType == GameData.ContentType.EXERCISE)
                {
                    return -1;
                }
            }
            else if (this.ContentType == GameData.ContentType.EXERCISE && bm.ContentType != GameData.ContentType.EXERCISE)
            {
                return 1;
            }

            if (this.ContentType == bm.ContentType && this.ArticleIndex != bm.ArticleIndex)
            {
                return this.ArticleIndex - bm.ArticleIndex;
            }

            if (this.ChapterIndex != bm.ChapterIndex)
            {
                return bm.ChapterIndex - bm.ChapterIndex;
            }

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

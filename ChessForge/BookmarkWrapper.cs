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
    public class BookmarkWrapper
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
        /// Indedx of the article in which this bookmarl belongs.
        /// It is only need for GUI display and orgerening
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

    }
}

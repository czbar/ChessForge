using GameTree;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChessForge
{
    /// <summary>
    /// Represents an Article in a structure convenient
    /// for showing in a ListView control.
    /// </summary>
    public class DuplicateListItem
    {
        /// <summary>
        /// Constructs the object and sets the underlying ArticleListItem
        /// </summary>
        /// <param name="item"></param>
        public DuplicateListItem(ArticleListItem item)
        {
            ArticleItem = item;
        }

        /// <summary>
        /// ArticleListItem represented by this object.
        /// If null, the hosting ListView will interpret this as an empty line item.
        /// </summary>
        public ArticleListItem ArticleItem;

        // id of the duplicate set within the entire list
        private int _duplicateNo = 0;

        // flags if this item is an "original"
        private bool _isOriginal = false;

        // selection flag
        private bool _isSelected = false;

        /// <summary>
        /// Item text to show in the hosting ListView.
        /// </summary>
        public string ItemText
        {
            get
            {
                if (ArticleItem == null)
                {
                    return "";
                }
                else
                {
                    return ArticleItem.ArticleTitleForDuplicateList;
                }
            }
        }

        /// <summary>
        /// Id of the subset of duplicates this item belongs to.
        /// </summary>
        public int DuplicateNo
        {
            get => _duplicateNo;
            set => _duplicateNo = value;
        }

        /// <summary>
        /// Flags whether this item is considered an "original"
        /// </summary>
        public bool IsOriginal
        {
            get => _isOriginal;
            set => _isOriginal = value;
        }

        /// <summary>
        /// Index of the chapter from which the hosted Article comes.
        /// </summary>
        public int ChapterIndex
        {
            get => ArticleItem.ChapterIndex;
        }

        /// <summary>
        /// ArticleIndex in its chapter.
        /// </summary>
        public int ArticleIndex
        {
            get => ArticleItem.ArticleIndex;
        }

        /// <summary>
        /// Content type of the hosted Article.
        /// </summary>
        public GameData.ContentType ContentType
        {
            get => ArticleItem.ContentType;
        }

        /// <summary>
        /// Selected flag.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => _isSelected = value;
        }

        /// <summary>
        /// Visibility status that will determine whether to show the selection check box or not.
        /// We are not showing it on empty lines.
        /// </summary>
        public string Visibility
        {
            get => ArticleItem == null ? "Collapsed" : "Visible";
        }
    }
}

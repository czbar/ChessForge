using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessForge
{
    /// <summary>
    /// An article being subject to a drag-and-drop action.
    /// </summary>
    public class DraggedArticle
    {
        /// <summary>
        /// Flags if the drag operation is in progress.
        /// </summary>
        public static bool IsDragInProgress;

        /// <summary>
        /// Type of the article being dragged.
        /// </summary>
        public static GameData.ContentType ContentType { get; set; }

        /// <summary>
        /// Chapter 
        /// </summary>
        public static int ChapterIndex { get; set; }

        /// <summary>
        /// The article being dragged.
        /// </summary>
        public static int ArticleIndex { get; set; }

        /// <summary>
        /// Whether entering the drag state is prohibited
        /// e.g. because mouse entered with a left button down before
        /// hovering over any itmem.
        /// </summary>
        public static bool IsBlocked
        {
            get => _isBlocked;
            set => _isBlocked = value;
        }

        /// whether entering the drag state is prohibited
        private static bool _isBlocked;

        /// <summary>
        /// Return true if the dragged object is chapter
        /// </summary>
        /// <returns></returns>
        public static bool IsChapterDragged()
        {
            return ContentType == GameData.ContentType.NONE && ArticleIndex == -1 && ChapterIndex >= 0;
        }

        /// <summary>
        /// Set the variables for the started drag operation.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="article"></param>
        public static bool StartDragOperation(int chapterIndex, int articleIndex, GameData.ContentType content)
        {
            if (IsBlocked)
            {
                return false;
            }
            else
            {
                ArticleIndex = articleIndex;
                ChapterIndex = chapterIndex;
                ContentType = content;
                IsDragInProgress = true;
                return true;
            }
        }

        /// <summary>
        /// Resets the drag variables
        /// </summary>
        public static void StopDragOperation()
        {
            IsDragInProgress = false;
            ArticleIndex = -1;
            ChapterIndex = -1;
        }
    }
}

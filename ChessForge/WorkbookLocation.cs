using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Identifies a location with a Workbook.
    /// Includes the type of view (tab) and article id
    /// </summary>
    public class WorkbookLocation
    {
        // Guid of the chapter
        private string _chapterGuid;

        // Type of the view
        private TabViewType _viewType;

        // Guid of the article where applicable
        private string _articleGuid;

        // Index of the article where applicable
        private int _articleIndex;

        /// <summary>
        /// Guid of the chapter
        /// </summary>
        public string ChapterGuid
        {
            get { return _chapterGuid; }
        }

        /// <summary>
        /// Index of the article where applicable
        /// </summary>
        public int ArticleIndex
        {
            get
            {
                return _articleIndex;
            }
        }

        /// <summary>
        /// Guid of the article where applicable
        /// </summary>
        public string ArticleGuid
        {
            get
            {
                return _articleGuid;
            }
        }

        /// <summary>
        /// Type of the view
        /// </summary>
        public TabViewType ViewType
        {
            get
            {
                return _viewType;
            }
        }

        /// <summary>
        /// Constructs the location object.
        /// </summary>
        /// <param name="chapterGuid"></param>
        /// <param name="viewType"></param>
        /// <param name="articleGuid"></param>
        public WorkbookLocation(string chapterGuid, TabViewType viewType, string articleGuid, int articleIndex)
        {
            _chapterGuid = chapterGuid;
            _viewType = viewType;
            _articleGuid = articleGuid;
            _articleIndex = articleIndex;
        }

    }
}

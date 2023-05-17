using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private WorkbookManager.TabViewType _viewType;

        // Guid of the article where applicable
        private string _articleGuid;

        /// <summary>
        /// Guid of the chapter
        /// </summary>
        public string ChapterGuid
        {
            get { return _chapterGuid; }
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
        public WorkbookManager.TabViewType ViewType
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
        public WorkbookLocation(string chapterGuid, WorkbookManager.TabViewType viewType, string articleGuid)
        {
            _chapterGuid = chapterGuid;
            _viewType = viewType;
            _articleGuid = articleGuid;
        }

    }
}

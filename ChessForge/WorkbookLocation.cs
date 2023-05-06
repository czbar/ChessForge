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
        /// <summary>
        /// Guid of the chapter
        /// </summary>
        public string ChapterGuid;

        /// <summary>
        /// Type of the view
        /// </summary>
        public WorkbookManager.TabViewType ViewType;

        /// <summary>
        /// Guid of the article where applicable
        /// </summary>
        public string ArticleGuid;
    }
}

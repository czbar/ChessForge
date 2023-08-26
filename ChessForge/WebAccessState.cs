using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Holds state of Explorer queries.
    /// An explorer query is either a request for Opening Stats
    /// or Tablebase. The program chooses which one to send based 
    /// on the number of pieces in the position.
    /// </summary>
    public class WebAccessState
    {
        /// <summary>
        /// Whether Explorer queries are enabled.
        /// </summary>
        public static bool IsEnabledExplorerQueries
        {
            get
            {
                return _isEnabledExplorerQueries;
            }
            set
            {
                _isEnabledExplorerQueries = value;
                QueuedNode = null;
                QueuedNodeTreeId = 0;
                IsExplorerRequestInProgress = false;
            }
        }

        /// <summary>
        /// A node that had to be queued while another request
        /// was in porogress
        /// </summary>
        public static TreeNode QueuedNode = null;

        /// <summary>
        /// Id of the Tree to which the queued node belongs.
        /// </summary>
        public static int QueuedNodeTreeId = 0;

        /// <summary>
        /// Whether there is an Explorer query in progress,
        /// </summary>
        public static bool IsExplorerRequestInProgress = false;

        /// <summary>
        /// Whether the Explorer Queries handlers have been initialized
        /// </summary>
        public static bool IsExplorerQueriesInitialized = false;

        // whether Explorer querying is enabled
        private static bool _isEnabledExplorerQueries = false;
    }
}

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
                WaitingNode = null;
                WaitingNodeTreeId = 0;
                IsWaitingForResults = false;
            }
        }

        /// <summary>
        /// A node for the waiting request.
        /// </summary>
        public static TreeNode WaitingNode = null;

        /// <summary>
        /// Id of the Tree for the waiting request.
        /// </summary>
        public static int WaitingNodeTreeId = 0;

        /// <summary>
        /// Whether the mandatory wait period from the previous request is on.
        /// </summary>
        public static bool IsMandatoryDelayOn = false;

        /// <summary>
        /// Whether there is an Explorer query in progress,
        /// </summary>
        public static bool IsWaitingForResults = false;

        /// <summary>
        /// Whether there is a request waiting to be processed.
        /// </summary>
        public static bool HasWaitingRequest = false;

        /// <summary>
        /// Whether the Explorer Queries handlers have been initialized
        /// </summary>
        public static bool IsExplorerQueriesInitialized = false;

        // whether Explorer querying is enabled
        private static bool _isEnabledExplorerQueries = false;
    }
}

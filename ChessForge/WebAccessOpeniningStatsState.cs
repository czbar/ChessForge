using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Holds state of OpeningStats query
    /// </summary>
    public class WebAccessOpeniningStatsState
    {
        /// <summary>
        /// Whether querying Opening Stats is enabled.
        /// </summary>
        public static bool IsEnabledOpeningStats
        {
            get
            {
                return _isEnabledOpeningStats;
            }
            set
            {
                _isEnabledOpeningStats = value;
                QueuedNode = null;
                QueuedNodeTreeId = 0;
                IsOpeningStatsRequestInProgress = false;
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
        /// Whether there is an OpeningStats query in progress,
        /// </summary>
        public static bool IsOpeningStatsRequestInProgress = false;

        /// <summary>
        /// Whether the OpeningStats handlers have been initialized
        /// </summary>
        public static bool IsOpeningStatsInitialized = false;

        // whether querying opening stats is enabled
        private static bool _isEnabledOpeningStats = false;
    }
}

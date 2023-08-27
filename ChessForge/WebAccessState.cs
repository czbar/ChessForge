using GameTree;
using System;
using System.Collections.Generic;

namespace ChessForge
{
    /// <summary>
    /// Maintains the state of Explorer queries.
    /// An explorer query is either a request for Opening Stats or Tablebase. 
    /// The program chooses which one to send based on the number of pieces in the position.
    /// The "Ready" node/request is the one that is either currently being processed or
    /// will be processed when the next polling event comes around.
    /// The "Queued" node/request will replace the "Ready" one as soon as the next polling event comes around.
    /// Conceptually, the Queued request immediately replaces the "Ready" now but for state maintenance reasons
    /// we need to allow the polling handler to do that.
    /// </summary>
    public class WebAccessState
    {
        /// <summary>
        /// Flags whether Explorer queries are enabled.
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
                ReadyNode = null;
                ReadyNodeTreeId = 0;
                IsWaitingForResults = false;
            }
        }

        /// <summary>
        /// Flags whether the mandatory wait period from the previous request is on.
        /// </summary>
        public static bool IsMandatoryDelayOn = false;

        /// <summary>
        /// A node for the current/ready request.
        /// </summary>
        public static TreeNode ReadyNode = null;

        /// <summary>
        /// Id of the Tree for the current/ready request.
        /// </summary>
        public static int ReadyNodeTreeId = 0;

        /// <summary>
        /// Whether there is an Explorer query in progress.
        /// </summary>
        public static bool IsWaitingForResults = false;

        /// <summary>
        /// Whether there is a request ready for immediate processing.
        /// </summary>
        public static bool HasReadyRequest = false;

        /// <summary>
        /// A node for the queued request.
        /// </summary>
        public static TreeNode QueuedNode = null;

        /// <summary>
        /// Id of the Tree for the queued request.
        /// </summary>
        public static int QueuedNodeTreeId = 0;

        /// <summary>
        /// Whether there is a queued request.
        /// </summary>
        public static bool HasQueuedRequest = false;

        /// <summary>
        /// Whether the Explorer Queries handlers have been initialized
        /// </summary>
        public static bool IsExplorerQueriesInitialized = false;

        // whether Explorer querying is enabled
        private static bool _isEnabledExplorerQueries = false;
    }
}

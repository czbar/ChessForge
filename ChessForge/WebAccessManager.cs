using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessPosition;
using WebAccess;

namespace ChessForge
{
    /// <summary>
    /// Manages communication with WebAccess library
    /// </summary>
    public class WebAccessManager
    {
        /// <summary>
        /// Whether querying Opening Stats is enabled.
        /// </summary>
        public static bool IsEnabledOpeningStats
        {
            get => WebAccessOpeniningStatsState.IsEnabledOpeningStats;
            set => WebAccessOpeniningStatsState.IsEnabledOpeningStats = value;
        }

        /// <summary>
        /// Calls WebAccess to retrieve Opening Stats.
        /// </summary>
        /// <param name="treeId"></param>
        /// <param name="nd"></param>
        public static void RequestOpeningStats(int treeId, TreeNode nd)
        {
            if (IsEnabledOpeningStats)
            {
                if (!WebAccessOpeniningStatsState.IsOpeningStatsInitialized)
                {
                    InitializeOpeningStats();
                }

                if (WebAccessOpeniningStatsState.IsOpeningStatsRequestInProgress)
                {
                    WebAccessOpeniningStatsState.QueuedNode = nd;
                    WebAccessOpeniningStatsState.QueuedNodeTreeId = treeId;
                }
                else
                {
                    WebAccessOpeniningStatsState.IsOpeningStatsRequestInProgress = true;
                    WebAccessOpeniningStatsState.QueuedNode = null;
                    OpeningExplorer.OpeningStats(treeId, nd);
                }
            }
        }

        /// <summary>
        /// Sets the event handling delegate.
        /// </summary>
        private static void InitializeOpeningStats()
        {
            OpeningExplorer.DataReceived += OpeningStatsRequestCompleted;
            WebAccessOpeniningStatsState.IsOpeningStatsInitialized = true;
        }

        /// <summary>
        /// Delegate listening to the OpeningStats request completed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OpeningStatsRequestCompleted(object sender, WebAccessEventArgs e)
        {
            WebAccessOpeniningStatsState.IsOpeningStatsRequestInProgress = false;

            // check if we have anything queued and if so run it
            if (WebAccessOpeniningStatsState.QueuedNode != null)
            {
                RequestOpeningStats(0, WebAccessOpeniningStatsState.QueuedNode);
                WebAccessOpeniningStatsState.QueuedNode = null;
            }
        }
    }
}

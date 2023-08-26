using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessPosition;
using WebAccess;
using System.Web;

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
        public static bool IsEnabledExplorerQueries
        {
            get => WebAccessState.IsEnabledExplorerQueries;
            set => WebAccessState.IsEnabledExplorerQueries = value;
        }

        /// <summary>
        /// Calls WebAccess with an Explorer Query.
        /// </summary>
        /// <param name="treeId"></param>
        /// <param name="nd"></param>
        public static void ExplorerRequest(int treeId, TreeNode nd, bool force = false)
        {
            if (nd != null)
            {
                if (IsEnabledExplorerQueries)
                {
                    if (!WebAccessState.IsExplorerQueriesInitialized)
                    {
                        InitializeExplorerQueries();
                    }

                    if (WebAccessState.IsExplorerRequestInProgress)
                    {
                        WebAccessState.QueuedNode = nd;
                        WebAccessState.QueuedNodeTreeId = treeId;
                    }
                    else
                    {
                        WebAccessState.IsExplorerRequestInProgress = true;
                        WebAccessState.QueuedNode = null;
                        int pieceCount = PositionUtils.GetPieceCount(nd.Position);
                        if (pieceCount > 7)
                        {
                            OpeningExplorer.RequestOpeningStats(treeId, nd, force);
                        }
                        else
                        {
                            OpeningExplorer.ResetLastRequestedFen();
                            TablebaseExplorer.RequestTablebaseData(treeId, nd, force);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the event handling delegate.
        /// </summary>
        private static void InitializeExplorerQueries()
        {
            OpeningExplorer.OpeningStatsReceived += ExplorerRequestCompleted;
            OpeningExplorer.OpeningStatsRequestIgnored += ExplorerRequestCompleted;
            TablebaseExplorer.TablebaseReceived += ExplorerRequestCompleted;
            WebAccessState.IsExplorerQueriesInitialized = true;
        }

        /// <summary>
        /// Delegate listening to the Explorer request completed events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ExplorerRequestCompleted(object sender, WebAccessEventArgs e)
        {
            WebAccessState.IsExplorerRequestInProgress = false;

            // check if we have anything queued and if so run it
            if (WebAccessState.QueuedNode != null)
            {
                ExplorerRequest(0, WebAccessState.QueuedNode);
                WebAccessState.QueuedNode = null;
            }
        }
    }
}

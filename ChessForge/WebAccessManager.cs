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
    /// Manages communication with WebAccess library.
    /// </summary>
    public class WebAccessManager
    {
        // number of pulses since last Web Access request before a new one can be issued.
        private static readonly int MANDATORY_DELAY_PULSES_COUNT = 2;

        // counts pulse events since last web access 
        private static int _webAccessCounter;

        // lock to prevent clashing of UpdateWebProcessing and RequestWebAccess
        private static object _lockWebUpdate = new object();

        /// <summary>
        /// This method is called by the client requesting Explorer data from a web site.
        /// It will be checked against the current state and executed, queued or discarded.
        /// </summary>
        /// <param name="treeId"></param>
        /// <param name="nd"></param>
        /// <param name="force">force execution even if this is the same node as last time. </param>
        public static void ExplorerRequest(int treeId, TreeNode nd, bool force = false)
        {
            RequestWebAccess(treeId, nd, force);
        }

        /// <summary>
        /// Invoked from the PulseManager after a PULSE timer event.
        /// If there is a ready request it will be executed if sufficient time
        /// since the last request has passed.
        /// </summary>
        public static void UpdateWebAccess()
        {
            _webAccessCounter++;

            lock (_lockWebUpdate)
            {
                // if there is a new queued request, and we are not in "mandatory delay", slot it in for immediate execution,
                // we are no longer interested in the current one
                if (WebAccessState.HasQueuedRequest && !WebAccessState.IsMandatoryDelayOn)
                {
                    WebAccessState.ReadyNode = WebAccessState.QueuedNode;
                    WebAccessState.ReadyNodeTreeId = WebAccessState.QueuedNodeTreeId;
                    WebAccessState.HasQueuedRequest = false;
                    WebAccessState.HasReadyRequest = true;
                    WebAccessState.IsWaitingForResults = true;
                }
            }

            // if we have a request in progress check for results
            if (WebAccessState.IsWaitingForResults)
            {
                LichessOpeningsStats stats = CheckResults();
                if (stats != null)
                {
                    AppLog.Message(2, "Received Web Data for: " + WebAccessState.ReadyNode == null ? "???" : WebAccessState.ReadyNode.LastMoveAlgebraicNotation);
                    WebAccessState.IsWaitingForResults = false;
                    AppState.MainWin.Dispatcher.Invoke(() =>
                    {
                        AppState.MainWin.OpeningStatsView.OpeningStatsReceived(stats, WebAccessState.ReadyNode, WebAccessState.ReadyNodeTreeId);
                        AppState.MainWin.TopGamesView.TopGamesReceived(stats);
                    });
                }
            }

            // reset the "mandatory delay" period, if it is time to do so
            if (_webAccessCounter > MANDATORY_DELAY_PULSES_COUNT)
            {
                WebAccessState.IsMandatoryDelayOn = false;
            }

            // if we are not in the "mandatory delay", check if we have a request to execute
            if (!WebAccessState.IsMandatoryDelayOn)
            {
                _webAccessCounter = 0;
                if (WebAccessState.HasReadyRequest)
                {
                    WebAccessState.IsMandatoryDelayOn = true;
                    AppLog.Message(2, "Execute Web Request for: " + (WebAccessState.ReadyNode == null ? "null" : WebAccessState.ReadyNode.LastMoveAlgebraicNotation));
                    ExecuteRequest(WebAccessState.ReadyNodeTreeId, WebAccessState.ReadyNode);
                    WebAccessState.HasReadyRequest = false;
                }
            }
        }

        /// <summary>
        /// Sets a queued request that will be picked up later on by UpdateWebAccess()
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool RequestWebAccess(int treeId, TreeNode nd, bool force = false)
        {
            if (nd != null)
            {
                lock (_lockWebUpdate)
                {
                    if (force || nd != WebAccessState.ReadyNode)
                    {
                        AppLog.Message(2, "Requesting Web Access for:" + nd.LastMoveAlgebraicNotation);

                        if (WebAccessState.ReadyNode != null)
                        {
                            AppLog.Message(2, "Cancelling Web Access for:" + WebAccessState.ReadyNode.LastMoveAlgebraicNotation);
                        }

                        WebAccessState.QueuedNode = nd;
                        WebAccessState.QueuedNodeTreeId = treeId;
                        WebAccessState.HasQueuedRequest = true;

                        if (WebAccessState.IsMandatoryDelayOn)
                        {
                            // reset the "mandatory delay" counter.
                            // this is to prevent processing of requests when too many come in a short
                            // period of time e.g. when the user keep the right arrow depressed.
                            _webAccessCounter = 0;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Whether querying Opening Stats is enabled.
        /// </summary>
        public static bool IsEnabledExplorerQueries
        {
            get => WebAccessState.IsEnabledExplorerQueries;
            set => WebAccessState.IsEnabledExplorerQueries = value;
        }

        /// <summary>
        /// Checks if the results are already in the cache.
        /// </summary>
        /// <returns></returns>
        private static LichessOpeningsStats CheckResults()
        {
            return WebAccess.OpeningExplorer.GetOpeningStats(WebAccessState.ReadyNode);
        }

        /// <summary>
        /// Executes a Web request.
        /// This will be invoked from PulseManager once the web query
        /// is greenlit for execution.
        /// </summary>
        /// <param name="treeId"></param>
        /// <param name="nd"></param>
        /// <param name="force"></param>
        private static void ExecuteRequest(int treeId, TreeNode nd)
        {
            if (nd != null)
            {
                try
                {
                    AppState.MainWin.Dispatcher.Invoke(() =>
                    {
                        WebAccessState.IsWaitingForResults = true;

                        if (IsEnabledExplorerQueries)
                        {
                            int pieceCount = PositionUtils.GetPieceCount(nd.Position);
                            if (pieceCount > 7)
                            {
                                if (OpeningExplorer.GetOpeningStats(nd) == null)
                                {
                                    OpeningExplorer.RequestOpeningStats(treeId, nd);
                                }
                            }
                            else
                            {
                                OpeningExplorer.ResetLastRequestedFen();
                                TablebaseExplorer.RequestTablebaseData(treeId, nd);
                            }
                        }
                    });
                }
                catch { }
            }
        }
    }
}

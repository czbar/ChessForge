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
        /// <param name="force"></param>
        public static void ExplorerRequest(int treeId, TreeNode nd, bool force = false)
        {
            RequestWebAccess(treeId, nd, force);
        }

        /// <summary>
        /// Invoked from the PulseManager after a PULSE timer event.
        /// If there is a waiting request it will be executed if sufficient time
        /// since the last request has passed.
        /// If there is no waiting request and the sufficient time since the last
        /// request has passed the request will be executed.
        /// </summary>
        public static void UpdateWebAccess()
        {
            _webAccessCounter++;

            lock (_lockWebUpdate)
            {
                // if there a new queued request, and we are not in "mandatory delay", slot it in for immediate execution,
                // we are no longer interested in the current one
                if (WebAccessState.HasQueuedRequest && !WebAccessState.IsMandatoryDelayOn)
                {
                    WebAccessState.WaitingNode = WebAccessState.QueuedNode;
                    WebAccessState.WaitingNodeTreeId = WebAccessState.QueuedNodeTreeId;
                    WebAccessState.HasQueuedRequest = false;
                    WebAccessState.HasWaitingRequest = true;
                    WebAccessState.IsWaitingForResults = true;
                }
            }

            // if we have a request in progress check for results
            if (WebAccessState.IsWaitingForResults)
            {
                LichessOpeningsStats stats = CheckResults();
                if (stats != null)
                {
                    AppLog.Message(2, "Received Web Data for: " + WebAccessState.WaitingNode.LastMoveAlgebraicNotation);
                    WebAccessState.IsWaitingForResults = false;
                    AppState.MainWin.Dispatcher.Invoke(() =>
                    {
                        AppState.MainWin.OpeningStatsView.OpeningStatsReceived(stats, WebAccessState.WaitingNode, WebAccessState.WaitingNodeTreeId);
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
                if (WebAccessState.HasWaitingRequest)
                {
                    WebAccessState.IsMandatoryDelayOn = true;
                    AppLog.Message(2, "Execute Web Request for: " + WebAccessState.WaitingNode.LastMoveAlgebraicNotation);
                    ExecuteRequest(WebAccessState.WaitingNodeTreeId, WebAccessState.WaitingNode);
                    WebAccessState.HasWaitingRequest = false;
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
            lock (_lockWebUpdate)
            {
                if (force || nd != WebAccessState.WaitingNode)
                {
                    AppLog.Message(2, "Requesting Web Access for:" + nd.LastMoveAlgebraicNotation);

                    if (WebAccessState.WaitingNode != null)
                    {
                        AppLog.Message(2, "Cancelling Web Access for:" + WebAccessState.WaitingNode.LastMoveAlgebraicNotation);
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
            return WebAccess.OpeningExplorer.GetOpeningStats(WebAccessState.WaitingNode);
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

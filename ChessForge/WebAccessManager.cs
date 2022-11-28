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
        public static bool IsEnabledExplorerQueries
        {
            get => WebAccessExplorersState.IsEnabledExplorerQueries;
            set => WebAccessExplorersState.IsEnabledExplorerQueries = value;
        }

        /// <summary>
        /// Calls WebAccess with an Explorer Query.
        /// </summary>
        /// <param name="treeId"></param>
        /// <param name="nd"></param>
        public static void ExplorerRequest(int treeId, TreeNode nd)
        {
            if (IsEnabledExplorerQueries)
            {
                if (!WebAccessExplorersState.IsExplorerQueriesInitialized)
                {
                    InitializeExplorerQueries();
                }

                if (WebAccessExplorersState.IsExplorerRequestInProgress)
                {
                    WebAccessExplorersState.QueuedNode = nd;
                    WebAccessExplorersState.QueuedNodeTreeId = treeId;
                }
                else
                {
                    WebAccessExplorersState.IsExplorerRequestInProgress = true;
                    WebAccessExplorersState.QueuedNode = null;
                    int pieceCount = PositionUtils.GetPieceCount(nd.Position);
                    if (pieceCount > 7)
                    {
                        OpeningExplorer.OpeningStats(treeId, nd);
                    }
                    else
                    {
                        TablebaseExplorer.TablebaseRequest(treeId, nd);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the event handling delegate.
        /// </summary>
        private static void InitializeExplorerQueries()
        {
            OpeningExplorer.DataReceived += ExplorerRequestCompleted;
            TablebaseExplorer.DataReceived += ExplorerRequestCompleted;
            WebAccessExplorersState.IsExplorerQueriesInitialized = true;
        }

        /// <summary>
        /// Delegate listening to the Explorer request completed events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ExplorerRequestCompleted(object sender, WebAccessEventArgs e)
        {
            WebAccessExplorersState.IsExplorerRequestInProgress = false;

            // check if we have anything queued and if so run it
            if (WebAccessExplorersState.QueuedNode != null)
            {
                ExplorerRequest(0, WebAccessExplorersState.QueuedNode);
                WebAccessExplorersState.QueuedNode = null;
            }
        }
    }
}

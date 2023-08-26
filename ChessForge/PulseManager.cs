using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Manages various actions and states in response to 
    /// a PULSE timer event.
    /// </summary>
    public class PulseManager
    {
        // number of pulses since last Web Access request before a new one can be issued.
        private static readonly int WEB_ACCESS_PULSES_COUNT = 2;

        // flags whether we are permitted to send a Web request
        private static bool _isWebAccessPermitted;

        // counts pulse events since last web access 
        private static int _webAccessCounter;

        // whether we have a Web access permitted recently
        private static bool _isWebAccessInProgress;

        /// <summary>
        /// Handles the PULSE timer event.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void PulseEventHandler(object source, ElapsedEventArgs e)
        {
            if (_isWebAccessInProgress)
            {
                _webAccessCounter++;
            }
            else
            {
                _webAccessCounter = 0;
            }

            UpdateEvaluationBar();
        }

        /// <summary>
        /// Invoked by the WebAccessManager.
        /// The request will be processed or queued depending on when the last
        /// request was issued.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool RequestWebAccess(TreeNode node)
        {
            if (!_isWebAccessInProgress || _webAccessCounter > WEB_ACCESS_PULSES_COUNT)
            {
                _webAccessCounter = 0;
                WebAccessExplorersState.IsExplorerRequestInProgress = true;
                WebAccessExplorersState.QueuedNode = null;
                return true;
            }
            else
            {
                WebAccessExplorersState.QueuedNode = node;
                return false;
            }
        }

        /// <summary>
        /// Updates the position of the evaluation bar.
        /// </summary>
        private static void UpdateEvaluationBar()
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                if (AppState.IsVariationTreeTabType
                || TrainingSession.IsTrainingInProgress
                   && (EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE || EvaluationManager.CurrentMode == EvaluationManager.Mode.CONTINUOUS)
               )
                {
                    TreeNode nd = null;
                    if (TrainingSession.IsTrainingInProgress)
                    {
                        nd = EvaluationManager.GetEvaluatedNode(out _);
                    }
                    else if (AppState.MainWin.ActiveTreeView != null)
                    {
                        nd = AppState.MainWin.ActiveTreeView.GetSelectedNode();
                        if (nd == null && AppState.MainWin.ActiveVariationTree != null && AppState.MainWin.ActiveVariationTree.Nodes.Count > 0)
                        {
                            nd = AppState.MainWin.ActiveVariationTree.Nodes[0];
                        }
                    }

                    EvaluationBar.ShowEvaluation(nd);
                }
                else
                {
                    EvaluationBar.Show(false);
                }
            });
        }

    }
}

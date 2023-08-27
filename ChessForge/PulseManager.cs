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
        /// <summary>
        /// Handles the PULSE timer event.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void PulseEventHandler(object source, ElapsedEventArgs e)
        {
            WebAccessManager.UpdateWebAccess();
            UpdateEvaluationBar();
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

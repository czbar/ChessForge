using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace ChessForge
{
    public class EvaluationActiveLine : EvaluationLine
    {
        /// <summary>
        /// Returns the Node currently being evaluated.
        /// </summary>
        /// <returns></returns>
        public override TreeNode GetCurrentEvaluatedNode()
        {
            return AppStateManager.MainWin.ActiveLine.GetNodeAtIndex(NodeIndex);
        }

        /// <summary>
        /// Returns true if the current evaluated node is last
        /// on the list.
        /// </summary>
        /// <returns></returns>
        public override bool IsLastPositionIndex()
        {
            return NodeIndex == AppStateManager.MainWin.ActiveLine.GetPlyCount() - 1;
        }

        /// <summary>
        /// Returns the next node to evaluate
        /// or null if we reached the end of the list.
        /// If there are no more Nodes to evaluate,
        /// the lists are reset.
        /// </summary>
        /// <returns></returns>
        public override TreeNode GetNextNodeToEvaluate()
        {
            NodeIndex++;
            return GetCurrentEvaluatedNode();
        }

        /// <summary>
        /// Clears the list of Nodes and Runs.
        /// Resets the evaluation index.
        /// </summary>
        public override void ResetsNodesToEvaluate()
        {
            NodeIndex = 0;
        }
    }
}

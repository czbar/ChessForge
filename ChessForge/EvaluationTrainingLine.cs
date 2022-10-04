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
    /// <summary>
    /// Nodes and Runs to evaluate when evaluating a line
    /// from the Training view.
    /// </summary>
    public class EvaluationTrainingLine : EvaluationLine
    {
        // The list of Runs to evaluate when we are evaluating a line
        // in the Training mode.
        private List<Run> _runsToEvaluate = new List<Run>();

        // The lists of Nodes corresponding to the Runs in _runsToEvaluate
        private List<TreeNode> _nodesToEvaluate = new List<TreeNode>();

        public EvaluationTrainingLine()
        {
            NodeIndex = -1;
        }

        /// <summary>
        /// Returns the Run currently being evaluated.
        /// </summary>
        /// <returns></returns>
        public override Run GetCurrentEvaluatedRun()
        {
            if (NodeIndex < 0 || NodeIndex >= _runsToEvaluate.Count)
            {
                return null;
            }

            return _runsToEvaluate[NodeIndex];
        }

        /// <summary>
        /// Returns the Node currently being evaluated.
        /// </summary>
        /// <returns></returns>
        public override TreeNode GetCurrentEvaluatedNode()
        {
            if (NodeIndex < 0 || NodeIndex >= _runsToEvaluate.Count)
            {
                return null;
            }

            return _nodesToEvaluate[NodeIndex];
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
            if (NodeIndex < _nodesToEvaluate.Count)
            {
                return _nodesToEvaluate[NodeIndex];
            }
            else
            {
                ResetsNodesToEvaluate();
                return null;
            }
        }
        /// <summary>
        /// Returns true if the current evaluated node is last
        /// on the list.
        /// </summary>
        /// <returns></returns>
        public override bool IsLastPositionIndex()
        {
            return NodeIndex == _nodesToEvaluate.Count - 1;
        }

        /// <summary>
        /// Adds a Run to the list of _runsToEvaluate.
        /// At the same time, adds the corresponding Node
        /// to the list of Nodes.
        /// </summary>
        /// <param name="r"></param>
        public override void AddRunToEvaluate(Run r)
        {
            int nodeId = TextUtils.GetNodeIdFromPrefixedString(r.Name);
            TreeNode nd = AppStateManager.MainWin.ActiveVariationTree.GetNodeFromNodeId(nodeId);

            _nodesToEvaluate.Add(nd);
            _runsToEvaluate.Add(r);
        }

        /// <summary>
        /// Clears the list of Nodes and Runs.
        /// Resets the evaluation index.
        /// </summary>
        public override void ResetsNodesToEvaluate()
        {
            _runsToEvaluate.Clear();
            _nodesToEvaluate.Clear();
            NodeIndex = -1;
        }
    }
}

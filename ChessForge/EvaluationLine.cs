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
    /// Base class for evaluation lines
    /// </summary>
    public abstract class EvaluationLine
    {
        // index of the node in the list of nodes
        protected int NodeIndex;

        /// <summary>
        /// Returns the Node currently being evaluated.
        /// </summary>
        /// <returns></returns>
        public abstract TreeNode GetCurrentEvaluatedNode();

        /// <summary>
        /// Returns the next node to evaluate
        /// or null if we reached the end of the list.
        /// If there are no more Nodes to evaluate,
        /// the list is reset.
        /// </summary>
        /// <returns></returns>
        public abstract TreeNode GetNextNodeToEvaluate();

        /// <summary>
        /// Returns true if the current evaluated node is last
        /// on the list.
        /// </summary>
        /// <returns></returns>
        public abstract bool IsLastPositionIndex();

        /// <summary>
        /// Clears the list of Nodes and Runs.
        /// Resets the evaluation index.
        /// </summary>
        public abstract void ResetsNodesToEvaluate();

        /// <summary>
        /// Returns the Run currently being evaluated.
        /// The base implementation returns null.
        /// Derived classes that deal with Runs must
        /// implement it
        /// </summary>
        /// <returns></returns>
        public virtual Run GetCurrentEvaluatedRun()
        {
            return null;
        }

        /// <summary>
        /// Adds a Run to the list of _runsToEvaluate.
        /// At the same time, adds the corresponding Node to the list of Nodes.
        /// The base implementation does nothing.
        /// Derived classes that deal with Runs must
        /// implement it
        /// </summary>
        /// <param name="r"></param>
        public virtual void AddRunToEvaluate(Run r)
        {
        }

        /// <summary>
        /// Sets the index in the list of nodes from which to start evaluation.
        /// </summary>
        /// <param name="index"></param>
        public void SetStartNodeIndex(int index)
        {
            NodeIndex = index;
        }

        public int GetNodeIndex()
        {
            return NodeIndex;
        }

    }
}

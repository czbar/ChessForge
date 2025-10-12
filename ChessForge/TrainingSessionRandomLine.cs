using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessForge
{
    /// <summary>
    /// This file holds partial class definition for TrainingSession
    /// that manages random line selection and maintenance.
    /// </summary>
    public partial class TrainingSession
    {
        // Training status of the tree.
        private static TreeTrainingStatus _trainingStatusTree;

        // List of nodes that are workbook forks for use in selecgting random lines.
        private static List<NodeTrainingStatus> _trainingForks;

        // Root node status for easy access.
        private static NodeTrainingStatus _ntsRoot;

        /// <summary>
        /// Builds the training status tree and identifies all forks for random line selection.
        /// </summary>
        public static void InitializeRandomLines()
        {
            _trainingStatusTree = new TreeTrainingStatus(AppState.MainWin.ActiveVariationTree, TrainingSide, StartPosition);
            _trainingForks = new List<NodeTrainingStatus>();

            _ntsRoot = _trainingStatusTree.GetNodeStatusById(StartPosition.NodeId);
            IdentifyTrainingForks(_ntsRoot);
        }

        /// <summary>
        /// Whether there are any random lines available.
        /// </summary>
        /// <returns></returns>
        public static bool HasRandomLines()
        {
            return _trainingForks != null && _trainingForks.Count > 0;
        }

        /// <summary>
        /// Finds a random not exhausted training fork,
        /// selects its random not trained child
        /// and builds a line from the root to that child.
        /// </summary>
        /// <param name="currNode"></param>
        /// <returns></returns>
        public static List<TreeNode> SelectRandomLine()
        {
            List<TreeNode> line = new List<TreeNode>();
            
            NodeTrainingStatus fork = SelectRandomNotExhaustedFork();
            
            if (fork != null)
            {
                NodeTrainingStatus lastLineMove = SelectRandomNotTrainedChild(fork, _trainingStatusTree);

                if (lastLineMove != null)
                {
                    // build the line from the root to the selected child
                    var current = lastLineMove;
                    while (current != _ntsRoot && current != null)
                    {
                        line.Add(current.Node);
                        if (current.Node.Parent != null)
                        {
                            current = _trainingStatusTree.GetNodeStatusById(current.Node.Parent.NodeId);
                        }
                        else
                        {
                            current = null;
                        }
                    }
                    line.Reverse();
                }
                return line;
            }

            return line;
        }

        /// <summary>
        /// Identifies all forks in the training tree by recursively traversing it
        /// and looking for positions where the training side has a move and there
        /// is more than 1 child.
        /// </summary>
        /// <param name="nts"></param>
        private static void IdentifyTrainingForks(NodeTrainingStatus nts)
        {
            if (nts != null)
            {
                if (nts.Children == null || nts.Children.Count == 0)
                {
                    // no children, nothing to do
                    return;
                }
                else
                {
                    // check if this is a "training" fork and add to the list if so
                    if (nts.Children.Count > 1 && nts.Node.ColorToMove != TrainingSide)
                    {
                        _trainingForks.Add(nts);
                    }

                    // continue recursively
                    foreach (var child in nts.Children)
                    {
                        IdentifyTrainingForks(child);
                    }
                }
            }
        }

        /// <summary>
        /// Randomly selects one of the not exhausted forks in the training tree.
        /// </summary>
        /// <returns></returns>
        private static NodeTrainingStatus SelectRandomNotExhaustedFork()
        {
            NodeTrainingStatus nextFork = null;

            var possibleForks = _trainingForks.Where(f => !f.IsExhausted).ToList();
            if (possibleForks.Count == 1)
            {
                nextFork = possibleForks[0];
            }
            else if (possibleForks.Count > 1)
            {
                var random = new Random();
                var randomIndex = random.Next(possibleForks.Count);
                nextFork = possibleForks[randomIndex];
            }

            return nextFork;
        }

        /// <summary>
        /// Randomly selects one of the untrained children of the current node.
        /// </summary>
        /// <param name="fork"></param>
        /// <param name="tts">TreeTrainingStatus is an argument as we anticipate that in the future
        /// we have to deal with a List here when we implement Workbook scope.</param>
        /// <returns></returns>
        private static NodeTrainingStatus SelectRandomNotTrainedChild(NodeTrainingStatus fork, TreeTrainingStatus tts)
        {
            NodeTrainingStatus nextNodeStatus = null;

            List<NodeTrainingStatus> possibleChildren = fork.Children.Where(c => !c.IsTrained).ToList();

            if (possibleChildren.Count == 0)
            {
                // this should not happen but just in case, we reset the status of all children
                foreach (var child in fork.Children)
                {
                    child.IsTrained = false;
                }

                possibleChildren = fork.Children;
            }

            if (possibleChildren.Count == 1)
            {
                nextNodeStatus = fork.Children[0];
            }
            else if (possibleChildren.Count > 1)
            {
                var random = new Random();
                var randomIndex = random.Next(possibleChildren.Count);
                nextNodeStatus = possibleChildren[randomIndex];
            }

            if (nextNodeStatus != null)
            {
                nextNodeStatus.IsTrained = true;
            }

            // check if all children are now trained, if so propagate up the IsExhausted status
            possibleChildren = fork.Children.Where(c => !c.IsTrained).ToList();
            if (possibleChildren.Count == 0)
            {
                fork.IsExhausted = true;
            }

            return nextNodeStatus;
        }

    }
}

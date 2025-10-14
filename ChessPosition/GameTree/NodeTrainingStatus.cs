using System;
using System.Collections.Generic;
using System.Linq;

namespace GameTree
{
    /// <summary>
    /// Represents the training status of a tree node, including its own status and the status of its child nodes.
    /// </summary>
    /// <remarks>This class tracks whether a specific tree node has been trained, whether all its child nodes
    /// have been trained, and provides functionality to manage and query the training status of child nodes. It is
    /// designed to support scenarios where nodes in a tree structure are iteratively trained and their statuses need to
    /// be monitored.</remarks>
    public class NodeTrainingStatus
    {
        /// <summary>
        /// The tree node this status refers to.
        /// </summary>
        public TreeNode Node { get; set; }

        /// <summary>
        /// Whether this node has been used in one of the executed training lines.
        /// </summary>
        public bool IsTrained { get; set; }

        /// <summary>
        /// Whether all child nodes have been trained at least once.
        /// </summary>
        public bool IsExhausted = false;

        /// <summary>
        /// Whether this node is a leaf (has no children).
        /// </summary>
        public bool IsLeaf = false;

        /// <summary>
        /// The collection of child nodes representing the training status of each node.
        /// </summary>
        public List<NodeTrainingStatus> Children;

        /// <summary>
        /// Creates a new instance of NodeTrainingStatus for the given tree node.
        /// </summary>
        public void Initialize()
        {
            if (Node.Children.Count > 0)
            {
                Children = new List<NodeTrainingStatus>();
            }
        }

        /// <summary>
        /// Selects a random child node that has not been marked as trained.
        /// </summary>
        /// <remarks>If all child nodes have been trained, the method resets the training status of all
        /// children and marks the parent node as exhausted. Subsequent calls will operate on the reset state.</remarks>
        /// <returns>A <see cref="TreeNode"/> representing a randomly selected child node that has not been trained, or <see
        /// langword="null"/> if no untrained child nodes are available.</returns>
        public TreeNode SelectRandomNotTrainedChild()
        {
            if (Children == null || Children.Count == 0)
            {
                return null;
            }

            var notTrainedChildren = Children.Where(c => !c.IsTrained).ToList();
            if (notTrainedChildren.Count == 0)
            {
                // this should not happen, but just in case, we reset the status of all children
                ClearChildrenIsTrainedStatus();
            }

            var random = new Random();
            var randomIndex = random.Next(notTrainedChildren.Count);
            notTrainedChildren[randomIndex].IsTrained = true; 

            if (!IsExhausted)
            {
                IsExhausted = IsAllChildrenTrained();

                // reset the IsTrained status of all children if all have been trained
                if (IsExhausted)
                {
                    ClearChildrenIsTrainedStatus();
                }
            }

            return notTrainedChildren[randomIndex].Node;
        }

        /// <summary>
        /// Determines whether all child elements are trained.
        /// </summary>
        /// <remarks>This method evaluates the training status of all child elements by checking their
        /// <c>IsTrained</c> property.</remarks>
        /// <returns><see langword="true"/> if all child elements are trained; otherwise, <see langword="false"/>.</returns>
        private bool IsAllChildrenTrained()
        {
            return Children.All(c => c.IsTrained);
        }

        /// <summary>
        /// Resets the <see cref="IsTrained"/> status of all child elements to <see langword="false"/>.
        /// </summary>
        /// <remarks>This method iterates through the collection of child elements, if any, and sets their
        /// <see cref="IsTrained"/> property to <see langword="false"/>. Ensure that the <c>Children</c> collection is
        /// properly initialized before invoking this method.</remarks>
        private void ClearChildrenIsTrainedStatus()
        {
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    child.IsTrained = false;
                }
            }
        }
    }
}

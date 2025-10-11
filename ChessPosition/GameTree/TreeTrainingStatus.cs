using ChessPosition;
using System.Collections.Generic;
using System.Linq;

namespace GameTree
{
    /// <summary>
    /// Represents the training status of a variation tree for a specific side in a chess training context.
    /// </summary>
    /// <remarks>This class tracks the training progress of nodes within a variation tree for a given side
    /// (e.g., white or black). It provides functionality to determine whether the training process is complete
    /// (exhausted) for the specified side.</remarks>
    public class TreeTrainingStatus
    {
        // Mapping of node identifiers to their corresponding NodeTrainingStatus objects.
        private Dictionary<int, NodeTrainingStatus> _nodeStatusMap;

        // The variation tree being tracked for training status.
        private VariationTree _tree;

        // The side (color) for which the training status is being tracked.
        private PieceColor _trainingSide;

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeTrainingStatus"/> class,  which tracks the training status
        /// of nodes in a variation tree for a specific side.
        /// </summary>
        /// <remarks>This constructor sets up the internal state required to monitor the training progress
        /// of nodes in the provided variation tree for the specified side.</remarks>
        /// <param name="tree">The variation tree to be tracked.</param>
        /// <param name="trainingSide">The side (color) for which the training status is being tracked.</param>
        public TreeTrainingStatus(VariationTree tree, PieceColor trainingSide)
        {
            _tree = tree;
            _trainingSide = trainingSide;
            _nodeStatusMap = new Dictionary<int, NodeTrainingStatus>();

            Initialize();
        }

        /// <summary>
        /// Gets a value indicating whether all nodes have been exhausted or are not on the training side.
        /// </summary>
        public bool IsExhausted
        {
            get
            {
                return _nodeStatusMap.Values.All(n => n.IsExhausted || n.Node.ColorToMove != _trainingSide);
            }
        }

        /// <summary>
        /// Gets the <see cref="TreeNode"/> associated with the specified node identifier.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public NodeTrainingStatus GetNodeStatusById(int nodeId)
        {
            if (_nodeStatusMap.TryGetValue(nodeId, out var status))
            {
                return status;
            }
            return null;
        }

        /// <summary>
        /// Initializes the training status for all nodes in the tree.
        /// </summary>
        /// <remarks>This method iterates through all nodes in the tree and creates a corresponding  <see
        /// cref="NodeTrainingStatus"/> object for each node. The status is initialized  with default values, and child
        /// nodes are populated if applicable.</remarks>
        public void Initialize()
        {
            _nodeStatusMap.Clear();

            foreach (var node in _tree.Nodes)
            {
                var status = new NodeTrainingStatus
                {
                    Node = node,
                    IsTrained = false,
                    IsExhausted = false,
                    Children = node.Children.Count > 0 ? new List<NodeTrainingStatus>() : null
                };
                _nodeStatusMap[node.NodeId] = status;

                PopulateChildren();
            }
        }

        /// <summary>
        /// Populates the child status mappings for each node in the tree.
        /// </summary>
        /// <remarks>This method iterates through all nodes in the tree and, for nodes with children, 
        /// associates the child nodes' statuses with their parent's status in the internal  status map. Only nodes with
        /// existing status mappings are processed.</remarks>
        private void PopulateChildren()
        {
            foreach (TreeNode node in _tree.Nodes)
            {
                if (node.Children.Count > 0)
                {
                    var parentStatus = _nodeStatusMap[node.NodeId];
                    foreach (var child in node.Children)
                    {
                        if (_nodeStatusMap.TryGetValue(child.NodeId, out var childStatus))
                        {
                            parentStatus.Children.Add(childStatus);
                        }
                    }
                }
            }
        }
    }
}

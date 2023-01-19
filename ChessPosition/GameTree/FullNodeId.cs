
namespace GameTree
{
    /// <summary>
    /// Identifies a Node by its Id 
    /// and holding Tree's Id.
    /// </summary>
    public class FullNodeId
    {
        public int TreeId;
        public int NodeId;

        public FullNodeId(int treeId, int nodeId)
        {
            TreeId = treeId;
            NodeId = nodeId;
        }
    }
}

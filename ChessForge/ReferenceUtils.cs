using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Utilities for handling Areticle References in TreeNodes
    /// </summary>
    public class ReferenceUtils
    {
        /// <summary>
        /// The last clicked reference.
        /// </summary>
        public static string LastClickedReference;

        /// <summary>
        /// The id of the node of the last clicked reference.
        /// </summary>
        public static int LastClickedReferenceNodeId;

        /// <summary>
        /// Adds a reference to the given node.
        /// TODO: refactor: replace calls to TreeNode.AddArticleReference with this one.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="articleRef"></param>
        public static void AddReferenceToNode(TreeNode node, string articleRef)
        {
            if (node != null && !string.IsNullOrEmpty(articleRef))
            {
                if (!string.IsNullOrEmpty(node.References))
                {
                    node.References += "|" + articleRef;
                }
                else
                {
                    node.References += articleRef;
                }
            }
        }

        /// <summary>
        /// Removes a reference from the given node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="articleRef"></param>
        public static void RemoveReferenceFromNode(TreeNode node, string articleRef)
        {
            if (node != null && !string.IsNullOrEmpty(node.References) && !string.IsNullOrEmpty(articleRef))
            {
                // simply removing the string from the references string may be risky in case of some corruption
                // so let's do it super safely.
                string[] tokens = node.References.Split('|');

                // re-form the references string without the articleRef
                node.References = null;
                foreach (string token in tokens)
                {
                    if (token != articleRef)
                    {
                        AddReferenceToNode(node, token);
                    }
                }
            }
        }
    }
}

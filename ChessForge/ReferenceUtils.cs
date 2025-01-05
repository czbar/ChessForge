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

        /// <summary>
        /// Gets the counts of all references found in the nodes of the passed tree.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="gameRefCount"></param>
        /// <param name="exerciseRefCount"></param>
        /// <param name="chapterRefCount"></param>
        public static void GetReferenceCountsByType(VariationTree tree, out int gameRefCount, out int exerciseRefCount, out int chapterRefCount)
        {
            gameRefCount = 0;
            exerciseRefCount = 0;
            chapterRefCount = 0;

            foreach (TreeNode node in tree.Nodes)
            {
                if (!string.IsNullOrEmpty(node.References))
                {
                    string[] refs = node.References.Split('|');
                    foreach (string guid in refs)
                    {
                        Article article = WorkbookManager.SessionWorkbook.GetArticleByGuid(guid, out _, out _, true);
                        if (article != null)
                        {
                            if (article.ContentType == GameData.ContentType.MODEL_GAME)
                            {
                                gameRefCount++;
                            }
                            else if (article.ContentType == GameData.ContentType.EXERCISE)
                            {
                                exerciseRefCount++;
                            }
                            else if (article.ContentType == GameData.ContentType.STUDY_TREE)
                            {
                                chapterRefCount++;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the counts of all references found in the passed node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="gameRefCount"></param>
        /// <param name="exerciseRefCount"></param>
        /// <param name="chapterRefCount"></param>
        public static void GetReferenceCountsByType(TreeNode node, out int gameRefCount, out int exerciseRefCount, out int chapterRefCount)
        {
            // create a dummy tree so we can use the overloaded method
            VariationTree tree = new VariationTree(GameData.ContentType.NONE, null);
            tree.Nodes.Add(node);

            GetReferenceCountsByType(tree, out gameRefCount, out exerciseRefCount, out chapterRefCount);
        }
    }
}

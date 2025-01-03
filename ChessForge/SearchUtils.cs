using GameTree;
using System.Collections.Generic;

namespace ChessForge
{
    /// <summary>
    /// Utilities for handling search operations.
    /// </summary>
    public class SearchUtils
    {
        /// <summary>
        /// Identifies the optimal referencing node for the referenced article.
        /// If the optimal node is different from the current node, moves the reference to the optimal node.
        /// </summary>
        /// <param name="hostArticle"></param>
        /// <param name="currentNode"></param>
        /// <param name="refGuid"></param>
        public static void RepositionReference(Article hostArticle, TreeNode currentNode, string refGuid)
        {
            Article referencedArticle = AppState.Workbook.GetArticleByGuid(refGuid, out _, out _);
            if (referencedArticle != null && referencedArticle.ContentType != GameData.ContentType.STUDY_TREE)
            {
                TreeNode optimalNode = FindBestReferencingNode(hostArticle, currentNode, referencedArticle);
                if (optimalNode != null || optimalNode != currentNode)
                {
                    //TODO: reposition and prepare undo
                    ReferenceUtils.RemoveReferenceFromNode(currentNode, refGuid);
                    ReferenceUtils.AddReferenceToNode(optimalNode, refGuid);
                }
            }
        }

        /// <summary>
        /// Identifies the optimal referencing nodes for all referenced articles in the host article.
        /// Moves the references to the optimal nodes.
        /// </summary>
        /// <param name="hostArticle"></param>
        public static void RepositionReferences(Article hostArticle)
        {
            BuildReferencingLists(hostArticle, out List<TreeNode> hostReferencingNodes, out List<Article> referencedArticles);
            List<TreeNode> proposedNodes = FindBestReferencingNodes(hostArticle, hostReferencingNodes, referencedArticles);

            // if all the proposed nodes are the same as hostReferencingNodes, we don't need to do anything
            if (!AreListsIdentical(hostReferencingNodes, proposedNodes))
            {
                // clear the references from the host nodes and the proposed nodes.
                ClearReferences(hostReferencingNodes);
                ClearReferences(proposedNodes);

                //TODO: reposition and prepare undo

                // build reference strings for the proposed nodes
                for (int i = 0; i < proposedNodes.Count; i++)
                {
                    ReferenceUtils.AddReferenceToNode(proposedNodes[i], referencedArticles[i].Guid);
                }
            }
        }

        /// <summary>
        /// Builds the list of referencing nodes and referenced articles..
        /// </summary>
        /// <param name="hostArticle"></param>
        /// <param name="hostReferencingNodes"></param>
        /// <param name="referencedArticles"></param>
        private static void BuildReferencingLists(Article hostArticle, out List<TreeNode> hostReferencingNodes, out List<Article> referencedArticles)
        {
            hostReferencingNodes = new List<TreeNode>();
            referencedArticles = new List<Article>();

            // build lists of referencing nodes and referenced trees
            foreach (TreeNode node in hostArticle.Tree.Nodes)
            {
                if (!string.IsNullOrEmpty(node.References))
                {
                    List<Article> articles = GuiUtilities.BuildReferencedArticlesList(node.References);
                    if (articles.Count > 0)
                    {
                        foreach (Article article in articles)
                        {
                            if (article.Tree != null)
                            {
                                referencedArticles.Add(article);
                                hostReferencingNodes.Add(node);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Identifies the optimal referencing nodes for the referenced articles. 
        /// The optimal node is one representing a position found in the referenced article 
        /// that is closest to the end of the game.
        /// Note that the size of hostReferencingNodes and referencedArticles is the same.
        /// Same node will appear multiple times in the hostReferencingNodes list if it had multiple refernces.
        /// The returned list will have the same size as the hostReferencingNodes and referencedArticles lists.
        /// 
        /// The references to chapters will not be changed. 
        /// </summary>
        /// <param name="hostArticle"></param>
        /// <param name="hostReferencingNodes"></param>
        /// <param name="referencedArticles"></param>
        /// <returns></returns>
        private static List<TreeNode> FindBestReferencingNodes(Article hostArticle, List<TreeNode> hostReferencingNodes, List<Article> referencedArticles)
        {
            // generate FENs for all nodes in the source list
            BuildFenHashSet(hostArticle, out HashSet<string> fenSet);

            // create a list of updated referencing nodes,
            // it will have the same size as the hostReferencingNodes and referencedArticles lists as there is a 1:1 correspondence
            List<TreeNode> updatedReferencingNodes = new List<TreeNode>();
            for (int i = 0; i < referencedArticles.Count; i++)
            {
                Article article = referencedArticles[i];

                if (article.ContentType == GameData.ContentType.STUDY_TREE)
                {
                    // this a chapter reference so just keep it.
                    updatedReferencingNodes.Add(hostReferencingNodes[i]);
                }
                else
                {
                    TreeNode lastNode = GetOptimalNode(article, fenSet);
                    updatedReferencingNodes.Add(lastNode);
                }
            }

            return updatedReferencingNodes;
        }

        /// <summary>
        /// Identifies the optimal referencing node for the referenced article.
        /// </summary>
        /// <param name="hostArticle"></param>
        /// <param name="hostReferencingNode"></param>
        /// <param name="referencedArticle"></param>
        /// <returns></returns>
        private static TreeNode FindBestReferencingNode(Article hostArticle, TreeNode hostReferencingNode, Article referencedArticle)
        {
            BuildFenHashSet(hostArticle, out HashSet<string> fenSet);

            // find the optimal referencing node
            TreeNode updatedReferencingNode;
            if (referencedArticle.ContentType == GameData.ContentType.STUDY_TREE)
            {
                // this a chapter reference so just keep it.
                updatedReferencingNode = hostReferencingNode;
            }
            else
            {
                TreeNode lastNode = GetOptimalNode(referencedArticle, fenSet);
                updatedReferencingNode = lastNode;
            }

            return updatedReferencingNode;
        }

        /// <summary>
        /// Finds the optimal node in the referenced article.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="fenSet"></param>
        /// <returns></returns>
        private static TreeNode GetOptimalNode(Article article, HashSet<string> fenSet)
        {
            // generate FENs for all nodes in the referenced Article
            TreeNode lastNode = null;

            foreach (TreeNode node in article.Tree.Nodes)
            {
                node.Fen = FenParser.GenerateShortFen(node.Position);
                if (fenSet.Contains(node.Fen))
                {
                    if (lastNode == null
                        || node.MoveNumber > lastNode.MoveNumber
                        || node.MoveNumber == lastNode.MoveNumber && node.ColorToMove == ChessPosition.PieceColor.White)
                    {
                        lastNode = node;
                    }
                }
            }

            return lastNode;
        }

        /// <summary>
        /// Clears the references from the nodes.
        /// </summary>
        /// <param name="nodes"></param>
        private static void ClearReferences(List<TreeNode> nodes)
        {
            foreach (TreeNode node in nodes)
            {
                node.References = null;
            }
        }

        /// <summary>
        /// Builds a hash set of FENs for all nodes in the host article.
        /// </summary>
        /// <param name="hostArticle"></param>
        /// <param name="fenSet"></param>
        private static void BuildFenHashSet(Article hostArticle, out HashSet<string> fenSet)
        {
            List<string> hostFens = new List<string>();
            foreach (TreeNode node in hostArticle.Tree.Nodes)
            {
                node.Fen = FenParser.GenerateShortFen(node.Position);
                hostFens.Add(node.Fen);
            }

            // place them in a hash set for faster lookup
            fenSet = new HashSet<string>(hostFens);
        }

        /// <summary>
        /// Checks if two lists of TreeNodes are identical.
        /// The nodes must be in the same order in both lists.
        /// </summary>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        private static bool AreListsIdentical(List<TreeNode> list1, List<TreeNode> list2)
        {
            // in our case, the lists will always have the same size so this is only a defensive check
            if (list1.Count != list2.Count)
            {
                return false;
            }
            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i] != list2[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}

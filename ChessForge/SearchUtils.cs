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
        public static TreeNode RepositionReference(VariationTree tree, TreeNode currentNode, string refGuid)
        {
            TreeNode optimalNode = currentNode;

            Article referencedArticle = AppState.Workbook.GetArticleByGuid(refGuid, out _, out _);
            if (referencedArticle != null && referencedArticle.ContentType != GameData.ContentType.STUDY_TREE)
            {
                optimalNode = FindBestReferencingNode(tree, currentNode, referencedArticle);
                if (optimalNode != null || optimalNode != currentNode)
                {
                    //TODO: reposition and prepare undo
                    ReferenceUtils.RemoveReferenceFromNode(currentNode, refGuid);
                    ReferenceUtils.AddReferenceToNode(optimalNode, refGuid);
                }
            }

            return optimalNode;
        }

        /// <summary>
        /// Identifies the optimal referencing nodes for all referenced articles in the host article.
        /// Moves the references to the optimal nodes.
        /// </summary>
        /// <param name="hostArticle"></param>
        public static List<TreeNode> RepositionReferences(VariationTree tree, TreeNode node, out List<TreeNode> hostReferencingNodes)
        {
            if (node != null)
            {
                tree = new VariationTree(GameData.ContentType.NONE, null);
                tree.Nodes.Add(node);
            }

            BuildReferencingLists(tree, node, out hostReferencingNodes, out List<Article> referencedArticles);
            List<TreeNode> proposedNodes = FindBestReferencingNodes(tree, hostReferencingNodes, referencedArticles);

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

            return proposedNodes;
        }

        /// <summary>
        /// Builds the list of referencing nodes and referenced articles.
        /// If the node is not null, the list will contain only that node
        /// otherwise we will use the list of all nodes from the passed tree.
        /// </summary>
        /// <param name="hostArticle"></param>
        /// <param name="hostReferencingNodes"></param>
        /// <param name="referencedArticles"></param>
        private static void BuildReferencingLists(VariationTree tree, TreeNode node, out List<TreeNode> hostReferencingNodes, out List<Article> referencedArticles)
        {
            hostReferencingNodes = new List<TreeNode>();
            referencedArticles = new List<Article>();

            List<TreeNode> nodes;
            if (node != null)
            {
                nodes = new List<TreeNode>
                {
                    node
                };
            }
            else
            {
                nodes = tree.Nodes;
            }

            // build lists of referencing nodes and referenced trees
            foreach (TreeNode nd in nodes)
            {
                if (!string.IsNullOrEmpty(nd.References))
                {
                    List<Article> articles = GuiUtilities.BuildReferencedArticlesList(nd.References);
                    if (articles.Count > 0)
                    {
                        foreach (Article article in articles)
                        {
                            if (article.Tree != null)
                            {
                                referencedArticles.Add(article);
                                hostReferencingNodes.Add(nd);
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
        private static List<TreeNode> FindBestReferencingNodes(VariationTree tree, List<TreeNode> hostReferencingNodes, List<Article> referencedArticles)
        {
            // generate FENs for all nodes in the source list
            Dictionary<string, TreeNode> fenToNode = BuildFenHashSet(tree, out HashSet<string> fenSet);

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
                    string fen = GetOptimalNode(article, fenSet);
                    updatedReferencingNodes.Add(fenToNode[fen]);
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
        private static TreeNode FindBestReferencingNode(VariationTree tree, TreeNode hostReferencingNode, Article referencedArticle)
        {
            Dictionary<string, TreeNode> fenToNode = BuildFenHashSet(tree, out HashSet<string> fenSet);

            // find the optimal referencing node
            TreeNode updatedReferencingNode;
            if (referencedArticle.ContentType == GameData.ContentType.STUDY_TREE)
            {
                // this a chapter reference so just keep it.
                updatedReferencingNode = hostReferencingNode;
            }
            else
            {
                string lastFen = GetOptimalNode(referencedArticle, fenSet);
                updatedReferencingNode = fenToNode[lastFen];
            }

            return updatedReferencingNode;
        }

        /// <summary>
        /// Finds the optimal node in the referenced article.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="fenSet"></param>
        /// <returns></returns>
        private static string GetOptimalNode(Article article, HashSet<string> fenSet)
        {
            string lastFen = null;
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
                        lastFen = node.Fen;
                    }
                }
            }

            return lastFen;
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
        private static Dictionary<string, TreeNode> BuildFenHashSet(VariationTree tree, out HashSet<string> fenSet)
        {
            Dictionary<string, TreeNode> fenToNode = new Dictionary<string, TreeNode>();

            List<string> hostFens = new List<string>();
            foreach (TreeNode node in tree.Nodes)
            {
                node.Fen = FenParser.GenerateShortFen(node.Position);
                hostFens.Add(node.Fen);
                fenToNode[node.Fen] = node;
            }

            // place them in a hash set for faster lookup
            fenSet = new HashSet<string>(hostFens);

            return fenToNode;
        }

        /// <summary>
        /// Checks if two lists of TreeNodes are identical.
        /// The nodes must be in the same order in both lists.
        /// </summary>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        public static bool AreListsIdentical(List<TreeNode> list1, List<TreeNode> list2)
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

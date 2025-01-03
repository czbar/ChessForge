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
        /// Identifies the optimal referencing nodes for the referenced articles.
        /// Moves the references to the optimal nodes.
        /// </summary>
        /// <param name="hostArticle"></param>
        public static void RepositionReferences(Article hostArticle)
        {
            List<TreeNode> hostReferencingNodes = new List<TreeNode>();
            List<Article> referencedArticles = new List<Article>();

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

            List<TreeNode> proposedNodes = FindBestReferencingNodes(hostArticle, hostReferencingNodes, referencedArticles);

            // if all the proposed nodes are the same as hostReferencingNodes, we don't need to do anything
            bool isSame = true;
            for (int i = 0; i < hostReferencingNodes.Count; i++)
            {
                if (hostReferencingNodes[i] != proposedNodes[i])
                {
                    isSame = false;
                    break;
                }
            }

            if (!isSame)
            {
                // clear the references from the host nodes and the proposed nodes.
                foreach (TreeNode node in hostReferencingNodes)
                {
                    node.References = null;
                }
                foreach (TreeNode node in proposedNodes)
                {
                    node.References = null;
                }

                // build references string for the proposed nodes
                for (int i = 0; i < proposedNodes.Count; i++)
                {
                    proposedNodes[i].AddArticleReference(referencedArticles[i].Guid);
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
            List<string> host = new List<string>();
            foreach (TreeNode node in hostArticle.Tree.Nodes)
            {
                node.Fen = FenParser.GenerateShortFen(node.Position);
                host.Add(node.Fen);
            }

            // place them in a hash set for faster lookup
            HashSet<string> fenSet = new HashSet<string>(host);

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
                    // generate FENs for all nodes in the referenced Article
                    TreeNode lastNode = null;

                    foreach (TreeNode node in article.Tree.Nodes)
                    {
                        node.Fen = FenParser.GenerateShortFen(node.Position);
                        if (host.Contains(node.Fen))
                        {
                            if (lastNode == null
                                || node.MoveNumber > lastNode.MoveNumber
                                || node.MoveNumber == lastNode.MoveNumber && node.ColorToMove == ChessPosition.PieceColor.White)
                            {
                                lastNode = node;
                            }
                        }
                    }

                    updatedReferencingNodes.Add(lastNode);
                }
            }

            return updatedReferencingNodes;
        }
    }
}

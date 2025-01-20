using ChessPosition;
using GameTree;
using System.Collections.Generic;

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
                    if (!node.References.Contains(articleRef))
                    {
                        node.References += "|" + articleRef;
                    }
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

        /// <summary>
        /// Moves all references from either a single node or the entire tree 
        /// to their optimal location.
        /// It has to handle 3 cases:
        /// 1. currentNode is null, then the entire tree is repositioned
        /// 2. currentNode is not null but guidRef is null, then only the references in the currentNode are repositioned
        /// 3. currentNode is not null and guidRef is not null, then only the reference with the guidRef at the currentNode is repositioned.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="currNode"></param>
        public static void RepositionReferences(VariationTree tree, TreeNode currNode, string guidRef = null)
        {
            // build the lists of referencing nodes and referenced articles, and identify the optimal nodes
            List<TreeNode> postOpNodes =
                IdentifyOptimalReferenceNodes(tree, currNode, guidRef, out List<TreeNode> preOpNodes, out List<Article> refArticles);

            if (!TreeUtils.AreNodeListsIdentical(preOpNodes, postOpNodes))
            {
                // get the deduplicated list of affected nodes
                List<TreeNode> affectedNodes = SelectUniqueNodes(preOpNodes, postOpNodes);
                
                // get the references from the affected nodes before the repositioning
                List<MoveAttributes> preOpReferences = GetNodeReferences(affectedNodes);
                
                // perform the actual repositioning
                UpdateOptimalNodesReferences(preOpNodes, postOpNodes, refArticles);
                
                // get the references from the affected nodes after the repositioning
                List<MoveAttributes> postOpReferences = GetNodeReferences(affectedNodes);

                // create the undo operation
                CreateReferenceUndoOp(preOpReferences, postOpReferences);

                // refresh the GUI
                RefreshReferencesInComments(preOpNodes, postOpNodes);
            }
        }

        /// <summary>
        /// Identifies the optimal referencing nodes for all referenced articles in the host article.
        /// Does not modify any nodes here.
        /// </summary>
        /// <param name="hostArticle"></param>
        private static List<TreeNode> IdentifyOptimalReferenceNodes(
                                            VariationTree tree,
                                            TreeNode node,
                                            string guidRef,
                                            out List<TreeNode> origNodes,
                                            out List<Article> refArticles)
        {
            BuildReferencingLists(tree, node, out origNodes, out refArticles);
            List<TreeNode> optimalNodes = FindBestReferencingNodes(tree, guidRef, origNodes, refArticles);

            return optimalNodes;
        }

        /// <summary>
        /// Creates an EditOperation for repositioning references before the actual
        /// repositioning was done.
        /// The Undo operation will clear the references in the optimal nodes 
        /// and set the references as per the hostReferencingNodes.
        /// Strictly speaking, only the preOpNodes info is needed for the undo operation.
        /// However, in the future we may need postOpNodes if we implement Redo()
        /// </summary>
        /// <param name="postOpNodes"></param>
        /// <param name="preOpNodes"></param>
        private static void CreateReferenceUndoOp(List<MoveAttributes> preOpNodes, List<MoveAttributes> postOpNodes)
        {
            EditOperation editOp =
                new EditOperation(EditOperation.EditType.REPOSITION_REFERENCES, preOpNodes, postOpNodes); 
            AppState.ActiveVariationTree.OpsManager.PushOperation(editOp);
        }

        /// <summary>
        /// Refreshes the references in the comments.
        /// Both the optimal nodes and the host referencing nodes are refreshed.
        /// No nodes data is manipulated here. It all should have been done before.
        /// </summary>
        /// <param name="optimalNodes"></param>
        /// <param name="hostReferencingNodes"></param>
        private static void RefreshReferencesInComments(List<TreeNode> hostReferencingNodes, List<TreeNode> optimalNodes)
        {

            foreach (TreeNode node in optimalNodes)
            {
                AppState.MainWin.ActiveTreeView.InsertOrUpdateCommentRun(node);
            }

            foreach (TreeNode node in hostReferencingNodes)
            {
                AppState.MainWin.ActiveTreeView.InsertOrUpdateCommentRun(node);
            }

            if (optimalNodes.Count > 0)
            {
                AppState.MainWin.ActiveTreeView.SelectNode(optimalNodes[0]);
                AppState.MainWin.SetActiveLine(optimalNodes[0].LineId, optimalNodes[0].NodeId);
                PulseManager.BringSelectedRunIntoView();
            }

            AppState.IsDirty = true;
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
        /// Identifies the optimal referencing nodes for the referenced articles. 
        /// The optimal node is one representing a position found in the referenced article 
        /// that is closest to the end of the game.
        /// Note that the size of hostReferencingNodes and referencedArticles is the same.
        /// Same node will appear multiple times in the hostReferencingNodes list if it had multiple references.
        /// The returned list will have the same size as the hostReferencingNodes and referencedArticles lists.
        /// 
        /// The references to chapters will not be changed. 
        /// </summary>
        /// <param name="hostArticle"></param>
        /// <param name="hostReferencingNodes"></param>
        /// <param name="referencedArticles"></param>
        /// <returns></returns>
        private static List<TreeNode> FindBestReferencingNodes(VariationTree tree, string guidRef, List<TreeNode> hostReferencingNodes, List<Article> referencedArticles)
        {
            // generate FENs for all nodes in the source list
            Dictionary<string, TreeNode> fenToNode = BuildFenHashSet(tree, out HashSet<string> fenSet);

            // create a list of updated referencing nodes,
            // it will have the same size as the hostReferencingNodes and referencedArticles lists as there is a 1:1 correspondence
            List<TreeNode> updatedReferencingNodes = new List<TreeNode>();
            for (int i = 0; i < referencedArticles.Count; i++)
            {
                Article article = referencedArticles[i];

                if (article.ContentType == GameData.ContentType.STUDY_TREE || (guidRef != null && article.Guid != guidRef))
                {
                    // this a chapter reference or not a guid we are after so just keep it.
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
        /// Builds the list of referencing nodes and referenced articles.
        /// If the node is not null, the list will contain only that node
        /// otherwise we will use the list of all nodes from the passed tree.
        /// </summary>
        /// <param name="hostArticle"></param>
        /// <param name="origNodes"></param>
        /// <param name="refArticles"></param>
        private static void BuildReferencingLists(VariationTree tree, TreeNode node, out List<TreeNode> origNodes, out List<Article> refArticles)
        {
            origNodes = new List<TreeNode>();
            refArticles = new List<Article>();

            List<TreeNode> nodes;
            if (node != null)
            {
                // if node is not null, we will only handle references from that node
                nodes = new List<TreeNode>
                {
                    node
                };
            }
            else
            {
                nodes = tree.Nodes;
            }

            // build lists of referencing nodes and referenced articles
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
                                refArticles.Add(article);
                                origNodes.Add(nd);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Builds a list of MoveAttributes with NodeIds and References from a list of TreeNodes.
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private static List<MoveAttributes> GetNodeReferences(List<TreeNode> nodes)
        {
            List<MoveAttributes> refs = new List<MoveAttributes>();

            foreach (var item in nodes)
            {
                refs.Add(new MoveAttributes(item.NodeId, item.References));
            }

            return refs;
        }

        /// <summary>
        /// Given 2 lists of TreeNodes, returns a combined list of unique nodes.
        /// </summary>
        /// <param name="list_1"></param>
        /// <param name="list_2"></param>
        /// <returns></returns>
        private static List<TreeNode> SelectUniqueNodes(List<TreeNode> list_1, List<TreeNode> list_2)
        {
            HashSet<TreeNode> hsNodes = new HashSet<TreeNode>();

            foreach (TreeNode node in list_1)
            {
                hsNodes.Add(node);
            }
            foreach (TreeNode node in list_2)
            {
                hsNodes.Add(node);
            }

            List<TreeNode> lstNodes = new List<TreeNode>();
            foreach (TreeNode node in hsNodes)
            {
                lstNodes.Add(node);
            }

            return lstNodes;
        }

        /// <summary>
        /// Identifies the optimal referencing node for the referenced article.
        /// </summary>
        /// <param name="optimalNodes"></param>
        /// <param name="origNodes"></param>
        /// <param name="refArticles"></param>
        private static void UpdateOptimalNodesReferences(List<TreeNode> origNodes, List<TreeNode> optimalNodes, List<Article> refArticles)
        {
            // remove references to the articles being moved.
            for (int i = 0; i < origNodes.Count; i++)
            {
                RemoveReferenceFromNode(origNodes[i], refArticles[i].Guid);
            }

            // note that the optimal nodes have all the necessary references as we created entries for the unchanged ones too
            for (int i = 0; i < optimalNodes.Count; i++)
            {
                AddReferenceToNode(optimalNodes[i], refArticles[i].Guid);
            }
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

    }
}

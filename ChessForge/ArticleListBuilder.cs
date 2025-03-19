using ChessPosition;
using GameTree;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChessForge
{
    public class ArticleListBuilder
    {
        /// <summary>
        /// Builds a list of nodes representing the exact same position as the passed node.
        /// </summary>
        /// <param name="crits"></param>
        /// <returns></returns>
        public static ObservableCollection<ArticleListItem> BuildIdenticalPositionsList(SearchPositionCriteria crits)
        {
            ObservableCollection<ArticleListItem> lstIdenticalPositions = new ObservableCollection<ArticleListItem>();

            bool found = false;

            for (int chapterIndex = 0; chapterIndex < WorkbookManager.SessionWorkbook.Chapters.Count; chapterIndex++)
            {
                if (found && crits.FindFirstOnly)
                {
                    break;
                }

                Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[chapterIndex];
                found = SearchInChapter(chapter, chapterIndex, crits, lstIdenticalPositions);
            }

            return lstIdenticalPositions;
        }

        /// <summary>
        /// Search for positions in a chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="chapterIndex"></param>
        /// <param name="crits"></param>
        /// <param name="lstIdenticalPositions"></param>
        /// <returns></returns>
        private static bool SearchInChapter(Chapter chapter, 
                                            int chapterIndex, 
                                            SearchPositionCriteria crits,
                                            ObservableCollection<ArticleListItem> lstIdenticalPositions)
        {
            bool found = false;

            // create a "chapter line" item that will be removed if nothing found in the chapter
            ArticleListItem chapterLine = new ArticleListItem(chapter, chapterIndex);
            chapterLine.IsSelected = true;

            lstIdenticalPositions.Add(chapterLine);
            int currentItemCount = lstIdenticalPositions.Count;

            found = SearchInArticle(chapter.StudyTree, chapterIndex, -1, crits, lstIdenticalPositions);

            if (!found || !(crits.FindFirstOnly))
            {
                for (int articleIndex = 0; articleIndex < chapter.ModelGames.Count; articleIndex++)
                {
                    Article article = chapter.ModelGames[articleIndex];
                    found = SearchInArticle(article, chapterIndex, articleIndex, crits, lstIdenticalPositions);
                }
            }

            if (!found || !(crits.FindFirstOnly))
            {
                for (int articleIndex = 0; articleIndex < chapter.Exercises.Count; articleIndex++)
                {
                    Article article = chapter.Exercises[articleIndex];
                    found = SearchInArticle(article, chapterIndex, articleIndex, crits, lstIdenticalPositions);
                }
            }

            if (currentItemCount == lstIdenticalPositions.Count)
            {
                // nothing added for this chapter so remove the chapter "line"
                lstIdenticalPositions.Remove(chapterLine);
            }

            return found;
        }

        /// <summary>
        /// Searches for the position in a single article.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="chapterIndex"></param>
        /// <param name="articleIndex"></param>
        /// <param name="crits"></param>
        /// <param name="lstIdenticalPositions"></param>
        /// <returns></returns>
        private static bool SearchInArticle(Article article,
                                            int chapterIndex,
                                            int articleIndex,
                                            SearchPositionCriteria crits,
                                            ObservableCollection<ArticleListItem> lstIdenticalPositions)
        {
            bool found = false;

            List<TreeNode> lstNodes = SearchPosition.FindIdenticalNodes(article.Tree, crits);
            if (crits.IsPartialSearch)
            {
                TreeNode node = FindEarliestNode(lstNodes, crits.ExcludeCurrentNode, crits.SearchNode);
                if (node == null)
                {
                    lstNodes = null;
                }
                else
                {
                    lstNodes.Clear();
                    lstNodes.Add(node);
                }
            }

            if (lstNodes != null)
            {
                foreach (TreeNode node in lstNodes)
                {
                    if (!crits.ExcludeCurrentNode || node != crits.SearchNode)
                    {
                        ArticleListItem ali = new ArticleListItem(null, chapterIndex, article, articleIndex, node);
                        SetItemForList(ali, node);
                        lstIdenticalPositions.Add(ali);
                        found = true;
                        if (crits.FindFirstOnly)
                        {
                            break;
                        }
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Finds the earliest node in the list.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="excludeCurrentNode"></param>
        /// <param name="searchNode"></param>
        /// <returns></returns>
        private static TreeNode FindEarliestNode(List<TreeNode> nodes, bool excludeCurrentNode, TreeNode searchNode)
        {
            TreeNode earliestNode = null;
            if (nodes != null)
            {
                foreach (TreeNode node in nodes)
                {
                    if (!excludeCurrentNode || node != searchNode)
                    {
                        if (earliestNode == null
                            || node.IsMainLine() && !earliestNode.IsMainLine()
                            || node.IsMainLine() == earliestNode.IsMainLine()
                            && (node.MoveNumber < earliestNode.MoveNumber
                            || node.MoveNumber == earliestNode.MoveNumber && node.ColorToMove == PieceColor.Black && earliestNode.ColorToMove == PieceColor.White))
                        {
                            earliestNode = node;
                        }
                    }
                }
            }
            return earliestNode;
        }

        /// <summary>
        /// Sets the passed item's fields values.
        /// </summary>
        /// <param name="ali"></param>
        /// <param name="node"></param>
        private static void SetItemForList(ArticleListItem ali, TreeNode node)
        {
            uint moveNumberOffset = 0;
            if (ali.Article != null && ali.Article.Tree != null)
            {
                moveNumberOffset = ali.Article.Tree.MoveNumberOffset;
            }
            ali.StemLineText = MoveUtils.BuildStemText(node, moveNumberOffset);
            ali.TailLineText = MoveUtils.BuildTailText(node, moveNumberOffset, out ali.TailLinePlyCount);
            ali.IsTailLineMain = node.IsMainLine();
            ali.StemLine = TreeUtils.GetStemLine(node);
            ali.TailLine = TreeUtils.GetTailLine(node);
            ali.IsSelected = true;
        }
    }
}

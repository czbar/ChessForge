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
        /// <param name="nd"></param>
        /// <param name="firstOnly">whether to look for the first match only</param>
        /// <param name="excludePassedNode"></param>
        /// <param name="checkDynamic">whether to check the dynamic properties (castling rights, e.p. whose move)</param>
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
                // create a "chapter line" item that will be removed if nothing found in the chapter
                ArticleListItem chapterLine = new ArticleListItem(chapter, chapterIndex);
                chapterLine.IsSelected = true;

                lstIdenticalPositions.Add(chapterLine);
                int currentItemCount = lstIdenticalPositions.Count;

                Article study = chapter.StudyTree;
                List<TreeNode> lstStudyNodes = SearchPosition.FindIdenticalNodes(study.Tree, crits);
                if (lstStudyNodes != null)
                {
                    foreach (TreeNode node in lstStudyNodes)
                    {
                        if (!crits.ExcludeCurrentNode || node != crits.SearchNode)
                        {
                            ArticleListItem ali = new ArticleListItem(null, chapterIndex, study, -1, node);
                            SetItemForList(ali, node);
                            lstIdenticalPositions.Add(ali);
                            found = true;
                            if (crits.FindFirstOnly || crits.IsPartialSearch)
                            {
                                break;
                            }
                        }
                    }
                }

                if (!found || !(crits.FindFirstOnly))
                {
                    for (int idx = 0; idx < chapter.ModelGames.Count; idx++)
                    {
                        Article article = chapter.ModelGames[idx];
                        List<TreeNode> lstNodes = SearchPosition.FindIdenticalNodes(article.Tree, crits);
                        if (lstNodes != null)
                        {
                            foreach (TreeNode node in lstNodes)
                            {
                                if (!crits.ExcludeCurrentNode || node != crits.SearchNode)
                                {
                                    ArticleListItem ali = new ArticleListItem(null, chapterIndex, article, idx, node);
                                    SetItemForList(ali, node);
                                    lstIdenticalPositions.Add(ali);
                                    found = true;
                                    if (crits.FindFirstOnly || crits.IsPartialSearch)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (!found || !(crits.FindFirstOnly))
                {
                    for (int idx = 0; idx < chapter.Exercises.Count; idx++)
                    {
                        Article article = chapter.Exercises[idx];
                        List<TreeNode> lstNodes = SearchPosition.FindIdenticalNodes(article.Tree, crits);
                        if (lstNodes != null)
                        {
                            foreach (TreeNode node in lstNodes)
                            {
                                if (!crits.ExcludeCurrentNode || node != crits.SearchNode)
                                {
                                    ArticleListItem ali = new ArticleListItem(null, chapterIndex, article, idx, node);
                                    SetItemForList(ali, node);
                                    lstIdenticalPositions.Add(ali);
                                    found = true;
                                    if (crits.FindFirstOnly || crits.IsPartialSearch)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (currentItemCount == lstIdenticalPositions.Count)
                {
                    // nothing added for this chapter so remove the chapter "line"
                    lstIdenticalPositions.Remove(chapterLine);
                }

            }

            return lstIdenticalPositions;
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

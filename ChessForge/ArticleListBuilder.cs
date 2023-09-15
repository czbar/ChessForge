using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    public class ArticleListBuilder
    {
        /// <summary>
        /// Builds a list of nodes representing the exact same position as the passed node.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="excludePassedNode"></param>
        /// <returns></returns>
        public static ObservableCollection<ArticleListItem> BuildIdenticalPositionsList(TreeNode nd, bool firstOnly, bool excludePassedNode = true)
        {
            ObservableCollection<ArticleListItem> lstIdenticalPositions = new ObservableCollection<ArticleListItem>();

            bool found = false;

            for (int chIndex = 0; chIndex < WorkbookManager.SessionWorkbook.Chapters.Count; chIndex++)
            {
                if (found && firstOnly)
                {
                    break;
                }
                Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[chIndex];
                // create a "chapter line" item that will be removed if nothing found in the chapter
                ArticleListItem chapterLine = new ArticleListItem(chapter, chIndex, null, 0);
                lstIdenticalPositions.Add(chapterLine);
                int currentItemCount = lstIdenticalPositions.Count;

                List<TreeNode> lstStudyNodes = TreeUtils.FindIdenticalNodes(chapter.StudyTree.Tree, nd);

                if (lstStudyNodes != null)
                {
                    foreach (TreeNode node in lstStudyNodes)
                    {
                        if (!excludePassedNode || node != nd)
                        {
                            ArticleListItem ali = new ArticleListItem(null, chIndex, chapter.StudyTree, 0, node);
                            uint moveNumberOffset = 0;
                            if (ali.Article != null && ali.Article.Tree != null)
                            {
                                moveNumberOffset = ali.Article.Tree.MoveNumberOffset;
                            }
                            ali.StemLineText = MoveUtils.BuildStemText(node, moveNumberOffset);
                            ali.TailLineText = MoveUtils.BuildTailText(node, moveNumberOffset, out ali.TailLinePlyCount);
                            ali.StemLine = TreeUtils.GetStemLine(node);
                            ali.TailLine = TreeUtils.GetTailLine(node);
                            lstIdenticalPositions.Add(ali);
                            found = true;
                            if (firstOnly)
                            {
                                break;
                            }
                        }
                    }
                }

                if (!found || !firstOnly)
                {
                    for (int art = 0; art < chapter.ModelGames.Count; art++)
                    {
                        Article article = chapter.ModelGames[art];
                        List<TreeNode> lstNodes = TreeUtils.FindIdenticalNodes(article.Tree, nd);
                        if (lstNodes != null)
                        {
                            foreach (TreeNode node in lstNodes)
                            {
                                if (!excludePassedNode || node != nd)
                                {
                                    ArticleListItem ali = new ArticleListItem(null, chIndex, article, art, node);
                                    uint moveNumberOffset = 0;
                                    if (ali.Article != null && ali.Article.Tree != null)
                                    {
                                        moveNumberOffset = ali.Article.Tree.MoveNumberOffset;
                                    }
                                    ali.StemLineText = MoveUtils.BuildStemText(node, moveNumberOffset);
                                    ali.TailLineText = MoveUtils.BuildTailText(node, moveNumberOffset, out ali.TailLinePlyCount);
                                    ali.StemLine = TreeUtils.GetStemLine(node);
                                    ali.TailLine = TreeUtils.GetTailLine(node);
                                    lstIdenticalPositions.Add(ali);
                                    found = true;
                                    if (firstOnly)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (!found || !firstOnly)
                {
                    for (int art = 0; art < chapter.Exercises.Count; art++)
                    {
                        Article article = chapter.Exercises[art];
                        List<TreeNode> lstNodes = TreeUtils.FindIdenticalNodes(article.Tree, nd);
                        if (lstNodes != null)
                        {
                            foreach (TreeNode node in lstNodes)
                            {
                                if (!excludePassedNode || node != nd)
                                {
                                    ArticleListItem ali = new ArticleListItem(null, chIndex, article, art, node);
                                    uint moveNumberOffset = 0;
                                    if (ali.Article != null && ali.Article.Tree != null)
                                    {
                                        moveNumberOffset = ali.Article.Tree.MoveNumberOffset;
                                    }
                                    ali.StemLineText = MoveUtils.BuildStemText(node, moveNumberOffset);
                                    ali.TailLineText = MoveUtils.BuildTailText(node, moveNumberOffset, out ali.TailLinePlyCount);
                                    ali.StemLine = TreeUtils.GetStemLine(node);
                                    ali.TailLine = TreeUtils.GetTailLine(node);
                                    lstIdenticalPositions.Add(ali);
                                    found = true;
                                    if (firstOnly)
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
    }
}

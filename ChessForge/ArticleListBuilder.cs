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
        public static ObservableCollection<ArticleListItem> BuildIdenticalPositionsList(TreeNode nd, bool excludePassedNode = true)
        {
            ObservableCollection<ArticleListItem> lstIdenticalPositions = new ObservableCollection<ArticleListItem>();

            for (int chIndex = 0; chIndex < WorkbookManager.SessionWorkbook.Chapters.Count; chIndex++)
            {
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
                            ali.StemLineText = MoveUtils.BuildStemText(node);
                            ali.TailLineText = MoveUtils.BuildTailText(node, out ali.TailLinePlyCount);
                            ali.StemLine = TreeUtils.GetStemLine(node);
                            ali.TailLine = TreeUtils.GetTailLine(node);
                            lstIdenticalPositions.Add(ali);
                        }
                    }
                }

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
                                ali.StemLineText = MoveUtils.BuildStemText(node);
                                ali.TailLineText = MoveUtils.BuildTailText(node, out ali.TailLinePlyCount);
                                ali.StemLine = TreeUtils.GetStemLine(node);
                                ali.TailLine = TreeUtils.GetTailLine(node);
                                lstIdenticalPositions.Add(ali);
                            }
                        }
                    }
                }

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
                                ali.StemLineText = MoveUtils.BuildStemText(node);
                                ali.TailLineText = MoveUtils.BuildTailText(node, out ali.TailLinePlyCount);
                                ali.StemLine = TreeUtils.GetStemLine(node);
                                ali.TailLine = TreeUtils.GetTailLine(node);
                                lstIdenticalPositions.Add(ali);
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

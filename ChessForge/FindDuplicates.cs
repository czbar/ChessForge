using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Identifies and manages duplicate articles.
    /// Articles will be deemed duplicates of each other if they 
    /// have the identical main lines.
    /// </summary>
    public class FindDuplicates
    {
        /// <summary>
        /// Identifies duplicate articles in a chapter or workbook in terms
        /// of having the identical main lines.
        /// </summary>
        /// <param name="chapter">if null, the entire workbook will be checked</param>
        /// <returns>true, if any duplicates found</returns>
        public static bool FindDuplicateArticles(Chapter chapter)
        {
            List<List<Article>> duplicates = new List<List<Article>>();

            Dictionary<int, List<Article>> dictHashes = new Dictionary<int, List<Article>>();
            CalculateArticleHashes(chapter, dictHashes);

            bool hasDupes = false;

            foreach (int key in dictHashes.Keys)
            {
                if (dictHashes[key].Count > 1)
                {
                    // identify the duplicates 
                    hasDupes = true;
                    duplicates.Add(dictHashes[key]);
                }
            }

            if (hasDupes)
            {
                VerifyDupes(duplicates);
                ExposeOriginal(duplicates);
                SelectDuplicatesToDelete(duplicates);
            }
            else
            {
                MessageBox.Show(Properties.Resources.MsgNoDuplicatesFound, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Information);
            }

            return hasDupes;
        }

        /// <summary>
        /// Invokes the dialog for selecting Articles/
        /// </summary>
        private static void SelectDuplicatesToDelete(List<List<Article>> duplicates)
        {
            string title = Properties.Resources.RemoveDuplicates;
            title = TextUtils.RemoveTrailingDots(title);

            List<ArticleListItem> articleList = BuildArticleItemList(duplicates);
            ObservableCollection<ArticleListItem> list = SortDuplicateList(articleList);

            SelectArticlesDialog dlg = new SelectArticlesDialog(null, true, title, ref list, true, ArticlesAction.DELETE_DUPLICATES);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);
            if (dlg.ShowDialog() == true)
            {
                DeleteArticlesUtils.DeleteArticles(list);
            }
        }

        /// <summary>
        /// Adds items representing chapter headers
        /// and then sorts the entire list
        /// </summary>
        /// <param name="articleList"></param>
        private static ObservableCollection<ArticleListItem> SortDuplicateList(List<ArticleListItem> articleList)
        {
            // first insert items for chapter headers
            Dictionary<int, Chapter> dictChapters = new Dictionary<int, Chapter>();
            List<ArticleListItem> chapterItems = new List<ArticleListItem>();
            foreach (ArticleListItem item in articleList)
            {
                if (!dictChapters.ContainsKey(item.ChapterIndex))
                {
                    Chapter chapter = AppState.Workbook.Chapters[item.ChapterIndex]; ;
                    dictChapters[item.ChapterIndex] = chapter;
                    chapterItems.Add(new ArticleListItem(chapter));
                }
            }

            foreach (ArticleListItem chapterItem in chapterItems)
            {
                articleList.Add(chapterItem);
            }

            // sort the list
            articleList.Sort(CompareDuplicates);

            ObservableCollection<ArticleListItem> list = new ObservableCollection<ArticleListItem>();
            foreach (ArticleListItem item in articleList)
            {
                list.Add(item);
            }

            return list;
        }

        private static int CompareDuplicates(ArticleListItem item1, ArticleListItem item2)
        {
            if (item1.ChapterIndex != item2.ChapterIndex)
            {
                return item1.ChapterIndex - item2.ChapterIndex;
            }

            if (item1.Article == null && item2.Article != null)
            {
                return -1;
            }

            if (item1.Article != null && item2.Article == null)
            {
                return 1;
            }

            // once here, we are in the same chapter and neither item is a chapter header
            if (item1.Article.ContentType == GameData.ContentType.MODEL_GAME && item2.ContentType != GameData.ContentType.MODEL_GAME)
            {
                return -1;
            }

            if (item1.Article.ContentType != GameData.ContentType.MODEL_GAME && item2.ContentType == GameData.ContentType.MODEL_GAME)
            {
                return 1;
            }

            return item1.ArticleIndex - item2.ArticleIndex;
        }

        /// <summary>
        /// Builds Article list based on the passed list of duplicates.
        /// We add all articles that are NOT at index 0 in the sublists.
        /// </summary>
        /// <returns></returns>
        private static List<ArticleListItem> BuildArticleItemList(List<List<Article>> duplicates)
        {
            List<ArticleListItem> articleList = new List<ArticleListItem>();

            foreach (List<Article> sublist in duplicates)
            {
                if (sublist.Count > 1)
                {
                    for (int i = 1; i < sublist.Count; i++)
                    {
                        Article art = AppState.Workbook.GetArticleByGuid(sublist[i].Guid, out int chapterIndex, out int articleIndex);
                        ArticleListItem item = new ArticleListItem(AppState.Workbook.Chapters[chapterIndex], chapterIndex, art, articleIndex);
                        articleList.Add(item);
                    }
                }
            }
            return articleList;
        }

        /// <summary>
        /// In each sub-list of dupes, we will identify the "original" and make sure
        /// it is placed at index 0.
        /// The "original" article is identified as the one with modes nodes, 
        /// then most comments, then most engine evaluations if previous criteria do not differentiate.
        /// The idea is not remove the article that the user worked with before importing a duplicate.
        /// </summary>
        /// <param name="duplicates"></param>
        private static void ExposeOriginal(List<List<Article>> duplicates)
        {
            foreach (List<Article> dupes in duplicates)
            {
                int origNodes = dupes[0].Tree.Nodes.Count;
                int origComments = TreeUtils.GetCommentsCount(dupes[0].Tree);
                int origEvals = TreeUtils.GetNodesWithEvalCount(dupes[0].Tree);

                int originalIndex = 0;
                for (int i = 1; i < dupes.Count; i++)
                {
                    bool isOrigin = false;
                    if (dupes[i].Tree.Nodes.Count > origNodes)
                    {
                        isOrigin = true;
                    }
                    else if (dupes[i].Tree.Nodes.Count == origNodes)
                    {
                        if (TreeUtils.GetCommentsCount(dupes[i].Tree) > origComments)
                        {
                            isOrigin = true;
                        }
                        else if (TreeUtils.GetCommentsCount(dupes[i].Tree) == origComments)
                        {
                            if (TreeUtils.GetNodesWithEvalCount(dupes[i].Tree) > origEvals)
                            {
                                isOrigin = true;
                            }
                        }
                    }

                    if (isOrigin)
                    {
                        originalIndex = i;
                        origNodes = dupes[i].Tree.Nodes.Count;
                        origComments = TreeUtils.GetCommentsCount(dupes[i].Tree);
                        origEvals = TreeUtils.GetNodesWithEvalCount(dupes[i].Tree);

                    }
                }

                if (originalIndex != 0)
                {
                    Article side = dupes[0];
                    dupes[0] = dupes[originalIndex];
                    dupes[originalIndex] = side;
                }
            }
        }

        /// <summary>
        /// Removes dupes that are false positives due to hash collision.
        /// </summary>
        /// <param name="duplicates"></param>
        private static void VerifyDupes(List<List<Article>> duplicates)
        {
            foreach (List<Article> dupes in duplicates)
            {
                List<Article> toRemove = new List<Article>();
                Article first = dupes[0];

                string firstMainLine = GetMainLineString(first.Tree);

                for (int i = 1; i < dupes.Count; i++)
                {
                    if (string.Compare(firstMainLine, GetMainLineString(dupes[i].Tree)) != 0)
                    {
                        toRemove.Add(dupes[i]);
                    }
                }
                foreach (Article art in toRemove)
                {
                    dupes.Remove(art);
                }
            }
        }

        /// <summary>
        /// Calculates hashes of all articles in a chapter or workbook.
        /// </summary>
        /// <param name="chapter">if null, the entire workbook will be checked.</param>
        /// <param name="dictHashes"></param>
        private static void CalculateArticleHashes(Chapter chapter, Dictionary<int, List<Article>> dictHashes)
        {
            if (chapter != null)
            {
                CalculateHashesInChapter(chapter, dictHashes);
            }
            else
            {
                foreach (Chapter ch in AppState.Workbook.Chapters)
                {
                    CalculateHashesInChapter(ch, dictHashes);
                }
            }
        }

        /// <summary>
        /// Calculates article hashes in a single chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="dictHashes"></param>
        private static void CalculateHashesInChapter(Chapter chapter, Dictionary<int, List<Article>> dictHashes)
        {
            foreach (Article game in chapter.ModelGames)
            {
                CalculateArticleHash(game, dictHashes);
            }

            foreach (Article exercise in chapter.Exercises)
            {
                CalculateArticleHash(exercise, dictHashes);
            }
        }

        /// <summary>
        /// Calculate main line hash of an individual article.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="dictHashes"></param>
        private static void CalculateArticleHash(Article article, Dictionary<int, List<Article>> dictHashes)
        {
            int hash = GetMainLineHash(article.Tree);
            if (!dictHashes.Keys.Contains(hash))
            {
                dictHashes[hash] = new List<Article>();
            }
            dictHashes[hash].Add(article);
        }

        /// <summary>
        /// Calculate hash of the main line from the passed tree.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        private static int GetMainLineHash(VariationTree tree)
        {
            string mainLine = GetMainLineString(tree);

            // append White and Black as with different players we do not consider articles as duplicates
            string white = tree.Header.GetWhitePlayer(out _);
            string black = tree.Header.GetBlackPlayer(out _);
            return (mainLine +white + black).GetHashCode();
        }

        /// <summary>
        /// Gets the main lines in the form of a string built by concatenating all moves notation.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        private static string GetMainLineString(VariationTree tree)
        {
            List<TreeNode> nodes = TreeUtils.GetMainLine(tree);
            StringBuilder sb = new StringBuilder();
            foreach (TreeNode node in nodes)
            {
                sb.Append(node.LastMoveAlgebraicNotation ?? "");
            }

            return sb.ToString();
        }
    }
}

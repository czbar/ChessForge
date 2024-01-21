using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            }

            return hasDupes;
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
                for (int i = 1; i < duplicates.Count; i++)
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

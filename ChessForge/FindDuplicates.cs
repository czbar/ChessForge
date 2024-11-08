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
        // list of duplicates created/refreshed at the start of FindDuplicateArticles()
        private static List<List<Article>> _duplicatesSets = new List<List<Article>>();

        /// <summary>
        /// Identifies duplicate articles in a chapter or workbook i.e. those that have
        /// the identical players' names and main lines.
        /// </summary>
        /// <param name="chapter">if null, the entire workbook will be checked</param>
        /// <returns>true, if any duplicates found</returns>
        public static bool FindDuplicateArticles(Chapter chapter)
        {
            Dictionary<int, List<Article>> dictHashes = new Dictionary<int, List<Article>>();
            CalculateArticleHashes(chapter, dictHashes);

            _duplicatesSets = new List<List<Article>>();

            // identify articles with the same hash of players names combined with main line moves
            bool hasDupes = false;
            foreach (int key in dictHashes.Keys)
            {
                if (dictHashes[key].Count > 1)
                {
                    hasDupes = true;
                    _duplicatesSets.Add(dictHashes[key]);
                }
            }

            if (hasDupes)
            {
                // perform the prepartory operations on the Article list
                VerifyDupes();
                MoveOriginalsToFront();
                
                // build the item list for the selection dialog and invoke it
                InvokeSelectDuplicatesDialog();
            }
            else
            {
                MessageBox.Show(Properties.Resources.MsgNoDuplicatesFound, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Information);
            }

            return hasDupes;
        }

        /// <summary>
        /// Invokes the dialog for selecting Duplicates to delete
        /// </summary>
        private static void InvokeSelectDuplicatesDialog()
        {
            // create and sort the list fpor the dialog
            List<DuplicateListItem> articleList = BuildDuplicateItemList();
            ObservableCollection<DuplicateListItem> duplicateList = SortDuplicateList(articleList);

            SelectDuplicatesDialog dlg = new SelectDuplicatesDialog(duplicateList);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);
            if (dlg.ShowDialog() == true)
            {
                DeleteArticlesUtils.DeleteDupeArticles(dlg.DuplicateList);
                AppState.MainWin.ChaptersView.IsDirty = true;
                AppState.MainWin.UiTabChapters.Focus();
            }
        }

        /// <summary>
        /// Sorts the list of duplicates into a new ObservableCollection.
        /// </summary>
        /// <param name="duplicateList"></param>
        private static ObservableCollection<DuplicateListItem> SortDuplicateList(List<DuplicateListItem> duplicateList)
        {
            // sort the list
            duplicateList.Sort(CompareDuplicates);

            ObservableCollection<DuplicateListItem> list = new ObservableCollection<DuplicateListItem>();
            foreach (DuplicateListItem item in duplicateList)
            {
                list.Add(item);
            }

            return list;
        }

        /// <summary>
        /// Compares two Articles so thay can be put in the appropriate order in the duplicates list.
        /// First we look at the id (number) of the duplicate set, then we place the "original"
        /// at the top of each set and we sort per position in the workbook.
        /// </summary>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <returns></returns>
        private static int CompareDuplicates(DuplicateListItem item1, DuplicateListItem item2)
        {
            // which sublist of dupes they are from
            if (item1.DuplicateNo != item2.DuplicateNo)
            {
                return item1.DuplicateNo  - item2.DuplicateNo;
            }

            // keep the original at the top
            if (item1.IsOriginal && !item2.IsOriginal)
            {
                return -1;
            }
            if (!item1.IsOriginal && item2.IsOriginal)
            {
                return 1;
            }

            if (item1.ChapterIndex != item2.ChapterIndex)
            {
                return item1.ChapterIndex - item2.ChapterIndex;
            }

            // once here, we are in the same chapter 
            if (item1.ContentType == GameData.ContentType.MODEL_GAME && item2.ContentType != GameData.ContentType.MODEL_GAME)
            {
                return -1;
            }

            if (item1.ContentType != GameData.ContentType.MODEL_GAME && item2.ContentType == GameData.ContentType.MODEL_GAME)
            {
                return 1;
            }

            return item1.ArticleIndex - item2.ArticleIndex;
        }

        /// <summary>
        /// Builds Article list based on the passed list of duplicates.
        /// </summary>
        /// <returns></returns>
        private static List<DuplicateListItem> BuildDuplicateItemList()
        {
            List<DuplicateListItem> duplicateList = new List<DuplicateListItem>();

            for (int listNo = 0; listNo < _duplicatesSets.Count; listNo++) // List<Article> sublist in _duplicates)
            {
                List<Article> dupeSet = _duplicatesSets[listNo];
                for (int i = 0; i < dupeSet.Count; i++)
                {
                    Article art = AppState.Workbook.GetArticleByGuid(dupeSet[i].Guid, out int chapterIndex, out int articleIndex);
                    ArticleListItem item = new ArticleListItem(AppState.Workbook.Chapters[chapterIndex], chapterIndex, art, articleIndex);

                    DuplicateListItem dupe = new DuplicateListItem(item);
                    dupe.DuplicateNo = listNo;
                    dupe.IsOriginal = i == 0;
                    dupe.IsSelected = i != 0;
                    duplicateList.Add(dupe);
                }
            }
            return duplicateList;
        }

        /// <summary>
        /// In each sub-list of dupes, we will identify the "original" and make sure
        /// it is placed at index 0.
        /// The "original" article is identified as the one with modes nodes, 
        /// then most comments, then most engine evaluations if previous criteria do not differentiate.
        /// The idea is not remove the article that the user worked with before importing a duplicate.
        /// </summary>
        private static void MoveOriginalsToFront()
        {
            foreach (List<Article> dupes in _duplicatesSets)
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
        private static void VerifyDupes()
        {
            foreach (List<Article> dupes in _duplicatesSets)
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
            return (mainLine + white + black).GetHashCode();
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

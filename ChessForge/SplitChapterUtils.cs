using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Utilities to handle splitting chapters into multiple chapters
    /// and "dispersing" the games.
    /// </summary>
    public class SplitChapterUtils
    {
        /// <summary>
        /// Invokes the Split Chapter dialog.
        /// </summary>
        /// <param name="chapter"></param>
        public static void InvokeSplitChapterDialog(Chapter chapter)
        {
            SplitChapterDialog dlg = new SplitChapterDialog(chapter);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 150);

            List<Chapter> createdChapters = null;

            string origTitle = chapter.Title;

            if (dlg.ShowDialog() == true)
            {
                if (dlg.MoveToChaptersPerECO)
                {
                    DistributeGamesByECO(chapter);
                }
                else
                {
                    switch (SplitChapterDialog.LastSplitBy)
                    {
                        case SplitBy.ECO:
                            createdChapters = SplitChapterByECO(SplitChapterDialog.LastSplitByCrtierion, chapter, origTitle);
                            break;
                        case SplitBy.DATE:
                            createdChapters = SplitChapterByDate(SplitChapterDialog.LastSplitByCrtierion, chapter, origTitle);
                            break;
                        case SplitBy.ROUND:
                            createdChapters = SplitChapterByRound(chapter, origTitle);
                            break;
                    }

                    if (createdChapters != null && createdChapters.Count > 1)
                    {
                        //collect info for the Undo operation
                        WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.SPLIT_CHAPTER, chapter, createdChapters);
                        WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);

                        // remove the current chapter, insert the new chapters
                        // and set the first new one as Active (so the ActiveChapterIndex will not change)
                        int index = AppState.Workbook.ActiveChapterIndex;

                        // replace the chapter at index with the first one from the list
                        AppState.Workbook.Chapters[index] = createdChapters[0];

                        for (int i = 1; i < createdChapters.Count; i++)
                        {
                            AppState.Workbook.Chapters.Insert(index + i, createdChapters[i]);
                        }

                        AppState.MainWin.ExpandCollapseChaptersView(false, false);
                        AppState.SetupGuiForCurrentStates();
                        AppState.IsDirty = true;
                    }
                }
            }
        }

        /// <summary>
        /// Performs comparison of 2 round numbers.
        /// If both strings represent an int or both are strings
        /// if we compare them normally against each other.
        /// If one represents an integer and the other does not,
        /// the former gets preference.
        /// </summary>
        /// <param name="sRoundNo1"></param>
        /// <param name="sRoundNo2"></param>
        /// <returns></returns>
        public static int CompareRoundNo(string sRoundNo1, string sRoundNo2)
        {
            double res;

            bool isInt1 = double.TryParse(sRoundNo1, out double intRoundNo1);
            bool isInt2 = double.TryParse(sRoundNo2, out double intRoundNo2);

            if (isInt1 && isInt2)
            {
                res = intRoundNo1 - intRoundNo2;
            }
            else if (!isInt1 && !isInt2)
            {
                res = string.Compare(sRoundNo1, sRoundNo2);
            }
            else
            {
                res = isInt1 ? -1 : 1;
            }

            int intRes = 0;
            if (res > 0)
            {
                return 1;
            }
            else if (res < 0)
            {
                return -1;
            }

            return intRes;
        }


        //**********************************************************
        //
        // Distributing chapter games to other chapters by ECO.
        //
        //**********************************************************


        /// <summary>
        /// Identifies the best target chapter for each game 
        /// based on the ECO and moves it there.
        /// </summary>
        /// <param name="chapter"></param>
        private static void DistributeGamesByECO(Chapter currChapter)
        {
            List<EcoChapterStats> stats = CalculateEcoStats(currChapter);
            AllocateGamesToChapterByEco(currChapter, stats);

            // Expose the items per target chapter
            ObservableCollection<ArticleListItem> list = BuildTargetEcoArticleList(stats);

            if (list.Count > 0)
            {
                SelectArticlesDialog dlg = new SelectArticlesDialog(null, false, Properties.Resources.SelectGamesToMoveToChapters, ref list, true, ChessPosition.ArticlesAction.MOVE);
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                if (dlg.ShowDialog() == true)
                {
                    list = RemoveChapterAndUnselectedItems(list);
                    if (list.Count > 0)
                    {
                        // move articles 
                        MoveArticlesToTargetEcoChapters(currChapter, list);

                        // collect info for the Undo operation
                        WorkbookOperationType typ = WorkbookOperationType.MOVE_ARTICLES_MULTI_CHAPTER;
                        WorkbookOperation op = new WorkbookOperation(typ, currChapter, (object)list);
                        WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
                    }

                    ChapterUtils.UpdateViewAfterCopyMoveArticles(currChapter, ArticlesAction.MOVE, GameData.ContentType.MODEL_GAME);

                }
            }
            else
            {
                MessageBox.Show(Properties.Resources.MsgNoGoodChapterForAnyGame, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }

        /// <summary>
        /// Adds games to the target chapter specified in the ArticleListItem object
        /// and deletes them from the source chapter.
        /// </summary>
        /// <param name="sourceChapter"></param>
        /// <param name="items"></param>
        private static void MoveArticlesToTargetEcoChapters(Chapter sourceChapter, ObservableCollection<ArticleListItem> items)
        {
            foreach (ArticleListItem item in items)
            {
                Chapter target = AppState.Workbook.GetChapterByIndex(item.ChapterIndex);
                if (target != null)
                {
                    target.AddModelGame(item.Article);
                    sourceChapter.DeleteArticle(item.Article);
                }
            }
        }

        /// <summary>
        /// Creates a list of ArticleListItems to pass on to the selection dialog.
        /// </summary>
        /// <param name="stats"></param>
        /// <returns></returns>
        private static ObservableCollection<ArticleListItem> BuildTargetEcoArticleList(List<EcoChapterStats> stats)
        {
            ObservableCollection<ArticleListItem> articleList = new ObservableCollection<ArticleListItem>();

            foreach (EcoChapterStats stat in stats)
            {
                Chapter chapter = stat.Chapter;
                int chapterIndex = chapter.Index;

                if (stat.Games.Count > 0)
                {
                    ArticleListItem chaptItem = new ArticleListItem(chapter);
                    articleList.Add(chaptItem);

                    foreach (var item in stat.Games)
                    {
                        articleList.Add(item);
                        item.ChapterIndex = chapterIndex;
                    }
                }
            }

            return articleList;
        }

        /// <summary>
        /// Allocates games to appropriate chapters
        /// </summary>
        /// <param name="stats"></param>
        private static void AllocateGamesToChapterByEco(Chapter currChapter, List<EcoChapterStats> stats)
        {
            for (int gameIndex = 0; gameIndex < currChapter.ModelGames.Count; gameIndex++)
            {
                Article game = currChapter.ModelGames[gameIndex];
                int eco = EcoChapterStats.EcoToInt(game.Tree.Header.GetECO(out _));
                if (eco > 0)
                {
                    EcoChapterStats bestStat = null;
                    int bestGameCount = 0;
                    int minChapterRange = -1;
                    bool foundExactMatch = false;

                    foreach (EcoChapterStats stat in stats)
                    {
                        int count = stat.GetEcoCount(eco);
                        if (count > 0)
                        {
                            if (count > bestGameCount)
                            {
                                bestStat = stat;
                                bestGameCount = count;
                                foundExactMatch = true;
                            }
                        }
                        else if (!foundExactMatch && stat.IsEcoInRange(eco))
                        {
                            if (stat.EcoRange < minChapterRange || minChapterRange == -1)
                            {
                                bestStat = stat;
                                minChapterRange = stat.EcoRange;
                            }
                        }
                    }

                    if (bestStat != null)
                    {
                        ArticleListItem item = new ArticleListItem(bestStat.Chapter, bestStat.Chapter.Index, game, gameIndex);
                        bestStat.AddGame(item);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates ECO stats that we need to allocate games.
        /// </summary>
        /// <param name="currChapter"></param>
        /// <returns></returns>
        private static List<EcoChapterStats> CalculateEcoStats(Chapter currChapter)
        {
            List<EcoChapterStats> stats = new List<EcoChapterStats>();

            // iterate over each chapter except the current one
            // and build a list of ECO counts in each
            foreach (Chapter chapter in AppState.Workbook.Chapters)
            {
                EcoChapterStats chapterStats = new EcoChapterStats(chapter, chapter == currChapter);
                if (chapter != currChapter)
                {
                    stats.Add(chapterStats);
                    foreach (Article game in chapter.ModelGames)
                    {
                        string eco = game.Tree.Header.GetECO(out _);
                        if (!string.IsNullOrEmpty(eco))
                        {
                            chapterStats.AddEco(eco);
                        }
                    }
                }
            }

            return stats;
        }

        /// <summary>
        /// Splits the passed chapter into multiple chapters based on 
        /// the games' ECO (or parts of ECO).
        /// Exercises go into a separate chapter.
        /// </summary>
        /// <param name="crit"></param>
        /// <param name="chapter"></param>
        /// <param name="origTitle"></param>
        /// <returns></returns>
        private static List<Chapter> SplitChapterByECO(SplitByCriterion crit, Chapter chapter, string origTitle)
        {
            List<Chapter> resChapters = new List<Chapter>();

            try
            {
                Dictionary<string, Chapter> _dictResChapters = new Dictionary<string, Chapter>();

                foreach (Article game in chapter.ModelGames)
                {
                    string critPart = GetEcoPartPerCriterion(game.Tree.Header.GetECO(out _), crit);
                    if (!_dictResChapters.ContainsKey(critPart))
                    {
                        _dictResChapters[critPart] = new Chapter();
                        origTitle = RemoveEcoFromOriginalChapterTitle(origTitle);
                        _dictResChapters[critPart].SetTitle(origTitle + " (" + critPart + ")");
                    }
                    _dictResChapters[critPart].ModelGames.Add(game);
                }

                // sort chapters by Eco
                List<string> lstEcoParts = _dictResChapters.Keys.ToList();
                lstEcoParts.Sort(CompareECO);

                foreach (string sEcoPart in lstEcoParts)
                {
                    resChapters.Add(_dictResChapters[sEcoPart]);
                    _dictResChapters[sEcoPart].StudyTree.Tree.CreateNew();
                }

                // first chapter gets the StudyTree
                resChapters[0].StudyTree = chapter.StudyTree;

                // extra chapter for exercises, if any
                if (chapter.Exercises.Count > 0)
                {
                    Chapter exercChapter = new Chapter();
                    exercChapter.StudyTree.Tree.CreateNew();
                    exercChapter.Exercises = chapter.Exercises;
                    exercChapter.SetTitle(origTitle + " - " + Properties.Resources.Exercises);
                    resChapters.Add(exercChapter);
                }

                // make sense to sort games in all new chapters by ECO, e.g. if we split by E2* we want E20 before E21 etc.
                foreach (Chapter ch in resChapters)
                {
                    ChapterUtils.SortGames(ch, GameSortCriterion.SortItem.ECO, GameSortCriterion.SortItem.ASCENDING);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("SplitChapterByEco()", ex);
            }

            return resChapters;
        }

        /// <summary>
        /// Removes the ECO code if found in parenthesis at the end
        /// of the chapter's name
        /// </summary>
        /// <param name="origTitle"></param>
        /// <returns></returns>
        private static string RemoveEcoFromOriginalChapterTitle(string origTitle)
        {
            origTitle = origTitle.TrimEnd();

            Regex ecoRegex = new Regex(@"\((?:A|B|C|D|E)\d{0,2}\)$", RegexOptions.Compiled);
            if (ecoRegex.Match(origTitle).Success)
            {
                int pos = origTitle.LastIndexOf('(');
                if (pos > 0)
                {
                    origTitle = origTitle.Substring(0, pos);
                }
            }

            return origTitle;
        }


        //**************************************************************
        //
        // END split chapter by ECO
        //
        //**************************************************************

        /// <summary>
        /// Builds a list of selected items.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private static ObservableCollection<ArticleListItem> RemoveChapterAndUnselectedItems(ObservableCollection<ArticleListItem> items)
        {
            ObservableCollection<ArticleListItem> retList = new ObservableCollection<ArticleListItem>();
            foreach (ArticleListItem item in items)
            {
                if (item.Article != null && item.IsSelected)
                {
                    retList.Add(item);
                }
            }
            return retList;
        }

        /// <summary>
        /// Splits the passed chapter into multiple chapters based on 
        /// the articles' dates (or part of dates)
        /// </summary>
        /// <param name="crit"></param>
        /// <param name="chapter"></param>
        /// <param name="origTitle"></param>
        /// <returns></returns>
        private static List<Chapter> SplitChapterByDate(SplitByCriterion crit, Chapter chapter, string origTitle)
        {
            List<Chapter> resChapters = new List<Chapter>();

            try
            {
                Dictionary<string, Chapter> _dictResChapters = new Dictionary<string, Chapter>();

                foreach (Article game in chapter.ModelGames)
                {
                    string critPart = GetDatePartPerCriterion(game.Tree.Header.GetDate(out _), crit);
                    if (!_dictResChapters.ContainsKey(critPart))
                    {
                        _dictResChapters[critPart] = new Chapter();
                        _dictResChapters[critPart].SetTitle(origTitle + " " + critPart);
                    }
                    _dictResChapters[critPart].ModelGames.Add(game);
                }

                foreach (Article exercise in chapter.Exercises)
                {
                    string critPart = GetDatePartPerCriterion(exercise.Tree.Header.GetDate(out _), crit);
                    if (!_dictResChapters.ContainsKey(critPart))
                    {
                        _dictResChapters[critPart] = new Chapter();
                        _dictResChapters[critPart].SetTitle(origTitle + " " + critPart);
                    }
                    _dictResChapters[critPart].Exercises.Add(exercise);
                }

                // sort chapters by date
                List<string> lstDateParts = _dictResChapters.Keys.ToList();
                lstDateParts.Sort(CompareDate);

                foreach (string sDatePart in lstDateParts)
                {
                    resChapters.Add(_dictResChapters[sDatePart]);
                    _dictResChapters[sDatePart].StudyTree.Tree.CreateNew();
                }

                // first chapter gets the StudyTree
                resChapters[0].StudyTree = chapter.StudyTree;
            }
            catch (Exception ex)
            {
                AppLog.Message("SplitChapterByDate()", ex);
            }

            return resChapters;
        }

        /// <summary>
        /// Splits the passed chapter into multiple chapters based on 
        /// the articles' round numbers
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="origTitle"></param>
        /// <returns></returns>
        private static List<Chapter> SplitChapterByRound(Chapter chapter, string origTitle)
        {
            List<Chapter> resChapters = new List<Chapter>();

            try
            {
                Dictionary<string, Chapter> _dictResChapters = new Dictionary<string, Chapter>();

                foreach (Article game in chapter.ModelGames)
                {
                    string round = game.Tree.Header.GetRound(out _);
                    if (!_dictResChapters.ContainsKey(round))
                    {
                        _dictResChapters[round] = new Chapter();
                        _dictResChapters[round].SetTitle(origTitle + " - " + Properties.Resources.Round + " " + round);
                    }
                    _dictResChapters[round].ModelGames.Add(game);
                }

                foreach (Article exercise in chapter.Exercises)
                {
                    string round = exercise.Tree.Header.GetRound(out _);
                    if (!_dictResChapters.ContainsKey(round))
                    {
                        _dictResChapters[round] = new Chapter();
                        _dictResChapters[round].SetTitle(origTitle + " - " + Properties.Resources.Round + " " + round);
                    }
                    _dictResChapters[round].Exercises.Add(exercise);
                }

                // sort chapters by round
                List<string> lstRoundNos = _dictResChapters.Keys.ToList();
                lstRoundNos.Sort(CompareRoundNo);

                foreach (string sRnd in lstRoundNos)
                {
                    resChapters.Add(_dictResChapters[sRnd]);
                    _dictResChapters[sRnd].StudyTree.Tree.CreateNew();
                }

                // first chapter gets the StudyTree
                resChapters[0].StudyTree = chapter.StudyTree;
            }
            catch (Exception ex)
            {
                AppLog.Message("SplitChapterByRound()", ex);
            }

            return resChapters;
        }

        /// <summary>
        /// Gets the first part of the eco code per crit.
        /// Ensures upperc case
        /// </summary>
        /// <param name="eco"></param>
        /// <param name="crit"></param>
        /// <returns></returns>
        private static string GetEcoPartPerCriterion(string eco, SplitByCriterion crit)
        {
            string ecoPart = "";

            switch (crit)
            {
                case SplitByCriterion.ECO_AE:
                    if (eco.Length >= 1)
                    {
                        ecoPart = eco.Substring(0, 1);
                    }
                    break;
                case SplitByCriterion.ECO_A0E9:
                    if (eco.Length >= 2)
                    {
                        ecoPart = eco.Substring(0, 2);
                    }
                    break;
                case SplitByCriterion.ECO_A00E99:
                    if (eco.Length >= 3)
                    {
                        ecoPart = eco.Substring(0, 3);
                    }
                    break;
            }

            return ecoPart.ToUpper();
        }

        /// <summary>
        /// Extracts the requested date parts from the date string.
        /// </summary>
        /// <param name="date">This argument must be in the from of yyyy.mm.dd where some chars may be '?'
        /// i.e. in the format returned by Tree.Header.GetRound()
        /// </param>
        /// <param name="crit"></param>
        /// <returns></returns>
        private static string GetDatePartPerCriterion(string date, SplitByCriterion crit)
        {
            string datePart = "";

            // replace possible question marks with 0's
            date = date.Replace('?', '0');

            string[] tokens = date.Split('.');
            if (tokens.Length == 3)
            {
                if (crit == SplitByCriterion.DATE_YEAR)
                {
                    datePart = tokens[0];
                }
                else if (crit == SplitByCriterion.DATE_MONTH)
                {
                    datePart = tokens[0] + "." + tokens[1];
                }
                else
                {
                    datePart = date;
                }
            }

            return datePart;
        }

        /// <summary>
        /// The passed ECO strings are in a consistent format
        /// so we can just use basic string comparison.
        /// </summary>
        /// <param name="sEco1"></param>
        /// <param name="sEco2"></param>
        /// <returns></returns>
        private static int CompareECO(string sEco1, string sEco2)
        {
            return string.Compare(sEco1, sEco2);
        }

        /// <summary>
        /// The passed date strings are in a consistent date format
        /// so we can just use basic string comparison.
        /// </summary>
        /// <param name="sDate1"></param>
        /// <param name="sDate2"></param>
        /// <returns></returns>
        private static int CompareDate(string sDate1, string sDate2)
        {
            return string.Compare(sDate1, sDate2);
        }

    }
}

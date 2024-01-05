using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessForge
{
    public class SplitChapterUtils
    {
        /// <summary>
        /// Invokes the Split Chapter dialog.
        /// </summary>
        /// <param name="chapter"></param>
        public static void InvokeSplitChapterDialog(Chapter chapter)
        {
            SplitChapterDialog dlg = new SplitChapterDialog();
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 150);

            List<Chapter> createdChapters = null;

            string origTitle = chapter.Title;

            if (dlg.ShowDialog() == true)
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
                    AppState.IsDirty = true;
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
            int res;

            bool isInt1 = int.TryParse(sRoundNo1, out int intRoundNo1);
            bool isInt2 = int.TryParse(sRoundNo2, out int intRoundNo2);

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

            return res;
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
            }
            catch (Exception ex)
            {
                AppLog.Message("SplitChapterByEco()", ex);
            }

            return resChapters;
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
                        _dictResChapters[critPart].SetTitle(origTitle + " " +  critPart);
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
                        ecoPart = eco.Substring(0,1);
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

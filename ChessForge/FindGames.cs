using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Criteria to apply in Search
    /// </summary>
    public class FindGamesCriteria
    {
        /// <summary>
        /// Partial name of the White player.
        /// </summary>
        public string WhiteName;

        /// <summary>
        /// Partial name of the Black player
        /// </summary>
        public string BlackName;

        /// <summary>
        /// Whether to look for players with the specified names
        /// regardless of the color they played with.
        /// </summary>
        public bool IgnoreColor;

        /// <summary>
        /// Minimum number of moves in the games to look for.
        /// </summary>
        public uint MinGameLength;

        /// <summary>
        /// Maximum number of moves in the games to look for.
        /// </summary>
        public uint MaxGameLength;

        /// <summary>
        /// Alphabetically, first ECO code to look for.
        /// </summary>
        public string MinECO;

        /// <summary>
        /// Alphabetically, last ECO code to look for.
        /// </summary>
        public string MaxECO;

        /// <summary>
        /// Start year for search.
        /// </summary>
        public uint MinYear;

        /// <summary>
        /// End year for search.
        /// </summary>
        public uint MaxYear;

        /// <summary>
        /// Whether to include games with year not specified 
        /// </summary>
        public bool IncludeEmptyYear;

        /// <summary>
        /// Search for White wins
        /// </summary>
        public bool ResultWhiteWin;

        /// <summary>
        /// Search for White losses
        /// </summary>
        public bool ResultBlackWin;

        /// <summary>
        /// Search for draws
        /// </summary>
        public bool ResultDraw;

        /// <summary>
        /// Search for Games with no result
        /// </summary>
        public bool ResultNone;
    }

    /// <summary>
    /// Utilities for performing game searches
    /// </summary>
    public class FindGames
    {
        /// <summary>
        /// Executes the process of specifying the search criteria and applying them.
        /// </summary>
        public static void SearchForGames()
        {
            if (AppState.Workbook == null || !AppState.Workbook.IsReady)
            {
                return;
            }

            ArticleSearchCriteriaDialog dlg = new ArticleSearchCriteriaDialog();
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

            if (dlg.ShowDialog() == true)
            {
                FindGamesCriteria crits = BuildCriteriaObject(ref dlg);
                if (crits.MaxGameLength > 0 && crits.MinGameLength > crits.MaxGameLength)
                {
                    MessageBox.Show(Properties.Resources.CritMaxMovesLessMin, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (IsFindGamesCriteriaEmpty(crits))
                {
                    MessageBox.Show(Properties.Resources.CritsInvalid, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    Mouse.SetCursor(Cursors.Wait);
                    ObservableCollection<ArticleListItem> lstGames = FindGamesByCrits(crits);
                    if (lstGames.Count == 0)
                    {
                        MessageBox.Show(Properties.Resources.MsgNoGamesFound, Properties.Resources.ChessForge, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        FoundArticlesDialog dlgEx = new FoundArticlesDialog(null,
                                                            FoundArticlesDialog.Mode.FILTER_GAMES,
                                                            ref lstGames, false);
                        GuiUtilities.PositionDialog(dlgEx, AppState.MainWin, 100);

                        if (dlgEx.ShowDialog() == true)
                        {
                            if (dlgEx.Request == FoundArticlesDialog.Action.CopyOrMoveArticles)
                            {
                                ChapterUtils.RequestCopyMoveArticles(null, false, lstGames, ArticlesAction.COPY_OR_MOVE, true);
                            }
                            else if (dlgEx.ArticleIndexId >= 0 && dlgEx.ArticleIndexId < lstGames.Count)
                            {
                                if (dlgEx.Request == FoundArticlesDialog.Action.OpenView)
                                {
                                    ArticleListItem item = lstGames[dlgEx.ArticleIndexId];
                                    WorkbookLocationNavigator.GotoArticle(item.ChapterIndex, item.Article.Tree.ContentType, item.ArticleIndex);
                                    if (AppState.ActiveVariationTree != null && AppState.MainWin.ActiveTreeView != null)
                                    {
                                        AppState.MainWin.SetActiveLine("1", 0);
                                        AppState.MainWin.ActiveTreeView.HighlightLineAndMove("1", 0);
                                    }
                                }
                            }
                        }
                    }
                    Mouse.SetCursor(Cursors.Arrow);
                }
            }
        }

        /// <summary>
        /// Iterates over the games in the current workbook to identify games
        /// meeting specified criteria.
        /// </summary>
        /// <param name="crits"></param>
        /// <returns></returns>
        private static ObservableCollection<ArticleListItem> FindGamesByCrits(FindGamesCriteria crits)
        {
            ObservableCollection<ArticleListItem> lstGames = new ObservableCollection<ArticleListItem>();

            try
            {
                for (int chIndex = 0; chIndex < WorkbookManager.SessionWorkbook.Chapters.Count; chIndex++)
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[chIndex];
                    // create a "chapter line" item that will be removed if nothing found in the chapter
                    ArticleListItem chapterLine = new ArticleListItem(chapter, chIndex);
                    chapterLine.IsSelected = true;

                    lstGames.Add(chapterLine);
                    int currentItemCount = lstGames.Count;


                    for (int art = 0; art < chapter.ModelGames.Count; art++)
                    {
                        Article game = chapter.ModelGames[art];
                        if (ArticleMeetsCriteria(game, crits))
                        {
                            ArticleListItem ali = new ArticleListItem(null, chIndex, game, art);
                            ali.MainLine = TreeUtils.GetTailLine(game.Tree.RootNode);
                            ali.IsSelected = true;
                            lstGames.Add(ali);
                        }
                    }

                    if (currentItemCount == lstGames.Count)
                    {
                        // nothing added for this chapter so remove the chapter "line"
                        lstGames.Remove(chapterLine);
                    }

                }
            }
            catch (Exception ex)
            {
                AppLog.Message("FindGamesByCrits", ex);
            }

            return lstGames;
        }

        /// <summary>
        /// Checks if the passed article meets the search criteria.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="crits"></param>
        /// <returns></returns>
        private static bool ArticleMeetsCriteria(Article article, FindGamesCriteria crits)
        {
            GameHeader header = article.Tree.Header;

            bool matchName = false;
            if (MeetsNamesCrit(header, crits))
            {
                matchName = true;
            }

            bool matchGameLen = false;
            if (MeetsGameLengthCrit(article.Tree, crits))
            {
                matchGameLen = true;
            }

            bool matchYear = false;
            if (MeetsYearsCrit(article.Tree, crits))
            {
                matchYear = true;
            }

            bool matchEco = false;
            if (MeetsEcoCrit(article.Tree.Header, crits))
            {
                matchEco = true;
            }

            bool matchResult = false;
            if (MeetsResultCrit(article.Tree.Header, crits))
            {
                matchResult = true;
            }

            return matchResult && matchEco && matchName && matchGameLen && matchYear;
        }

        /// <summary>
        /// If any name is specified, checks if it meets criteria.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="crits"></param>
        /// <returns></returns>
        private static bool MeetsNamesCrit(GameHeader header, FindGamesCriteria crits)
        {
            bool match = false;

            string white = header.GetWhitePlayer(out _);
            string black = header.GetBlackPlayer(out _);

            if (crits.WhiteName.Length > 0 || crits.BlackName.Length > 0)
            {
                if (white != null && crits.WhiteName.Length > 0 && (white.ToLower().Contains(crits.WhiteName)))
                {
                    match = true;
                }
                else if (black != null && crits.BlackName.Length > 0 && (black.ToLower().Contains(crits.BlackName)))
                {
                    match = true;
                }

                if (!match && crits.IgnoreColor)
                {
                    // try cross checking
                    if (white != null &&  crits.BlackName.Length > 0 && white.ToLower().Contains(crits.BlackName))
                    {
                        match = true;
                    }
                    else if (black != null && crits.WhiteName.Length > 0 && black.ToLower().Contains(crits.WhiteName))
                    {
                        match = true;
                    }
                }
            }
            else
            {
                match = true;
            }

            return match;
        }

        /// <summary>
        /// If any result criteria specified, checks if they are met
        /// </summary>
        /// <param name="header"></param>
        /// <param name="crits"></param>
        /// <returns></returns>
        private static bool MeetsResultCrit(GameHeader header, FindGamesCriteria crits)
        {
            bool match = false;

            if (crits.ResultWhiteWin || crits.ResultBlackWin || crits.ResultDraw || crits.ResultNone)
            {
                string result = header.GetResult(out _).Trim();
                if (crits.ResultWhiteWin && (result.StartsWith(Constants.PGN_WHITE_WIN_RESULT) || result.StartsWith(Constants.PGN_WHITE_WIN_RESULT_EX)))
                {
                    match = true;
                }
                else if (crits.ResultBlackWin && (result.StartsWith(Constants.PGN_BLACK_WIN_RESULT) || result.StartsWith(Constants.PGN_BLACK_WIN_RESULT_EX)))
                {
                    match = true;
                }
                else if (crits.ResultDraw && (result.StartsWith(Constants.PGN_DRAW_SHORT_RESULT) || result.StartsWith(Constants.PGN_DRAW_RESULT)))
                {
                    match = true;
                }
                else if (crits.ResultNone && result.StartsWith(Constants.PGN_NO_RESULT))
                {
                    match = true;
                }
            }
            else
            {
                match = true;
            }

            return match;
        }

        /// <summary>
        /// Return true if one if min or max is specified
        /// AND matches.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="crits"></param>
        /// <returns></returns>
        private static bool MeetsGameLengthCrit(VariationTree tree, FindGamesCriteria crits)
        {
            bool match = false;

            if (crits.MinGameLength > 0 || crits.MaxGameLength > 0)
            {
                uint lastMoveNo = TreeUtils.GetLastMoveNumberInMainLine(tree);

                if (crits.MaxGameLength == 0 || lastMoveNo <= crits.MaxGameLength)
                {
                    // max met, check min
                    if (crits.MinGameLength == 0 || lastMoveNo >= crits.MinGameLength)
                    {
                        match = true;
                    }
                }
            }
            else
            {
                match = true;
            }

            return match;
        }

        /// <summary>
        /// If min or max year specified, check if meets criteria.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="crits"></param>
        /// <returns></returns>
        private static bool MeetsYearsCrit(VariationTree tree, FindGamesCriteria crits)
        {
            bool match = false;

            if (crits.MinYear > 0 || crits.MaxYear > 0)
            {
                int year = tree.Header.GetYear();
                if (year == 0 && crits.IncludeEmptyYear)
                {
                    match = true;
                }
                else 
                {
                    if ((crits.MinYear == 0 || year >= crits.MinYear) && (crits.MaxYear == 0 || year <= crits.MaxYear))
                    {
                        match = true;
                    }
                }
            }
            else
            {
                match = true;
            }

            return match;
        }

        /// <summary>
        /// Return true if one if min or max is specified
        /// AND matches.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="crits"></param>
        /// <returns></returns>
        private static bool MeetsEcoCrit(GameHeader header, FindGamesCriteria crits)
        {
            string eco = header.GetECO(out _);
            if (string.IsNullOrWhiteSpace(eco) || eco.Length != 3)
            {
                return true;
            }

            eco = eco.ToUpper();

            bool match = false;
            if (crits.MinECO.Length > 0 || crits.MaxECO.Length > 0)
            {
                if (crits.MinECO.Length == 0 || (string.Compare(crits.MinECO, eco) == 0) || crits.MaxECO.Length > 0 && string.Compare(crits.MinECO, eco) <= 0)
                {
                    // min met, check max
                    if (crits.MaxECO.Length == 0 || (string.Compare(crits.MaxECO, eco) == 0) || crits.MinECO.Length > 0 && string.Compare(crits.MaxECO, eco) >= 0)
                    {
                        match = true;
                    }
                }
            }
            else
            {
                match = true;
            }

            return match;
        }

        /// <summary>
        /// Builds and sets up the criteria object based on dialog results.
        /// </summary>
        /// <param name="dlg"></param>
        /// <returns></returns>
        private static FindGamesCriteria BuildCriteriaObject(ref ArticleSearchCriteriaDialog dlg)
        {
            FindGamesCriteria crits = new FindGamesCriteria();

            crits.WhiteName = (dlg.UiTbWhite.Text ?? "").ToLower();
            crits.BlackName = (dlg.UiTbBlack.Text ?? "").ToLower();

            crits.IgnoreColor = dlg.UiCbIgnoreColors.IsChecked == true;

            uint.TryParse(dlg.UiTbMinMoves.Text, out crits.MinGameLength);
            uint.TryParse(dlg.UiTbMaxMoves.Text, out crits.MaxGameLength);

            uint.TryParse(dlg.UiTbMinYear.Text, out crits.MinYear);
            uint.TryParse(dlg.UiTbMaxYear.Text, out crits.MaxYear);

            crits.IncludeEmptyYear = dlg.UiCbEmptyYear.IsChecked == true;

            // massage the crits so no nulls and valid ECOs
            crits.MinECO = dlg.UiTbMinEco.Text ?? string.Empty;
            crits.MaxECO = dlg.UiTbMaxEco.Text ?? string.Empty;

            if (crits.MinECO.Length < 3)
            {
                crits.MinECO = "";
            }
            else
            {
                crits.MinECO = crits.MinECO.Substring(0, 3).ToUpper();
            }

            if (crits.MaxECO.Length < 3)
            {
                crits.MaxECO = "";
            }
            else
            {
                crits.MaxECO = crits.MaxECO.Substring(0, 3).ToUpper();
            }

            crits.ResultWhiteWin = dlg.UiCbWhiteWin.IsChecked == true;
            crits.ResultBlackWin = dlg.UiCbWhiteLoss.IsChecked == true;
            crits.ResultDraw = dlg.UiCbDraw.IsChecked == true;
            crits.ResultNone = dlg.UiCbNoResult.IsChecked == true;

            return crits;
        }

        /// <summary>
        /// Checks if any criteria have been specified.
        /// </summary>
        /// <param name="crits"></param>
        /// <returns></returns>
        private static bool IsFindGamesCriteriaEmpty(FindGamesCriteria crits)
        {
            if (string.IsNullOrWhiteSpace(crits.WhiteName) && string.IsNullOrWhiteSpace(crits.BlackName)
                && string.IsNullOrWhiteSpace(crits.MinECO) && string.IsNullOrWhiteSpace(crits.MaxECO)
                && crits.MinGameLength == 0 && crits.MaxGameLength == 0
                && crits.MinYear == 0 && crits.MaxYear == 0
                && !crits.ResultWhiteWin && !crits.ResultBlackWin && !crits.ResultDraw && !crits.ResultNone)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

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

            ArticleSearchCriteriaDialog dlg = new ArticleSearchCriteriaDialog()
            {
                Left = AppState.MainWin.ChessForgeMain.Left + 100,
                Top = AppState.MainWin.ChessForgeMain.Top + 100,
                Topmost = false,
                Owner = AppState.MainWin
            };

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
                    ObservableCollection<ArticleListItem> lstGames = FindGamesByCrits(crits);
                    if (lstGames.Count == 0)
                    {
                        MessageBox.Show(Properties.Resources.MsgNoGamesFound, Properties.Resources.ChessForge, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        FoundArticlesDialog dlgEx = new FoundArticlesDialog(null,
                                                            FoundArticlesDialog.Mode.FILTER_GAMES,
                                                            ref lstGames)
                        {
                            Left = AppState.MainWin.ChessForgeMain.Left + 100,
                            Top = AppState.MainWin.ChessForgeMain.Top + 100,
                            Topmost = false,
                            Owner = AppState.MainWin
                        };

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
                                        AppState.MainWin.ActiveTreeView.SelectLineAndMove("1", 0);
                                    }
                                }
                            }
                        }
                    }
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
            bool match = false;

            GameHeader header = article.Tree.Header;

            string white = header.GetWhitePlayer(out _);
            string black = header.GetBlackPlayer(out _);

            if (white != null && crits.WhiteName.Length > 0 && (white.ToLower().Contains(crits.WhiteName)
                || crits.IgnoreColor && crits.BlackName.Length > 0 && white.ToLower().Contains(crits.BlackName)))
            {
                match = true;
            }
            else if (black != null && crits.BlackName.Length > 0 && (black.ToLower().Contains(crits.BlackName)
                || crits.IgnoreColor && crits.WhiteName.Length > 0 && black.ToLower().Contains(crits.WhiteName)))
            {
                match = true;
            }
            else if (MeetsGameLengthCrit(article.Tree, crits))
            {
                match = true;
            }
            else if (MeetsEcoCrit(article.Tree.Header, crits))
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
                return false;
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

            crits.WhiteName = dlg.UiTbWhite.Text ?? "";
            crits.BlackName = dlg.UiTbBlack.Text ?? "";

            crits.IgnoreColor = dlg.UiCbIgnoreColors.IsChecked == true;

            uint.TryParse(dlg.UiTbMinMoves.Text, out crits.MinGameLength);
            uint.TryParse(dlg.UiTbMaxMoves.Text, out crits.MaxGameLength);

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
                && crits.MinGameLength == 0 && crits.MaxGameLength == 0)
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

using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace ChessForge
{
    public class Tools
    {
        /// <summary>
        /// Invokes the scope dialog and works out ECOs for articles
        /// in the selected scope.
        /// </summary>
        public static bool UiAssignEcoToArticles()
        {
            bool anyUpdated = false;

            try
            {
                OperationScopeDialog dlg = new OperationScopeDialog(Properties.Resources.ScopeForAssignECO);
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                if (dlg.ShowDialog() == true)
                {
                    Mouse.SetCursor(Cursors.Wait);
                    switch (dlg.ApplyScope)
                    {
                        case OperationScope.ACTIVE_ITEM:
                            anyUpdated = AssignEcoToArticle(AppState.Workbook.ActiveArticle);
                            break;
                        case OperationScope.CHAPTER:
                            anyUpdated = AssignEcosInChapter(AppState.ActiveChapter);
                            break;
                        case OperationScope.WORKBOOK:
                            anyUpdated = AssignEcosInWorbook(AppState.Workbook);
                            break;
                    }
                }
            }
            catch
            {
            }

            Mouse.SetCursor(Cursors.Arrow);

            if (anyUpdated)
            {
                AppState.IsDirty = true;
                AppState.MainWin.ChaptersView.IsDirty = true;
                if (AppState.ActiveTab == TabViewType.CHAPTERS)
                {
                    GuiUtilities.RefreshChaptersView(null);
                    AppState.SetupGuiForCurrentStates();
                    AppState.MainWin.UiTabChapters.Focus();
                }
                else if (AppState.MainWin.ActiveTreeView != null)
                {
                    // TODO: implement function to refresh just the page header.
                    AppState.MainWin.ActiveTreeView.BuildFlowDocumentForVariationTree();
                }
            }

            return anyUpdated;
        }

        /// <summary>
        /// Assign ECO to an article.
        /// </summary>
        /// <param name="article"></param>
        /// <returns>true if an ECO was found and it was different to the current one.</returns>
        private static bool AssignEcoToArticle(Article article)
        {
            bool res = false;

            EcoUtils.GetArticleEcoFromDictionary(article, out string eco);
            if (!string.IsNullOrEmpty(eco))
            {
                string oldEco = article.Tree.Header.GetECO(out _);
                if (oldEco != eco)
                {
                    article.Tree.Header.SetHeaderValue(PgnHeaders.KEY_ECO, eco);
                    res = true;
                }
            }

            return res;
        }

        /// <summary>
        /// Assign a ECO to every article in the Chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static bool AssignEcosInChapter(Chapter chapter)
        {
            bool anyUpdated = false;

            anyUpdated = AssignEcoToArticle(chapter.StudyTree) ? true : anyUpdated;

            foreach (Article article in chapter.ModelGames)
            {
                anyUpdated = AssignEcoToArticle(article) ? true : anyUpdated;
            }

            foreach (Article article in chapter.Exercises)
            {
                anyUpdated = AssignEcoToArticle(article) ? true : anyUpdated;
            }

            return anyUpdated;
        }

        /// <summary>
        /// Assign ECO to each article in the passed workbook.
        /// </summary>
        /// <param name="workbook"></param>
        /// <returns></returns>
        private static bool AssignEcosInWorbook(Workbook workbook)
        {
            bool anyUpdated = false;

            foreach (Chapter chapter in workbook.Chapters)
            {
                anyUpdated = AssignEcosInChapter(chapter) ? true : anyUpdated;
            }

            return anyUpdated;
        }
    }
}

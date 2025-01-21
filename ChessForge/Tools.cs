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

            Dictionary<string, string> _dictArticleGuidToEco = new Dictionary<string, string>();

            try
            {
                OperationScopeDialog dlg = new OperationScopeDialog(Properties.Resources.ScopeForAssignECO, OperationScopeDialog.ScopedAction.ASSIGN_ECO);
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                if (dlg.ShowDialog() == true)
                {
                    Mouse.SetCursor(Cursors.Wait);

                    switch (dlg.ApplyScope)
                    {
                        case OperationScope.ACTIVE_ITEM:
                            anyUpdated = AssignEcoToArticle(AppState.Workbook.ActiveArticle, _dictArticleGuidToEco);
                            break;
                        case OperationScope.CHAPTER:
                            anyUpdated = AssignEcosInChapter(AppState.ActiveChapter, dlg.ApplyToStudies, dlg.ApplyToGames, dlg.ApplyToExercises, _dictArticleGuidToEco);
                            break;
                        case OperationScope.WORKBOOK:
                            anyUpdated = AssignEcosInWorbook(AppState.Workbook, dlg.ApplyToStudies, dlg.ApplyToGames, dlg.ApplyToExercises, _dictArticleGuidToEco);
                            break;
                    }
                }
                Mouse.SetCursor(Cursors.Arrow);

                if (anyUpdated)
                {
                    PostEcoAssignCleanup();
                }

                // create Undo even of nothing changed so we don't confuse the user who won't know if anything was updated.
                WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.ASSIGN_ECO, _dictArticleGuidToEco);
                AppState.Workbook.OpsManager.PushOperation(op);
            }
            catch
            {
            }

            return anyUpdated;
        }

        /// <summary>
        /// Performs the Undo operation.
        /// </summary>
        /// <param name="opData"></param>
        public static void UndoAssignEco(object opData)
        {
            try
            {
                if (opData is Dictionary<string, string> dictEcos)
                {
                    foreach (string key in dictEcos.Keys)
                    {
                        Article article = AppState.Workbook.GetArticleByGuid(key, out _, out _);
                        if (article != null)
                        {
                            article.Tree.Header.SetHeaderValue(PgnHeaders.KEY_ECO, dictEcos[key]);
                        }
                    }
                }
            }
            catch
            {
            }

            PostEcoAssignCleanup();
        }

        /// <summary>
        /// Refreshes relevant views after ECOs have been assigned.
        /// </summary>
        private static void PostEcoAssignCleanup()
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
                AppState.MainWin.ActiveTreeView.BuildFlowDocumentForVariationTree(false);
            }
        }

        /// <summary>
        /// Assign ECO to an article.
        /// </summary>
        /// <param name="article"></param>
        /// <returns>true if an ECO was found and it was different to the current one.</returns>
        private static bool AssignEcoToArticle(Article article, Dictionary<string, string> oldEcoForUndo)
        {
            bool res = false;

            EcoUtils.GetArticleEcoFromDictionary(article, out string eco);
            if (!string.IsNullOrEmpty(eco))
            {
                string oldEco = article.Tree.Header.GetECO(out _);
                if (oldEco != eco)
                {
                    oldEcoForUndo[article.Guid] = oldEco;
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
        private static bool AssignEcosInChapter(Chapter chapter, bool study, bool games, bool exercises, Dictionary<string, string> oldEcoForUndo)
        {
            bool anyUpdated = false;

            if (study)
            {
                anyUpdated = AssignEcoToArticle(chapter.StudyTree, oldEcoForUndo) ? true : anyUpdated;
            }

            if (games)
            {
                foreach (Article article in chapter.ModelGames)
                {
                    anyUpdated = AssignEcoToArticle(article, oldEcoForUndo) ? true : anyUpdated;
                }
            }

            if (exercises)
            {
                foreach (Article article in chapter.Exercises)
                {
                    anyUpdated = AssignEcoToArticle(article, oldEcoForUndo) ? true : anyUpdated;
                }
            }

            return anyUpdated;
        }

        /// <summary>
        /// Assign ECO to each article in the passed workbook.
        /// </summary>
        /// <param name="workbook"></param>
        /// <returns></returns>
        private static bool AssignEcosInWorbook(Workbook workbook, bool studies, bool games, bool exercises, Dictionary<string, string> oldEcoForUndo)
        {
            bool anyUpdated = false;

            foreach (Chapter chapter in workbook.Chapters)
            {
                anyUpdated = AssignEcosInChapter(chapter, studies, games, exercises, oldEcoForUndo) ? true : anyUpdated;
            }

            return anyUpdated;
        }
    }
}

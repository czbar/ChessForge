using ChessPosition;
using GameTree;
using System;

namespace ChessForge
{
    /// <summary>
    /// Uitlities for creating new articles (Model Games, Exercises).
    /// </summary>
    public class CreateNewArticle
    {
        /// <summary>
        /// Creates a new Model Game and makes it "Active".
        /// </summary>
        public static void CreateNewModelGame(VariationTree gameTree = null)
        {
            try
            {
                VariationTree tree;

                if (gameTree != null)
                {
                    tree = TreeUtils.CopyVariationTree(gameTree);
                    tree.Header.SetContentType(GameData.ContentType.MODEL_GAME);
                }
                else
                {
                    tree = new VariationTree(GameData.ContentType.MODEL_GAME);
                }

                GameHeaderDialog dlg = new GameHeaderDialog(tree, Properties.Resources.GameHeader);
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);
                dlg.ShowDialog();
                if (dlg.ExitOK)
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                    Article article = chapter.AddModelGame(tree);
                    article.IsReady = true;

                    WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.CREATE_MODEL_GAME, chapter, article, chapter.ModelGames.Count - 1);
                    WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);

                    chapter.ActiveModelGameIndex
                        = chapter.GetModelGameCount() - 1;
                    AppState.MainWin.ChaptersView.BuildFlowDocumentForChaptersView(false);

                    if (AppState.ActiveTab == TabViewType.MODEL_GAME)
                    {
                        AppState.MainWin.SelectModelGame(chapter.ActiveModelGameIndex, true);
                    }
                    else
                    {
                        // if ActiveTab is not MODEL_GAME, Focus() will call SelectModelGame()
                        // Do not call it explicitly here!
                        AppState.MainWin.UiTabModelGames.Focus();
                    }

                    if (AppState.AreExplorersOn)
                    {
                        WebAccessManager.ExplorerRequest(AppState.ActiveTreeId, AppState.MainWin.ActiveVariationTree.SelectedNode, true);
                    }
                    AppState.IsDirty = true;
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("_UiMnGame_CreateModelGame_Click()", ex);
            }
        }

        /// <summary>
        /// Creates a new Exercise and makes it "Active".
        /// </summary>
        public static void CreateNewExercise()
        {
            try
            {
                PositionSetupDialog dlgPosSetup = new PositionSetupDialog(null);
                GuiUtilities.PositionDialog(dlgPosSetup, AppState.MainWin, 100);
                dlgPosSetup.ShowDialog();

                if (dlgPosSetup.ExitOK)
                {
                    BoardPosition pos = dlgPosSetup.PositionSetup;

                    VariationTree tree = new VariationTree(GameData.ContentType.EXERCISE);
                    tree.CreateNew(pos);

                    GameHeaderDialog dlgHeader = new GameHeaderDialog(tree, Properties.Resources.ResourceManager.GetString("ExerciseHeader"));
                    GuiUtilities.PositionDialog(dlgHeader, AppState.MainWin, 100);

                    dlgHeader.ShowDialog();
                    if (dlgHeader.ExitOK)
                    {
                        CreateNewExerciseFromTree(tree);
                        AppState.MainWin.RefreshExercisesView(out Chapter chapter, out int articleIndex);
                        WorkbookLocationNavigator.SaveNewLocation(chapter, GameData.ContentType.EXERCISE, articleIndex);
                        if (AppState.AreExplorersOn)
                        {
                            WebAccessManager.ExplorerRequest(AppState.ActiveTreeId, AppState.MainWin.ActiveVariationTree.SelectedNode);
                        }
                        AppState.IsDirty = true;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnAddExercise_Click()", ex);
            }
        }

        /// <summary>
        /// Creates a new exercise from the passed VariationTree
        /// </summary>
        /// <param name="tree"></param>
        public static Article CreateNewExerciseFromTree(VariationTree tree)
        {
            Article exercise = null;

            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;

                exercise = WorkbookManager.SessionWorkbook.ActiveChapter.AddExercise(tree);
                exercise.Tree.ShowTreeLines = chapter.ShowSolutionsOnOpen;

                WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.CREATE_EXERCISE, chapter, exercise, chapter.Exercises.Count - 1);
                WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);

                chapter.ActiveExerciseIndex = WorkbookManager.SessionWorkbook.ActiveChapter.GetExerciseCount() - 1;
                AppState.MainWin.ChaptersView.BuildFlowDocumentForChaptersView(false);
                AppState.MainWin.SelectExercise(chapter.ActiveExerciseIndex, true);
                AppState.IsDirty = true;
            }
            catch (Exception ex)
            {
                AppLog.Message("CreateNewExercise()", ex);
            }

            return exercise;
        }
    }
}

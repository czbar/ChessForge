using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ChessForge
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// A request from the menu to start training at the currently selected position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnStartTrainingHere_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveVariationTree == null || !AppState.IsTreeViewTabActive())
            {
                return;
            }

            // do some housekeeping just in case
            if (AppState.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
            {
                StopEngineGame();
            }
            else if (EvaluationManager.IsRunning)
            {
                EngineMessageProcessor.StopEngineEvaluation();
            }

            TreeNode nd = ActiveLine.GetSelectedTreeNode();
            if (nd != null)
            {
                SetAppInTrainingMode(nd);
                UiTrainingSessionBox.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show(Properties.Resources.NoTrainingStartMove, Properties.Resources.Training, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Invoked from the TrainingSession box rather than the menu.
        /// The arguments determine whether to exit without saving, 
        /// merge the session line to the source or create a new game from the session line.
        /// Both mergeLine and createGame arguments cannot be true ate the same time.
        /// </summary>
        /// <param name="mergeLine"></param>
        /// <param name="createGame"></param>
        public void ExitTrainingFromSessionBox(bool mergeLine, bool createGame)
        {
            if (mergeLine)
            {
                WorkbookManager.MergeLineFromTraining();
                ExitTraining(true);
            }
            else if (createGame)
            {
                Chapter chapter = AppState.ActiveChapter;
                if (chapter != null)
                {
                    // put nodes in a List<> 'coz Copy needs it
                    List<TreeNode> gameNodes = new List<TreeNode>();
                    foreach (TreeNode node in EngineGame.Line.NodeList)
                    {
                        gameNodes.Add(node);
                    }

                    // deep copy nodes from the last training session line 
                    List<TreeNode> newGameNodes = TreeUtils.CopyNodeList(gameNodes);

                    // create the game tree and set its header
                    VariationTree tree = new VariationTree(GameData.ContentType.MODEL_GAME);
                    tree.Nodes = newGameNodes;
                    GuiUtilities.CreateHeaderForTrainingGame(tree, LearningMode.TrainingSideCurrent);

                    // remove training moves from source, BEFORE we change ActiveVariation tree
                    AppState.MainWin.ActiveVariationTree.RemoveTrainingMoves();
                    
                    // clear training move flags (otherwise we have a side effect of the program asking whether to save the training line!)
                    tree.ClearTrainingFlags();

                    chapter.AddModelGame(tree);
                    int index = chapter.GetModelGameCount() - 1;

                    // "activate" the game tree so that ExitTraining gets us there
                    AppState.LastActiveManualReviewTab = ChessPosition.TabViewType.MODEL_GAME;
                    chapter.ActiveModelGameIndex = index;
                    chapter.SetActiveVariationTree(GameData.ContentType.MODEL_GAME, index);
                    AppState.IsDirty = true;

                    // now call ExitTraining with "no save"
                    ExitTraining(false);
                    UiTabModelGames.Focus();
                }
            }
            else
            {
                // exit without saving was requested
                AppState.MainWin.ActiveVariationTree.RemoveTrainingMoves();
                ExitTraining(false);
            }
        }

        /// <summary>
        /// Handles a context menu event to start training
        /// from the recently clicked bookmark.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnTrainFromBookmark_Click(object sender, RoutedEventArgs e)
        {
            BookmarkManager.SetActiveEntities(false);
            SetAppInTrainingMode(BookmarkManager.SelectedBookmarkNode);
        }

        /// <summary>
        /// Mandatory operations before exiting training whether saving
        /// or not.
        /// </summary>
        private void PrepareTrainingExit()
        {
            EngineMessageProcessor.ResetEngineEvaluation();
            UiTrainingView.CleanupVariationTree();
        }

        /// <summary>
        /// Exits the training session by tidying all relevant states.
        /// If saved is true, merges the training moves into the source. 
        /// </summary>
        /// <param name="saved"></param>
        private void ExitTraining(bool saved)
        {
            try
            {
                AppLog.Message("Exit Training");
                PrepareTrainingExit();

                EngineMessageProcessor.StopEngineEvaluation();
                EvaluationManager.Reset();

                TrainingSession.IsTrainingInProgress = false;
                TrainingSession.IsContinuousEvaluation = false;
                MainChessBoard.RemoveMoveSquareColors();

                GameData.ContentType contentType = ActiveVariationTree == null ? GameData.ContentType.NONE : ActiveVariationTree.ContentType;

                AppState.SwapCommentBoxForEngineLines(false);
                BoardCommentBox.RestoreTitleMessage(contentType);
                LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);

                if (contentType == GameData.ContentType.EXERCISE)
                {
                    _exerciseTreeView.DeactivateSolvingMode(VariationTree.SolvingMode.NONE);
                }

                if (saved)
                {
                    // at this point the source tree is set.
                    // Find the last node in EngineGame that we can find in the ActiveTree too 
                    TreeNode lastNode = UiTrainingView.LastTrainingNodePresentInActiveTree();
                    {
                        if (lastNode != null)
                        {
                            SetActiveLine(lastNode.LineId, lastNode.NodeId);
                            ActiveTreeView.SelectLineAndMove(lastNode.LineId, lastNode.NodeId);
                        }
                    }
                }

                ActiveLine.DisplayPositionForSelectedCell();
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnStopTraining_Click()", ex);
            }
        }

        /// <summary>
        /// Exits the Training session, if confirmed by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnStopTraining_Click(object sender, RoutedEventArgs e)
        {
            // check if really wants to exit
            if (MessageBox.Show(Properties.Resources.ExitTrainingSession, Properties.Resources.Training, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // check if there are moves to save and if so ask the user
                if (WorkbookManager.PromptAndSaveWorkbook(false, out bool saved))
                {
                    ExitTraining(saved);
                }
            }
        }
    }
}

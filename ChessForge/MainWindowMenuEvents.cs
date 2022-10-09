using ChessPosition;
using GameTree;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using ChessPosition.GameTree;
using System.Collections.ObjectModel;
using ChessForge;

namespace ChessForge
{
    public partial class MainWindow : Window
    {

        //**********************
        //
        //  FILE OPERATIONS
        // 
        //**********************

        /// <summary>
        /// Loads a new Workbook file.
        /// If the application is NOT in the IDLE mode, it will ask the user:
        /// - to close/cancel/save/put_aside the current tree (TODO: TO BE IMPLEMENTED)
        /// - stop a game against the engine, if in progress
        /// - stop any engine evaluations if in progress (TODO: it should be allowed to continue background analysis in a separate low-pri thread).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnOpenWorkbook_Click(object sender, RoutedEventArgs e)
        {
            if (ChangeAppModeWarning(LearningMode.Mode.MANUAL_REVIEW))
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Multiselect = false,
                    Filter = "Workbooks (*.pgn)|*.pgn;*.pgn|Legacy CHF (*.chf)|*.chf"
                };

                string initDir;
                if (!string.IsNullOrEmpty(Configuration.LastOpenDirectory))
                {
                    initDir = Configuration.LastOpenDirectory;
                }
                else
                {
                    initDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                openFileDialog.InitialDirectory = initDir;

                bool? result;

                try
                {
                    result = openFileDialog.ShowDialog();
                }
                catch
                {
                    openFileDialog.InitialDirectory = "";
                    result = openFileDialog.ShowDialog();
                };

                if (result == true)
                {
                    Configuration.LastOpenDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                    ReadWorkbookFile(openFileDialog.FileName, false, ref WorkbookManager.VariationTreeList);
                }
            }
        }

        /// <summary>
        /// An item from the "dynamic" Recent Files mene list
        /// was selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenRecentWorkbookFile(object sender, RoutedEventArgs e)
        {
            if (ChangeAppModeWarning(LearningMode.Mode.MANUAL_REVIEW))
            {
                string menuItemName = ((MenuItem)e.Source).Name;
                string path = Configuration.GetRecentFile(menuItemName);
                ReadWorkbookFile(path, false, ref WorkbookManager.VariationTreeList);
            }
        }

        /// <summary>
        /// Request to open the Annotations dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnAnnotationsDialog_Click(object sender, RoutedEventArgs e)
        {
            TreeNode nd = ActiveLine.GetSelectedTreeNode();
            if (InvokeAnnotationsDialog(nd))
            {
                _workbookView.InsertOrUpdateCommentRun(nd);
            }
        }

        /// <summary>
        /// Writes out all debug files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDebugDump_Click(object sender, RoutedEventArgs e)
        {
            DumpDebugLogs(true);
        }

        /// <summary>
        /// Writes out the debug file with states and timers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDebugDumpStates_Click(object sender, RoutedEventArgs e)
        {
            DumpDebugStates();
        }

        /// <summary>
        /// Tidy up upon application closing.
        /// Stop all timers, write out any logs,
        /// save any unsaved bits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChessForgeMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AppLog.Message("Application Closing");

            StopEvaluation();

            EngineMessageProcessor.ChessEngineService.StopEngine();

            if (AppStateManager.CurrentLearningMode != LearningMode.Mode.IDLE
                && AppStateManager.IsDirty || (ActiveVariationTree != null && ActiveVariationTree.HasTrainingMoves()))
            {
                WorkbookManager.PromptAndSaveWorkbook(false, true);
            }
            Timers.StopAll();

            DumpDebugLogs(false);
            Configuration.WriteOutConfiguration();
        }

        /// <summary>
        /// Request to save the Workbook.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnWorkbookSave_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
            try
            {
                WorkbookManager.PromptAndSaveWorkbook(true);
            }
            catch (Exception ex)
            {
                AppLog.Message("Error in PromptAndSaveWorkbook(): " + ex.Message);
            }
            this.Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// Request to save Workbook under a different name.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnWorkbookSaveAs_Click(object sender, RoutedEventArgs e)
        {
            WorkbookManager.SaveWorkbookToNewFile(AppStateManager.WorkbookFilePath, false);
        }

        /// <summary>
        /// Creates a new Workbook.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnNewWorkbook_Click(object sender, RoutedEventArgs e)
        {
            if (!WorkbookManager.AskToCloseWorkbook())
            {
                return;
            }

            // prepare document
            AppStateManager.RestartInIdleMode(false);
            WorkbookManager.CreateNewWorkbook();
            _workbookView = new VariationTreeView(UiRtbWorkbookView.Document, this);

            // ask for the options
            if (!ShowWorkbookOptionsDialog())
            {
                // user abandoned
                return;
            }

            if (!WorkbookManager.SaveWorkbookToNewFile(null, false))
            {
                AppStateManager.RestartInIdleMode(false);
                return;
            }

            BoardCommentBox.ShowWorkbookTitle();

            LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);

            AppStateManager.SetupGuiForCurrentStates();
            //StudyTree.CreateNew();
            UiTabWorkbook.Focus();
            _workbookView.BuildFlowDocumentForWorkbook();
            int startingNode = 0;
            string startLineId = ActiveVariationTree.GetDefaultLineIdForNode(startingNode);
            SetActiveLine(startLineId, startingNode);
        }


        //**********************
        //
        //  EVALUATIONS
        // 
        //**********************

        /// <summary>
        /// The user requested evaluation of the currently selected move.
        /// Check if there is an item currently selected. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnEvaluatePosition_Click(object sender, RoutedEventArgs e)
        {
            EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.CONTINUOUS);
            EvaluateActiveLineSelectedPosition();
        }

        /// <summary>
        /// The user requested a line evaluation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnEvaluateLine_Click(object sender, RoutedEventArgs e)
        {
            // a defensive check
            if (ActiveLine.GetPlyCount() == 0)
            {
                return;
            }

            if (EvaluationManager.CurrentMode != EvaluationManager.Mode.IDLE)
            {
                StopEvaluation();
            }

            // we will start with the first move of the active line
            if (EngineMessageProcessor.IsEngineAvailable)
            {
                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.LINE, EvaluationManager.LineSource.ACTIVE_LINE);

                int idx = ActiveLine.GetSelectedPlyNodeIndex(true);
                TreeNode nd = ActiveLine.GetSelectedTreeNode();
                EvaluationManager.SetStartNodeIndex(idx > 0 ? idx : 1);

                UiDgActiveLine.SelectedCells.Clear();


                EngineMessageProcessor.RequestMoveEvaluation(idx, nd);
            }
            else
            {
                MessageBox.Show("Chess Engine is not available.", "Move Evaluation Failure", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        //**********************
        //
        //  ENGINE GAME
        // 
        //**********************

        /// <summary>
        /// The user requests a game against the computer starting from the current position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnPlayEngine_Click(object sender, RoutedEventArgs e)
        {
            // check that there is a move selected in the _dgMainLineView so
            // that we have somewhere to start
            TreeNode nd = ActiveLine.GetSelectedTreeNode();
            if (nd != null)
            {
                StartEngineGame(nd, false);
                if (nd.ColorToMove == PieceColor.White && !MainChessBoard.IsFlipped || nd.ColorToMove == PieceColor.Black && MainChessBoard.IsFlipped)
                {
                    MainChessBoard.FlipBoard();
                }
            }
            else
            {
                MessageBox.Show("Select the move from which to start.", "Computer Game", MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// The user requested exit from the game against the engine.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExitEngineGame_Click(object sender, RoutedEventArgs e)
        {
            StopEngineGame();
        }

        /// <summary>
        /// The "Exit Game" button was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnExitGame_Click(object sender, RoutedEventArgs e)
        {
            StopEngineGame();
        }


        //**************************************************************
        //
        //  CHAPTERS VIEW 
        // 
        //**************************************************************

        /// <summary>
        /// The Chapters view was clicked somewhere.
        /// Here we configured the context menu items as if no chapter was clicked.
        /// If any chapter line was clicked the menus will be re-configured
        /// accordingly in the event handler for the the Chapter related Run..
        /// All the above happens before the context menu is invoked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Chapters_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            WorkbookManager.LastClickedChapterId = -1;
            WorkbookManager.EnableChaptersMenus(_cmChapters, false);
        }

        /// <summary>
        /// Selects the clicked Chapter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnSelectChapter_Click(object sender, RoutedEventArgs e)
        {
            SelectChapter(WorkbookManager.LastClickedChapterId);
        }

        /// <summary>
        /// Invokes the Chapter Title dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnRenameChapter_Click(object sender, RoutedEventArgs e)
        {
            Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterById(WorkbookManager.LastClickedChapterId);
            if (chapter != null && ShowChapterTitleDialog(chapter))
            {
                SetupGuiForActiveStudyTree();
                AppStateManager.IsDirty = true;
            }
        }

        /// <summary>
        /// Creates a new Chapter and adds it to the Workbook.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnAddChapter_Click(object sender, RoutedEventArgs e)
        {
            Chapter chapter = WorkbookManager.SessionWorkbook.CreateNewChapter();
            if (ShowChapterTitleDialog(chapter))
            {
                AppStateManager.IsDirty = true;
            }
            else
            {
                // remove the just created Chapter
                SessionWorkbook.Chapters.Remove(chapter);
            }
        }

        /// <summary>
        /// Deletes the entire chapter from the Workbook.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDeleteChapter_Click(object sender, RoutedEventArgs e)
        {
            Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterById(WorkbookManager.LastClickedChapterId);
            if (chapter != null)
            {
                var res = MessageBox.Show("Deleting chapter \"" + chapter.Title + ". Are you sure?", "Delete Chapter", MessageBoxButton.YesNoCancel);
                if (res == MessageBoxResult.Yes)
                {
                    WorkbookManager.SessionWorkbook.Chapters.Remove(chapter);
                    if (chapter.Id == WorkbookManager.SessionWorkbook.ActiveChapter.Id)
                    {
                        WorkbookManager.SessionWorkbook.SelectDefaultActiveChapter();
                    }
                    _chaptersView.BuildFlowDocumentForChaptersView();
                    SetupGuiForActiveStudyTree();
                    AppStateManager.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Requests import of Model Games from a PGN file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnImportModelGames_Click(object sender, RoutedEventArgs e)
        {
            string fileName = SelectPgnFile();
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                ObservableCollection<GameMetadata> games = new ObservableCollection<GameMetadata>();
                int gameCount = WorkbookManager.ReadPgnFile(fileName, ref games);
                if (gameCount > 0)
                {
                }
                else
                {
                    MessageBox.Show("No games found in " + fileName, "Import PGN", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        /// <summary>
        /// Requests import of Exercises from a PGN file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnImportExercises_Click(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Shows the OpenFileDialog to let the user
        /// select a PGN file.
        /// </summary>
        /// <returns></returns>
        private string SelectPgnFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "Game files (*.pgn)|*.pgn;*.pgn|All files (*.*)|*.*"
            };

            string initDir;
            if (!string.IsNullOrEmpty(Configuration.LastImportDirectory))
            {
                initDir = Configuration.LastImportDirectory;
            }
            else
            {
                initDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            openFileDialog.InitialDirectory = initDir;

            bool? result;

            try
            {
                result = openFileDialog.ShowDialog();
            }
            catch
            {
                openFileDialog.InitialDirectory = "";
                result = openFileDialog.ShowDialog();
            };

            if (result == true)
            {
                Configuration.LastImportDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                ReadWorkbookFile(openFileDialog.FileName, false, ref WorkbookManager.VariationTreeList);
                return openFileDialog.FileName;
            }
            else
            {
                return null;
            }
        }


        //**********************
        //
        //  BOOKMARKS
        // 
        //**********************

        /// <summary>
        /// The user requested to bookmark the currently selected position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnBookmarkPosition_Click(object sender, RoutedEventArgs e)
        {
            int moveIndex = ActiveLine.GetSelectedPlyNodeIndex(false);
            if (moveIndex < 0)
            {
                return;
            }
            else
            {
                int posIndex = moveIndex;
                TreeNode nd = ActiveLine.GetNodeAtIndex(posIndex);
                BookmarkManager.AddBookmark(nd);
                UiTabBookmarks.Focus();
            }
        }

        /// <summary>
        /// Allows the user to add a bookmark by re-directing them to the Workbook view 
        /// and advising on the procedure. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnAddBookmark_Click(object sender, RoutedEventArgs e)
        {
            UiTabWorkbook.Focus();
            MessageBox.Show("Right-click a move and select \"Add to Bookmarks\" from the popup-menu", "Chess Forge Training", MessageBoxButton.OK);
        }

        /// <summary>
        /// A request to delete the clicked bookmark.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDeleteBookmark_Click(object sender, RoutedEventArgs e)
        {
            BookmarkManager.DeleteBookmark();
        }

        /// <summary>
        /// The user requested to delete all bookmarks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDeleteAllBookmarks_Click(object sender, RoutedEventArgs e)
        {
            BookmarkManager.DeleteAllBookmarks();
        }

        /// <summary>
        /// Adds the last click node, and all its siblings to bookmarks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnWorkbookBookmarkAlternatives_Click(object sender, RoutedEventArgs e)
        {
            int ret = BookmarkManager.AddAllSiblingsToBookmarks(_workbookView.LastClickedNodeId);
            if (ret == 1)
            {
                MessageBox.Show("Bookmarks already exist.", "Training Bookmarks", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else if (ret == -1)
            {
                MessageBox.Show("Failed to add the bookmarks.", "Training Bookmarks", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                AppStateManager.IsDirty = true;
                UiTabBookmarks.Focus();
            }
        }

        /// <summary>
        /// Adds the last clicked node to bookmarks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnWorkbookSelectAsBookmark_Click(object sender, RoutedEventArgs e)
        {
            int ret = BookmarkManager.AddBookmark(_workbookView.LastClickedNodeId);
            if (ret == 1)
            {
                MessageBox.Show("This bookmark already exists.", "Training Bookmarks", MessageBoxButton.OK);
            }
            else if (ret == -1)
            {
                MessageBox.Show("Failed to add the bookmark.", "Training Bookmarks", MessageBoxButton.OK);
            }
            else
            {
                AppStateManager.IsDirty = true;
                UiTabBookmarks.Focus();
            }
        }

        /// <summary>
        /// A request to auto-generate bookmarks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnGenerateBookmark_Click(object sender, RoutedEventArgs e)
        {
            BookmarkManager.GenerateBookmarks();
        }


        //**********************
        //
        //  TRAINING
        // 
        //**********************

        /// <summary>
        /// Re-directs the user to the bookmark page where they can
        /// select a bookmarked position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnStartTraining_Click(object sender, RoutedEventArgs e)
        {
            if (AppStateManager.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
            {
                StopEngineGame();
            }
            else if (EvaluationManager.IsRunning)
            {
                EngineMessageProcessor.StopEngineEvaluation();
            }

            LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);
            EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);

            AppStateManager.SwapCommentBoxForEngineLines(false);

            UiTabBookmarks.Focus();
        }

        /// <summary>
        /// A request from the menu to start training at the currently selected position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnStartTrainingHere_Click(object sender, RoutedEventArgs e)
        {
            // do some housekeeping just in case
            if (AppStateManager.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
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
                if (!BookmarkManager.IsBookmarked(nd.NodeId))
                {
                    if (MessageBox.Show("Do you want to bookmark this move?", "Training", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        BookmarkManager.AddBookmark(nd);
                    }
                }
                SetAppInTrainingMode(nd);
            }
            else
            {
                MessageBox.Show("No move selected to start training from.", "Training", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Exits the Training session, if confirmed by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnStopTraining_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Exit the training session?", "Chess Forge Training", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (WorkbookManager.PromptAndSaveWorkbook(false))
                {
                    EngineMessageProcessor.StopEngineEvaluation();
                    EvaluationManager.Reset();

                    TrainingSession.IsTrainingInProgress = false;
                    MainChessBoard.RemoveMoveSquareColors();
                    LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);
                    AppStateManager.SetupGuiForCurrentStates();

                    ActiveLine.DisplayPositionForSelectedCell();
                    AppStateManager.SwapCommentBoxForEngineLines(false);
                    BoardCommentBox.RestoreTitleMessage();
                }
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
            if (BookmarkManager.ClickedIndex >= 0)
            {
                SetAppInTrainingMode(BookmarkManager.ClickedIndex);
            }
        }

        /// <summary>
        /// The user requested to restart the training game after the most recently 
        /// clicked run/move.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnTrainRestartGame_Click(object sender, RoutedEventArgs e)
        {
            UiTrainingView.RestartGameAfter(sender, e);
        }

        /// <summary>
        /// The user requested to roll back training to the most recently clicked
        /// Workbook move.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnTrainSwitchToWorkbook_Click(object sender, RoutedEventArgs e)
        {
            UiTrainingView.RollbackToWorkbookMove();
        }

        /// <summary>
        /// The user requested evaluation of the most recently clicked run/move.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnTrainEvalMove_Click(object sender, RoutedEventArgs e)
        {
            UiTrainingView.RequestMoveEvaluation();
        }

        /// <summary>
        /// The user requested evaluation of the line from the most recently clicked run/move.
        /// It could be the line from the game against the engine, or the Workbok training line.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnTrainEvalLine_Click(object sender, RoutedEventArgs e)
        {
            UiTrainingView.RequestLineEvaluation();
        }

        /// <summary>
        /// Restarts training from the same position/bookmark
        /// that we started the current session with.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnTrainRestartTraining_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Restart the training session?", "Chess Forge Training", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                SetAppInTrainingMode(TrainingSession.StartPosition);
            }
        }

        /// <summary>
        /// Training Browse tab received focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabItemTrainingBrowse_GotFocus(object sender, RoutedEventArgs e)
        {
            AppStateManager.SetupGuiForTrainingBrowseMode();
        }

        /// <summary>
        /// Training View received focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbTrainingProgress_GotFocus(object sender, RoutedEventArgs e)
        {
            AppStateManager.SetupGuiForTrainingProgressMode();
        }

        /// <summary>
        /// The "Exit Training" button was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnExitTraining_Click(object sender, RoutedEventArgs e)
        {
            UiMnStopTraining_Click(sender, e);
        }


        //**********************
        //
        //  TREE OPERATIONS
        // 
        //**********************

        /// <summary>
        /// The user requested to promote the currently selected line.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnPromoteLine_Click(object sender, RoutedEventArgs e)
        {
            _workbookView.PromoteCurrentLine();
        }

        /// <summary>
        /// The user requested to deleted the sub-tree starting at the selected node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDeleteMovesFromHere_Click(object sender, RoutedEventArgs e)
        {
            _workbookView.DeleteRemainingMoves();
        }


        //*********************
        //
        // DIALOGS
        //
        //*********************

        /// <summary>
        /// View->Select Engine... menu item clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnSelectEngine_Click(object sender, RoutedEventArgs e)
        {
            string searchPath = Path.GetDirectoryName(Configuration.EngineExePath);
            if (!string.IsNullOrEmpty(Configuration.SelectEngineExecutable(searchPath)))
            {
                ReloadEngine();
            }
        }

        /// <summary>
        /// User clicked Help->About
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnHelpAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutBoxDialog dlg = new AboutBoxDialog();
            dlg.ShowDialog();
        }

        /// <summary>
        /// The user requested to edit Workbook options.
        /// The dialog will be shown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnWorkbookOptions_Click(object sender, RoutedEventArgs e)
        {
            if (AppStateManager.CurrentLearningMode != LearningMode.Mode.IDLE)
            {
                ShowWorkbookOptionsDialog();
            }
        }

        /// <summary>
        /// The user requested to edit Application options.
        /// The dialog will be shown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnApplicationOptions_Click(object sender, RoutedEventArgs e)
        {
            if (AppStateManager.CurrentLearningMode != LearningMode.Mode.IDLE)
            {
                ShowApplicationOptionsDialog();
            }
        }


        //*********************
        //
        // OTHER
        //
        //*********************

        /// <summary>
        /// Auto-replays the current Active Line on a menu request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnReplayLine_Clicked(object sender, RoutedEventArgs e)
        {
            ActiveLine.ReplayLine(0);
        }

        /// <summary>
        /// Flips the main chess board upside down.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnFlipBoard_Click(object sender, RoutedEventArgs e)
        {
            MainChessBoard.FlipBoard();
        }

        /// <summary>
        /// CTRL+S shortcut for saving workbook.
        /// Executes only if the corresponding menu item is enabled
        /// in order to honor the program's logic.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CustomCommand_SaveWorkbook(object sender, RoutedEventArgs e)
        {
            if (UiMnWorkbookSave.IsEnabled == true)
            {
                UiMnWorkbookSave_Click(sender, e);
            }
        }

    }
}

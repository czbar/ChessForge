using ChessPosition;
using GameTree;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        /// - to close/cancel/save/put_aside the current tree
        /// - stop a game against the engine, if in progress
        /// - stop any engine evaluations if in progress (TODO: it should be allowed to continue background analysis in a separate low-pri thread).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnOpenWorkbook_Click(object sender, RoutedEventArgs e)
        {
            bool proceed = true;

            if (WorkbookManager.SessionWorkbook != null && AppStateManager.IsDirty)
            {
                proceed = WorkbookManager.PromptAndSaveWorkbook(false);
            }

            if (proceed && ChangeAppModeWarning(LearningMode.Mode.MANUAL_REVIEW))
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
            bool proceed = true;

            if (WorkbookManager.SessionWorkbook != null && AppStateManager.IsDirty)
            {
                proceed = WorkbookManager.PromptAndSaveWorkbook(false);
            }

            if (proceed && ChangeAppModeWarning(LearningMode.Mode.MANUAL_REVIEW))
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
                ActiveTreeView.InsertOrUpdateCommentRun(nd);
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

            StopEvaluation(false);

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
            Mouse.SetCursor(Cursors.Wait);
            try
            {
                WorkbookManager.PromptAndSaveWorkbook(true);
            }
            catch (Exception ex)
            {
                AppLog.Message("Error in PromptAndSaveWorkbook(): " + ex.Message);
            }
            Mouse.SetCursor(Cursors.Arrow);
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
            _studyTreeView = new VariationTreeView(UiRtbStudyTreeView.Document, this);

            // ask for the options
            if (!ShowWorkbookOptionsDialog(false))
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

            SetupGuiForNewSession(AppStateManager.WorkbookFilePath, true);

            AppStateManager.SetupGuiForCurrentStates();
            //StudyTree.CreateNew();
            _studyTreeView.BuildFlowDocumentForVariationTree();
            UiTabStudyTree.Focus();

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
            if (EngineMessageProcessor.IsEngineAvailable)
            {
                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.CONTINUOUS);
                EvaluateActiveLineSelectedPosition();
            }
            else
            {
                MessageBox.Show("Chess Engine is not available.", "Move Evaluation Failure", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// The user requested a line evaluation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnEvaluateLine_Click(object sender, RoutedEventArgs e)
        {
            // a defensive check
            if (ActiveLine.GetPlyCount() <= 1)
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
            if (!EngineMessageProcessor.IsEngineAvailable)
            {
                MessageBox.Show("Chess Engine not available", "Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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
            WorkbookManager.EnableChaptersContextMenuItems(_cmChapters, false, GameData.ContentType.GENERIC);
        }

        /// <summary>
        /// Selects the clicked Chapter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnSelectChapter_Click(object sender, RoutedEventArgs e)
        {
            SelectChapter(WorkbookManager.LastClickedChapterId, true);
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
                AppStateManager.IsDirty = true;
            }
        }

        /// <summary>
        /// Creates a new Chapter and adds it to the Workbook.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnCreateNewChapter_Click(object sender, RoutedEventArgs e)
        {
            Chapter lastActiveChapter = WorkbookManager.SessionWorkbook.ActiveChapter;
            Chapter chapter = WorkbookManager.SessionWorkbook.CreateNewChapter();
            if (ShowChapterTitleDialog(chapter))
            {
                SelectChapter(chapter.Id, false);
                AppStateManager.IsDirty = true;
            }
            else
            {
                // remove the just created Chapter
                WorkbookManager.SessionWorkbook.ActiveChapter = lastActiveChapter;
                SessionWorkbook.Chapters.Remove(chapter);
            }
        }

        /// <summary>
        /// Invokes the Import PGN dialog to allow the user to select games
        /// to merge into a new Study Tree from which a Chapter will be created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnImportChapter_Click(object sender, RoutedEventArgs e)
        {
            string fileName = SelectPgnFile();
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                bool success = false;
                Chapter previousActiveChapter = WorkbookManager.SessionWorkbook.ActiveChapter;

                Chapter chapter = WorkbookManager.SessionWorkbook.CreateNewChapter();
                ObservableCollection<GameData> games = new ObservableCollection<GameData>();

                int gamesCount = WorkbookManager.ReadPgnFile(fileName, ref games, GameData.ContentType.GENERIC);
                if (gamesCount > 0)
                {
                    int processedGames = WorkbookManager.MergeGames(ref chapter.StudyTree, ref games);
                    if (processedGames == 0)
                    {
                        MessageBox.Show("No valid games found. No new chapter has been created.", "PGN Import", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        success = true;
                    }
                }
                else
                {
                    ShowNoGamesError(GameData.ContentType.GENERIC, fileName);
                }

                if (success)
                {
                    _chaptersView.BuildFlowDocumentForChaptersView();
                    SelectChapter(chapter.Id, false);
                }
                else
                {
                    // delete the above created chapter and activate the previously active one
                    WorkbookManager.SessionWorkbook.ActiveChapter = previousActiveChapter;
                    WorkbookManager.SessionWorkbook.Chapters.Remove(chapter);
                }
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
                var res = MessageBox.Show("Deleting chapter \"" + chapter.GetTitle() + ". Are you sure?", "Delete Chapter", MessageBoxButton.YesNoCancel);
                if (res == MessageBoxResult.Yes)
                {
                    WorkbookManager.SessionWorkbook.Chapters.Remove(chapter);
                    if (chapter.Id == WorkbookManager.SessionWorkbook.ActiveChapter.Id)
                    {
                        WorkbookManager.SessionWorkbook.SelectDefaultActiveChapter();
                    }
                    _chaptersView.BuildFlowDocumentForChaptersView();
                    SetupGuiForActiveStudyTree(false);
                    AppStateManager.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Moves chapter up one position in the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnChapterUp_Click(object sender, RoutedEventArgs e)
        {
            int index = WorkbookManager.SessionWorkbook.GetChapterIndexFromId(WorkbookManager.LastClickedChapterId);
            if (index > 0 && index < WorkbookManager.SessionWorkbook.Chapters.Count)
            {
                Chapter hold = WorkbookManager.SessionWorkbook.Chapters[index];
                WorkbookManager.SessionWorkbook.Chapters[index] = WorkbookManager.SessionWorkbook.Chapters[index - 1];
                WorkbookManager.SessionWorkbook.Chapters[index - 1] = hold;
                _chaptersView.BuildFlowDocumentForChaptersView();
                AppStateManager.IsDirty = true;
            }
        }

        /// <summary>
        /// Moves chapter down one position in the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnChapterDown_Click(object sender, RoutedEventArgs e)
        {
            int index = WorkbookManager.SessionWorkbook.GetChapterIndexFromId(WorkbookManager.LastClickedChapterId);
            if (index >= 0 && index < WorkbookManager.SessionWorkbook.Chapters.Count - 1)
            {
                Chapter hold = WorkbookManager.SessionWorkbook.Chapters[index];
                WorkbookManager.SessionWorkbook.Chapters[index] = WorkbookManager.SessionWorkbook.Chapters[index + 1];
                WorkbookManager.SessionWorkbook.Chapters[index + 1] = hold;
                _chaptersView.BuildFlowDocumentForChaptersView();
                AppStateManager.IsDirty = true;
            }
        }

        /// <summary>
        /// Moves game up one position in the chapter's game list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnGameUp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                //int index = chapter.ActiveModelGameIndex;
                int index = WorkbookManager.LastClickedModelGameIndex;
                int gameCount = chapter.GetModelGameCount();

                if (index > 0 && index < gameCount)
                {
                    VariationTree hold = chapter.ModelGames[index];
                    chapter.ModelGames[index] = chapter.ModelGames[index - 1];
                    chapter.ModelGames[index - 1] = hold;
                    chapter.ActiveModelGameIndex = index - 1;

                    _chaptersView.BuildFlowDocumentForChaptersView();
                    AppStateManager.IsDirty = true;
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnGameUp_Click()", ex);
            }
        }

        /// <summary>
        /// Moves game up one position in the chapter's game list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExerciseUp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                int index = WorkbookManager.LastClickedExerciseIndex;
                int exerciseCount = chapter.GetExerciseCount();

                if (index > 0 && index < exerciseCount)
                {
                    VariationTree hold = chapter.Exercises[index];
                    chapter.Exercises[index] = chapter.Exercises[index - 1];
                    chapter.Exercises[index - 1] = hold;
                    chapter.ActiveExerciseIndex = index - 1;

                    _chaptersView.BuildFlowDocumentForChaptersView();
                    AppStateManager.IsDirty = true;
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnExerciseUp_Click()", ex);
            }
        }

        /// <summary>
        /// Moves game down one position in the chapter's game list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnGameDown_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                //int index = chapter.ActiveModelGameIndex;
                int index = WorkbookManager.LastClickedModelGameIndex;
                int gameCount = chapter.GetModelGameCount();

                if (index >= 0 && index < gameCount - 1)
                {
                    VariationTree hold = chapter.ModelGames[index];
                    chapter.ModelGames[index] = chapter.ModelGames[index + 1];
                    chapter.ModelGames[index + 1] = hold;
                    chapter.ActiveModelGameIndex = index + 1;

                    _chaptersView.BuildFlowDocumentForChaptersView();
                    AppStateManager.IsDirty = true;
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnGameDown_Click()", ex);
            }
        }

        /// <summary>
        /// Moves game down one position in the chapter's game list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExerciseDown_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                int index = WorkbookManager.LastClickedExerciseIndex;
                int exerciseCount = chapter.GetExerciseCount();

                if (index >= 0 && index < exerciseCount - 1)
                {
                    VariationTree hold = chapter.Exercises[index];
                    chapter.Exercises[index] = chapter.Exercises[index + 1];
                    chapter.Exercises[index + 1] = hold;
                    chapter.ActiveExerciseIndex = index + 1;

                    _chaptersView.BuildFlowDocumentForChaptersView();
                    AppStateManager.IsDirty = true;
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnExerciseDown_Click()", ex);
            }
        }


        /// <summary>
        /// Requests import of Model Games from a PGN file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnImportModelGames_Click(object sender, RoutedEventArgs e)
        {
            ImportGamesFromPgn(GameData.ContentType.GENERIC);
        }

        /// <summary>
        /// Requests import of Model Games from a PGN file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnImportGame_Click(object sender, RoutedEventArgs e)
        {
            UiMnImportModelGames_Click(sender, e);
        }

        /// <summary>
        /// Requests import of Exercises from a PGN file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnImportExercises_Click(object sender, RoutedEventArgs e)
        {
            ImportGamesFromPgn(GameData.ContentType.EXERCISE);
        }

        /// <summary>
        /// Imports Model Games or Exercises from a PGN file.
        /// </summary>
        /// <param name="contentType"></param>
        private int ImportGamesFromPgn(GameData.ContentType contentType)
        {
            int gameCount = 0;
            if ((contentType == GameData.ContentType.GENERIC || contentType == GameData.ContentType.MODEL_GAME || contentType == GameData.ContentType.EXERCISE)
                && WorkbookManager.SessionWorkbook.ActiveChapter != null)
            {
                string fileName = SelectPgnFile();
                if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                    ObservableCollection<GameData> games = new ObservableCollection<GameData>();
                    gameCount = WorkbookManager.ReadPgnFile(fileName, ref games, contentType);

                    int errorCount = 0;
                    StringBuilder sbErrors = new StringBuilder();

                    if (gameCount > 0)
                    {
                        if (ShowSelectGamesDialog(contentType, ref games))
                        {
                            Mouse.SetCursor(Cursors.Wait);
                            try
                            {
                                for (int i = 0; i < games.Count; i++)
                                {
                                    if (games[i].IsSelected)
                                    {
                                        try
                                        {
                                            chapter.AddGame(games[i], contentType);
                                            AppStateManager.IsDirty = true;
                                        }
                                        catch (Exception ex)
                                        {
                                            errorCount++;
                                            sbErrors.Append(TextUtils.BuildGameProcessingErrorText(games[i], i + 1, ex.Message));
                                        }
                                    }
                                }
                                RefreshChaptersViewAfterImport(contentType, chapter);
                            }
                            catch { }

                            Mouse.SetCursor(Cursors.Arrow);
                        }
                        else
                        {
                            gameCount = 0;
                        }
                    }
                    else
                    {
                        ShowNoGamesError(contentType, fileName);
                    }

                    if (errorCount > 0)
                    {
                        TextBoxDialog tbDlg = new TextBoxDialog("PGN Parsing Errors", sbErrors.ToString())
                        {
                            Left = ChessForgeMain.Left + 100,
                            Top = ChessForgeMain.Top + 100,
                            Topmost = false,
                            Owner = this
                        };
                        tbDlg.Show();
                    }
                }
            }
            return gameCount;
        }


        //*****************************************************************************
        //
        //   MODEL GAMES and EXERCISES MENUS
        //
        //*****************************************************************************

        /// <summary>
        /// Creates a new Model Game from the Chapters View context menu.
        /// If successfully returned, adds the Tree to the list of Model Games
        /// and opens the ModelGames view (where the game text will be empty)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnChpt_CreateModelGame_Click(object sender, RoutedEventArgs e)
        {
            CreateNewModelGame();
        }

        /// <summary>
        /// Creates a new Model Game from the Games View context menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _UiMnGame_CreateModelGame_Click(object sender, RoutedEventArgs e)
        {
            CreateNewModelGame();
        }

        /// <summary>
        /// Creates a new Exercise from the Model Games View context menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnGame_CreateExercise_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TreeNode nd = _modelGameTreeView.GetSelectedNode();
                if (nd != null)
                {
                    VariationTree tree = VariationTree.CreateNewTreeFromNode(nd, GameData.ContentType.EXERCISE);
                    Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                    CopyHeaderFromGame(tree, chapter.GetActiveModelGameHeader());
                    CreateNewExerciseFromTree(tree);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnGame_CreateExercise_Click()", ex);
            }
        }

        /// <summary>
        /// Creates a new Exercise starting from the position currently selected in the Study Tree.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnStudy_CreateExercise_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TreeNode nd = _studyTreeView.GetSelectedNode();
                if (nd != null)
                {
                    VariationTree tree = VariationTree.CreateNewTreeFromNode(nd, GameData.ContentType.EXERCISE);
                    Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                    CopyHeaderFromGame(tree, chapter.StudyTree.Header);
                    if (string.IsNullOrEmpty(tree.Header.GetEventName(out _)))
                    {
                        tree.Header.SetHeaderValue(PgnHeaders.KEY_EVENT, "Study Tree after " + MoveUtils.BuildSingleMoveText(nd, true, true));
                    }
                    CreateNewExerciseFromTree(tree);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnStudy_CreateExercise_Click()", ex);
            }
        }

        /// <summary>
        /// Copies a header from a GameHeader object to the Tree.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="header"></param>
        private void CopyHeaderFromGame(VariationTree tree, GameHeader header)
        {
            tree.Header.SetHeaderValue(PgnHeaders.KEY_WHITE, header.GetWhitePlayer(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_BLACK, header.GetBlackPlayer(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_RESULT, header.GetResult(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_EVENT, header.GetEventName(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_DATE, header.GetDate(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_ROUND, header.GetRound(out _));
        }

        /// <summary>
        /// Creates a new Exercise from the Exercise View context menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExerc_CreateExercise_Click(object sender, RoutedEventArgs e)
        {
            CreateNewExercise();
        }

        /// <summary>
        /// Deletes a Model Game.
        /// Invoked from Games View menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnGame_DeleteModelGame_Click(object sender, RoutedEventArgs e)
        {
            DeleteModelGame();
        }

        /// <summary>
        /// Deletes an Exercise.
        /// Invoked from Exercises View menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExerc_DeleteExercise_Click(object sender, RoutedEventArgs e)
        {
            DeleteExercise();
        }

        /// <summary>
        /// Edits a Model Game header.
        /// Invoked from Games View menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnGame_EditHeader_Click(object sender, RoutedEventArgs e)
        {
            EditGameHeader();
        }

        /// <summary>
        /// Edits an Exercise header.
        /// Invoked from Exercises View menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExerc_EditHeader_Click(object sender, RoutedEventArgs e)
        {
            EditExerciseHeader();
        }

        /// <summary>
        /// Edits a Game header.
        /// Invoked from Chapters View menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnEditGameHeader_Click(object sender, RoutedEventArgs e)
        {
            EditGameHeader();
        }

        /// <summary>
        /// Edits an Exercise header.
        /// Invoked from Exercises View menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnEditExerciseHeader_Click(object sender, RoutedEventArgs e)
        {
            EditExerciseHeader();
        }

        /// <summary>
        /// Opens the Active Model Game. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnOpenGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                if (chapter.ActiveModelGameIndex >= 0)
                {
                    SelectModelGame(chapter.ActiveModelGameIndex, true);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnOpenGame_Click()", ex);
            }
        }

        /// <summary>
        /// Opens the last clicked exercise. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnOpenExercise_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                if (chapter.ActiveExerciseIndex >= 0)
                {
                    SelectExercise(chapter.ActiveExerciseIndex, true);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnOpenExercise_Click()", ex);
            }
        }

        /// <summary>
        /// Deletes the selected Game.
        /// Invoked from the Chapters View context menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnChpt_DeleteGame_Click(object sender, RoutedEventArgs e)
        {
            DeleteModelGame();
        }

        /// <summary>
        /// Deletes the selected Exercise.
        /// Invoked from the Chapters View context menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnChpt_DeleteExercise_Click(object sender, RoutedEventArgs e)
        {
            DeleteExercise();
        }

        /// <summary>
        /// Creates a new VariationTree object and invokes
        /// the Position Setup dialog.
        /// If successfully returned, adds the Tree to the list of Exercises
        /// and opens the Exercise view (where the Exercise text will be empty)
        /// Invoked from the Chapters View context menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnChpt_AddExercise_Click(object sender, RoutedEventArgs e)
        {
            CreateNewExercise();
        }

        /// <summary>
        /// Sets the list in the Chapters View in the correct Expand/Collapse state.
        /// Rebuilds the Paragraph for the chapter.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="chapter"></param>
        public void RefreshChaptersViewAfterImport(GameData.ContentType contentType, Chapter chapter)
        {
            chapter.IsViewExpanded = true;
            switch (contentType)
            {
                case GameData.ContentType.MODEL_GAME:
                    chapter.IsModelGamesListExpanded = true;
                    break;
                case GameData.ContentType.EXERCISE:
                    chapter.IsExercisesListExpanded = true;
                    break;
            }

            _chaptersView.RebuildChapterParagraph(WorkbookManager.SessionWorkbook.ActiveChapter);
        }

        /// <summary>
        /// Show the Select Games dialog.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="games"></param>
        /// <returns></returns>
        private bool ShowSelectGamesDialog(GameData.ContentType contentType, ref ObservableCollection<GameData> games)
        {
            string dlgTitle = "";
            if (contentType == GameData.ContentType.MODEL_GAME)
            {
                dlgTitle = "Select Model Games to Import";
            }
            else if (contentType == GameData.ContentType.EXERCISE)
            {
                dlgTitle = "Select Exercises to Import";
            }

            SelectGamesDialog dlg = new SelectGamesDialog(ref games, dlgTitle)
            {
                Left = ChessForgeMain.Left + 100,
                Top = ChessForgeMain.Top + 100,
                Topmost = false,
                Owner = AppStateManager.MainWin
            };
            dlg.ShowDialog();
            return dlg.ExitOK;
        }

        /// <summary>
        /// Show the error when no games were found in the file.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="fileName"></param>
        private void ShowNoGamesError(GameData.ContentType contentType, string fileName)
        {
            string sError;
            if (contentType == GameData.ContentType.EXERCISE)
            {
                sError = "No Exercises found in ";
            }
            else
            {
                sError = "No Games found in ";
            }
            MessageBox.Show(sError + fileName, "Import PGN", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Shows the dialog with info about generic PGN files.
        /// </summary>
        /// <returns></returns>
        public bool ShowGenericPgnInfoDialog()
        {
            bool res = true;

            if (Configuration.ShowGenericPgnInfo)
            {
                GenericPgnInfoDialog dlgInfo = new GenericPgnInfoDialog
                {
                    Left = ChessForgeMain.Left + 100,
                    Top = ChessForgeMain.Top + 100,
                    Topmost = false,
                    Owner = this
                };
                dlgInfo.ShowDialog();
                if (dlgInfo.ExitOk)
                {
                    if (Configuration.ShowGenericPgnInfo != dlgInfo.ShowGenericPgnInfo)
                    {
                        Configuration.ShowGenericPgnInfo = dlgInfo.ShowGenericPgnInfo;
                        Configuration.WriteOutConfiguration();
                    }
                    res = true;
                }
                else
                {
                    res = false;
                }
            }

            return res;
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
            UiTabStudyTree.Focus();
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
            int ret = BookmarkManager.AddAllSiblingsToBookmarks(_studyTreeView.LastClickedNodeId);
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
            int ret = BookmarkManager.AddBookmark(_studyTreeView.LastClickedNodeId);
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
                    SetStudyStateOnFocus();

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
        /// Handles Game import from the Games context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnGame_ImportGames_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                int count = chapter.GetModelGameCount();
                if (ImportGamesFromPgn(GameData.ContentType.GENERIC) > 0)
                {

                    if (chapter.GetModelGameCount() > count)
                    {
                        chapter.ActiveModelGameIndex = count;
                    }
                    else
                    {
                        chapter.ActiveModelGameIndex = count - 1;
                    }
                    SelectModelGame(chapter.ActiveModelGameIndex, false);
                }
            }
            catch
            {
            }
        }


        /// <summary>
        /// Handles Exercise import from the Exercises context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExerc_ImportExercises_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                int count = chapter.GetExerciseCount();
                if (ImportGamesFromPgn(GameData.ContentType.EXERCISE) > 0)
                {
                    if (chapter.GetExerciseCount() > count)
                    {
                        chapter.ActiveExerciseIndex = count;
                    }
                    else
                    {
                        chapter.ActiveExerciseIndex = count - 1;
                    }
                    SelectExercise(chapter.ActiveExerciseIndex, false);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// The user requested from the Study menu to promote the currently selected line.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnPromoteLine_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView.PromoteCurrentLine();
        }

        /// <summary>
        /// The user requested from the Exercises menu to promote the currently selected line.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExerc_PromoteLine_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView.PromoteCurrentLine();
        }

        /// <summary>
        /// The user requested from the Games menu to promote the currently selected line.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnGame_PromoteLine_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView.PromoteCurrentLine();
        }


        /// <summary>
        /// The user requested from the Study menu to delete the sub-tree starting at the selected node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDeleteMovesFromHere_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView.DeleteRemainingMoves();
        }

        /// <summary>
        /// The user requested from the Exercises menu to delete the sub-tree starting at the selected node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExerc_DeleteMovesFromHere_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView.DeleteRemainingMoves();
        }

        /// <summary>
        /// The user requested from the Games menu to delete the sub-tree starting at the selected node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnGame_DeleteMovesFromHere_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView.DeleteRemainingMoves();
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
            AboutBoxDialog dlg = new AboutBoxDialog()
            {
                Left = ChessForgeMain.Left + 100,
                Top = ChessForgeMain.Top + 100,
                Topmost = false,
                Owner = this
            };
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
                if (ShowWorkbookOptionsDialog(false))
                {
                    AppStateManager.IsDirty = true;
                }
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

        /// <summary>
        /// Calls the Internet browser to open the ChessForge Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnHelpWiki_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki");
        }


        //********************************************************
        //
        // Methods invoked from Menu Evant handlers 
        //
        //********************************************************


        /// <summary>
        /// Creates a new Model Game and makes it "Active".
        /// </summary>
        private void CreateNewModelGame()
        {
            try
            {
                VariationTree tree = new VariationTree(GameData.ContentType.MODEL_GAME);
                GameHeaderDialog dlg = new GameHeaderDialog(tree, "Game Header")
                {
                    Left = ChessForgeMain.Left + 100,
                    Top = ChessForgeMain.Top + 100,
                    Topmost = false,
                    Owner = this
                };
                dlg.ShowDialog();
                if (dlg.ExitOK)
                {
                    WorkbookManager.SessionWorkbook.ActiveChapter.AddModelGame(tree);
                    WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex
                        = WorkbookManager.SessionWorkbook.ActiveChapter.GetModelGameCount() - 1;
                    _chaptersView.BuildFlowDocumentForChaptersView();
                    SelectModelGame(WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex, true);
                    AppStateManager.IsDirty = true;
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
        private void CreateNewExercise()
        {
            try
            {
                PositionSetupDialog dlgPosSetup = new PositionSetupDialog()
                {
                    Left = ChessForgeMain.Left + 100,
                    Top = ChessForgeMain.Top + 100,
                    Topmost = false,
                    Owner = this
                };
                dlgPosSetup.ShowDialog();

                if (dlgPosSetup.ExitOK)
                {
                    BoardPosition pos = dlgPosSetup.PositionSetup;

                    VariationTree tree = new VariationTree(GameData.ContentType.EXERCISE);
                    tree.CreateNew(pos);

                    GameHeaderDialog dlgHeader = new GameHeaderDialog(tree, "Exercise Header")
                    {
                        Left = ChessForgeMain.Left + 100,
                        Top = ChessForgeMain.Top + 100,
                        Topmost = false,
                        Owner = this
                    };

                    dlgHeader.ShowDialog();
                    if (dlgHeader.ExitOK)
                    {
                        CreateNewExerciseFromTree(tree);
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
        private void CreateNewExerciseFromTree(VariationTree tree)
        {
            try
            {
                WorkbookManager.SessionWorkbook.ActiveChapter.AddExercise(tree);
                WorkbookManager.SessionWorkbook.ActiveChapter.ActiveExerciseIndex
                    = WorkbookManager.SessionWorkbook.ActiveChapter.GetExerciseCount() - 1;
                _chaptersView.BuildFlowDocumentForChaptersView();
                SelectExercise(WorkbookManager.SessionWorkbook.ActiveChapter.ActiveExerciseIndex, true);
                AppStateManager.IsDirty = true;
            }
            catch (Exception ex)
            {
                AppLog.Message("CreateNewExercise()", ex);
            }
        }

        /// <summary>
        /// Deletes a Model Game
        /// </summary>
        private void DeleteModelGame()
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                if (chapter.ActiveModelGameIndex >= 0)
                {
                    string gameTitle = chapter.ModelGames[chapter.ActiveModelGameIndex].Header.BuildGameHeaderLine();
                    if (MessageBox.Show("Delete this Game?\n\n  " + gameTitle, "Delete Model Game", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        DeleteModelGame(chapter.ActiveModelGameIndex);

                        int gameCount = chapter.GetModelGameCount();
                        if (chapter.GetModelGameCount() == 0)
                        {
                            chapter.ActiveModelGameIndex = -1;
                            DisplayPosition(PositionUtils.SetupStartingPosition());
                        }
                        else if (chapter.ActiveModelGameIndex >= gameCount - 1)
                        {
                            chapter.ActiveModelGameIndex = gameCount - 1;
                        }

                        _chaptersView.RebuildChapterParagraph(WorkbookManager.SessionWorkbook.ActiveChapter);
                        if (WorkbookManager.ActiveTab == WorkbookManager.TabViewType.MODEL_GAME)
                        {
                            SelectModelGame(chapter.ActiveModelGameIndex, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("DeleteModelGame()", ex);
            }
        }

        /// <summary>
        /// Deletes an Exercise
        /// </summary>
        private void DeleteExercise()
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                if (chapter.ActiveExerciseIndex >= 0)
                {
                    string exerciseTitle = chapter.Exercises[chapter.ActiveExerciseIndex].Header.BuildGameHeaderLine();
                    if (MessageBox.Show("Delete this Exercise?\n\n  " + exerciseTitle, "Delete Exercise", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        DeleteExercise(chapter.ActiveExerciseIndex);

                        int exerciseCount = chapter.GetExerciseCount();
                        if (exerciseCount == 0)
                        {
                            chapter.ActiveExerciseIndex = -1;
                            DisplayPosition(PositionUtils.SetupStartingPosition());
                        }
                        else if (chapter.ActiveExerciseIndex >= exerciseCount - 1)
                        {
                            chapter.ActiveExerciseIndex = exerciseCount - 1;
                        }

                        _chaptersView.RebuildChapterParagraph(WorkbookManager.SessionWorkbook.ActiveChapter);
                        if (WorkbookManager.ActiveTab == WorkbookManager.TabViewType.EXERCISE)
                        {
                            SelectExercise(chapter.ActiveExerciseIndex, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("DeleteExercise()", ex);
            }
        }

        /// <summary>
        /// Deletes the Game at the requested index from the list of games.
        /// </summary>
        /// <param name="index"></param>
        private void DeleteModelGame(int index)
        {
            Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
            int gameCount = chapter.GetModelGameCount();
            if (index >= 0 && index < gameCount)
            {
                chapter.ModelGames.RemoveAt(index);
                AppStateManager.IsDirty = true;
            }
        }

        /// <summary>
        /// Deletes the Exercise at the requested index from the list of games.
        /// </summary>
        /// <param name="index"></param>
        private void DeleteExercise(int index)
        {
            Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
            int exerciseCount = chapter.GetExerciseCount();
            if (index >= 0 && index < exerciseCount)
            {
                chapter.Exercises.RemoveAt(index);
                AppStateManager.IsDirty = true;
            }
        }

        /// <summary>
        /// Invokes the dialog for editing game header.
        /// </summary>
        private void EditGameHeader()
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;

                VariationTree game = WorkbookManager.SessionWorkbook.ActiveChapter.ModelGames[chapter.ActiveModelGameIndex];
                var dlg = new GameHeaderDialog(game, "Game Header")
                {
                    Left = ChessForgeMain.Left + 100,
                    Top = ChessForgeMain.Top + 100,
                    Topmost = false,
                    Owner = this
                };
                dlg.ShowDialog();
                if (dlg.ExitOK)
                {
                    AppStateManager.IsDirty = true;
                    _chaptersView.BuildFlowDocumentForChaptersView();
                    if (WorkbookManager.ActiveTab == WorkbookManager.TabViewType.MODEL_GAME)
                    {
                        _modelGameTreeView.BuildFlowDocumentForVariationTree();
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Error in UiMnEditGameHeader_Click(): " + ex.Message);
            }
        }

        /// <summary>
        /// Invokes the dialog for editing game header.
        /// </summary>
        private void EditExerciseHeader()
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;

                VariationTree game = WorkbookManager.SessionWorkbook.ActiveChapter.Exercises[chapter.ActiveExerciseIndex];
                var dlg = new GameHeaderDialog(game, "Exercise Header")
                {
                    Left = ChessForgeMain.Left + 100,
                    Top = ChessForgeMain.Top + 100,
                    Topmost = false,
                    Owner = this
                };
                dlg.ShowDialog();
                if (dlg.ExitOK)
                {
                    AppStateManager.IsDirty = true;
                    _chaptersView.BuildFlowDocumentForChaptersView();
                    if (WorkbookManager.ActiveTab == WorkbookManager.TabViewType.EXERCISE)
                    {
                        _exerciseTreeView.BuildFlowDocumentForVariationTree();
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("EditExerciseHeader()" + ex.Message);
            }
        }
    }
}

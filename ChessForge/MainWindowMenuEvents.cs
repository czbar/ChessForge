using ChessPosition;
using GameTree;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChessPosition.GameTree;
using System.Windows.Documents;

namespace ChessForge
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Handles creation of a new Workbook
        /// </summary>
        /// <returns></returns>
        public bool CreateNewWorkbook()
        {
            if (!WorkbookManager.AskToCloseWorkbook())
            {
                return false;
            }

            // prepare document
            AppState.RestartInIdleMode(false);
            WorkbookManager.CreateNewWorkbook();

            // TODO: this call looks unnecessary as SetupGuiForNewSession() below creates this view again.
            _studyTreeView = new StudyTreeView(UiRtbStudyTreeView, GameData.ContentType.STUDY_TREE);

            // ask for the options
            if (!ShowWorkbookOptionsDialog())
            {
                // user abandoned
                return false;
            }

            if (!WorkbookManager.SaveWorkbookToNewFile(null))
            {
                AppState.RestartInIdleMode(false);
                return false;
            }

            BoardCommentBox.ShowTabHints();

            LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);

            SetupGuiForNewSession(AppState.WorkbookFilePath, true, null);

            AppState.SetupGuiForCurrentStates();
            //StudyTree.CreateNew();
            _studyTreeView.BuildFlowDocumentForVariationTree();
            UiTabStudyTree.Focus();

            int startingNode = 0;
            string startLineId = ActiveVariationTree.GetDefaultLineIdForNode(startingNode);
            ActiveVariationTree.SetSelectedLineAndMove(startLineId, startingNode);
            SetActiveLine(startLineId, startingNode);

            return true;
        }

        /// <summary>
        /// Promotes the currently selected line.
        /// </summary>
        public void PromoteLine()
        {
            TreeNode nd = ActiveTreeView.GetSelectedNode();
            if (nd != null)
            {
                // must set the LastClickedNode as this is what the Promote method takes as the "current" line.
                ActiveTreeView.LastClickedNodeId = nd.NodeId;
                UiMnPromoteLine_Click(null, null);
            }
        }

        /// <summary>
        /// Rebuilds all tree views
        /// </summary>
        public void RebuildAllTreeViews(bool? increaseFontDirection = null, bool? updateColors = null)
        {
            _studyTreeView?.BuildFlowDocumentForVariationTree();
            _modelGameTreeView?.BuildFlowDocumentForVariationTree();
            _exerciseTreeView?.BuildFlowDocumentForVariationTree();
            _chaptersView?.BuildFlowDocumentForChaptersView();

            if (TrainingSession.IsTrainingInProgress)
            {
                UiTrainingView.IncrementFontSize(increaseFontDirection);
                if (updateColors == true)
                {
                    UiTrainingView.UpdateColors();
                }
            }
            else
            {
                RestoreSelectedLineAndMoveInActiveView();
            }
        }

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
            if (PrepareToReadWorkbook())
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Multiselect = false,
                    Filter = Properties.Resources.Workbooks + " (*.pgn)|*.pgn;*.pgn"
                };

                string initDir;
                if (!string.IsNullOrEmpty(Configuration.LastOpenDirectory))
                {
                    initDir = Configuration.LastOpenDirectory;
                }
                else
                {
                    initDir = App.AppPath;
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
            if (PrepareToReadWorkbook())
            {
                string menuItemName = ((MenuItem)e.Source).Name;
                string path = Configuration.GetRecentFile(menuItemName);
                Configuration.LastOpenDirectory = Path.GetDirectoryName(path);
                ReadWorkbookFile(path, false, ref WorkbookManager.VariationTreeList);
            }
        }

        /// <summary>
        /// Obtains the content of the private libray, if configured.
        /// If nor configured, opens the configuration dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnOnlineLibraryPrivate_Click(object sender, RoutedEventArgs e)
        {
            // if not specified, ask the user to select/configure
            if (string.IsNullOrEmpty(Configuration.LastPrivateLibrary))
            {
                UiMnOnlineLibraries_Click(null, null);
            }
            else
            {
                if (!ShowLibraryContent(Configuration.LastPrivateLibrary))
                {
                    UiMnOnlineLibraries_Click(null, null);
                }
            }
        }

        /// <summary>
        /// Obtains the content of the online library.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnOnlineLibrary_Click(object sender, RoutedEventArgs e)
        {
            ShowLibraryContent(Configuration.PUBLIC_LIBRARY_URL);
        }

        /// <summary>
        /// Obtains and displays content of the online library.
        /// </summary>
        /// <param name="url"></param>
        private bool ShowLibraryContent(string url)
        {
            bool result = false;
            string error;

            WebAccess.LibraryContent library = ReadLibraryContentFile(url, true, out error);
            if (library == null)
            {
                MessageBox.Show(Properties.Resources.ErrAccessOnlineLibrary + ": " + error, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                result = true;
                Point leftTop = Application.Current.MainWindow.PointToScreen(new Point(0, 0));

                double offset = 50;
                double dlgHeight = Math.Max(ChessForgeMain.ActualHeight - (2 * offset + 40), 550);
                double dlgWidth = Math.Max(ChessForgeMain.ActualWidth - (2 * offset), 800);

                OnlineLibraryContentDialog dlg = new OnlineLibraryContentDialog(library)
                {
                    Left = leftTop.X + offset,
                    Top = leftTop.Y + offset,
                    Width = dlgWidth,
                    Height = dlgHeight,
                    Topmost = false,
                    Owner = this
                };


                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        Mouse.SetCursor(Cursors.Wait);

                        // download the workbook
                        string bookText = WebAccess.OnlineLibrary.GetWorkbookText(url, dlg.SelectedBook.File, out error);
                        if (!string.IsNullOrEmpty(error))
                        {
                            MessageBox.Show(Properties.Resources.ErrAccessOnlineLibrary + ": " + error, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            if (PrepareToReadWorkbook())
                            {
                                WorkbookManager.ReadPgnFile(bookText, ref WorkbookManager.VariationTreeList, GameData.ContentType.GENERIC, GameData.ContentType.NONE);
                                bool res = WorkbookManager.PrepareWorkbook(ref WorkbookManager.VariationTreeList, out bool isChessForgeFile);
                                if (!isChessForgeFile)
                                {
                                    MessageBox.Show(Properties.Resources.ErrFileFormatOrCorrupt, Properties.Resources.ErrLibraryDownload, MessageBoxButton.OK, MessageBoxImage.Error);
                                    AppState.RestartInIdleMode();
                                }
                                else
                                {
                                    SetupGuiForNewSession("", true, null);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLog.Message("UiMnOnlineLibrary_Click()", ex);
                    }
                    finally
                    {
                        Mouse.SetCursor(Cursors.Arrow);
                    }
                }
                else
                {
                    if (dlg.ShowLibraries)
                    {
                        UiMnOnlineLibraries_Click(null, null);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Reads the library content file respecting redirects.
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        private WebAccess.LibraryContent ReadLibraryContentFile(string url, bool isPublic, out string error)
        {
            error = "";
            int allowedRedirections = 5;

            WebAccess.LibraryContent library = null;

            for (int i = 0; i < allowedRedirections; i++)
            {
                library = WebAccess.OnlineLibrary.GetLibraryContent(url, out error);
                if (library == null || string.IsNullOrWhiteSpace(library.Redirect))
                {
                    break;
                }
            }

            if (library != null && !string.IsNullOrWhiteSpace(library.Redirect))
            {
                library = null;
            }

            return library;
        }

        /// <summary>
        /// Checks if we can proceed with the opening of the Workbook.
        /// </summary>
        /// <returns></returns>
        private bool PrepareToReadWorkbook()
        {
            bool proceed = true;

            // check with the user if required
            if (WorkbookManager.SessionWorkbook != null)
            {
                if (AppState.IsDirty)
                {
                    proceed = WorkbookManager.PromptAndSaveWorkbook(false, out _);
                }
                else
                {
                    // not dirty but the state may have changed
                    WorkbookViewState wvs = new WorkbookViewState(SessionWorkbook);
                    wvs.SaveState();
                }
            }

            if (proceed && ChangeAppModeWarning(LearningMode.Mode.MANUAL_REVIEW))
            {
                AppState.RestartInIdleMode();
            }
            else
            {
                proceed = false;
            }

            return proceed;
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
        /// Request to open the Comment Before Move dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnCommentBeforeMoveDialog_Click(object sender, RoutedEventArgs e)
        {
            TreeNode nd = ActiveLine.GetSelectedTreeNode();
            if (InvokeCommentBeforeMoveDialog(nd))
            {
                ActiveTreeView.InsertOrUpdateCommentBeforeMoveRun(nd);
            }
        }


        /// <summary>
        /// The users requesting merging of chapters.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnMergeChapters_Click(object sender, RoutedEventArgs e)
        {
            SelectChaptersDialog dlg = new SelectChaptersDialog(WorkbookManager.SessionWorkbook, Properties.Resources.SelectChaptersToMerge);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

            dlg.ShowDialog();
            if (dlg.ExitOK)
            {
                int mergedCount = 0;
                VariationTree merged = null;
                string title = "";

                foreach (SelectedChapter ch in dlg.ChapterList)
                {
                    if (ch.IsSelected)
                    {
                        if (mergedCount == 0)
                        {
                            title = ch.Chapter.Title;
                            merged = ch.Chapter.StudyTree.Tree;
                        }
                        else
                        {
                            merged = TreeMerge.MergeVariationTrees(merged, ch.Chapter.StudyTree.Tree);
                        }
                        mergedCount++;
                    }
                }

                if (mergedCount > 1 && _chaptersView != null)
                {
                    List<Chapter> sourceChapters = new List<Chapter>();
                    foreach (SelectedChapter ch in dlg.ChapterList)
                    {
                        if (ch.IsSelected)
                        {
                            sourceChapters.Add(ch.Chapter);
                        }
                    }

                    // Prepare Flash Notification text
                    StringBuilder flash = new StringBuilder("Merging chapters:");
                    foreach (Chapter chapter in sourceChapters)
                    {
                        flash.Append(" [" + (chapter.Index + 1).ToString() + "]");
                    }


                    WorkbookManager.SessionWorkbook.MergeChapters(merged, title, sourceChapters);
                    _chaptersView.BuildFlowDocumentForChaptersView();
                    UiTabChapters.Focus();

                    // complete and display the Flash notification
                    flash.Append("\ninto new chapter [" + (WorkbookManager.SessionWorkbook.GetChapterCount()).ToString() + "]");
                    AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(flash.ToString(), CommentBox.HintType.INFO);

                    PulseManager.ChapterIndexToBringIntoView = WorkbookManager.SessionWorkbook.GetChapterCount() - 1;
                }

                AppState.IsDirty = mergedCount > 1;
            }
        }

        /// <summary>
        /// Deletes all selected chapters.
        /// </summary>
        /// <param name="sources"></param>
        private void DeleteChapters(ObservableCollection<SelectedChapter> chapters)
        {
            List<Chapter> lstChapters = new List<Chapter>();
            foreach (SelectedChapter ch in chapters)
            {
                if (ch.IsSelected)
                {
                    lstChapters.Add(ch.Chapter);
                }
            }

            if (lstChapters.Count > 0)
            {
                WorkbookManager.SessionWorkbook.DeleteChapters(lstChapters);
            }
        }

        /// <summary>
        /// Paste content of clipboard into the view or workbook
        /// if possible.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnPaste_Click(object sender, RoutedEventArgs e)
        {
            CopyPasteMoves.PasteMoveList();
        }

        /// <summary>
        /// The user requested Undo. 
        /// If the active view is Chapters then this is a workbook/chapter operation that needs undoing.
        /// If we are in Study, Game or Exercise view, it could be a tree operation
        /// or an Delete Game/Exercise operation. If there is no Active Tree (e.g. because 
        /// we deleted the last game) then it was a Delete operation, otherwise compare the timestamps
        /// on the Wokbook and Tree operations.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnUndo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (WorkbookManager.SessionWorkbook == null
                    || AppState.CurrentLearningMode == LearningMode.Mode.TRAINING
                    || AppState.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
                {
                    return;
                }

                if (AppState.ActiveTab == TabViewType.CHAPTERS || AppState.ActiveVariationTree == null)
                {
                    UndoWorkbookOperation();
                }
                else if (AppState.ActiveTab == TabViewType.STUDY
                     || AppState.ActiveTab == TabViewType.MODEL_GAME
                     || AppState.ActiveTab == TabViewType.EXERCISE)
                {
                    if (WorkbookManager.SessionWorkbook.OpsManager.Timestamp > AppState.ActiveVariationTree.OpsManager.Timestamp)
                    {
                        UndoWorkbookOperation();
                    }
                    else
                    {
                        // if no operations for the tree
                        // try the Workbook level undo
                        if (!UndoTreeEditOperation())
                        {
                            UndoWorkbookOperation();
                        }
                    }
                }

                AppState.IsDirty = true;
            }
            catch { }
        }

        /// <summary>
        /// Undo the last EditOperation 
        /// </summary>
        private bool UndoTreeEditOperation()
        {
            if (AppState.ActiveVariationTree == null || AppState.ActiveVariationTree.OpsManager.IsEmpty)
            {
                return false;
            }

            AppState.ActiveVariationTree.OpsManager.Undo(out EditOperation.EditType opType, out string selectedLineId, out int selectedNodeId);

            TreeNode selectedNode = AppState.ActiveVariationTree.GetNodeFromNodeId(selectedNodeId);
            ActiveLine.UpdateMoveText(selectedNode);

            MultiTextBoxManager.ShowEvaluationChart(true);

            if (selectedNode == null)
            {
                selectedNodeId = 0;
                selectedLineId = "1";
                MainChessBoard.DisplayPosition(AppState.ActiveVariationTree.RootNode, true);
            }

            AppState.ActiveVariationTree.BuildLines();
            if (!string.IsNullOrEmpty(selectedLineId))
            {
                AppState.MainWin.SetActiveLine(selectedLineId, selectedNodeId);
            }
            else if (selectedNodeId >= 0)
            {
                if (selectedNode != null)
                {
                    selectedLineId = selectedNode.LineId;
                    AppState.MainWin.SetActiveLine(selectedLineId, selectedNodeId);
                }
            }

            AppState.MainWin.ActiveTreeView.BuildFlowDocumentForVariationTree();
            if (opType == EditOperation.EditType.UPDATE_ANNOTATION)
            {
                AppState.MainWin.ActiveTreeView.InsertOrUpdateCommentRun(selectedNode);
            }
            else if (opType == EditOperation.EditType.UPDATE_COMMENT_BEFORE_MOVE)
            {
                AppState.MainWin.ActiveTreeView.InsertOrUpdateCommentBeforeMoveRun(selectedNode);
            }

            if (!string.IsNullOrEmpty(selectedLineId))
            {
                AppState.MainWin.ActiveTreeView.SelectLineAndMove(selectedLineId, selectedNodeId);
            }

            AppState.MainWin.ActiveTreeView.BringSelectedRunIntoView();

            AppState.IsDirty = true;

            return true;
        }

        /// <summary>
        /// Undo the last WorkbookOperation 
        /// </summary>
        private void UndoWorkbookOperation()
        {
            try
            {
                WorkbookOperation op = WorkbookManager.SessionWorkbook.OpsManager.Peek();
                if (op != null)
                {
                    ConfirmUndoDialog dlg = new ConfirmUndoDialog(op);
                    GuiUtilities.PositionDialog(dlg, this, 100);
                    if (dlg.ShowDialog() == true)
                    {
                        if (WorkbookManager.SessionWorkbook.OpsManager.Undo(out WorkbookOperationType opType, out int selectedChapterIndex, out int selectedArticleIndex))
                        {
                            switch (opType)
                            {
                                case WorkbookOperationType.RENAME_CHAPTER:
                                    AppState.MainWin.ActiveTreeView?.BuildFlowDocumentForVariationTree();
                                    _chaptersView.BuildFlowDocumentForChaptersView();
                                    break;
                                case WorkbookOperationType.DELETE_CHAPTER:
                                case WorkbookOperationType.CREATE_CHAPTER:
                                    _chaptersView.BuildFlowDocumentForChaptersView();
                                    if (AppState.ActiveTab != TabViewType.CHAPTERS)
                                    {
                                        UiTabChapters.Focus();
                                    }
                                    AppState.DoEvents();
                                    _chaptersView.BringChapterIntoViewByIndex(selectedChapterIndex);
                                    break;
                                case WorkbookOperationType.CREATE_ARTICLE:
                                    if (AppState.ActiveTab == TabViewType.CHAPTERS)
                                    {
                                        _chaptersView.BuildFlowDocumentForChaptersView();
                                    }
                                    else
                                    {
                                        _chaptersView.IsDirty = true;
                                        SelectModelGame(selectedArticleIndex, true);
                                    }
                                    break;
                                case WorkbookOperationType.DELETE_MODEL_GAME:
                                case WorkbookOperationType.DELETE_MODEL_GAMES:
                                case WorkbookOperationType.DELETE_ARTICLES:
                                case WorkbookOperationType.DELETE_CHAPTERS:
                                case WorkbookOperationType.MERGE_CHAPTERS:
                                case WorkbookOperationType.SPLIT_CHAPTER:
                                    AppState.MainWin.ChaptersView.IsDirty = true;
                                    GuiUtilities.RefreshChaptersView(null);
                                    AppState.MainWin.UiTabChapters.Focus();
                                    break;
                                case WorkbookOperationType.DELETE_EXERCISE:
                                case WorkbookOperationType.DELETE_EXERCISES:
                                    _chaptersView.BuildFlowDocumentForChaptersView();
                                    SelectExercise(selectedArticleIndex, AppState.ActiveTab != TabViewType.CHAPTERS);
                                    break;
                                case WorkbookOperationType.COPY_ARTICLES:
                                case WorkbookOperationType.INSERT_ARTICLES:
                                case WorkbookOperationType.IMPORT_CHAPTERS:
                                case WorkbookOperationType.MOVE_ARTICLES:
                                case WorkbookOperationType.MOVE_ARTICLES_MULTI_CHAPTER:
                                    _chaptersView.IsDirty = true;
                                    UiTabChapters.Focus();
                                    break;
                                case WorkbookOperationType.DELETE_COMMENTS:
                                    AppState.MainWin.ActiveTreeView?.BuildFlowDocumentForVariationTree();
                                    break;
                                case WorkbookOperationType.DELETE_ENGINE_EVALS:
                                    AppState.MainWin.ActiveTreeView?.BuildFlowDocumentForVariationTree();
                                    ActiveLine.RefreshNodeList(true);
                                    break;
                            }

                            AppState.IsDirty = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UndoWorkbookOperation()", ex);
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

            WorkbookViewState wvs = new WorkbookViewState(SessionWorkbook);
            wvs.SaveState();

            if (AppState.CurrentLearningMode != LearningMode.Mode.IDLE
                && (AppState.IsDirty || string.IsNullOrEmpty(AppState.WorkbookFilePath)) || (ActiveVariationTree != null && ActiveVariationTree.HasTrainingMoves()))
            {
                try
                {
                    WorkbookManager.PromptAndSaveWorkbook(false, out _, true);
                }
                catch (Exception ex)
                {
                    AppLog.Message("ChessForgeMain_Closing() abandoned", ex);
                    e.Cancel = true;
                }
            }

            SoundPlayer.CloseAll();

            if (e.Cancel != true)
            {
                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);
                EngineMessageProcessor.ChessEngineService.StopEngine();

                Timers.StopAll();

                DumpDebugLogs(false);
                Configuration.WriteOutConfiguration();
            }
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
                WorkbookManager.PromptAndSaveWorkbook(true, out _);
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
            WorkbookManager.SaveWorkbookToNewFile(AppState.WorkbookFilePath);
        }

        /// <summary>
        /// Asks the user to confirm the backup with bumped worked version.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnBackupVersion_Click(object sender, RoutedEventArgs e)
        {
            BackupVersionDialog dlg = new BackupVersionDialog(WorkbookManager.SessionWorkbook);
            GuiUtilities.PositionDialog(dlg, this, 100);
            if (dlg.ShowDialog() == true)
            {
                AppState.SaveWorkbookFile(dlg.BackupPath);
                WorkbookManager.SessionWorkbook.SetVersion(dlg.IncrementedVersion);
                AppState.UpdateAppTitleBar();
                AppState.IsDirty = true;
            }
        }

        /// <summary>
        /// Creates a new Workbook.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnNewWorkbook_Click(object sender, RoutedEventArgs e)
        {
            CreateNewWorkbook();
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
                MessageBox.Show(Properties.Resources.EngineNotAvailable, Properties.Resources.EvaluationError, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// The user requested a line evaluation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnEvaluateLine_Click(object sender, RoutedEventArgs e)
        {
            // a defensive check
            if (ActiveLine.GetPlyCount() <= 1)
            {
                return;
            }

            if (EvaluationManager.CurrentMode != EvaluationManager.Mode.IDLE)
            {
                StopEvaluation(true);
            }

            if (EngineMessageProcessor.IsEngineAvailable)
            {
                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.LINE, true, EvaluationManager.LineSource.ACTIVE_LINE);

                int idx = ActiveLine.GetSelectedPlyNodeIndex(true);
                // if idx == 0, bump it to 1
                idx = idx > 0 ? idx : 1;
                EvaluationManager.SetStartNodeIndex(idx);
                TreeNode nd = ActiveLine.GetNodeAtIndex(idx);

                UiDgActiveLine.SelectedCells.Clear();

                if (!EngineMessageProcessor.RequestMoveEvaluation(idx, nd, ActiveVariationTreeId))
                {
                    StopEvaluation(true);
                }
            }
            else
            {
                MessageBox.Show(Properties.Resources.EngineNotAvailable, Properties.Resources.EvaluationError, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        //**********************
        //
        //  FIND POSITIONS
        // 
        //**********************

        /// <summary>
        /// Finds the list of positions identical to the currently selected node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnFindIdenticalPosition_Click(object sender, RoutedEventArgs e)
        {
            bool isTrainingOrSolving = TrainingSession.IsTrainingInProgress || AppState.IsUserSolving();

            if (isTrainingOrSolving || ActiveVariationTree == null || AppState.ActiveTab == TabViewType.CHAPTERS)
            {
                return;
            }

            try
            {
                TreeNode nd = ActiveVariationTree == null ? null : ActiveVariationTree.SelectedNode;

                bool externalSearch = !AppState.IsTreeViewTabActive();
                FindIdenticalPositions.Search(false, nd, FindIdenticalPositions.Mode.FIND_AND_REPORT, externalSearch, true, out _);
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnFindIdenticalPosition_Click()", ex);
            }
        }

        /// <summary>
        /// Invoke the Edit FEN dialog and perform search by the specified FEN
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnFindPositions_Click(object sender, RoutedEventArgs e)
        {
            bool isTrainingOrSolving = TrainingSession.IsTrainingInProgress || AppState.IsUserSolving();

            if (isTrainingOrSolving)
            {
                return;
            }

            try
            {
                BoardPosition position = null;
                TreeNode nd = ActiveVariationTree == null ? null : ActiveVariationTree.SelectedNode;
                if (nd == null)
                {
                    string fen = PositionUtils.GetFenFromClipboard();
                    if (string.IsNullOrEmpty(fen))
                    {
                        try
                        {
                            FenParser.ParseFenIntoBoard(fen, ref position);
                        }
                        catch
                        {
                            position = null;
                            position = PositionUtils.SetupStartingPosition();
                        }
                    }
                }
                else
                {
                    position = nd.Position;
                }

                TreeNode searchNode = new TreeNode(null, "", 1);
                bool stopSearch = false;
                while (!stopSearch)
                {
                    SearchPositionDialog dlg = new SearchPositionDialog(position);
                    GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);
                    if (dlg.ShowDialog() == true)
                    {
                        searchNode.Position = new BoardPosition(dlg.PositionSetup);
                        // store for another possible loop
                        position = searchNode.Position;
                        stopSearch = FindIdenticalPositions.Search(true, searchNode, FindIdenticalPositions.Mode.FIND_AND_REPORT, true, false, out bool searchAgain);
                        if (searchAgain)
                        {
                            stopSearch = false;
                        }
                        else if (!stopSearch)
                        {
                            if (MessageBox.Show(Properties.Resources.MsgEditPositionSearch, Properties.Resources.MsgTitlePositionSearch, MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
                            {
                                stopSearch = true;
                            }
                        }
                    }
                    else
                    {
                        stopSearch = true;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("SearchByFen_Click()", ex);
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

            // double check that we are in the Study or Games tab,
            // we don't allow starting a game from anywhere else
            if (AppState.ActiveTab == TabViewType.STUDY || AppState.ActiveTab == TabViewType.MODEL_GAME)
            {
                if (!EngineMessageProcessor.IsEngineAvailable)
                {
                    MessageBox.Show(Properties.Resources.EngineNotAvailable, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // check that there is a move selected so that we have somewhere to start
                TreeNode origNode = ActiveLine.GetSelectedTreeNode();

                // make a deep  copy of the stem so that we detach our tree from the previously Active Tree.
                List<TreeNode> gameStem = TreeUtils.CopyNodeList(TreeUtils.GetStemLine(origNode, true));

                TreeNode nd = gameStem.Find(x => x.NodeId == origNode.NodeId);

                if (nd != null)
                {
                    AppState.SetupGuiForEngineGame();
                    StartEngineGame(nd, false);
                }
                else
                {
                    MessageBox.Show(Properties.Resources.SelectEngineStartMove, Properties.Resources.EngineGame, MessageBoxButton.OK);
                }
            }
        }

        /// <summary>
        /// The user requested exit from the game against the engine.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExitEngineGame_Click(object sender, RoutedEventArgs e)
        {
            UiBtnExitGame_Click(sender, e);
        }

        /// <summary>
        /// The "Exit Game" button was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnExitGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool? saveGame = SaveEngineGame();

                if (saveGame != null)
                {
                    StopEngineGame();
                    EnableGui(true);

                    if (saveGame == true)
                    {
                        string key = EngineGame.EngineColor == PieceColor.White ? PgnHeaders.KEY_WHITE : PgnHeaders.KEY_BLACK;
                        if (EngineGame.EngineColor != PieceColor.None)
                        {
                            EngineGame.Line.Tree.Header.SetHeaderValue(key, Properties.Resources.Engine + " " + AppState.EngineName);
                        }
                        EngineGame.Line.Tree.Header.SetHeaderValue(PgnHeaders.KEY_DATE, PgnHeaders.FormatPgnDateString(DateTime.Now));
                        CreateNewModelGame(EngineGame.Line.Tree);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Determines whether the game should be saved.
        /// If we are not in the pure Engine Game mode
        /// but rather in Trainging, then simply return false,
        /// otherwise ask the user.
        /// If the user chooses Cancel then retun null.
        /// </summary>
        /// <returns></returns>
        private bool? SaveEngineGame()
        {
            bool? save = false;

            if (!TrainingSession.IsTrainingInProgress)
            {
                MessageBoxResult res = MessageBox.Show(Properties.Resources.EngGameSave,
                    Properties.Resources.EngineGame
                    , MessageBoxButton.YesNoCancel
                    , MessageBoxImage.Question
                    );

                if (res == MessageBoxResult.Cancel)
                {
                    save = null;
                }
                else if (res == MessageBoxResult.Yes)
                {
                    save = true;
                }
            }

            return save;
        }

        //**************************************************************
        //
        //  CHAPTERS VIEW 
        // 
        //**************************************************************

        /// <summary>
        /// The Chapters view was clicked somewhere.
        /// Here we configured the context menu items as if the currently active/selected
        /// chapter was clicked.
        /// If the click was on a non-chapter object, the menu items will be re-configured
        /// accordingly in the event handler for the clicked run.
        /// All the above happens before the context menu is invoked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Chapters_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (WorkbookManager.SessionWorkbook != null && WorkbookManager.SessionWorkbook.ActiveChapter != null)
            {
                WorkbookManager.LastClickedChapterIndex = WorkbookManager.SessionWorkbook.ActiveChapter.Index;
            }
            else
            {
                WorkbookManager.LastClickedChapterIndex = -1;
            }
            WorkbookManager.EnableChaptersContextMenuItems(UiMncChapters, WorkbookManager.LastClickedChapterIndex >= 0, GameData.ContentType.GENERIC);
        }

        /// <summary>
        /// The right button mouse up event will trigger bringing the last marked 
        /// Run into view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Chapters_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _chaptersView?.BringRunToview();
        }

        /// <summary>
        /// Expand all chapter headers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnChptExpandChapters_Click(object sender, RoutedEventArgs e)
        {
            ExpandCollapseChaptersView(true, false);
        }

        /// <summary>
        /// Collapse all chapter headers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnChptCollapseChapters_Click(object sender, RoutedEventArgs e)
        {
            ExpandCollapseChaptersView(false, false);
        }

        /// <summary>
        /// Reports chapter statistics
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnChapterStats_Click(object sender, RoutedEventArgs e)
        {
            StatsUtils.ReportStats(OperationScope.CHAPTER);
        }

        /// <summary>
        /// Report workbook statistics
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnWorkbookStats_Click(object sender, RoutedEventArgs e)
        {
            StatsUtils.ReportStats(OperationScope.WORKBOOK);
        }

        /// <summary>
        /// Expand all chapters and article headers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnChptExpandAll_Click(object sender, RoutedEventArgs e)
        {
            ExpandCollapseChaptersView(true, true);
        }

        /// <summary>
        /// Collapse all chapters and article headers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnChptCollapseAll_Click(object sender, RoutedEventArgs e)
        {
            ExpandCollapseChaptersView(false, true);
        }

        /// <summary>
        /// Expands or Collapses chapters and/or articles.
        /// </summary>
        /// <param name="expand">true to expand / false to collapse</param>
        /// <param name="all">true to expand everything / false to expand chapter headers only</param>
        public void ExpandCollapseChaptersView(bool expand, bool all)
        {
            List<Chapter> chapters = WorkbookManager.SessionWorkbook.Chapters;
            foreach (Chapter chapter in chapters)
            {
                chapter.IsViewExpanded = expand;
                if (all)
                {
                    chapter.IsModelGamesListExpanded = expand;
                    chapter.IsExercisesListExpanded = expand;
                }
            }

            _chaptersView?.BuildFlowDocumentForChaptersView();
            _chaptersView?.BringActiveChapterIntoView();
        }

        /// <summary>
        /// Selects the clicked Chapter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnSelectChapter_Click(object sender, RoutedEventArgs e)
        {
            SelectChapterByIndex(WorkbookManager.LastClickedChapterIndex, true);
        }

        /// <summary>
        /// Show the Intro tab for the selected chapter and go to it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnChptCreateIntro_Click(object sender, RoutedEventArgs e)
        {
            Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[WorkbookManager.LastClickedChapterIndex];
            if (chapter != null)
            {
                chapter.AlwaysShowIntroTab = true;
                UiTabIntro.Visibility = Visibility.Visible;
                UiTabIntro.Focus();
            }
        }

        /// <summary>
        /// Invokes the Chapter Title dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnRenameChapter_Click(object sender, RoutedEventArgs e)
        {
            Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[WorkbookManager.LastClickedChapterIndex];
            RenameChapter(chapter);
        }

        /// <summary>
        /// Invokes the Chapter Title dialog.
        /// </summary>
        /// <param name="chapter"></param>
        public void RenameChapter(Chapter chapter)
        {
            if (chapter == null)
            {
                return;
            }

            string prevTitle = chapter.GetTitle();
            if (ShowChapterTitleDialog(chapter) && chapter.GetTitle() != prevTitle)
            {
                WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.RENAME_CHAPTER, chapter, prevTitle);
                WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
                AppState.IsDirty = true;
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
                SelectChapterByIndex(chapter.Index, false);
                AppState.IsDirty = true;
                if (_chaptersView != null)
                {
                    AppState.DoEvents();
                    _chaptersView.BringChapterIntoView(chapter.Index);
                }
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
                ObservableCollection<GameData> games = new ObservableCollection<GameData>();

                int gamesCount = WorkbookManager.ReadPgnFile(fileName, ref games, GameData.ContentType.GENERIC, GameData.ContentType.NONE);

                // if this is a ChessForge Workbook, list the chapters and allow the user to copy them over
                if (WorkbookManager.IsChessForgeWorkbook(ref games))
                {
                    Workbook workbook = new Workbook();
                    WorkbookManager.CreateWorkbookFromGameList(ref workbook, ref games);
                    SelectChaptersDialog dlg = new SelectChaptersDialog(workbook);
                    GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                    dlg.ShowDialog();
                    if (dlg.ExitOK)
                    {
                        List<ArticleListItem> undoArticleList = new List<ArticleListItem>();

                        int importedChapters = 0;

                        foreach (SelectedChapter ch in dlg.ChapterList)
                        {
                            if (ch.IsSelected)
                            {
                                WorkbookManager.SessionWorkbook.Chapters.Add(ch.Chapter);
                                undoArticleList.Add(new ArticleListItem(ch.Chapter));

                                importedChapters++;

                                if (_chaptersView != null)
                                {
                                    _chaptersView.BuildFlowDocumentForChaptersView();
                                    PulseManager.ChapterIndexToBringIntoView = WorkbookManager.SessionWorkbook.GetChapterCount() - 1;
                                }
                                AppState.IsDirty = true;

                                if (undoArticleList.Count > 0)
                                {
                                    WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.IMPORT_CHAPTERS, (object)undoArticleList);
                                    WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
                                }
                            }
                        }

                        if (importedChapters > 0)
                        {
                            AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(
                                Properties.Resources.FlMsgChaptersImported + " (" + importedChapters.ToString() + ")", CommentBox.HintType.INFO);
                        }

                    }
                }
                else
                {
                    CreateChapterFromNewGames(gamesCount, ref games, fileName);
                    AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(
                        Properties.Resources.FlMsgChapterImported, CommentBox.HintType.INFO);
                }
            }
        }

        /// <summary>
        /// Forces focus on the Chapters view.
        /// </summary>
        public void FocusOnChapterView()
        {
            UiTabChapters.Focus();

            //TODO: probably can be removed.
            AppState.DoEvents();
            _chaptersView.BringChapterIntoView(WorkbookManager.SessionWorkbook.ActiveChapterIndex);
        }

        /// <summary>
        /// Lets the user select games exercises from which to create a new Chapter.
        /// </summary>
        /// <param name="gamesCount"></param>
        /// <param name="games"></param>
        /// <param name="fileName"></param>
        private void CreateChapterFromNewGames(int gamesCount, ref ObservableCollection<GameData> games, string fileName)
        {
            try
            {
                Chapter previousActiveChapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                Chapter chapter = WorkbookManager.SessionWorkbook.CreateNewChapter();

                List<ArticleListItem> undoArticleList = new List<ArticleListItem>();

                if (gamesCount > 0)
                {
                    if (SelectArticlesFromPgnFile(ref games, SelectGamesDialog.Mode.IMPORT_INTO_NEW_CHAPTER))
                    {
                        // content type may have been reset to GENERIC in MergeGames above
                        chapter.StudyTree.Tree.ContentType = GameData.ContentType.STUDY_TREE;

                        CopySelectedItemsToChapter(chapter, true, out string error, games, out _);

                        undoArticleList.Add(new ArticleListItem(chapter));

                        _chaptersView.BuildFlowDocumentForChaptersView();
                        SelectChapterByIndex(chapter.Index, false);
                        _chaptersView.BringChapterIntoView(chapter.Index);

                        if (undoArticleList.Count > 0)
                        {
                            WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.IMPORT_CHAPTERS, (object)undoArticleList);
                            WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
                        }
                    }
                    AppState.IsDirty = true;
                }
                else
                {
                    ShowNoGamesError(GameData.ContentType.GENERIC, fileName);

                    // delete the above created chapter and activate the previously active one
                    WorkbookManager.SessionWorkbook.ActiveChapter = previousActiveChapter;
                    WorkbookManager.SessionWorkbook.Chapters.Remove(chapter);
                }
            }
            catch (Exception ex)
            {
                DebugUtils.ShowDebugMessage(ex.Message);
            }
        }

        /// <summary>
        /// Copies selected items from the list
        /// into the passed chapter
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="games"></param>
        public int CopySelectedItemsToChapter(Chapter chapter, bool copyGames, out string error, ObservableCollection<GameData> games, out int copiedExercises)
        {
            copiedExercises = 0;

            int copiedCount = 0;
            error = string.Empty;
            StringBuilder sbErrors = new StringBuilder();
            int gameIndex = 0;

            foreach (GameData gd in games)
            {
                if (gd.IsSelected)
                {
                    if (gd.GetContentType(false) == GameData.ContentType.EXERCISE)
                    {
                        if (PgnArticleUtils.AddArticle(chapter, gd, GameData.ContentType.EXERCISE, out error, GameData.ContentType.EXERCISE) >= 0)
                        {
                            copiedCount++;
                            copiedExercises++;
                        }
                        chapter.StudyTree.Tree.ContentType = GameData.ContentType.STUDY_TREE;
                    }
                    else if (copyGames && (gd.GetContentType(false) == GameData.ContentType.GENERIC || gd.GetContentType(false) == GameData.ContentType.MODEL_GAME))
                    {
                        if (PgnArticleUtils.AddArticle(chapter, gd, GameData.ContentType.MODEL_GAME, out error, GameData.ContentType.MODEL_GAME) >= 0)
                        {
                            copiedCount++;
                        }
                        if (!string.IsNullOrEmpty(error))
                        {
                            sbErrors.Append(GuiUtilities.BuildGameProcessingErrorText(gd, gameIndex + 1, error));
                        }
                    }

                    gameIndex++;
                }
            }

            error = sbErrors.ToString();

            return copiedCount;
        }

        /// <summary>
        /// Calls the SelectGamesDialog to let the user select Games and/or Exercise
        /// from the passed list.
        /// </summary>
        /// <param name="games"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public bool SelectArticlesFromPgnFile(ref ObservableCollection<GameData> games, SelectGamesDialog.Mode mode)
        {
            bool res = false;

            SelectGamesDialog dlg = new SelectGamesDialog(ref games, mode);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

            if (dlg.ShowDialog() == true)
            {
                // remove articles that are not selected
                List<GameData> gamesToDelete = new List<GameData>();
                foreach (GameData game in games)
                {
                    if (!game.IsSelected)
                    {
                        gamesToDelete.Add(game);
                    }
                }
                foreach (GameData game in gamesToDelete)
                {
                    games.Remove(game);
                }

                if (games.Count == 0)
                {
                    MessageBox.Show(Properties.Resources.ErrNoItemsSelected, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else
                {
                    res = true;
                }
            }
            return res;
        }

        /// <summary>
        /// Deletes the entire chapter from the Workbook.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDeleteChapter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.Chapters[WorkbookManager.LastClickedChapterIndex];
                if (chapter != null)
                {
                    string txt = Properties.Resources.DeleteChapterConfirm;
                    txt = txt.Replace("$0", chapter.GetTitle());
                    var res = MessageBox.Show(txt, Properties.Resources.DeleteChapter, MessageBoxButton.YesNoCancel);
                    if (res == MessageBoxResult.Yes)
                    {
                        WorkbookManager.SessionWorkbook.DeleteChapter(chapter); // .Remove(chapter);
                        if (chapter.Index == WorkbookManager.SessionWorkbook.ActiveChapter.Index)
                        {
                            WorkbookManager.SessionWorkbook.SelectDefaultActiveChapter();
                        }
                        _chaptersView.BuildFlowDocumentForChaptersView();
                        SetupGuiForActiveStudyTree(false);
                        AppState.IsDirty = true;
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Brings the requested chapter into view in ChaptersView.
        /// </summary>
        /// <param name="index"></param>
        public void BringChapterIntoView(int index)
        {
            _chaptersView.BringChapterIntoViewByIndex(index);
        }

        /// <summary>
        /// Brings the requested article into view in ChaptersView.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="contentType"></param>
        /// <param name="index"></param>
        public void BringArticleIntoView(int chapterIndex, GameData.ContentType contentType, int index)
        {
            _chaptersView.BringArticleIntoView(chapterIndex, contentType, index);
        }

        /// <summary>
        /// Moves chapter up one position in the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnChapterUp_Click(object sender, RoutedEventArgs e)
        {
            int index = AppState.Workbook.ActiveChapterIndex;
            if (index > 0)
            {
                Chapter hold = WorkbookManager.SessionWorkbook.Chapters[index];
                AppState.Workbook.Chapters[index] = AppState.Workbook.Chapters[index - 1];
                AppState.Workbook.Chapters[index - 1] = hold;

                _chaptersView.RebuildChapterParagraph(AppState.Workbook.Chapters[index]);
                _chaptersView.RebuildChapterParagraph(AppState.Workbook.Chapters[index - 1]);
                SelectChapterByIndex(index - 1, false, false);

                PulseManager.ChapterIndexToBringIntoView = index - 1;
                AppState.IsDirty = true;
            }
        }

        /// <summary>
        /// Moves chapter down one position in the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnChapterDown_Click(object sender, RoutedEventArgs e)
        {
            int index = AppState.Workbook.ActiveChapterIndex;
            if (index >= 0 && index < AppState.Workbook.Chapters.Count - 1)
            {
                Chapter hold = AppState.Workbook.Chapters[index];
                AppState.Workbook.Chapters[index] = AppState.Workbook.Chapters[index + 1];
                AppState.Workbook.Chapters[index + 1] = hold;

                _chaptersView.RebuildChapterParagraph(AppState.Workbook.Chapters[index]);
                _chaptersView.RebuildChapterParagraph(AppState.Workbook.Chapters[index + 1]);
                SelectChapterByIndex(index + 1, false, false);

                PulseManager.ChapterIndexToBringIntoView = index + 1;
                AppState.IsDirty = true;
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
                int index = chapter.ActiveModelGameIndex;
                int gameCount = chapter.GetModelGameCount();

                if (index > 0 && index < gameCount)
                {
                    Article hold = chapter.ModelGames[index];
                    chapter.ModelGames[index] = chapter.ModelGames[index - 1];
                    chapter.ModelGames[index - 1] = hold;
                    chapter.ActiveModelGameIndex = index - 1;

                    _chaptersView.SwapModelGames(chapter, index, index - 1);
                    AppState.IsDirty = true;
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
                int index = chapter.ActiveExerciseIndex;
                int exerciseCount = chapter.GetExerciseCount();

                if (index > 0 && index < exerciseCount)
                {
                    Article hold = chapter.Exercises[index];
                    chapter.Exercises[index] = chapter.Exercises[index - 1];
                    chapter.Exercises[index - 1] = hold;
                    chapter.ActiveExerciseIndex = index - 1;

                    _chaptersView.SwapExercises(chapter, index, index - 1);
                    AppState.IsDirty = true;
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
                int index = chapter.ActiveModelGameIndex;
                //int index = WorkbookManager.LastClickedModelGameIndex;
                int gameCount = chapter.GetModelGameCount();

                if (index >= 0 && index < gameCount - 1)
                {
                    Article hold = chapter.ModelGames[index];
                    chapter.ModelGames[index] = chapter.ModelGames[index + 1];
                    chapter.ModelGames[index + 1] = hold;
                    chapter.ActiveModelGameIndex = index + 1;

                    _chaptersView.SwapModelGames(chapter, index, index + 1);
                    AppState.IsDirty = true;
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
                int index = chapter.ActiveExerciseIndex;
                int exerciseCount = chapter.GetExerciseCount();

                if (index >= 0 && index < exerciseCount - 1)
                {
                    Article hold = chapter.Exercises[index];
                    chapter.Exercises[index] = chapter.Exercises[index + 1];
                    chapter.Exercises[index + 1] = hold;
                    chapter.ActiveExerciseIndex = index + 1;

                    _chaptersView.SwapExercises(chapter, index, index + 1);
                    AppState.IsDirty = true;
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnExerciseDown_Click()", ex);
            }
        }

        /// <summary>
        /// Lets the user select a chapter to move the currently selected game to.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnMoveGameToChapter_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<ArticleListItem> articleList = new ObservableCollection<ArticleListItem>();
            Chapter chapter = AppState.ActiveChapter;
            if (chapter != null)
            {
                int articleIndex = chapter.ActiveModelGameIndex;
                ArticleListItem item = new ArticleListItem(null, chapter.Index, chapter.GetModelGameAtIndex(articleIndex), articleIndex);
                articleList.Add(item);
                ChapterUtils.ProcessCopyOrMoveArticles(null, articleList, ArticlesAction.MOVE);
            }
        }

        /// <summary>
        /// Lets the user select a chapter to move the currently selected exercise to.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnMoveExerciseToChapter_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<ArticleListItem> articleList = new ObservableCollection<ArticleListItem>();
            Chapter chapter = AppState.ActiveChapter;
            if (chapter != null)
            {
                int articleIndex = chapter.ActiveExerciseIndex;
                ArticleListItem item = new ArticleListItem(null, chapter.Index, chapter.GetExerciseAtIndex(articleIndex), articleIndex);
                articleList.Add(item);
                ChapterUtils.ProcessCopyOrMoveArticles(null, articleList, ArticlesAction.MOVE);
            }
        }

        /// <summary>
        /// Requests import of Model Games from a PGN file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnImportModelGames_Click(object sender, RoutedEventArgs e)
        {
            int count = ImportGamesFromPgn(GameData.ContentType.GENERIC, GameData.ContentType.MODEL_GAME);
            if (count > 0)
            {
                AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(
                    Properties.Resources.FlMsgGamesImported + " (" + count.ToString() + ")", CommentBox.HintType.INFO);
            }
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
            int count = ImportGamesFromPgn(GameData.ContentType.EXERCISE, GameData.ContentType.EXERCISE);

            if (count > 0)
            {
                AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(
                    Properties.Resources.FlMsgExercisesImported + " (" + count.ToString() + ")", CommentBox.HintType.INFO);
            }
        }

        /// <summary>
        /// Imports Model Games or Exercises from a PGN file.
        /// </summary>
        /// <param name="contentType"></param>
        private int ImportGamesFromPgn(GameData.ContentType contentType, GameData.ContentType targetcontentType)
        {
            int gameCount;
            int importedGames = 0;
            int skippedDueToType = 0;
            int firstImportedGameIndex = -1;
            if ((contentType == GameData.ContentType.GENERIC || contentType == GameData.ContentType.MODEL_GAME || contentType == GameData.ContentType.EXERCISE)
                && WorkbookManager.SessionWorkbook.ActiveChapter != null)
            {
                string fileName = SelectPgnFile();
                if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
                {
                    Chapter activeChapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                    ObservableCollection<GameData> games = new ObservableCollection<GameData>();
                    gameCount = WorkbookManager.ReadPgnFile(fileName, ref games, contentType, targetcontentType);

                    // clear the default selections
                    foreach (GameData gd in games)
                    {
                        gd.IsSelected = false;
                    }

                    int errorCount = 0;
                    StringBuilder sbErrors = new StringBuilder();

                    ArticleListItem undoItem;
                    List<ArticleListItem> undoArticleList = new List<ArticleListItem>();

                    if (gameCount > 0)
                    {
                        if (ShowSelectGamesDialog(contentType, ref games))
                        {
                            int chapterIndex = ChapterUtils.InvokeSelectSingleChapterDialog(activeChapter.Index, out bool newChapter);

                            bool proceed = true;

                            if (chapterIndex >= 0)
                            {
                                Chapter targetChapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(chapterIndex);
                                if (newChapter)
                                {
                                    ChapterTitleDialog dlg = new ChapterTitleDialog(targetChapter);
                                    GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);
                                    if (dlg.ShowDialog() == true)
                                    {
                                        targetChapter.SetTitle(dlg.ChapterTitle);
                                        targetChapter.SetAuthor(dlg.Author);
                                        AppState.Workbook.ActiveChapter = targetChapter;
                                    }
                                    else
                                    {
                                        AppState.Workbook.Chapters.Remove(targetChapter);
                                        AppState.Workbook.ActiveChapter = activeChapter;
                                        proceed = false;
                                    }
                                }
                                else
                                {
                                    AppState.Workbook.ActiveChapter = targetChapter;
                                }

                                if (proceed)
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
                                                    int index = PgnArticleUtils.AddArticle(targetChapter, games[i], contentType, out string error, targetcontentType);
                                                    if (index < 0)
                                                    {
                                                        if (string.IsNullOrEmpty(error))
                                                        {
                                                            skippedDueToType++;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        undoItem = new ArticleListItem(targetChapter, targetChapter.Index, targetChapter.GetArticleAtIndex(targetcontentType, index), index);
                                                        if (undoItem.Article != null)
                                                        {
                                                            undoArticleList.Add(undoItem);
                                                        }

                                                        if (firstImportedGameIndex < 0)
                                                        {
                                                            firstImportedGameIndex = index;
                                                        }
                                                    }

                                                    AppState.IsDirty = true;
                                                    if (!string.IsNullOrEmpty(error))
                                                    {
                                                        errorCount++;
                                                        sbErrors.Append(GuiUtilities.BuildGameProcessingErrorText(games[i], i + 1, error));
                                                    }
                                                    importedGames++;
                                                }
                                                catch (Exception ex)
                                                {
                                                    errorCount++;
                                                    sbErrors.Append(GuiUtilities.BuildGameProcessingErrorText(games[i], i + 1, ex.Message));
                                                }
                                            }
                                        }
                                        RefreshChaptersViewAfterImport(targetcontentType, targetChapter, firstImportedGameIndex);
                                    }
                                    catch { }

                                    if (undoArticleList.Count > 0)
                                    {
                                        WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.INSERT_ARTICLES, (object)undoArticleList);
                                        WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
                                    }

                                    if (AppState.ActiveTab == TabViewType.CHAPTERS)
                                    {
                                        ChaptersView.BringActiveChapterIntoView();
                                    }
                                    Mouse.SetCursor(Cursors.Arrow);
                                }
                            }
                        }
                        else
                        {
                            gameCount = 0;
                            importedGames = 0;
                        }
                    }
                    else
                    {
                        ShowNoGamesError(contentType, fileName);
                    }

                    if (errorCount > 0 || skippedDueToType > 0)
                    {
                        if (skippedDueToType > 0)
                        {
                            string invalidEntities = Properties.Resources.WrongTypeEntitiesNotImported + ", ";
                            invalidEntities += (Properties.Resources.Count + " " + skippedDueToType.ToString() + ".");
                            sbErrors.AppendLine(invalidEntities);
                        }
                        TextBoxDialog tbDlg = new TextBoxDialog(Properties.Resources.PgnErrors, sbErrors.ToString());
                        GuiUtilities.PositionDialog(tbDlg, this, 100);
                        tbDlg.ShowDialog();
                    }
                }
            }
            return importedGames;
        }


        //*****************************************************************************
        //
        //   TOP GAMES EXPLORER
        //
        //*****************************************************************************


        /// <summary>
        /// Opens the Game Preview dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnTopGamePreview_Click(object sender, RoutedEventArgs e)
        {
            _topGamesView.OpenReplayDialog();
        }

        /// <summary>
        /// Downloads the game last clicked in the Top Games view
        /// and adds it to the Active Chapter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnTopGameImport_Click(object sender, RoutedEventArgs e)
        {
            AppState.DownloadLichessGameToActiveChapter(_topGamesView.CurrentGameId);
        }

        /// <summary>
        /// Opens the last clicked Top Game in the browser,
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnTopGameLichessView_Click(object sender, RoutedEventArgs e)
        {
            AppState.ViewGameOnLichess(_topGamesView.CurrentGameId);
        }

        //*****************************************************************************
        //
        //   MODEL GAMES and EXERCISES MENUS
        //
        //*****************************************************************************

        /// <summary>
        /// Event handler for Article selection.
        /// MainWindow subscribes to it with EventSelectArticle().
        /// </summary>
        public static event EventHandler<ChessForgeEventArgs> ArticleSelected;


        /// <summary>
        /// Invokes the Select Articles dialog to allow the user
        /// to edit Article references.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnReferenceArticles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TreeNode nd = ActiveTreeView.GetSelectedNode();
                ObservableCollection<ArticleListItem> articleList = WorkbookManager.SessionWorkbook.GenerateArticleList();
                SelectArticlesDialog dlg = new SelectArticlesDialog(nd, true, null, ref articleList, false, ArticlesAction.NONE);
                //{
                //    Left = ChessForgeMain.Left + 100,
                //    Top = ChessForgeMain.Top + 100,
                //    Topmost = false,
                //    Owner = this
                //};
                GuiUtilities.PositionDialog(dlg, this, 100);
                if (dlg.ShowDialog() == true)
                {
                    List<string> refGuids = dlg.GetSelectedReferenceStrings();
                    ActiveVariationTree.SetArticleRefs(nd, refGuids);
                    ActiveTreeView.InsertOrDeleteReferenceRun(nd);
                    AppState.IsDirty = true;

                    if (dlg.SelectedArticle != null)
                    {
                        WorkbookManager.SessionWorkbook.GetArticleByGuid(dlg.SelectedArticle.Tree.Header.GetGuid(out _), out int chapterIndex, out int articleIndex);

                        ChessForgeEventArgs args = new ChessForgeEventArgs
                        {
                            ChapterIndex = chapterIndex,
                            ArticleIndex = articleIndex,
                            ContentType = dlg.SelectedArticle.Tree.Header.GetContentType(out _)
                        };

                        ArticleSelected?.Invoke(this, args);
                    }
                }

            }
            catch
            {
            }
        }

        /// <summary>
        /// Shows/hides solution in the current exercise view
        /// </summary>
        public void UpdateShowSolutionInExerciseView(bool show)
        {
            try
            {
                if (_exerciseTreeView != null)
                {
                    _exerciseTreeView.ShowHideSolution(show);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UpdateShowSolutionInExerciseView()", ex);
            }
        }

        /// <summary>
        /// Toggles the Diagram flag on the currently selected node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMn_ToggleDiagramFlag_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView?.ToggleDiagramFlag();
        }

        /// <summary>
        /// Marks the current node as a Thumbnail for the current tree.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMn_MarkThumbnail_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView?.MarkSelectedNodeAsThumbnail();
        }

        /// <summary>
        /// Marks the current node as a Thumbnail for the current tree
        /// if it is exercise.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMn_ExerciseMarkThumbnail_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView?.MarkSelectedNodeAsThumbnail(true);
        }

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
        private void UiMnGame_CreateModelGame_Click(object sender, RoutedEventArgs e)
        {
            CreateNewModelGame();
        }


        /// <summary>
        /// Copy FEN of the selected position to the clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnGame_CopyFen_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView.CopyFenToClipboard();
        }

        /// <summary>
        /// Creates a new Exercise from the Model Games View context menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnGame_CreateExercise_Click(object sender, RoutedEventArgs e)
        {
            UiMn_CreateExercise_Click(sender, e);
        }

        /// <summary>
        /// Creates a new Exercise starting from the position currently selected in the Active Tree.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMn_CreateExercise_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TreeNode nd = ActiveLine.GetSelectedTreeNode();
                if (nd != null)
                {
                    // get the move number offset
                    uint moveNumberOffset = nd.MoveNumber;
                    if (nd.Position.ColorToMove == PieceColor.Black && moveNumberOffset > 0)
                    {
                        moveNumberOffset--;
                    }

                    VariationTree tree = TreeUtils.CreateNewTreeFromNode(nd, GameData.ContentType.EXERCISE);
                    // preserve opening info from the first node
                    string firstNodeEco = tree.RootNode.Eco;
                    TreeUtils.RemoveOpeningInfo(tree);
                    tree.MoveNumberOffset = moveNumberOffset;

                    Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                    CopyHeaderFromGame(tree, ActiveVariationTree.Header, false);
                    if (!string.IsNullOrEmpty(firstNodeEco))
                    {
                        tree.Header.SetHeaderValue(PgnHeaders.KEY_ECO, firstNodeEco);
                    }
                    if (ActiveVariationTree.Header.GetContentType(out _) == GameData.ContentType.STUDY_TREE)
                    {
                        tree.Header.SetHeaderValue(PgnHeaders.KEY_WHITE, chapter.Title);
                        tree.Header.SetHeaderValue(PgnHeaders.KEY_BLACK, Properties.Resources.StudyTreeAfter + " " + MoveUtils.BuildSingleMoveText(nd, true, true, ActiveVariationTree.MoveNumberOffset));

                        ChapterUtils.ClearStudyTreeHeader(tree);
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
        /// Copy FEN of the selected position to the clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnCopyFen_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView.CopyFenToClipboard();
        }


        /// <summary>
        /// Copies a header from a GameHeader object to the Tree.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="header"></param>
        private void CopyHeaderFromGame(VariationTree tree, GameHeader header, bool overrideGuid = false)
        {
            // TODO: replace with Header.CloneMe()
            tree.Header.SetHeaderValue(PgnHeaders.KEY_WHITE, header.GetWhitePlayer(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_BLACK, header.GetBlackPlayer(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_VARIANT, header.GetVariant(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_WHITE_ELO, header.GetWhitePlayerElo(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_BLACK_ELO, header.GetBlackPlayerElo(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_ANNOTATOR, header.GetAnnotator(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_RESULT, header.GetResult(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_EVENT, header.GetEventName(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_ROUND, header.GetRound(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_ECO, header.GetECO(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_LICHESS_ID, header.GetLichessId(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_CHESSCOM_ID, header.GetChessComId(out _));
            if (overrideGuid)
            {
                tree.Header.SetHeaderValue(PgnHeaders.KEY_GUID, header.GetGuid(out _));
            }
            tree.Header.SetHeaderValue(PgnHeaders.KEY_DATE, header.GetDate(out _));

            List<string> preamble = header.GetPreamble();
            tree.Header.SetPreamble(preamble);
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
        /// Shows all solutions in the current chapter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExerc_ShowSolutions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                if (chapter != null)
                {
                    ChapterUtils.UpdateShowSolutionsInChapter(chapter, true);
                    UpdateShowSolutionInExerciseView(true);
                    BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgSolutionsShown, CommentBox.HintType.INFO);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnExerc_ShowSolutions_Click()", ex);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Hides all solutions in the current chapter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExerc_HideSolutions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                if (chapter != null)
                {
                    ChapterUtils.UpdateShowSolutionsInChapter(chapter, false);
                    UpdateShowSolutionInExerciseView(false);
                    BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgSolutionsHidden, CommentBox.HintType.INFO);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnExerc_ShowSolutions_Click()", ex);
            }

            e.Handled = true;
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
        /// Allows the user to edit the starting position of an exercise.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnExerc_EditPosition_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                int index = chapter.ActiveExerciseIndex;
                if (index >= 0)
                {
                    VariationTree tree = chapter.Exercises[index].Tree;
                    PositionSetupDialog dlg = new PositionSetupDialog(tree);
                    GuiUtilities.PositionDialog(dlg, this, 100);
                    dlg.ShowDialog();
                    if (dlg.ExitOK)
                    {
                        chapter.Exercises[index].Tree = dlg.FixedTree;
                        //chapter.SetActiveVariationTree(GameData.ContentType.EXERCISE, index);
                        //_exerciseTreeView.BuildFlowDocumentForVariationTree();
                        SelectExercise(index, false);
                        AppState.IsDirty = true;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnExerc_EditPosition_Click()", ex);
            }
        }

        /// <summary>
        /// Exits solving mode if currently active
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExercExitSolving_Click(object sender, RoutedEventArgs e)
        {
            DeactivateSolvingMode();
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
        public void RefreshChaptersViewAfterImport(GameData.ContentType contentType, Chapter chapter, int gameUinitIndex)
        {
            chapter.IsViewExpanded = true;
            switch (contentType)
            {
                case GameData.ContentType.MODEL_GAME:
                    chapter.IsModelGamesListExpanded = true;
                    chapter.ActiveModelGameIndex = gameUinitIndex;
                    break;
                case GameData.ContentType.EXERCISE:
                    chapter.IsExercisesListExpanded = true;
                    chapter.ActiveExerciseIndex = gameUinitIndex;
                    break;
            }

            _chaptersView.BuildFlowDocumentForChaptersView();
            _chaptersView.BringArticleIntoView(chapter.Index, contentType, gameUinitIndex);
        }

        /// <summary>
        /// Show the Select Games dialog.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="games"></param>
        /// <returns></returns>
        public bool ShowSelectGamesDialog(GameData.ContentType contentType, ref ObservableCollection<GameData> games)
        {
            SelectGamesDialog.Mode mode = SelectGamesDialog.Mode.IMPORT_GAMES;
            if (contentType == GameData.ContentType.EXERCISE)
            {
                mode = SelectGamesDialog.Mode.IMPORT_EXERCISES;
            }

            bool res = false;
            SelectGamesDialog dlg = new SelectGamesDialog(ref games, mode);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);
            if (dlg.ShowDialog() == true)
            {
                foreach (var game in games)
                {
                    if (game.IsSelected)
                    {
                        res = true;
                        break;
                    }
                }
            }

            return res;
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
                sError = Properties.Resources.NoExerciseInFile + " ";
            }
            else
            {
                sError = Properties.Resources.NoGamesInFile + " ";
            }
            MessageBox.Show(sError + fileName, Properties.Resources.ImportPgn, MessageBoxButton.OK, MessageBoxImage.Information);
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
                Filter = Properties.Resources.PgnFile + " (*.pgn)|*.pgn;*.pgn|" + Properties.Resources.AllFiles + " (*.*)|*.*"
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
        /// Copies FEN of the selected position to the Clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnciCopyFen_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView.CopyFenToClipboard();
            BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgCopiedFEN, CommentBox.HintType.INFO);
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
        /// Adds the last clicked node to bookmarks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnMarkBookmark_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!AppState.IsUserSolving())
                {
                    Bookmark bm = BookmarkManager.AddBookmark(AppState.ActiveVariationTree, AppState.ActiveVariationTree.SelectedNodeId, AppState.ActiveArticleIndex, out bool alreadyExists);
                    BookmarkManager.SetLastAddedBookmark(bm);

                    if (bm == null)
                    {
                        if (alreadyExists)
                        {
                            MessageBox.Show(Properties.Resources.BookmarkAlreadyExists, Properties.Resources.Bookmarks, MessageBoxButton.OK);
                        }
                        else
                        {
                            MessageBox.Show(Properties.Resources.SelectPosition, Properties.Resources.Bookmarks, MessageBoxButton.OK);
                        }
                    }
                    else
                    {
                        AppState.IsDirty = true;
                        SoundPlayer.PlayConfirmationSound();
                        BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.BookmarkAdded, CommentBox.HintType.INFO);
                        //  UiTabBookmarks.Focus();
                    }
                }
                else
                {
                    MessageBox.Show(Properties.Resources.ErrorNoBookmarksWhileSolving, Properties.Resources.ChessForge, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Deletes a bookmark from the currently selected node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnciDeleteBookmark_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int chapterIndex = AppState.ActiveChapter.Index;
                GameData.ContentType articleType = ActiveVariationTree.ContentType;
                int articleIndex = AppState.ActiveArticleIndex;
                int nodeId = AppState.ActiveVariationTree.SelectedNodeId;

                if (BookmarkManager.DeleteBookmark(chapterIndex, articleType, articleIndex, nodeId) != null)
                {
                    SoundPlayer.PlayConfirmationSound();
                    BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.BookmarkDeleted, CommentBox.HintType.INFO);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnciDeleteBookmark_Click()", ex);
            }
        }


        //**********************
        //
        //  TRAINING
        // 
        //**********************

        /// <summary>
        /// Performs the process of selecting and copying games between chapters.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnCopyArticles_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<ArticleListItem> articleList = WorkbookManager.SessionWorkbook.GenerateArticleList();
            ChapterUtils.RequestCopyMoveArticles(null, true, articleList, ArticlesAction.COPY, false);
        }

        /// <summary>
        /// Performs the process of selecting and moving games between chapters.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnMoveArticles_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<ArticleListItem> articleList = WorkbookManager.SessionWorkbook.GenerateArticleList();
            ChapterUtils.RequestCopyMoveArticles(null, true, articleList, ArticlesAction.MOVE, false);
        }

        /// <summary>
        /// Invokes dialog for sorting games in the active chapter / workbook.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnSortGames_Click(object sender, RoutedEventArgs e)
        {
            ChapterUtils.InvokeSortGamesDialog(AppState.ActiveChapter);
        }

        /// <summary>
        /// Invokes dialog for creating thumbnails.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnSetThumbnails_Click(object sender, RoutedEventArgs e)
        {
            ChapterUtils.InvokeSetThumbnailsDialog(AppState.ActiveChapter);
        }

        /// <summary>
        /// Invokes dialog for configuring Exercise View.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExerciseViewConfig_Click(object sender, RoutedEventArgs e)
        {
            ChapterUtils.InvokeExerciseViewConfigDialog(AppState.ActiveChapter);
        }

        /// <summary>
        /// Invokes dialog to split the active chapter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnSplitChapter_Click(object sender, RoutedEventArgs e)
        {
            SplitChapterUtils.InvokeSplitChapterDialog(AppState.ActiveChapter);
        }

        /// <summary>
        /// Opens the dialog for importing games from the Web
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDownloadWebGames_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (TrainingSession.IsTrainingInProgress)
            {
                GuiUtilities.ShowExitTrainingInfoMessage();
            }
            else
            {
                DownloadWebGamesManager.DownloadGames();
            }
        }

        /// <summary>
        /// Handles a request from the Bookmarks page to navigate to 
        /// the bookmarked position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnBmGotoPosition_Click(object sender, RoutedEventArgs e)
        {
            BookmarkManager.SetActiveEntities(true);
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
        /// The user requested to roll back training to the most recently
        /// clicked run/move.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnRollBackTraining_Click(object sender, RoutedEventArgs e)
        {
            UiTrainingView.RollbackTraining();
        }

        /// <summary>
        /// The user wants to replace the clicked engine move with their own.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnReplaceEngineMove_Click(object sender, RoutedEventArgs e)
        {
            UiTrainingView.ReplaceEngineMove();
        }

        /// <summary>
        /// The user requested evaluation of the most recently clicked run/move.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnTrainEvalMove_Click(object sender, RoutedEventArgs e)
        {
            UiTrainingView.RequestMoveEvaluation(ActiveVariationTree.TreeId);
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
            if (MessageBox.Show(Properties.Resources.RestartTraining, Properties.Resources.Training, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                SetAppInTrainingMode(TrainingSession.StartPosition, TrainingSession.IsContinuousEvaluation);
            }
        }

        /// <summary>
        /// Training View received focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbTrainingProgress_GotFocus(object sender, RoutedEventArgs e)
        {
            if (AppState.ActiveTab == TabViewType.TRAINING)
            {
                return;
            }
        }


        /// <summary>
        /// Sets ActiveTab to Training when Training Tab becomes visible.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabTrainingProgress_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool visible = (bool)e.NewValue;
            if (visible == true)
            {
                WorkbookManager.ActiveTab = TabViewType.TRAINING;
            }
        }

        /// <summary>
        /// Sets ActiveTab to EngineGame when EngineGame Tab becomes visible.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabEngineGame_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool visible = (bool)e.NewValue;
            if (visible == true)
            {
                WorkbookManager.ActiveTab = TabViewType.ENGINE_GAME;
            }
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
        //  ENGINE GAME
        // 
        //**********************

        /// <summary>
        /// Swap sides, user with the engine.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnEngGame_SwapSides_Click(object sender, RoutedEventArgs e)
        {
            EngineGameView?.SwapSides();
        }

        /// <summary>
        /// Restart game from the clicked move.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnEngGame_RestartFromMove_Click(object sender, RoutedEventArgs e)
        {
            EngineGameView?.RestartFromNode(true);
        }

        /// <summary>
        /// Start game from the initial position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnEngGame_StartFromInit_Click(object sender, RoutedEventArgs e)
        {
            EngineGameView?.RestartFromNode(false);
        }

        /// <summary>
        /// Exit the game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnEngGame_ExitGame_Click(object sender, RoutedEventArgs e)
        {
            UiBtnExitGame_Click(sender, e);
        }

        /// <summary>
        /// Registers a mouse click anywhere in the EngineGame RTB.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabEngineGame_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (EngineGameView != null)
            {
                EngineGameView.GeneralMouseClick(e);
            }
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
                int importedGames = ImportGamesFromPgn(GameData.ContentType.GENERIC, GameData.ContentType.MODEL_GAME);
                if (importedGames > 0)
                {
                    if (count > 0)
                    {
                        AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(
                            Properties.Resources.FlMsgGamesImported + " (" + importedGames.ToString() + ")", CommentBox.HintType.INFO);
                    }
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

                int importedExercises = ImportGamesFromPgn(GameData.ContentType.EXERCISE, GameData.ContentType.EXERCISE);
                if (importedExercises > 0)
                {
                    AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(
                        Properties.Resources.FlMsgExercisesImported + " (" + importedExercises.ToString() + ")", CommentBox.HintType.INFO);
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
        /// Copies FEN of the selected move to Clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExerc_CopyFen_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView.CopyFenToClipboard();
        }

        /// <summary>
        /// The user indicated intention to regenerate the study.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnRegenerateStudy_Click(object sender, RoutedEventArgs e)
        {
            ChapterUtils.InvokeRegenerateStudyDialog(AppState.ActiveChapter);
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
        /// Invokes the dialog on the children of a Node to allow
        /// the user to re-order lines.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnReorderLines_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TreeNode nd = ActiveVariationTree.SelectedNode;
                if (nd != null && nd.Parent != null)
                {
                    // create the operation before reordering but only push on the stack if performed
                    List<TreeNode> lstChildrenOrder = new List<TreeNode>();
                    foreach (TreeNode child in nd.Parent.Children)
                    {
                        lstChildrenOrder.Add(child);
                    }

                    EditOperation op = new EditOperation(EditOperation.EditType.REORDER_LINES, nd.Parent, lstChildrenOrder);

                    uint moveOffset = ActiveVariationTree.MoveNumberOffset;
                    ReorderLinesDialog dlg = new ReorderLinesDialog(nd.Parent, moveOffset);
                    {
                        GuiUtilities.PositionDialog(dlg, this, 100);
                        if (dlg.ShowDialog() == true && dlg.HasChanged)
                        {
                            AppState.IsDirty = true;

                            // push operation on the undo stack 
                            ActiveVariationTree.OpsManager.PushOperation(op);

                            ActiveVariationTree.BuildLines();
                            ActiveTreeView.BuildFlowDocumentForVariationTree();
                            SelectLineAndMoveInWorkbookViews(ActiveTreeView, nd.LineId, ActiveLine.GetSelectedPlyNodeIndex(false), false);
                            PulseManager.BringSelectedRunIntoView();
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Creates a new Chapter from the currently selected line.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnCreateChapterFromLine_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView.CreateChapterFromLine();
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
        /// Deletes the subtree having checked with the user.
        /// </summary>
        public void DeleteRemainingMoves()
        {
            if (ActiveTreeView != null && AppState.IsVariationTreeTabType)
            {
                TreeNode nd = ActiveTreeView.GetSelectedNode();
                if (nd != null)
                {
                    if (nd.Children.Count == 0 ||
                        MessageBox.Show(Properties.Resources.MsgConfirmDeleteSubtree, Properties.Resources.Confirmation,
                           MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        ActiveTreeView.DeleteRemainingMoves();
                    }
                }
            }
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

        /// <summary>
        /// Copies selected moves from the view into the Clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnCopyMoves_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView?.PlaceSelectedForCopyInClipboard();
        }

        /// <summary>
        /// Cuts the selected moves i.e. removes the selected moves
        /// and places them in the clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnCutMoves_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveTreeView != null)
            {
                ActiveTreeView.PlaceSelectedForCopyInClipboard();
                ActiveTreeView.DeleteRemainingMoves();
                BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgCopiedMoves, CommentBox.HintType.INFO);
            }
        }

        /// <summary>
        /// Pastes moves from the Clipboard in the view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnPasteMoves_Click(object sender, RoutedEventArgs e)
        {
            CopyPasteMoves.PasteMoveList();
        }

        /// <summary>
        /// Selects the line currently highlighted in the view (Active Line)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnSelectHighlighted_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveTreeView != null)
            {
                ActiveTreeView.SelectActiveLineForCopy();
                ActiveTreeView.PlaceSelectedForCopyInClipboard();
                BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgCopiedLine, CommentBox.HintType.INFO);
            }
        }

        /// <summary>
        /// Selects the Subtree under the currently selected node
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnSelectSubtree_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveTreeView != null)
            {
                ActiveTreeView.SelectSubtreeForCopy();
                ActiveTreeView.PlaceSelectedForCopyInClipboard();
                BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgCopiedTree, CommentBox.HintType.INFO);
            }
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
            GuiUtilities.PositionDialog(dlg, this, 100);
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
            if (AppState.CurrentLearningMode != LearningMode.Mode.IDLE)
            {
                if (ShowWorkbookOptionsDialog())
                {
                    AppState.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// The user requested to edit Blunder Detection options.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnBlunderDetectionOptions_Click(object sender, RoutedEventArgs e)
        {
            BlunderDetectionDialog dlg = new BlunderDetectionDialog();
            GuiUtilities.PositionDialog(dlg, this, 100);

            if (dlg.ShowDialog() == true)
            {
                Configuration.WriteOutConfiguration();
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
            ShowApplicationOptionsDialog();
        }

        /// <summary>
        /// Invokes the Online Libraries dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnOnlineLibraries_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SelectLibraryDialog dlg = new SelectLibraryDialog();
                GuiUtilities.PositionDialog(dlg, this, 100);
                if (dlg.ShowDialog() == true && !string.IsNullOrEmpty(dlg.LibraryToOpen))
                {
                    ShowLibraryContent(dlg.LibraryToOpen);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Invokes the dialog for configuring chessboard colors
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnChessboards_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChessboardColorsDialog dlg = new ChessboardColorsDialog();
                GuiUtilities.PositionDialog(dlg, this, 100);
                if (dlg.ShowDialog() == true)
                {
                    switch (AppState.ActiveTab)
                    {
                        case TabViewType.STUDY:
                        case TabViewType.INTRO:
                        case TabViewType.CHAPTERS:
                        case TabViewType.BOOKMARKS:
                            UiImgMainChessboard.Source = Configuration.StudyBoardSet.MainBoard;
                            break;
                        case TabViewType.MODEL_GAME:
                            UiImgMainChessboard.Source = Configuration.GameBoardSet.MainBoard;
                            break;
                        case TabViewType.EXERCISE:
                            UiImgMainChessboard.Source = Configuration.ExerciseBoardSet.MainBoard;
                            break;
                        case TabViewType.TRAINING:
                            UiImgMainChessboard.Source = Configuration.TrainingBoardSet.MainBoard;
                            break;
                    }
                    Configuration.WriteOutConfiguration();
                }
            }
            catch { }
        }

        /// <summary>
        /// The user requested to edit Engine configuration.
        /// The dialog will be shown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnEngineOptions_Click(object sender, RoutedEventArgs e)
        {
            ShowEngineOptionsDialog();
        }



        //*********************
        //
        // OTHER
        //
        //*********************

        // allows or blocks mode re-initialization.
        // this is used at startup when this method is called when we set IsChecked
        // on the menu item.
        private bool _modeUpdatesBlocked = false;

        /// <summary>
        /// The user requests switch to the dark mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDarkMode_Checked(object sender, RoutedEventArgs e)
        {
            if (!_modeUpdatesBlocked)
            {
                try
                {
                    ChessForgeColors.Initialize(ColorThemes.DARK_MODE);
                    Configuration.IsDarkMode = true;
                    ChessForgeColors.SetMainControlColors();
                    RebuildAllTreeViews(null, true);
                    _openingStatsView.UpdateColorTheme();
                    _topGamesView.UpdateColorTheme();
                    BoardCommentBox.UpdateColorTheme();
                }
                catch (Exception ex)
                {
                    AppLog.Message("UiMnDarkMode_Checked()", ex);
                }
            }
            _modeUpdatesBlocked = false;
        }

        /// <summary>
        /// The user requests switch to the light mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDarkMode_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_modeUpdatesBlocked)
            {
                try
                {
                    ChessForgeColors.Initialize(ColorThemes.LIGHT_MODE);
                    Configuration.IsDarkMode = false;
                    ChessForgeColors.SetMainControlColors();
                    RebuildAllTreeViews(null, true);
                    _openingStatsView.UpdateColorTheme();
                    _topGamesView.UpdateColorTheme();
                    BoardCommentBox.UpdateColorTheme();
                }
                catch (Exception ex)
                {
                    AppLog.Message("UiMnDarkMode_Unchecked()", ex);
                }
            }
            _modeUpdatesBlocked = false;
        }

        /// <summary>
        /// Update the status of the DontSaveEvals flag
        /// per the menu item's status in the event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDontSaveEvals_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mni)
            {
                SetDontSaveEvalsMenuItems(mni.IsChecked);
            }
        }

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
        /// If we are in the Exercise view, make sure the little "passive" board
        /// is oriented the same way.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnFlipBoard_Click(object sender, RoutedEventArgs e)
        {
            bool isFlipped = MainChessBoard.FlipBoard();
            switch (WorkbookManager.ActiveTab)
            {
                case TabViewType.STUDY:
                    SetCustomBoardOrientation((isFlipped ? PieceColor.Black : PieceColor.White), WorkbookManager.ItemType.STUDY);
                    break;
                case TabViewType.MODEL_GAME:
                    SetCustomBoardOrientation((isFlipped ? PieceColor.Black : PieceColor.White), WorkbookManager.ItemType.MODEL_GAME);
                    break;
                case TabViewType.EXERCISE:
                    _exerciseTreeView?.AlignExerciseAndMainBoards();
                    SetCustomBoardOrientation((isFlipped ? PieceColor.Black : PieceColor.White), WorkbookManager.ItemType.EXERCISE);
                    break;
            }
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
        /// Sets the current position in the ActiveTree as a Thumbnail for that tree.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CustomCommand_SetThumbnail(object sender, RoutedEventArgs e)
        {
            UiMn_MarkThumbnail_Click(sender, e);
        }

        /// <summary>
        /// Moves an item (chapter, game, exercise)
        /// up in the list of items, depending which one was the last highlighted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CustomCommand_MoveItemUp(object sender, RoutedEventArgs e)
        {
            if (_chaptersView != null && AppState.ActiveTab == TabViewType.CHAPTERS)
            {
                switch (_chaptersView.LastClickedItemType)
                {
                    case WorkbookManager.ItemType.CHAPTER:
                    case WorkbookManager.ItemType.NONE:
                        UiMnChapterUp_Click(sender, e);
                        break;
                    case WorkbookManager.ItemType.MODEL_GAME:
                        UiMnGameUp_Click(sender, e);
                        break;
                    case WorkbookManager.ItemType.EXERCISE:
                        UiMnExerciseUp_Click(sender, e);
                        break;
                }
            }
        }

        /// <summary>
        /// Moves an item (chapter, game, exercise)
        /// up in the list of items, depending which one was the last highlighted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CustomCommand_MoveItemDown(object sender, RoutedEventArgs e)
        {
            if (_chaptersView != null && AppState.ActiveTab == TabViewType.CHAPTERS)
            {
                switch (_chaptersView.LastClickedItemType)
                {
                    case WorkbookManager.ItemType.CHAPTER:
                    case WorkbookManager.ItemType.NONE:
                        UiMnChapterDown_Click(sender, e);
                        break;
                    case WorkbookManager.ItemType.MODEL_GAME:
                        UiMnGameDown_Click(sender, e);
                        break;
                    case WorkbookManager.ItemType.EXERCISE:
                        UiMnExerciseDown_Click(sender, e);
                        break;
                }
            }
        }

        /// <summary>
        /// Calls the Internet browser to open the ChessForge Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnHelpWiki_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/User's-Manual");
        }


        //********************************************************
        //
        // Methods invoked from Menu Event handlers 
        //
        //********************************************************


        /// <summary>
        /// Checks for updates.
        /// If there is a newer version shows the update info dialog.
        /// Otherwise shows a MessageBox info.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnCheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            if (!ReportNewVersionAvailable(false))
            {
                MessageBox.Show(Properties.Resources.NoNewVersion, Properties.Resources.UpdateCheck, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        /// <summary>
        /// Creates a new Model Game and makes it "Active".
        /// </summary>
        public void CreateNewModelGame(VariationTree gameTree = null)
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
                GuiUtilities.PositionDialog(dlg, this, 100);
                dlg.ShowDialog();
                if (dlg.ExitOK)
                {
                    Article article = WorkbookManager.SessionWorkbook.ActiveChapter.AddModelGame(tree);
                    article.IsReady = true;

                    WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex
                        = WorkbookManager.SessionWorkbook.ActiveChapter.GetModelGameCount() - 1;
                    _chaptersView.BuildFlowDocumentForChaptersView();

                    if (AppState.ActiveTab == TabViewType.MODEL_GAME)
                    {
                        SelectModelGame(WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex, true);
                        //RefreshGamesView(out Chapter chapter, out int articleIndex);
                        //WorkbookLocationNavigator.SaveNewLocation(chapter, GameData.ContentType.MODEL_GAME, articleIndex);

                    }
                    else
                    {
                        // if ActiveTab is not MODEL_GAME, Focus() will call SelectModelGame()
                        // Do not call it explicitly here!
                        UiTabModelGames.Focus();
                    }

                    if (AppState.AreExplorersOn)
                    {
                        WebAccessManager.ExplorerRequest(AppState.ActiveTreeId, ActiveVariationTree.SelectedNode, true);
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
        private void CreateNewExercise()
        {
            try
            {
                PositionSetupDialog dlgPosSetup = new PositionSetupDialog(null)
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

                    GameHeaderDialog dlgHeader = new GameHeaderDialog(tree, Properties.Resources.ResourceManager.GetString("ExerciseHeader"))
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
                        RefreshExercisesView(out Chapter chapter, out int articleIndex);
                        WorkbookLocationNavigator.SaveNewLocation(chapter, GameData.ContentType.EXERCISE, articleIndex);
                        if (AppState.AreExplorersOn)
                        {
                            WebAccessManager.ExplorerRequest(AppState.ActiveTreeId, ActiveVariationTree.SelectedNode);
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
        private void CreateNewExerciseFromTree(VariationTree tree)
        {
            try
            {
                WorkbookManager.SessionWorkbook.ActiveChapter.AddExercise(tree);
                WorkbookManager.SessionWorkbook.ActiveChapter.ActiveExerciseIndex
                    = WorkbookManager.SessionWorkbook.ActiveChapter.GetExerciseCount() - 1;
                _chaptersView.BuildFlowDocumentForChaptersView();
                SelectExercise(WorkbookManager.SessionWorkbook.ActiveChapter.ActiveExerciseIndex, true);
                AppState.IsDirty = true;
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
                    string gameTitle = chapter.ModelGames[chapter.ActiveModelGameIndex].Tree.Header.BuildGameHeaderLine(true);
                    if (MessageBox.Show(Properties.Resources.ConfirmDeleteGame + "?\n\n  " + gameTitle, Properties.Resources.DeleteGame, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
                        if (WorkbookManager.ActiveTab == TabViewType.MODEL_GAME)
                        {
                            SelectModelGame(chapter.ActiveModelGameIndex, false);
                        }
                        AppState.SetupGuiForCurrentStates();
                        if (ActiveVariationTree == null || AppState.CurrentEvaluationMode != EvaluationManager.Mode.CONTINUOUS)
                        {
                            StopEvaluation(true);
                            BoardCommentBox.ShowTabHints();
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
                    string exerciseTitle = chapter.Exercises[chapter.ActiveExerciseIndex].Tree.Header.BuildGameHeaderLine(true);
                    if (MessageBox.Show(Properties.Resources.ConfirmDeleteExercise + "?\n\n  " + exerciseTitle, Properties.Resources.DeleteExercise, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
                        AppState.SetupGuiForCurrentStates();
                        if (WorkbookManager.ActiveTab == TabViewType.EXERCISE)
                        {
                            SelectExercise(chapter.ActiveExerciseIndex, false);
                        }
                    }
                    if (ActiveVariationTree == null || AppState.CurrentEvaluationMode != EvaluationManager.Mode.CONTINUOUS)
                    {
                        StopEvaluation(true);
                        BoardCommentBox.ShowTabHints();
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
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                int gameCount = chapter.GetModelGameCount();
                if (index >= 0 && index < gameCount)
                {
                    Article article = chapter.GetModelGameAtIndex(index);
                    string guid = article.Tree.Header.GetGuid(out _);
                    WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.DELETE_MODEL_GAME, chapter, article, index);
                    chapter.ModelGames.RemoveAt(index);
                    List<FullNodeId> affectedNodes = WorkbookManager.RemoveArticleReferences(guid);
                    if (affectedNodes.Count > 0)
                    {
                        _studyTreeView?.UpdateReferenceRuns(affectedNodes);
                    }
                    WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
                    AppState.IsDirty = true;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Deletes the Exercise at the requested index from the list of games.
        /// </summary>
        /// <param name="index"></param>
        private void DeleteExercise(int index)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                int exerciseCount = chapter.GetExerciseCount();
                if (index >= 0 && index < exerciseCount)
                {
                    Article article = chapter.GetExerciseAtIndex(index);
                    string guid = article.Tree.Header.GetGuid(out _);
                    WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.DELETE_EXERCISE, chapter, article, index);
                    chapter.Exercises.RemoveAt(index);
                    List<FullNodeId> affectedNodes = WorkbookManager.RemoveArticleReferences(guid);
                    if (affectedNodes.Count > 0)
                    {
                        _studyTreeView?.UpdateReferenceRuns(affectedNodes);
                    }
                    WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
                    AppState.IsDirty = true;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Invokes the dialog for editing game header.
        /// </summary>
        public void EditGameHeader()
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;

                VariationTree game = WorkbookManager.SessionWorkbook.ActiveChapter.ModelGames[chapter.ActiveModelGameIndex].Tree;
                var dlg = new GameHeaderDialog(game, Properties.Resources.GameHeader);
                //{
                //    Left = ChessForgeMain.Left + 100,
                //    Top = ChessForgeMain.Top + 100,
                //    Topmost = false,
                //    Owner = this
                //};
                GuiUtilities.PositionDialog(dlg, this, 100);
                dlg.ShowDialog();
                if (dlg.ExitOK)
                {
                    AppState.IsDirty = true;
                    _chaptersView.BuildFlowDocumentForChaptersView();
                    if (WorkbookManager.ActiveTab == TabViewType.MODEL_GAME)
                    {
                        _modelGameTreeView.BuildFlowDocumentForVariationTree();
                    }
                    if (AppState.AreExplorersOn)
                    {
                        WebAccessManager.ExplorerRequest(AppState.ActiveTreeId, ActiveVariationTree.SelectedNode);
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
        public void EditExerciseHeader()
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;

                VariationTree game = WorkbookManager.SessionWorkbook.ActiveChapter.Exercises[chapter.ActiveExerciseIndex].Tree;
                var dlg = new GameHeaderDialog(game, Properties.Resources.ExerciseHeader);
                //{
                //    Left = ChessForgeMain.Left + 100,
                //    Top = ChessForgeMain.Top + 100,
                //    Topmost = false,
                //    Owner = this
                //};
                GuiUtilities.PositionDialog(dlg, this, 100);
                dlg.ShowDialog();
                if (dlg.ExitOK)
                {
                    AppState.IsDirty = true;
                    _chaptersView.BuildFlowDocumentForChaptersView();
                    if (WorkbookManager.ActiveTab == TabViewType.EXERCISE)
                    {
                        _exerciseTreeView.BuildFlowDocumentForVariationTree();
                    }
                }
                if (AppState.AreExplorersOn)
                {
                    WebAccessManager.ExplorerRequest(AppState.ActiveTreeId, ActiveVariationTree.SelectedNode, true);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("EditExerciseHeader()" + ex.Message);
            }
        }

        //*****************************************************************
        //
        // SELF-INDEXING VIEW methods
        //
        //*****************************************************************

        /// <summary>
        /// Expand the clicked sector
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnciExpand_Click(object sender, RoutedEventArgs e)
        {
            _studyTreeView?.ExpandSectorFromMenu(sender);
            e.Handled = true;
        }

        /// <summary>
        /// Collapse the clicked sector
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnciCollapse_Click(object sender, RoutedEventArgs e)
        {
            _studyTreeView?.CollapseSectorFromMenu(sender);
            e.Handled = true;
        }

        /// <summary>
        /// Expand all sectors
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnciExpandAll_Click(object sender, RoutedEventArgs e)
        {
            _studyTreeView?.ExpandAllSectorsFromMenu(sender);
            e.Handled = true;
        }

        /// <summary>
        /// Collapse all sectors
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void UiMnciCollapseAll_Click(object sender, RoutedEventArgs e)
        {
            _studyTreeView?.CollapseAllSectorsFromMenu(sender);
            e.Handled = true;
        }

        /// <summary>
        /// Expand just the clicked sector
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnciExpandThisOne_Click(object sender, RoutedEventArgs e)
        {
            _studyTreeView?.ExpandThisSectorOnlyFromMenu(sender);
            e.Handled = true;
        }

        //*****************************************************************
        //
        // INTRO VIEW methods
        //
        //*****************************************************************

        /// <summary>
        /// The user wants to create a new diagram in the Intro view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCmiInsertDiagram_Click(object sender, RoutedEventArgs e)
        {
            _introView?.CreateDiagram();
        }

        /// <summary>
        /// Editing of a diagram in the Intro view was requested.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCmiEditDiagram_Click(object sender, RoutedEventArgs e)
        {
            _introView?.EditDiagram();
        }

        /// <summary>
        /// The user wants to create a hyperlink in the Intro view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCmiInsertHyperlink_Click(object sender, RoutedEventArgs e)
        {
            _introView?.CreateHyperlink();
        }

        /// <summary>
        /// Editing of a hyperlink in the Intro view was requested.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCmiEditHyperlink_Click(object sender, RoutedEventArgs e)
        {
            _introView?.EditHyperlink(sender);
        }

        /// <summary>
        /// Paste previously copied content 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IntroViewPaste_Click(object sender, RoutedEventArgs e)
        {
            _introView?.Paste(sender, e);
        }

        /// <summary>
        /// Cut selected content
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IntroViewCut_Click(object sender, RoutedEventArgs e)
        {
            _introView?.Cut(sender, e);
        }

        /// <summary>
        /// Store the current selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IntroViewCopy_Click(object sender, RoutedEventArgs e)
        {
            _introView?.Copy(sender, e);
        }

        /// <summary>
        /// Undo previous edit operation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IntroViewUndo_Click(object sender, RoutedEventArgs e)
        {
            _introView?.Undo(sender, e);
        }

        /// <summary>
        /// To be used if early text input preview is required.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbIntroView_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //if (e.SystemText.Length > 0 && e.SystemText[0] >= 32)
            //{
            //    if (GuiUtilities.InsertFigurine(UiRtbIntroView, e.SystemText[0]))
            //    {
            //        e.Handled = true;
            //    }
            //}
        }

        /// <summary>
        /// Invoked when selection in the Intro view changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbIntroView_SelectionChanged(object sender, RoutedEventArgs e)
        {
        }


        /// <summary>
        /// Flip the diagram in the Intro view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCmiFlipDiagram_Click(object sender, RoutedEventArgs e)
        {
            _introView?.FlipDiagram();
        }

        /// <summary>
        /// Editing of a move in the Intro view was requested.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCmiEditMove_Click(object sender, RoutedEventArgs e)
        {
            _introView?.EditMove();
        }

        /// <summary>
        /// The AutoSave Off image was clicked
        /// which toggles it to On.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgAutoSaveOff_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Configuration.AutoSave = true;
            Timers.Start(AppTimers.TimerId.AUTO_SAVE);
            SetupMenuBarControls();
        }

        /// <summary>
        /// The AutoSave On image was clicked
        /// which toggles it to Off.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgAutoSaveOn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Configuration.AutoSave = false;
            Timers.Stop(AppTimers.TimerId.AUTO_SAVE);
            SetupMenuBarControls();
        }

        /// <summary>
        /// The increase font button was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnFontSizeUp_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.ActiveTab == TabViewType.INTRO)
            {
                UiBtnIntroFontSizeUp_Click(sender, e);
            }
            else if (!Configuration.IsFontSizeAtMax)
            {
                Configuration.FontSizeDiff++;
                SetupMenuBarControls();
                RebuildAllTreeViews(true);
            }
            AppState.ConfigureFontSizeMenus();
        }

        /// <summary>
        /// The decrease font button was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnFontSizeDown_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.ActiveTab == TabViewType.INTRO)
            {
                UiBtnIntroFontSizeDown_Click(sender, e);
            }
            else if (!Configuration.IsFontSizeAtMin)
            {
                Configuration.FontSizeDiff--;
                SetupMenuBarControls();
                RebuildAllTreeViews(false);
            }

            AppState.ConfigureFontSizeMenus();
        }

        /// <summary>
        /// The button requesting the use of fixed size font was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnFontSizeFixed_Click(object sender, RoutedEventArgs e)
        {
            Configuration.UseFixedFont = true;
            SetupMenuBarControls();
            RebuildAllTreeViews();
            AppState.ConfigureFontSizeMenus();
        }

        /// <summary>
        /// The button requesting the use of variable size font was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnFontSizeVariable_Click(object sender, RoutedEventArgs e)
        {
            Configuration.UseFixedFont = false;
            SetupMenuBarControls();
            RebuildAllTreeViews();
            AppState.ConfigureFontSizeMenus();
        }

        /// <summary>
        /// A menu item was clciked requesting a flip between a fixed and variable font size.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnFixVariableFont_Click(object sender, RoutedEventArgs e)
        {
            if (Configuration.UseFixedFont == true)
            {
                UiBtnFontSizeVariable_Click(sender, e);
            }
            else
            {
                UiBtnFontSizeFixed_Click(sender, e);
            }
        }

        /// <summary>
        /// The user requested that the font defaults be restored.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnFontDefaults_Click(object sender, RoutedEventArgs e)
        {
            Configuration.FontSizeDiff = 0;
            // note that the next call causes a rebuild of the views so do not call it explicitly 
            UiBtnFontSizeVariable_Click(sender, e);
            AppState.ConfigureFontSizeMenus();
        }

        /// <summary>
        /// Sets up controls in the menu bar according to the current configuration
        /// </summary>
        private void SetupMenuBarControls()
        {
            UiImgAutoSaveOn.Visibility = Configuration.AutoSave ? Visibility.Visible : Visibility.Hidden;
            UiImgAutoSaveOff.Visibility = Configuration.AutoSave ? Visibility.Hidden : Visibility.Visible;

            UiBtnFontSizeUp.IsEnabled = !Configuration.IsFontSizeAtMax;
            UiBtnFontSizeDown.IsEnabled = !Configuration.IsFontSizeAtMin;

            UiBtnFontSizeFixed.Visibility = Configuration.UseFixedFont ? Visibility.Hidden : Visibility.Visible;
            UiBtnFontSizeVariable.Visibility = Configuration.UseFixedFont ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>
        /// Invoked from a debug menu,
        /// writes out the content of the current view to an RTF file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnWriteRtf_Click(object sender, RoutedEventArgs e)
        {
            bool done = false;

            while (!done)
            {
                done = true;
                RtfExportDialog dlg = new RtfExportDialog();
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        string filePath = RtfWriter.SelectTargetRtfFile();

                        if (!string.IsNullOrEmpty(filePath) && filePath[0] != '.')
                        {
                            Mouse.SetCursor(Cursors.Wait);
                            done = RtfWriter.WriteRtf(filePath);
                            Mouse.SetCursor(Cursors.Arrow);
                        }
                    }
                    catch { }
                }
            }
        }
    }
}

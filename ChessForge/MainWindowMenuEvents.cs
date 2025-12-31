using ChessPosition;
using ChessPosition.GameTree;
using GameTree;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

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
            _studyTreeView.BuildFlowDocumentForVariationTree(false);
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
            _studyTreeView?.BuildFlowDocumentForVariationTree(false);
            _modelGameTreeView?.BuildFlowDocumentForVariationTree(false);
            _exerciseTreeView?.BuildFlowDocumentForVariationTree(false);
            _chaptersView?.BuildFlowDocumentForChaptersView(false);

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
                }
                ;

                if (result == true)
                {
                    try
                    {
                        Configuration.LastOpenDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                    }
                    catch { }
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
                try
                {
                    Configuration.LastOpenDirectory = Path.GetDirectoryName(path);
                }
                catch { }
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
                                    AppState.SetupGuiForCurrentStates();
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
                ActiveTreeView.InsertOrUpdateCommentBeforeMoveRun(nd);
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
            SelectChaptersDialog dlg = new SelectChaptersDialog(WorkbookManager.SessionWorkbook, SelectChaptersDialog.Mode.MERGE, Properties.Resources.SelectChaptersToMerge);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

            dlg.ShowDialog();
            if (dlg.ExitOK)
            {
                int mergedCount = 0;
                VariationTree merged = null;
                string title = "";

                List<VariationTree> treeList = new List<VariationTree>();
                foreach (SelectedChapter ch in dlg.ChapterList)
                {
                    if (ch.IsSelected)
                    {
                        treeList.Add(ch.Chapter.StudyTree.Tree);
                        if (mergedCount == 0)
                        {
                            title = ch.Chapter.Title;
                        }
                        mergedCount++;
                    }
                }

                if (mergedCount > 1 && _chaptersView != null)
                {
                    merged = TreeMerge.MergeVariationTreeListEx(treeList, 0, true);

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
                    _chaptersView.BuildFlowDocumentForChaptersView(false);
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

            PulseManager.SetPauseCounter(5);
            AppState.ActiveVariationTree.OpsManager.Undo(out EditOperation.EditType opType,
                                                         out string selectedLineId,
                                                         out int selectedNodeId,
                                                         out HashSet<int> nodesToUpdate,
                                                         out bool noRefresh);

            try
            {
                // we fully rebuild the view only if nodesToUpdate is null,
                // otherwise we just update the nodes in the list
                bool fullRebuild = nodesToUpdate == null;

                TreeNode selectedNode = AppState.ActiveVariationTree.GetNodeFromNodeId(selectedNodeId);
                ActiveLine.UpdateMoveText(selectedNode);

                MultiTextBoxManager.ShowEvaluationChart(true);

                if (selectedNode == null && !noRefresh)
                {
                    selectedNodeId = 0;
                    selectedLineId = "1";
                    MainChessBoard.DisplayPosition(AppState.ActiveVariationTree.RootNode, true);
                }

                if (fullRebuild)
                {
                    AppState.MainWin.ActiveTreeView.BuildFlowDocumentForVariationTree(false);
                }
                else
                {
                    foreach (int nodeId in nodesToUpdate)
                    {
                        TreeNode node = AppState.ActiveVariationTree.GetNodeFromNodeId(nodeId);
                        AppState.MainWin.ActiveTreeView.InsertOrUpdateCommentRun(node);
                    }
                }

                if (!noRefresh)
                {
                    RefreshLineAndRunSelection(selectedLineId, selectedNode, selectedNodeId);
                }

                if (opType == EditOperation.EditType.UPDATE_ANNOTATION)
                {
                    AppState.MainWin.ActiveTreeView.InsertOrUpdateCommentRun(selectedNode);
                    AppState.MainWin.ActiveTreeView.InsertOrUpdateCommentBeforeMoveRun(selectedNode);
                }
                else if (opType == EditOperation.EditType.UPDATE_COMMENT_BEFORE_MOVE)
                {
                    AppState.MainWin.ActiveTreeView.InsertOrUpdateCommentBeforeMoveRun(selectedNode);
                }

                if (!string.IsNullOrEmpty(selectedLineId))
                {
                    AppState.MainWin.ActiveTreeView.HighlightLineAndMove(AppState.MainWin.ActiveTreeView.HostRtb.Document, selectedLineId, selectedNodeId);
                }

                if (!noRefresh)
                {
                    PulseManager.BringSelectedRunIntoView();
                }

                AppState.IsDirty = true;
            }
            catch (Exception ex)
            {
                AppLog.Message("UndoTreeEditOperation()", ex);
            }

            return true;
        }

        /// <summary>
        /// Refreshes/restores the line and runs selection after an undo operation.
        /// </summary>
        /// <param name="selectedLineId"></param>
        /// <param name="selectedNode"></param>
        /// <param name="selectedNodeId"></param>
        private void RefreshLineAndRunSelection(string selectedLineId, TreeNode selectedNode, int selectedNodeId)
        {
            if (!string.IsNullOrEmpty(selectedLineId))
            {
                AppState.MainWin.ActiveTreeView.SelectNode(selectedNode);
                AppState.MainWin.SetActiveLine(selectedLineId, selectedNodeId);
            }
            else if (selectedNodeId >= 0)
            {
                if (selectedNode != null)
                {
                    selectedLineId = selectedNode.LineId;
                    AppState.MainWin.ActiveTreeView.SelectNode(selectedNode);
                    AppState.MainWin.SetActiveLine(selectedLineId, selectedNodeId);
                }
            }
        }

        /// <summary>
        /// Undo the last WorkbookOperation 
        /// </summary>
        private void UndoWorkbookOperation()
        {
            try
            {
                PulseManager.SetPauseCounter(5);
                WorkbookOperation op = WorkbookManager.SessionWorkbook.OpsManager.Peek();
                if (op != null)
                {
                    ConfirmUndoDialog dlg = new ConfirmUndoDialog(op);
                    GuiUtilities.PositionDialog(dlg, this, 100);
                    if (dlg.ShowDialog() == true)
                    {
                        if (WorkbookManager.SessionWorkbook.OpsManager.Undo(out WorkbookOperationType opType, out int selectedChapterIndex, out int selectedArticleIndex))
                        {
                            VariationTreeView view = AppState.MainWin.ActiveTreeView;
                            switch (opType)
                            {
                                case WorkbookOperationType.RENAME_CHAPTER:
                                    view?.BuildFlowDocumentForVariationTree(false);
                                    _chaptersView.BuildFlowDocumentForChaptersView(false);
                                    break;
                                case WorkbookOperationType.DELETE_CHAPTER:
                                case WorkbookOperationType.CREATE_CHAPTER:
                                    _chaptersView.BuildFlowDocumentForChaptersView(false);
                                    if (AppState.ActiveTab != TabViewType.CHAPTERS)
                                    {
                                        UiTabChapters.Focus();
                                    }
                                    AppState.DoEvents();
                                    _chaptersView.BringChapterIntoViewByIndex(_chaptersView.HostRtb.Document, selectedChapterIndex);
                                    break;
                                case WorkbookOperationType.IMPORT_LICHESS_GAME:
                                    if (AppState.ActiveTab == TabViewType.CHAPTERS)
                                    {
                                        _chaptersView.BuildFlowDocumentForChaptersView(false);
                                    }
                                    else
                                    {
                                        _chaptersView.IsDirty = true;
                                        if (AppState.ActiveTab == TabViewType.MODEL_GAME)
                                        {
                                            SelectModelGame(selectedArticleIndex, true);
                                        }
                                    }
                                    break;
                                case WorkbookOperationType.DELETE_MODEL_GAMES:
                                case WorkbookOperationType.DELETE_EXERCISES:
                                case WorkbookOperationType.DELETE_ARTICLES:
                                case WorkbookOperationType.REGENERATE_STUDIES:
                                    break;
                                case WorkbookOperationType.DELETE_CHAPTERS:
                                case WorkbookOperationType.MERGE_CHAPTERS:
                                case WorkbookOperationType.SPLIT_CHAPTER:
                                case WorkbookOperationType.SORT_GAMES:
                                    AppState.MainWin.ChaptersView.IsDirty = true;
                                    GuiUtilities.RefreshChaptersView(null);
                                    AppState.MainWin.UiTabChapters.Focus();
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
                                case WorkbookOperationType.DELETE_ENGINE_EVALS:
                                case WorkbookOperationType.CLEAN_LINES_AND_COMMENTS:
                                    RebuildAllTreeViews();
                                    ActiveLine.RefreshNodeList(true);
                                    if (view != null)
                                    {
                                        TreeNode selNode = view.GetSelectedNode();
                                        if (selNode != null)
                                        {
                                            view.RestoreSelectedLineAndNode();
                                        }
                                    }
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
            DebugDumps.DumpDebugLogs(true);
        }

        /// <summary>
        /// Writes out the debug file with states and timers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDebugDumpStates_Click(object sender, RoutedEventArgs e)
        {
            DebugDumps.DumpDebugStates();
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

            if (AppState.CurrentLearningMode != LearningMode.Mode.IDLE
                && (AppState.IsDirty || string.IsNullOrEmpty(AppState.WorkbookFilePath)) || (ActiveVariationTree != null && ActiveVariationTree.HasTrainingMoves()))
            {
                try
                {
                    if (!WorkbookManager.PromptAndSaveWorkbook(false, out _, true))
                    {
                        e.Cancel = true;
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Message("ChessForgeMain_Closing() abandoned", ex);
                    e.Cancel = true;
                }
            }

            if (e.Cancel != true)
            {
                WorkbookViewState wvs = new WorkbookViewState(SessionWorkbook);
                wvs.SaveState();

                SoundPlayer.CloseAll();

                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);
                EngineMessageProcessor.ChessEngineService.StopEngine();

                Timers.StopAll();

                DebugDumps.DumpDebugLogs(false);
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
            if (isTrainingOrSolving
                || ActiveVariationTree == null
                || !(AppState.IsTreeViewTabActive() || AppState.ActiveTab == TabViewType.INTRO))
            {
                return;
            }

            try
            {
                TreeNode nd = ActiveVariationTree == null ? null : ActiveVariationTree.SelectedNode;

                SearchPositionCriteria crits = new SearchPositionCriteria(nd);
                crits.FindMode = FindIdenticalPositions.Mode.IDENTICAL;
                crits.IsPartialSearch = false;
                crits.SetCheckDynamicAttrs(true);
                crits.ExcludeCurrentNode = true;
                crits.ReportNoFind = true;

                FindIdenticalPositions.Search(false, crits, out _);
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
                BoardPosition position = PreparePositionForSearch();
                TreeNode searchNode = new TreeNode(null, "", 1);
                bool searchAgain = true;
                while (searchAgain)
                {
                    SearchPositionDialog dlg = new SearchPositionDialog(position);
                    GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);
                    if (dlg.ShowDialog() == true)
                    {
                        searchNode.Position = new BoardPosition(dlg.PositionSetup);
                        // store for another possible loop
                        position = searchNode.Position;

                        SearchPositionCriteria crits = new SearchPositionCriteria(searchNode);
                        crits.FindMode = FindIdenticalPositions.Mode.POSITION_MATCH;
                        crits.IsPartialSearch = Configuration.PartialSearch;
                        crits.SetCheckDynamicAttrs(false);
                        crits.ExcludeCurrentNode = false;
                        crits.ReportNoFind = true;

                        FindIdenticalPositions.Search(true, crits, out searchAgain);
                    }
                    else
                    {
                        searchAgain = false;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnFindPositions_Click()", ex);
            }
        }

        /// <summary>
        /// Determines the position to use for search.
        /// If there is a selected node its position will be used for search.
        /// If not, the clipboard content will be tested if it contains a valid FEN.
        /// If so, it will be used, otherwise we will set the starting position.
        /// </summary>
        /// <returns></returns>
        private BoardPosition PreparePositionForSearch()
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

            return position;
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

            // double check that we are in the Study / Games /EXERCISES tab.
            if (AppState.ActiveTab == TabViewType.STUDY || AppState.ActiveTab == TabViewType.MODEL_GAME || AppState.ActiveTab == TabViewType.EXERCISE)
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
                    EngineGame.ActiveTabOnStart = AppState.ActiveTab;
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
                        CreateNewArticle.CreateNewModelGame(EngineGame.Line.Tree);
                        UiTabModelGames.Focus();
                    }

                    GameData.ContentType contentType = ActiveVariationTree == null ? GameData.ContentType.NONE : ActiveVariationTree.ContentType;
                    if (contentType == GameData.ContentType.EXERCISE)
                    {
                        _exerciseTreeView.DeactivateSolvingMode(VariationTree.SolvingMode.NONE);
                    }

                    AppState.SwapCommentBoxForEngineLines(false);
                    BoardCommentBox.RestoreTitleMessage(contentType);
                    LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);
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

            if (!TrainingSession.IsTrainingInProgress 
                && (EngineGame.ActiveTabOnStart == TabViewType.MODEL_GAME || EngineGame.ActiveTabOnStart == TabViewType.STUDY)
                && EngineGame.HasNewMoves())
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


        //**********************
        //
        //  ASSESSMENTS CONTEXT MENU
        // 
        //**********************

        /// <summary>
        /// Removes the clicked assessment.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnciRemoveAssessment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int nodeId = ActiveTreeView.LastClickedAssessmentNodeId;
                TreeNode node = ActiveVariationTree.GetNodeFromNodeId(nodeId);

                if (node != null && node.Assessment != 0)
                {
                    node.Assessment = 0;

                    //EditOperation editOp = new EditOperation(EditOperation.EditType.DELETE_ASSESSMENTS, node, refGuid);
                    //ActiveVariationTree.OpsManager.PushOperation(editOp);
                    ActiveTreeView.InsertOrUpdateCommentRun(node);
                    AppState.IsDirty = true;
                }
            }
            catch { }
        }

        /// <summary>
        /// Removes all assessments in the current view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnciRemoveAllAssessments_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var nodeAssessments = new Dictionary<int, uint>();
                foreach (TreeNode node in ActiveVariationTree.Nodes)
                {
                    if (node.Assessment != 0)
                    {
                        nodeAssessments.Add(node.NodeId, node.Assessment);
                        node.Assessment = 0;
                        ActiveTreeView.InsertOrUpdateCommentRun(node);
                    }
                }

                if (nodeAssessments.Count > 0)
                {
                    AppState.IsDirty = true;
                    EditOperation editOp = new EditOperation(EditOperation.EditType.DELETE_ASSESSMENTS, nodeAssessments, null);
                    ActiveVariationTree.OpsManager.PushOperation(editOp);
                }
            }

            catch { }
        }



        //**********************
        //
        //  REFERENCES CONTEXT MENU
        // 
        //**********************

        /// <summary>
        /// Removes the clicked reference.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnciRemoveReference_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int nodeId = ReferenceUtils.LastClickedReferenceNodeId;
                string refGuid = ReferenceUtils.LastClickedReference;
                TreeNode node = ActiveVariationTree.GetNodeFromNodeId(nodeId);

                EditOperation editOp = new EditOperation(EditOperation.EditType.DELETE_REFERENCE, node, refGuid);
                ActiveVariationTree.OpsManager.PushOperation(editOp);

                ReferenceUtils.RemoveReferenceFromNode(node, refGuid);
                ActiveTreeView.InsertOrUpdateCommentRun(node);
                AppState.IsDirty = true;
            }
            catch { }
        }

        /// <summary>
        /// Moves the clicked reference to its optimal location.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnciAutoPlaceReference_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TreeNode referencingNode = ActiveVariationTree.GetNodeFromNodeId(ReferenceUtils.LastClickedReferenceNodeId);
                ReferenceUtils.RepositionReferences(ActiveVariationTree, referencingNode, ReferenceUtils.LastClickedReference);
            }
            catch { }
        }

        /// <summary>
        /// Moves all references in the node with the last clicked reference to their optimal location.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnciAutoPlaceMoveReferences_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TreeNode referencingNode = ActiveVariationTree.GetNodeFromNodeId(ReferenceUtils.LastClickedReferenceNodeId);
                ReferenceUtils.RepositionReferences(ActiveVariationTree, referencingNode);
            }
            catch { }
        }

        /// <summary>
        /// Moves all references in the ActiveTree to their optimal location.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnciAutoPlaceAllReferences_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReferenceUtils.RepositionReferences(ActiveVariationTree, null);
            }
            catch { }
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

            if (e.ChangedButton == MouseButton.Right)
            {
                WorkbookManager.EnableChaptersContextMenuItems(UiMncChapters, WorkbookManager.LastClickedChapterIndex >= 0, GameData.ContentType.GENERIC);
            }
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

            _chaptersView?.BuildFlowDocumentForChaptersView(false);
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
                    _chaptersView.BringChapterIntoView(_chaptersView.HostRtb.Document, chapter.Index);
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
            try
            {
                ImportFromPgn.ImportChapter();
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnImportChapter_Click()", ex);
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
            _chaptersView.BringChapterIntoView(_chaptersView.HostRtb.Document, WorkbookManager.SessionWorkbook.ActiveChapterIndex);
        }

        /// <summary>
        /// Lets the user select games exercises from which to create a new Chapter.
        /// </summary>
        /// <param name="gamesCount"></param>
        /// <param name="games"></param>
        /// <param name="fileName"></param>
        public void CreateChapterFromNewGames(int gamesCount, ref ObservableCollection<GameData> games, string fileName)
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

                        _chaptersView.BuildFlowDocumentForChaptersView(false);
                        SelectChapterByIndex(chapter.Index, false);
                        _chaptersView.BringChapterIntoView(_chaptersView.HostRtb.Document, chapter.Index);

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
                    ImportFromPgn.ShowNoGamesError(GameData.ContentType.GENERIC, fileName);

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
                        if (PgnArticleUtils.AddArticle(chapter, gd, GameData.ContentType.EXERCISE, out error, out _, GameData.ContentType.EXERCISE) >= 0)
                        {
                            copiedCount++;
                            copiedExercises++;
                        }
                        chapter.StudyTree.Tree.ContentType = GameData.ContentType.STUDY_TREE;
                    }
                    else if (copyGames && (gd.GetContentType(false) == GameData.ContentType.GENERIC || gd.GetContentType(false) == GameData.ContentType.MODEL_GAME))
                    {
                        if (PgnArticleUtils.AddArticle(chapter, gd, GameData.ContentType.MODEL_GAME, out error, out _, GameData.ContentType.MODEL_GAME) >= 0)
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
                        _chaptersView.BuildFlowDocumentForChaptersView(false);
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
            _chaptersView.BringChapterIntoViewByIndex(_chaptersView.HostRtb.Document, index);
        }

        /// <summary>
        /// Brings the requested article into view in ChaptersView.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="contentType"></param>
        /// <param name="index"></param>
        public void BringArticleIntoView(int chapterIndex, GameData.ContentType contentType, int index)
        {
            _chaptersView.BringArticleIntoView(_chaptersView.HostRtb.Document, chapterIndex, contentType, index);
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

                _chaptersView.RebuildChapterParagraph(_chaptersView.HostRtb.Document, AppState.Workbook.Chapters[index]);
                _chaptersView.RebuildChapterParagraph(_chaptersView.HostRtb.Document, AppState.Workbook.Chapters[index - 1]);
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

                _chaptersView.RebuildChapterParagraph(_chaptersView.HostRtb.Document, AppState.Workbook.Chapters[index]);
                _chaptersView.RebuildChapterParagraph(_chaptersView.HostRtb.Document, AppState.Workbook.Chapters[index + 1]);
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

                    _chaptersView.SwapModelGames(_chaptersView.HostRtb.Document, chapter, index, index - 1);
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

                    _chaptersView.SwapExercises(_chaptersView.HostRtb.Document, chapter, index, index - 1);
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

                    _chaptersView.SwapModelGames(_chaptersView.HostRtb.Document, chapter, index, index + 1);
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

                    _chaptersView.SwapExercises(_chaptersView.HostRtb.Document, chapter, index, index + 1);
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
        /// Toggles the Post Comment Diagram flag on the currently selected node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMn_InsertBeforeMoveDiagram_Click(object sender, RoutedEventArgs e)
        {
            InsertOrDeleteDiagram(true, true);
        }

        /// <summary>
        /// Toggles the Post Comment Diagram flag on the currently selected node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMn_InsertAfterMoveDiagram_Click(object sender, RoutedEventArgs e)
        {
            InsertOrDeleteDiagram(false, true);
        }

        /// <summary>
        /// Deletes the diagram. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMn_DeleteDiagram_Click(object sender, RoutedEventArgs e)
        {
            InsertOrDeleteDiagram(false, false);
        }

        /// <summary>
        /// Swaps the comment with the diagram. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMn_SwapDiagramComment_Click(object sender, RoutedEventArgs e)
        {
            SwapCommentWithDiagram();
        }

        /// <summary>
        /// Inserts or removes a diagram.
        /// </summary>
        /// <param name="insertOrDelete"></param>
        /// <param name="beforeMove"></param>
        public void InsertOrDeleteDiagram(bool beforeMove, bool? insertOrDelete)
        {
            if (AppState.MainWin.ActiveTreeView != null)
            {
                TreeNode nd = AppState.MainWin.ActiveTreeView.GetSelectedNode();
                if (nd != null)
                {
                    if (insertOrDelete == null)
                    {
                        insertOrDelete = !nd.IsDiagram;
                    }
                    string lineId = AppState.MainWin.ActiveVariationTree.SelectedLineId;

                    if (insertOrDelete == true)
                    {
                        nd.IsDiagramBeforeMove = beforeMove;
                        // when inserting a new diagram, we set the IsDiagramPreComment flag to true
                        nd.IsDiagramPreComment = true;
                    }

                    ActiveTreeView?.ToggleDiagramFlag(nd, insertOrDelete == true);
                    if (nd.IsDiagramBeforeMove)
                    {
                        ActiveTreeView.InsertOrUpdateCommentBeforeMoveRun(nd);
                    }
                    else
                    {
                        ActiveTreeView.InsertOrUpdateCommentRun(nd);
                    }
                }
            }
        }

        /// <summary>
        /// Swaps the position of the diagram versus the comment's text
        /// (i.e. before the diagram or after)
        /// </summary>
        public void SwapCommentWithDiagram()
        {
            if (AppState.MainWin.ActiveTreeView != null)
            {
                TreeNode nd = AppState.MainWin.ActiveTreeView.GetSelectedNode();
                if (nd != null && nd.IsDiagram)
                {
                    string lineId = AppState.MainWin.ActiveVariationTree.SelectedLineId;
                    ActiveTreeView.SwapCommentWithDiagram(nd);
                    if (nd.IsDiagramBeforeMove)
                    {
                        ActiveTreeView.InsertOrUpdateCommentBeforeMoveRun(nd);
                    }
                    else
                    {
                        ActiveTreeView.InsertOrUpdateCommentRun(nd);
                    }
                }
            }
        }

        /// <summary>
        /// Invert the diagram.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMn_InvertDiagram_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.MainWin.ActiveTreeView != null)
            {
                TreeNode nd = AppState.MainWin.ActiveTreeView.GetSelectedNode();
                if (nd != null)
                {
                    if (nd.IsDiagram)
                    {
                        nd.IsDiagramFlipped = !nd.IsDiagramFlipped;
                        if (nd.IsDiagramBeforeMove)
                        {
                            ActiveTreeView.InsertOrUpdateCommentBeforeMoveRun(nd);
                        }
                        else
                        {
                            ActiveTreeView.InsertOrUpdateCommentRun(nd);
                        }
                        AppState.IsDirty = true;
                    }
                }
            }
        }

        /// <summary>
        /// Saves the diagram as an image.
        /// The request is coming from the main board menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMn_SaveDiagramMainBoard_Click(object sender, RoutedEventArgs e)
        {
            SaveDiagram.SaveDiagramAsImage(true);
        }

        /// <summary>
        /// Saves the diagram as an image.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMn_SaveDiagram_Click(object sender, RoutedEventArgs e)
        {
            SaveDiagram.SaveDiagramAsImage(false);
        }

        /// <summary>
        /// Saves all diagrams as images.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMn_SaveDiagrams_Click(object sender, RoutedEventArgs e)
        {
            SaveDiagram.SaveDiagramsAsImages();
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
            CreateNewArticle.CreateNewModelGame();
        }

        /// <summary>
        /// Creates a new Model Game from the Games View context menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnGame_CreateModelGame_Click(object sender, RoutedEventArgs e)
        {
            CreateNewArticle.CreateNewModelGame();
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

                    // remove any comments, references and the diagram from the first move
                    tree.RootNode.Comment = "";
                    tree.RootNode.CommentBeforeMove = "";
                    tree.RootNode.Nags = "";

                    tree.RootNode.References = "";

                    tree.RootNode.IsDiagram = false;
                    tree.RootNode.IsDiagramFlipped = false;
                    tree.RootNode.IsDiagramPreComment = false;
                    tree.RootNode.IsDiagramBeforeMove = false;

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

                    // remember the currently active because the next call will change it
                    VariationTreeView startView = ActiveTreeView;
                    CreateNewArticle.CreateNewExerciseFromTree(tree);

                    // now SortReferenceString will find the just created exercise so we can go ahead and update refs
                    nd.AddArticleReference(tree.Header.GetGuid(out _));
                    nd.References = ReferenceUtils.SortReferenceString(nd.References);
                    startView.InsertOrUpdateCommentRun(nd);
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
            CreateNewArticle.CreateNewExercise();
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

                    // preserve current ShowTreeLines setting
                    bool showLines = tree.ShowTreeLines;

                    PositionSetupDialog dlg = new PositionSetupDialog(tree);
                    GuiUtilities.PositionDialog(dlg, this, 100);
                    dlg.ShowDialog();
                    if (dlg.ExitOK)
                    {
                        chapter.Exercises[index].Tree = dlg.FixedTree;

                        // restore ShowTreeLines setting
                        SelectExercise(index, false, showLines);
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
            CreateNewArticle.CreateNewExercise();
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
            SortArticlesUtils.InvokeSortGamesDialog(AppState.ActiveChapter);
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
            //UiMnExerciseViewConfig.IsChecked = AppState.ActiveChapter != null && AppState.ActiveChapter.ShowSolutionsOnOpen;
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
        /// Finds games in the active chapter that would better 
        /// be placed in other chapters based on their ECO codes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnMatchGamesToChapterByECO_Click(object sender, RoutedEventArgs e)
        {
            int gamesMoved = SplitChapterUtils.DistributeGamesByECO(AppState.ActiveChapter);

            if (gamesMoved > 0)
            {
                BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgNumberGamesMoved + ": " + gamesMoved.ToString(), CommentBox.HintType.INFO);
            }
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
        /// Restarts training from the training starting position
        /// repeating the current training line.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnTrainFromBeginning_Click(object sender, RoutedEventArgs e)
        {
            TreeNode updatedNode = TrainingSession.BuildNextTrainingLine();
            if (updatedNode == null)
            {
                TrainingSession.BuildFirstTrainingLine();
            }
            ResetTrainingMode();
        }

        /// <summary>
        /// Restarts training from the next training line.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnTrainNextLine_Click(object sender, RoutedEventArgs e)
        {
            TreeNode updatedNode = TrainingSession.BuildNextTrainingLine();
            if (updatedNode != null)
            {
                UiTrainingView.RollbackToUserMove(updatedNode);
                AppState.ConfigureMenusForTraining();
            }
        }

        /// <summary>
        /// Restarts training from the the previous training line.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnTrainPreviousLine_Click(object sender, RoutedEventArgs e)
        {
            TreeNode updatedNode = TrainingSession.BuildPreviousTrainingLine();
            if (updatedNode != null)
            {
                UiTrainingView.RollbackToUserMove(updatedNode);
                AppState.ConfigureMenusForTraining();
            }
        }

        /// <summary>
        /// Restarts training on a random training line.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnTrainRandomLine(object sender, RoutedEventArgs e)
        {
            List<TreeNode> lstLine = TrainingSession.SelectRandomLine();

            if (lstLine != null)
            {
                ResetTrainingMode();
                UiTrainingView.BuildTrainingLineParas(lstLine);
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

                    EditOperation op = new EditOperation(EditOperation.EditType.REORDER_LINES, nd, lstChildrenOrder);

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
                            ActiveTreeView.BuildFlowDocumentForVariationTree(false);
                            ActiveTreeView.SelectLineAndMoveInWorkbookViews(nd.LineId, ActiveLine.GetSelectedPlyNodeIndex(false), false);
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
            ActiveTreeView?.PromoteCurrentLine();
        }

        /// <summary>
        /// The user requested from the Games menu to promote the currently selected line.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnGame_PromoteLine_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView?.PromoteCurrentLine();
        }

        /// <summary>
        /// The user requested that a null move be added.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnEnterNullMove_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView?.EnterNullMove();
        }

        /// <summary>
        /// The user requested that the current move be duplicated as a variation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDuplicateAsVariation_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView?.DuplicateAsVariation();
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
        private void UiMnMainCopy_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.IsTreeViewTabActive())
            {
                ActiveTreeView?.PlaceSelectedForCopyInClipboard();
                BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgCopiedMoves, CommentBox.HintType.INFO);
            }
            else if (AppState.ActiveTab == TabViewType.INTRO)
            {
                IntroViewCopy_Click(sender, e);
            }
        }

        /// <summary>
        /// Cuts the selected content moves 
        /// i.e. removes it from the clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnMainCut_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.IsTreeViewTabActive())
            {
                ActiveTreeView.PlaceSelectedForCopyInClipboard();
                if (ActiveTreeView.HasMovesSelectedForCopy)
                {
                    ActiveTreeView.DeleteRemainingMoves();
                    BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgCopiedMoves, CommentBox.HintType.INFO);
                }
            }
            else if (AppState.ActiveTab == TabViewType.INTRO)
            {
                IntroViewCut_Click(sender, e);
            }
        }

        /// <summary>
        /// Pastes content of the Clipboard in the view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnPaste_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.IsTreeViewTabActive())
            {
                CopyPasteMoves.PasteMoveList();
            }
            else if (AppState.ActiveTab == TabViewType.INTRO)
            {
                IntroViewPaste_Click(sender, e);
            }
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
                BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgCopiedMoves, CommentBox.HintType.INFO);
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
                BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgCopiedMoves, CommentBox.HintType.INFO);
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
            string searchPath = "";

            try
            {
                searchPath = Path.GetDirectoryName(Configuration.EngineExePath);
            }
            catch { }

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
                    _openingStatsView.RebuildView();
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
                    _openingStatsView.RebuildView();
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
                Configuration.DontSavePositionEvals = !mni.IsChecked;
                ActiveLine.ToggleDontSaveEvals();
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
                        DeleteArticlesUtils.DeleteModelGame(chapter.ActiveModelGameIndex);
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
                        DeleteArticlesUtils.DeleteExercise(chapter.ActiveExerciseIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("DeleteExercise()", ex);
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

                Article game = chapter.ModelGames[chapter.ActiveModelGameIndex];
                VariationTree tree = game.Tree;
                var dlg = new GameHeaderDialog(tree, Properties.Resources.GameHeader);
                GuiUtilities.PositionDialog(dlg, this, 100);
                dlg.ShowDialog();
                if (dlg.ExitOK)
                {
                    AppState.IsDirty = true;
                    ReferenceUtils.UpdateReferenceText(game.Guid);

                    _chaptersView.BuildFlowDocumentForChaptersView(false);
                    if (WorkbookManager.ActiveTab == TabViewType.MODEL_GAME)
                    {
                        _modelGameTreeView.BuildFlowDocumentForVariationTree(false);
                        _modelGameTreeView.RestoreSelectedLineAndNode();
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

                Article exercise = chapter.Exercises[chapter.ActiveExerciseIndex];
                VariationTree tree = exercise.Tree;
                var dlg = new GameHeaderDialog(tree, Properties.Resources.ExerciseHeader);
                GuiUtilities.PositionDialog(dlg, this, 100);
                dlg.ShowDialog();
                if (dlg.ExitOK)
                {
                    AppState.IsDirty = true;
                    ReferenceUtils.UpdateReferenceText(exercise.Guid);

                    _chaptersView.BuildFlowDocumentForChaptersView(false);
                    if (WorkbookManager.ActiveTab == TabViewType.EXERCISE)
                    {
                        _exerciseTreeView.BuildFlowDocumentForVariationTree(false);
                        _exerciseTreeView.RestoreSelectedLineAndNode();
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
                UiTbEngineLines.FontSize = Constants.BASE_ENGINE_LINES_FONT_SIZE + Configuration.FontSizeDiff;
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
                UiTbEngineLines.FontSize = Constants.BASE_ENGINE_LINES_FONT_SIZE + Configuration.FontSizeDiff;
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
        /// Writes out the content of the current view to an RTF file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnWriteRtf_Click(object sender, RoutedEventArgs e)
        {
            bool done = false;

            while (!done)
            {
                done = true;
                RtfExportDialog dlg = new RtfExportDialog(RtfExportDialog.ExportFormat.RTF);
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                if (dlg.ShowDialog() == true)
                {
                    WaitDialog waitDlg = null;
                    try
                    {
                        string filePath = RtfWriter.SelectTargetRtfFile();

                        if (!string.IsNullOrEmpty(filePath) && filePath[0] != '.')
                        {
                            Mouse.SetCursor(Cursors.Wait);
                            waitDlg = new WaitDialog(Properties.Resources.ExportToRtf);
                            GuiUtilities.PositionDialogInMiddle(waitDlg, this);
                            waitDlg.Show();
                            AppState.DoEvents();
                            done = RtfWriter.WriteRtf(filePath);
                            BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.OperationCompleted, CommentBox.HintType.INFO);
                        }
                    }
                    catch { }
                    finally
                    {
                        if (waitDlg != null)
                        {
                            waitDlg.Close();
                        }
                        Mouse.SetCursor(Cursors.Arrow);
                    }
                }
            }
        }

        /// <summary>
        /// Writes out the content of the current view to an PGN file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnWritePgn_Click(object sender, RoutedEventArgs e)
        {
            bool done = false;

            while (!done)
            {
                done = true;
                PgnExportDialog dlg = new PgnExportDialog();
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                if (dlg.ShowDialog() == true)
                {
                    WaitDialog waitDlg = null;
                    try
                    {
                        string filePath = "";

                        // SelectTargetPgnFile() will return null if user chose an invalid file
                        // and "" if user cancelled.
                        // So if it is null we give them another chance, hence the loop
                        while ((filePath = PgnWriter.SelectTargetPgnFile()) == null)
                        { }

                        if (!string.IsNullOrEmpty(filePath) && filePath[0] != '.')
                        {
                            Mouse.SetCursor(Cursors.Wait);
                            waitDlg = new WaitDialog(Properties.Resources.ExportToPgn);
                            GuiUtilities.PositionDialogInMiddle(waitDlg, this);
                            waitDlg.Show();
                            AppState.DoEvents();
                            done = PgnWriter.WritePgn(filePath);
                            BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.OperationCompleted, CommentBox.HintType.INFO);
                        }
                    }
                    catch { }
                    finally
                    {
                        if (waitDlg != null)
                        {
                            waitDlg.Close();
                        }
                        Mouse.SetCursor(Cursors.Arrow);
                    }
                }
            }
        }

        /// <summary>
        /// Writes out the content of the current view to a text file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnWriteText_Click(object sender, RoutedEventArgs e)
        {
            bool done = false;

            while (!done)
            {
                done = true;
                RtfExportDialog dlg = new RtfExportDialog(RtfExportDialog.ExportFormat.TEXT);
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                if (dlg.ShowDialog() == true)
                {
                    WaitDialog waitDlg = null;
                    try
                    {
                        string filePath = TextWriter.SelectTargetTextFile();

                        if (!string.IsNullOrEmpty(filePath) && filePath[0] != '.')
                        {
                            Mouse.SetCursor(Cursors.Wait);
                            waitDlg = new WaitDialog(Properties.Resources.ExportToText);
                            GuiUtilities.PositionDialogInMiddle(waitDlg, this);
                            waitDlg.Show();
                            AppState.DoEvents();
                            done = TextWriter.WriteText(filePath);
                            BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.OperationCompleted, CommentBox.HintType.INFO);
                        }
                    }
                    catch { }
                    finally
                    {
                        if (waitDlg != null)
                        {
                            waitDlg.Close();
                        }
                        Mouse.SetCursor(Cursors.Arrow);
                    }
                }
            }
        }
    }
}

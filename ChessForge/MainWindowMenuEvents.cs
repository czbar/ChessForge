﻿using ChessPosition;
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
using static ChessForge.WorkbookOperation;
using ChessPosition.GameTree;
using ChessForge;
using System.Windows.Documents;
using System.Linq;

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
            _studyTreeView = new VariationTreeView(UiRtbStudyTreeView.Document, this, GameData.ContentType.STUDY_TREE, -1);

            // ask for the options
            if (!ShowWorkbookOptionsDialog(false))
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
            SetActiveLine(startLineId, startingNode);

            return true;
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
            bool proceed = true;

            if (WorkbookManager.SessionWorkbook != null && AppState.IsDirty)
            {
                proceed = WorkbookManager.PromptAndSaveWorkbook(false, out _);
                if (proceed)
                {
                    WorkbookManager.SessionWorkbook.GamesManager.CancelAll();
                }
            }

            if (proceed && ChangeAppModeWarning(LearningMode.Mode.MANUAL_REVIEW))
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
            bool proceed = true;

            if (WorkbookManager.SessionWorkbook != null && AppState.IsDirty)
            {
                proceed = WorkbookManager.PromptAndSaveWorkbook(false, out _);
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
        /// The users requesting merging of chapters.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnMergeChapters_Click(object sender, RoutedEventArgs e)
        {
            SelectChaptersDialog dlg = new SelectChaptersDialog(WorkbookManager.SessionWorkbook, Properties.Resources.SelectChaptersToMerge)
            {
                Left = AppState.MainWin.ChessForgeMain.Left + 100,
                Top = AppState.MainWin.ChessForgeMain.Top + 100,
                Topmost = false,
                Owner = AppState.MainWin
            };

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
                            merged = WorkbookTreeMerge.MergeVariationTrees(merged, ch.Chapter.StudyTree.Tree);
                        }
                        mergedCount++;
                    }
                }

                if (mergedCount > 1 && _chaptersView != null)
                {
                    Chapter mergedChapter = WorkbookManager.SessionWorkbook.CreateNewChapter(merged);
                    mergedChapter.SetTitle(title);
                    CopyGamesToChapter(mergedChapter, dlg.ChapterList);
                    CopyExercisesToChapter(mergedChapter, dlg.ChapterList);
                    DeleteChapters(dlg.ChapterList);

                    _chaptersView.BuildFlowDocumentForChaptersView();
                    AppState.DoEvents();
                    _chaptersView.BringChapterIntoView(WorkbookManager.SessionWorkbook.GetChapterCount() - 1);
                }

                AppState.IsDirty = mergedCount > 1;
            }
        }

        /// <summary>
        /// Copies all games from the selected chapters in the list to the target chapter
        /// </summary>
        /// <param name="target"></param>
        /// <param name="sources"></param>
        private void CopyGamesToChapter(Chapter target, ObservableCollection<SelectedChapter> sources)
        {
            foreach (SelectedChapter ch in sources)
            {
                if (ch.IsSelected)
                {
                    foreach (Article game in ch.Chapter.ModelGames)
                    {
                        target.AddModelGame(game.Tree);
                    }
                }
            }
        }

        /// <summary>
        /// Copies all exercises from the selected chapters in the list to the target chapter
        /// </summary>
        /// <param name="target"></param>
        /// <param name="sources"></param>
        private void CopyExercisesToChapter(Chapter target, ObservableCollection<SelectedChapter> sources)
        {
            foreach (SelectedChapter ch in sources)
            {
                if (ch.IsSelected)
                {
                    foreach (Article item in ch.Chapter.Exercises)
                    {
                        target.AddExercise(item.Tree);
                    }
                }
            }
        }

        /// <summary>
        /// Deletes all selected chapters.
        /// </summary>
        /// <param name="sources"></param>
        private void DeleteChapters(ObservableCollection<SelectedChapter> chapters)
        {
            foreach (SelectedChapter ch in chapters)
            {
                if (ch.IsSelected)
                {
                    WorkbookManager.SessionWorkbook.DeleteChapter(ch.Chapter);
                }
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

                if (AppState.ActiveTab == WorkbookManager.TabViewType.CHAPTERS || AppState.ActiveVariationTree == null)
                {
                    UndoWorkbookOperation();
                }
                else if (AppState.ActiveTab == WorkbookManager.TabViewType.STUDY
                     || AppState.ActiveTab == WorkbookManager.TabViewType.MODEL_GAME
                     || AppState.ActiveTab == WorkbookManager.TabViewType.EXERCISE)
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
            catch {}
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
            if (!string.IsNullOrEmpty(selectedLineId))
            {
                AppState.MainWin.ActiveTreeView.SelectLineAndMove(selectedLineId, selectedNodeId);
            }

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
                WorkbookManager.SessionWorkbook.OpsManager.Undo(out WorkbookOperation.WorkbookOperationType opType, out int selectedChapterIndex, out int selectedArticleIndex);
                switch (opType)
                {
                    case WorkbookOperation.WorkbookOperationType.RENAME_CHAPTER:
                        AppState.MainWin.ActiveTreeView?.BuildFlowDocumentForVariationTree();
                        _chaptersView.BuildFlowDocumentForChaptersView();
                        break;
                    case WorkbookOperation.WorkbookOperationType.DELETE_CHAPTER:
                    case WorkbookOperation.WorkbookOperationType.CREATE_CHAPTER:
                        _chaptersView.BuildFlowDocumentForChaptersView();
                        if (AppState.ActiveTab != WorkbookManager.TabViewType.CHAPTERS)
                        {
                            UiTabChapters.Focus();
                        }
                        AppState.DoEvents();
                        _chaptersView.BringChapterIntoViewByIndex(selectedChapterIndex);
                        break;
                    case WorkbookOperation.WorkbookOperationType.CREATE_ARTICLE:
                        if (AppState.ActiveTab == WorkbookManager.TabViewType.CHAPTERS)
                        {
                            _chaptersView.BuildFlowDocumentForChaptersView();
                        }
                        else
                        {
                            _chaptersView.IsDirty = true;
                            SelectModelGame(selectedArticleIndex, true);
                        }
                        break;
                    case WorkbookOperation.WorkbookOperationType.DELETE_MODEL_GAME:
                    case WorkbookOperation.WorkbookOperationType.DELETE_MODEL_GAMES:
                        _chaptersView.BuildFlowDocumentForChaptersView();
                        SelectModelGame(selectedArticleIndex, AppState.ActiveTab != WorkbookManager.TabViewType.CHAPTERS);
                        break;
                    case WorkbookOperation.WorkbookOperationType.DELETE_EXERCISE:
                    case WorkbookOperation.WorkbookOperationType.DELETE_EXERCISES:
                        _chaptersView.BuildFlowDocumentForChaptersView();
                        SelectExercise(selectedArticleIndex, AppState.ActiveTab != WorkbookManager.TabViewType.CHAPTERS);
                        break;
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UndoWorkbookOperation()", ex);
            }

            AppState.IsDirty = true;
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
                && AppState.IsDirty || (ActiveVariationTree != null && ActiveVariationTree.HasTrainingMoves()))
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
            BackupVersionDialog dlg = new BackupVersionDialog(WorkbookManager.SessionWorkbook)
            {
                Left = ChessForgeMain.Left + 100,
                Top = ChessForgeMain.Top + 100,
                Topmost = false,
                Owner = this
            };
            if (dlg.ShowDialog() == true)
            {
                AppState.SaveWorkbookFile(dlg.BackupPath);
                WorkbookManager.SessionWorkbook.SetVersion(dlg.IncrementedVersion);
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

            // we will start with the first move of the active line
            if (EngineMessageProcessor.IsEngineAvailable)
            {
                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.LINE, EvaluationManager.LineSource.ACTIVE_LINE);

                int idx = ActiveLine.GetSelectedPlyNodeIndex(true);
                TreeNode nd = ActiveLine.GetSelectedTreeNode();
                EvaluationManager.SetStartNodeIndex(idx > 0 ? idx : 1);

                UiDgActiveLine.SelectedCells.Clear();


                EngineMessageProcessor.RequestMoveEvaluation(idx, nd, ActiveVariationTreeId);
            }
            else
            {
                MessageBox.Show(Properties.Resources.EngineNotAvailable, Properties.Resources.EvaluationError, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Finds the list of positions identical to the currently selected node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiMnFindIdenticalPosition_Click(object sender, RoutedEventArgs e)
        {
            bool isTrainingOrSolving = TrainingSession.IsTrainingInProgress || AppState.IsUserSolving();

            if (isTrainingOrSolving || ActiveVariationTree == null || AppState.ActiveTab == WorkbookManager.TabViewType.CHAPTERS)
            {
                return;
            }

            try
            {
                TreeNode nd = ActiveLine.GetSelectedTreeNode();
                FindIdenticalPositions.Search(nd, FindIdenticalPositions.Mode.FIND_AND_REPORT);
            }
            catch
            {
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
                MessageBox.Show(Properties.Resources.EngineNotAvailable, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(Properties.Resources.SelectEngineStartMove, Properties.Resources.EngineGame, MessageBoxButton.OK);
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
            WorkbookManager.EnableChaptersContextMenuItems(_cmChapters, WorkbookManager.LastClickedChapterIndex >= 0, GameData.ContentType.GENERIC);
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
        private void ExpandCollapseChaptersView(bool expand, bool all)
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
                WorkbookOperation op = new WorkbookOperation(WorkbookOperation.WorkbookOperationType.RENAME_CHAPTER, chapter, prevTitle);
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
                    SelectChaptersDialog dlg = new SelectChaptersDialog(workbook)
                    {
                        Left = AppState.MainWin.ChessForgeMain.Left + 100,
                        Top = AppState.MainWin.ChessForgeMain.Top + 100,
                        Topmost = false,
                        Owner = AppState.MainWin
                    };

                    dlg.ShowDialog();
                    if (dlg.ExitOK)
                    {
                        foreach (SelectedChapter ch in dlg.ChapterList)
                        {
                            if (ch.IsSelected)
                            {
                                WorkbookManager.SessionWorkbook.Chapters.Add(ch.Chapter);
                                if (_chaptersView != null)
                                {
                                    _chaptersView.BuildFlowDocumentForChaptersView();
                                    AppState.DoEvents();
                                    _chaptersView.BringChapterIntoView(WorkbookManager.SessionWorkbook.GetChapterCount() - 1);
                                }
                                AppState.IsDirty = true;
                            }
                        }
                    }
                }
                else
                {
                    CreateChapterFromNewGames(gamesCount, ref games, fileName);
                }
            }
        }

        /// <summary>
        /// Invokes the dialog to select Games to delete and deletes them. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDeleteGames_Click(object sender, RoutedEventArgs e)
        {
            DeleteArticles(AppState.ActiveChapter, GameData.ContentType.MODEL_GAME);
        }

        /// <summary>
        /// Invokes the dialog to select Exercises to delete and deletes them. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnDeleteExercises_Click(object sender, RoutedEventArgs e)
        {
            DeleteArticles(AppState.ActiveChapter, GameData.ContentType.EXERCISE);
        }

        /// <summary>
        /// Deletes articles passed in the artcole list
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="articleType"></param>
        private void DeleteArticles(Chapter chapter, GameData.ContentType articleType)
        {
            if (chapter == null)
            {
                return;
            }

            try
            {
                ObservableCollection<ArticleListItem> articleList = WorkbookManager.SessionWorkbook.GenerateArticleList(AppState.ActiveChapter, articleType);
                SelectArticlesDialog dlg = new SelectArticlesDialog(null, ref articleList, articleType)
                {
                    Left = ChessForgeMain.Left + 100,
                    Top = ChessForgeMain.Top + 100,
                    Topmost = false,
                    Owner = this
                };
                if (dlg.ShowDialog() == true)
                {
                    List<Article> articlesToDelete = new List<Article>();
                    List<int> indicesToDelete = new List<int>();
                    int index = 0;
                    foreach (ArticleListItem item in articleList)
                    {
                        if (item.IsSelected)
                        {
                            articlesToDelete.Add(item.Article);
                            indicesToDelete.Add(index);
                        }
                        index++;
                    }

                    List<Article> deletedArticles = new List<Article>();
                    List<int> deletedIndices = new List<int>();
                    for (int i = 0; i < articlesToDelete.Count; i++)
                    {
                        bool res = chapter.DeleteArticle(articlesToDelete[i]);
                        if (res)
                        {
                            deletedArticles.Add(articlesToDelete[i]);
                            deletedIndices.Add(indicesToDelete[i]);
                        }
                    }

                    if (deletedArticles.Count > 0)
                    {
                        WorkbookOperation.WorkbookOperationType wot =
                            articleType == GameData.ContentType.MODEL_GAME ? WorkbookOperationType.DELETE_MODEL_GAMES : WorkbookOperationType.DELETE_EXERCISES;
                        int activeArticleIndex = articleType == GameData.ContentType.MODEL_GAME ? chapter.ActiveModelGameIndex : chapter.ActiveExerciseIndex;
                        WorkbookOperation op = new WorkbookOperation(wot, chapter, activeArticleIndex, deletedArticles, deletedIndices);
                        WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
                        AppState.IsDirty = true;
                        _chaptersView.RebuildChapterParagraph(chapter);
                    }
                }
            }
            catch { }
        }

        public void FocusOnChapterView()
        {
            UiTabChapters.Focus();
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

                if (gamesCount > 0)
                {
                    if (SelectArticlesFromPgnFile(ref games, SelectGamesDialog.Mode.IMPORT_INTO_NEW_CHAPTER, out bool createStudy, out bool copyGames, out _))
                    {
                        if (createStudy)
                        {
                            WorkbookManager.MergeGames(ref chapter.StudyTree.Tree, ref games);
                        }
                        // content type may have been reset to GENERIC in MergeGames above
                        chapter.StudyTree.Tree.ContentType = GameData.ContentType.STUDY_TREE;

                        CopySelectedItemsToChapter(chapter, copyGames, out string error, games, out _);

                        _chaptersView.BuildFlowDocumentForChaptersView();
                        SelectChapterByIndex(chapter.Index, false);
                        AppState.DoEvents();
                        _chaptersView.BringChapterIntoView(chapter.Index);
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
                    if (gd.GetContentType() == GameData.ContentType.EXERCISE)
                    {
                        if (chapter.AddArticle(gd, GameData.ContentType.EXERCISE, out error, GameData.ContentType.EXERCISE) >= 0)
                        {
                            copiedCount++;
                            copiedExercises++;
                        }
                        chapter.StudyTree.Tree.ContentType = GameData.ContentType.STUDY_TREE;
                    }
                    else if (copyGames && (gd.GetContentType() == GameData.ContentType.GENERIC || gd.GetContentType() == GameData.ContentType.MODEL_GAME))
                    {
                        if (chapter.AddArticle(gd, GameData.ContentType.MODEL_GAME, out error, GameData.ContentType.MODEL_GAME) >= 0)
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
        public bool SelectArticlesFromPgnFile(ref ObservableCollection<GameData> games, SelectGamesDialog.Mode mode, out bool createStudy, out bool copyGames, out bool multiChapter)
        {
            SelectGamesDialog dlg = new SelectGamesDialog(ref games, mode)
            {
                Left = AppState.MainWin.ChessForgeMain.Left + 100,
                Top = AppState.MainWin.ChessForgeMain.Top + 100,
                Topmost = false,
                Owner = AppState.MainWin
            };

            bool res = dlg.ShowDialog() == true;

            createStudy = dlg.CreateStudy;
            copyGames = dlg.CopyGames;
            multiChapter = dlg.MultiChapter;

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
            int index = WorkbookManager.SessionWorkbook.ActiveChapterIndex;
            if (index > 0)
            {
                Chapter hold = WorkbookManager.SessionWorkbook.Chapters[index];
                WorkbookManager.SessionWorkbook.Chapters[index] = WorkbookManager.SessionWorkbook.Chapters[index - 1];
                WorkbookManager.SessionWorkbook.Chapters[index - 1] = hold;
                _chaptersView.BuildFlowDocumentForChaptersView();
                SelectChapterByIndex(index - 1, false, false);
                PulseManager.ChaperIndexToBringIntoView = index - 1;
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
            int index = WorkbookManager.SessionWorkbook.ActiveChapterIndex;
            if (index >= 0 && index < WorkbookManager.SessionWorkbook.Chapters.Count - 1)
            {
                Chapter hold = WorkbookManager.SessionWorkbook.Chapters[index];
                WorkbookManager.SessionWorkbook.Chapters[index] = WorkbookManager.SessionWorkbook.Chapters[index + 1];
                WorkbookManager.SessionWorkbook.Chapters[index + 1] = hold;
                _chaptersView.BuildFlowDocumentForChaptersView();
                SelectChapterByIndex(index + 1, false, false);
                PulseManager.ChaperIndexToBringIntoView = index + 1;
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
                //int index = WorkbookManager.LastClickedModelGameIndex;
                int gameCount = chapter.GetModelGameCount();

                if (index > 0 && index < gameCount)
                {
                    Article hold = chapter.ModelGames[index];
                    chapter.ModelGames[index] = chapter.ModelGames[index - 1];
                    chapter.ModelGames[index - 1] = hold;
                    chapter.ActiveModelGameIndex = index - 1;

                    _chaptersView.BuildFlowDocumentForChaptersView();
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
                //                int index = WorkbookManager.LastClickedExerciseIndex;
                int exerciseCount = chapter.GetExerciseCount();

                if (index > 0 && index < exerciseCount)
                {
                    Article hold = chapter.Exercises[index];
                    chapter.Exercises[index] = chapter.Exercises[index - 1];
                    chapter.Exercises[index - 1] = hold;
                    chapter.ActiveExerciseIndex = index - 1;

                    _chaptersView.BuildFlowDocumentForChaptersView();
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

                    _chaptersView.BuildFlowDocumentForChaptersView();
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
                //int index = WorkbookManager.LastClickedExerciseIndex;
                int exerciseCount = chapter.GetExerciseCount();

                if (index >= 0 && index < exerciseCount - 1)
                {
                    Article hold = chapter.Exercises[index];
                    chapter.Exercises[index] = chapter.Exercises[index + 1];
                    chapter.Exercises[index + 1] = hold;
                    chapter.ActiveExerciseIndex = index + 1;

                    _chaptersView.BuildFlowDocumentForChaptersView();
                    AppState.IsDirty = true;
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("UiMnExerciseDown_Click()", ex);
            }
        }

        /// <summary>
        /// Invokes the InvokeSelectSingleChapter dialog
        /// and returns the selected index.
        /// </summary>
        /// <returns></returns>
        private int InvokeSelectSingleChapterDialog()
        {
            try
            {
                SelectSingleChapterDialog dlg = new SelectSingleChapterDialog()
                {
                    Left = ChessForgeMain.Left + 100,
                    Top = ChessForgeMain.Top + 100,
                    Topmost = false,
                    Owner = this
                };
                dlg.ShowDialog();

                return dlg.ExitOk ? dlg.SelectedIndex : -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Lets the user select a chapter to move the curently selected game to.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnMoveGameToChapter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chapter activeChapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                int selectedChapterIndex = MoveGameBetweenChapters();

                if (selectedChapterIndex >= 0)
                {
                    Chapter targetChapter = WorkbookManager.SessionWorkbook.Chapters[selectedChapterIndex];
                    activeChapter.CorrectActiveModelGameIndex();

                    switch (AppState.ActiveTab)
                    {
                        case WorkbookManager.TabViewType.CHAPTERS:
                            WorkbookManager.SessionWorkbook.ActiveChapter = targetChapter;
                            targetChapter.ActiveModelGameIndex = targetChapter.GetModelGameCount() - 1;
                            _chaptersView.BuildFlowDocumentForChaptersView();

                            AppState.DoEvents();
                            _chaptersView.BringChapterIntoViewByIndex(selectedChapterIndex);
                            break;
                        case WorkbookManager.TabViewType.MODEL_GAME:
                            if (activeChapter.ActiveModelGameIndex < 0)
                            {
                                DisplayPosition(PositionUtils.SetupStartingPosition());
                            }
                            SelectModelGame(activeChapter.ActiveModelGameIndex, false);
                            _chaptersView.BuildFlowDocumentForChaptersView();
                            break;
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Moves a game between chapters after invoking a dialog
        /// to select the target chapter
        /// </summary>
        /// <returns></returns>
        private int MoveGameBetweenChapters()
        {
            int selectedChapterIndex = -1;

            try
            {
                selectedChapterIndex = InvokeSelectSingleChapterDialog();

                if (selectedChapterIndex >= 0)
                {
                    Chapter activeChapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                    int activeChapterIndex = WorkbookManager.SessionWorkbook.ActiveChapterIndex;
                    int gameIndex = activeChapter.ActiveModelGameIndex;
                    if (selectedChapterIndex >= 0 && selectedChapterIndex != activeChapterIndex)
                    {
                        Chapter targetChapter = WorkbookManager.SessionWorkbook.Chapters[selectedChapterIndex];

                        Article game = activeChapter.GetModelGameAtIndex(gameIndex);
                        targetChapter.ModelGames.Add(game);
                        activeChapter.ModelGames.Remove(game);

                        targetChapter.IsModelGamesListExpanded = true;

                        AppState.IsDirty = true;
                    }
                }
            }
            catch
            {
            }

            return selectedChapterIndex;
        }

        /// <summary>
        /// Lets the user select a chapter to move the curently selected exercise to.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnMoveExerciseToChapter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int selectedChapterIndex = InvokeSelectSingleChapterDialog();

                if (selectedChapterIndex >= 0)
                {
                    Chapter activeChapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                    int activeChapterIndex = WorkbookManager.SessionWorkbook.ActiveChapterIndex;
                    int exerciseIndex = activeChapter.ActiveExerciseIndex;
                    if (selectedChapterIndex >= 0 && selectedChapterIndex != activeChapterIndex)
                    {
                        Chapter targetChapter = WorkbookManager.SessionWorkbook.Chapters[selectedChapterIndex];

                        Article exercise = activeChapter.GetExerciseAtIndex(exerciseIndex);
                        targetChapter.Exercises.Add(exercise);
                        activeChapter.Exercises.Remove(exercise);

                        targetChapter.IsExercisesListExpanded = true;
                        WorkbookManager.SessionWorkbook.ActiveChapter = targetChapter;
                        targetChapter.ActiveExerciseIndex = targetChapter.GetExerciseCount() - 1;

                        AppState.IsDirty = true;
                        _chaptersView.BuildFlowDocumentForChaptersView();
                        AppState.DoEvents();
                        _chaptersView.BringChapterIntoViewByIndex(selectedChapterIndex);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Requests import of Model Games from a PGN file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnImportModelGames_Click(object sender, RoutedEventArgs e)
        {
            ImportGamesFromPgn(GameData.ContentType.GENERIC, GameData.ContentType.MODEL_GAME);
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
            ImportGamesFromPgn(GameData.ContentType.EXERCISE, GameData.ContentType.EXERCISE);
        }

        /// <summary>
        /// Imports Model Games or Exercises from a PGN file.
        /// </summary>
        /// <param name="contentType"></param>
        private int ImportGamesFromPgn(GameData.ContentType contentType, GameData.ContentType targetcontentType)
        {
            int gameCount = 0;
            int skippedDueToType = 0;
            int firstImportedGameIndex = -1;
            if ((contentType == GameData.ContentType.GENERIC || contentType == GameData.ContentType.MODEL_GAME || contentType == GameData.ContentType.EXERCISE)
                && WorkbookManager.SessionWorkbook.ActiveChapter != null)
            {
                string fileName = SelectPgnFile();
                if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                    ObservableCollection<GameData> games = new ObservableCollection<GameData>();
                    gameCount = WorkbookManager.ReadPgnFile(fileName, ref games, contentType, targetcontentType);

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
                                            int index = chapter.AddArticle(games[i], contentType, out string error, targetcontentType);
                                            if (index < 0)
                                            {
                                                if (string.IsNullOrEmpty(error))
                                                {
                                                    skippedDueToType++;
                                                }
                                            }
                                            else if (firstImportedGameIndex < 0)
                                            {
                                                firstImportedGameIndex = index;
                                            }
                                            AppState.IsDirty = true;
                                            if (!string.IsNullOrEmpty(error))
                                            {
                                                errorCount++;
                                                sbErrors.Append(GuiUtilities.BuildGameProcessingErrorText(games[i], i + 1, error));
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            errorCount++;
                                            sbErrors.Append(GuiUtilities.BuildGameProcessingErrorText(games[i], i + 1, ex.Message));
                                        }
                                    }
                                }
                                RefreshChaptersViewAfterImport(targetcontentType, chapter, firstImportedGameIndex);
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

                    if (errorCount > 0 || skippedDueToType > 0)
                    {
                        if (skippedDueToType > 0)
                        {
                            string invalidEntities = Properties.Resources.WrongTypeEntitiesNotImported + ", ";
                            invalidEntities += (Properties.Resources.Count + " " + skippedDueToType.ToString() + ".");
                            sbErrors.AppendLine(invalidEntities);
                        }
                        TextBoxDialog tbDlg = new TextBoxDialog(Properties.Resources.PgnErrors, sbErrors.ToString())
                        {
                            Left = ChessForgeMain.Left + 100,
                            Top = ChessForgeMain.Top + 100,
                            Topmost = false,
                            Owner = this
                        };
                        tbDlg.ShowDialog();
                    }
                }
            }
            return gameCount;
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
                SelectArticlesDialog dlg = new SelectArticlesDialog(nd, ref articleList)
                {
                    Left = ChessForgeMain.Left + 100,
                    Top = ChessForgeMain.Top + 100,
                    Topmost = false,
                    Owner = this
                };
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
                    tree.MoveNumberOffset = moveNumberOffset;

                    Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                    CopyHeaderFromGame(tree, ActiveVariationTree.Header, false);
                    if (ActiveVariationTree.Header.GetContentType(out _) == GameData.ContentType.STUDY_TREE)
                    {
                        tree.Header.SetHeaderValue(PgnHeaders.KEY_WHITE, chapter.Title);
                        tree.Header.SetHeaderValue(PgnHeaders.KEY_BLACK, Properties.Resources.StudyTreeAfter + " " + MoveUtils.BuildSingleMoveText(nd, true, true));
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
            tree.Header.SetHeaderValue(PgnHeaders.KEY_RESULT, header.GetResult(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_EVENT, header.GetEventName(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_ECO, header.GetECO(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_LICHESS_ID, header.GetLichessId(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_CHESSCOM_ID, header.GetChessComId(out _));
            if (overrideGuid)
            {
                tree.Header.SetHeaderValue(PgnHeaders.KEY_GUID, header.GetGuid(out _));
            }
            tree.Header.SetHeaderValue(PgnHeaders.KEY_DATE, header.GetDate(out _));
            tree.Header.SetHeaderValue(PgnHeaders.KEY_ROUND, header.GetRound(out _));

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
                    PositionSetupDialog dlg = new PositionSetupDialog(tree)
                    {
                        Left = ChessForgeMain.Left + 100,
                        Top = ChessForgeMain.Top + 100,
                        Topmost = false,
                        Owner = this
                    };
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

            SelectGamesDialog dlg = new SelectGamesDialog(ref games, mode)
            {
                Left = ChessForgeMain.Left + 100,
                Top = ChessForgeMain.Top + 100,
                Topmost = false,
                Owner = AppState.MainWin
            };
            return dlg.ShowDialog() == true;
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


#if false

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
#endif


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
        private void UiMnciCopyFen_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView.CopyFenToClipboard();
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
                        BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.BookmarkAdded, System.Windows.Media.Brushes.Green);
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


        //**********************
        //
        //  TRAINING
        // 
        //**********************

        /// <summary>
        /// Strips all the comments from the currently shown tree. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiStripComments_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveTreeView != null && AppState.IsTreeViewTabActive())
            {
                if (MessageBox.Show(Properties.Resources.MsgConfirmStripComments, Properties.Resources.Confirm,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    VariationTree tree = ActiveTreeView.ShownVariationTree;

                    // get data for the Undo operation first
                    List<NagsAndComment> comments = TreeUtils.BuildNagsAndCommentsList(tree);
                    EditOperation op = new EditOperation(EditOperation.EditType.STRIP_COMMENTS, comments, null);
                    tree.OpsManager.PushOperation(op);

                    tree.StripCommentsAndNags();
                    AppState.IsDirty = true;

                    ActiveTreeView.BuildFlowDocumentForVariationTree();
                }
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
        /// Opens the dialog for selecting and evaluating games
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnEvaluateGames_Click(object sender, RoutedEventArgs e)
        {

            if (TrainingSession.IsTrainingInProgress)
            {
                GuiUtilities.ShowExitTrainingInfoMessage();
            }
            else
            {
                try
                {
                    if (AppState.ActiveChapter != null)
                    {
                        int chapterIndex = WorkbookManager.SessionWorkbook.GetChapterIndex(AppState.ActiveChapter);
                        SelectGamesForEvalDialog dlg = new SelectGamesForEvalDialog(AppState.ActiveChapter, chapterIndex, AppState.ActiveChapter.ModelGames)
                        {
                            Left = ChessForgeMain.Left + 100,
                            Top = ChessForgeMain.Top + 100,
                            Topmost = false,
                            Owner = AppState.MainWin
                        };
                        dlg.ShowDialog();
                    }
                }
                catch
                {
                }
            }

            e.Handled = true;
        }

        /// <summary>
        /// A request from the menu to start training at the currently selected position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnStartTrainingHere_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveVariationTree == null)
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
            }
            else
            {
                MessageBox.Show(Properties.Resources.NoTrainingStartMove, Properties.Resources.Training, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Exits the Training session, if confirmed by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnStopTraining_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.ExitTrainingSession, Properties.Resources.Training, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    AppLog.Message("Stopping Training Session");
                    EngineMessageProcessor.ResetEngineEvaluation();

                    UiTrainingView.CleanupVariationTree();
                    if (WorkbookManager.PromptAndSaveWorkbook(false, out bool saved))
                    {
                        EngineMessageProcessor.StopEngineEvaluation();
                        EvaluationManager.Reset();

                        TrainingSession.IsTrainingInProgress = false;
                        TrainingSession.IsContinuousEvaluation = false;
                        MainChessBoard.RemoveMoveSquareColors();
                        LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);
                        if (ActiveVariationTree.ContentType == GameData.ContentType.EXERCISE)
                        {
                            _exerciseTreeView.DeactivateSolvingMode(VariationTree.SolvingMode.NONE);
                        }
                        AppState.SetupGuiForCurrentStates();

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
                        AppState.SwapCommentBoxForEngineLines(false);
                        BoardCommentBox.RestoreTitleMessage();
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Message("UiMnStopTraining_Click()", ex);
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
            BookmarkManager.SetActiveEntities(false);
            SetAppInTrainingMode(BookmarkManager.SelectedBookmarkNode);
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
            if (AppState.ActiveTab == WorkbookManager.TabViewType.TRAINING)
            {
                return;
            }
        }


        /// <summary>
        /// TODO: gradually replace all Got/LostFocus with IsVisibleChanged.
        /// Sets ActiveTab to Training when Training Tab becomes visible.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabTrainingProgress_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool visible = (bool)e.NewValue;
            if (visible == true)
            {
                WorkbookManager.ActiveTab = WorkbookManager.TabViewType.TRAINING;
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
        //  TREE OPERATIONS
        // 
        //**********************

        /// <summary>
        /// Checks the type of the clipboard content and undertakes appropriate action.
        /// </summary>
        public void PasteChfClipboard()
        {
            try
            {
                if (ChfClipboard.Type == ChfClipboard.ItemType.NODE_LIST)
                {
                    List<TreeNode> lstNodes = ChfClipboard.Value as List<TreeNode>;
                    if (lstNodes.Count > 0 && AppState.IsVariationTreeTabType)
                    {
                        List<TreeNode> insertedNewNodes = new List<TreeNode>();
                        List<TreeNode> failedInsertions = new List<TreeNode>();
                        TreeNode firstInserted = ActiveTreeView.InsertSubtree(lstNodes, ref insertedNewNodes, ref failedInsertions);
                        if (failedInsertions.Count == 0)
                        {
                            // if we inserted an already existing line, do nothing
                            if (insertedNewNodes.Count > 0)
                            {
                                ActiveVariationTree.BuildLines();
                                ActiveTreeView.BuildFlowDocumentForVariationTree();
                                TreeNode insertedRoot = ActiveVariationTree.GetNodeFromNodeId(firstInserted.NodeId);
                                SetActiveLine(insertedRoot.LineId, insertedRoot.NodeId);
                                ActiveTreeView.SelectNode(firstInserted.NodeId);
                            }
                        }
                        else
                        {
                            if (insertedNewNodes.Count > 0)
                            {
                                // remove inserted nodes after first removing the inserted root from the parent's children list.
                                insertedNewNodes[0].Parent.Children.Remove(insertedNewNodes[0]);
                                foreach (TreeNode node in insertedNewNodes)
                                {
                                    ActiveVariationTree.Nodes.Remove(node);
                                }
                            }

                            string msg = Properties.Resources.ErrClipboardLinePaste + " ("
                                + MoveUtils.BuildSingleMoveText(failedInsertions[0], true, false) + ")";
                            MessageBox.Show(msg, Properties.Resources.ClipboardOperation, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("PasteChfClipboard()", ex);
            }
        }

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
                if (ImportGamesFromPgn(GameData.ContentType.GENERIC, GameData.ContentType.MODEL_GAME) > 0)
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
                if (ImportGamesFromPgn(GameData.ContentType.EXERCISE, GameData.ContentType.EXERCISE) > 0)
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
        /// Copies FEN of the selected move to Clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnExerc_CopyFen_Click(object sender, RoutedEventArgs e)
        {
            ActiveTreeView.CopyFenToClipboard();
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
            }
        }

        /// <summary>
        /// Pastes moves from the Clipboard in the view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnPasteMoves_Click(object sender, RoutedEventArgs e)
        {
            PasteChfClipboard();
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
            if (AppState.CurrentLearningMode != LearningMode.Mode.IDLE)
            {
                if (ShowWorkbookOptionsDialog(false))
                {
                    AppState.IsDirty = true;
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
            ShowApplicationOptionsDialog();
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
                case WorkbookManager.TabViewType.STUDY:
                    SetCustomBoardOrientation((isFlipped ? PieceColor.Black : PieceColor.White), WorkbookManager.ItemType.STUDY);
                    break;
                case WorkbookManager.TabViewType.MODEL_GAME:
                    SetCustomBoardOrientation((isFlipped ? PieceColor.Black : PieceColor.White), WorkbookManager.ItemType.MODEL_GAME);
                    break;
                case WorkbookManager.TabViewType.EXERCISE:
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
            if (_chaptersView != null && AppState.ActiveTab == WorkbookManager.TabViewType.CHAPTERS)
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
            if (_chaptersView != null && AppState.ActiveTab == WorkbookManager.TabViewType.CHAPTERS)
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
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki");
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
        private void CreateNewModelGame()
        {
            try
            {
                VariationTree tree = new VariationTree(GameData.ContentType.MODEL_GAME);
                GameHeaderDialog dlg = new GameHeaderDialog(tree, Properties.Resources.ResourceManager.GetString("GameHeader"))
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

                    // TODO: is this spurious? RefreshGamesView() calls SelectModelGame too
                    SelectModelGame(WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex, true);
                    RefreshGamesView(out Chapter chapter, out int articleIndex);
                    WorkbookLocationNavigator.SaveNewLocation(chapter, GameData.ContentType.MODEL_GAME, articleIndex);
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
                        if (WorkbookManager.ActiveTab == WorkbookManager.TabViewType.MODEL_GAME)
                        {
                            SelectModelGame(chapter.ActiveModelGameIndex, false);
                        }
                    }
                }
                BoardCommentBox.ShowTabHints();
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
                        if (WorkbookManager.ActiveTab == WorkbookManager.TabViewType.EXERCISE)
                        {
                            SelectExercise(chapter.ActiveExerciseIndex, false);
                        }
                    }
                }
                BoardCommentBox.ShowTabHints();
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
                        // TODO: we should save this list for the Undo operation
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
                        // TODO: we should save this list for the Undo operation
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
                var dlg = new GameHeaderDialog(game, Properties.Resources.ResourceManager.GetString("GameHeader"))
                {
                    Left = ChessForgeMain.Left + 100,
                    Top = ChessForgeMain.Top + 100,
                    Topmost = false,
                    Owner = this
                };
                dlg.ShowDialog();
                if (dlg.ExitOK)
                {
                    AppState.IsDirty = true;
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
        public void EditExerciseHeader()
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;

                VariationTree game = WorkbookManager.SessionWorkbook.ActiveChapter.Exercises[chapter.ActiveExerciseIndex].Tree;
                var dlg = new GameHeaderDialog(game, Properties.Resources.ResourceManager.GetString("ExerciseHeader"))
                {
                    Left = ChessForgeMain.Left + 100,
                    Top = ChessForgeMain.Top + 100,
                    Topmost = false,
                    Owner = this
                };
                dlg.ShowDialog();
                if (dlg.ExitOK)
                {
                    AppState.IsDirty = true;
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
        /// Rebuilds all tree views
        /// </summary>
        private void RebuildAllTreeViews(bool? increaseFontDirection = null)
        {
            _studyTreeView?.BuildFlowDocumentForVariationTree();

            _modelGameTreeView?.BuildFlowDocumentForVariationTree();

            _exerciseTreeView?.BuildFlowDocumentForVariationTree();

            _chaptersView?.BuildFlowDocumentForChaptersView();

            if (TrainingSession.IsTrainingInProgress)
            {
                UiTrainingView.IncrementFontSize(increaseFontDirection);
            }
            else
            {
                RestoreSelectedLineAndMoveInActiveView();
            }
        }

        /// <summary>
        /// The increase font button was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnFontSizeUp_Click(object sender, RoutedEventArgs e)
        {
            // lets limit the increase to 4 pixels
            if (!Configuration.IsFontSizeAtMax)
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
            // do not allow decrease by more than 2 pixels
            if (!Configuration.IsFontSizeAtMin)
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
        /// Print request from the Main Menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnPrint_Click(object sender, RoutedEventArgs e)
        {
            WorkbookManager.TabViewType vt = AppState.ActiveTab;
            switch (vt)
            {
                case WorkbookManager.TabViewType.STUDY:
                    PrintView(UiRtbStudyTreeView);
                    break;
                case WorkbookManager.TabViewType.MODEL_GAME:
                    PrintView(UiRtbModelGamesView);
                    break;
                case WorkbookManager.TabViewType.EXERCISE:
                    PrintView(UiRtbExercisesView);
                    break;
            }
        }

        /// <summary>
        /// Prints the content of the passed RichTextBox
        /// </summary>
        /// <param name="rtb"></param>
        private void PrintView(RichTextBox rtb)
        {
            PrintDialog pd = new PrintDialog();
            if ((pd.ShowDialog() == true))
            {
                pd.PrintDocument((((IDocumentPaginatorSource)rtb.Document).DocumentPaginator), "printing as paginator");
            }
        }

    }
}

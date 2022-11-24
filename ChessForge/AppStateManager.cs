using System;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using ChessPosition;
using GameTree;
using Path = System.IO.Path;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ChessForge;
using System.Reflection;

namespace ChessForge
{
    /// <summary>
    /// Manages the application state and transitions between states.
    /// The App State is an aggregation of the Learning Mode, Evaluation State
    /// and Game State.
    /// 
    /// The combination of those values determines what actions are available 
    /// to the user what GUI controls are shown etc.
    /// 
    /// The Learning Mode can be MANUAL_REVIEW or TRAINING (also IDLE if no file
    /// is loaded).
    /// 
    /// The Evaluation State determines whether any evaluation is being run at all
    /// and if so, whether this is a single move or line evaluation. 
    /// 
    /// Within either Learning Mode, there can be a game played by the user against 
    /// the engine. While being played, the game will be in one of a few modes e.g.
    /// ENGINE_THINKING or USER_THINKING.
    /// 
    /// </summary>
    public class AppStateManager
    {
        // main application window
        private static MainWindow _mainWin;

        // last active tab in the Manual Review tab control
        private static WorkbookManager.TabViewType _lastActiveManualReviewTab = WorkbookManager.TabViewType.NONE;

        /// <summary>
        /// Gets the version of this Assembly
        /// </summary>
        /// <returns></returns>
        public static Version GetAssemblyVersion()
        {
            Assembly assem = typeof(AppStateManager).Assembly;
            AssemblyName assemName = assem.GetName();
            return assemName.Version;
        }

        /// <summary>
        /// The currently Active Tab.
        /// </summary>
        public static WorkbookManager.TabViewType ActiveTab
        {
            get => WorkbookManager.ActiveTab;
        }

        /// <summary>
        /// The most recent active tab in the Manual Review tab control.
        /// This value does not include the Training tab which is in a different
        /// tab control.
        /// </summary>
        public static WorkbookManager.TabViewType LastActiveManualReviewTab
        {
            get => _lastActiveManualReviewTab;
            set => _lastActiveManualReviewTab = value;
        }


        /// <summary>
        /// Accessor to the current ActiveVariationTree
        /// </summary>
        public static VariationTree ActiveVariationTree
        {
            get => MainWin.ActiveVariationTree;
        }

        /// <summary>
        /// Session Id of the Active Tree
        /// </summary>
        public static int ActiveTreeId
        {
            get => ActiveVariationTree == null ? -1 : ActiveVariationTree.Id;
        }

        /// <summary>
        /// Returns the number of Model Games in the Active Chapter.
        /// </summary>
        public static int ActiveChapterGamesCount
        {
            get
            {
                if (WorkbookManager.SessionWorkbook != null && WorkbookManager.SessionWorkbook.ActiveChapter != null)
                {
                    return WorkbookManager.SessionWorkbook.ActiveChapter.ModelGames.Count;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Returns the number of Exercises in the Active Chapter.
        /// </summary>
        public static int ActiveChapterExerciseCount
        {
            get
            {
                if (WorkbookManager.SessionWorkbook != null && WorkbookManager.SessionWorkbook.ActiveChapter != null)
                {
                    return WorkbookManager.SessionWorkbook.ActiveChapter.Exercises.Count;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Returns the current solving mode, if any/
        /// </summary>
        public static VariationTree.SolvingMode CurrentSolvingMode
        {
            get
            {
                if (AppStateManager.MainWin.ActiveVariationTree == null)
                {
                    return VariationTree.SolvingMode.NONE;
                }
                else
                {
                    return AppStateManager.MainWin.ActiveVariationTree.CurrentSolvingMode;
                }
            }
        }

        /// <summary>
        /// Returns the ContentType of the curent ActiveVariationTree
        /// </summary>
        public static GameData.ContentType ActiveContentType
        {
            get
            {
                if (ActiveVariationTree != null)
                {
                    return ActiveVariationTree.ContentType;
                }
                else
                {
                    return GameData.ContentType.NONE;
                }
            }
        }

        /// <summary>
        /// Returns the Mode object given its Tree and Node id.
        /// </summary>
        /// <param name="treeId"></param>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public static TreeNode GetNodeByIds(int treeId, int nodeId)
        {
            // TODO: handle treeId
            if (treeId < 0 && ActiveVariationTree != null)
            {
                return ActiveVariationTree.GetNodeFromNodeId(nodeId);
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Types of files that Chess Forge can handle.
        /// PGN can only be viewed, not edited.
        /// CHF can be viewed and edited.
        /// </summary>
        public enum FileType
        {
            NONE,
            CHESS_FORGE_PGN,
            GENERIC_PGN
        }

        /// <summary>
        /// A utility clearing the events so that e.g. we can 
        /// show messages in the BoardCommentBox immediately.
        /// </summary>
        public static void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(delegate { }));
        }

        /// <summary>
        /// Name of the engine currently loaded.
        /// </summary>
        public static string EngineName = "unknown";

        /// <summary>
        /// Indicates whether there are any unsaved changes in the Workbook
        /// </summary>
        private static bool _isDirty;

        // path to the current workbook file
        private static string _workbookFilePath;

        // type of the current workbook (chf or pgn)
        private static FileType _workbookFileType;

        /// <summary>
        /// The file path of the current Workbook file.
        /// When set, checks if there was a different value previously, and if
        /// so, if it should be saved.
        /// </summary>
        public static string WorkbookFilePath
        {
            get => _workbookFilePath;
            set
            {
                _workbookFilePath = value;
                if (string.IsNullOrWhiteSpace(_workbookFilePath))
                {
                    _workbookFileType = FileType.NONE;
                }
                else
                {
                    _workbookFileType = FileType.CHESS_FORGE_PGN;
                }
            }
        }

        /// <summary>
        /// Handles events from the AutoSave timer.
        /// Writes out the current file if it is "dirty" and if
        /// AutoSave is enabled (which it should be if this event
        /// is enabled but we do a defensive check)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void AutoSaveEvent(object source, ElapsedEventArgs e)
        {
            if (IsDirty && Configuration.AutoSave)
            {
                SaveWorkbookFile();
            }
        }

        /// <summary>
        /// Saves the Workbook to a new file, updates the title bar
        /// and the list of recent files.
        /// </summary>
        /// <param name="pgnFileName"></param>
        /// <param name="chfFileName"></param>
        public static void SaveWorkbookToNewFile(string pgnFileName, string chfFileName, bool typeConversion)
        {
            WorkbookFilePath = chfFileName;
            SaveWorkbookFile();
            UpdateAppTitleBar();
            if (typeConversion)
            {
                Configuration.RemoveFromRecentFiles(pgnFileName);
            }
            Configuration.AddRecentFile(chfFileName);
            _mainWin.RecreateRecentFilesMenuItems();
            Configuration.LastWorkbookFile = chfFileName;
            Configuration.WriteOutConfiguration();
        }

        /// <summary>
        /// Updates the application's title bar with the name of the open file.
        /// </summary>
        public static void UpdateAppTitleBar()
        {
            _mainWin.Title = _mainWin.APP_NAME + " - " + Path.GetFileName(WorkbookFilePath);
        }

        /// <summary>
        /// Saves the workbook to a file.
        /// It will only write to the file if the 
        /// session's file type is CHF
        /// </summary>
        public static void SaveWorkbookFile(bool checkDirty = false)
        {
            if (checkDirty && !IsDirty)
                return;

            try
            {
                if (WorkbookFileType == FileType.CHESS_FORGE_PGN)
                {
                    string chfText = WorkbookFileTextBuilder.BuildWorkbookText();
                    File.WriteAllText(WorkbookFilePath, chfText);
                    IsDirty = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save file: " + ex.Message, "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Type of the file currently open as
        /// the Workbook.
        /// </summary>
        public static FileType WorkbookFileType { get => _workbookFileType; }

        /// <summary>
        /// Main application window
        /// </summary>
        public static MainWindow MainWin { get => _mainWin; set => _mainWin = value; }

        /// <summary>
        /// Reference to the ActiveLine object
        /// </summary>
        public static ActiveLineManager ActiveLine { get => MainWin.ActiveLine; }

        /// <summary>
        /// Current Learning Mode
        /// </summary>
        public static LearningMode.Mode CurrentLearningMode
        {
            get { return LearningMode.CurrentMode; }
        }

        /// <summary>
        /// Current Evaluation State
        /// </summary>
        public static EvaluationManager.Mode CurrentEvaluationMode
        {
            get { return EvaluationManager.CurrentMode; }
        }

        /// <summary>
        /// Current Game State.
        /// </summary>
        public static EngineGame.GameState CurrentGameState
        {
            get { return EngineGame.CurrentState; }
        }

        /// <summary>
        /// Indicates whether the currently open workbook has been modified.
        /// </summary>
        public static bool IsDirty
        {
            get => _isDirty;
            set
            {
                _mainWin.Dispatcher.Invoke(() =>
                {

                    _isDirty = value;
                    ConfigureSaveMenus();
                });
            }
        }

        /// <summary>
        /// Resets the relevant controls to bring the application
        /// into the IDLE mode after it was in another mode.
        /// </summary>
        public static void RestartInIdleMode(bool updateCommentBox = true)
        {
            BookmarkManager.ClearBookmarksGui();
            IsDirty = false;
            WorkbookManager.ClearAll();
            _mainWin.ClearTreeViews();
            _mainWin.UiTabChapters.Focus();
            _mainWin.SetupGuiForChapters();
            _mainWin.Dispatcher.Invoke(() =>
            {
                _mainWin.ActiveLine.Clear();
                _mainWin.UiRtbChaptersView.Document.Blocks.Clear();
                _mainWin.UiRtbStudyTreeView.Document.Blocks.Clear();
                _mainWin.UiRtbTrainingProgress.Document.Blocks.Clear();

                _mainWin.ResetEvaluationProgressBae();

                EngineGame.ChangeCurrentState(EngineGame.GameState.IDLE);
                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);

                _mainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardBlue;
                _mainWin.MainChessBoard.FlipBoard(PieceColor.White);
                _mainWin.DisplayPosition(PositionUtils.SetupStartingPosition());
                _mainWin.RemoveMoveSquareColors();
                WorkbookFilePath = "";
                UpdateAppTitleBar();
                SwapCommentBoxForEngineLines(false);
                LearningMode.ChangeCurrentMode(LearningMode.Mode.IDLE);
                SetupGuiForCurrentStates();
                if (updateCommentBox)
                {
                    _mainWin.BoardCommentBox.OpenFile();
                }
            });
        }

        /// <summary>
        /// The Learning Mode, Evaluation State and Game State
        /// determine visibility of most of the GUI controls.
        /// There are some controls that have to be handled dynamically
        /// within the modes/states e.g. we don't want to replace the 
        /// Evaluation Lines with the Comment Box as soon as Evaluation
        /// stops but only after some additional user action. Therefore,
        /// this method will not hide Evalauation Lines as their visibility
        /// will be handled elsewhere
        /// </summary>
        public static void SetupGuiForCurrentStates()
        {
            switch (CurrentLearningMode)
            {
                case LearningMode.Mode.IDLE:
                case LearningMode.Mode.MANUAL_REVIEW:
                    SetupGuiForManualReview();
                    break;
                case LearningMode.Mode.TRAINING:
                    SetupGuiForTraining();
                    break;
                case LearningMode.Mode.ENGINE_GAME:
                    SetupGuiForEngineGame();
                    break;
            }
            ShowEvaluationControlsForCurrentStates();
            ConfigureMainBoardContextMenu();
            ConfigureSaveMenus();
            ConfigureFontSizeMenus();
        }
        /// <summary>
        /// Enables Move/Line evaluation menus.
        /// Hides engine evaluation progress bar.
        /// </summary>
        public static void ResetEvaluationControls()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                _mainWin.UiMnciEvalLine.IsEnabled = true;
                _mainWin.UiMnciEvalPos.IsEnabled = true;
                _mainWin.UiPbEngineThinking.Visibility = Visibility.Hidden;
            });
        }

        /// <summary>
        /// Preparations for move evaluation that are common for Position/Line 
        /// evaluation as well as requesting engine move in a game.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="position"></param>
        public static string PrepareMoveEvaluation(BoardPosition position, bool monitorLines)
        {
            string fen = FenParser.GenerateFenFromPosition(position);
            return PrepareMoveEvaluation(fen, monitorLines);
        }

        /// <summary>
        /// Preparations for move evaluation that are common for Position/Line 
        /// evaluation as well as requesting engine move in a game.
        /// </summary>
        /// <param name="fen"></param>
        /// <param name="monitorLines"></param>
        /// <returns></returns>
        public static string PrepareMoveEvaluation(string fen, bool monitorLines)
        {
            PrepareEvaluationControls();

            // do not remove/stop EVALUATION_LINE_DISPLAY timer as it is responsible
            // for keeping the progress bar active
            _mainWin.Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
            _mainWin.Timers.Start(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);

            return fen;
        }

        /// <summary>
        /// Depending on the "showEngineLines" argument
        /// shows either the Comment Box or the Engine Lines text box.
        /// </summary>
        /// <param name="showEngineLines"></param>
        public static void SwapCommentBoxForEngineLines(bool showEngineLines)
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                _mainWin.UiRtbBoardComment.Visibility = showEngineLines ? Visibility.Hidden : Visibility.Visible;
                _mainWin.UiTbEngineLines.Visibility = showEngineLines ? Visibility.Visible : Visibility.Hidden;
                if (!showEngineLines)
                {
                    _mainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                }
            });
        }

        /// <summary>
        /// Sets visibility for the controls relevant to move evaluation modes.
        /// NOTE: this is not applicable to move evaluation during a game.
        /// Engine Lines TextBox replaces the Board Comment RichTextBox if
        /// we are in the Position/Line evaluation mode.
        /// </summary>
        /// <param name="visible"></param>
        /// <param name="keepLinesBox"></param>
        public static void ShowMoveEvaluationControls(bool visible, bool keepLinesBox = false)
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                if (visible && CurrentEvaluationMode != EvaluationManager.Mode.ENGINE_GAME)
                {
                    _mainWin.UiRtbBoardComment.Visibility = Visibility.Hidden;
                    _mainWin.UiTbEngineLines.Visibility = Visibility.Visible;
                }
                else
                {
                    if (!keepLinesBox)
                    {
                        _mainWin.UiRtbBoardComment.Visibility = Visibility.Visible;
                        _mainWin.UiTbEngineLines.Visibility = Visibility.Hidden;
                    }

                }
                ShowEvaluationControlsForCurrentStates();
            });

        }


        /// <summary>
        /// This will setup the GUI for the Training progress
        /// unless we are in a game mode and the focus is here because
        /// the user requested the context menu.
        /// </summary>
        public static void SetupGuiForTrainingProgressMode()
        {
            if (AppStateManager.CurrentLearningMode == LearningMode.Mode.TRAINING)
            {
                _mainWin.Dispatcher.Invoke(() =>
                {
                    TrainingSession.IsBrowseActive = false;
                    _mainWin.UiTabCtrlTraining.Margin = new Thickness(5, 5, 5, 5);
                    _mainWin.UiDgEngineGame.Visibility = Visibility.Hidden;
                    _mainWin.UiDgActiveLine.Visibility = Visibility.Hidden;
                    _mainWin.UiLblScoresheet.Visibility = Visibility.Hidden;

                    _mainWin.DisplayPosition(EngineGame.GetLastGameNode());
                });
            }
        }

        /// <summary>
        /// Depending on what type of file we have and its state,
        /// set the state of the menus.
        /// </summary>
        public static void ConfigureSaveMenus()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {

                if (!string.IsNullOrEmpty(WorkbookFilePath) && IsDirty)
                {
                    _mainWin.UiMnWorkbookSave.IsEnabled = true;
                    _mainWin.UiMnWorkbookSave.Header = "Save " + Path.GetFileName(WorkbookFilePath);
                }
                else
                {
                    _mainWin.UiMnWorkbookSave.IsEnabled = false;
                    _mainWin.UiMnWorkbookSave.Header = "Save " + Path.GetFileName(WorkbookFilePath);
                }

                if (!string.IsNullOrEmpty(WorkbookFilePath))
                {
                    _mainWin.UiMnWorkbookSaveAs.IsEnabled = true;
                    _mainWin.UiMnWorkbookSaveAs.Header = "Save " + Path.GetFileName(WorkbookFilePath) + " As...";
                }
                else
                {
                    _mainWin.UiMnWorkbookSaveAs.IsEnabled = false;
                    _mainWin.UiMnWorkbookSaveAs.Header = "Save As...";
                }
            });
        }

        /// <summary>
        /// Sets up Font Size menus state
        /// </summary>
        public static void ConfigureFontSizeMenus()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                _mainWin.UiMnIncreaseFontSize.IsEnabled = !Configuration.IsFontSizeAtMax;
                _mainWin.UiMnDecreaseFontSize.IsEnabled = !Configuration.IsFontSizeAtMin;
                if (Configuration.UseFixedFont)
                {
                    _mainWin.UiMnFixVariableFontSize.Header = "Use Variable Size Font";
                    (_mainWin.UiMnFixVariableFontSize.Icon as Image).Source = ImageSources.FontSizeVariable;
                }
                else
                {
                    _mainWin.UiMnFixVariableFontSize.Header = "Use Fixed Size Font";
                    (_mainWin.UiMnFixVariableFontSize.Icon as Image).Source = ImageSources.FontSizeFixed;
                }
            });
        }

        /// <summary>
        /// Sets the image for the main chessboard matching the current active tab.
        /// </summary>
        /// <param name="tabType"></param>
        public static void SetChessboardForTab(WorkbookManager.TabViewType tabType)
        {
            switch (tabType)
            {
                case WorkbookManager.TabViewType.CHAPTERS:
                    MainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardBlue;
                    break;
                case WorkbookManager.TabViewType.STUDY:
                    MainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardBlue;
                    break;
                case WorkbookManager.TabViewType.MODEL_GAME:
                    //bool res = UiTabModelGames.Focus();
                    MainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardLightBlue;
                    break;
                case WorkbookManager.TabViewType.EXERCISE:
                    MainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardLightGreen;
                    break;
                case WorkbookManager.TabViewType.BOOKMARKS:
                    MainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardBlue;
                    break;
                default:
                    MainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardBlue;
                    break;
            }
        }


        /// <summary>
        /// Sets up GUI elements for the Manual Review mode.
        /// </summary>
        private static void SetupGuiForManualReview()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                if (CurrentLearningMode == LearningMode.Mode.IDLE)
                {
                    _mainWin.UiMnCloseWorkbook.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _mainWin.UiMnCloseWorkbook.Visibility = Visibility.Visible;
                }

                //MainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardBlue;
                SetChessboardForActiveTab();

                if (AppStateManager.ActiveContentType == GameData.ContentType.STUDY_TREE && WorkbookManager.ActiveTab == WorkbookManager.TabViewType.STUDY)
                {
                    _mainWin.UiDgActiveLine.Visibility = Visibility.Visible;
                    _mainWin.UiLblScoresheet.Visibility = Visibility.Visible;
                    _mainWin.UiDgEngineGame.Visibility = Visibility.Hidden;
                }
                else
                {
                    _mainWin.UiDgActiveLine.Visibility = Visibility.Hidden;
                    _mainWin.UiLblScoresheet.Visibility = Visibility.Hidden;
                    _mainWin.UiDgEngineGame.Visibility = Visibility.Hidden;
                }

                _mainWin.UiTabCtrlTraining.Visibility = Visibility.Hidden;
                _mainWin.UiTabCtrlManualReview.Visibility = Visibility.Visible;

                _mainWin.UiTabStudyTree.Visibility = Visibility.Visible;
                _mainWin.UiTabBookmarks.Visibility = Visibility.Visible;

                _mainWin.UiTabTrainingProgress.Visibility = Visibility.Hidden;

                // these tabs may have been disabled for the engine game
                _mainWin.UiRtbStudyTreeView.Opacity = 1;
                _mainWin.UiRtbStudyTreeView.IsEnabled = true;

                _mainWin.UiTabBookmarks.Opacity = 1;
                _mainWin.UiTabBookmarks.IsEnabled = true;

                _mainWin.UiBtnExitTraining.Visibility = Visibility.Collapsed;
                _mainWin.UiBtnExitGame.Visibility = Visibility.Collapsed;

                ShowGuiActiveLine(true);
                ShowEvaluationControlsForCurrentStates();

                ConfigureMenusForManualReview();
            });
        }

        /// <summary>
        /// Sets the chessboard image according to the current tab.
        /// </summary>
        private static void SetChessboardForActiveTab()
        {
            SetChessboardForTab(WorkbookManager.ActiveTab);
        }

        /// <summary>
        /// Sets up GUI elements for the Training mode.
        /// </summary>
        private static void SetupGuiForTraining()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                _mainWin.UiMnCloseWorkbook.Visibility = Visibility.Visible;

                _mainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardGreen;

                _mainWin.UiDgActiveLine.Visibility = Visibility.Hidden;
                _mainWin.UiLblScoresheet.Visibility = Visibility.Hidden;
                ShowGuiEngineGameLine(false);

                _mainWin.UiTabCtrlManualReview.Visibility = Visibility.Hidden;
                _mainWin.UiTabCtrlTraining.Visibility = Visibility.Visible;

                _mainWin.UiTabStudyTree.Visibility = Visibility.Hidden;
                _mainWin.UiTabBookmarks.Visibility = Visibility.Hidden;

                _mainWin.UiTabTrainingProgress.Visibility = Visibility.Visible;

                _mainWin.UiBtnExitTraining.Visibility = Visibility.Visible;
                _mainWin.UiBtnExitGame.Visibility = Visibility.Collapsed;

                ShowEvaluationControlsForCurrentStates();

                ConfigureMenusForTraining();
            });
        }

        /// <summary>
        /// Sets up GUI elements for the Training mode.
        /// </summary>
        private static void SetupGuiForEngineGame()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                _mainWin.UiMnCloseWorkbook.Visibility = Visibility.Visible;

                if (TrainingSession.IsTrainingInProgress)
                {
                    _mainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardGreen;

                    _mainWin.UiDgActiveLine.Visibility = Visibility.Hidden;
                    _mainWin.UiLblScoresheet.Visibility = Visibility.Hidden;
                    _mainWin.UiDgEngineGame.Visibility = Visibility.Visible;

                    _mainWin.UiTabCtrlManualReview.Visibility = Visibility.Hidden;
                    _mainWin.UiTabStudyTree.Visibility = Visibility.Hidden;
                    _mainWin.UiTabBookmarks.Visibility = Visibility.Hidden;

                    _mainWin.UiTabCtrlTraining.Visibility = Visibility.Visible;
                    _mainWin.UiTabTrainingProgress.Visibility = Visibility.Visible;

                    _mainWin.UiBtnExitTraining.Visibility = Visibility.Visible;
                    _mainWin.UiBtnExitGame.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _mainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardGreen;

                    _mainWin.UiDgActiveLine.Visibility = Visibility.Hidden;
                    _mainWin.UiLblScoresheet.Visibility = Visibility.Hidden;
                    _mainWin.UiDgEngineGame.Visibility = Visibility.Visible;

                    _mainWin.UiTabCtrlManualReview.Visibility = Visibility.Visible;
                    _mainWin.UiTabStudyTree.Visibility = Visibility.Visible;
                    _mainWin.UiTabBookmarks.Visibility = Visibility.Visible;

                    _mainWin.UiTabCtrlTraining.Visibility = Visibility.Hidden;
                    _mainWin.UiTabTrainingProgress.Visibility = Visibility.Hidden;

                    _mainWin.UiBtnExitTraining.Visibility = Visibility.Collapsed;
                    _mainWin.UiBtnExitGame.Visibility = Visibility.Visible;
                }

                ShowEvaluationControlsForCurrentStates();
                ShowGuiEngineGameLine(true);

                ConfigureMenusForEngineGame();
            });
        }

        /// <summary>
        /// Configures menu items for the Manual Review mode
        /// </summary>
        private static void ConfigureMenusForManualReview()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                _mainWin.UiMnStartTraining.IsEnabled = true;
                _mainWin.UiMnRestartTraining.IsEnabled = false;
                _mainWin.UiMnExitTraining.IsEnabled = false;

                _mainWin.UiMnciPlayEngine.IsEnabled = true;
            });
        }

        /// <summary>
        /// Configures menu items for the Training mode
        /// </summary>
        private static void ConfigureMenusForTraining()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                _mainWin.UiMnStartTraining.IsEnabled = false;
                _mainWin.UiMnRestartTraining.IsEnabled = true;
                _mainWin.UiMnExitTraining.IsEnabled = true;

                _mainWin.UiMnciPlayEngine.IsEnabled = false;
            });
        }

        /// <summary>
        /// Configures menu items for the Engine Game mode
        /// </summary>
        private static void ConfigureMenusForEngineGame()
        {
            bool train = TrainingSession.IsTrainingInProgress;

            _mainWin.Dispatcher.Invoke(() =>
            {
                _mainWin.UiMnStartTraining.IsEnabled = !train;
                _mainWin.UiMnRestartTraining.IsEnabled = train;
                _mainWin.UiMnExitTraining.IsEnabled = train;

                _mainWin.UiMnciPlayEngine.IsEnabled = true;
            });
        }

        /// <summary>
        /// Configure the Main Board's context menu.
        /// </summary>
        public static void ConfigureMainBoardContextMenu()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                switch (CurrentLearningMode)
                {
                    case LearningMode.Mode.MANUAL_REVIEW:
                        _mainWin.UiMnciStartTraining.Visibility = Visibility.Visible;
                        _mainWin.UiMnciStartTrainingHere.Visibility = Visibility.Visible;
                        _mainWin.UiMnciRestartTraining.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciExitTraining.Visibility = Visibility.Collapsed;

                        _mainWin.UiMncMainBoardSepar_1.Visibility = Visibility.Visible;

                        _mainWin.UiMnciEvalPos.Visibility = Visibility.Visible;
                        _mainWin.UiMnciEvalLine.Visibility = Visibility.Visible;

                        _mainWin.UiMncMainBoardSepar_2.Visibility = Visibility.Visible;

                        _mainWin.UiMnciReplay.Visibility = Visibility.Visible;

                        _mainWin.UiMncMainBoardSepar_3.Visibility = Visibility.Visible;

                        _mainWin.UiMnciPlayEngine.Visibility = Visibility.Visible;
                        _mainWin.UiMnciExitEngineGame.Visibility = Visibility.Collapsed;
                        break;
                    case LearningMode.Mode.TRAINING:
                        _mainWin.UiMnciStartTraining.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciStartTrainingHere.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciRestartTraining.Visibility = Visibility.Visible;
                        _mainWin.UiMnciExitTraining.Visibility = Visibility.Visible;

                        _mainWin.UiMncMainBoardSepar_1.Visibility = Visibility.Collapsed;

                        _mainWin.UiMnciEvalPos.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciEvalLine.Visibility = Visibility.Collapsed;

                        _mainWin.UiMncMainBoardSepar_2.Visibility = Visibility.Collapsed;

                        _mainWin.UiMnciReplay.Visibility = Visibility.Collapsed;

                        _mainWin.UiMncMainBoardSepar_3.Visibility = Visibility.Collapsed;

                        _mainWin.UiMnciPlayEngine.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciExitEngineGame.Visibility = Visibility.Collapsed;
                        break;
                    case LearningMode.Mode.ENGINE_GAME:
                        if (TrainingSession.IsTrainingInProgress)
                        {
                            _mainWin.UiMnciStartTraining.Visibility = Visibility.Collapsed;
                            _mainWin.UiMnciStartTrainingHere.Visibility = Visibility.Collapsed;
                            _mainWin.UiMnciRestartTraining.Visibility = Visibility.Visible;
                            _mainWin.UiMnciExitTraining.Visibility = Visibility.Visible;

                            _mainWin.UiMncMainBoardSepar_1.Visibility = Visibility.Collapsed;

                            _mainWin.UiMnciExitEngineGame.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            _mainWin.UiMnciStartTraining.Visibility = Visibility.Visible;
                            _mainWin.UiMnciStartTrainingHere.Visibility = Visibility.Collapsed;
                            _mainWin.UiMnciRestartTraining.Visibility = Visibility.Collapsed;
                            _mainWin.UiMnciExitTraining.Visibility = Visibility.Collapsed;

                            _mainWin.UiMncMainBoardSepar_1.Visibility = Visibility.Visible;

                            _mainWin.UiMnciExitEngineGame.Visibility = Visibility.Visible;
                        }

                        _mainWin.UiMnciEvalPos.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciEvalLine.Visibility = Visibility.Collapsed;

                        _mainWin.UiMncMainBoardSepar_2.Visibility = Visibility.Collapsed;

                        _mainWin.UiMnciReplay.Visibility = Visibility.Collapsed;

                        _mainWin.UiMncMainBoardSepar_3.Visibility = Visibility.Collapsed;

                        _mainWin.UiMnciPlayEngine.Visibility = Visibility.Collapsed;
                        break;
                }

                // if ContentType is ModelGame or exercise, hide Training menus
                Workbook wb = WorkbookManager.SessionWorkbook;
                if (wb != null &&
                    (wb.ActiveContentType == GameData.ContentType.MODEL_GAME
                    || wb.ActiveContentType == GameData.ContentType.EXERCISE))
                {
                    _mainWin.UiMnciStartTraining.Visibility = Visibility.Collapsed;
                    _mainWin.UiMnciStartTrainingHere.Visibility = Visibility.Collapsed;
                    _mainWin.UiMnciRestartTraining.Visibility = Visibility.Collapsed;
                    _mainWin.UiMnciExitTraining.Visibility = Visibility.Collapsed;
                    _mainWin.UiMncMainBoardSepar_1.Visibility = Visibility.Collapsed;
                }

            });
        }

        /// <summary>
        /// Shows/hides the engine evaluation progress bar, labels,
        /// and the menu items for move and line evaluation.
        /// </summary>
        private static void ShowEvaluationControlsForCurrentStates()
        {
            bool eval = EvaluationManager.IsRunning;
            // hide eval info if this is a game AND we are not requesting eval durin game in Training mode
            bool game = LearningMode.CurrentMode == LearningMode.Mode.ENGINE_GAME &&
                    (EvaluationManager.CurrentMode == EvaluationManager.Mode.ENGINE_GAME || EvaluationManager.CurrentMode == EvaluationManager.Mode.IDLE);

            _mainWin.Dispatcher.Invoke(() =>
             {
                 if (eval)
                 {
                     if (EvaluationManager.CurrentMode == EvaluationManager.Mode.CONTINUOUS)
                     {
                         _mainWin.UiImgEngineOn.Visibility = Visibility.Visible;
                         _mainWin.UiImgEngineOff.Visibility = Visibility.Collapsed;

                         _mainWin.UiMnciEvalLine.IsEnabled = true;
                         _mainWin.UiMnciEvalPos.IsEnabled = false;

                         _mainWin.UiPbEngineThinking.Visibility = Visibility.Collapsed;

                         _mainWin.UiLblEvaluating.Visibility = Visibility.Visible;
                         _mainWin.UiLblMoveUnderEval.Visibility = Visibility.Visible;
                         _mainWin.UiLblEvalSecretMode.Visibility = Visibility.Collapsed;
                     }
                     else
                     {
                         _mainWin.UiImgEngineOn.Visibility = Visibility.Visible;
                         _mainWin.UiImgEngineOff.Visibility = Visibility.Collapsed;

                         _mainWin.UiMnciEvalLine.IsEnabled = false;
                         _mainWin.UiMnciEvalPos.IsEnabled = false;

                         _mainWin.UiPbEngineThinking.Visibility = Visibility.Visible;

                         if (game)
                         {
                             _mainWin.UiLblEvaluating.Visibility = Visibility.Hidden;
                             _mainWin.UiLblMoveUnderEval.Visibility = Visibility.Hidden;
                             _mainWin.UiLblEvalSecretMode.Visibility = Visibility.Visible;
                         }
                         else
                         {
                             _mainWin.UiLblEvaluating.Visibility = Visibility.Visible;
                             _mainWin.UiLblMoveUnderEval.Visibility = Visibility.Visible;
                             _mainWin.UiLblEvalSecretMode.Visibility = Visibility.Hidden;
                         }
                     }
                 }
                 else
                 {
                     _mainWin.UiImgEngineOn.Visibility = Visibility.Collapsed;
                     if (LearningMode.CurrentMode == LearningMode.Mode.IDLE)
                     {
                         _mainWin.UiImgEngineOff.Visibility = Visibility.Collapsed;
                     }
                     else
                     {
                         _mainWin.UiImgEngineOff.Visibility = Visibility.Visible;
                     }

                     _mainWin.UiMnciEvalLine.IsEnabled = true;
                     _mainWin.UiMnciEvalPos.IsEnabled = true;

                     _mainWin.UiPbEngineThinking.Visibility = Visibility.Hidden;
                     _mainWin.UiLblEvaluating.Visibility = Visibility.Hidden;
                     _mainWin.UiLblMoveUnderEval.Visibility = Visibility.Hidden;
                     _mainWin.UiLblEvalSecretMode.Visibility = Visibility.Hidden;
                 }
             });
        }

        /// <summary>
        /// Shows ActiveLine's DataGrid control.
        /// The width, as well as the size of the Tab controls depends on whether
        /// we are showing evaluations as well.
        /// </summary>
        /// <param name="includeEvals"></param>
        private static void ShowGuiActiveLine(bool includeEvals)
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                // only applicable to StudyTree
                if (ActiveContentType == GameData.ContentType.STUDY_TREE && WorkbookManager.ActiveTab == WorkbookManager.TabViewType.STUDY
                    && CurrentLearningMode != LearningMode.Mode.ENGINE_GAME)
                {
                    _mainWin.UiDgActiveLine.Visibility = Visibility.Visible;
                    _mainWin.UiLblScoresheet.Visibility = Visibility.Visible;
                    _mainWin.UiDgActiveLine.Columns[2].Visibility = includeEvals ? Visibility.Visible : Visibility.Hidden;
                    _mainWin.UiDgActiveLine.Columns[4].Visibility = includeEvals ? Visibility.Visible : Visibility.Hidden;
                    _mainWin.UiDgActiveLine.Width = includeEvals ? 260 : 160;

                    if (includeEvals)
                    {
                        _mainWin.ResizeTabControl(_mainWin.UiTabCtrlManualReview, TabControlSizeMode.SHOW_ACTIVE_LINE);
                    }
                    else
                    {
                        _mainWin.ResizeTabControl(_mainWin.UiTabCtrlManualReview, TabControlSizeMode.SHOW_ACTIVE_LINE_NO_EVAL);
                    }
                }
            });
        }

        /// <summary>
        /// Shows or hides EngineGame's DataGrid control.
        /// </summary>
        /// <param name="show"></param>
        private static void ShowGuiEngineGameLine(bool show)
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                // only applicable when playing in ManualReview
                if (ActiveContentType == GameData.ContentType.STUDY_TREE
                    && CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
                {
                    _mainWin.UiDgEngineGame.Visibility = show ? Visibility.Visible : Visibility.Hidden;
                    _mainWin.UiDgEngineGame.Width = 160;

                    // adjust tab controls position
                    if (TrainingSession.IsTrainingInProgress)
                    {
                        if (show)
                        {
                            MainWin.ResizeTabControl(_mainWin.UiTabCtrlTraining, TabControlSizeMode.SHOW_ENGINE_GAME_LINE);
                        }
                        else
                        {
                            MainWin.ResizeTabControl(_mainWin.UiTabCtrlTraining, TabControlSizeMode.HIDE_ENGINE_GAME_LINE);
                        }
                        //_mainWin.UiTabCtrlTraining.Margin = show ? new Thickness(180, 5, 5, 5) : new Thickness(5, 5, 5, 5);
                    }
                    else
                    {
                        if (show)
                        {
                            MainWin.ResizeTabControl(_mainWin.UiTabCtrlManualReview, TabControlSizeMode.SHOW_ENGINE_GAME_LINE);
                        }
                        else
                        {
                            MainWin.ResizeTabControl(_mainWin.UiTabCtrlManualReview, TabControlSizeMode.HIDE_ENGINE_GAME_LINE);
                        }
                        //_mainWin.UiTabCtrlManualReview.Margin = show ? new Thickness(180, 5, 5, 5) : new Thickness(5, 5, 5, 5);

                        _mainWin.UiTabStudyTree.Focus();
                    }
                }
            });
        }


        private static void PrepareEvaluationControls()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                _mainWin.UiMnciEvalLine.IsEnabled = false;
                _mainWin.UiMnciEvalPos.IsEnabled = false;

                _mainWin.UiPbEngineThinking.Minimum = 0;
                int moveTime = AppStateManager.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME ?
                    Configuration.EngineMoveTime : Configuration.EngineEvaluationTime;
                _mainWin.UiPbEngineThinking.Maximum = moveTime;
                _mainWin.UiPbEngineThinking.Value = 0;
            });
        }

    }
}

using ChessForge.Properties;
using ChessPosition;
using GameTree;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using WebAccess;
using Path = System.IO.Path;

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
    public class AppState
    {
        // main application window
        private static MainWindow _mainWin;

        // last active tab in the Manual Review tab control
        private static TabViewType _lastActiveManualReviewTab = TabViewType.NONE;

        /// <summary>
        /// Determines whether the Explorers are on.
        /// </summary>
        public static bool AreExplorersOn
        {
            get => Configuration.ShowExplorers;
        }

        /// <summary>
        /// Gets the version of this Assembly
        /// </summary>
        /// <returns></returns>
        public static Version GetAssemblyVersion()
        {
            Assembly assem = typeof(AppState).Assembly;
            AssemblyName assemName = assem.GetName();
            return assemName.Version;
        }

        /// <summary>
        /// Evaluations are not updatable if we are in the CONTINUOUS evaluation mode
        /// and Configuration.DontSavePositionEvals is true.
        /// </summary>
        public static bool EngineEvaluationsUpdateble
        {
            get => !Configuration.DontSavePositionEvals || CurrentEvaluationMode != EvaluationManager.Mode.CONTINUOUS;
        }

        /// <summary>
        /// Depending on the cofiguration and current evaluation mode,
        /// update Engine Toggle imgaes
        /// </summary>
        public static void UpdateEngineToggleImages()
        {
            MainWin.Dispatcher.Invoke(() =>
            {
                if (MainWin.UiImgEngineOn.Visibility == Visibility.Visible || MainWin.UiImgEngineOnGray.Visibility == Visibility.Visible)
                {
                    if (EngineEvaluationsUpdateble)
                    {
                        _mainWin.UiImgEngineOn.Visibility = Visibility.Visible;
                        _mainWin.UiImgEngineOnGray.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        _mainWin.UiImgEngineOn.Visibility = Visibility.Collapsed;
                        _mainWin.UiImgEngineOnGray.Visibility = Visibility.Visible;
                    }
                }
            });
        }

        /// <summary>
        /// The currently Active Tab.
        /// </summary>
        public static TabViewType ActiveTab
        {
            get => WorkbookManager.ActiveTab;
        }

        /// <summary>
        /// Returns content type corresponding to the ActiveTab.
        /// </summary>
        /// <returns></returns>
        public static GameData.ContentType GetContentTypeForActiveTab()
        {
            GameData.ContentType contentType = GameData.ContentType.UNKNOWN;

            switch (ActiveTab)
            {
                case TabViewType.INTRO:
                    contentType = GameData.ContentType.INTRO;
                    break;
                case TabViewType.STUDY:
                    contentType = GameData.ContentType.STUDY_TREE;
                    break;
                case TabViewType.MODEL_GAME:
                    contentType = GameData.ContentType.MODEL_GAME;
                    break;
                case TabViewType.EXERCISE:
                    contentType = GameData.ContentType.EXERCISE;
                    break;
            }

            return contentType;
        }

        /// <summary>
        /// Returns the current Workbook object
        /// </summary>
        public static Workbook Workbook
        {
            get => WorkbookManager.SessionWorkbook ?? null;
        }

        /// <summary>
        /// Determine whether the Intro tab should be shown
        /// and set its visibility accordingly.
        /// </summary>
        /// <param name="chapter"></param>
        public static void ShowIntroTab(Chapter chapter)
        {
            if (chapter != null)
            {
                MainWin.UiTabIntro.Visibility = chapter.ShowIntro ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Whether a tab with an active Tree View is currently open.
        /// </summary>
        /// <param name="ignoreActiveTree">If true, the function will ignore the stated of the Active Tree.
        /// Otherwise, it will return false if the ActiveTree is null. </param>
        /// <returns></returns>
        public static bool IsTreeViewTabActive(bool ignoreActiveTree = false)
        {
            return (ActiveVariationTree != null || ignoreActiveTree) &&
                (ActiveTab == TabViewType.STUDY
                || ActiveTab == TabViewType.MODEL_GAME
                || ActiveTab == TabViewType.EXERCISE);
        }

        /// <summary>
        /// Returns true if the Active Tab's type is one that allows drawing
        /// shapes on the board.
        /// </summary>
        public static bool IsTabAllowingBoardDraw
        {
            get =>
                 ActiveTab == TabViewType.STUDY
                || ActiveTab == TabViewType.MODEL_GAME
                || (ActiveTab == TabViewType.EXERCISE) // && !AppState.IsUserSolving())
                || ActiveTab == TabViewType.INTRO;
        }

        /// <summary>
        /// Returns true if the active tab if of the type that hosts a variation tree.
        /// </summary>
        public static bool IsVariationTreeTabType
        {
            get =>
                 ActiveTab == TabViewType.STUDY
                || ActiveTab == TabViewType.MODEL_GAME
                || ActiveTab == TabViewType.EXERCISE;
        }

        /// <summary>
        /// The most recent active tab in the Manual Review tab control.
        /// This value does not include the Training tab which is in a different
        /// tab control.
        /// </summary>
        public static TabViewType LastActiveManualReviewTab
        {
            get => _lastActiveManualReviewTab;
            set => _lastActiveManualReviewTab = value;
        }

        /// <summary>
        /// Accessor to the ActiveChapter object
        /// </summary>
        public static Chapter ActiveChapter
        {
            get
            {
                if (WorkbookManager.SessionWorkbook != null)
                {
                    return WorkbookManager.SessionWorkbook.ActiveChapter;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Accessor to the current ActiveVariationTree
        /// </summary>
        public static VariationTree ActiveVariationTree
        {
            get => MainWin.ActiveVariationTree;
        }

        /// <summary>
        ///  Returns the index of the active article in the chapter's list 
        ///  of articles for the type of ActiveVariationTree
        /// </summary>
        public static int ActiveArticleIndex
        {
            get
            {
                if (ActiveVariationTree == null || ActiveChapter == null)
                {
                    return -1;
                }

                GameData.ContentType contentType = ActiveVariationTree.ContentType;
                switch (contentType)
                {
                    case GameData.ContentType.STUDY_TREE:
                        return -1;
                    case GameData.ContentType.MODEL_GAME:
                        return ActiveChapter.ActiveModelGameIndex;
                    case GameData.ContentType.EXERCISE:
                        return ActiveChapter.ActiveExerciseIndex;
                    default:
                        return -1;
                }
            }
        }

        /// <summary>
        /// Session Id of the Active Tree
        /// </summary>
        public static int ActiveTreeId
        {
            get => ActiveVariationTree == null ? -1 : ActiveVariationTree.TreeId;
        }

        /// <summary>
        /// Returns the currently selected ("active") node.
        /// </summary>
        /// <returns></returns>
        public static TreeNode GetCurrentNode()
        {
            TreeNode nd;

            if (AppState.CurrentLearningMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                nd = MainWin.ActiveLine.GetSelectedTreeNode();
            }
            else
            {
                nd = EngineGame.GetLastGameNode();
            }

            return nd;
        }

        /// <summary>
        /// Notification that the background read has finished.
        /// Set the flag, update menus' status.
        /// </summary>
        public static void BackgroundReadFinished()
        {
            // if workbook is already set to ready, do not do anything.  This must have been a notification from background read for import.
            if (Workbook != null && !Workbook.IsReady)
            {
                Workbook.IsReady = true;
                if (LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW)
                {
                    ConfigureMenusForManualReview();
                }
            }
        }

        /// <summary>
        /// Checks if the passed move (in the engine notation) is valid
        /// in the currently selected position.
        /// </summary>
        /// <param name="engMove"></param>
        /// <returns></returns>
        public static bool IsMoveValid(string engMove)
        {
            bool valid = false;

            TreeNode nd = GetCurrentNode();
            if (nd != null)
            {
                BoardPosition pos = new BoardPosition(nd.Position);
                string alg = MoveUtils.EngineNotationToAlgebraic(engMove, ref pos, out _);
                if (!string.IsNullOrEmpty(alg) && !alg.StartsWith("?"))
                {
                    valid = true;
                }
            }

            return valid;
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
                if (AppState.MainWin.ActiveVariationTree == null)
                {
                    return VariationTree.SolvingMode.NONE;
                }
                else
                {
                    return AppState.MainWin.ActiveVariationTree.CurrentSolvingMode;
                }
            }
        }

        /// <summary>
        /// Checks if the user is currently solving an exercise rather thanjust editing it 
        /// (or being in a different mode altogether)
        /// </summary>
        /// <returns></returns>
        public static bool IsUserSolving()
        {
            return CurrentSolvingMode == VariationTree.SolvingMode.GUESS_MOVE || CurrentSolvingMode == VariationTree.SolvingMode.ANALYSIS;
        }

        /// <summary>
        /// Hide NAG buttons when in Solving Mode.
        /// </summary>
        public static void EnableNagBar()
        {
            bool enable = !IsUserSolving();
            foreach (var child in MainWin.UiStpExNags.Children)
            {
                if (child is Button button)
                {
                    button.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
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
            if (ActiveVariationTree != null && ActiveVariationTree.TreeId == treeId)
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
        public static string EngineName = Properties.Resources.UnknownEngine;

        // Indicates whether there are any unsaved changes in the Workbook
        private static bool _isDirty;

        // Indicates whether the index depth in any of the Studies is dirty
        private static bool _isIndexDepthDirty;

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
        /// Enable/Disable menu items for the Active Tab's context menu.
        /// </summary>
        /// <param name="tabType"></param>
        /// <param name="lastClickedNodeId"></param>
        /// <param name="isEnabled"></param>
        public static void EnableTabViewMenuItems(TabViewType tabType, int lastClickedNodeId, bool isEnabled)
        {
            TreeNode selectedNode = null;
            if (MainWin.ActiveTreeView != null)
            {
                selectedNode = MainWin.ActiveTreeView.GetSelectedNode();
            }

            int activeNode = selectedNode == null ? -1 : selectedNode.NodeId;
            switch (tabType)
            {
                case TabViewType.STUDY:
                    EnableStudyTreeMenuItems(selectedNode == null ? -1 : selectedNode.NodeId, isEnabled);
                    break;
                case TabViewType.MODEL_GAME:
                    EnableModelGamesMenuItems(selectedNode == null ? -1 : selectedNode.NodeId);
                    break;
                case TabViewType.EXERCISE:
                    EnableExercisesMenuItems(activeNode);
                    break;
            }
        }

        /// <summary>
        /// Handles events from the AutoSave timer.
        /// Writes out the current file if it is "dirty" and if
        /// AutoSave is enabled (which it should be if this event
        /// is enabled but we do a defensive check).
        /// However, if WorkbookFilePath is empty, this indicates that the user
        /// downloaded the workbook and declined to save it locally.
        /// Therefore, we will not save it.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void AutoSaveEvent(object source, ElapsedEventArgs e)
        {
            if (IsDirty && Configuration.AutoSave && !string.IsNullOrEmpty(WorkbookFilePath))
            {
                SaveWorkbookFile(null);
            }
        }

        /// <summary>
        /// Saves the Workbook to a new file, updates the title bar
        /// and the list of recent files.
        /// </summary>
        /// <param name="pgnFileName"></param>
        /// <param name="chfFileName"></param>
        public static void SaveWorkbookToNewFile(string pgnFileName, string chfFileName)
        {
            WorkbookFilePath = chfFileName;
            SaveWorkbookFile(null);
            UpdateAppTitleBar();
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
            StringBuilder sb = new StringBuilder(_mainWin.APP_NAME + " - ");
            if (Workbook != null)
            {
                string titleToShow = string.IsNullOrEmpty(WorkbookFilePath) ? Workbook.Title : Path.GetFileName(WorkbookFilePath);
                sb.Append(titleToShow);
                sb.Append(" " + Properties.Resources.VersionAbbr + " " + Workbook.Version.ToString());
            }
            _mainWin.Title = sb.ToString();
        }


        // A lock preventing serving of multiple save requests.
        // Otherwise manual save while AutoSave is running
        // could corrupt the file
        private static object _lockFileSave = new object();

        /// <summary>
        /// Saves the workbook to its PGN file.
        /// Note that if the user is working with w workbook file
        /// that was downloaded and not saved locally, both filePath
        /// and WorkbookFilePath will be null or empty.
        /// In this case, we will prompt the user to choose the file
        /// name and save it, or not save it.
        /// </summary>
        /// <returns>true if successful, false on exception</returns>
        public static bool SaveWorkbookFile(string filePath, bool checkDirty = false)
        {
            bool result = true;

            if (checkDirty && !IsDirty && !IsIndexDepthDirty)
            {
                return true;
            }

            AppLog.Message("Saving Workbook to File");
            try
            {
                string savePath = string.IsNullOrWhiteSpace(filePath) ? WorkbookFilePath : filePath;
                if (string.IsNullOrEmpty(savePath))
                {
                    // downloaded file, not saved previously
                    WorkbookManager.SaveWorkbookToNewFile(null);
                }
                else
                {
                    if (WorkbookFileType == FileType.CHESS_FORGE_PGN)
                    {
                        lock (_lockFileSave)
                        {
                            MainWin.SaveIntro();
                            string chfText = WorkbookFileTextBuilder.BuildWorkbookText();
                            File.WriteAllText(savePath, chfText);
                        }

                        // if background processing is in progress, do not mark as clean
                        if (!WorkbookManager.SessionWorkbook.IsBackgroundLoadingInProgress)
                        {
                            IsDirty = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = false;
                AppLog.Message("SaveWorkbookFile()", ex);
                MessageBox.Show(Properties.Resources.FailedToSaveFile + ": " + ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            AppLog.Message("Workbook Saved to File");

            return result;
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
        /// Download a game from lichess and add to ActiveChapter
        /// </summary>
        /// <param name="gameId"></param>
        public static async void DownloadLichessGameToActiveChapter(string gameId)
        {
            try
            {
                await WebAccess.GameDownload.GetGame(gameId);

                Chapter chapter = AppState.ActiveChapter;
                if (chapter != null)
                {
                    VariationTree tree = new VariationTree(GameData.ContentType.MODEL_GAME);
                    PgnGameParser pgnGame = new PgnGameParser(GameDownload.GameText, tree, null);
                    if (string.IsNullOrEmpty(GameDownload.GameText))
                    {
                        throw new Exception(Properties.Resources.ErrNoTextReceived);
                    }

                    FinalizeLichessDownload(chapter, tree, gameId, ActiveTab);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Properties.Resources.CouldNotImportGame + ": " + ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Finalizes the operations after a successful import from Lichess.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="tree"></param>
        /// <param name="lichessGameId"></param>
        /// <param name="activeTabOnEntry"></param>
        /// <returns></returns>
        public static bool FinalizeLichessDownload(Chapter chapter, VariationTree tree, string lichessGameId, TabViewType activeTabOnEntry)
        {
            bool added = false;

            tree.ContentType = GameData.ContentType.MODEL_GAME;
            tree.Header.SetHeaderValue(PgnHeaders.KEY_LICHESS_ID, lichessGameId);
            Article article = chapter.AddModelGame(tree);
            int articleIndex = chapter.GetModelGameCount() - 1;

            if (article != null)
            {
                added = true;

                WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.CREATE_ARTICLE, chapter, article, articleIndex);
                WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);

                chapter.ActiveModelGameIndex = chapter.GetModelGameCount() - 1;
                string guid = tree.Header.GetGuid(out _);

                // if the current active tree is Study Tree, add reference
                if (activeTabOnEntry == TabViewType.STUDY)
                {
                    TreeNode nd = chapter.StudyTree.Tree.SelectedNode;
                    if (nd != null)
                    {
                        nd.AddArticleReference(guid);
                        if (MainWin.StudyTreeView != null)
                        {
                            MainWin.StudyTreeView.InsertOrDeleteReferenceRun(nd);
                        }
                    }
                }

                MainWin.ChaptersView.IsDirty = true;
                IsDirty = true;
                MainWin.SelectModelGame(chapter.ActiveModelGameIndex, true);

                MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgGameImportSuccess, CommentBox.HintType.INFO);
            }

            return added;
        }

        /// <summary>
        /// Opens the last clicked Top Game in the browser.
        /// </summary>
        /// <param name="gameId"></param>
        public static void ViewGameOnLichess(string gameId)
        {
            System.Diagnostics.Process.Start("https://lichess.org/" + gameId);
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
        /// Indicates whether the index depth in any of the Studies was modified.
        /// </summary>
        public static bool IsIndexDepthDirty
        {
            get => _isIndexDepthDirty;
            set
            {
                _mainWin.Dispatcher.Invoke(() =>
                {

                    _isIndexDepthDirty = value;
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
            OpeningExplorer.ResetLastRequestedFen();
            IsDirty = false;
            WorkbookManager.ClearAll();
            Workbook?.GamesManager.CancelAll();
            _mainWin.ClearTreeViews(true);
            MainWin.UiTabIntro.Visibility = Visibility.Collapsed;
            _mainWin.UiTabChapters.Focus();
            _mainWin.SetupGuiForChapters();
            _mainWin.Dispatcher.Invoke(() =>
            {
                _mainWin.ActiveLine.Clear();
                _mainWin.UiRtbChaptersView.Document.Blocks.Clear();
                _mainWin.UiRtbIntroView.Document.Blocks.Clear();
                _mainWin.UiRtbStudyTreeView.Document.Blocks.Clear();
                _mainWin.UiRtbTrainingProgress.Document.Blocks.Clear();

                _mainWin.ResetEvaluationProgressBae();

                EngineGame.ChangeCurrentState(EngineGame.GameState.IDLE);

                _mainWin.UiImgMainChessboard.Source = Configuration.StudyBoardSet.MainBoard;
                _mainWin.MainChessBoard.FlipBoard(PieceColor.White);
                _mainWin.DisplayPosition(PositionUtils.SetupStartingPosition());
                _mainWin.RemoveMoveSquareColors();
                WorkbookFilePath = "";
                UpdateAppTitleBar();
                SwapCommentBoxForEngineLines(false);
                LearningMode.ChangeCurrentMode(LearningMode.Mode.IDLE, false);
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
            _mainWin.Dispatcher.Invoke(() =>
            {
                MainWin.UiRtbStudyTreeView.IsEnabled = true;

                MainWin.UiMnAnnotations.IsEnabled = false;
                MainWin.UiMnPaste.IsEnabled = false;
                MainWin.UiMnCommentBeforeMove.IsEnabled = false;
                MainWin.UiMnMergeChapters.IsEnabled = false;

                MainWin.UiMnWorkbookOptions.IsEnabled = WorkbookManager.SessionWorkbook != null;
                switch (CurrentLearningMode)
                {
                    case LearningMode.Mode.IDLE:
                    case LearningMode.Mode.MANUAL_REVIEW:
                        SetupGuiForManualReview();
                        ShowExplorers(AreExplorersOn, MainWin.ActiveTreeView != null && MainWin.ActiveTreeView.HasEntities);
                        break;
                    case LearningMode.Mode.TRAINING:
                        SetupGuiForTraining();
                        ShowExplorers(false, false);
                        break;
                    case LearningMode.Mode.ENGINE_GAME:
                        SetupGuiForEngineGame();
                        ShowExplorers(false, false);
                        break;
                }
                MultiTextBoxManager.ShowEvaluationChart(false);
                EnableNavigationArrows();
                ShowEvaluationControlsForCurrentStates();
                ConfigureMainBoardContextMenu();
                ConfigureSaveMenus();
                ConfigureFontSizeMenus();
            });
        }

        /// <summary>
        /// Set up Navigation arrows opacity per current learning mode and location position.
        /// </summary>
        public static void EnableNavigationArrows()
        {
            if (TrainingSession.IsTrainingInProgress)
            {
                MainWin.UiImgNavigateBack.Visibility = Visibility.Collapsed;
                MainWin.UiImgNavigateForward.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainWin.UiImgNavigateBack.Visibility = Visibility.Visible;
                MainWin.UiImgNavigateForward.Visibility = Visibility.Visible;
            }

            MainWin.UiImgNavigateBack.Opacity = WorkbookLocationNavigator.IsFirstLocation || CurrentLearningMode != LearningMode.Mode.MANUAL_REVIEW ? 0.5 : 1;
            MainWin.UiImgNavigateForward.Opacity = WorkbookLocationNavigator.IsLastLocation || CurrentLearningMode != LearningMode.Mode.MANUAL_REVIEW ? 0.5 : 1;
        }

        /// <summary>
        /// Shows/Hides Openings and Top Games
        /// explorers.
        /// </summary>
        /// <param name="visible"></param>
        public static void ShowExplorers(bool visible, bool anythingToShow)
        {
            bool validTabActive = ActiveTab == TabViewType.STUDY
                || ActiveTab == TabViewType.MODEL_GAME
                || ActiveTab == TabViewType.EXERCISE && (ActiveVariationTree == null || ActiveVariationTree.ShowTreeLines)
                || ActiveTab == TabViewType.INTRO;

            if (visible && validTabActive && WorkbookManager.SessionWorkbook != null)
            {
                MainWin.UiRtbOpenings.Visibility = Visibility.Visible;
                MainWin.UiRtbTopGames.Visibility = Visibility.Visible;
                MainWin.UiBtnShowExplorer.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainWin.UiRtbOpenings.Visibility = Visibility.Hidden;
                MainWin.UiRtbTopGames.Visibility = Visibility.Hidden;
                MainWin.UiBtnShowExplorer.Visibility = (!AreExplorersOn && validTabActive && anythingToShow) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (visible && AreExplorersOn)
            {
                MainWin.UpdateExplorersToggleState();
            }
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

                if (showEngineLines && MultiTextBoxManager.CanShowEvaluationChart(false, out _))
                {
                    _mainWin.UiEvalChart.Visibility = Visibility.Visible;
                }
                else
                {
                    _mainWin.UiEvalChart.Visibility = Visibility.Hidden;
                }

                if (!showEngineLines)
                {
                    _mainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                }
            });
        }

        /// <summary>
        /// Determines whether the evaluation lines can be shown to the user.
        /// We only want to hide them when:
        /// - we are playing a game against the engine and we are not in Training
        /// - we are training and IsContinuousEvaluation is false
        /// </summary>
        /// <returns></returns>
        public static bool ShowEvaluationLines()
        {
            return CurrentEvaluationMode != EvaluationManager.Mode.ENGINE_GAME
                                         || (TrainingSession.IsTrainingInProgress && TrainingSession.IsContinuousEvaluation);
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
                if (visible && (CurrentEvaluationMode != EvaluationManager.Mode.ENGINE_GAME || TrainingSession.IsContinuousEvaluation))
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
        /// Depending on what type of file we have and its state,
        /// set the state of the menus.
        /// </summary>
        public static void ConfigureSaveMenus()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                string filePath = WorkbookFilePath ?? "";
                string fileName = Path.GetFileName(filePath);

                _mainWin.UiMnWorkbookSave.Header = Properties.Resources.Save + " " + fileName;

                string resSaveAs = Properties.Resources.SaveAs ?? "";
                resSaveAs = resSaveAs.Replace("$0", fileName);
                _mainWin.UiMnWorkbookSaveAs.Header = resSaveAs;

                if (!string.IsNullOrEmpty(filePath) && (IsDirty || IsIndexDepthDirty))
                {
                    _mainWin.UiMnWorkbookSave.IsEnabled = true;
                }
                else
                {
                    _mainWin.UiMnWorkbookSave.IsEnabled = false;
                }

                if (!string.IsNullOrEmpty(filePath))
                {
                    _mainWin.UiMnWorkbookSaveAs.IsEnabled = true;
                    _mainWin.UiMnBackupVersion.IsEnabled = true;
                }
                else
                {
                    _mainWin.UiMnWorkbookSaveAs.IsEnabled = false;
                    _mainWin.UiMnBackupVersion.IsEnabled = false;
                }

                MainWin.UiMnExportRtf.IsEnabled = AppState.Workbook != null;
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
                    _mainWin.UiMnFixVariableFontSize.Header = Resources.ResourceManager.GetString("UseVariableFontSize");
                    (_mainWin.UiMnFixVariableFontSize.Icon as Image).Source = ImageSources.FontSizeVariable;
                }
                else
                {
                    _mainWin.UiMnFixVariableFontSize.Header = Resources.ResourceManager.GetString("UseFixedFontSize");
                    (_mainWin.UiMnFixVariableFontSize.Icon as Image).Source = ImageSources.FontSizeFixed;
                }
            });
        }

        /// <summary>
        /// Returns true if the position with a given NodeId is the active tree
        /// is a checkmate or a stalemate.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public static bool IsCheckMateOrStalemate(int nodeId)
        {
            bool isMate = false;

            if (ActiveVariationTree != null)
            {
                TreeNode nd = ActiveVariationTree.GetNodeFromNodeId(nodeId);
                if (nd != null && (nd.Position.IsCheckmate || nd.Position.IsStalemate))
                {
                    isMate = true;
                }
            }

            return isMate;
        }

        /// <summary>
        /// Sets the image for the main chessboard matching the current active tab.
        /// </summary>
        /// <param name="tabType"></param>
        public static void SetChessboardForTab(TabViewType tabType)
        {
            switch (tabType)
            {
                case TabViewType.CHAPTERS:
                    MainWin.UiImgMainChessboard.Source = Configuration.StudyBoardSet.MainBoard;
                    break;
                case TabViewType.STUDY:
                    MainWin.UiImgMainChessboard.Source = Configuration.StudyBoardSet.MainBoard;
                    break;
                case TabViewType.MODEL_GAME:
                    //bool res = UiTabModelGames.Focus();
                    MainWin.UiImgMainChessboard.Source = Configuration.GameBoardSet.MainBoard;
                    break;
                case TabViewType.EXERCISE:
                    MainWin.UiImgMainChessboard.Source = Configuration.ExerciseBoardSet.MainBoard;
                    break;
                case TabViewType.BOOKMARKS:
                    MainWin.UiImgMainChessboard.Source = Configuration.StudyBoardSet.MainBoard;
                    break;
                default:
                    MainWin.UiImgMainChessboard.Source = Configuration.StudyBoardSet.MainBoard;
                    break;
            }
        }

        /// <summary>
        /// Sets up StudyTrees's context menu.
        /// </summary>
        /// <param name="lastClickedNodeId"></param>
        /// <param name="isEnabled"></param>
        private static void EnableStudyTreeMenuItems(int lastClickedNodeId, bool isEnabled)
        {
            try
            {
                bool isMate = IsCheckMateOrStalemate(lastClickedNodeId);

                VariationTree tree = ActiveVariationTree;
                VariationTreeView view = AppState.MainWin.ActiveTreeView;

                ConfigureBookmarkMenuOptions(MainWin.UiMnMarkBookmark, MainWin.UiMnStDeleteBookmark);

                foreach (var item in MainWin.UiMncStudyTree.Items)
                {
                    if (item is MenuItem)
                    {
                        MenuItem menuItem = item as MenuItem;
                        switch (menuItem.Name)
                        {
                            case "UiMnRegenerateStudy":
                                menuItem.IsEnabled = true;
                                break;
                            case "UiMnStudyStartTrainingHere":
                            case "UiMnStudy_CreateExercise":
                                menuItem.IsEnabled = !isMate;
                                break;
                            case "UiMnMarkBookmark":
                            case "UiMnStudyFindIdentical":
                                menuItem.IsEnabled = isEnabled;
                                break;
                            case "UiMnStudyWorkbookEvalMove":
                                menuItem.IsEnabled = tree != null && tree.SelectedNode != null;
                                break;
                            case "_mnWorkbookEvalLine":
                                menuItem.IsEnabled = tree != null && tree.Nodes.Count > 1;
                                break;
                            case "_mnStudyTree_MarkThumbnail":
                                menuItem.IsEnabled = tree != null && tree.SelectedNode != null;
                                break;
                            case "UiMnStCopyMoves":
                            case "UiMnStCutMoves":
                                menuItem.IsEnabled = view != null && view.HasMovesSelectedForCopy;
                                break;
                            case "UiMnStPasteMoves":
                                menuItem.IsEnabled = SystemClipboard.HasSerializedData() || !string.IsNullOrEmpty(SystemClipboard.GetText());
                                break;
                            case "UiMnPromoteLine":
                                menuItem.IsEnabled = tree.SelectedNode != null && !tree.SelectedNode.IsMainLine();
                                break;
                            case "UiMnReorderLines":
                                menuItem.IsEnabled = tree.SelectedNode != null && tree.SelectedNode.Parent != null && tree.SelectedNode.Parent.Children.Count > 1;
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("EnableStudyTreeMenus()", ex);
            }
        }

        /// <summary>
        /// Sets up ModelGames context menu.
        /// </summary>
        /// <param name="selectedNodeId"></param>
        /// <param name="isEnabled"></param>
        private static void EnableModelGamesMenuItems(int selectedNodeId)
        {
            try
            {
                bool isMate = IsCheckMateOrStalemate(selectedNodeId);

                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                int gameCount = chapter.GetModelGameCount();
                int gameIndex = chapter.ActiveModelGameIndex;

                VariationTreeView view = MainWin.ActiveTreeView;

                ConfigureBookmarkMenuOptions(MainWin.UiMnGameMarkBookmark, MainWin.UiMnGameDeleteBookmark);

                foreach (var item in MainWin.UiMncModelGames.Items)
                {
                    if (item is MenuItem)
                    {
                        MenuItem menuItem = item as MenuItem;
                        switch (menuItem.Name)
                        {
                            case "_mnGame_EditHeader":
                            case "UiMnGameMoveToChapter":
                                menuItem.IsEnabled = gameIndex >= 0;
                                break;
                            case "_mnGame_CreateModelGame":
                                menuItem.IsEnabled = true;
                                break;
                            case "_mnGame_StartTrainingFromHere":
                                menuItem.IsEnabled = gameIndex >= 0 && !isMate;
                                break;
                            case "_mnGame_MergeToStudy":
                                menuItem.IsEnabled = gameIndex >= 0 && selectedNodeId >= 0;
                                break;
                            case "_mnGame_CopyFen":
                                menuItem.IsEnabled = gameIndex >= 0 && ActiveVariationTree != null;
                                break;
                            case "_mnGame_CreateExercise":
                                menuItem.IsEnabled = gameIndex >= 0 && selectedNodeId > 0 && !isMate;
                                break;
                            case "_mnGame_PromoteLine":
                                menuItem.IsEnabled = gameIndex >= 0 && selectedNodeId > 0;
                                break;
                            case "_mnGame_DeleteMovesFromHere":
                                menuItem.IsEnabled = gameIndex >= 0 && selectedNodeId > 0;
                                break;
                            case "_mnGame_DeleteModelGame":
                                menuItem.IsEnabled = gameIndex >= 0;
                                break;
                            case "UiMnGameMarkBookmark":
                                menuItem.IsEnabled = gameIndex >= 0 && selectedNodeId > 0;
                                break;
                            case "_mnGame_MarkThumbnail":
                                menuItem.IsEnabled = gameIndex >= 0 && selectedNodeId > 0;
                                break;
                            case "UiMnGameCopyMoves":
                            case "UiMnGameCutMoves":
                                menuItem.IsEnabled = view != null && view.HasMovesSelectedForCopy;
                                break;
                            case "UiMnGamePasteMoves":
                                menuItem.IsEnabled = SystemClipboard.HasSerializedData() || !string.IsNullOrEmpty(SystemClipboard.GetText());
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("EnableModelGamesMenus()", ex);
            }
        }

        /// <summary>
        /// Sets up Exercises context menu.
        /// </summary>
        /// <param name="selectedNodeId"></param>
        private static void EnableExercisesMenuItems(int selectedNodeId)
        {
            bool isMate = IsCheckMateOrStalemate(selectedNodeId);

            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                int exerciseCount = chapter.GetExerciseCount();
                int exerciseIndex = chapter.ActiveExerciseIndex;

                bool isTrainingOrSolving = TrainingSession.IsTrainingInProgress || IsUserSolving();
                VariationTreeView view = AppState.MainWin.ActiveTreeView;
                bool isSolutionShown = ActiveVariationTree == null ? false : ActiveVariationTree.ShowTreeLines;

                if (isTrainingOrSolving)
                {
                    MainWin.UiMnExercMarkBookmark.Visibility = Visibility.Collapsed;
                    MainWin.UiMnExercDeleteBookmark.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ConfigureBookmarkMenuOptions(MainWin.UiMnExercMarkBookmark, MainWin.UiMnExercDeleteBookmark);
                }

                foreach (var item in MainWin.UiMncExercises.Items)
                {
                    if (item is MenuItem)
                    {
                        MenuItem menuItem = item as MenuItem;
                        switch (menuItem.Name)
                        {
                            case "UiMnExercExitSolving":
                                menuItem.Visibility = AppState.IsUserSolving() ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "_mnExerc_EditHeader":
                                menuItem.IsEnabled = exerciseIndex >= 0;
                                menuItem.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "_mnExerc_EditPosition":
                                menuItem.IsEnabled = exerciseIndex >= 0;
                                menuItem.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "UiMnMoveExerciseToChapter":
                                menuItem.IsEnabled = exerciseIndex >= 0 && WorkbookManager.SessionWorkbook.GetChapterCount() > 1;
                                menuItem.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "_mnExerc_StartTrainingFromHere":
                                menuItem.IsEnabled = exerciseIndex >= 0 && !isMate;
                                menuItem.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "_mnExerc_CreateExercise":
                                menuItem.IsEnabled = !isMate;
                                menuItem.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "_mnExerc_CopyFen":
                                menuItem.IsEnabled = exerciseIndex >= 0 && ActiveVariationTree != null && isSolutionShown;
                                break;
                            case "_mnExerc_PromoteLine":
                                menuItem.IsEnabled = exerciseIndex >= 0 && selectedNodeId > 0 && isSolutionShown;
                                menuItem.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "_mnExerc_DeleteMovesFromHere":
                            case "UiMnExerc_EvalLine":
                            case "UiMnExerc_EvalMove":
                            case "UiMnExercDontSaveEvals":
                                menuItem.IsEnabled = exerciseIndex >= 0 && selectedNodeId > 0 && isSolutionShown;
                                menuItem.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "_mnExerc_DeleteThisExercise":
                                menuItem.IsEnabled = exerciseIndex >= 0;
                                menuItem.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "UiMnExerc_ShowAllSolutions":
                                menuItem.IsEnabled = exerciseCount > 1 || exerciseCount == 1 && ActiveVariationTree?.ShowTreeLines == false;
                                menuItem.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "UiMnExerc_HideAllSolutions":
                                menuItem.IsEnabled = exerciseCount > 1 || exerciseCount == 1 && ActiveVariationTree?.ShowTreeLines == true;
                                menuItem.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "UiMnExercMarkBookmark":
                                menuItem.IsEnabled = exerciseIndex >= 0 && selectedNodeId > 0 && isSolutionShown;
                                menuItem.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "_mnExerc_MarkThumbnail":
                                menuItem.IsEnabled = exerciseIndex >= 0 && selectedNodeId > 0 && isSolutionShown;
                                menuItem.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "UiMnExercCopyMoves":
                            case "UiMnExercCutMoves":
                                menuItem.IsEnabled = view != null && view.HasMovesSelectedForCopy && isSolutionShown;
                                menuItem.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "UiMnExercPasteMoves":
                                menuItem.IsEnabled = SystemClipboard.HasSerializedData() || !string.IsNullOrEmpty(SystemClipboard.GetText());
                                menuItem.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "UiMnExerc_ImportExercises":
                                menuItem.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "UiMnExercSelectHighlighted":
                            case "UiMnExercSelectSubtree":
                            case "UiMnExercFindIdentical":
                            case "UiMnExercFindPositions":
                                menuItem.IsEnabled = isSolutionShown;
                                menuItem.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                        }
                    }
                    else if (item is Separator)
                    {
                        Separator separ = item as Separator;
                        switch (separ.Name)
                        {
                            case "UiMnExerc_Separator_2":
                            case "UiMnExerc_Separator_3":
                            case "UiMnExerc_Separator_4":
                            case "UiMnExerc_Separator_5":
                            case "UiMnExerc_Separator_6":
                            case "UiMnExerc_Separator_7":
                            case "UiMnExerc_Separator_8":
                            case "UiMnStudyExercSepar_EvalPos":
                            case "UiMnStudyExercSepar_EvalLine":
                                separ.Visibility = isTrainingOrSolving ? Visibility.Collapsed : Visibility.Visible;
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("EnableModelGamesMenus()", ex);
            }
        }

        /// <summary>
        /// Sets up GUI elements for the Manual Review mode.
        /// </summary>
        private static void SetupGuiForManualReview()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                if (Workbook == null)
                {
                    _mainWin.UiMnCloseWorkbook.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _mainWin.UiMnCloseWorkbook.Visibility = Visibility.Visible;
                }

                SetChessboardForActiveTab();

                if (AppState.ActiveContentType == GameData.ContentType.STUDY_TREE && WorkbookManager.ActiveTab == TabViewType.STUDY
                   || WorkbookManager.ActiveTab == TabViewType.MODEL_GAME)
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
                _mainWin.UiTabCtrlEngineGame.Visibility = Visibility.Hidden;
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
                _mainWin.UiTrainingSessionBox.Visibility = Visibility.Collapsed;
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
                GuiConfiguration.ConfigureAppBarFontButtons();

                _mainWin.UiMnCloseWorkbook.Visibility = Visibility.Visible;

                _mainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardGreen;

                //_mainWin.UiDgActiveLine.Visibility = Visibility.Hidden;
                //_mainWin.UiDgEngineGame.Visibility = Visibility.Visible;

                _mainWin.UiLblScoresheet.Visibility = Visibility.Hidden;
                MainWin.ResizeTabControl(MainWin.UiTabCtrlTraining, TabControlSizeMode.SHOW_ENGINE_GAME_LINE);

                _mainWin.UiTabCtrlManualReview.Visibility = Visibility.Hidden;
                _mainWin.UiTabCtrlTraining.Visibility = Visibility.Visible;

                _mainWin.UiTabStudyTree.Visibility = Visibility.Hidden;
                _mainWin.UiTabBookmarks.Visibility = Visibility.Hidden;

                _mainWin.UiTabTrainingProgress.Visibility = Visibility.Visible;

                //_mainWin.UiBtnExitTraining.Visibility = Visibility.Visible;
                _mainWin.UiBtnExitTraining.Visibility = Visibility.Collapsed; // to be deprecated
                _mainWin.UiTrainingSessionBox.Visibility = Visibility.Visible;
                _mainWin.UiBtnExitGame.Visibility = Visibility.Collapsed;

                ShowEvaluationControlsForCurrentStates();
                MainWin.TurnExplorersOff(false);

                ConfigureMenusForTraining();
                _mainWin.UiTrainingSessionBox.ShowElements(false);
            });
        }

        /// <summary>
        /// Sets up GUI elements for Engine game.
        /// The setup is different depending on whether the game
        /// is played during the training session or not (i.e. the user
        /// requested a game against the engine from the Study tab.)
        /// </summary>
        public static void SetupGuiForEngineGame()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                _mainWin.UiMnCloseWorkbook.Visibility = Visibility.Visible;

                GuiConfiguration.ConfigureAppBarFontButtons();

                if (TrainingSession.IsTrainingInProgress)
                {
                    _mainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardGreen;

                    _mainWin.UiDgActiveLine.Visibility = Visibility.Hidden;
                    _mainWin.UiLblScoresheet.Visibility = Visibility.Hidden;
                    _mainWin.UiDgEngineGame.Visibility = Visibility.Visible;

                    // set visibility of UiTabCtrlManualReview first as it triggers an event
                    // that must be run before the event triggerd by UiTabCtrlTraining.Visibility
                    _mainWin.UiTabCtrlManualReview.Visibility = Visibility.Hidden;
                    _mainWin.UiTabStudyTree.Visibility = Visibility.Hidden;
                    _mainWin.UiTabBookmarks.Visibility = Visibility.Hidden;

                    _mainWin.UiTabCtrlTraining.Visibility = Visibility.Visible;
                    _mainWin.UiTabTrainingProgress.Visibility = Visibility.Visible;

                    _mainWin.UiTabCtrlEngineGame.Visibility = Visibility.Hidden;

                    //                    _mainWin.UiBtnExitTraining.Visibility = Visibility.Visible;
                    _mainWin.UiBtnExitTraining.Visibility = Visibility.Collapsed; // to be deprecated
                    _mainWin.UiTrainingSessionBox.Visibility = Visibility.Visible;
                    _mainWin.UiBtnExitGame.Visibility = Visibility.Collapsed;


                    ShowEvaluationControlsForCurrentStates();
                    ConfigureMenusForEngineGame();

                    _mainWin.UiTrainingSessionBox.ShowElements(true);
                    //ShowGuiEngineGameLine(true, MainWin.UiTabCtrlTraining);
                }
                else
                {
                    _mainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardGreen;

                    MainWin.ResizeTabControl(MainWin.UiTabCtrlEngineGame, TabControlSizeMode.SHOW_ENGINE_GAME_LINE);

                    _mainWin.UiDgActiveLine.Visibility = Visibility.Hidden;
                    _mainWin.UiLblScoresheet.Visibility = Visibility.Hidden;
                    _mainWin.UiDgEngineGame.Visibility = Visibility.Visible;

                    // set visibility of UiTabCtrlManualReview first as it triggers an event
                    // that must be run before the event triggerd by UiTabCtrlEngineGame.Visibility
                    _mainWin.UiTabCtrlManualReview.Visibility = Visibility.Hidden;
                    _mainWin.UiTabCtrlEngineGame.Visibility = Visibility.Visible;
                    _mainWin.UiTabCtrlTraining.Visibility = Visibility.Hidden;

                    _mainWin.UiTabTrainingProgress.Visibility = Visibility.Hidden;

                    _mainWin.UiBtnExitTraining.Visibility = Visibility.Collapsed;
                    _mainWin.UiTrainingSessionBox.Visibility = Visibility.Collapsed;
                    _mainWin.UiBtnExitGame.Visibility = Visibility.Visible;

                    _mainWin.UiMnCloseWorkbook.Visibility = Visibility.Visible;


                    _mainWin.UiTabStudyTree.Visibility = Visibility.Hidden;
                    _mainWin.UiTabBookmarks.Visibility = Visibility.Hidden;

                    MainWin.EnableGui(false);

                    //ShowGuiEngineGameLine(true, MainWin.UiTabCtrlEngineGame);
                }
            });
        }

        /// <summary>
        /// Configures menu items for the Manual Review mode
        /// </summary>
        public static void ConfigureMenusForManualReview()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                try
                {
                    GuiConfiguration.ConfigureAppBarFontButtons();

                    MainWin.UiMnStartTraining.IsEnabled = IsVariationTreeTabType;
                    MainWin.UiMnRestartTraining.IsEnabled = false;
                    MainWin.UiMnExitTraining.IsEnabled = false;

                    bool engGameEnabled = ActiveVariationTree != null
                        && (ActiveTab == TabViewType.STUDY || ActiveTab == TabViewType.MODEL_GAME);
                    MainWin.UiMnMainPlayEngine.Visibility = Visibility.Visible;
                    MainWin.UiMnMainPlayEngine.IsEnabled = engGameEnabled;

                    MainWin.UiMnciPlayEngine.IsEnabled = true;

                    MainWin.UiMnEvaluateGames.IsEnabled = AppState.Workbook != null && Workbook.HasAnyModelGames;
                    MainWin.UiMnFindGames.IsEnabled = AppState.Workbook != null && Workbook.IsReady;
                    MainWin.UiMnImportGames.IsEnabled = AppState.Workbook != null && Workbook.IsReady;
                    MainWin.UiMnDeleteComments.IsEnabled = WorkbookManager.SessionWorkbook != null;
                    MainWin.UiMnDeleteEngineEvals.IsEnabled = WorkbookManager.SessionWorkbook != null;
                    MainWin.UiMnAssignECO.IsEnabled = WorkbookManager.SessionWorkbook != null;
                    MainWin.UiMnCopyArticles.IsEnabled = WorkbookManager.SessionWorkbook != null;
                    MainWin.UiMnMoveArticles.IsEnabled = WorkbookManager.SessionWorkbook != null;

                    MainWin.UiMnOrderGames.IsEnabled = AppState.ActiveChapter != null;
                    MainWin.UiMnSetThumbnails.IsEnabled = AppState.ActiveChapter != null;
                    MainWin.UiMnExerciseViewConfig.IsEnabled = AppState.ActiveChapter != null;
                    MainWin.UiMnSplitChapter.IsEnabled = AppState.ActiveChapter != null;
                    

                    MainWin.UiMnAnnotations.IsEnabled = IsTreeViewTabActive();
                    MainWin.UiMnPaste.IsEnabled = !string.IsNullOrEmpty(SystemClipboard.GetText());
                    MainWin.UiMnCommentBeforeMove.IsEnabled = IsTreeViewTabActive();
                    MainWin.UiMnMergeChapters.IsEnabled = WorkbookManager.SessionWorkbook != null && WorkbookManager.SessionWorkbook.GetChapterCount() > 1;

                    MainWin.UiMnMainDeleteGames.IsEnabled = WorkbookManager.SessionWorkbook != null && WorkbookManager.SessionWorkbook.HasAnyModelGames;
                    MainWin.UiMnMainDeleteExercises.IsEnabled = WorkbookManager.SessionWorkbook != null && WorkbookManager.SessionWorkbook.HasAnyExercises;
                    MainWin.UiMnRemoveDuplicates.IsEnabled = WorkbookManager.SessionWorkbook != null &&
                            (WorkbookManager.SessionWorkbook.HasAnyModelGames || WorkbookManager.SessionWorkbook.HasAnyExercises);
                }
                catch (Exception ex)
                {
                    AppLog.Message("ConfigureMenusForManualReview()", ex);
                }
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

                _mainWin.UiMnDeleteComments.IsEnabled = false;
                _mainWin.UiMnDeleteEngineEvals.IsEnabled = false;
                _mainWin.UiMnAssignECO.IsEnabled = false;
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
                MainWin.UiMnAnnotations.IsEnabled = false;
                MainWin.UiMnPaste.IsEnabled = false;
                MainWin.UiMnCommentBeforeMove.IsEnabled = false;
                MainWin.UiMnMergeChapters.IsEnabled = false;

                MainWin.UiMnciBookmarkPosition.IsEnabled = false;
                MainWin.UiMnciDeleteBookmark.Visibility = Visibility.Collapsed;

                _mainWin.UiMnMain_CreateExercise.Visibility = Visibility.Visible;
                _mainWin.UiMnciBookmarkPosition.Visibility = Visibility.Visible;
                _mainWin.UiMnciDeleteBookmark.Visibility = Visibility.Visible;
                _mainWin.UiMnciFindIdentical.Visibility = Visibility.Visible;

                _mainWin.UiMncMainBoardSepar_1.Visibility = Visibility.Visible;
                _mainWin.UiMncMainBoardSepar_1a.Visibility = Visibility.Visible;
                _mainWin.UiMncMainBoardSepar_2.Visibility = Visibility.Visible;
                _mainWin.UiMncMainBoardSepar_3.Visibility = Visibility.Visible;
                _mainWin.UiMncMainBoardSepar_4.Visibility = Visibility.Visible;

                switch (CurrentLearningMode)
                {
                    case LearningMode.Mode.MANUAL_REVIEW:
                        ConfigureMenusForManualReview();
                        _mainWin.UiMnciStartTrainingHere.Visibility = Visibility.Visible;
                        _mainWin.UiMnciRestartTraining.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciExitTraining.Visibility = Visibility.Collapsed;

                        bool engGameEnabled = ActiveVariationTree != null
                            && (ActiveTab == TabViewType.STUDY || ActiveTab == TabViewType.MODEL_GAME);
                        _mainWin.UiMnMainPlayEngine.Visibility = Visibility.Visible;
                        _mainWin.UiMnMainPlayEngine.IsEnabled = engGameEnabled;

                        _mainWin.UiMncMainBoardSepar_1.Visibility = Visibility.Visible;

                        _mainWin.UiMnciEvalPos.Visibility = Visibility.Visible;
                        _mainWin.UiMnciEvalLine.Visibility = Visibility.Visible;
                        _mainWin.UiMnciDontSaveEvals.Visibility = Visibility.Visible;
                        _mainWin.UiMnciSepar_EvalPos.Visibility = Visibility.Visible;
                        _mainWin.UiMnciSepar_EvalLine.Visibility = Visibility.Visible;

                        _mainWin.UiMncMainBoardSepar_2.Visibility = Visibility.Visible;

                        _mainWin.UiMnciReplay.Visibility = Visibility.Visible;

                        _mainWin.UiMncMainBoardSepar_3.Visibility = Visibility.Visible;

                        _mainWin.UiMnciPlayEngine.Visibility = Visibility.Visible;
                        _mainWin.UiMnciExitEngineGame.Visibility = Visibility.Collapsed;

                        ConfigureBookmarkMenuOptions(_mainWin.UiMnciBookmarkPosition, _mainWin.UiMnciDeleteBookmark);

                        break;
                    case LearningMode.Mode.TRAINING:
                        _mainWin.UiMnciStartTrainingHere.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciRestartTraining.Visibility = Visibility.Visible;
                        _mainWin.UiMnciExitTraining.Visibility = Visibility.Visible;
                        _mainWin.UiMnMainPlayEngine.Visibility = Visibility.Collapsed;

                        _mainWin.UiMncMainBoardSepar_1.Visibility = Visibility.Collapsed;

                        _mainWin.UiMnciEvalPos.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciEvalLine.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciDontSaveEvals.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciSepar_EvalPos.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciSepar_EvalLine.Visibility = Visibility.Collapsed;

                        _mainWin.UiMncMainBoardSepar_2.Visibility = Visibility.Collapsed;

                        _mainWin.UiMnciReplay.Visibility = Visibility.Collapsed;

                        _mainWin.UiMncMainBoardSepar_3.Visibility = Visibility.Collapsed;

                        _mainWin.UiMnciPlayEngine.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciExitEngineGame.Visibility = Visibility.Collapsed;

                        _mainWin.UiMnMain_CreateExercise.Visibility = Visibility.Collapsed;
                        break;
                    case LearningMode.Mode.ENGINE_GAME:
                        if (TrainingSession.IsTrainingInProgress)
                        {
                            _mainWin.UiMnciRestartTraining.Visibility = Visibility.Visible;
                            _mainWin.UiMnciExitTraining.Visibility = Visibility.Visible;
                            _mainWin.UiMnMainPlayEngine.Visibility = Visibility.Collapsed;

                            _mainWin.UiMncMainBoardSepar_1.Visibility = Visibility.Collapsed;

                            _mainWin.UiMnciExitEngineGame.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            _mainWin.UiMnciRestartTraining.Visibility = Visibility.Collapsed;
                            _mainWin.UiMnciExitTraining.Visibility = Visibility.Collapsed;
                            _mainWin.UiMnMainPlayEngine.Visibility = Visibility.Collapsed;

                            _mainWin.UiMncMainBoardSepar_1.Visibility = Visibility.Visible;

                            _mainWin.UiMnciExitEngineGame.Visibility = Visibility.Visible;
                        }

                        _mainWin.UiMncMainBoardSepar_1.Visibility = Visibility.Collapsed;
                        _mainWin.UiMncMainBoardSepar_1a.Visibility = Visibility.Collapsed;
                        _mainWin.UiMncMainBoardSepar_2.Visibility = Visibility.Collapsed;
                        _mainWin.UiMncMainBoardSepar_3.Visibility = Visibility.Collapsed;
                        _mainWin.UiMncMainBoardSepar_4.Visibility = Visibility.Collapsed;

                        _mainWin.UiMnciStartTrainingHere.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnMain_CreateExercise.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciBookmarkPosition.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciDeleteBookmark.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciFindIdentical.Visibility = Visibility.Collapsed;

                        _mainWin.UiMnciEvalPos.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciEvalLine.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciDontSaveEvals.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciSepar_EvalPos.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciSepar_EvalLine.Visibility = Visibility.Collapsed;

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
                    _mainWin.UiMnciRestartTraining.Visibility = Visibility.Collapsed;
                    _mainWin.UiMnciExitTraining.Visibility = Visibility.Collapsed;
                    _mainWin.UiMncMainBoardSepar_1.Visibility = Visibility.Collapsed;
                }

            });
        }


        /// <summary>
        /// Configures the Add/Delete bookmark menu options
        /// </summary>
        /// <param name="addBookmark"></param>
        /// <param name="deleteBookmark"></param>
        private static void ConfigureBookmarkMenuOptions(MenuItem addBookmark, MenuItem deleteBookmark)
        {
            TreeNode nd = AppState.GetCurrentNode();

            if (nd == null)
            {
                addBookmark.Visibility = Visibility.Visible;
                addBookmark.IsEnabled = false;

                deleteBookmark.Visibility = Visibility.Collapsed;
            }
            else
            {
                bool hasBookmark = nd.IsBookmark;
                addBookmark.Visibility = hasBookmark ? Visibility.Collapsed : Visibility.Visible;
                addBookmark.IsEnabled = !hasBookmark;

                deleteBookmark.Visibility = hasBookmark ? Visibility.Visible : Visibility.Collapsed;
                deleteBookmark.IsEnabled = hasBookmark;
            }
        }

        /// <summary>
        /// Shows/hides the engine evaluation progress bar, labels,
        /// and the menu items for move and line evaluation.
        /// </summary>
        private static void ShowEvaluationControlsForCurrentStates()
        {
            bool eval = EvaluationManager.IsRunning;

            _mainWin.Dispatcher.Invoke(() =>
             {
                 _mainWin.UiImgEngineOff.IsEnabled = true;
                 if (eval)
                 {
                     if (EngineEvaluationsUpdateble)
                     {
                         _mainWin.UiImgEngineOn.Visibility = Visibility.Visible;
                         _mainWin.UiImgEngineOnGray.Visibility = Visibility.Collapsed;
                     }
                     else
                     {
                         _mainWin.UiImgEngineOn.Visibility = Visibility.Collapsed;
                         _mainWin.UiImgEngineOnGray.Visibility = Visibility.Visible;
                     }
                     _mainWin.UiImgEngineOff.Visibility = Visibility.Collapsed;

                     if (EvaluationManager.CurrentMode == EvaluationManager.Mode.CONTINUOUS
                         && (LearningMode.CurrentMode != LearningMode.Mode.ENGINE_GAME || EngineGame.CurrentState != EngineGame.GameState.ENGINE_THINKING))
                     {
                         _mainWin.UiMnciEvalLine.IsEnabled = true;
                         _mainWin.UiMnciEvalPos.IsEnabled = false;

                         _mainWin.UiPbEngineThinking.Visibility = Visibility.Collapsed;
                     }
                     else
                     {
                         _mainWin.UiMnciEvalLine.IsEnabled = false;
                         _mainWin.UiMnciEvalPos.IsEnabled = false;

                         _mainWin.UiPbEngineThinking.Visibility = Visibility.Visible;
                     }
                 }
                 else
                 {
                     _mainWin.UiImgEngineOn.Visibility = Visibility.Collapsed;
                     _mainWin.UiImgEngineOnGray.Visibility = Visibility.Collapsed;
                     _mainWin.UiImgEngineOff.Visibility = Visibility.Visible;

                     if (MainWin.ActiveLineReplay.IsReplayActive)
                     {
                         _mainWin.UiMnciEvalLine.IsEnabled = false;
                         _mainWin.UiMnciEvalPos.IsEnabled = false;
                     }
                     else
                     {
                         _mainWin.UiMnciEvalLine.IsEnabled = true;
                         _mainWin.UiMnciEvalPos.IsEnabled = true;
                     }

                     _mainWin.UiPbEngineThinking.Visibility = Visibility.Hidden;
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
                if (ActiveContentType == GameData.ContentType.STUDY_TREE && WorkbookManager.ActiveTab == TabViewType.STUDY
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
        /// Resets evaluation controls.
        /// </summary>
        private static void PrepareEvaluationControls()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                _mainWin.UiMnciEvalLine.IsEnabled = false;
                _mainWin.UiMnciEvalPos.IsEnabled = false;

                _mainWin.UiPbEngineThinking.Minimum = 0;
                int moveTime = CurrentLearningMode == LearningMode.Mode.ENGINE_GAME ?
                    Configuration.EngineMoveTime : Configuration.EngineEvaluationTime;
                _mainWin.UiPbEngineThinking.Maximum = moveTime;
                _mainWin.UiPbEngineThinking.Value = 0;
            });
        }

    }
}

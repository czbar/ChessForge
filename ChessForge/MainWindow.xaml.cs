using ChessPosition;
using EngineService;
using GameTree;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Security.Policy;
using System.Diagnostics;
using static ChessForge.AppStateManager;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // prefix use for manu items showing recent files
        public readonly string MENUITEM_RECENT_FILES_PREFIX = "RecentFiles";

        public readonly string APP_NAME = "Chess Forge";

        /// <summary>
        /// The RichTextBox based Study Tree view
        /// </summary>
        private VariationTreeView _studyTreeView;

        /// <summary>
        /// The RichTextBox based Model Game view
        /// </summary>
        private VariationTreeView _modelGameTreeView;

        /// <summary>
        /// The RichTextBox based Exercise view
        /// </summary>
        private VariationTreeView _exerciseTreeView;

        /// <summary>
        /// The Tree view corresponding to the type of the current ActiveVariationTree
        /// </summary>
        public VariationTreeView ActiveTreeView
        {
            get
            {
                GameData.ContentType gt = WorkbookManager.SessionWorkbook.ActiveVariationTree.ContentType;
                switch (gt)
                {
                    case GameData.ContentType.STUDY_TREE:
                        return _studyTreeView;
                    case GameData.ContentType.MODEL_GAME:
                        return _modelGameTreeView;
                    case GameData.ContentType.EXERCISE:
                        return _exerciseTreeView;
                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Sets the selections in the ActiveTreeView as they were stored last.
        /// </summary>
        public void RestoreSelectedLineAndMoveInActiveView()
        {
            VariationTree tree = WorkbookManager.SessionWorkbook.ActiveVariationTree;
            if (tree != null)
            {
                string lineId = tree.SelectedLineId;
                int nodeId = tree.SelectedNodeId;
                ActiveTreeView.SelectLineAndMove(lineId, nodeId);

                ObservableCollection<TreeNode> lineToSelect = tree.SelectLine(lineId);
                SetActiveLine(lineToSelect, nodeId);

                DisplayPosition(tree.SelectedNode);
            }
        }

        /// <summary>
        /// The RichTextBox based Chapters view
        /// </summary>
        private ChaptersView _chaptersView;

        /// <summary>
        /// The RichTextBox based training view
        /// </summary>
        public TrainingView UiTrainingView;

        // width and and height of a square in the main chessboard
        private const int squareSize = 80;

        AnimationState MoveAnimation = new AnimationState();
        public EvaluationManager EvaluationMgr;

        // The main chessboard of the application
        public ChessBoard MainChessBoard;

        /// <summary>
        /// Chessboard shown over moves in different views
        /// </summary>
        public ChessBoardSmall FloatingChessBoard;

        /// <summary>
        /// The RichTextBox based comment box
        /// underneath the main chessbaord.
        /// </summary>
        public CommentBox BoardCommentBox;

        public GameReplay ActiveLineReplay;

        /// <summary>
        /// manages data for the ActiveLine DataGrid
        /// </summary>
        public ActiveLineManager ActiveLine;

        /// <summary>
        /// The workbook for this session
        /// </summary>
        public Workbook SessionWorkbook
        {
            get
            {
                return WorkbookManager.SessionWorkbook;
            }
        }

        /// <summary>
        /// The variation tree currently being processed
        /// </summary>
        public VariationTree ActiveVariationTree
        {
            get
            {
                if (SessionWorkbook == null)
                {
                    return null;
                }
                else
                {
                    return SessionWorkbook.ActiveVariationTree;
                }
            }
        }

        /// <summary>
        /// Determines if the program is running in Debug mode.
        /// </summary>
        private bool _isDebugMode = false;

        /// <summary>
        /// Coordinates of the last right-clicked point
        /// </summary>
        private Point? _lastRightClickedPoint;

        /// <summary>
        /// Collection of timers for this application.
        /// </summary>
        public AppTimers Timers;

        /// <summary>
        /// The main application window.
        /// Initializes the GUI controls.
        /// Note that some of the controls must be initialized
        /// in a particular order as one control may use a reference 
        /// to another one.
        /// </summary>
        public MainWindow()
        {
            AppStateManager.MainWin = this;

            EvaluationMgr = new EvaluationManager();

            InitializeComponent();
            SoundPlayer.Initialize();

            BoardCommentBox = new CommentBox(UiRtbBoardComment.Document, this);
            ActiveLine = new ActiveLineManager(UiDgActiveLine, this);

            EngineLinesBox.Initialize(this, UiTbEngineLines, UiPbEngineThinking);
            Timers = new AppTimers(this);

            Configuration.Initialize(this);
            Configuration.StartDirectory = App.AppPath;
            Configuration.ReadConfigurationFile();
            if (Configuration.IsMainWinPosValid())
            {
                this.Left = Configuration.MainWinPos.Left;
                this.Top = Configuration.MainWinPos.Top;
                this.Width = Configuration.MainWinPos.Right - Configuration.MainWinPos.Left;
                this.Height = Configuration.MainWinPos.Bottom - Configuration.MainWinPos.Top;
            }
            
            DebugUtils.DebugLevel = Configuration.DebugLevel;

            // main chess board
            MainChessBoard = new ChessBoard(MainCanvas, UiImgMainChessboard, null, true, true);

            FloatingChessBoard = new ChessBoardSmall(_cnvFloat, _imgFloatingBoard, null, true, false);


            BookmarkManager.InitBookmarksGui(this);

            ActiveLineReplay = new GameReplay(this, MainChessBoard, BoardCommentBox);

            _isDebugMode = Configuration.DebugLevel != 0;
        }

        /// <summary>
        /// Actions taken after the main window
        /// has been loaded.
        /// In particular, if the last used file can be identified
        /// it will be read in and the session initialized with it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UiDgActiveLine.ContextMenu = UiMnMainBoard;
            AddDebugMenu();

            ResizeTabControl(UiTabCtrlManualReview, TabControlSizeMode.HIDE_ACTIVE_LINE);
            LearningMode.ChangeCurrentMode(LearningMode.Mode.IDLE);
            AppStateManager.SetupGuiForCurrentStates();

            Timers.Start(AppTimers.TimerId.APP_START);
        }

        [Conditional("DEBUG")]
        private void AddDebugMenu()
        {
            MenuItem mnDebug = new MenuItem
            {
                Name = "DebugMenu"
            };

            mnDebug.Header = "Debug";
            UiMainMenu.Items.Add(mnDebug);

            MenuItem mnDebugDump = new MenuItem
            {
                Name = "DebugDumpMenu"
            };

            mnDebugDump.Header = "Dump All";
            mnDebug.Items.Add(mnDebugDump);
            mnDebugDump.Click += UiMnDebugDump_Click;

            MenuItem mnDebugDumpStates = new MenuItem
            {
                Name = "DebugDumpStates"
            };

            mnDebugDumpStates.Header = "Dump States and Timers";
            mnDebug.Items.Add(mnDebugDumpStates);
            mnDebugDumpStates.Click += UiMnDebugDumpStates_Click;
        }

        // tracks the application start stage
        private int _appStartStage = 0;

        // lock object to use during the startup process
        private object _appStartLock = new object();

        /// <summary>
        /// This method controls the two important stages of the startup process.
        /// When the Appstart timer invokes it for the first time, the engine
        /// will be loaded while the timer is stopped.
        /// The second time it is invoked, it will read the most recent file
        /// if such file exists.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void AppStartTimeUp(object source, ElapsedEventArgs e)
        {
            lock (_appStartLock)
            {

                if (_appStartStage == 0)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        BoardCommentBox.StartingEngine();
                    });
                    _appStartStage = 1;
                    Timers.Stop(AppTimers.TimerId.APP_START);
                    EngineMessageProcessor.CreateEngineService(this, _isDebugMode);
                    Timers.Start(AppTimers.TimerId.APP_START);
                }
                else if (_appStartStage == 1)
                {
                    _appStartStage = 2;
                    this.Dispatcher.Invoke(() =>
                    {
                        CreateRecentFilesMenuItems();
                        Timers.Stop(AppTimers.TimerId.APP_START);
                        bool engineStarted = EngineMessageProcessor.StartEngineService();
                        Timers.Start(AppTimers.TimerId.APP_START);
                        if (!engineStarted)
                        {
                            MessageBox.Show("Failed to load the engine. Move evaluation will not be available.", "Chess Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        // if we have LastWorkbookFile or a name on the command line
                        // we will try to open
                        string cmdLineFile = App.CmdLineFileName;
                        bool success = false;
                        if (!string.IsNullOrEmpty(cmdLineFile))
                        {
                            try
                            {
                                ReadWorkbookFile(cmdLineFile, true, ref WorkbookManager.VariationTreeList);
                                success = true;
                            }
                            catch
                            {
                                success = false;
                            }
                        }

                        if (!success)
                        {
                            string lastWorkbookFile = Configuration.LastWorkbookFile;

                            if (!string.IsNullOrEmpty(lastWorkbookFile))
                            {
                                try
                                {
                                    ReadWorkbookFile(lastWorkbookFile, true, ref WorkbookManager.VariationTreeList);
                                }
                                catch
                                {
                                }
                            }
                            else
                            {
                                BoardCommentBox.OpenFile();
                            }
                        }
                    });
                }
            }

            if (_appStartStage == 2)
            {
                Timers.Stop(AppTimers.TimerId.APP_START);
            }
        }

        /// <summary>
        /// Selects the chapter.
        /// </summary>
        /// <param name="chapterId"></param>
        public void SelectChapter(int chapterId, bool focusOnStudyTree)
        {
            if (chapterId >= 0)
            {
                WorkbookManager.SessionWorkbook.SetActiveChapterTreeById(chapterId, GameData.ContentType.STUDY_TREE);
                ClearTabViews();
                _chaptersView.HighlightActiveChapter();
                SetupGuiForActiveStudyTree(focusOnStudyTree);
            }
        }

        /// <summary>
        /// Clears the TreeViews' douments in the tab control.
        /// </summary>
        private void ClearTabViews()
        {
            ClearTreeView(_studyTreeView);
            ClearTreeView(_modelGameTreeView);
            ClearTreeView(_exerciseTreeView);
        }

        /// <summary>
        /// Clears the passed VariationTreeView
        /// </summary>
        /// <param name="view"></param>
        private void ClearTreeView(VariationTreeView view)
        {
            if (view != null)
            {
                view.Clear();
            }
        }


        /// <summary>
        /// Select and activate view for the model game in the ActiveChapter
        /// at the passed index.
        /// </summary>
        /// <param name="gameIndex"></param>
        public void SelectModelGame(int gameIndex, bool setFocus)
        {
            try
            {
                if (gameIndex >= 0 && gameIndex < WorkbookManager.SessionWorkbook.ActiveChapter.GetModelGameCount())
                {
                    WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex = gameIndex;
                    WorkbookManager.SessionWorkbook.ActiveChapter.SetActiveVariationTree(GameData.ContentType.MODEL_GAME, gameIndex);

                    MainChessBoard.FlipBoard(false);
                    WorkbookManager.SessionWorkbook.IsModelGameBoardFlipped = null;

                    SetupGuiForActiveModelGame(gameIndex, setFocus);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in SelectModelGame(): " + ex.Message);
            }
        }

        /// <summary>
        /// Select and activate view for the exercise in the ActiveChapter
        /// at the passed index.
        /// </summary>
        /// <param name="exerciseIndex"></param>
        public void SelectExercise(int exerciseIndex, bool setFocus)
        {
            WorkbookManager.SessionWorkbook.ActiveChapter.ActiveExerciseIndex = exerciseIndex;
            WorkbookManager.SessionWorkbook.ActiveChapter.SetActiveVariationTree(GameData.ContentType.EXERCISE, exerciseIndex);

            MainChessBoard.FlipBoard(false);
            WorkbookManager.SessionWorkbook.IsExerciseBoardFlipped = null;

            SetupGuiForActiveExercise(exerciseIndex, setFocus);
        }

        /// <summary>
        /// Creates menu items for the Recent Files and 
        /// adds them to the File menu.
        /// </summary>
        private void CreateRecentFilesMenuItems()
        {
            List<string> recentFiles = Configuration.RecentFiles;
            for (int i = 0; i < recentFiles.Count; i++)
            {
                MenuItem mi = new MenuItem
                {
                    Name = MENUITEM_RECENT_FILES_PREFIX + i.ToString()
                };
                try
                {
                    string fileName = Path.GetFileName(recentFiles.ElementAt(i));
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        mi.Header = fileName;
                        MenuFile.Items.Add(mi);
                        mi.Click += OpenRecentWorkbookFile;
                    }
                }
                catch { };
            }
        }

        /// <summary>
        /// Returns the flipped state of the Main Chessboard
        /// </summary>
        /// <returns></returns>
        public bool IsMainChessboardFlipped()
        {
            return MainChessBoard.IsFlipped;
        }

        /// <summary>
        /// Determined the color of the arrow to be drawn based
        /// on the special key pressed and calls to BoardArrowsManager
        /// to do the drawing.
        /// </summary>
        /// <param name="sq"></param>
        private void StartShapeDraw(SquareCoords sq, bool isTentative)
        {
            string color = "yellow";

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                color = Constants.COLOR_YELLOW;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                color = Constants.COLOR_RED;
            }
            else if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                color = Constants.COLOR_BLUE;
            }
            else
            {
                color = Constants.COLOR_GREEN;
            }

            BoardShapesManager.StartShapeDraw(sq, color, isTentative);
        }

        /// <summary>
        /// Saves the Arrow positions string to the Node currently
        /// hosted in the Main Chessboard.
        /// </summary>
        /// <param name="arrowsString"></param>
        /// <return>whether the new string is different</return>
        public bool SaveArrowsStringInCurrentNode(string arrowsString)
        {
            if (MainChessBoard != null)
            {
                TreeNode nd = MainChessBoard.DisplayedNode;
                if (nd != null && nd.Arrows != arrowsString)
                {
                    nd.Arrows = arrowsString;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Saves the Circle positions string to the Node currently
        /// hosted in the Main Chessboard.
        /// </summary>
        /// <param name="circlesString"></param>
        public bool SaveCirclesStringInCurrentNode(string circlesString)
        {
            if (MainChessBoard != null)
            {
                TreeNode nd = MainChessBoard.DisplayedNode;
                if (nd != null && nd.Circles != circlesString)
                {
                    nd.Circles = circlesString;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the clicked piece is eligible for making a move.
        /// </summary>
        /// <param name="sqNorm"></param>
        /// <returns></returns>
        private bool CanMovePiece(SquareCoords sqNorm)
        {
            PieceColor pieceColor = MainChessBoard.GetPieceColor(sqNorm);

            // in the Manual Review, the color of the piece on the main board must match the side on the move in the selected position
            if (LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                TreeNode nd = ActiveLine.GetSelectedTreeNode();
                if (nd == null)
                {
                    nd = ActiveVariationTree.Nodes[0];
                }

                if (pieceColor != PieceColor.None && pieceColor == nd.ColorToMove)
                    return true;
                else
                    return false;
            }
            else if (LearningMode.CurrentMode == LearningMode.Mode.ENGINE_GAME && EngineGame.CurrentState == EngineGame.GameState.USER_THINKING
                || LearningMode.CurrentMode == LearningMode.Mode.TRAINING && TrainingSession.CurrentState == TrainingSession.State.AWAITING_USER_TRAINING_MOVE && !TrainingSession.IsBrowseActive)
            {
                if (EngineGame.GetPieceColor(sqNorm) == EngineGame.ColorToMove)
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Shows a GUI element allowing the user 
        /// to select the piece to promote to.
        /// </summary>
        /// <param name="normTarget">Normalized propmotion square coordinates
        /// i.e. 0 is for Black and 7 is for White promotion.</param>
        /// <returns></returns>
        public PieceType GetUserPromoSelection(SquareCoords normTarget)
        {
            bool whitePromotion = normTarget.Ycoord == 7;
            PromotionDialog dlg = new PromotionDialog(whitePromotion);

            Point pos = CalculatePromoDialogLocation(normTarget, whitePromotion);
            dlg.Left = pos.X;
            dlg.Top = pos.Y;
            dlg.ShowDialog();

            return dlg.SelectedPiece;
        }

        /// <summary>
        /// Given the promotion square in the normalized
        /// form (i.e. ignoring a possible chessboard flip),
        /// works out the Left and Top position of the Promotion
        /// dialog.
        /// The dialog should fit entirely within the board and its boarders and should
        /// nicely overlap with the promotion square.
        /// </summary>
        /// <param name="normTarget"></param>
        /// <returns></returns>
        private Point CalculatePromoDialogLocation(SquareCoords normTarget, bool whitePromotion)
        {
            //TODO: this is far from ideal.
            // We need to find a better way of calulating the position against
            // the chessboard
            Point leftTop = new Point();
            if (!MainChessBoard.IsFlipped)
            {
                leftTop.X = ChessForgeMain.Left + ChessForgeMain.UiImgMainChessboard.Margin.Left + 20 + normTarget.Xcoord * 80;
                if (whitePromotion)
                {
                    leftTop.Y = ChessForgeMain.Top + ChessForgeMain.UiImgMainChessboard.Margin.Top + 40 + (7 - normTarget.Ycoord) * 80;
                }
                else
                {
                    leftTop.Y = ChessForgeMain.Top + ChessForgeMain.UiImgMainChessboard.Margin.Top + 40 + (3 - normTarget.Ycoord) * 80;
                }
            }
            else
            {
                leftTop.X = ChessForgeMain.Left + ChessForgeMain.UiImgMainChessboard.Margin.Left + 20 + (7 - normTarget.Xcoord) * 80;
                if (whitePromotion)
                {
                    leftTop.X = ChessForgeMain.Top + ChessForgeMain.UiImgMainChessboard.Margin.Top + 40 + (normTarget.Ycoord - 4) * 80;
                }
                else
                {
                    leftTop.X = ChessForgeMain.Top + ChessForgeMain.UiImgMainChessboard.Margin.Top + 40 + (normTarget.Ycoord) * 80;
                }
            }

            return leftTop;
        }

        /// <summary>
        /// Invoked from the menu item File->Close Workbook
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnCloseWorkbook_Click(object sender, RoutedEventArgs e)
        {
            WorkbookManager.AskToSaveWorkbookOnClose();
        }

        /// <summary>
        /// Returns true if user accept the change. of mode.
        /// </summary>
        /// <param name="newMode"></param>
        /// <returns></returns>
        private bool ChangeAppModeWarning(LearningMode.Mode newMode)
        {
            if (LearningMode.CurrentMode == LearningMode.Mode.IDLE)
            {
                // it is a fresh state, no need for any warnings
                return true;
            }

            bool result = false;
            // we may not be changing the mode, but changing
            // the variation tree we are working with.
            if (LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW && newMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                // TODO: ask what to do with the current tree
                // abandon, save, put aside
                result = true;
            }
            else if (LearningMode.CurrentMode != LearningMode.Mode.MANUAL_REVIEW && newMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                switch (LearningMode.CurrentMode)
                {
                    case LearningMode.Mode.ENGINE_GAME:
                        if (MessageBox.Show("Cancel Game?", "Game with the Computer is in Progress", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            result = true;
                        break;
                    default:
                        result = true;
                        break;
                }
            }
            else
            {
                return true;
            }

            return result;
        }

        /// <summary>
        /// Recreates the "Recent Files" menu items by
        /// removing the exisiting ones and inserting
        /// ones corresponding to what's in the configuration file.
        /// </summary>
        public void RecreateRecentFilesMenuItems()
        {
            List<object> itemsToRemove = new List<object>();

            for (int i = 0; i < MenuFile.Items.Count; i++)
            {
                if (MenuFile.Items[i] is MenuItem item)
                {
                    if (item.Name.StartsWith(MENUITEM_RECENT_FILES_PREFIX))
                    {
                        itemsToRemove.Add(item);
                    }
                }
            }

            foreach (MenuItem item in itemsToRemove.Cast<MenuItem>())
            {
                MenuFile.Items.Remove(item);
            }

            CreateRecentFilesMenuItems();
        }

        /// <summary>
        /// Reads in the file and builds the internal structures for the entire content.
        /// Chess Forge files are PGN files with special headers identifying them as such.
        /// A .chf file is a legacy Chess Forge file that will be read in as a single, variation tree - only, chapter.
        /// We will also read non - Chess Forge .pgn files and process/convert them.
        /// </summary>
        /// <param name="fileName">path to the file</param>
        /// <param name="isLastOpen">were we asked to open the file that was open last in the previous session</param>
        private void ReadWorkbookFile(string fileName, bool isLastOpen, ref ObservableCollection<GameData> GameList)
        {
            try
            {
                if (!WorkbookManager.CheckFileExists(fileName, isLastOpen))
                {
                    return;
                }

                AppStateManager.RestartInIdleMode(false);
                AppStateManager.WorkbookFilePath = fileName;
                BoardCommentBox.ReadingFile();

                string fileExtension = Path.GetExtension(fileName).ToLower();

                bool acceptFile = false;
                bool isChessForgeFile = false;

                switch (fileExtension)
                {
                    case ".chf":
                        acceptFile = WorkbookManager.ReadLegacyChfFile(fileName);
                        isChessForgeFile = true;
                        break;
                    case ".pgn":
                        WorkbookManager.ReadPgnFile(fileName, ref GameList, GameData.ContentType.GENERIC);
                        bool res = WorkbookManager.PrepareWorkbook(ref GameList, out isChessForgeFile);
                        if (res)
                        {
                            WorkbookManager.AssignChaptersIds();
                            acceptFile = true;
                        }
                        else
                        {
                            acceptFile = false;
                        }
                        break;
                    default:
                        MessageBox.Show("Unrecognized file format: " + fileName, "Input File", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        acceptFile = false;
                        break;
                }

                if (acceptFile)
                {
                    SetupGuiForNewSession(AppStateManager.WorkbookFilePath, isChessForgeFile);
                }
                else
                {
                    AppStateManager.RestartInIdleMode(true);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error processing input file", MessageBoxButton.OK, MessageBoxImage.Error);
                AppStateManager.RestartInIdleMode();
            }
        }

        /// <summary>
        /// Sets up GUI views and components for the start of
        /// a new session in the MANUAL_REVIEW learning mode.
        /// </summary>
        /// <param name="fileName"></param>
        private void SetupGuiForNewSession(string fileName, bool isChessForgeFile = true)
        {
            // if we are here, the WorkbookFileName must have been updated
            // and the WorkbookFileType was set to CHESS_FORGE_PGN 

            // if this is a new session we will set ActiveChapter to the first chapter
            // and Active Tree to the Study Tree in that chapter.
            WorkbookManager.SessionWorkbook.SetActiveChapterTreeByIndex(0, GameData.ContentType.STUDY_TREE);
            AppStateManager.UpdateAppTitleBar();
            BoardCommentBox.ShowWorkbookTitle();

            if (SessionWorkbook.TrainingSide == PieceColor.None)
            {
                ShowWorkbookOptionsDialog(false);
            }

            if (SessionWorkbook.TrainingSide == PieceColor.White && MainChessBoard.IsFlipped || SessionWorkbook.TrainingSide == PieceColor.Black && !MainChessBoard.IsFlipped)
            {
                MainChessBoard.FlipBoard();
            }

            if (isChessForgeFile)
            {
                WorkbookManager.UpdateRecentFilesList(fileName);
            }

            BoardCommentBox.ShowWorkbookTitle();
            InitializeChaptersView();

            SetupGuiForActiveStudyTree(false);

            LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);
            SetupGuiForChapters();
        }

        /// <summary>
        /// Sets up the data and GUI for the ActiveStudyTree.
        /// This method will be called e.g. when opening a new
        /// Workbook and initializing the view.
        /// </summary>
        public void SetupGuiForActiveStudyTree(bool focusOnStudyTree)
        {
            _studyTreeView = new VariationTreeView(UiRtbStudyTreeView.Document, this);
            if (ActiveVariationTree.Nodes.Count == 0)
            {
                ActiveVariationTree.CreateNew();
            }
            else
            {
                ActiveVariationTree.BuildLines();
            }

            _studyTreeView.BuildFlowDocumentForVariationTree();

            string startLineId;
            int startNodeId = 0;

            startLineId = ActiveVariationTree.GetDefaultLineIdForNode(0);

            ActiveVariationTree.SelectedLineId = startLineId;
            ActiveVariationTree.SelectedNodeId = startNodeId;

            if (focusOnStudyTree)
            {
                UiTabStudyTree.Focus();
                UiRtbStudyTreeView.Focus();
            }
            else
            {
                // in the above branch this will be executed by the Focus() methods.
                SetActiveLine(startLineId, startNodeId);
            }

            BookmarkManager.ShowBookmarks();

            int nodeIndex = ActiveLine.GetIndexForNode(startNodeId);
            SelectLineAndMoveInWorkbookViews(_studyTreeView, startLineId, nodeIndex);
        }

        /// <summary>
        /// Sets up the data and GUI for the ActiveTree of Model Game.
        /// This method will be called e.g. when opening a new
        /// Workbook and initializing the view.
        /// </summary>
        public void SetupGuiForActiveModelGame(int gameIndex, bool focusOnModelGame)
        {
            _modelGameTreeView = new VariationTreeView(UiRtbModelGamesView.Document, this);
            if (ActiveVariationTree.Nodes.Count == 0)
            {
                ActiveVariationTree.CreateNew();
            }
            else
            {
                ActiveVariationTree.BuildLines();
            }

            _modelGameTreeView.BuildFlowDocumentForVariationTree();

            string startLineId;
            int startNodeId = 0;

            if (!string.IsNullOrEmpty(ActiveVariationTree.SelectedLineId) && ActiveVariationTree.SelectedNodeId >= 0)
            {
                startLineId = ActiveVariationTree.SelectedLineId;
                startNodeId = ActiveVariationTree.SelectedNodeId;
            }
            else
            {
                startLineId = ActiveVariationTree.GetDefaultLineIdForNode(0);
            }

            ActiveVariationTree.SelectedLineId = startLineId;
            ActiveVariationTree.SelectedNodeId = startNodeId;

            if (focusOnModelGame)
            {
                UiTabModelGames.Focus();
                UiRtbModelGamesView.Focus();
            }
            else
            {
                // in the above branch this will be executed by the Focus() methods.
                SetActiveLine(startLineId, startNodeId);
            }

            //BookmarkManager.ShowBookmarks();

            int nodeIndex = ActiveLine.GetIndexForNode(startNodeId);
            SelectLineAndMoveInWorkbookViews(_modelGameTreeView, startLineId, nodeIndex);
        }

        /// <summary>
        /// Activates the Chapters tab.
        /// </summary>
        public void SetupGuiForChapters()
        {
            UiTabChapters.Focus();
        }

        /// <summary>
        /// Sets up the data and GUI for the ActiveTree of Exercise Game.
        /// This method will be called e.g. when opening a new
        /// Workbook and initializing the view.
        /// </summary>
        public void SetupGuiForActiveExercise(int gameIndex, bool focusOnExercise)
        {
            _exerciseTreeView = new VariationTreeView(UiRtbExercisesView.Document, this);
            UiRtbExercisesView.IsDocumentEnabled = true;

            if (ActiveVariationTree.Nodes.Count == 0)
            {
                ActiveVariationTree.CreateNew();
            }
            else
            {
                ActiveVariationTree.BuildLines();
            }

            _exerciseTreeView.BuildFlowDocumentForVariationTree();

            string startLineId;
            int startNodeId = 0;

            if (!string.IsNullOrEmpty(ActiveVariationTree.SelectedLineId) && ActiveVariationTree.SelectedNodeId >= 0)
            {
                startLineId = ActiveVariationTree.SelectedLineId;
                startNodeId = ActiveVariationTree.SelectedNodeId;
            }
            else
            {
                startLineId = ActiveVariationTree.GetDefaultLineIdForNode(0);
            }

            ActiveVariationTree.SelectedLineId = startLineId;
            ActiveVariationTree.SelectedNodeId = startNodeId;

            if (focusOnExercise)
            {
                UiTabExercises.Focus();
                UiRtbExercisesView.Focus();
            }
            else
            {
                // in the above branch this will be executed by the Focus() methods.
                SetActiveLine(startLineId, startNodeId);
            }

            //BookmarkManager.ShowBookmarks();

            int nodeIndex = ActiveLine.GetIndexForNode(startNodeId);
            SelectLineAndMoveInWorkbookViews(_exerciseTreeView, startLineId, nodeIndex);
        }

        /// <summary>
        /// Initializes the ChaptersView
        /// </summary>
        private void InitializeChaptersView()
        {
            _chaptersView = new ChaptersView(UiRtbChaptersView.Document, this);
            _chaptersView.BuildFlowDocumentForChaptersView();

        }

        /// <summary>
        /// Rebuilds the entire Workbook View
        /// </summary>
        public void RebuildActiveTreeView()
        {
            ActiveTreeView.BuildFlowDocumentForVariationTree();
        }

        /// <summary>
        /// Obtains the current ActiveLine's LineId and move,
        /// and asks other view to select / re-select.
        /// This is needed e.g. when the WorkbookTree is rebuilt after
        /// adding nodes.
        /// </summary>
        public void RefreshSelectedActiveLineAndNode()
        {
            string lineId = ActiveLine.GetLineId();
            SelectLineAndMoveInWorkbookViews(ActiveTreeView, lineId, ActiveLine.GetSelectedPlyNodeIndex(true));
        }

        /// <summary>
        /// Adds a new Node to the Workbook View,
        /// avoiding the full rebuild (performance).
        /// This can only be done "safely" if we are adding a move to a leaf.
        /// </summary>
        /// <param name="nd"></param>
        public void AddNewNodeToVariationTreeView(TreeNode nd)
        {
            if (ActiveVariationTree.ShowTreeLines)
            {
                ActiveTreeView.AddNewNodeToDocument(nd);
            }
        }

        /// <summary>
        /// Selects a line and move in the VariationTree view.
        /// </summary>
        /// <param name="lineId"></param>
        /// <param name="index"></param>
        public void SelectLineAndMoveInWorkbookViews(VariationTreeView view, string lineId, int index)
        {
            TreeNode nd = ActiveLine.GetNodeAtIndex(index);
            if (nd != null)
            {
                WorkbookManager.SessionWorkbook.ActiveVariationTree.SetSelectedLineAndMove(lineId, nd.NodeId);
                view.SelectLineAndMove(lineId, nd.NodeId);
                if (EvaluationManager.CurrentMode == EvaluationManager.Mode.CONTINUOUS)
                {
                    EvaluateActiveLineSelectedPosition(nd);
                }
            }
        }

        private MessageBoxResult AskToGenerateBookmarks()
        {
            return MessageBox.Show("Would you like to auto-select positions for training?",
                "No Bookmarks in this Workbook", MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        public void SetActiveLine(string lineId, int selectedNodeId, bool displayPosition = true)
        {
            ObservableCollection<TreeNode> line = ActiveVariationTree.SelectLine(lineId);
            SetActiveLine(line, selectedNodeId, displayPosition);
        }

        /// <summary>
        /// Displays the position of the passed node
        /// and any associated arrows or circles.
        /// </summary>
        /// <param name="nd"></param>
        public void DisplayPosition(TreeNode nd)
        {
            MainChessBoard.DisplayPosition(nd);
        }

        /// <summary>
        /// Displays the passed position.
        /// Will not show arrows and circles if associated with this position.
        /// </summary>
        /// <param name="nd"></param>
        public void DisplayPosition(BoardPosition pos)
        {
            MainChessBoard.DisplayPosition(null, pos);
        }

        public void RemoveMoveSquareColors()
        {
            MainChessBoard.RemoveMoveSquareColors();
        }

        /// <summary>
        /// Sets data and selection for the Active Line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="selectedNodeId"></param>
        /// <param name="displayPosition"></param>
        public void SetActiveLine(ObservableCollection<TreeNode> line, int selectedNodeId, bool displayPosition = true)
        {
            ActiveLine.SetNodeList(line);

            if (selectedNodeId >= 0)
            {
                TreeNode nd = ActiveLine.GetNodeFromId(selectedNodeId);
                if (selectedNodeId > 0)
                {
                    ActiveLine.SelectPly((int)nd.Parent.MoveNumber, nd.Parent.ColorToMove);
                }
                if (displayPosition)
                {
                    MainChessBoard.DisplayPosition(nd);
                }
                if (EvaluationManager.CurrentMode == EvaluationManager.Mode.CONTINUOUS)
                {
                    EvaluateActiveLineSelectedPosition(nd);
                }
            }
        }

        /// <summary>
        /// Appends a new node to the Active Line.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="displayPosition"></param>
        public void AppendNodeToActiveLine(TreeNode nd, bool displayPosition = true)
        {
            if (nd.NodeId > 0)
            {
                ActiveLine.Line.AddPlyAndMove(nd);
                ActiveLine.SelectPly((int)nd.Parent.MoveNumber, nd.Parent.ColorToMove);
                if (displayPosition)
                {
                    MainChessBoard.DisplayPosition(nd);
                }
            }
        }

        /// <summary>
        /// Writes out all logs.
        /// If userRequested == true, this was requested via the menu
        /// and we dump everything with distinct file names.
        /// Otherwise we only dump app and engine logs, ovewriting previous
        /// logs.
        /// </summary>
        /// <param name="userRequested"></param>
        public void DumpDebugLogs(bool userRequested)
        {
            string distinct = null;

            if (userRequested)
            {
                distinct = "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                AppLog.DumpVariationTree(DebugUtils.BuildLogFileName(App.AppPath, "wktree", distinct), ActiveVariationTree);
                AppLog.DumpStatesAndTimers(DebugUtils.BuildLogFileName(App.AppPath, "timest", distinct));
            }

            try
            {
                AppLog.Dump(DebugUtils.BuildLogFileName(App.AppPath, "applog", distinct));
                EngineLog.Dump(DebugUtils.BuildLogFileName(App.AppPath, "engine", distinct));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dump logs exception: " + ex.Message, "DEBUG", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }


        public void DumpDebugStates()
        {
            string distinct = "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            AppLog.DumpStatesAndTimers(DebugUtils.BuildLogFileName(App.AppPath, "timest", distinct));
        }

        private void EvaluateActiveLineSelectedPosition()
        {
            TreeNode nd = ActiveLine.GetSelectedTreeNode();
            if (nd == null)
            {
                nd = ActiveVariationTree.Nodes[0];
            }
            EvaluationManager.SetSingleNodeToEvaluate(nd);
            // stop the timer to prevent showing garbage after position is set but engine has not received our commands yet
            EngineMessageProcessor.RequestPositionEvaluation(nd, Configuration.EngineMpv, 0);
        }

        private void EvaluateActiveLineSelectedPosition(TreeNode nd)
        {
            EvaluationManager.SetSingleNodeToEvaluate(nd);
            EngineMessageProcessor.RequestPositionEvaluation(nd, Configuration.EngineMpv, 0);
        }

        public void UpdateLastMoveTextBox(TreeNode nd)
        {
            string moveTxt = MoveUtils.BuildSingleMoveText(nd, true);

            UpdateLastMoveTextBox(moveTxt);
        }

        public void UpdateLastMoveTextBox(int posIndex)
        {
            string moveTxt = EvaluationManager.GetEvaluatedNode(out _).Position.MoveNumber.ToString()
                    + (EvaluationManager.GetEvaluatedNode(out _).Position.ColorToMove == PieceColor.Black ? "." : "...")
                    + ActiveLine.GetNodeAtIndex(posIndex).LastMoveAlgebraicNotation;

            UpdateLastMoveTextBox(moveTxt);
        }

        /// <summary>
        /// Sets text for the label showing the last/current
        /// move (depending on the context it can be e.g. the move being evaluated).
        /// </summary>
        /// <param name="moveTxt"></param>
        public void UpdateLastMoveTextBox(string moveTxt)
        {
            UiLblMoveUnderEval.Dispatcher.Invoke(() =>
            {
                UiLblMoveUnderEval.Content = moveTxt;
            });
        }

        public void ResetEvaluationProgressBar()
        {
            EngineLinesBox.ResetEvaluationProgressBar();
        }

        /// <summary>
        /// If in training mode, we want to keep the evaluation lines
        /// visible in the comment box, and display the response moves
        /// with their line evaluations in the Training tab.
        /// </summary>
        public void MoveEvaluationFinishedInTraining(TreeNode nd)
        {
            AppStateManager.ShowMoveEvaluationControls(false, true);
            UiTrainingView.ShowEvaluationResult(nd);
        }

        /// <summary>
        /// This method will start a game vs the engine.
        /// It will be called in one of two possible contexts:
        /// either the game was requested from MANUAL_REVIEW
        /// or during TRAINING.
        /// If the latter, then the EngineGame has already been
        /// constructed and we start from the last move/ply.
        /// </summary>
        /// <param name="startNode"></param>
        public void StartEngineGame(TreeNode startNode, bool IsTraining)
        {
            if (!EngineMessageProcessor.IsEngineAvailable)
            {
                MessageBox.Show("Chess Engine not available", "Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            UiImgMainChessboard.Source = ChessBoards.ChessBoardGreen;

            LearningMode.ChangeCurrentMode(LearningMode.Mode.ENGINE_GAME);

            // TODO: should make a call to SetupGUI for game, instead
            AppStateManager.ShowMoveEvaluationControls(false, false);

            EngineGame.InitializeGameObject(startNode, true, IsTraining);
            UiDgEngineGame.ItemsSource = EngineGame.Line.MoveList;

            if (startNode.ColorToMove == PieceColor.White)
            {
                if (!MainChessBoard.IsFlipped)
                {
                    MainChessBoard.FlipBoard();
                }
            }

            EngineMessageProcessor.RequestEngineMove(startNode.Position);
        }

        /// <summary>
        /// This method will be invoked periodically by the 
        /// timer checking for the completion of user moves.
        /// The user can make moves in 2 contexts:
        /// 1. a game against the engine (in this case EngineGame.State 
        /// should already be set to ENGINE_THINKING)
        /// 2. a user entered the move as part of training and we will
        /// provide them a feedback based on the content of the workbook.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void CheckForUserMoveTimerEvent(object source, ElapsedEventArgs e)
        {
            if (TrainingSession.IsTrainingInProgress && LearningMode.CurrentMode != LearningMode.Mode.ENGINE_GAME)
            {
                if ((TrainingSession.CurrentState == TrainingSession.State.USER_MOVE_COMPLETED))
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
                        UiTrainingView.ReportLastMoveVsWorkbook();
                    });
                }
            }
            else // this is a game user vs engine then
            {
                // check if the user move was completed and if so request engine move
                if (EngineGame.CurrentState == EngineGame.GameState.ENGINE_THINKING)
                {
                    Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
                    EngineMessageProcessor.RequestEngineMove(EngineGame.GetLastPosition());
                }
            }
        }

        /// <summary>
        /// Reset controls and restore selection in the ActiveLine
        /// control.
        /// We are going back to the MANUAL REVIEW mode
        /// so Active Line view will be shown.
        /// </summary>
        public void StopEngineGame()
        {
            Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);

            ResetEvaluationProgressBae();

            MainChessBoard.RemoveMoveSquareColors();

            EvaluationManager.Reset();
            EngineMessageProcessor.StopEngineEvaluation();
            LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);
            EngineGame.ChangeCurrentState(EngineGame.GameState.IDLE);

            Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);

            AppStateManager.MainWin.ActiveVariationTree.BuildLines();
            RebuildActiveTreeView();

            AppStateManager.SetupGuiForCurrentStates();

            ActiveLine.DisplayPositionForSelectedCell();
            AppStateManager.SwapCommentBoxForEngineLines(false);
            BoardCommentBox.RestoreTitleMessage();
        }

        /// <summary>
        /// Resets the engine evaluation progress bar.
        /// Sets its visibility to hidden.
        /// and Maximum value to the appropriate engine time: move or evaluation.
        /// </summary>
        public void ResetEvaluationProgressBae()
        {
            UiPbEngineThinking.Dispatcher.Invoke(() =>
            {
                UiPbEngineThinking.Visibility = Visibility.Hidden;
                UiPbEngineThinking.Minimum = 0;

                int moveTime = AppStateManager.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME ?
                    Configuration.EngineMoveTime : Configuration.EngineEvaluationTime;
                UiPbEngineThinking.Maximum = moveTime;
                UiPbEngineThinking.Value = 0;
            });

        }

        /// <summary>
        /// Main Window received a Key Down event.
        /// If we are in Manual Review, pass it on to the ActiveLine.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChessForgeMain_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                ActiveLine.PreviewKeyDown(sender, e);
            }
        }

        /// <summary>
        /// A key pressed event has been received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbStudyTree_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Hand it off to the ActiveLine view.
            // In the future we may want to handle some key strokes here
            // but for now we will respond to whatever the ActiveLine view will request.
            ActiveLine.PreviewKeyDown(sender, e);
        }

        /// <summary>
        /// A key pressed event has been received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabModelGames_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ActiveLine.PreviewKeyDown(sender, e);
        }

        /// <summary>
        /// A key pressed event has been received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabExercises_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ActiveLine.PreviewKeyDown(sender, e);
        }

        /// <summary>
        /// Starts a training session from the specified bookmark position.
        /// </summary>
        /// <param name="bookmarkIndex"></param>
        public void SetAppInTrainingMode(int bookmarkIndex)
        {
            if (bookmarkIndex >= ActiveVariationTree.Bookmarks.Count)
            {
                return;
            }

            TreeNode startNode = ActiveVariationTree.Bookmarks[bookmarkIndex].Node;
            SetAppInTrainingMode(startNode);

        }

        /// <summary>
        /// Starts a training session from the specified Node.
        /// </summary>
        /// <param name="startNode"></param>
        public void SetAppInTrainingMode(TreeNode startNode)
        {
            // Set up the training mode
            StopEvaluation();
            LearningMode.ChangeCurrentMode(LearningMode.Mode.TRAINING);
            TrainingSession.IsTrainingInProgress = true;
            TrainingSession.ChangeCurrentState(TrainingSession.State.AWAITING_USER_TRAINING_MOVE);
            EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);

            LearningMode.TrainingSide = startNode.ColorToMove;
            MainChessBoard.DisplayPosition(startNode);

            UiTrainingView = new TrainingView(UiRtbTrainingProgress.Document, this);
            UiTrainingView.Initialize(startNode);

            if (LearningMode.TrainingSide == PieceColor.Black && !MainChessBoard.IsFlipped
                || LearningMode.TrainingSide == PieceColor.White && MainChessBoard.IsFlipped)
            {
                MainChessBoard.FlipBoard();
            }

            AppStateManager.ShowMoveEvaluationControls(false, false);
            BoardCommentBox.TrainingSessionStart();

            // The Line display is the same as when playing a game against the computer 
            EngineGame.InitializeGameObject(startNode, false, false);
            UiDgEngineGame.ItemsSource = EngineGame.Line.MoveList;
            Timers.Start(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
        }

        public void InvokeRequestWorkbookResponse(object source, ElapsedEventArgs e)
        {
            UiTrainingView.RequestWorkbookResponse();
        }

        public void ShowTrainingProgressPopupMenu(object source, ElapsedEventArgs e)
        {
            UiTrainingView.ShowPopupMenu();
        }

        public void FlashAnnouncementTimeUp(object source, ElapsedEventArgs e)
        {
            BoardCommentBox.HideFlashAnnouncement();
        }

        public void ShowFloatingChessboard(bool visible)
        {
            this.Dispatcher.Invoke(() =>
            {
                UiVbFloatingChessboard.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            });
        }

        /// <summary>
        /// The user pressed a key to be handled by Active Line.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewActiveLine_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ActiveLine.PreviewKeyDown(sender, e);
        }

        /// <summary>
        /// Advise the Training View that the engine made a move
        /// while playing a training game against the user.
        /// </summary>
        public void EngineTrainingGameMoveMade()
        {
            this.Dispatcher.Invoke(() =>
            {
                UiTrainingView.EngineMoveMade();
            });
        }

        /// <summary>
        /// Shade the "from" and "to" squares of the passed move.
        /// </summary>
        /// <param name="engCode"></param>
        public void ColorMoveSquares(string engCode)
        {
            this.Dispatcher.Invoke(() =>
            {
                MainChessBoard.RemoveMoveSquareColors();

                MoveUtils.EngineNotationToCoords(engCode, out SquareCoords sqOrig, out SquareCoords sqDest);
                MainChessBoard.ColorMoveSquare(sqOrig.Xcoord, sqOrig.Ycoord, true);
                MainChessBoard.ColorMoveSquare(sqDest.Xcoord, sqDest.Ycoord, false);
            });
        }

        /// <summary>
        /// Stops and restarts the engine.
        /// </summary>
        /// <returns></returns>
        public bool ReloadEngine()
        {
            EngineMessageProcessor.StopEngineService();
            EngineMessageProcessor.CreateEngineService(this, _isDebugMode);

            bool engineStarted = EngineMessageProcessor.StartEngineService();
            if (!engineStarted)
            {
                MessageBox.Show("Failed to load the engine. Move evaluation will not be available.", "Chess Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Shows the Workbook options dialog.
        /// </summary>
        /// <returns></returns>
        public bool ShowWorkbookOptionsDialog(bool save)
        {
            WorkbookOptionsDialog dlg = new WorkbookOptionsDialog(SessionWorkbook)
            {
                Left = ChessForgeMain.Left + 100,
                Top = ChessForgeMain.Top + 100,
                Topmost = false,
                Owner = this
            };
            dlg.ShowDialog();

            if (dlg.ExitOK)
            {
                SessionWorkbook.TrainingSide = dlg.TrainingSide;
                SessionWorkbook.Title = dlg.WorkbookTitle;
                if (save)
                {
                    AppStateManager.SaveWorkbookFile();
                }

                MainChessBoard.FlipBoard(SessionWorkbook.TrainingSide);
                if (_chaptersView != null)
                {
                    _chaptersView.BuildFlowDocumentForChaptersView();
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Shows the Application Options dialog.
        /// </summary>
        private void ShowApplicationOptionsDialog()
        {
            AppOptionsDialog dlg = new AppOptionsDialog
            {
                Left = ChessForgeMain.Left + 100,
                Top = ChessForgeMain.Top + 100,
                Topmost = false,
                Owner = this
            };
            dlg.ShowDialog();

            if (dlg.ExitOK)
            {
                if (dlg.ChangedEnginePath)
                    Configuration.WriteOutConfiguration();
                if (dlg.ChangedEnginePath)
                {
                    ReloadEngine();
                }
            }
        }

        /// <summary>
        /// Shows the Chapter Title options dialog.
        /// </summary>
        /// <returns></returns>
        private bool ShowChapterTitleDialog(Chapter chapter)
        {
            ChapterTitleDialog dlg = new ChapterTitleDialog(chapter)
            {
                Left = ChessForgeMain.Left + 100,
                Top = ChessForgeMain.Top + 100,
                Topmost = false,
                Owner = this
            };
            dlg.ShowDialog();

            if (dlg.ExitOK)
            {
                chapter.Title = dlg.ChapterTitle;
                _chaptersView.BuildFlowDocumentForChaptersView();
                AppStateManager.IsDirty = true;
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Stops any evaluation that is currently happening.
        /// Resets evaluation state and adjusts the GUI accordingly. 
        /// </summary>
        public void StopEvaluation(bool updateGui = true)
        {
            EngineMessageProcessor.StopEngineEvaluation();

            if (updateGui)
            {
                EvaluationManager.Reset();
            }

            AppStateManager.ResetEvaluationControls();
            AppStateManager.ShowMoveEvaluationControls(false, true);

            if (updateGui)
            {
                // TODO: remove as EvaluationManager.Reset() already calls this
                AppStateManager.SetupGuiForCurrentStates();
            }

            if (LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                Timers.StopAll();
            }
        }

        /// <summary>
        /// Invokes the Annotations dialog.
        /// </summary>
        /// <param name="nd"></param>
        public bool InvokeAnnotationsDialog(TreeNode nd)
        {
            bool changed = false;

            if (nd != null)
            {
                AnnotationsDialog dlg = new AnnotationsDialog(nd)
                {
                    Left = ChessForgeMain.Left + 100,
                    Top = ChessForgeMain.Top + 100,
                    Topmost = false,
                    Owner = this
                };
                dlg.ShowDialog();
                if (dlg.ExitOk)
                {
                    if (nd.Comment != dlg.Comment || nd.Nags != dlg.Nags)
                    {
                        changed = true;
                        nd.Comment = dlg.Comment;
                        nd.SetNags(dlg.Nags);
                        AppStateManager.IsDirty = true;
                    }
                }
            }

            return changed;
        }


        /// <summary>
        /// Resizes the tab control to show/hide ActiveLine/GameLine controls.
        /// </summary>
        /// <param name="ctrl"></param>
        /// <param name="sizeMode"></param>
        public void ResizeTabControl(TabControl ctrl, TabControlSizeMode sizeMode)
        {
            switch (sizeMode)
            {
                case TabControlSizeMode.SHOW_ACTIVE_LINE:
                    ctrl.Margin = new Thickness(275, 5, 5, 5);
                    UiDgActiveLine.Visibility = Visibility.Visible;
                    break;
                case TabControlSizeMode.HIDE_ACTIVE_LINE:
                    ctrl.Margin = new Thickness(5, 5, 5, 5);
                    UiDgActiveLine.Visibility = Visibility.Hidden;
                    break;
                case TabControlSizeMode.SHOW_ACTIVE_LINE_NO_EVAL:
                    ctrl.Margin = new Thickness(175, 5, 5, 5);
                    UiDgActiveLine.Visibility = Visibility.Visible;
                    break;
                case TabControlSizeMode.SHOW_ENGINE_GAME_LINE:
                    ctrl.Margin = new Thickness(180, 5, 5, 5);
                    UiDgActiveLine.Visibility = Visibility.Hidden;
                    break;
                default:
                    ctrl.Margin = new Thickness(180, 5, 5, 5);
                    UiDgActiveLine.Visibility = Visibility.Visible;
                    break;
            }
        }

        /// <summary>
        /// Invoked before the context menu shows.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainCanvas_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (WorkbookManager.ActiveTab == WorkbookManager.TabViewType.STUDY)
            {
                _lastRightClickedPoint = null;
                if (BoardShapesManager.IsShapeBuildInProgress)
                {
                    BoardShapesManager.CancelShapeDraw(true);
                }
                UiMnMainBoard.Visibility = Visibility.Visible;
            }
            else
            {
                UiMnMainBoard.Visibility = Visibility.Collapsed;
                e.Handled = true;
            }
        }

    }
}
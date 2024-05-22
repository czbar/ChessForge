using ChessPosition;
using ChessPosition.Utils;
using EngineService;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WebAccess;
using System.Management;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // prefix used for menu items showing recent files
        public readonly string MENUITEM_RECENT_FILES_PREFIX = "RecentFiles";

        public readonly string APP_NAME = "Chess Forge";

        /// <summary>
        /// Public reference to ChaptersView 
        /// </summary>
        public ChaptersView ChaptersView { get => _chaptersView; }

        /// <summary>
        /// Public reference to StudyTreeView 
        /// </summary>
        public VariationTreeView StudyTreeView { get => _studyTreeView; }

        /// <summary>
        /// Public reference to EngineGameView 
        /// </summary>
        public EngineGameView EngineGameView { get => _engineGameView; }

        /// <summary>
        /// Public reference to OpeningStatsView
        /// </summary>
        public OpeningStatsView OpeningStatsView { get => _openingStatsView; }

        /// <summary>
        /// Public reference to TopGamesView
        /// </summary>
        public TopGamesView TopGamesView { get => _topGamesView; }

        /// <summary>
        /// Default menu size
        /// </summary>
        public double DefaultMenuFontSize;

        /// <summary>
        /// The RichTextBox based Chapters view
        /// </summary>
        private ChaptersView _chaptersView;

        /// <summary>
        /// The RichTextBox based Intro view
        /// </summary>
        private IntroView _introView;

        /// <summary>
        /// The RichTextBox based Study Tree view
        /// </summary>
        private StudyTreeView _studyTreeView;

        /// <summary>
        /// The RichTextBox based Model Game view
        /// </summary>
        private VariationTreeView _modelGameTreeView;

        /// <summary>
        /// The RichTextBox based Exercise view
        /// </summary>
        private ExerciseTreeView _exerciseTreeView;

        /// <summary>
        /// The RichTextBox based EngineGame view
        /// </summary>
        private EngineGameView _engineGameView;

        /// <summary>
        /// The RichTextBox based Opening Stats view
        /// </summary>
        private OpeningStatsView _openingStatsView;

        /// <summary>
        /// The RichTextBox based Top Games from lichess view
        /// </summary>
        private TopGamesView _topGamesView;

        /// <summary>
        /// The Tree view corresponding to the type of the current ActiveVariationTree
        /// </summary>
        public VariationTreeView ActiveTreeView
        {
            get
            {
                if (WorkbookManager.SessionWorkbook == null || WorkbookManager.SessionWorkbook.ActiveVariationTree == null)
                {
                    return null;
                }

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
        /// Clears content of the tree views.
        /// </summary>
        public void ClearTreeViews(bool rebuild = false)
        {
            if (_studyTreeView != null)
            {
                _studyTreeView.Clear(GameData.ContentType.STUDY_TREE);
                if (rebuild)
                {
                    _studyTreeView.BuildFlowDocumentForVariationTree();
                }
            }
            if (_modelGameTreeView != null)
            {
                _modelGameTreeView.Clear(GameData.ContentType.MODEL_GAME);
                if (rebuild)
                {
                    _modelGameTreeView.BuildFlowDocumentForVariationTree();
                }
            }
            if (_exerciseTreeView != null)
            {
                _exerciseTreeView.Clear(GameData.ContentType.EXERCISE);
                if (rebuild)
                {
                    _exerciseTreeView.BuildFlowDocumentForVariationTree();
                }
            }
        }

        /// <summary>
        /// Clears the view corresponding to the passed ContentType
        /// and displays the "quick skip" message.
        /// NOTE: in the current design can't use the same technique for 
        /// the Intro view as it would persist the message!
        /// TODO: consider showing some modeless Window.
        /// (maybe actually for all of this?)
        /// </summary>
        /// <param name="contentType"></param>
        public void ClearViewForQuickSkip(GameData.ContentType contentType)
        {
            switch (contentType)
            {
                case GameData.ContentType.STUDY_TREE:
                    _studyTreeView.ClearForQuickSkip();
                    break;
                case GameData.ContentType.MODEL_GAME:
                    _modelGameTreeView.ClearForQuickSkip();
                    break;
                case GameData.ContentType.EXERCISE:
                    _exerciseTreeView.ClearForQuickSkip();
                    break;
            }
        }

        /// <summary>
        /// Sets the selections in the ActiveTreeView as they were stored last.
        /// </summary>
        public void RestoreSelectedLineAndMoveInActiveView()
        {
            if (WorkbookManager.SessionWorkbook == null || ActiveTreeView == null)
            {
                return;
            }

            VariationTree tree = WorkbookManager.SessionWorkbook.ActiveVariationTree;
            if (tree != null)
            {
                string lineId = tree.SelectedLineId == "" ? "1" : tree.SelectedLineId;
                int nodeId = tree.SelectedNodeId < 0 ? 0 : tree.SelectedNodeId;
                ActiveTreeView.SelectLineAndMove(lineId, nodeId);

                ObservableCollection<TreeNode> lineToSelect = tree.SelectLine(lineId);
                SetActiveLine(lineToSelect, nodeId);

                if (tree.SelectedNode == null)
                {
                    DisplayPosition(tree.RootNode);
                }
                else
                {
                    DisplayPosition(tree.SelectedNode);
                }
            }
        }

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
        /// Chessboard shown over moves in Training View
        /// </summary>
        public ChessBoardSmall TrainingFloatingBoard;

        /// <summary>
        /// Chessboard shown over chapters headers in Chapters View
        /// </summary>
        public ChessBoardSmall ChaptersFloatingBoard;

        /// <summary>
        /// Chessboard shown over game headers in Chapters View
        /// </summary>
        public ChessBoardSmall ModelGameFloatingBoard;

        /// <summary>
        /// Chessboard shown over exercise headers in Chapters View
        /// </summary>
        public ChessBoardSmall ExerciseFloatingBoard;

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
        /// Enables or disables the entire GUI.
        /// </summary>
        public void EnableGui(bool enable)
        {
            UiMainMenu.IsEnabled = enable;

            UiImgAutoSaveOff.IsEnabled = enable;
            UiImgAutoSaveOn.IsEnabled = enable;

            UiImgEngineOn.IsEnabled = enable;
            UiImgEngineOnGray.IsEnabled = enable;
            UiImgEngineOff.IsEnabled = enable;

            UiImgExplorersOn.IsEnabled = enable;
            UiImgExplorersOff.IsEnabled = enable;
        }


        /// <summary>
        /// The variation tree currently being in view or processed.
        /// This will be Study, Game or Exercise tree from the 
        /// active chapter of the current workbook,
        /// except during an Engine Game where the EngineGame.Tree is
        /// set as the Active Tree.
        /// 
        /// Note that the Training tree does not become Active when opened.
        /// The previously opened tree remains active.
        /// Also note that the return can be null;
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
                    if (AppState.ActiveTab == TabViewType.ENGINE_GAME)
                    {
                        return EngineGame.Line.Tree;
                    }
                    else
                    {
                        return SessionWorkbook.ActiveVariationTree;
                    }
                }
            }
        }

        /// <summary>
        /// Returns id of the Active Tree or -1 if no active tree.
        /// </summary>
        public int ActiveVariationTreeId
        {
            get
            {
                if (SessionWorkbook == null || SessionWorkbook.ActiveVariationTree == null)
                {
                    return -1;
                }
                else
                {
                    return ActiveVariationTree.TreeId;
                }
            }
        }

        /// <summary>
        /// The variation tree currently being processed
        /// </summary>
        public Article ActiveArticle
        {
            get
            {
                if (SessionWorkbook == null
                    || AppState.ActiveTab == TabViewType.CHAPTERS
                    || AppState.ActiveTab == TabViewType.BOOKMARKS)
                {
                    return null;
                }
                else
                {
                    return SessionWorkbook.ActiveArticle;
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
            AppState.MainWin = this;
            _ = WebAccess.SourceForgeCheck.GetVersion();

            // the next lines pertain to localization, must be invoked here (before InitializeComponent) and in this order
            ReadConfiguration();
            Languages.UseFigurines = Configuration.UseFigurines;
            SetCultureInfo(Configuration.CultureName);
            InitializedLocalizedDictionary();
            InitializeLanguages();

            AppLog.Initialize(Configuration.DebugLevel);

            EvaluationMgr = new EvaluationManager();

            InitializeComponent();
            UiTabIntro.Visibility = Configuration.ShowIntroTab ? Visibility.Visible : Visibility.Collapsed;
            SoundPlayer.Initialize();

            InitializeConfiguration();

            // initialize GUI theme
            if (Configuration.IsDarkMode) 
            {
                ChessForgeColors.Initialize(ColorThemes.DARK_MODE);
                _modeUpdatesBlocked = true;
                UiMnDarkMode.IsChecked = Configuration.IsDarkMode;
            }
            else
            {
                ChessForgeColors.Initialize(ColorThemes.LIGHT_MODE);
            }
            ChessForgeColors.SetMainControlColors();

            SetDontSaveEvalsMenuItems(Configuration.DontSavePositionEvals);

            BoardCommentBox = new CommentBox(UiRtbBoardComment.Document, this);
            ActiveLine = new ActiveLineManager(UiDgActiveLine, this);

            EngineLinesBox.Initialize(this, UiTbEngineLines, UiPbEngineThinking);

            // Note: must be called only AFTER configuration has been initialized.
            Timers = new AppTimers(this);

            // main chess board
            MainChessBoard = new ChessBoard(true, MainCanvas, UiImgMainChessboard, null, true, true);

            // floating boards
            TrainingFloatingBoard = new ChessBoardSmall(_cnvTrainingFloat, _imgTrainingFloatingBoard, null, null, true, false);
            ChaptersFloatingBoard = new ChessBoardSmall(_cnvChaptersFloat, _imgChaptersFloatingBoard, null, null, true, false);
            ModelGameFloatingBoard = new ChessBoardSmall(_cnvModelGameFloat, _imgModelGameFloatingBoard, null, null, true, false);
            ExerciseFloatingBoard = new ChessBoardSmall(_cnvExerciseFloat, _imgExerciseFloatingBoard, null, null, true, false);


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
            try
            {
                ObjectQuery winQuery = new ObjectQuery("SELECT * FROM Win32_ComputerSystem");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(winQuery);
                foreach (ManagementObject item in searcher.Get())
                {
                    long totalMemory = Convert.ToInt64(item["TotalPhysicalMemory"]);
                    Configuration.TotalMemory = totalMemory;
                }

                Configuration.CoreCount = Environment.ProcessorCount;
            }
            catch { }

            if (Configuration.LargeMenuFont)
            {
                DefaultMenuFontSize = Constants.DEAFULT_MENU_FONT_SIZE;
                SetMenuFontSize(Constants.LARGE_MENU_FONT_SIZE);
            }
            else
            {
                DefaultMenuFontSize = UiMainMenu.FontSize;
            }

            if (Configuration.IsMainWinMaximized())
            {
                this.WindowState = WindowState.Maximized;
            }

            InitializeContentTypeSelectionComboBox();

            UiDgActiveLine.ContextMenu = UiMncMainBoard;
            UiBtnExitGame.Background = ChessForgeColors.ExitButtonLinearBrush;
            UiBtnExitTraining.Background = ChessForgeColors.ExitButtonLinearBrush;
            UiBtnShowExplorer.Background = ChessForgeColors.ShowExplorerLinearBrush;

            _openingStatsView = new OpeningStatsView(UiRtbOpenings.Document);
            _topGamesView = new TopGamesView(UiRtbTopGames.Document, true);
            UiRtbStudyTreeView.IsDocumentEnabled = true;
            UiRtbTopGames.IsDocumentEnabled = true;
            UiRtbOpenings.IsDocumentEnabled = true;


            AddDebugMenu();

            ResizeTabControl(UiTabCtrlManualReview, TabControlSizeMode.HIDE_ACTIVE_LINE);
            SetEvaluationLabels();

            LearningMode.ChangeCurrentMode(LearningMode.Mode.IDLE, false);

            if (Configuration.ShowExplorers)
            {
                TurnExplorersOn();
            }
            else
            {
                // TurnExplorersOn calls this in the branch above
                AppState.SetupGuiForCurrentStates();
            }

            if (Configuration.ShowEvaluationChart)
            {
                UiImgChartOn.Visibility = Visibility.Visible;
                UiImgChartOff.Visibility = Visibility.Hidden;
            }

            Timers.Start(AppTimers.TimerId.APP_START);

            ArticleSelected += EventSelectArticle;

            AppState.UpdateEngineToggleImages();

            Timers.Start(AppTimers.TimerId.PULSE);
            AppLog.LogAvailableThreadsCounts();
        }

        /// <summary>
        /// If the user is currently solving an exercise,
        /// leaves the Solving Mode and returns to the main tree viewing editing
        /// </summary>
        public void DeactivateSolvingMode()
        {
            if (ActiveVariationTree != null && ActiveVariationTree.ContentType == GameData.ContentType.EXERCISE)
            {
                _exerciseTreeView.DeactivateSolvingMode(VariationTree.SolvingMode.NONE);
            }
        }

        /// <summary>
        /// Fills the Bookmark View's ComboBox for Content Type selection.
        /// </summary>
        private void InitializeContentTypeSelectionComboBox()
        {
            UiComboBoxBmContent.Items.Add(new ContentTypeListItem(GameData.ContentType.NONE, "*"));
            UiComboBoxBmContent.Items.Add(new ContentTypeListItem(GameData.ContentType.STUDY_TREE, Properties.Resources.Study));
            UiComboBoxBmContent.Items.Add(new ContentTypeListItem(GameData.ContentType.MODEL_GAME, Properties.Resources.Games));
            UiComboBoxBmContent.Items.Add(new ContentTypeListItem(GameData.ContentType.EXERCISE, Properties.Resources.Exercises));
        }

        /// <summary>
        /// Updates the menu and configuration item for DontSavePositionEvals. 
        /// </summary>
        /// <param name="isOn"></param>
        public void SetDontSaveEvalsMenuItems(bool isOn)
        {
            UiMnciDontSaveEvals.IsChecked = isOn;
            UiMnExercDontSaveEvals.IsChecked = isOn;
            UiMnGameDontSaveEvals.IsChecked = isOn;
            UiMnStudyDontSaveEvals.IsChecked = isOn;

            Configuration.DontSavePositionEvals = isOn;
            AppState.UpdateEngineToggleImages();
        }

        /// <summary>
        /// Sets the langauge for the current session.
        /// </summary>
        /// <param name="culture"></param>
        private void SetCultureInfo(string culture)
        {
            if (!string.IsNullOrWhiteSpace(culture))
            {
                CultureInfo ci = CultureInfo.GetCultureInfo(culture);
                CultureInfo.DefaultThreadCurrentCulture = ci;
                CultureInfo.DefaultThreadCurrentUICulture = ci;
                CultureInfo.CurrentUICulture = ci;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            }
        }

        /// <summary>
        /// Sends selected localized strings down to the ChessPosition library.
        /// The library itself cannot access Resources.resx
        /// </summary>
        private void InitializedLocalizedDictionary()
        {
            LocalizedStrings.Values.Add(LocalizedStrings.StringId.Move, Properties.Resources.Move);
            LocalizedStrings.Values.Add(LocalizedStrings.StringId.Game, Properties.Resources.Game);
            LocalizedStrings.Values.Add(LocalizedStrings.StringId.Exercise, Properties.Resources.Exercise);
            LocalizedStrings.Values.Add(LocalizedStrings.StringId.PGN, Properties.Resources.PGN);

            LocalizedStrings.Values.Add(LocalizedStrings.StringId.PgnMissingMoveAfter, Properties.Resources.PgnMissingMoveAfter);

            LocalizedStrings.Values.Add(LocalizedStrings.StringId.FenTooFewFields, Properties.Resources.FenTooFewFields);
            LocalizedStrings.Values.Add(LocalizedStrings.StringId.FenInvalidEnpassant, Properties.Resources.FenInvalidEnpassant);
            LocalizedStrings.Values.Add(LocalizedStrings.StringId.FenColorNotSpecified, Properties.Resources.FenColorNotSpecified);
            LocalizedStrings.Values.Add(LocalizedStrings.StringId.FenTooFewRows, Properties.Resources.FenTooFewRows);
            LocalizedStrings.Values.Add(LocalizedStrings.StringId.FenRowIncomplete, Properties.Resources.FenRowIncomplete);

            LocalizedStrings.Values.Add(LocalizedStrings.StringId.InvalidEngineMoveReceived, Properties.Resources.InvalidEngineMoveReceived);
            LocalizedStrings.Values.Add(LocalizedStrings.StringId.CannotIdentifyPiece, Properties.Resources.CannotIdentifyPiece);
            LocalizedStrings.Values.Add(LocalizedStrings.StringId.IllegalCastling, Properties.Resources.IllegalCastling);
            LocalizedStrings.Values.Add(LocalizedStrings.StringId.AmbiguousMove, Properties.Resources.AmbiguousMove);

            LocalizedStrings.Values.Add(LocalizedStrings.StringId.StartingPosition, Properties.Resources.StartingPosition);

            LocalizedStrings.Values.Add(LocalizedStrings.StringId.PieceSymbolMap, Properties.Resources.PieceSymbolMap);
        }

        /// <summary>
        /// Initializes the list of languages and the symbol map.
        /// </summary>
        private void InitializeLanguages()
        {
            Languages.AddLanguage("en", Properties.Resources.LangEnglish);
            Languages.AddLanguage("pl", Properties.Resources.LangPolish);

            Languages.SetSessionLanguage(Configuration.CultureName);

            Languages.InitializeChessSymbolMapping(Properties.Resources.PieceSymbolMap);
        }

        /// <summary>
        /// Subscribes to events requiring selections of an Article.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventSelectArticle(object sender, ChessForgeEventArgs e)
        {
            SelectArticle(e.ChapterIndex, e.ContentType, e.ArticleIndex);
        }

        /// <summary>
        /// Selects the requested Chapter and Article
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="contentType"></param>
        /// <param name="articleIndex"></param>
        public void SelectArticle(int chapterIndex, GameData.ContentType contentType, int articleIndex)
        {
            WorkbookLocationNavigator.GotoArticle(chapterIndex, contentType, articleIndex);
        }

        /// <summary>
        /// Read configuration data.
        /// Some of it will be acted on later but
        /// CultureInfo is needed now.
        /// </summary>
        private void ReadConfiguration()
        {
            Configuration.StartDirectory = App.AppPath;
            Configuration.ReadConfigurationFile();
        }

        /// <summary>
        /// Initializes configurable entities.
        /// </summary>
        private void InitializeConfiguration()
        {
            if (Configuration.IsMainWinPosValid())
            {
                this.Left = Configuration.MainWinPos.Left;
                this.Top = Configuration.MainWinPos.Top;
                this.Width = Configuration.MainWinPos.Right - Configuration.MainWinPos.Left;
                this.Height = Configuration.MainWinPos.Bottom - Configuration.MainWinPos.Top;
            }

            DebugUtils.DebugLevel = Configuration.DebugLevel;

            // setup control positions
            UiDgActiveLine.HorizontalAlignment = HorizontalAlignment.Right;
            UiDgActiveLine.Margin = new Thickness(0, 27, 10, 0);

            UiLblScoresheet.HorizontalAlignment = HorizontalAlignment.Right;
            UiLblScoresheet.Margin = new Thickness(0, 0, 10 + (UiDgActiveLine.Width - UiLblScoresheet.Width), 0);

            UiDgEngineGame.HorizontalAlignment = HorizontalAlignment.Right;
            UiDgEngineGame.Margin = new Thickness(0, 27, 10, 0);

            SetupMenuBarControls();
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

            MenuItem mnDebugWriteRtf = new MenuItem
            {
                Name = "WriteRtf"
            };

            mnDebugWriteRtf.Header = "Write RTF";
            mnDebug.Items.Add(mnDebugWriteRtf);
            mnDebugWriteRtf.Click += UiMnWriteRtf_Click;

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
            Timers.Stop(AppTimers.TimerId.APP_START);

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
                    BoardCommentBox.ReadingFile();
                }
                else if (_appStartStage == 1)
                {
                    _appStartStage = 2;
                    Timers.Stop(AppTimers.TimerId.APP_START);
                    this.Dispatcher.Invoke(() =>
                    {
                        CreateRecentFilesMenuItems();
                        Timers.Stop(AppTimers.TimerId.APP_START);
                        bool engineStarted = EngineMessageProcessor.StartEngineService();
                        if (!engineStarted)
                        {
                            MessageBox.Show(Properties.Resources.LoadEngineError, Properties.Resources.Error,
                                MessageBoxButton.OK, MessageBoxImage.Error);
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
                ReportNewVersionAvailable(true);
            }
            else
            {
                Timers.Start(AppTimers.TimerId.APP_START);
            }
        }

        /// <summary>
        /// If there is a new version available for download, and it is not "supressed", 
        /// shows the informational dialog and return true.
        /// Otherwise return false.
        /// Two locations are checked with the Microsoft Store given priority.
        /// Only when there is a newer version on SourceForge, we report that one.
        /// </summary>
        /// <returns></returns>
        public bool ReportNewVersionAvailable(bool suppress)
        {
            bool res = false;

            try
            {
                Version ver = SelectAvailableUpdate(out int updSource);
                if (updSource != 0)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        if (!suppress || ver.ToString() != Configuration.DoNotShowVersion)
                        {
                            int verCompare = AppState.GetAssemblyVersion().CompareTo(ver);
                            if (verCompare < 0)
                            {
                                UpdateAvailableDialog dlg = new UpdateAvailableDialog(ver, updSource);
                                GuiUtilities.PositionDialog(dlg, this, 100);
                                dlg.ShowDialog();
                                res = true;
                            }
                        }
                    });
                }
            }
            catch
            {
                res = false;
            }

            return res;
        }

        /// <summary>
        /// Checks which new version to handle.
        /// If the Microsoft Store version is at least as new as the SourceForge one,
        /// returns 1. If SourceForge one is newer, retruns -1.
        /// If both are null, retruns 0.
        /// </summary>
        /// <returns></returns>
        private Version SelectAvailableUpdate(out int updSource)
        {
            Version ver = null;

            if (SourceForgeCheck.VersionAtSourceForge == null)
            {
                if (SourceForgeCheck.VersionAtMicrosoftAppStore == null)
                {
                    updSource = 0;
                }
                else
                {
                    updSource = 1;
                    ver = SourceForgeCheck.VersionAtMicrosoftAppStore;
                }
            }
            else if (SourceForgeCheck.VersionAtMicrosoftAppStore == null)
            {
                updSource = 0;
            }
            else
            {
                int comp = (SourceForgeCheck.VersionAtMicrosoftAppStore).CompareTo(SourceForgeCheck.VersionAtSourceForge);
                if (comp < 0)
                {
                    updSource = -1;
                    ver = SourceForgeCheck.VersionAtSourceForge;
                }
                else
                {
                    updSource = 1;
                    ver = SourceForgeCheck.VersionAtMicrosoftAppStore;
                }
            }

            return ver;
        }

        /// <summary>
        /// Selects the chapter given its index.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="focusOnStudyTree"></param>
        public void SelectChapterByIndex(int chapterIndex, bool focusOnStudyTree, bool rebuild = true)
        {
            if (chapterIndex >= 0 && chapterIndex < WorkbookManager.SessionWorkbook.Chapters.Count)
            {
                WorkbookManager.SessionWorkbook.SetActiveChapterTreeByIndex(chapterIndex, GameData.ContentType.STUDY_TREE, 0, focusOnStudyTree);
                if (AppState.ActiveTab == TabViewType.CHAPTERS)
                {
                    _chaptersView.HighlightActiveChapter();
                }

                if (rebuild)
                {
                    ClearTabViews();
                    SetupGuiForActiveStudyTree(focusOnStudyTree);
                }
            }
        }

        /// <summary>
        /// Rebuilds the Chapters view.
        /// </summary>
        public void RebuildChaptersView(bool forceExpandGames = false, bool forceExpandExercises = false)
        {
            try
            {
                if (forceExpandGames)
                {
                    WorkbookManager.SessionWorkbook.ActiveChapter.IsModelGamesListExpanded = true;
                }
                if (forceExpandExercises)
                {
                    WorkbookManager.SessionWorkbook.ActiveChapter.IsExercisesListExpanded = true;
                }
                _chaptersView.BuildFlowDocumentForChaptersView();
            }
            catch { }
        }

        /// <summary>
        /// Clears the TreeViews' documents in the tab control.
        /// </summary>
        private void ClearTabViews()
        {
            ClearTreeView(_studyTreeView, GameData.ContentType.STUDY_TREE);
            ClearTreeView(_modelGameTreeView, GameData.ContentType.MODEL_GAME);
            ClearTreeView(_exerciseTreeView, GameData.ContentType.EXERCISE);
        }

        /// <summary>
        /// Clears the passed VariationTreeView
        /// </summary>
        /// <param name="view"></param>
        private void ClearTreeView(VariationTreeView view, GameData.ContentType contentType)
        {
            if (view != null)
            {
                view.Clear(contentType);
            }
            else
            {
                // need to build an empty bar or we end up with PLACEHOLDER rubbish
                PreviousNextViewBars.BuildPreviousNextBar(contentType);
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
                Chapter activeChapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                gameIndex = AdjustArticleIndex(gameIndex, activeChapter.GetModelGameCount());
                if (gameIndex >= 0 && gameIndex < activeChapter.GetModelGameCount())
                {
                    Article article = activeChapter.ModelGames[gameIndex];
                    if (!article.IsReady)
                    {
                        activeChapter.ModelGames[gameIndex] = WorkbookManager.SessionWorkbook.GamesManager.ProcessArticleSync(article);
                    }

                    activeChapter.ActiveModelGameIndex = gameIndex;
                    activeChapter.SetActiveVariationTree(GameData.ContentType.MODEL_GAME, gameIndex);

                    PieceColor orient = EffectiveBoardOrientation(WorkbookManager.ItemType.MODEL_GAME);
                    MainChessBoard.FlipBoard(orient);

                    SetupGuiForActiveModelGame(gameIndex, setFocus);
                }
                else
                {
                    if (_modelGameTreeView != null)
                    {
                        _modelGameTreeView.Clear(GameData.ContentType.MODEL_GAME);
                        activeChapter.SetActiveVariationTree(GameData.ContentType.NONE);
                        AppState.MainWin.UiTabModelGames.Focus();
                    }
                    PreviousNextViewBars.BuildPreviousNextBar(GameData.ContentType.MODEL_GAME);
                }

                if (setFocus && WorkbookManager.SessionWorkbook != null)
                {
                    WorkbookLocationNavigator.SaveNewLocation(activeChapter, GameData.ContentType.MODEL_GAME, gameIndex);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in SelectModelGame(): " + ex.Message);
            }
        }

        /// <summary>
        /// Returns desired board orientaion based on Workbook settings
        /// and potential user selected custom.
        /// </summary>
        /// <param name="itemType"></param>
        /// <returns></returns>
        private PieceColor EffectiveBoardOrientation(WorkbookManager.ItemType itemType)
        {
            PieceColor effectiveOrientation = PieceColor.None;
            switch (itemType)
            {
                case WorkbookManager.ItemType.STUDY:
                    effectiveOrientation = WorkbookManager.SessionWorkbook.StudyBoardOrientationConfig;
                    break;
                case WorkbookManager.ItemType.MODEL_GAME:
                    effectiveOrientation = WorkbookManager.SessionWorkbook.GameBoardOrientationConfig;
                    break;
                case WorkbookManager.ItemType.EXERCISE:
                    effectiveOrientation = WorkbookManager.SessionWorkbook.ExerciseBoardOrientationConfig;
                    break;
            }

            if (ActiveVariationTree != null)
            {
                PieceColor customBoardOrientation = ActiveVariationTree.CustomBoardOrientation;

                if (customBoardOrientation != PieceColor.None)
                {
                    // if settings changed in the meantime, reset custom
                    if (effectiveOrientation == customBoardOrientation)
                    {
                        ResetCustomBoardOrientation(ActiveVariationTree);
                        ActiveVariationTree.CustomBoardOrientation = PieceColor.None;
                    }
                    else
                    {
                        effectiveOrientation = customBoardOrientation;
                    }
                }
            }

            return effectiveOrientation;
        }

        /// <summary>
        /// Select and activate view for the exercise in the ActiveChapter
        /// at the passed index.
        /// </summary>
        /// <param name="exerciseIndex"></param>
        public void SelectExercise(int exerciseIndex, bool setFocus)
        {
            try
            {
                Chapter activeChapter = WorkbookManager.SessionWorkbook.ActiveChapter;
                exerciseIndex = AdjustArticleIndex(exerciseIndex, activeChapter.GetExerciseCount());
                if (exerciseIndex >= 0 && exerciseIndex < activeChapter.GetExerciseCount())
                {
                    Article article = activeChapter.Exercises[exerciseIndex];
                    if (!article.IsReady)
                    {
                        activeChapter.Exercises[exerciseIndex] = WorkbookManager.SessionWorkbook.GamesManager.ProcessArticleSync(article);
                    }

                    activeChapter.ActiveExerciseIndex = exerciseIndex;
                    activeChapter.SetActiveVariationTree(GameData.ContentType.EXERCISE, exerciseIndex);

                    PieceColor orient = EffectiveBoardOrientation(WorkbookManager.ItemType.EXERCISE);
                    if (orient == PieceColor.None)
                    {
                        orient = ActiveVariationTree.RootNode.ColorToMove;
                    }
                    MainChessBoard.FlipBoard(orient);

                    SetupGuiForActiveExercise(exerciseIndex, setFocus);
                    if (setFocus && WorkbookManager.SessionWorkbook != null)
                    {
                        WorkbookLocationNavigator.SaveNewLocation(activeChapter, GameData.ContentType.EXERCISE, exerciseIndex);
                    }
                }
                else
                {
                    if (_exerciseTreeView != null)
                    {
                        _exerciseTreeView.Clear(GameData.ContentType.EXERCISE);
                        activeChapter.SetActiveVariationTree(GameData.ContentType.NONE);
                    }
                    PreviousNextViewBars.BuildPreviousNextBar(GameData.ContentType.EXERCISE);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in SelectExercise(): " + ex.Message);
            }
        }

        /// <summary>
        /// Brings into view the selected run in the Active View
        /// </summary>
        public void BringSelectedRunIntoView()
        {
            if (ActiveTreeView != null)
            {
                ActiveTreeView.BringSelectedRunIntoView();
            }
        }

        /// <summary>
        /// Corrects the index of an article if the passed one is out of range.
        /// This can happen e.g. when the workbook is exited without saving while
        /// the last edited article was a newly created one.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="articleCount"></param>
        /// <returns></returns>
        private int AdjustArticleIndex(int index, int articleCount)
        {
            if (index >= 0 && index < articleCount)
            {
                return index;
            }
            else
            {
                if (index >= articleCount)
                {
                    index = articleCount - 1;
                }
                return index;
            }
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
                        mi.ToolTip = recentFiles.ElementAt(i);
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
        /// Accessor to Intro's SaveXAMLContent()
        /// </summary>
        public void SaveIntro()
        {
            try
            {
                if (_introView != null)
                {
                    _introView.SaveXAMLContent(true);
                }
            }
            catch
            { };
        }

        /// <summary>
        /// Saves the Arrow positions string.
        /// If the passed node is null, we find the node
        /// currently displayed in the Main Board.
        /// </summary>
        /// <param name="arrowsString"></param>
        /// <return>whether the new string is different</return>
        public bool SaveArrowsStringInCurrentNode(TreeNode nd, string arrowsString)
        {
            if (nd == null)
            {
                if (MainChessBoard != null)
                {
                    nd = MainChessBoard.DisplayedNode;
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
            else
            {
                if (nd.Arrows != arrowsString)
                {
                    nd.Arrows = arrowsString;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Saves the Circle positions string to the Node currently
        /// hosted in the Main Chessboard.
        /// </summary>
        /// <param name="circlesString"></param>
        public bool SaveCirclesStringInCurrentNode(TreeNode nd, string circlesString)
        {
            if (nd == null)
            {
                if (MainChessBoard != null)
                {
                    nd = MainChessBoard.DisplayedNode;
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
            else
            {
                if (nd.Circles != circlesString)
                {
                    nd.Circles = circlesString;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Checks if the main board can be used i.e. if the active tab has a variation tree to use.
        /// </summary>
        /// <returns></returns>
        private bool IsActiveMainBoard()
        {
            if (WorkbookManager.SessionWorkbook == null || WorkbookManager.SessionWorkbook.ActiveChapter == null)
            {
                return false;
            }

            if (WorkbookManager.ActiveTab == TabViewType.EXERCISE && WorkbookManager.SessionWorkbook.ActiveChapter.ActiveExerciseIndex < 0
                ||
               WorkbookManager.ActiveTab == TabViewType.MODEL_GAME && WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex < 0
                ||
               WorkbookManager.ActiveTab == TabViewType.CHAPTERS
                ||
               WorkbookManager.ActiveTab == TabViewType.BOOKMARKS)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Checks if the clicked piece is eligible for making a move.
        /// </summary>
        /// <param name="sqNorm"></param>
        /// <returns></returns>
        private bool CanMovePiece(SquareCoords sqNorm)
        {
            if (IsActiveMainBoard() && (AppState.ActiveTab != TabViewType.EXERCISE || ActiveArticle == null || ActiveArticle.Solver == null || ActiveArticle.Solver.IsMovingAllowed()))
            {
                PieceColor pieceColor = MainChessBoard.GetPieceColor(sqNorm);

                // in the Manual Review, the color of the piece on the main board must match the side on the move in the selected position
                // unless we are on the Intro tab where no checks are performed
                if (LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW)
                {
                    if (AppState.ActiveTab == TabViewType.INTRO)
                    {
                        // as long as there is a piece to drag and the view is not null
                        return pieceColor != PieceColor.None && _introView != null;
                    }
                    else
                    {
                        TreeNode nd = ActiveLine.GetSelectedTreeNode() ?? ActiveVariationTree.Nodes[0];
                        return pieceColor != PieceColor.None && pieceColor == nd.ColorToMove;
                    }
                }
                else if (LearningMode.CurrentMode == LearningMode.Mode.ENGINE_GAME && EngineGame.CurrentState == EngineGame.GameState.USER_THINKING
                      || LearningMode.CurrentMode == LearningMode.Mode.TRAINING && TrainingSession.CurrentState == TrainingSession.State.AWAITING_USER_TRAINING_MOVE)
                {
                    return EngineGame.GetPieceColor(sqNorm) == EngineGame.ColorToMove;
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
        /// Requests that the promo tray be displayed to allow the user 
        /// to select the piece to promote to.
        /// </summary>
        /// <param name="normTarget">Normalized propmotion square coordinates
        /// i.e. 0 is for Black and 7 is for White promotion.</param>
        /// <returns></returns>
        public PieceType GetUserPromoSelection(SquareCoords normTarget)
        {
            try
            {
                bool whitePromotion = normTarget.Ycoord == 7;

                AppState.MainWin.MainChessBoard.PlacePromoImage(whitePromotion ? PieceColor.White : PieceColor.Black, normTarget);

                HiddenClickDialog dlgBlock = new HiddenClickDialog();
                dlgBlock.ShowDialog();

                AppState.MainWin.MainChessBoard.RemovePromoImage();

                PieceType piece = AppState.MainWin.MainChessBoard.CalculatePromotionPieceFromPoint(dlgBlock.ClickPoint);
                return piece;
            }
            catch (Exception ex)
            {
                AppLog.Message("GetUserPromoSelection()", ex);
                AppState.MainWin.MainChessBoard.RemovePromoImage();
                return PieceType.None;
            }
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
                    leftTop.Y = ChessForgeMain.Top + ChessForgeMain.UiImgMainChessboard.Margin.Top + 40 + (normTarget.Ycoord - 4) * 80;
                }
                else
                {
                    leftTop.Y = ChessForgeMain.Top + ChessForgeMain.UiImgMainChessboard.Margin.Top + 40 + (normTarget.Ycoord) * 80;
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
        /// Returns true if user accepted the change of mode.
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
            // we may not be changing the mode, but changing the variation tree we are working with.
            if (LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW && newMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                result = true;
            }
            else if (LearningMode.CurrentMode != LearningMode.Mode.MANUAL_REVIEW && newMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                if (LearningMode.CurrentMode == LearningMode.Mode.ENGINE_GAME && !TrainingSession.IsTrainingInProgress)
                {
                    if (MessageBox.Show(Properties.Resources.CancelGame, Properties.Resources.GameInProgress, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        result = true;
                    }
                }
                else
                {
                    result = true;
                }
            }
            else
            {
                result = true;
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
        /// We will also read non - Chess Forge .pgn files and process/convert them.
        /// </summary>
        /// <param name="fileName">path to the file</param>
        /// <param name="isLastOpen">were we asked to open the file that was open last in the previous session</param>
        private void ReadWorkbookFile(string fileName, bool isLastOpen, ref ObservableCollection<GameData> GameList)
        {
            Cursor prevCursor = Cursor;

            try
            {
                if (!WorkbookManager.CheckFileExists(fileName, isLastOpen))
                {
                    BoardCommentBox.ShowTabHints();
                    return;
                }
                BoardCommentBox.ReadingFile();

                AppState.RestartInIdleMode(false);
                AppState.WorkbookFilePath = fileName;

                string fileExtension = Path.GetExtension(fileName).ToLower();

                bool acceptFile = false;
                bool isChessForgeFile = false;

                switch (fileExtension)
                {
                    case ".pgn":
                        Cursor = Cursors.Wait;
                        WorkbookManager.ReadPgnFile(fileName, ref GameList, GameData.ContentType.GENERIC, GameData.ContentType.NONE);
                        bool res = WorkbookManager.PrepareWorkbook(ref GameList, out isChessForgeFile);
                        acceptFile = res;
                        break;
                    default:
                        MessageBox.Show(Properties.Resources.UnrecognizedFileFormat + " " + fileName, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        acceptFile = false;
                        break;
                }

                if (acceptFile)
                {
                    WorkbookViewState wvs = new WorkbookViewState(WorkbookManager.SessionWorkbook);
                    try
                    {
                        // don't read the state it is a new workbook built from generic PGN
                        if (isChessForgeFile)
                        {
                            wvs.ReadState();
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLog.Message("wvs.ReadState()", ex);
                    }

                    SetupGuiForNewSession(AppState.WorkbookFilePath, true, wvs);
                    AppState.SetupGuiForCurrentStates();
                }
                else
                {
                    AppState.RestartInIdleMode(true);
                }
            }
            catch (Exception e)
            {
                AppLog.Message("ReadWorkbookFile()", e);
                MessageBox.Show(e.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                AppState.RestartInIdleMode();
            }

            Cursor = prevCursor;
        }

        /// <summary>
        /// Sets up GUI views and components for the start of
        /// a new session in the MANUAL_REVIEW learning mode.
        /// </summary>
        /// <param name="fileName"></param>
        private void SetupGuiForNewSession(string fileName, bool isChessForgeFile, WorkbookViewState wvs)
        {
            try
            {
                // if this is a new file (wvs == null) or we do not have ActiveChapter from the
                // saved configuration we will set ActiveChapter to the first chapter
                // and Active Tree to the Study Tree in that chapter.

                Workbook workbook = WorkbookManager.SessionWorkbook;
                TabViewType tabToFocus = TabViewType.NONE;

                if (wvs == null)
                {
                    // this is a Workbook that is opened for the first time
                    tabToFocus = TabViewType.CHAPTERS;
                    workbook.SelectDefaultActiveChapter();

                    int articleCount = workbook.GetArticleCount();
                    if (articleCount < 500)
                    {
                        // open everything
                        ExpandCollapseChaptersView(true, true);
                    }
                    else
                    {
                        // expand just the first chapter
                        Chapter chapter = workbook.Chapters[0];
                        chapter.IsViewExpanded = true;
                        chapter.IsModelGamesListExpanded = true;
                        chapter.IsExercisesListExpanded = true;
                    }
                }
                else
                {
                    tabToFocus = wvs.ActiveViewType == TabViewType.NONE ? TabViewType.CHAPTERS : wvs.ActiveViewType;
                    workbook.SelectActiveChapter(wvs.ActiveChapterIndex);
                }

                AppState.UpdateAppTitleBar();
                if (SessionWorkbook.TrainingSideConfig == PieceColor.None)
                {
                    ShowWorkbookOptionsDialog(false);
                }
                MainChessBoard.FlipBoard(SessionWorkbook.StudyBoardOrientationConfig);
                if (isChessForgeFile)
                {
                    WorkbookManager.UpdateRecentFilesList(fileName);
                }
                LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW, false);

                // only build chapters view here, if we are showing this tab first
                if (tabToFocus == TabViewType.CHAPTERS)
                {
                    InitializeChaptersView();
                }
                else
                {
                    _chaptersView = new ChaptersView(UiRtbChaptersView.Document, this);
                    _chaptersView.IsDirty = true;
                }

                // reset so that GotFocus() does not bail 
                WorkbookManager.ActiveTab = TabViewType.NONE;

                // this just in case and for extra future proofing...
                // move the focus somewhere away from any tab that may have it so that the next call to Focus() is effective 
                // However, due the use of ForceFocus() below, this is not necessary anymore
                // UiRtbBoardComment.Focus();

                if (tabToFocus == TabViewType.INTRO && WorkbookManager.SessionWorkbook.ActiveChapter.IsIntroEmpty())
                {
                    tabToFocus = TabViewType.STUDY;
                }

                GuiUtilities.ForceFocus(tabToFocus, TabViewType.STUDY);
                MultiTextBoxManager.ShowEvaluationChart(false);
            }
            catch (Exception ex)
            {
                AppLog.Message("SetupGuiForNewSession()", ex);
            }
        }

        /// <summary>
        /// Rebuilds and shows the Intro view.
        /// </summary>
        public void SetupGuiForIntro(bool focusOnIntro)
        {
            if (AppState.ActiveChapter == null)
            {
                return;
            }

            try
            {
                Article article = AppState.ActiveChapter.Intro;
                if (!article.IsReady)
                {
                    AppState.ActiveChapter.Intro = WorkbookManager.SessionWorkbook.GamesManager.ProcessArticleSync(article);
                }

                // if we are in the INTRO tab, we need to force a rebuild
                if (AppState.ActiveTab == TabViewType.INTRO)
                {
                    RebuildIntroView();
                }
                else
                {
                    // if not in the INTRO tab, calling Focus will do the job.
                    UiTabIntro.Focus();
                    UiRtbIntroView.Focus();
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("SetupGuiForIntro()", ex);
            }
        }

        /// <summary>
        /// Sets up the data and GUI for the ActiveStudyTree.
        /// This method will be called e.g. when opening a new
        /// Workbook and initializing the view.
        /// </summary>
        public void SetupGuiForActiveStudyTree(bool focusOnStudyTree)
        {
            if (AppState.ActiveChapter == null)
            {
                return;
            }

            Mouse.SetCursor(Cursors.Wait);
            try
            {

                _studyTreeView = new StudyTreeView(UiRtbStudyTreeView, GameData.ContentType.STUDY_TREE, -1);

                _studyTreeView.ArticleSelected -= ArticleSelected;
                _studyTreeView.ArticleSelected += ArticleSelected;

                VariationTree studyTree;

                Article article = AppState.ActiveChapter.StudyTree;
                if (!article.IsReady)
                {
                    // temporarily hide engine lines and chart if visible so that we can see the progress messages
                    GuiUtilities.HideEngineLinesAndChart(out bool engineVisibility, out bool chartVisibility);

                    AppState.ActiveChapter.StudyTree = WorkbookManager.SessionWorkbook.GamesManager.ProcessArticleSync(article);
                    studyTree = AppState.ActiveChapter.StudyTree.Tree;

                    // unhide engine lines and chart
                    GuiUtilities.ShowEngineLinesAndChart(engineVisibility, chartVisibility);
                }
                else
                {
                    studyTree = AppState.ActiveChapter.StudyTree.Tree;
                }

                if (studyTree.Nodes.Count == 0)
                {
                    studyTree.CreateNew();
                }
                else
                {
                    studyTree.BuildLines();
                }

                // may have to be set explicitly in some cases
                studyTree.ContentType = GameData.ContentType.STUDY_TREE;

                _studyTreeView.BuildFlowDocumentForVariationTree();

                string startLineId;
                int startNodeId = 0;

                if (!string.IsNullOrEmpty(studyTree.SelectedLineId) && studyTree.SelectedNodeId >= 0)
                {
                    startLineId = studyTree.SelectedLineId;
                    startNodeId = studyTree.SelectedNodeId;
                }
                else
                {
                    startLineId = studyTree.GetDefaultLineIdForNode(0);
                }

                studyTree.SelectedLineId = startLineId;
                studyTree.SetSelectedNodeId(startNodeId);

                if (focusOnStudyTree)
                {
                    UiTabStudyTree.Focus();
                    UiRtbStudyTreeView.Focus();
                }

                SetActiveLine(studyTree, startLineId, startNodeId);

                BookmarkManager.IsDirty = true;

                int nodeIndex = ActiveLine.GetIndexForNode(startNodeId);
                SelectLineAndMoveInWorkbookViews(_studyTreeView, startLineId, nodeIndex, false);
            }
            catch (Exception ex)
            {
                AppLog.Message("SetupGuiForActiveStudyTree()", ex);
            }

            Mouse.SetCursor(Cursors.Arrow);
        }

        /// <summary>
        /// Sets up the data and GUI for the ActiveTree of Model Game.
        /// This method will be called e.g. when opening a new
        /// Workbook and initializing the view.
        /// </summary>
        public void SetupGuiForActiveModelGame(int gameIndex, bool focusOnModelGame)
        {
            if (ActiveVariationTree == null)
            {
                return;
            }

            try
            {
                _modelGameTreeView = new VariationTreeView(UiRtbModelGamesView, GameData.ContentType.MODEL_GAME, gameIndex);
                UiRtbModelGamesView.IsDocumentEnabled = true;
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
                ActiveVariationTree.SetSelectedNodeId(startNodeId);

                bool isModelGameTabAlready = AppState.ActiveTab == TabViewType.MODEL_GAME;
                if (focusOnModelGame)
                {
                    UiTabModelGames.Focus();
                    UiRtbModelGamesView.Focus();
                }

                // if !focusOnModelGame and or we are already in the Model Game tab, the Focus methods above won't be called or won't refresh the view
                if (!focusOnModelGame || isModelGameTabAlready)
                {
                    SetActiveLine(startLineId, startNodeId);
                }

                int nodeIndex = ActiveLine.GetIndexForNode(startNodeId);
                SelectLineAndMoveInWorkbookViews(_modelGameTreeView, startLineId, nodeIndex, false);
            }
            catch (Exception ex)
            {
                AppLog.Message("SetupGuiForActiveModelGame()", ex);
            }
        }

        /// <summary>
        /// Activates the Chapters tab.
        /// </summary>
        public void SetupGuiForChapters()
        {
        }

        /// <summary>
        /// Sets up the data and GUI for the ActiveTree of Exercise Game.
        /// This method will be called e.g. when opening a new
        /// Workbook and initializing the view.
        /// </summary>
        public void SetupGuiForActiveExercise(int exerciseIndex, bool focusOnExercise)
        {
            if (AppState.ActiveChapter == null)
            {
                return;
            }

            try
            {
                _exerciseTreeView = new ExerciseTreeView(GameData.ContentType.EXERCISE, exerciseIndex);
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
                if (_exerciseTreeView.IsMainVariationTreeEmpty && !_exerciseTreeView.AreLinesShown)
                {
                    _exerciseTreeView.EventShowHideButtonClicked(null, null);
                }

                string startLineId;
                int startNodeId = 0;

                if (!string.IsNullOrEmpty(ActiveVariationTree.SelectedLineId) && ActiveVariationTree.SelectedNodeId >= 0 && ActiveVariationTree.ShowTreeLines)
                {
                    startLineId = ActiveVariationTree.SelectedLineId;
                    startNodeId = ActiveVariationTree.SelectedNodeId;
                }
                else
                {
                    startLineId = ActiveVariationTree.GetDefaultLineIdForNode(0);
                }

                ActiveVariationTree.SelectedLineId = startLineId;
                ActiveVariationTree.SetSelectedNodeId(startNodeId);

                bool isExerciseTabAlready = AppState.ActiveTab == TabViewType.EXERCISE;
                if (focusOnExercise)
                {
                    UiTabExercises.Focus();
                    UiRtbExercisesView.Focus();
                }

                // if !focusOnExercise and or we are already in the Exercise tab, the Focus methods above won't be called or won't refresh the view
                if (!focusOnExercise || isExerciseTabAlready)
                {
                    SetActiveLine(startLineId, startNodeId);
                }

                int nodeIndex = ActiveLine.GetIndexForNode(startNodeId);
                SelectLineAndMoveInWorkbookViews(_exerciseTreeView, startLineId, nodeIndex, false);

                if (!ActiveVariationTree.ShowTreeLines)
                {
                    AppState.ShowExplorers(false, false);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("SetupGuiForActiveExercise()", ex);
            }
        }

        /// <summary>
        /// Initializes the ChaptersView
        /// </summary>
        private void InitializeChaptersView()
        {
            _chaptersView = new ChaptersView(UiRtbChaptersView.Document, this);

            // if this is very big, make sure the view is collapsed
            // to speed up initial reading
            // int articleCount = AppState.Workbook.GetArticleCount();
            // if (articleCount > 500)  // TODO: this could be annoying to the user, on the other hand could be a big perf hit, maybe do it per chapter?
            //{
            //    ExpandCollapseChaptersView(false, true);
            //}

            _chaptersView.BuildFlowDocumentForChaptersView();
            AppState.DoEvents();
            _chaptersView.BringChapterIntoView(WorkbookManager.SessionWorkbook.ActiveChapterIndex);
        }

        /// <summary>
        /// Rebuilds the entire Workbook View
        /// </summary>
        public void RebuildActiveTreeView()
        {
            if (ActiveTreeView != null)
            {
                ActiveTreeView.BuildFlowDocumentForVariationTree();
            }
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
            SelectLineAndMoveInWorkbookViews(ActiveTreeView, lineId, ActiveLine.GetSelectedPlyNodeIndex(true), true);
        }

        /// <summary>
        /// Adds a new Node to the Workbook View,
        /// avoiding the full rebuild (performance).
        /// This can only be done "safely" if we are adding a move to a leaf.
        /// </summary>
        /// <param name="nd"></param>
        public void AddNewNodeToVariationTreeView(TreeNode nd)
        {
            if (ActiveVariationTree != null && ActiveVariationTree.ShowTreeLines)
            {
                ActiveTreeView.AddNewNodeToDocument(nd);
            }
        }

        /// <summary>
        /// Selects a line and move in the VariationTree view.
        /// </summary>
        /// <param name="lineId"></param>
        /// <param name="index"></param>
        public void SelectLineAndMoveInWorkbookViews(VariationTreeView view, string lineId, int index, bool queryExplorer)
        {
            try
            {
                TreeNode nd = ActiveLine.GetNodeAtIndex(index);
                if (nd == null)
                {
                    // try the node at index 0
                    nd = ActiveLine.GetNodeAtIndex(0);
                }

                if (nd != null && WorkbookManager.SessionWorkbook.ActiveVariationTree != null)
                {
                    WorkbookManager.SessionWorkbook.ActiveVariationTree.SetSelectedLineAndMove(lineId, nd.NodeId);
                    view.SelectLineAndMove(lineId, nd.NodeId);
                    if (EvaluationManager.CurrentMode == EvaluationManager.Mode.CONTINUOUS && AppState.ActiveTab != TabViewType.CHAPTERS)
                    {
                        EvaluateActiveLineSelectedPosition(nd);
                    }
                    if (AppState.MainWin.UiEvalChart.Visibility == System.Windows.Visibility.Visible)
                    {
                        if (AppState.MainWin.UiEvalChart.IsDirty)
                        {
                            MultiTextBoxManager.ShowEvaluationChart(true);
                        }
                        AppState.MainWin.UiEvalChart.SelectMove(nd);
                    }
                    if (queryExplorer)// && !GamesEvaluationManager.IsEvaluationInProgress)
                    {
                        _openingStatsView.SetOpeningName();
                        WebAccessManager.ExplorerRequest(AppState.ActiveTreeId, ActiveVariationTree.SelectedNode);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Sets active line and node in the ActiveVariationTree
        /// </summary>
        /// <param name="lineId"></param>
        /// <param name="selectedNodeId"></param>
        /// <param name="displayPosition"></param>
        public void SetActiveLine(string lineId, int selectedNodeId, bool displayPosition = true)
        {
            if (ActiveVariationTree != null)
            {
                ObservableCollection<TreeNode> line = ActiveVariationTree.SelectLine(lineId);
                SetActiveLine(line, selectedNodeId, displayPosition);
            }
        }

        /// <summary>
        /// Sets active line and node in the passed VariationTree
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="lineId"></param>
        /// <param name="selectedNodeId"></param>
        /// <param name="displayPosition"></param>
        public void SetActiveLine(VariationTree tree, string lineId, int selectedNodeId, bool displayPosition = true)
        {
            if (tree != null)
            {
                ObservableCollection<TreeNode> line = tree.SelectLine(lineId);
                SetActiveLine(line, selectedNodeId, displayPosition);
            }
        }

        /// <summary>
        /// Displays the position of the passed node
        /// and any associated arrows or circles.
        /// </summary>
        /// <param name="nd"></param>
        public void DisplayPosition(TreeNode nd)
        {
            MainChessBoard.DisplayPosition(nd, true);
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
            try
            {
                ActiveLine.SetNodeList(line, false);

                if (selectedNodeId >= 0)
                {
                    TreeNode nd = ActiveLine.GetNodeFromId(selectedNodeId);

                    if (AppState.MainWin.UiEvalChart.Visibility == System.Windows.Visibility.Visible)
                    {
                        AppState.MainWin.UiEvalChart.SelectMove(nd);
                    }

                    if (selectedNodeId > 0)
                    {
                        ActiveLine.SelectPly((int)nd.Parent.MoveNumber, nd.Parent.ColorToMove);
                    }
                    if (displayPosition)
                    {
                        MainChessBoard.DisplayPosition(nd, true);
                        _openingStatsView.SetOpeningName();
                        // TODO: should this be only called when AppState.AreExplorersOn == true? Check carefully.
                        WebAccessManager.ExplorerRequest(AppState.ActiveTreeId, nd);
                    }
                    if (EvaluationManager.CurrentMode == EvaluationManager.Mode.CONTINUOUS && AppState.ActiveTab != TabViewType.CHAPTERS)
                    {
                        EvaluateActiveLineSelectedPosition(nd);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("SetActiveLine()", ex);
            }
        }

        /// <summary>
        /// Returns th elist of nodes in the current line
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<TreeNode> GetActiveLine()
        {
            return ActiveLine.GetNodeList();
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
                    MainChessBoard.DisplayPosition(nd, true);
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
            AppLog.LogAvailableThreadsCounts();

            if (userRequested)
            {
                distinct = "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                AppLog.DumpVariationTree(DebugUtils.BuildLogFileName(App.AppPath, "wktree", distinct), ActiveVariationTree);
                if (_studyTreeView != null)
                {
                    AppLog.DumpLineSectorTree(DebugUtils.BuildLogFileName(App.AppPath, "lstree", distinct), _studyTreeView.LineManager.LineSectors);
                }
                DebugDumps.DumpStatesAndTimers(DebugUtils.BuildLogFileName(App.AppPath, "timest", distinct));
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
            DebugDumps.DumpStatesAndTimers(DebugUtils.BuildLogFileName(App.AppPath, "timest", distinct));
        }

        /// <summary>
        /// Requests evaluation of a position currently selected
        /// in the Active Line.
        /// </summary>
        private void EvaluateActiveLineSelectedPosition()
        {
            if (ActiveVariationTree == null || AppState.ActiveTab == TabViewType.CHAPTERS)
            {
                return;
            }

            TreeNode nd = ActiveLine.GetSelectedTreeNode();
            if (nd == null)
            {
                nd = ActiveVariationTree.Nodes[0];
            }
            EvaluationManager.SetSingleNodeToEvaluate(nd);
            // stop the timer to prevent showing garbage after position is set but engine has not received our commands yet
            EngineMessageProcessor.RequestPositionEvaluation(nd, ActiveVariationTreeId, Configuration.EngineMpv, 0);
        }

        private void EvaluateActiveLineSelectedPosition(TreeNode nd)
        {
            EvaluationManager.SetSingleNodeToEvaluate(nd);
            EngineMessageProcessor.RequestPositionEvaluation(nd, ActiveVariationTreeId, Configuration.EngineMpv, 0);
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
        public void MoveEvaluationFinishedInTraining(TreeNode nd, bool delayed)
        {
            AppState.ShowMoveEvaluationControls(false, true);
            UiTrainingView.ShowEvaluationResult(nd, delayed);
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
                MessageBox.Show(Properties.Resources.EngineNotAvailable, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AppLog.Message("StartEngineGame() at move " + startNode.LastMoveAlgebraicNotation);

            UiImgMainChessboard.Source = ChessBoards.ChessBoardGreen;

            LearningMode.ChangeCurrentMode(LearningMode.Mode.ENGINE_GAME);

            AppState.SetupGuiForEngineGame();

            EngineGame.InitializeGameObject(startNode, true, IsTraining);
            UiDgEngineGame.ItemsSource = EngineGame.Line.MoveList;

            if (TrainingSession.IsTrainingInProgress && TrainingSession.IsContinuousEvaluation)
            {
                AppState.ShowMoveEvaluationControls(true, true);
            }
            else
            {
                AppState.ShowMoveEvaluationControls(false, false);
            }

            // adjust board orientation
            // Note: if this is in training we let the engine make the first move, otherwise the user will
            if (startNode.ColorToMove == PieceColor.White && TrainingSession.IsTrainingInProgress
                || startNode.ColorToMove == PieceColor.Black && !TrainingSession.IsTrainingInProgress)
            {
                if (!MainChessBoard.IsFlipped)
                {
                    MainChessBoard.FlipBoard();
                }
            }

            if (!TrainingSession.IsTrainingInProgress)
            {
                BoardCommentBox.EngineGameStart();
                _engineGameView = new EngineGameView(UiRtbEngineGame.Document);
                _engineGameView.BuildFlowDocumentForGameLine(startNode.ColorToMove);
                EngineGame.SwitchToAwaitUserMove(startNode);
                EngineGame.EngineColor = MoveUtils.ReverseColor(startNode.ColorToMove);
            }
            else
            {
                EngineMessageProcessor.RequestEngineMove(startNode, ActiveVariationTreeId);
            }
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
                    EngineMessageProcessor.RequestEngineMove(EngineGame.GetLastGameNode(), ActiveVariationTreeId);
                }
            }
        }

        /// <summary>
        /// The app is in the Solving GUESS_MOVE mode
        /// and the user made their move.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void SolvingGuessMoveMadeTimerEvent(object source, ElapsedEventArgs e)
        {
            // stop the timer
            Timers.Stop(AppTimers.TimerId.SOLVING_GUESS_MOVE_MADE);
            if (ActiveArticle.Solver != null)
            {
                ActiveArticle.Solver.ProcessUserMoveInGuessMode();
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
            EngineMessageProcessor.ResetEngineEvaluation();

            LearningMode.ChangeCurrentMode(LearningMode.Mode.MANUAL_REVIEW);
            EngineGame.ChangeCurrentState(EngineGame.GameState.IDLE);

            Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);

            //AppState.MainWin.ActiveVariationTree.BuildLines();
            //RebuildActiveTreeView();

            AppState.SetupGuiForCurrentStates();

            ActiveLine.DisplayPositionForSelectedCell();
            AppState.SwapCommentBoxForEngineLines(false);
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

                int moveTime = AppState.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME ?
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
                // first handle special cases
                if (!HandleSpecialKeys(sender, e))
                {
                    ActiveLine.PreviewKeyDown(sender, e);
                }
            }
        }

        /// <summary>
        /// Handles special cases that need to be handled here and not
        /// in ActiveLine
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool HandleSpecialKeys(object sender, KeyEventArgs e)
        {
            bool handled = false;

            if (Keyboard.Modifiers == 0 && ActiveTreeView != null && AppState.IsVariationTreeTabType)
            {
                TreeNode node = null;
                switch (e.Key)
                {
                    case Key.Up:
                        node = ActiveTreeView.SelectParallelLine(true);
                        handled = true;
                        break;
                    case Key.Down:
                        node = ActiveTreeView.SelectParallelLine(false);
                        handled = true;
                        break;
                }

                if (node != null)
                {
                    SetActiveLine(node.LineId, node.NodeId);
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
            {
                var key = (e.Key == Key.System ? e.SystemKey : e.Key);
                switch (key)
                {
                    case Key.Left:
                        UiImgNavigateBack_MouseDown(null, null);
                        handled = true;
                        break;
                    case Key.Right:
                        UiImgNavigateForward_MouseDown(null, null);
                        handled = true;
                        break;
                    default:
                        break;
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
            {
                var key = (e.Key == Key.System ? e.SystemKey : e.Key);
                switch (key)
                {
                    case Key.OemPlus:
                        if (AppState.ActiveTab == TabViewType.CHAPTERS)
                        {
                            ExpandCollapseChaptersView(true, true);
                            handled = true;
                        }
                        break;
                    case Key.OemMinus:
                        if (AppState.ActiveTab == TabViewType.CHAPTERS)
                        {
                            ExpandCollapseChaptersView(false, true);
                            handled = true;
                        }
                        break;
                    case Key.Home:
                        if (AppState.ActiveTab == TabViewType.CHAPTERS)
                        {
                            UiRtbChaptersView.ScrollToHome();
                            handled = true;
                        }
                        break;
                    case Key.End:
                        if (AppState.ActiveTab == TabViewType.CHAPTERS)
                        {
                            UiRtbChaptersView.ScrollToEnd();
                            handled = true;
                        }
                        break;
                    default:
                        break;
                }
            }
            else if (AppState.ActiveTab == TabViewType.CHAPTERS)
            {
                if (e.Key == Key.PageUp)
                {
                    UiRtbChaptersView.PageUp();
                    handled = true;
                }
                else if (e.Key == Key.PageDown)
                {
                    UiRtbChaptersView.PageDown();
                    handled = true;
                }
            }

            if (handled)
            {
                e.Handled = true;
            }

            return handled;
        }

        /// <summary>
        /// Main Window received a Key Up event.
        /// If we are in Manual Review, pass it on to the ActiveLine.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChessForgeMain_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                ActiveLine.PreviewKeyUp(sender, e);
            }
        }

        /// <summary>
        /// A key pressed event has been received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbStudyTree_PreviewKeyDown(object sender, KeyEventArgs e)
        {
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
        /// Starts a training session from the specified Node.
        /// </summary>
        /// <param name="startNode"></param>
        public void SetAppInTrainingMode(TreeNode startNode, bool isContinuousEvaluation = false)
        {
            if (ActiveVariationTree == null || startNode == null)
            {
                return;
            }

            AppLog.Message("Starting Training Session");

            // Set up the training mode
            StopEvaluation(true);
            StopReplayIfActive();

            LearningMode.ChangeCurrentMode(LearningMode.Mode.TRAINING);
            TrainingSession.IsTrainingInProgress = true;
            TrainingSession.ChangeCurrentState(TrainingSession.State.AWAITING_USER_TRAINING_MOVE);

            AppState.EnableNavigationArrows();

            if (isContinuousEvaluation)
            {
                TrainingSession.IsContinuousEvaluation = true;
            }
            else
            {
                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);
            }

            LearningMode.TrainingSideCurrent = startNode.ColorToMove;
            MainChessBoard.DisplayPosition(startNode, true);

            UiTrainingView = new TrainingView(UiRtbTrainingProgress.Document, this);
            UiTrainingView.Initialize(startNode, ActiveVariationTree.ContentType);
            UiTrainingView.RemoveTrainingMoves(startNode);

            if (LearningMode.TrainingSideCurrent == PieceColor.Black && !MainChessBoard.IsFlipped
                || LearningMode.TrainingSideCurrent == PieceColor.White && MainChessBoard.IsFlipped)
            {
                MainChessBoard.FlipBoard();
            }

            AppState.ShowMoveEvaluationControls(isContinuousEvaluation, isContinuousEvaluation);
            AppState.ShowExplorers(false, false);
            BoardCommentBox.TrainingSessionStart();

            // The Line display is the same as when playing a game against the computer 
            EngineGame.InitializeGameObject(startNode, false, false);
            UiDgEngineGame.ItemsSource = EngineGame.Line.MoveList;
            Timers.Start(AppTimers.TimerId.CHECK_FOR_USER_MOVE);

            if (isContinuousEvaluation)
            {
                UiTrainingView.RequestMoveEvaluation(ActiveVariationTree.TreeId, true);
                AppState.SwapCommentBoxForEngineLines(true);
            }

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

        /// <summary>
        /// Displays Floating Board in Training View
        /// </summary>
        /// <param name="visible"></param>
        public void ShowTrainingFloatingBoard(bool visible)
        {
            this.Dispatcher.Invoke(() =>
            {
                UiVbTrainingFloatingBoard.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            });
        }

        /// <summary>
        /// Displays Floating Board in Chapters View
        /// </summary>
        /// <param name="visible"></param>
        public void ShowChaptersFloatingBoard(bool visible, TabViewType viewType)
        {
            this.Dispatcher.Invoke(() =>
            {
                switch (viewType)
                {
                    case TabViewType.CHAPTERS:
                        UiVbChaptersFloatingBoard.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
                        break;
                    case TabViewType.MODEL_GAME:
                        UiVbModelGameFloatingBoard.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
                        break;
                    case TabViewType.EXERCISE:
                        UiVbExerciseFloatingBoard.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
                        break;
                }
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
                UiTrainingView.EngineGameMoveMade();
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

                MoveUtils.EngineNotationToCoords(engCode, out SquareCoords sqOrig, out SquareCoords sqDest, out _);
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
            BoardCommentBox.StartingEngine();

            StopEvaluation(true);

            EngineMessageProcessor.StopEngineService();
            EngineMessageProcessor.CreateEngineService(this, _isDebugMode);

            bool engineStarted = EngineMessageProcessor.StartEngineService();

            BoardCommentBox.RestoreTitleMessage();
            if (!engineStarted)
            {
                MessageBox.Show(Properties.Resources.LoadEngineError, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else
            {
                BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.EngineReplaced, CommentBox.HintType.INFO);
                return true;
            }
        }

        /// <summary>
        /// Shows the Workbook options dialog.
        /// </summary>
        /// <returns></returns>
        public bool ShowWorkbookOptionsDialog(bool save)
        {
            WorkbookOptionsDialog dlg = new WorkbookOptionsDialog(SessionWorkbook);
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
                SessionWorkbook.Title = dlg.WorkbookTitle;
                SessionWorkbook.TrainingSideConfig = dlg.TrainingSide;
                SessionWorkbook.TrainingSideCurrent = dlg.TrainingSide;

                SessionWorkbook.StudyBoardOrientationConfig = dlg.StudyBoardOrientation;
                SessionWorkbook.GameBoardOrientationConfig = dlg.GameBoardOrientation;
                SessionWorkbook.ExerciseBoardOrientationConfig = dlg.ExerciseBoardOrientation;

                AppState.IsDirty = true;

                if (save)
                {
                    AppState.SaveWorkbookFile(null);
                }

                switch (WorkbookManager.ActiveTab)
                {
                    case TabViewType.CHAPTERS:
                    case TabViewType.STUDY:
                    case TabViewType.BOOKMARKS:
                        MainChessBoard.FlipBoard(EffectiveBoardOrientation(WorkbookManager.ItemType.STUDY));
                        break;
                    case TabViewType.MODEL_GAME:
                        MainChessBoard.FlipBoard(EffectiveBoardOrientation(WorkbookManager.ItemType.MODEL_GAME));
                        break;
                    case TabViewType.EXERCISE:
                        MainChessBoard.FlipBoard(EffectiveBoardOrientation(WorkbookManager.ItemType.EXERCISE));
                        break;
                }

                if (_chaptersView != null)
                {
                    _chaptersView.BuildFlowDocumentForChaptersView();
                }

                BoardCommentBox.ShowTabHints();
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
            AppOptionsDialog dlg = new AppOptionsDialog();
            GuiUtilities.PositionDialog(dlg, this, 100);
            dlg.ShowDialog();

            if (dlg.DialogResult == true)
            {
                if (dlg.LanguageChanged)
                {
                    Configuration.CultureName = dlg.ExitLanguage;
                    Languages.SetSessionLanguage(dlg.ExitLanguage);
                    MessageBox.Show(Properties.Resources.ChangeLanguageNote, Properties.Resources.Language, MessageBoxButton.OK, MessageBoxImage.Information);
                }

                if (dlg.LargeMenuFontChanged)
                {
                    SetMenuFontSize(Configuration.LargeMenuFont ? Constants.LARGE_MENU_FONT_SIZE : DefaultMenuFontSize);
                }

                Configuration.WriteOutConfiguration();

                if (dlg.EngineParamsChanged)
                {
                    EngineMessageProcessor.SendOptionsCommand();
                }

                if (dlg.UseFigurinesChanged)
                {
                    Languages.UseFigurines = Configuration.UseFigurines;
                    ClearTreeViews(true);
                    try
                    {
                        TreeNode nd = ActiveLine.GetSelectedTreeNode();
                        if (nd != null)
                        {
                            string lineId = ActiveVariationTree.GetDefaultLineIdForNode(nd.NodeId);
                            ActiveTreeView.SelectAndHighlightLine(lineId, nd.NodeId);
                            RefreshSelectedActiveLineAndNode();
                        }

                        EngineGame.Line.RefreshMovesText();

                        if (AppState.AreExplorersOn)
                        {
                            WebAccessManager.ExplorerRequest(ActiveVariationTree.TreeId, nd, true);
                        }
                    }
                    catch { }
                }

                if (dlg.ChangedEnginePath)
                {
                    ReloadEngine();
                }
            }
        }

        /// <summary>
        /// Shows the Engine Configuration dialog.
        /// </summary>
        public void ShowEngineOptionsDialog()
        {
            EngineOptionsDialog dlg = new EngineOptionsDialog();
            //{
            //    Left = ChessForgeMain.Left + 100,
            //    Top = ChessForgeMain.Top + 100,
            //    Topmost = false,
            //    Owner = this
            //};
            GuiUtilities.PositionDialog(dlg, this, 100);
            dlg.ShowDialog();

            if (dlg.DialogResult == true)
            {
                Configuration.WriteOutConfiguration();

                if (dlg.EngineParamsChanged)
                {
                    EngineMessageProcessor.SendOptionsCommand();
                }

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
            ChapterTitleDialog dlg = new ChapterTitleDialog(chapter);
            GuiUtilities.PositionDialog(dlg, this, 100);

            bool res = dlg.ShowDialog() == true;

            if (res)
            {
                chapter.SetTitle(dlg.ChapterTitle);
                _chaptersView.BuildFlowDocumentForChaptersView();
                // study tree also shows title so update it there
                if (_studyTreeView == null)
                {
                    _studyTreeView = new StudyTreeView(UiRtbStudyTreeView, GameData.ContentType.STUDY_TREE, -1);
                    _studyTreeView.BuildFlowDocumentForVariationTree();
                }
                else
                {
                    _studyTreeView.UpdateChapterTitle();
                }

                AppState.IsDirty = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Sends the Stop command to the engine
        /// thus forcing a move.
        /// </summary>
        public void ForceEngineMove()
        {
            EngineMessageProcessor.StopEngineEvaluation(false);
        }

        /// <summary>
        /// Stops any evaluation that is currently happening.
        /// Resets evaluation state and adjusts the GUI accordingly. 
        /// </summary>
        public void StopEvaluation(bool ignoreBestMoveResponse, bool updateGui = true)
        {
            EngineMessageProcessor.StopEngineEvaluation(ignoreBestMoveResponse);

            EvaluationManager.Reset(updateGui);

            AppState.ResetEvaluationControls();
            AppState.ShowMoveEvaluationControls(false, true);

            if (LearningMode.CurrentMode == LearningMode.Mode.MANUAL_REVIEW)
            {
                Timers.StopAllEvalTimers();
            }
        }

        /// <summary>
        /// Invokes the Annotations dialog.
        /// </summary>
        /// <param name="nd"></param>
        public bool InvokeAnnotationsDialog(TreeNode nd)
        {
            if (!WorkbookManager.IsAnyArticleTabActive)
            {
                return false;
            }

            bool changed = false;

            if (nd != null)
            {
                EditOperation op = null;

                if (AppState.ActiveVariationTree != null)
                {
                    op = new EditOperation(EditOperation.EditType.UPDATE_ANNOTATION, nd);
                }

                AnnotationsDialog dlg = new AnnotationsDialog(nd);
                GuiUtilities.PositionDialog(dlg, this, 100);
                dlg.ShowDialog();
                if (dlg.ExitOk)
                {
                    if (nd.Comment != dlg.Comment || nd.Nags != dlg.Nags || nd.QuizPoints != dlg.QuizPoints)
                    {
                        changed = true;
                        if (nd.Nags != dlg.Nags)
                        {
                            if (NagUtils.GetMoveEvalNagId(nd.Nags) != NagUtils.GetMoveEvalNagId(dlg.Nags))
                            {
                                nd.Assessment = 0;
                                nd.BestResponse = "";
                            }
                        }

                        nd.Comment = dlg.Comment;
                        nd.SetNags(dlg.Nags);
                        nd.QuizPoints = dlg.QuizPoints;
                        ActiveLine.UpdateMoveText(nd);
                        AppState.IsDirty = true;

                        if (op != null)
                        {
                            AppState.ActiveVariationTree.OpsManager.PushOperation(op);
                        }
                    }
                }
            }

            return changed;
        }

        /// <summary>
        /// Invokes the Comment Before Move dialog.
        /// </summary>
        /// <param name="nd"></param>
        public bool InvokeCommentBeforeMoveDialog(TreeNode nd)
        {
            if (!WorkbookManager.IsAnyArticleTabActive)
            {
                return false;
            }

            bool changed = false;

            if (nd != null && nd.NodeId != 0)
            {
                EditOperation op = null;

                if (AppState.ActiveVariationTree != null)
                {
                    op = new EditOperation(EditOperation.EditType.UPDATE_COMMENT_BEFORE_MOVE, nd);
                }

                CommentBeforeMoveDialog dlg = new CommentBeforeMoveDialog(nd);
                GuiUtilities.PositionDialog(dlg, this, 200);
                if (dlg.ShowDialog() == true)
                {
                    if (nd.CommentBeforeMove != dlg.CommentBeforeMove)
                    {
                        changed = true;
                        nd.CommentBeforeMove = dlg.CommentBeforeMove;
                        AppState.IsDirty = true;

                        if (op != null)
                        {
                            AppState.ActiveVariationTree.OpsManager.PushOperation(op);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show(Properties.Resources.MsgNoMoveSelected, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
                    ctrl.Margin = new Thickness(5, 5, 275, 5);

                    UiDgActiveLine.Visibility = Visibility.Visible;
                    PositionScoresheetLabel(UiDgActiveLine);
                    UiLblScoresheet.Visibility = Visibility.Visible;
                    UiLblScoresheet.Margin = new Thickness(0, 0, 10 + (UiDgActiveLine.Width - UiLblScoresheet.Width), 0);

                    //UiDgEngineGame.Visibility = Visibility.Hidden;
                    break;
                case TabControlSizeMode.HIDE_ACTIVE_LINE:
                    ctrl.Margin = new Thickness(5, 5, 5, 5);
                    UiDgActiveLine.Visibility = Visibility.Hidden;
                    UiLblScoresheet.Visibility = Visibility.Hidden;
                    //UiDgEngineGame.Visibility = Visibility.Hidden;
                    break;
                case TabControlSizeMode.SHOW_ACTIVE_LINE_NO_EVAL:
                    ctrl.Margin = new Thickness(5, 5, 195, 5);
                    UiDgActiveLine.Visibility = Visibility.Visible;
                    PositionScoresheetLabel(UiDgActiveLine);
                    UiLblScoresheet.Visibility = Visibility.Visible;
                    //UiDgEngineGame.Visibility = Visibility.Hidden;
                    break;
                case TabControlSizeMode.SHOW_ENGINE_GAME_LINE:
                    ctrl.Margin = new Thickness(5, 5, 195, 5);
                    UiDgActiveLine.Visibility = Visibility.Hidden;
                    PositionScoresheetLabel(UiDgEngineGame);
                    UiLblScoresheet.Visibility = Visibility.Visible;
                    UiDgEngineGame.Visibility = Visibility.Visible;
                    break;
                case TabControlSizeMode.HIDE_ENGINE_GAME_LINE:
                    ctrl.Margin = new Thickness(5, 5, 5, 5);
                    UiDgActiveLine.Visibility = Visibility.Hidden;
                    UiLblScoresheet.Visibility = Visibility.Hidden;
                    //UiDgEngineGame.Visibility = Visibility.Hidden;
                    break;
                default:
                    ctrl.Margin = new Thickness(5, 5, 190, 5);
                    UiDgActiveLine.Visibility = Visibility.Visible;
                    UiLblScoresheet.Visibility = Visibility.Hidden;
                    break;
            }
        }

        /// <summary>
        /// Adjusts the position of the "Scoresheet" label in relation
        /// to the Scoresheet (DataGrid) control it associated with
        /// </summary>
        /// <param name="dgControl"></param>
        public void PositionScoresheetLabel(DataGrid dgControl)
        {
            UiLblScoresheet.Margin = new Thickness(0, 0, 10 + (dgControl.Width - UiLblScoresheet.Width), 0);
        }

        /// <summary>
        /// Shows the evaluation for the passed node in the Training View.
        /// </summary>
        /// <param name="nd"></param>
        public void ShowEvaluationRunInTraining(TreeNode nd)
        {
            if (nd != null)
            {
                UiTrainingView.ShowEvaluationRun(nd);
            }
        }

        /// <summary>
        /// Resets the board orientation to Workbook default
        /// by clearing the custom setting.
        /// </summary>
        /// <param name="tree"></param>
        private void ResetCustomBoardOrientation(VariationTree tree)
        {
            if (tree != null)
            {
                tree.CustomBoardOrientation = PieceColor.None;
                if (tree.AssociatedPrimary != null)
                {
                    tree.AssociatedPrimary.CustomBoardOrientation = PieceColor.None;
                }
                if (tree.AssociatedSecondary != null)
                {
                    tree.AssociatedSecondary.CustomBoardOrientation = PieceColor.None;
                }
            }
        }

        /// <summary>
        /// Sets custom board orientation.
        /// </summary>
        /// <param name="customOrient"></param>
        /// <param name="itemType"></param>
        private void SetCustomBoardOrientation(PieceColor customOrient, WorkbookManager.ItemType itemType)
        {
            if (ActiveVariationTree != null)
            {
                VariationTree tree = ActiveVariationTree;
                PieceColor config = PieceColor.None;
                switch (itemType)
                {
                    case WorkbookManager.ItemType.STUDY:
                        config = WorkbookManager.SessionWorkbook.StudyBoardOrientationConfig;
                        break;
                    case WorkbookManager.ItemType.MODEL_GAME:
                        config = WorkbookManager.SessionWorkbook.GameBoardOrientationConfig;
                        break;
                    case WorkbookManager.ItemType.EXERCISE:
                        config = WorkbookManager.SessionWorkbook.ExerciseBoardOrientationConfig;
                        break;
                }

                PieceColor orientToSet;
                if (config == customOrient)
                {
                    orientToSet = PieceColor.None;
                }
                else
                {
                    orientToSet = customOrient;
                }

                tree.CustomBoardOrientation = orientToSet;
                if (tree.AssociatedPrimary != null)
                {
                    tree.AssociatedPrimary.CustomBoardOrientation = orientToSet;
                }
                if (tree.AssociatedSecondary != null)
                {
                    tree.AssociatedSecondary.CustomBoardOrientation = orientToSet;
                }
            }
        }

        /// <summary>
        /// Invoked before the context menu shows.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainCanvas_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (WorkbookManager.ActiveTab == TabViewType.STUDY)
            {
                _lastRightClickedPoint = null;
                if (MainChessBoard.Shapes.IsShapeBuildInProgress)
                {
                    MainChessBoard.Shapes.CancelShapeDraw(true);
                }
                AppState.ConfigureMainBoardContextMenu();
                UiMncMainBoard.Visibility = Visibility.Visible;
            }
            else
            {
                UiMncMainBoard.Visibility = Visibility.Collapsed;
                e.Handled = true;
            }
        }

        private void SupressContextMenu(object sender, ContextMenuEventArgs e)
        {
            if (WorkbookManager.SessionWorkbook == null)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// On mouse re-entring check if the left button is released.
        /// If so, the mouse may left the window while dragging so 
        /// cancel dragging.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChessForgeMain_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DraggedPiece.isDragInProgress && e.LeftButton == MouseButtonState.Released)
            {
                DraggedPiece.isDragInProgress = false;
                ReturnDraggedPiece(false);
            }
        }

        /// <summary>
        /// Upon start up or when returning from Training or Engine Game the tab control will receive an IsVisibleChanged 
        /// notification.  We store the active tab when losing visibility and send focus to it when regaining it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabCtrlManualReview_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool visible = (bool)e.NewValue;
            if (visible == true)
            {
                switch (AppState.LastActiveManualReviewTab)
                {
                    case TabViewType.CHAPTERS:
                        UiTabChapters_GotFocus(null, null);
                        break;
                    case TabViewType.STUDY:
                        UiTabStudyTree_GotFocus(null, null);
                        break;
                    case TabViewType.MODEL_GAME:
                        UiTabModelGames_GotFocus(null, null);
                        break;
                    case TabViewType.EXERCISE:
                        UiTabExercises_GotFocus(null, null);
                        break;
                    case TabViewType.BOOKMARKS:
                        UiTabBookmarks_GotFocus(null, null);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                AppState.LastActiveManualReviewTab = WorkbookManager.ActiveTab;
            }
        }

        /// <summary>
        /// Prevent Bookmarks context menu from opening when there are no Bookmarks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBookmarkMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (WorkbookManager.SessionWorkbook == null || BookmarkManager.BookmarkCount == 0)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Selection in the ComboBox with the chapter list has changed in the Bookmarks view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiComboBoxBmChapters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BookmarkManager.ComboBoxChaptersSelectionChanged();
        }

        /// <summary>
        /// Selection in the ComboBox with the content type list has changed in the Bookmarks view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiComboBoxBmContent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BookmarkManager.ComboBoxContentSelectionChanged();
        }

        /// <summary>
        /// A key was pressed in the Intro view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbIntroView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (GuiUtilities.InsertFigurine(UiRtbIntroView, sender, e))
            {
                e.Handled = true;
            }
            else
            {
                if (_introView != null)
                {
                    _introView.PreviewKeyDown(sender, e);
                }
            }
        }

        /// <summary>
        /// Handles key down events from the main window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (TrainingSession.IsTrainingInProgress && UiTrainingView != null)
            {
                UiTrainingView.ProcessKeyDown(e);
            }
            else
            {
                if (AppState.ActiveTab == TabViewType.INTRO)
                {
                    _introView.PreviewKeyDown(sender, e);
                }
                else
                {
                    ChessForgeMain_PreviewKeyDown(sender, e);
                }
            }
        }

        /// <summary>
        /// Handles key up events from the main window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            ChessForgeMain_PreviewKeyUp(sender, e);
        }

        /// <summary>
        /// Hover over the host bar detected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblEvalBarHost_MouseMove(object sender, MouseEventArgs e)
        {
            ShowFloatEval(true);
        }

        /// <summary>
        /// Hover over the "white" bar detected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblEvalBarWhite_MouseMove(object sender, MouseEventArgs e)
        {
            ShowFloatEval(true);
        }

        /// <summary>
        /// Hover over the "float" bar detected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblEvalFloat_MouseMove(object sender, MouseEventArgs e)
        {
            ShowFloatEval(true);
        }

        /// <summary>
        /// The mouse left the host bar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblEvalBarHost_MouseLeave(object sender, MouseEventArgs e)
        {
            ShowFloatEval(false);
        }

        /// <summary>
        /// The mouse left the "white" bar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblEvalBarWhite_MouseLeave(object sender, MouseEventArgs e)
        {
            ShowFloatEval(false);
        }

        /// <summary>
        /// The mouse left the "float" bar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblEvalFloat_MouseLeave(object sender, MouseEventArgs e)
        {
            ShowFloatEval(false);
        }

        /// <summary>
        /// Show or collapse the floating eval label
        /// </summary>
        /// <param name="show"></param>
        private void ShowFloatEval(bool show)
        {
            UiLblEvalFloat.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Intro's RichTextBox editing command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnIntroUndo_Click(object sender, RoutedEventArgs e)
        {
            _introView?.Command_Undo(sender, e);
        }

        /// <summary>
        /// Intro's RichTextBox editing command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnIntroToggleBold_Click(object sender, RoutedEventArgs e)
        {
            _introView?.Command_ToggleBold(sender, e);
        }

        /// <summary>
        /// Intro's RichTextBox editing command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnIntroToggleItalic_Click(object sender, RoutedEventArgs e)
        {
            _introView?.Command_ToggleItalic(sender, e);
        }

        /// <summary>
        /// Intro's RichTextBox editing command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnIntroToggleUnderline_Click(object sender, RoutedEventArgs e)
        {
            _introView?.Command_ToggleUnderline(sender, e);
        }

        /// <summary>
        /// Intro's RichTextBox editing command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnIntroFontSizeUp_Click(object sender, RoutedEventArgs e)
        {
            _introView?.Command_FontSizeUp(sender, e);
        }

        /// <summary>
        /// Intro's RichTextBox editing command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnIntroFontSizeDown_Click(object sender, RoutedEventArgs e)
        {
            _introView?.Command_FontSizeDown(sender, e);
        }

        /// <summary>
        /// Intro's RichTextBox editing command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnIntroIncreaseIndent_Click(object sender, RoutedEventArgs e)
        {
            _introView?.Command_IncreaseIndent(sender, e);
        }

        /// <summary>
        /// Intro's RichTextBox editing command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnIntroDecreaseIndent_Click(object sender, RoutedEventArgs e)
        {
            _introView?.Command_DecreaseIndent(sender, e);
        }

        /// <summary>
        /// Block all keys in Engine Game mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabEngineGame_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Block all keys in Engine Game mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTabEngineGame_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }
    }
}

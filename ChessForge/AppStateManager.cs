using System;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using ChessPosition;
using GameTree;
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
    public class AppStateManager
    {
        // main application window
        private static MainWindow _mainWin;

        /// <summary>
        /// Types of files that Chess Forge can handle.
        /// PGN can only be viewed, not edited.
        /// CHF can be viewed and edited.
        /// </summary>
        public enum FileType
        {
            NONE,
            LEGACY_CHF,
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
                else if (Path.GetExtension(_workbookFilePath).ToLower() == ".chf")
                {
                    _workbookFileType = FileType.LEGACY_CHF;
                }
                else
                {
                    _workbookFileType = FileType.CHESS_FORGE_PGN;
                }
            }
        }

        /// <summary>
        /// Determines the type of the file based on its extension
        /// (chf or pgn) and its first header. 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static FileType DetermineFileType(string path)
        {
            FileType ft = FileType.NONE;
            try
            {
                if (Path.GetExtension(_workbookFilePath).ToLower() == ".chf")
                {
                    ft = FileType.LEGACY_CHF;
                }
                else if (Path.GetExtension(_workbookFilePath).ToLower() == ".pgn")
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                string name = PgnHeaders.ParsePgnHeaderLine(line, out string value);
                                if (name == PgnHeaders.NAME_WORKBOOK_TITLE)
                                {
                                    ft = FileType.CHESS_FORGE_PGN;
                                }
                                else
                                {
                                    ft = FileType.GENERIC_PGN;
                                }
                                break;
                            }
                        }
                    }
                }
                else
                {
                    ft = FileType.NONE;
                }
            }
            catch (Exception ex)
            {
                DebugUtils.ShowDebugMessage("Exception in DetermineFileType(): " + ex.Message);
            }

            return ft;
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

            if (WorkbookFileType == FileType.CHESS_FORGE_PGN)
            {
                //string chfText = ChfTextBuilder.BuildText(AppStateManager.MainWin.StudyTree);
                string chfText = ChfTextBuilder.BuildWorkbookText();
                File.WriteAllText(WorkbookFilePath, chfText);
                _isDirty = false;
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
            _mainWin.Dispatcher.Invoke(() =>
            {
                _mainWin.ActiveLine.Clear();
                _mainWin.UiRtbWorkbookView.Document.Blocks.Clear();
                _mainWin.UiRtbTrainingProgress.Document.Blocks.Clear();

                _mainWin.ResetEvaluationProgressBae();

                EngineGame.ChangeCurrentState(EngineGame.GameState.IDLE);
                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);

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


        public static void SetupGuiForTrainingBrowseMode()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                TrainingSession.IsBrowseActive = true;
                _mainWin.UiTabCtrlTraining.Margin = new Thickness(185, 5, 5, 5);
                _mainWin.UiDgEngineGame.Visibility = Visibility.Hidden;

                _mainWin.UiDgActiveLine.Visibility = Visibility.Visible;
                _mainWin.UiDgActiveLine.Columns[2].Visibility = Visibility.Collapsed;
                _mainWin.UiDgActiveLine.Columns[4].Visibility = Visibility.Collapsed;
                _mainWin.UiDgActiveLine.Width = 160;
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

                if (!string.IsNullOrEmpty(WorkbookFilePath) && IsDirty && WorkbookFileType == FileType.LEGACY_CHF)
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

                _mainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardBlue;

                _mainWin.UiDgActiveLine.Visibility = Visibility.Visible;
                _mainWin.UiDgEngineGame.Visibility = Visibility.Hidden;

                _mainWin.UiTabCtrlManualReview.Visibility = Visibility.Visible;
                _mainWin.UiTabCtrlTraining.Visibility = Visibility.Hidden;

                _mainWin.UiTabWorkbook.Visibility = Visibility.Visible;
                _mainWin.UiTabBookmarks.Visibility = Visibility.Visible;

                _mainWin.UiTabTrainingProgress.Visibility = Visibility.Hidden;

                // these tabs may have been disabled for the engine game
                _mainWin.UiRtbWorkbookView.Opacity = 1;
                _mainWin.UiRtbWorkbookView.IsEnabled = true;

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
        /// Sets up GUI elements for the Training mode.
        /// </summary>
        private static void SetupGuiForTraining()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                _mainWin.UiMnCloseWorkbook.Visibility = Visibility.Visible;

                _mainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardGreen;

                _mainWin.UiDgActiveLine.Visibility = Visibility.Hidden;
                ShowGuiEngineGameLine(false);

                _mainWin.UiTabCtrlManualReview.Visibility = Visibility.Hidden;
                _mainWin.UiTabCtrlTraining.Visibility = Visibility.Visible;

                _mainWin.UiTabWorkbook.Visibility = Visibility.Hidden;
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
                    _mainWin.UiDgEngineGame.Visibility = Visibility.Visible;

                    _mainWin.UiTabCtrlManualReview.Visibility = Visibility.Hidden;
                    _mainWin.UiTabWorkbook.Visibility = Visibility.Hidden;
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
                    _mainWin.UiDgEngineGame.Visibility = Visibility.Visible;

                    _mainWin.UiTabCtrlManualReview.Visibility = Visibility.Visible;
                    _mainWin.UiTabWorkbook.Visibility = Visibility.Visible;
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
        private static void ConfigureMainBoardContextMenu()
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
                _mainWin.UiDgActiveLine.Visibility = Visibility.Visible;
                _mainWin.UiDgActiveLine.Columns[2].Visibility = includeEvals ? Visibility.Visible : Visibility.Hidden;
                _mainWin.UiDgActiveLine.Columns[4].Visibility = includeEvals ? Visibility.Visible : Visibility.Hidden;
                _mainWin.UiDgActiveLine.Width = includeEvals ? 260 : 160;

                // adjust tab controls position
                _mainWin.UiTabCtrlManualReview.Margin = includeEvals ? new Thickness(275, 5, 5, 5) : new Thickness(175, 5, 5, 5);
                _mainWin.UiTabCtrlTraining.Margin = includeEvals ? new Thickness(185, 5, 5, 5) : new Thickness(5, 5, 5, 5);
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
                _mainWin.UiDgEngineGame.Visibility = show ? Visibility.Visible : Visibility.Hidden;
                _mainWin.UiDgEngineGame.Width = 160;

                // adjust tab controls position
                if (TrainingSession.IsTrainingInProgress)
                {
                    _mainWin.UiTabCtrlTraining.Margin = show ? new Thickness(180, 5, 5, 5) : new Thickness(5, 5, 5, 5);
                }
                else
                {
                    _mainWin.UiTabCtrlManualReview.Margin = show ? new Thickness(180, 5, 5, 5) : new Thickness(5, 5, 5, 5);

                    _mainWin.UiTabWorkbook.Focus();
                    _mainWin.UiRtbWorkbookView.Opacity = 0.1;
                    _mainWin.UiRtbWorkbookView.IsEnabled = false;

                    _mainWin.UiTabBookmarks.Opacity = 0.1;
                    _mainWin.UiTabBookmarks.IsEnabled = false;
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

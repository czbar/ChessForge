using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessPosition;
using GameTree;

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
        /// Main application window
        /// </summary>
        public static MainWindow MainWin { get => _mainWin; set => _mainWin = value; }

        /// <summary>
        /// Current Learning Mode
        /// </summary>
        public static LearningMode.Mode CurrentLearningMode
        {
            get { return LearningMode.CurrentMode; }
            set { LearningMode.CurrentMode = value; }
        }

        /// <summary>
        /// Current Evaluation State
        /// </summary>
        public static EvaluationState.EvaluationMode CurrentEvaluationMode
        {
            get { return MainWin.Evaluation.CurrentMode; }
        }

        /// <summary>
        /// Adjusts the GUI to the changed Evaluation state.
        /// The state of the GUI will depend on the Learning Mode
        /// and Game State.
        /// </summary>
        /// <param name="mode"></param>
        public static void SetCurrentEvaluationMode(EvaluationState.EvaluationMode mode)
        {
            MainWin.Evaluation.SetCurrentMode(mode);
            ShowEvaluationProgressControlsForCurrentStates();
        }

        /// <summary>
        /// Current Game State.
        /// </summary>
        public static EngineGame.GameState CurrentGameState
        {
            get { return EngineGame.CurrentState; }
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
            ShowEvaluationProgressControlsForCurrentStates();
            ConfigureMainBoardContextMenu();
        }

        /// <summary>
        /// Sets up GUI elements for the Manual Review mode.
        /// </summary>
        private static void SetupGuiForManualReview()
        {
            _mainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardBlue;

            _mainWin.UiDgActiveLine.Visibility = Visibility.Visible;
            _mainWin.UiDgEngineGame.Visibility = Visibility.Hidden;

            _mainWin.UiTabCtrlManualReview.Visibility = Visibility.Visible;
            _mainWin.UiTabCtrlTraining.Visibility = Visibility.Hidden;

            _mainWin.UiTabWorkbook.Visibility = Visibility.Visible;
            _mainWin.UiTabBookmarks.Visibility = Visibility.Visible;

            _mainWin.UiTabTrainingProgress.Visibility = Visibility.Hidden;
            _mainWin.UiTabTrainingBrowse.Visibility = Visibility.Hidden;

            _mainWin.UiSldReplaySpeed.Visibility = Visibility.Hidden;

            // these tabs may have been disabled for the engine game
            _mainWin.UiRtbWorkbookView.Opacity = 1;
            _mainWin.UiRtbWorkbookView.IsEnabled = true;

            _mainWin.UiTabBookmarks.Opacity = 1;
            _mainWin.UiTabBookmarks.IsEnabled = true;

            _mainWin.UiBtnExitTraining.Visibility = Visibility.Collapsed;
            _mainWin.UiBtnExitGame.Visibility = Visibility.Collapsed;

            ShowGuiActiveLine(true);
            ShowEvaluationProgressControlsForCurrentStates();

            ConfigureMenusForManualReview();
        }

        /// <summary>
        /// Sets up GUI elements for the Training mode.
        /// </summary>
        private static void SetupGuiForTraining()
        {
            _mainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardGreen;

            _mainWin.UiDgActiveLine.Visibility = Visibility.Hidden;
            ShowGuiEngineGameLine(false);

            _mainWin.UiTabCtrlManualReview.Visibility = Visibility.Hidden;
            _mainWin.UiTabCtrlTraining.Visibility = Visibility.Visible;

            _mainWin.UiTabWorkbook.Visibility = Visibility.Hidden;
            _mainWin.UiTabBookmarks.Visibility = Visibility.Hidden;

            _mainWin.UiTabTrainingProgress.Visibility = Visibility.Visible;
            _mainWin.UiTabTrainingBrowse.Visibility = Visibility.Visible;

            // this tab may have been disabled for the engine game 
            _mainWin.UiTabTrainingBrowse.Opacity = 1;
            _mainWin.UiTabTrainingBrowse.IsEnabled = true;

            _mainWin.UiBtnExitTraining.Visibility = Visibility.Visible;
            _mainWin.UiBtnExitGame.Visibility = Visibility.Collapsed;

            ShowEvaluationProgressControlsForCurrentStates();

            ConfigureMenusForTraining();
        }

        /// <summary>
        /// Sets up GUI elements for the Training mode.
        /// </summary>
        private static void SetupGuiForEngineGame()
        {
            if (TrainingState.IsTrainingInProgress)
            {
                _mainWin.UiImgMainChessboard.Source = ChessBoards.ChessBoardGreen;

                _mainWin.UiDgActiveLine.Visibility = Visibility.Hidden;
                _mainWin.UiDgEngineGame.Visibility = Visibility.Visible;

                _mainWin.UiTabCtrlManualReview.Visibility = Visibility.Hidden;
                _mainWin.UiTabWorkbook.Visibility = Visibility.Hidden;
                _mainWin.UiTabBookmarks.Visibility = Visibility.Hidden;

                _mainWin.UiTabCtrlTraining.Visibility = Visibility.Visible;
                _mainWin.UiTabTrainingProgress.Visibility = Visibility.Visible;
                _mainWin.UiTabTrainingBrowse.Visibility = Visibility.Visible;

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
                _mainWin.UiTabTrainingBrowse.Visibility = Visibility.Hidden;

                _mainWin.UiBtnExitTraining.Visibility = Visibility.Collapsed;
                _mainWin.UiBtnExitGame.Visibility = Visibility.Visible;
            }

            ShowEvaluationProgressControlsForCurrentStates();
            ShowGuiEngineGameLine(true);

            ConfigureMenusForEngineGame();
        }

        /// <summary>
        /// Configures menu items for the Manual Review mode
        /// </summary>
        private static void ConfigureMenusForManualReview()
        {
            _mainWin.UiMnStartTraining.IsEnabled = true;
            _mainWin.UiMnRestartTraining.IsEnabled = false;
            _mainWin.UiMnExitTraining.IsEnabled = false;

            _mainWin.UiMnciPlayEngine.IsEnabled = true;
        }

        /// <summary>
        /// Configures menu items for the Training mode
        /// </summary>
        private static void ConfigureMenusForTraining()
        {
            _mainWin.UiMnStartTraining.IsEnabled = false;
            _mainWin.UiMnRestartTraining.IsEnabled = true;
            _mainWin.UiMnExitTraining.IsEnabled = true;

            _mainWin.UiMnciPlayEngine.IsEnabled = false;
        }

        /// <summary>
        /// Configures menu items for the Engine Game mode
        /// </summary>
        private static void ConfigureMenusForEngineGame()
        {
            bool train = TrainingState.IsTrainingInProgress;

            _mainWin.UiMnStartTraining.IsEnabled = !train;
            _mainWin.UiMnRestartTraining.IsEnabled = train;
            _mainWin.UiMnExitTraining.IsEnabled = train;

            _mainWin.UiMnciPlayEngine.IsEnabled = true;
        }

        /// <summary>
        /// Configure the Main Board's context menu.
        /// </summary>
        private static void ConfigureMainBoardContextMenu()
        {
            switch (CurrentLearningMode)
            {
                case LearningMode.Mode.MANUAL_REVIEW:
                    _mainWin.UiMnciStartTraining.Visibility = Visibility.Visible;
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
                    if (TrainingState.IsTrainingInProgress)
                    {
                        _mainWin.UiMnciStartTraining.Visibility = Visibility.Collapsed;
                        _mainWin.UiMnciRestartTraining.Visibility = Visibility.Visible;
                        _mainWin.UiMnciExitTraining.Visibility = Visibility.Visible;

                        _mainWin.UiMncMainBoardSepar_1.Visibility = Visibility.Collapsed;

                        _mainWin.UiMnciExitEngineGame.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        _mainWin.UiMnciStartTraining.Visibility = Visibility.Visible;
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
        }

        /// <summary>
        /// Shows/hides the engine evaluation progress bar, labels,
        /// and the menu items for move and line evaluation.
        /// </summary>
        public static void ShowEvaluationProgressControlsForCurrentStates()
        {
            bool eval = MainWin.Evaluation.IsRunning;
            bool game = EngineGame.CurrentState != EngineGame.GameState.IDLE;

            _mainWin.Dispatcher.Invoke(() =>
                {
                    if (eval)
                    {
                        _mainWin.UiMnciEvalLine.IsEnabled = false;
                        _mainWin.UiMnciEvalPos.IsEnabled = false;

                        _mainWin.UiPbEngineThinking.Visibility = Visibility.Visible;
                        _mainWin.UiImgStop.Visibility = Visibility.Visible;

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
                    else
                    {
                        _mainWin.UiMnciEvalLine.IsEnabled = true;
                        _mainWin.UiMnciEvalPos.IsEnabled = true;

                        _mainWin.UiPbEngineThinking.Visibility = Visibility.Hidden;
                        _mainWin.UiImgStop.Visibility = Visibility.Hidden;
                        _mainWin.UiLblEvaluating.Visibility = Visibility.Hidden;
                        _mainWin.UiLblMoveUnderEval.Visibility = Visibility.Hidden;
                        _mainWin.UiLblEvalSecretMode.Visibility = Visibility.Hidden;
                    }
                });
        }


#if false
        /// <summary>
        /// Shows/hides evaluation progress controls depending on the
        /// relevant states/modes in the application.
        /// These are the controls showing the lines being evaluated,
        /// what move is being evaluated and such.
        /// In particular in the context of a game, whether in MANUAL_REVIEW or TRAINING
        /// we do not want to show the lines being evaluated so that it feels like 
        /// a "real" game.
        /// </summary>
        public static void ShowEvaluationProgressControls()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                if (_mainWin.Evaluation.CurrentMode == EvaluationState.EvaluationMode.IDLE)
                {
                    _mainWin.UiImgStop.Visibility = Visibility.Hidden;
                    _mainWin.UiPbEngineThinking.Visibility = Visibility.Hidden;

                    _mainWin.UiLblEvaluating.Visibility = Visibility.Hidden;
                    _mainWin.UiLblMoveUnderEval.Visibility = Visibility.Hidden;

                    _mainWin.UiLblEvalSecretMode.Visibility = Visibility.Hidden;
                }
                else if (LearningMode.CurrentMode == LearningMode.Mode.ENGINE_GAME
                        && (!TrainingState.IsTrainingInProgress || _mainWin.Evaluation.CurrentMode == EvaluationState.EvaluationMode.ENGINE_GAME))
                {
                    bool think = EngineGame.CurrentState == EngineGame.GameState.ENGINE_THINKING;
                    _mainWin.UiImgStop.Visibility = think ? Visibility.Visible : Visibility.Hidden;
                    _mainWin.UiPbEngineThinking.Visibility = Visibility.Visible;

                    _mainWin.UiLblEvaluating.Visibility = Visibility.Hidden;
                    _mainWin.UiLblMoveUnderEval.Visibility = Visibility.Hidden;

                    _mainWin.UiLblEvalSecretMode.Visibility = think ? Visibility.Visible : Visibility.Hidden;
                }
                else
                {
                    _mainWin.UiImgStop.Visibility = Visibility.Visible;
                    _mainWin.UiPbEngineThinking.Visibility = Visibility.Visible;

                    _mainWin.UiLblEvaluating.Visibility = Visibility.Visible;
                    _mainWin.UiLblMoveUnderEval.Visibility = Visibility.Visible;

                    _mainWin.UiLblEvalSecretMode.Visibility = Visibility.Hidden;
                }
            });
        }
#endif

        /// <summary>
        /// Shows ActiveLine's DataGrid control.
        /// The width, as well as the size of the Tab controls depends on whether
        /// we are showing evaluations as well.
        /// </summary>
        /// <param name="includeEvals"></param>
        private static void ShowGuiActiveLine(bool includeEvals)
        {
            _mainWin.UiDgActiveLine.Visibility = Visibility.Visible;
            _mainWin.UiDgActiveLine.Columns[2].Visibility = includeEvals ? Visibility.Visible : Visibility.Hidden;
            _mainWin.UiDgActiveLine.Columns[4].Visibility = includeEvals ? Visibility.Visible : Visibility.Hidden;
            _mainWin.UiDgActiveLine.Width = includeEvals ? 260 : 160;

            // adjust tab controls position
            _mainWin.UiTabCtrlManualReview.Margin = includeEvals ? new Thickness(275, 5, 5, 5) : new Thickness(175, 5, 5, 5);
            _mainWin.UiTabCtrlTraining.Margin = includeEvals ? new Thickness(185, 5, 5, 5) : new Thickness(5, 5, 5, 5);
        }

        /// <summary>
        /// Shows or hides EngineGame's DataGrid control.
        /// </summary>
        /// <param name="show"></param>
        private static void ShowGuiEngineGameLine(bool show)
        {
            _mainWin.UiDgEngineGame.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            _mainWin.UiDgEngineGame.Width = 160;

            // adjust tab controls position
            if (TrainingState.IsTrainingInProgress)
            {
                _mainWin.UiTabCtrlTraining.Margin = show ? new Thickness(180, 5, 5, 5) : new Thickness(5, 5, 5, 5);

                _mainWin.UiTabTrainingBrowse.Opacity = 0.3;
                _mainWin.UiTabTrainingBrowse.IsEnabled = false;
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
        public static string PrepareMoveEvaluation(BoardPosition position)
        {
            PrepareEvaluationControls();

            _mainWin.Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
            _mainWin.Timers.Start(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);

            return FenParser.GenerateFenFromPosition(position);
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
                if (visible)
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
                ShowEvaluationProgressControlsForCurrentStates();
            });

        }


        public static void SetupGuiForTrainingBrowseMode()
        {
            TrainingState.IsBrowseActive = true;
            _mainWin.UiTabCtrlTraining.Margin = new Thickness(185, 5, 5, 5);
            _mainWin.UiDgEngineGame.Visibility = Visibility.Hidden;

            _mainWin.UiDgActiveLine.Visibility = Visibility.Visible;
            _mainWin.UiDgActiveLine.Columns[2].Visibility = Visibility.Collapsed;
            _mainWin.UiDgActiveLine.Columns[4].Visibility = Visibility.Collapsed;
            _mainWin.UiDgActiveLine.Width = 160;
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
                TrainingState.IsBrowseActive = false;
                _mainWin.UiTabCtrlTraining.Margin = new Thickness(5, 5, 5, 5);
                _mainWin.UiDgEngineGame.Visibility = Visibility.Hidden;
                _mainWin.UiDgActiveLine.Visibility = Visibility.Hidden;

                _mainWin.DisplayPosition(EngineGame.GetCurrentPosition());
            }
        }

        private static void PrepareEvaluationControls()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                _mainWin.UiMnciEvalLine.IsEnabled = false;
                _mainWin.UiMnciEvalPos.IsEnabled = false;

                _mainWin.UiPbEngineThinking.Visibility = Visibility.Visible;
                _mainWin.UiPbEngineThinking.Minimum = 0;
                // add 50% to compensate for any processing delays, we don't want to be too optimistic
                _mainWin.UiPbEngineThinking.Maximum = (int)(Configuration.EngineEvaluationTime * 1.5);
                _mainWin.UiPbEngineThinking.Value = 0;
            });
        }

    }
}

using ChessForge.Properties;
using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Manages the RichTextBox's FlowDocument that displays hints to the user next to (below) the 
    /// main chess board.
    /// This includes e.g. prompts to make a move or wait for the engine's move when
    /// playing against the computer.
    /// </summary>
    public class CommentBox : RichTextBuilder
    {
        /// <summary>
        /// Type of comment being displayed.
        /// </summary>
        public enum HintType
        {
            ERROR,  // red
            INFO,   // green
            PROGRESS // blue
        }

        // flags whether a special message is currently being shown
        public bool HasSpecialMessage = false;

        // main application window
        private MainWindow _mainWin;

        /// <summary>
        /// Copy of the document to hold while a flash notification
        /// is displayed.
        /// </summary>
        private FlowDocument _docOnHold = new FlowDocument();

        // holds the original visibility status during the "flash announcement" 
        private bool _engineLinesVisible;

        // flags whether a "flash announcement" is currently being shown
        private bool _flashAnnouncementShown;


        /// <summary>
        /// Instantiates the object. Sets references to the main application
        /// window and the Flow Document.
        /// </summary>
        /// <param name="rtb"></param>
        /// <param name="mainWin"></param>
        public CommentBox(RichTextBox rtb, MainWindow mainWin) : base(rtb)
        {
            _mainWin = mainWin;
        }

        override internal Dictionary<string, RichTextPara> RichTextParas { get { return _richTextParas; } }

        /// <summary>
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        internal Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            ["title"] = new RichTextPara(0, 0, 24, FontWeights.Bold, TextAlignment.Center),
            ["big_red"] = new RichTextPara(0, 10, 20, FontWeights.Bold, TextAlignment.Center),
            ["user_wait"] = new RichTextPara(0, 10, 20, FontWeights.Bold, TextAlignment.Center),
            ["bold_prompt"] = new RichTextPara(0, 5, 14, FontWeights.Bold, TextAlignment.Center),
            ["eval_results"] = new RichTextPara(30, 5, 14, FontWeights.Normal, TextAlignment.Center),
            ["normal"] = new RichTextPara(0, 0, 12, FontWeights.Normal, TextAlignment.Center),
            ["normal_14"] = new RichTextPara(0, 0, 12, FontWeights.Normal, TextAlignment.Center),
            ["default"] = new RichTextPara(10, 5, 12, FontWeights.Normal, TextAlignment.Center),
            ["dummy"] = new RichTextPara(0, 16, 10, FontWeights.Normal, TextAlignment.Center),
            ["bold_16"] = new RichTextPara(0, 0, 16, FontWeights.Bold, TextAlignment.Center),
            ["bold_18"] = new RichTextPara(0, 5, 18, FontWeights.Bold, TextAlignment.Center),
            ["end_of_game"] = new RichTextPara(0, 5, 18, FontWeights.Bold, TextAlignment.Center),
        };

        /// <summary>
        /// When the user switches between the Light and Dark mode, we cannot easily repaint the comment box as
        /// we don't keep track of what is exactly displayed there.
        /// Therefore, we simply apply the CurrentTheme.RtbForeground and CurrentTheme.RtbBackground
        /// to all Paragraphs and Runs.
        /// It will be exactly what's needed in most cases and in others, temporarily the colors may be a little
        /// different than expected. The effort to make it perfect would not be justified.
        /// </summary>
        public void UpdateColorTheme()
        {
            foreach (Block block in HostRtb.Document.Blocks)
            {
                if (block is Paragraph para)
                {
                    para.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
                    para.Background = ChessForgeColors.CurrentTheme.RtbBackground;
                    foreach (Inline inl in para.Inlines)
                    {
                        if (inl is Run run)
                        {
                            run.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
                            run.Background = ChessForgeColors.CurrentTheme.RtbBackground;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Displays the last move made by the user.
        /// </summary>
        /// <param name="nd"></param>
        public void GameMoveMade(TreeNode nd, bool userMove)
        {
            HostRtb.Document.Blocks.Clear();
            AddNewParagraphToDoc(HostRtb.Document,"dummy", "");

            // user moved and it was not user replacing the last engine move in Training
            if (userMove && (!TrainingSession.IsTrainingInProgress || nd.ColorToMove != TrainingSession.TrainingSide))
            {
                AddNewParagraphToDoc(HostRtb.Document, "normal", Properties.Resources.YourMoveWas + ":");
                uint moveNumberOffset = 0;
                if (AppState.ActiveVariationTree != null)
                {
                    moveNumberOffset = AppState.ActiveVariationTree.MoveNumberOffset;
                }
                AddNewParagraphToDoc(HostRtb.Document, "bold_prompt", MoveUtils.BuildSingleMoveText(nd, true, false, moveNumberOffset));
                if (AppState.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
                {
                    AddNewParagraphToDoc(HostRtb.Document, "normal", Properties.Resources.WaitForEngineResponse);
                }
                else if (TrainingSession.IsTrainingInProgress)
                {
                    AddNewParagraphToDoc(HostRtb.Document, "normal", Properties.Resources.WaitForResponse);
                }
            }
            else // engine or "coach" moved
            {
                if (AppState.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
                {
                    AddNewParagraphToDoc(HostRtb.Document, "normal", Properties.Resources.EnginePlayed + ":");
                }
                else
                {
                    AddNewParagraphToDoc(HostRtb.Document, "normal", Properties.Resources.CoachPlayed + ":");
                }
                uint moveNumberOffset = 0;
                if (AppState.ActiveVariationTree != null)
                {
                    moveNumberOffset = AppState.ActiveVariationTree.MoveNumberOffset;
                }
                AddNewParagraphToDoc(HostRtb.Document, "bold_16", MoveUtils.BuildSingleMoveText(nd, true, false, moveNumberOffset));
                AddNewParagraphToDoc(HostRtb.Document, "normal", Properties.Resources.YourTurn);
            }
            HasSpecialMessage = false;
        }

        /// <summary>
        /// Prompt the user to make a replacement move for the passed engine move.
        /// </summary>
        /// <param name="nd"></param>
        public void GameEngineReplacementToMake(TreeNode nd)
        {
            HostRtb.Document.Blocks.Clear();
            AddNewParagraphToDoc(HostRtb.Document, "dummy", "");

            try
            {
                uint moveNumberOffset = 0;
                if (AppState.ActiveVariationTree != null)
                {
                    moveNumberOffset = AppState.ActiveVariationTree.MoveNumberOffset;
                }
                AddNewParagraphToDoc(HostRtb.Document, "bold_16", Properties.Resources.MakeMoveForEngine);
            }
            catch { }
            HasSpecialMessage = false;
        }


        /// <summary>
        /// A message about the chess engine being loaded
        /// displayed during the application's startup.
        /// </summary>
        public void StartingEngine()
        {
            UserWaitAnnouncement(Properties.Resources.ChessEngineLoading, HintType.PROGRESS);
        }

        /// <summary>
        /// A message to display while the workbook is being read
        /// and processed.
        /// </summary>
        public void ReadingFile()
        {
            UserWaitAnnouncement(Properties.Resources.ReadingWorkbookFile, HintType.PROGRESS);
        }

        /// <summary>
        /// A message to display progress while workbook items are being read and processed.
        /// </summary>
        /// <param name="itemNo"></param>
        /// <param name="itemCount"></param>
        public void ReadingItems(int itemNo, int itemCount, GameData game = null, long ticks = 0)
        {
            string msg = Properties.Resources.ReadingItems;
            msg = msg.Replace("$0", itemNo.ToString());
            msg = msg.Replace("$1", itemCount.ToString());

            List<string> subLines = null;
            if (game != null)
            {
                TimeSpan ts = TimeSpan.FromTicks(DateTime.Now.Ticks - ticks);
                if (ts.Seconds > 1)
                {
                    subLines = new List<string>();
                    string typ = GuiUtilities.GetGameDataTypeString(game);
                    subLines.Add(Properties.Resources.LargeItem + " (" + typ + "): " + GuiUtilities.GetGameDataTitle(game));

                    string tsString = ts.TotalSeconds.ToString("F1");
                    subLines.Add(Properties.Resources.ProcessingTimeSecs + ": " + tsString);
                }
            }

            UserWaitAnnouncement(msg, HintType.PROGRESS, subLines);
        }

        /// <summary>
        /// A prompt to the user to open a Workbook.
        /// </summary>
        public void OpenFile()
        {
            UserWaitAnnouncement(Properties.Resources.cbUseMenuToOpenWorkbook, HintType.PROGRESS);
        }

        /// <summary>
        /// Shows a "flash announcement" and starts a timer
        /// to close it later on.
        /// </summary>
        /// <param name="txt"></param>
        public void ShowFlashAnnouncement(string txt, HintType typ, int fontSize = 0)
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                if (!_flashAnnouncementShown)
                {
                    Panel.SetZIndex(_mainWin.UiRtbBoardComment, 10);
                    _flashAnnouncementShown = true;
                    _engineLinesVisible = _mainWin.UiTbEngineLines.Visibility == Visibility.Visible;

                    MoveDocument(HostRtb.Document, _docOnHold);

                    Paragraph dummy = CreateParagraphWithText("dummy", "", false);
                    HostRtb.Document.Blocks.Add(dummy);

                    Paragraph para = CreateParagraphWithText("big_red", txt, false);
                    if (fontSize > 0)
                    {
                        para.FontSize = fontSize;
                    }
                    HostRtb.Document.Blocks.Add(para);
                    para.Foreground = ChessForgeColors.GetHintForeground(typ);

                    _mainWin.Timers.Start(AppTimers.TimerId.FLASH_ANNOUNCEMENT);

                    _mainWin.UiRtbBoardComment.Visibility = Visibility.Visible;
                    _mainWin.UiTbEngineLines.Visibility = Visibility.Hidden;
                }
            });
        }

        /// <summary>
        /// Closes the "flash announcement".
        /// </summary>
        public void HideFlashAnnouncement()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                MoveDocument(_docOnHold, HostRtb.Document);
                _mainWin.Timers.Stop(AppTimers.TimerId.FLASH_ANNOUNCEMENT);
                Panel.SetZIndex(_mainWin.UiRtbBoardComment, 0);

                if (_engineLinesVisible)
                {
                    _mainWin.UiRtbBoardComment.Visibility = Visibility.Hidden;
                    _mainWin.UiTbEngineLines.Visibility = Visibility.Visible;
                }
                _flashAnnouncementShown = false;
            });
        }

        /// <summary>
        /// The main message when a new workbook was loaded or when nothing
        /// more relevant is to be shown.
        /// </summary>
        /// <param name="title"></param>
        public void ShowTabHints(TabViewType viewType = TabViewType.NONE)
        {
            HasSpecialMessage = false;

            if (WorkbookManager.SessionWorkbook != null && WorkbookManager.SessionWorkbook.GamesManager.State == ProcessState.RUNNING
                || _mainWin.ActiveLineReplay.IsReplayActive
                || MultiTextBoxManager.CanShowEvaluationChart(false, out _) )
            {
                return;
            }

            _mainWin.Dispatcher.Invoke(() =>
            {
                HostRtb.Document.Blocks.Clear();

                if (_mainWin.SessionWorkbook != null)
                {
                    try
                    {
                        string title = _mainWin.SessionWorkbook.Title;
                        if (string.IsNullOrEmpty(_mainWin.SessionWorkbook.Title))
                        {
                            title = Properties.Resources.UntitledWorkbook;
                        }
                        
                        Paragraph paraTitle =  AddNewParagraphToDoc(HostRtb.Document, "title", title);
                        paraTitle.Name = Constants.WORKBOOK_TITLE_PARAGRAPH_NAME;

                        AddNewParagraphToDoc(HostRtb.Document, "bold_prompt", Properties.Resources.cbActions);
                        string commentText = "";
                        if (viewType == TabViewType.NONE)
                        {
                            viewType = AppState.ActiveTab;
                        }

                        switch (viewType)
                        {
                            case TabViewType.CHAPTERS:
                                commentText = Strings.QuickInstructionForChapters;
                                break;
                            case TabViewType.INTRO:
                                commentText = Strings.QuickInstructionForIntro;
                                break;
                            case TabViewType.STUDY:
                                commentText = Strings.QuickInstructionForStudy;
                                break;
                            case TabViewType.BOOKMARKS:
                                commentText = Strings.QuickInstructionForBookmarks;
                                break;
                            case TabViewType.MODEL_GAME:
                                if (AppState.ActiveChapterGamesCount > 0)
                                {
                                    commentText = Strings.QuickInstructionForGames;
                                }
                                else
                                {
                                    commentText = Strings.QuickInstructionForGamesEmpty;
                                }
                                break;
                            case TabViewType.EXERCISE:
                                if (AppState.ActiveChapterExerciseCount == 0)
                                {
                                    commentText = Strings.QuickInstructionForExercisesEmpty;
                                }
                                else
                                {
                                    switch (AppState.CurrentSolvingMode)
                                    {
                                        case VariationTree.SolvingMode.ANALYSIS:
                                        case VariationTree.SolvingMode.GUESS_MOVE:
                                            commentText = Strings.QuickInstructionForExerciseSolving;
                                            break;
                                        case VariationTree.SolvingMode.EDITING:
                                            commentText = Strings.QuickInstructionForExercises;
                                            break;
                                        default:
                                            commentText = Strings.QuickInstructionForExercisesHiddenSolution;
                                            break;
                                    }
                                }
                                break;
                        }
                        AddNewParagraphToDoc(HostRtb.Document, "normal", commentText);
                    }
                    catch { }
                }
                AppState.SwapCommentBoxForEngineLines(false);
            });
        }

        /// <summary>
        /// Invoked when the game replay stops to revert to showing
        /// the workbook title message.
        /// </summary>
        public void RestoreTitleMessage(GameData.ContentType contentType = GameData.ContentType.NONE)
        {
            if (_mainWin.SessionWorkbook != null)
            {
                TabViewType viewType = GuiUtilities.ContentTypeToTabView(contentType);
                ShowTabHints(viewType);
            }
        }

        /// <summary>
        /// Adds a note about identical position(s) found elsewhere.
        /// </summary>
        /// <param name="nd"></param>
        public void ReportIdenticalPositionFound(TreeNode nd)
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                AddNewParagraphToDoc(HostRtb.Document, "bold_prompt", Resources.cbIdenticalPositionFound).Foreground = Brushes.Green;
                AddNewParagraphToDoc(HostRtb.Document, "normal_14", Resources.cbInvestigateIdenticalPositions).Foreground = Brushes.Green;
            });

            HasSpecialMessage = false;
        }

        /// <summary>
        /// Informs the user about the auto-replay
        /// in progress.
        /// </summary>
        public void GameReplayStart()
        {
            HostRtb.Document.Blocks.Clear();

            AddNewParagraphToDoc(HostRtb.Document, "dummy", "");
            AddNewParagraphToDoc(HostRtb.Document, "bold_18", Resources.cbAutoReplayInProgress);
            AddNewParagraphToDoc(HostRtb.Document, "normal", Resources.cbClickToStop);

            HasSpecialMessage = false;
        }

        /// <summary>
        /// Informs the user that the Training Session has started.
        /// </summary>
        public void TrainingSessionStart()
        {
            HostRtb.Document.Blocks.Clear();
            AddNewParagraphToDoc(HostRtb.Document, "dummy", "");
            AddNewParagraphToDoc(HostRtb.Document, "bold_16", Resources.cbTrainingStarted);
            AddNewParagraphToDoc(HostRtb.Document, "normal_14", Resources.cbMakeMoveAndWatch);

            HasSpecialMessage = false;
        }

        /// <summary>
        /// Informs the user that the Engine Game has started.
        /// </summary>
        public void EngineGameStart()
        {
            HostRtb.Document.Blocks.Clear();
            AddNewParagraphToDoc(HostRtb.Document, "dummy", "");
            AddNewParagraphToDoc(HostRtb.Document, "bold_16", Resources.EngGameStarted);

            HasSpecialMessage = false;
        }

        /// <summary>
        /// Displays the checkmate message.
        /// </summary>
        /// <param name="userMade"></param>
        public void ReportCheckmate(bool userMade)
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                HostRtb.Document.Blocks.Clear();

                Paragraph dummy = CreateParagraphWithText("dummy", "", false);
                HostRtb.Document.Blocks.Add(dummy);

                string txt;
                if (userMade)
                {
                    if (AppState.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
                    {
                        txt = Resources.cbYouCheckmatedEngine;
                    }
                    else if (TrainingSession.IsTrainingInProgress)
                    {
                        txt = Resources.cbYouCheckmatedCoach;
                    }
                    else
                    {
                        txt = Resources.cbCheckMateEnd;
                    }
                }
                else
                {
                    txt = Resources.cbEngineCheckmatedYou;
                }

                Paragraph para = CreateParagraphWithText("end_of_game", txt, false);
                HostRtb.Document.Blocks.Add(para);
                para.Foreground = Brushes.Red;
            });

            HasSpecialMessage = true;
        }

        /// <summary>
        /// Displays the stalemate message.
        /// </summary>
        public void ReportStalemate()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                HostRtb.Document.Blocks.Clear();

                Paragraph dummy = CreateParagraphWithText("dummy", "", false);
                HostRtb.Document.Blocks.Add(dummy);

                string txt = Resources.cbStalemateAndDraw;

                Paragraph para = CreateParagraphWithText("end_of_game", txt, false);
                HostRtb.Document.Blocks.Add(para);
                para.Foreground = Brushes.Red;
            });

            HasSpecialMessage = true;
        }

        /// <summary>
        /// Displays the stalemate message.
        /// </summary>
        public void ReportInsufficientMaterial()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                HostRtb.Document.Blocks.Clear();

                Paragraph dummy = CreateParagraphWithText("dummy", "", false);
                HostRtb.Document.Blocks.Add(dummy);

                string txt = Resources.cbInsufficientMaterialAndDraw;

                Paragraph para = CreateParagraphWithText("end_of_game", txt, false);
                HostRtb.Document.Blocks.Add(para);
                para.Foreground = Brushes.Red;
            });

            HasSpecialMessage = true;
        }

        /// <summary>
        /// Notifies the user that the evaluation was stopped and engine reset
        /// upon error. 
        /// </summary>
        /// <param name="txt"></param>
        public void EngineResetOnError(string txt)
        {
            UserWaitAnnouncement(txt, HintType.ERROR);
        }

        /// <summary>
        /// Displays a special message to the user.
        /// Uses the main window's dispatcher context
        /// so can be invoked from a timer thread.
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="brush"></param>
        public void UserWaitAnnouncement(string txt, HintType typ, List<string> subLines = null)
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                HostRtb.Document.Blocks.Clear();

                Paragraph dummy = CreateParagraphWithText("dummy", "", false);
                HostRtb.Document.Blocks.Add(dummy);

                Paragraph para = CreateParagraphWithText("user_wait", txt, false);
                HostRtb.Document.Blocks.Add(para);
                para.Foreground = ChessForgeColors.GetHintForeground(typ);

                if (subLines != null)
                {
                    foreach (string line in subLines)
                    {
                        Run run = new Run("\n" + line);
                        run.FontSize = Constants.BASE_FIXED_FONT_SIZE - 2;
                        run.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
                        run.FontWeight = FontWeights.Normal;

                        para.Inlines.Add(run);
                    }
                }
            });
        }
    }
}

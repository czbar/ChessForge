using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Timers;
using System.Text;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ChessForge;
using ChessPosition;
using GameTree;
using ChessForge.Properties;

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
        /// <param name="doc"></param>
        /// <param name="mainWin"></param>
        public CommentBox(FlowDocument doc, MainWindow mainWin) : base(doc)
        {
            _mainWin = mainWin;
        }

        override internal Dictionary<string, RichTextPara> RichTextParas { get { return _richTextParas; } }

        /// <summary>
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        internal Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            ["title"] = new RichTextPara(0, 0, 24, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(0, 0, 0)), TextAlignment.Center),
            ["big_red"] = new RichTextPara(0, 10, 20, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(0, 0, 0)), TextAlignment.Center),
            ["user_wait"] = new RichTextPara(0, 10, 20, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(0, 0, 0)), TextAlignment.Center),
            ["bold_prompt"] = new RichTextPara(0, 5, 14, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Center),
            ["eval_results"] = new RichTextPara(30, 5, 14, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(51, 159, 141)), TextAlignment.Center),
            ["normal"] = new RichTextPara(0, 0, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Center),
            ["normal_14"] = new RichTextPara(0, 0, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Center),
            ["default"] = new RichTextPara(10, 5, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(128, 98, 63)), TextAlignment.Center),
            ["dummy"] = new RichTextPara(0, 16, 10, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(0, 0, 0)), TextAlignment.Center),
            ["bold_16"] = new RichTextPara(0, 0, 16, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Center),
            ["bold_18"] = new RichTextPara(0, 5, 18, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Center),
            ["end_of_game"] = new RichTextPara(0, 5, 18, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Center),
        };

        /// <summary>
        /// Displays the last move made by the user.
        /// </summary>
        /// <param name="nd"></param>
        public void GameMoveMade(TreeNode nd, bool userMove)
        {
            Document.Blocks.Clear();
            AddNewParagraphToDoc("dummy", "");

            if (userMove)
            {
                AddNewParagraphToDoc("normal", Strings.GetResource("YourMoveWas") + ":");
                AddNewParagraphToDoc("bold_prompt", MoveUtils.BuildSingleMoveText(nd, true));
                if (AppState.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
                {
                    AddNewParagraphToDoc("normal", Strings.GetResource("WaitForEngineResponse"));
                }
                else if (TrainingSession.IsTrainingInProgress)
                {
                    AddNewParagraphToDoc("normal", Strings.GetResource("WaitForResponse"));
                }
            }
            else // engine or "coach" moved
            {
                if (AppState.CurrentLearningMode == LearningMode.Mode.ENGINE_GAME)
                {
                    AddNewParagraphToDoc("normal", Strings.GetResource("EnginePlayed") + ":");
                }
                else
                {
                    AddNewParagraphToDoc("normal", Strings.GetResource("CoachPlayed") + ":");
                }
                AddNewParagraphToDoc("bold_16", MoveUtils.BuildSingleMoveText(nd, true));
                AddNewParagraphToDoc("normal", Strings.GetResource("YourTurn"));
            }
        }

        /// <summary>
        /// A message about the chess engine being loaded
        /// displayed during the application's startup.
        /// </summary>
        public void StartingEngine()
        {
            UserWaitAnnouncement(Resources.ResourceManager.GetString("ChessEngineLoading"), null);
        }

        /// <summary>
        /// A message to display while the workbook is being read
        /// and processed.
        /// </summary>
        public void ReadingFile()
        {
            UserWaitAnnouncement(Resources.ResourceManager.GetString("ReadingWorkbookFile"), Brushes.Blue);
        }

        /// <summary>
        /// A prompt to the user to open a Workbook.
        /// </summary>
        public void OpenFile()
        {
            UserWaitAnnouncement(Strings.GetResource("cbUseMenuToOpenWorkbook"), Brushes.Blue);
        }

        /// <summary>
        /// Shows a "flash announcement" and starts a timer
        /// to close it later on.
        /// </summary>
        /// <param name="txt"></param>
        public void ShowFlashAnnouncement(string txt)
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                if (!_flashAnnouncementShown)
                {
                    _flashAnnouncementShown = true;
                    _engineLinesVisible = _mainWin.UiTbEngineLines.Visibility == Visibility.Visible;

                    MoveDocument(ref Document, ref _docOnHold);

                    Paragraph dummy = CreateParagraphWithText("dummy", "", false);
                    Document.Blocks.Add(dummy);

                    Paragraph para = CreateParagraphWithText("big_red", txt, false);
                    Document.Blocks.Add(para);
                    para.Foreground = Brushes.Red;

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
                MoveDocument(ref _docOnHold, ref Document);
                _mainWin.Timers.Stop(AppTimers.TimerId.FLASH_ANNOUNCEMENT);

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
        public void ShowTabHints()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                Document.Blocks.Clear();

                if (_mainWin.SessionWorkbook != null)
                {
                    string title = _mainWin.SessionWorkbook.Title;
                    if (string.IsNullOrEmpty(_mainWin.SessionWorkbook.Title))
                    {
                        title = Properties.Resources.UntitledWorkbook;
                    }
                    AddNewParagraphToDoc("title", title);
                    AddNewParagraphToDoc("bold_prompt", Properties.Resources.cbActions);
                    string commentText = "";
                    switch (AppState.ActiveTab)
                    {
                        case WorkbookManager.TabViewType.CHAPTERS:
                            commentText = Strings.QuickInstructionForChapters;
                            break;
                        case WorkbookManager.TabViewType.STUDY:
                            commentText = Strings.QuickInstructionForStudy;
                            break;
                        case WorkbookManager.TabViewType.BOOKMARKS:
                            commentText = Strings.QuickInstructionForBookmarks;
                            break;
                        case WorkbookManager.TabViewType.MODEL_GAME:
                            if (AppState.ActiveChapterGamesCount > 0)
                            {
                                commentText = Strings.QuickInstructionForGames;
                            }
                            else
                            {
                                commentText = Strings.QuickInstructionForGamesEmpty;
                            }
                            break;
                        case WorkbookManager.TabViewType.EXERCISE:
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
                    AddNewParagraphToDoc("normal", commentText);
                }
                AppState.SwapCommentBoxForEngineLines(false);
            });
        }

        /// <summary>
        /// Invoked when the game replay stops to revert to showing
        /// the workbook title message.
        /// </summary>
        public void RestoreTitleMessage()
        {
            if (_mainWin.SessionWorkbook != null)
            {
                ShowTabHints();
            }
        }

        /// <summary>
        /// Informs the user about the auto-replay
        /// in progress.
        /// </summary>
        public void GameReplayStart()
        {
            Document.Blocks.Clear();

            AddNewParagraphToDoc("dummy", "");
            AddNewParagraphToDoc("bold_18", Resources.cbAutoReplayInProgress);
            AddNewParagraphToDoc("normal", Resources.cbClickToStop);
        }

        /// <summary>
        /// Informs the user that the Training Session has started.
        /// </summary>
        public void TrainingSessionStart()
        {
            Document.Blocks.Clear();
            AddNewParagraphToDoc("dummy", "");
            AddNewParagraphToDoc("bold_16", Resources.cbTrainingStarted);
            AddNewParagraphToDoc("normal_14", Resources.cbMakeMoveAndWatch);
        }

        /// <summary>
        /// Displays  a checkmate message.
        /// </summary>
        /// <param name="userMade"></param>
        public void ReportCheckmate(bool userMade)
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                Document.Blocks.Clear();

                Paragraph dummy = CreateParagraphWithText("dummy", "", false);
                Document.Blocks.Add(dummy);

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
                Document.Blocks.Add(para);
                para.Foreground = Brushes.Red;
            });
        }

        /// <summary>
        /// Displays a stalemate message.
        /// </summary>
        public void ReportStalemate()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                Document.Blocks.Clear();

                Paragraph dummy = CreateParagraphWithText("dummy", "", false);
                Document.Blocks.Add(dummy);

                string txt = Resources.cbStalemateAndDraw;

                Paragraph para = CreateParagraphWithText("end_of_game", txt, false);
                Document.Blocks.Add(para);
                para.Foreground = Brushes.Red;
            });
        }

        /// <summary>
        /// Displays a special message to the user.
        /// Uses the main window's dispatcher context
        /// so can be invoked from a timer thread.
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="brush"></param>
        private void UserWaitAnnouncement(string txt, Brush brush)
        {
            if (brush == null)
            {
                brush = Brushes.Green;
            }

            _mainWin.Dispatcher.Invoke(() =>
            {
                Document.Blocks.Clear();

                Paragraph dummy = CreateParagraphWithText("dummy", "", false);
                Document.Blocks.Add(dummy);

                Paragraph para = CreateParagraphWithText("user_wait", txt, false);
                Document.Blocks.Add(para);
                para.Foreground = brush;
            });

            AppState.DoEvents();
        }
    }
}

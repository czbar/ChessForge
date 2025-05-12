using GameTree;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ChessPosition;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace ChessForge
{
    /// <summary>
    /// Specialized subclass of VariationTreeView dealing
    /// with presentation of Exercises.
    /// </summary>
    public class ExerciseTreeView : VariationTreeView
    {
        /// <summary>
        /// Sets the visibility of the controls in the Solving panel
        /// according to the passed Solving Mode.
        /// </summary>
        /// <param name="mode"></param>
        public void SetSolvingMode(VariationTree.SolvingMode mode)
        {
            _mainVariationTree.CurrentSolvingMode = mode;
            if (_mainVariationTree.AssociatedSecondary != null)
            {
                _mainVariationTree.AssociatedSecondary.CurrentSolvingMode = mode;
                if (mode == VariationTree.SolvingMode.ANALYSIS)
                {
                    // calculate available points
                    _mainWin.ActiveArticle.Solver.CalculateAvailableQuizPoints(_mainVariationTree);
                }
            }
            if (mode != VariationTree.SolvingMode.ANALYSIS)
            {
                _mainWin.ActiveArticle.Solver.ResetQuizPoints();
                _mainWin.ActiveArticle.Solver.IsAnalysisSubmitted = false;
            }

            if (mode == VariationTree.SolvingMode.NONE)
            {
                _mainWin.ActiveArticle.Solver.SolvingStarted = false;
                _mainWin.ActiveArticle.Solver.IsAnalysisSubmitted = false;
                _mainWin.ActiveArticle.Solver.IsGuessingFinished = false;
            }

            // Show scoresheet only in editing mode
            if (mode == VariationTree.SolvingMode.EDITING)
            {
                _mainWin.ResizeTabControl(_mainWin.UiTabCtrlManualReview, TabControlSizeMode.SHOW_ACTIVE_LINE);
            }
            else
            {
                _mainWin.ResizeTabControl(_mainWin.UiTabCtrlManualReview, TabControlSizeMode.HIDE_ACTIVE_LINE);
            }

            AppState.EnableNagBar();
            _mainWin.BoardCommentBox.ShowTabHints();
        }

        /// <summary>
        /// If this is an Exercise view in the solving mode and there are no moves yet,
        /// prompt the user to start entering them.
        /// </summary>
        /// <returns></returns>
        override public Paragraph BuildYourMovePrompt()
        {
            if (_mainWin.ActiveArticle != null && _mainWin.ActiveArticle.Solver != null && _mainWin.ActiveArticle.Solver.SolvingStarted)
            {
                return null;
            }

            Paragraph para = null;

            if ((_mainVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.ANALYSIS
                || _mainVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.GUESS_MOVE)
                && ShownVariationTree.Nodes.Count == 1)
            {
                para = CreateParagraph("1", true);

                Run r = new Run();
                r.Foreground = ChessForgeColors.GetHintForeground(CommentBox.HintType.INFO);
                if (_mainVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.ANALYSIS)
                {
                    r.Text = Properties.Resources.AnalysisByMoves;
                }
                else
                {
                    r.Text = Properties.Resources.GuessMoves;
                }
                r.FontStyle = FontStyles.Italic;

                para.Inlines.Add(r);
            }
            return para;
        }

        /// <summary>
        /// If this is an Exercise view in the ANALYSIS solving mode 
        /// and there are quiz points to be awarded,
        /// advise the user.
        /// </summary>
        /// <returns></returns>
        override public Paragraph BuildQuizInfoPara()
        {
            if (_mainVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.ANALYSIS)
            {
                int mainQuizPoints = _mainWin.ActiveArticle.Solver.MainQuizPoints;
                int sideLineQuizPoints = _mainWin.ActiveArticle.Solver.SideLineQuizPoints;

                if (_mainVariationTree.CurrentSolvingMode != VariationTree.SolvingMode.ANALYSIS
                    || (mainQuizPoints == 0 && sideLineQuizPoints == 0))
                {
                    return null;
                }

                Paragraph para = null;

                para = CreateParagraph("1", true);

                Run r = new Run();
                r.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
                r.Text = Properties.Resources.QuizPointsAvailable + ": " + (mainQuizPoints + sideLineQuizPoints).ToString() + ". ";

                if (sideLineQuizPoints != 0)
                {
                    r.Text += Properties.Resources.QuizPointsSidelines + ": " + sideLineQuizPoints.ToString();
                }
                r.TextDecorations = TextDecorations.Underline;
                para.Inlines.Add(r);

                if (_mainWin.ActiveArticle.Solver.IsAnalysisSubmitted)
                {
                    Run rScore = new Run();
                    rScore.Text = "\n" + Properties.Resources.QuizScore + ": " + _mainWin.ActiveArticle.Solver.PointsScored.ToString();
                    rScore.FontWeight = FontWeights.Bold;
                    para.Inlines.Add(rScore);
                }


                return para;
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Sets up visibility of the controls
        /// in the Solving Panel
        /// </summary>
        /// <param name="mode"></param>
        public void SetupGuiForSolvingMode(VariationTree.SolvingMode mode)
        {

            if (mode == VariationTree.SolvingMode.GUESS_MOVE || mode == VariationTree.SolvingMode.ANALYSIS)
            {
                _lblExit.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;

                Article article = WorkbookManager.SessionWorkbook.ActiveArticle;
                if (article.Solver.IsSolvingFinished
                    ||
                    mode == VariationTree.SolvingMode.ANALYSIS && _mainWin.ActiveArticle.Solver.IsAnalysisSubmitted)
                {
                    _gbSolvingPanel.Header = Properties.Resources.SolvingCompleted;
                }
                else
                {
                    _gbSolvingPanel.Header = Properties.Resources.SolvingInProgress;
                }

                _btnGuessMove.Visibility = Visibility.Collapsed;
                _btnAnalysis.Visibility = Visibility.Collapsed;
                _lblGuessMove.Visibility = Visibility.Collapsed;
                _lblAnalysis.Visibility = Visibility.Collapsed;

                if (mode == VariationTree.SolvingMode.ANALYSIS
                    &&
                    !article.Solver.IsAnalysisSubmitted)
                {
                    _btnSubmitAnalysis.Visibility = Visibility.Visible;
                    _lblSubmitAnalysis.Visibility = Visibility.Visible;
                }
                else
                {
                    _btnSubmitAnalysis.Visibility = Visibility.Collapsed;
                    _lblSubmitAnalysis.Visibility = Visibility.Collapsed;
                }
                _btnExit.Visibility = Visibility.Visible;
                _lblExit.Visibility = Visibility.Visible;
            }
            else
            {
                _gbSolvingPanel.Header = Properties.Resources.SelectSolvingMode;
                _btnGuessMove.Visibility = Visibility.Visible;
                _btnAnalysis.Visibility = Visibility.Visible;
                _lblGuessMove.Visibility = Visibility.Visible;
                _lblAnalysis.Visibility = Visibility.Visible;

                _btnSubmitAnalysis.Visibility = Visibility.Collapsed;
                _btnExit.Visibility = Visibility.Collapsed;
                _lblSubmitAnalysis.Visibility = Visibility.Collapsed;
                _lblExit.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Sets up text for the Show/Hide Run button
        /// </summary>
        /// <param name="mode"></param>
        private void UpdateShowEditButton(VariationTree.SolvingMode mode)
        {
            if (mode == VariationTree.SolvingMode.GUESS_MOVE || mode == VariationTree.SolvingMode.ANALYSIS)
            {
                _runShowEdit.Text = "";

            }
            else
            {
                if (_mainVariationTree.ShowTreeLines)
                {
                    _runShowEdit.Text = "    " + Constants.CharCollapse.ToString() + " " + Properties.Resources.HideSolution;
                }
                else
                {
                    _runShowEdit.Text = Constants.CharExpand.ToString() + " " + Properties.Resources.ShowSolution;
                }
            }
        }


        //*******************************************
        // Solving Panel control references
        //*******************************************
        private GroupBox _gbSolvingPanel;
        private Button _btnGuessMove;
        private Button _btnAnalysis;

        private Button _btnSubmitAnalysis;
        private Button _btnExit;

        private Label _lblGuessMove;
        private Label _lblAnalysis;

        private Label _lblSubmitAnalysis;
        private Label _lblExit;

        private Run _runShowEdit;

        //******************************************


        // flag indictating a possible attempt by the user to drag on the "dummy" board
        private bool _dummyBoardLeftClicked = false;
        private bool _dummyBoardInDrag = false;

        /// <summary>
        /// Calls the base class constructor.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="mainWin"></param>
        /// <param name="contentType"></param>
        public ExerciseTreeView(RichTextBox rtb, GameData.ContentType contentType, bool isPrinting = false)
            : base(rtb, contentType)
        {
            _isPrinting = isPrinting;
        }

        /// <summary>
        /// Whether move/node selection is allowed in the current mode.
        /// </summary>
        /// <returns></returns>
        override protected bool IsSelectionEnabled()
        {
            if (_mainVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.GUESS_MOVE)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Whether the lines are currently shown.
        /// </summary>
        public bool AreLinesShown
        {
            get => (_mainVariationTree != null && _mainVariationTree.ShowTreeLines);
        }

        /// <summary>
        /// Whether any text to display exists.
        /// Note if there are no solution moves but there is a comment for the move 0, 
        /// then there is something to display.
        /// </summary>
        public bool IsMainVariationTreeEmpty
        {
            get => (_mainVariationTree == null 
                || _mainVariationTree.Nodes.Count == 0 
                || _mainVariationTree.Nodes.Count == 1 && string.IsNullOrWhiteSpace(_mainVariationTree.Nodes[0].Comment));
        }

        /// <summary>
        /// Whether the exercise has any solution moves (i.e. moves beyond the move 0).
        /// </summary>
        public bool ExerciseHasSolution
        {
            get => _mainVariationTree != null && _mainVariationTree.Nodes.Count > 1;
        }

        /// <summary>
        /// Appends this paragraph to the document to advise the user that the
        /// solving is over.
        /// </summary>
        override public Paragraph BuildGuessingFinishedParagraph()
        {
            if (_mainWin.ActiveArticle != null && _mainWin.ActiveArticle.Solver != null && _mainWin.ActiveArticle.Solver.IsGuessingFinished)
            {
                Paragraph para = CreateParagraph("1", true);

                Run r = new Run(Properties.Resources.ExVwCompletedGuessing);
                r.Foreground = Brushes.DarkGreen;
                para.Inlines.Add(r);

                return para;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Build Exercise specific Paragraphs 
        /// and adds them to the Document.
        /// </summary>
        override protected void BuildExerciseParagraphs(FlowDocument doc)
        {
            Paragraph boardPara = BuildExercisesChessboardParagraph();
            if (boardPara != null)
            {
                doc.Blocks.Add(boardPara);
            }

            Paragraph buttonShowHide = BuildExerciseBoardControls();
            if (buttonShowHide != null)
            {
                doc.Blocks.Add(buttonShowHide);
            }
        }

        /// <summary>
        /// Builds the chessboard image
        /// and the Solving Mode buttons for the Exercises view.
        /// </summary>
        /// <returns></returns>
        private Paragraph BuildExercisesChessboardParagraph()
        {
            if (_mainVariationTree != null && _mainVariationTree.Header.GetContentType(out _) == GameData.ContentType.EXERCISE)
            {
                Paragraph para = CreateParagraph("2", false);
                para.Name = RichTextBoxUtilities.DiagramParaPrefix + _mainVariationTree.Nodes[0].NodeId;
                para.Margin = new Thickness(20, 0, 0, 20);


                Canvas canvas = SetupExerciseCanvas();
                Image imgChessBoard = CreatePassiveChessBoard(canvas);

                canvas.Children.Add(imgChessBoard);
                PopulateSolvingPanel(canvas);

                Viewbox viewBox = SetupExerciseViewbox(canvas);

                InlineUIContainer uIContainer = new InlineUIContainer();
                uIContainer.Child = viewBox;
                para.Inlines.Add(uIContainer);

                return para;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a Viewbox for the chessboard
        /// </summary>
        /// <param name="canvas"></param>
        /// <returns></returns>
        private Viewbox SetupExerciseViewbox(Canvas canvas)
        {
            Viewbox viewBox = new Viewbox();
            viewBox.Child = canvas;
            viewBox.Width = 250;
            viewBox.Height = 250;
            viewBox.Visibility = Visibility.Visible;

            return viewBox;
        }

        /// <summary>
        /// Creates a Canvas for the chessboard. 
        /// </summary>
        /// <returns></returns>
        private Canvas SetupExerciseCanvas()
        {
            Canvas canvas = new Canvas();
            canvas.Background = Brushes.Black;
            canvas.Width = 250;
            canvas.Height = 250;

            return canvas;
        }

        /// <summary>
        /// Creats the "passive" chessboard control.
        /// </summary>
        /// <param name="canvas"></param>
        /// <returns></returns>
        private Image CreatePassiveChessBoard(Canvas canvas)
        {
            Image imgChessBoard = new Image();
            imgChessBoard.Margin = new Thickness(5, 5, 5, 5);
            imgChessBoard.Source = ChessBoards.ChessBoardGreySmall;

            _exercisePassiveChessBoard = new ChessBoardSmall(canvas, imgChessBoard, null, null, false, false);
            _exercisePassiveChessBoard.DisplayPosition(_mainVariationTree.Nodes[0], false);
            AlignExerciseAndMainBoards();

            canvas.MouseLeftButtonDown += EventDummyBoardMouseDown;
            canvas.MouseLeftButtonUp += EventDummyBoardMouseUp;
            canvas.MouseMove += EventDummyBoardMouseMove;
            canvas.MouseLeave += EventDummyBoardMouseLeave;

            return imgChessBoard;
        }

        /// <summary>
        /// Creates the side-to-move label and the Show/Hide button.
        /// </summary>
        /// <returns></returns>
        private Paragraph BuildExerciseBoardControls()
        {
            if (_mainVariationTree != null && _mainVariationTree.Header.GetContentType(out _) == GameData.ContentType.EXERCISE)
            {
                Paragraph para = CreateParagraph("2", false);
                para.Name = RichTextBoxUtilities.ExerciseUnderBoardControls;
                para.Margin = new Thickness(90, 0, 0, 20);

                PieceColor color = WorkbookManager.SessionWorkbook.ActiveChapter.GetSideToSolveExercise();

                para.Inlines.Add(BuildSideToMoveRun(color));
                para.Inlines.Add(BuildShowEditButtonRun());

                return para;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates the Side-to-Move label.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private Run BuildSideToMoveRun(PieceColor color)
        {
            Run rSideToMove = new Run();
            if (color == PieceColor.Black)
            {
                rSideToMove.Text = "   " + Properties.Resources.BlackToMove + "\n";
            }
            else
            {
                rSideToMove.Text = "   " + Properties.Resources.WhiteToMove + "\n";
            }
            rSideToMove.FontWeight = FontWeights.Bold;

            return rSideToMove;
        }

        /// <summary>
        /// Creates the Show/Edit button.
        /// </summary>
        /// <returns></returns>
        private Run BuildShowEditButtonRun()
        {
            _runShowEdit = new Run();

            UpdateShowEditButton(_mainVariationTree.CurrentSolvingMode);
            _runShowEdit.FontWeight = FontWeights.Normal;
            _runShowEdit.FontSize = 12;

            _runShowEdit.PreviewMouseDown += EventShowHideButtonClicked;
            _runShowEdit.PreviewMouseUp += EventShowHideButtonMouseUp;
            _runShowEdit.Cursor = Cursors.Hand;

            return _runShowEdit;
        }

        /// <summary>
        /// Inserts controls into the Solving Modes panel
        /// </summary>
        /// <param name="canvas"></param>
        private void PopulateSolvingPanel(Canvas canvas)
        {
            int leftMargin = 275;
            int topMargin = 50;

            if (_mainVariationTree != null && _mainVariationTree.Header.GetContentType(out _) == GameData.ContentType.EXERCISE)
            {
                AddSolvingPanelGroupBox(canvas, leftMargin, topMargin);

                // add label first as AddSolvingButton is relying on it being non-null
                AddSolvingButtonLabel(canvas, VariationTree.SolvingMode.GUESS_MOVE, 57 + leftMargin, 37 + topMargin);
                AddSolvingButton(canvas, VariationTree.SolvingMode.GUESS_MOVE, 15 + leftMargin, 32 + topMargin);

                AddSubmitAnalysisButtonLabel(canvas, 57 + leftMargin, 37 + topMargin);
                AddSubmitAnalysisButton(canvas, 15 + leftMargin, 32 + topMargin);

                AddSolvingButtonLabel(canvas, VariationTree.SolvingMode.ANALYSIS, 57 + leftMargin, 79 + topMargin);
                AddSolvingButton(canvas, VariationTree.SolvingMode.ANALYSIS, 15 + leftMargin, 77 + topMargin);

                if (_mainVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.GUESS_MOVE
                    || _mainVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.ANALYSIS && _mainWin.ActiveArticle.Solver.IsAnalysisSubmitted)
                {
                    AddExitButtonLabel(canvas, 57 + leftMargin, 37 + topMargin);
                    AddExitButton(canvas, 15 + leftMargin, 32 + topMargin);
                }
                else
                {
                    AddExitButtonLabel(canvas, 57 + leftMargin, 79 + topMargin);
                    AddExitButton(canvas, 15 + leftMargin, 77 + topMargin);
                }

                SetupGuiForSolvingMode(_mainVariationTree.CurrentSolvingMode);
            }
        }

        /// <summary>
        /// Creates a button for the requested Solving Mode.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="mode"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        private Button AddSolvingButton(Canvas canvas, VariationTree.SolvingMode mode, double left, double top)
        {
            Button btn = BuildSolvingModeButton(mode);
            canvas.Children.Add(btn);
            Canvas.SetLeft(btn, left);
            Canvas.SetTop(btn, top);

            // set event handlers
            switch (mode)
            {
                case VariationTree.SolvingMode.GUESS_MOVE:
                    btn.PreviewMouseDown += UiBtnGuessMoveDown_Click;
                    btn.PreviewMouseUp += UiBtnGuessMoveUp_Click;
                    // click on the label has the same effect
                    _lblGuessMove.PreviewMouseDown += UiBtnGuessMoveDown_Click;
                    _lblGuessMove.PreviewMouseUp += UiBtnGuessMoveUp_Click;
                    _btnGuessMove = btn;
                    break;
                case VariationTree.SolvingMode.ANALYSIS:
                    btn.PreviewMouseDown += UiBtnAnalysisDown_Click;
                    btn.PreviewMouseUp += UiBtnAnalysisUp_Click;
                    // click on the label has the same effect
                    _lblAnalysis.PreviewMouseDown += UiBtnAnalysisDown_Click;
                    _lblAnalysis.PreviewMouseUp += UiBtnAnalysisUp_Click;
                    _btnAnalysis = btn;
                    break;
            }
            return btn;
        }

        /// <summary>
        /// Adds the Exit button to the panel.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <returns></returns>
        private Button AddExitButton(Canvas canvas, double left, double top)
        {
            Button btn = BuildSolvingModeButton(VariationTree.SolvingMode.NONE, ImageSources.SolvingExit);
            canvas.Children.Add(btn);
            Canvas.SetLeft(btn, left);
            Canvas.SetTop(btn, top);

            btn.PreviewMouseDown += UiBtnExitDown_Click;
            btn.PreviewMouseUp += UiBtnExitUp_Click;

            _lblExit.PreviewMouseDown += UiBtnExitDown_Click;
            _lblExit.PreviewMouseUp += UiBtnExitUp_Click;

            _btnExit = btn;

            return btn;
        }

        /// <summary>
        /// Adds the submit analysis button to the panel.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <returns></returns>
        private Button AddSubmitAnalysisButton(Canvas canvas, double left, double top)
        {
            Button btn = BuildSolvingModeButton(VariationTree.SolvingMode.NONE, ImageSources.SolvingComplete);
            canvas.Children.Add(btn);
            Canvas.SetLeft(btn, left);
            Canvas.SetTop(btn, top);

            btn.PreviewMouseDown += UiBtnSubmitDown_Click;
            _btnSubmitAnalysis = btn;

            _lblSubmitAnalysis.PreviewMouseDown += UiBtnSubmitDown_Click;

            return btn;
        }

        /// <summary>
        /// Creates a GroupBox frame for the Solving Modes panel.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        private void AddSolvingPanelGroupBox(Canvas canvas, double left, double top)
        {
            _gbSolvingPanel = new GroupBox();
            _gbSolvingPanel.Margin = new Thickness(left, top, 0, 0);
            if (_mainVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.GUESS_MOVE
                ||
                _mainVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.ANALYSIS && _mainWin.ActiveArticle.Solver.IsAnalysisSubmitted)
            {
                _gbSolvingPanel.Height = 80;
            }
            else
            {
                _gbSolvingPanel.Height = 130;
            }
            _gbSolvingPanel.Width = 265;
            _gbSolvingPanel.FontSize = 12; // not configurable!
            _gbSolvingPanel.BorderBrush = Brushes.Black;
            _gbSolvingPanel.Header = "";
            _gbSolvingPanel.FontWeight = FontWeights.Bold;
            canvas.Children.Add(_gbSolvingPanel);
        }

        /// <summary>
        /// Creates a label for a solving mode button
        /// and adds it to the canvas.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="mode"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        private void AddSolvingButtonLabel(Canvas canvas, VariationTree.SolvingMode mode, double left, double top)
        {
            Label lbl = new Label();
            lbl.FontSize = 12; // not configurable!
            lbl.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
            switch (mode)
            {
                case VariationTree.SolvingMode.GUESS_MOVE:
                    lbl.Content = Properties.Resources.GuessNextMove;
                    _lblGuessMove = lbl;
                    break;
                case VariationTree.SolvingMode.ANALYSIS:
                    lbl.Content = Properties.Resources.EnterAnalysis;
                    _lblAnalysis = lbl;
                    break;
                default:
                    lbl.Content = "";
                    break;
            }
            canvas.Children.Add(lbl);
            Canvas.SetLeft(lbl, left);
            Canvas.SetTop(lbl, top);
        }

        /// <summary>
        /// Adds the Exit Button's label to the panel
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        private void AddExitButtonLabel(Canvas canvas, double left, double top)
        {
            Label lbl = new Label();
            lbl.FontSize = 12; // not configurable!
            lbl.Content = Properties.Resources.Exit;

            _lblExit = lbl;

            canvas.Children.Add(lbl);
            Canvas.SetLeft(lbl, left);
            Canvas.SetTop(lbl, top);
        }

        /// <summary>
        /// Adds the Submit Analysis Button's label to the panel
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        private void AddSubmitAnalysisButtonLabel(Canvas canvas, double left, double top)
        {
            Label lbl = new Label();
            lbl.FontSize = 12; // not configurable!
            if (_mainVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.ANALYSIS)
            {
                lbl.Content = Properties.Resources.SubmitAnalysis;
            }
            else
            {
                lbl.Content = Properties.Resources.Done;
            }

            _lblSubmitAnalysis = lbl;

            canvas.Children.Add(lbl);
            Canvas.SetLeft(lbl, left);
            Canvas.SetTop(lbl, top);
        }

        /// <summary>
        /// Creats a solving mode button
        /// and adds it to the canvas.
        /// </summary>
        /// <param name="solvingMode"></param>
        /// <returns></returns>
        private Button BuildSolvingModeButton(VariationTree.SolvingMode solvingMode, BitmapImage img = null)
        {
            BitmapImage imgForButton;
            switch (solvingMode)
            {
                case VariationTree.SolvingMode.ANALYSIS:
                    imgForButton = ImageSources.SolveAnalysis;
                    break;
                case VariationTree.SolvingMode.GUESS_MOVE:
                    imgForButton = ImageSources.SolveGuess;
                    break;
                default:
                    imgForButton = img;
                    break;
            }

            Button btnGuessMove = new Button()
            {
                Width = 32,
                Height = 32,
                Content = new Image
                {
                    Source = imgForButton,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            return btnGuessMove;
        }

        //*******************************************************************
        //
        // SOLVING MODE EVENTS
        //
        //*******************************************************************

        /// <summary>
        /// Activates the Solving Tree which is an Associated Tree 
        /// in the Active Exercise Tree. 
        /// </summary>
        /// <param name="mode"></param>
        private void ActivateSolvingMode(VariationTree.SolvingMode mode)
        {
            if (!ExerciseHasSolution)
            {
                MessageBox.Show(Properties.Resources.ExerciseNoSolution, Properties.Resources.Exercise, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                try
                {
                    if (_mainVariationTree.AssociatedSecondary == null)
                    {
                        _mainVariationTree.AssociatedSecondary = new VariationTree(GameData.ContentType.EXERCISE, _mainVariationTree.RootNode.CloneMe(true));
                        _mainVariationTree.AssociatedSecondary.CurrentSolvingMode = mode;
                        _mainVariationTree.AssociatedSecondary.Header = _mainVariationTree.Header.CloneMe(false);
                        _mainVariationTree.AssociatedSecondary.AssociatedPrimary = _mainVariationTree;
                    }

                    _mainVariationTree.IsAssociatedTreeActive = true;
                    ShownVariationTree.ShowTreeLines = true;

                    SetSolvingMode(mode);
                    _mainWin.EngineToggleOn_OnPreviewMouseLeftButtonDown(null, null);
                    _mainWin.TurnExplorersOff(false);

                    string lineId = ShownVariationTree.SelectedLineId;
                    if (string.IsNullOrEmpty(lineId))
                    {
                        lineId = "1";
                    }

                    int nodeId = ShownVariationTree.SelectedNodeId;
                    if (nodeId < 0)
                    {
                        nodeId = 0;
                    }
                    HighlightLineAndMove(HostRtb.Document, lineId, nodeId);

                    ObservableCollection<TreeNode> lineToSelect = ShownVariationTree.GetNodesForLine(lineId);
                    _mainWin.SetActiveLine(lineToSelect, nodeId);

                    SetupGuiForSolvingMode(mode);
                    BuildFlowDocumentForVariationTree(false);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Leaves the Solving Mode and returns
        /// to main tree viewing editing
        /// </summary>
        public void DeactivateSolvingMode(VariationTree.SolvingMode mode)
        {
            try
            {
                SetSolvingMode(mode);

                _mainVariationTree.IsAssociatedTreeActive = false;
                _mainVariationTree.AssociatedSecondary = null;
                _mainVariationTree.ShowTreeLines = (mode == VariationTree.SolvingMode.EDITING);

                _mainVariationTree.SelectedLineId = "1";
                string lineId = "1";

                _mainVariationTree.SetSelectedNodeId(0);
                int nodeId = 0;
                HighlightLineAndMove(HostRtb.Document, lineId, nodeId);

                ObservableCollection<TreeNode> lineToSelect = _mainVariationTree.GetNodesForLine(lineId);
                _mainWin.SetActiveLine(lineToSelect, nodeId);

                SetupGuiForSolvingMode(mode);
                BuildFlowDocumentForVariationTree(false);

                _mainWin.UpdateExplorersToggleState();
                _mainWin.BoardCommentBox.ShowTabHints();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Mouse Down event on the Guess Move button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnGuessMoveDown_Click(object sender, RoutedEventArgs e)
        {
            ActivateSolvingMode(VariationTree.SolvingMode.GUESS_MOVE);
        }

        /// <summary>
        /// Mouse Up event on the Guess Move button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnGuessMoveUp_Click(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Mouse Down event on the Analysis button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnAnalysisDown_Click(object sender, RoutedEventArgs e)
        {
            ActivateSolvingMode(VariationTree.SolvingMode.ANALYSIS);
        }

        /// <summary>
        /// Mouse Up event on the Analysis button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnAnalysisUp_Click(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Mouse Down event on the Exit button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnExitDown_Click(object sender, RoutedEventArgs e)
        {
            DeactivateSolvingMode(VariationTree.SolvingMode.NONE);
        }

        /// <summary>
        /// Mouse Up event on the Exit button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnExitUp_Click(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// The user submitted their solution. Check the score and save.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnSubmitDown_Click(object sender, RoutedEventArgs e)
        {
            // for all nodes in the main tree that have quiz points assigned, look for an identical node in the solution
            // and update the comment if found
            int pointsScored = 0;
            FindMoveInSolution(_mainVariationTree.RootNode, ref pointsScored);
            MarkWrongSolutionMove(_mainVariationTree.AssociatedSecondary.RootNode, _mainVariationTree.AssociatedSecondary.RootNode.ColorToMove);
            _mainWin.ActiveArticle.Solver.IsAnalysisSubmitted = true;
            _mainWin.ActiveArticle.Solver.PointsScored = pointsScored;

            BuildFlowDocumentForVariationTree(false);
        }

        /// <summary>
        /// Puts crosses next to all moves made by the solving side that have
        /// not been found in the solution unless they were in response
        /// to a move that wasn't found in the solution (as marken by IsProcessed).
        /// </summary>
        /// <param name="node"></param>
        private void MarkWrongSolutionMove(TreeNode node, PieceColor color)
        {
            if (!node.IsProcessed)
            {
                if (node.ColorToMove != color) // solving side's move
                {
                    if (node.Parent == null || node.Parent.IsProcessed)
                    {
                        if (string.IsNullOrEmpty(node.Comment))
                        {
                            node.Comment = Constants.CharCrossMark.ToString();
                        }
                        else
                        {
                            node.Comment = Constants.CharCrossMark.ToString() + " " + node.Comment;
                        }
                    }
                }
                else // response side
                {
                    // this is not response so see if a predecessor was already marked
                    bool alreadyMarked = false;
                    TreeNode pred = node;
                    while (pred.Parent != null)
                    {
                        if (pred.Comment != null && pred.Comment.Contains(Properties.Resources.cbResponseNotCovered))
                        {
                            alreadyMarked = true;
                            break;
                        }
                        pred = pred.Parent;
                    }
                    if (!alreadyMarked)
                    {
                        node.Comment = Properties.Resources.cbResponseNotCovered;
                    }
                }
            }

            foreach (TreeNode child in node.Children)
            {
                MarkWrongSolutionMove(child, color);
            }
        }

        /// <summary>
        /// Recursively find the nodes in the solution that correspond to the scoring nodes
        /// in the exercise.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="pointsScored"></param>
        private void FindMoveInSolution(TreeNode node, ref int pointsScored)
        {
            TreeNode found = _mainVariationTree.AssociatedSecondary.FindIdenticalNode(node, false);
            if (found != null)
            {
                found.IsProcessed = true;

                bool isFirstChild = found.IsFirstChild();
                int quizPoints = node.QuizPoints;
                if (found.ColorToMove != _mainVariationTree.AssociatedSecondary.RootNode.ColorToMove)
                {
                    if (isFirstChild)
                    {
                        found.Comment = Constants.CharCheckMark.ToString();
                        pointsScored += quizPoints;
                    }
                    else
                    {
                        if (found.Parent != null && found.Parent.Parent != null && found.Parent.Parent.Comment != null
                            && found.Parent.Parent.Comment.Contains(Properties.Resources.cbThisWasSolution))
                        {
                            found.Comment = "";
                        }
                        else
                        {
                            found.Comment = Properties.Resources.cbThisWasSolution;
                        }
                    }
                }

                if (quizPoints != 0)
                {
                    if (!string.IsNullOrEmpty(found.Comment))
                    {
                        found.Comment += " ";
                    }
                    if (!isFirstChild)
                    {
                        found.Comment += "(" + Properties.Resources.LostQuizPoints + ": ";
                    }
                    else
                    {
                        found.Comment += Properties.Resources.QuizPoints + ": ";
                    }
                    found.Comment += quizPoints.ToString();
                    if (!isFirstChild)
                    {
                        found.Comment += ")";
                    }
                }
            }

            foreach (TreeNode child in node.Children)
            {
                FindMoveInSolution(child, ref pointsScored);
            }
        }

        //****************************************************************
        //
        // MOUSE EVENTS ON THE DUMMY BOARD
        //
        //****************************************************************

        /// <summary>
        /// Registers the dummy board having a mouse down event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void EventDummyBoardMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                _dummyBoardLeftClicked = false;
                _mainWin.UiMnExerc_EditPosition_Click(null, null);
            }
            else
            {
                _dummyBoardLeftClicked = true;
                _dummyBoardInDrag = false;
            }
            e.Handled = true;
        }

        /// <summary>
        /// Registers the dummy board having a mouse move event
        /// following a mouse left button down.
        /// This may indicate an attempt by the user to drag a piece on the
        /// dummy board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void EventDummyBoardMouseMove(object sender, RoutedEventArgs e)
        {
            if (_dummyBoardLeftClicked)
            {
                _dummyBoardInDrag = true;
            }
            else
            {
                _dummyBoardInDrag = false;
            }
            e.Handled = true;
        }

        /// <summary>
        /// Clears dummy board mouse move events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void EventDummyBoardMouseLeave(object sender, RoutedEventArgs e)
        {
            _dummyBoardInDrag = false;
            _dummyBoardLeftClicked = false;
        }

        /// <summary>
        /// Looks like a user attempted to make a move on the dummy board.
        /// Give them some info.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void EventDummyBoardMouseUp(object sender, RoutedEventArgs e)
        {
            if (_dummyBoardInDrag)
            {
                AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.cbJustPicture, CommentBox.HintType.ERROR);
                _dummyBoardInDrag = false;
            }
            _dummyBoardLeftClicked = false;
            e.Handled = true;
        }

    }
}

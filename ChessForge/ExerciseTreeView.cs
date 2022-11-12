using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ChessPosition;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
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
                _gbSolvingPanel.Header = "Solving in Progress ...";
                _btnGuessMove.Visibility = Visibility.Collapsed;
                _btnAnalysis.Visibility = Visibility.Collapsed;
                _lblGuessMove.Visibility = Visibility.Collapsed;
                _lblAnalysis.Visibility = Visibility.Collapsed;

                _btnSubmitAnalysis.Visibility = Visibility.Visible;
                _btnCancel.Visibility = Visibility.Visible;
                _lblSubmitAnalysis.Visibility = Visibility.Visible;
                _lblCancel.Visibility = Visibility.Visible;
            }
            else
            {
                _gbSolvingPanel.Header = "Select Solving Mode";
                _btnGuessMove.Visibility = Visibility.Visible;
                _btnAnalysis.Visibility = Visibility.Visible;
                _lblGuessMove.Visibility = Visibility.Visible;
                _lblAnalysis.Visibility = Visibility.Visible;

                _btnSubmitAnalysis.Visibility = Visibility.Collapsed;
                _btnCancel.Visibility = Visibility.Collapsed;
                _lblSubmitAnalysis.Visibility = Visibility.Collapsed;
                _lblCancel.Visibility = Visibility.Collapsed;
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
                    _runShowEdit.Text = "    " + Constants.CharCollapse.ToString() + " " + "Hide Solution";
                }
                else
                {
                    _runShowEdit.Text = Constants.CharExpand.ToString() + " " + "Show/Edit Solution";
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
        private Button _btnCancel;

        private Label _lblGuessMove;
        private Label _lblAnalysis;

        private Label _lblSubmitAnalysis;
        private Label _lblCancel;

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
        /// <param name="entityIndex"></param>
        public ExerciseTreeView(FlowDocument doc, MainWindow mainWin, GameData.ContentType contentType, int entityIndex)
            : base(doc, mainWin, contentType, entityIndex)
        {
        }

        /// <summary>
        /// Build Exercise specific Paragraphs 
        /// and adds them to the Document.
        /// </summary>
        override protected void BuildExerciseParagraphs()
        {
            Paragraph boardPara = BuildExercisesChessboardParagraph();
            if (boardPara != null)
            {
                Document.Blocks.Add(boardPara);
            }

            Paragraph buttonShowHide = BuildExerciseBoardControls();
            if (buttonShowHide != null)
            {
                Document.Blocks.Add(buttonShowHide);
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

            canvas.MouseLeftButtonDown += EventDummyBoardMouseDown;
            canvas.MouseLeftButtonUp += EventDummyBoardMouseUp;
            canvas.MouseMove += EventDummyBoardMouseMove;

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

            _exercisePassiveChessBoard = new ChessBoardSmall(canvas, imgChessBoard, null, false, false);
            _exercisePassiveChessBoard.DisplayPosition(_mainVariationTree.Nodes[0]);
            AlignExerciseAndMainBoards();

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
                rSideToMove.Text = "   Black to move\n";
            }
            else
            {
                rSideToMove.Text = "   White to move\n";
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
            int leftMargin = 280;
            int topMargin = 56;

            if (_mainVariationTree != null && _mainVariationTree.Header.GetContentType(out _) == GameData.ContentType.EXERCISE)
            {
                AddSolvingPanelGroupBox(canvas, leftMargin, topMargin);

                AddSolvingButton(canvas, VariationTree.SolvingMode.GUESS_MOVE, 15 + leftMargin, 32 + topMargin);
                AddSolvingButtonLabel(canvas, VariationTree.SolvingMode.GUESS_MOVE, 57 + leftMargin, 37 + topMargin);

                AddSubmitAnalysisButton(canvas, 15 + leftMargin, 32 + topMargin);
                AddSubmitAnalysisButtonLabel(canvas, 57 + leftMargin, 37 + topMargin);

                AddSolvingButton(canvas, VariationTree.SolvingMode.ANALYSIS, 15 + leftMargin, 77 + topMargin);
                AddSolvingButtonLabel(canvas, VariationTree.SolvingMode.ANALYSIS, 57 + leftMargin, 79 + topMargin);

                AddCancelButton(canvas, 15 + leftMargin, 77 + topMargin);
                AddCancelButtonLabel(canvas, 57 + leftMargin, 79 + topMargin);

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
                    _btnGuessMove = btn;
                    break;
                case VariationTree.SolvingMode.ANALYSIS:
                    btn.PreviewMouseDown += UiBtnAnalysisDown_Click;
                    btn.PreviewMouseUp += UiBtnAnalysisUp_Click;
                    _btnAnalysis = btn;
                    break;
            }
            return btn;
        }

        /// <summary>
        /// Adds the Cancel button to the panel.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <returns></returns>
        private Button AddCancelButton(Canvas canvas, double left, double top)
        {
            Button btn = BuildSolvingModeButton(VariationTree.SolvingMode.NONE,
                         new BitmapImage(new Uri("pack://application:,,,/Resources/Images/cancel.png", UriKind.RelativeOrAbsolute)));
            canvas.Children.Add(btn);
            Canvas.SetLeft(btn, left);
            Canvas.SetTop(btn, top);

            //btn.PreviewMouseDown += UiBtnGuessMoveDown_Click;
            //btn.PreviewMouseUp += UiBtnGuessMoveUp_Click;
            _btnCancel = btn;

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
            Button btn = BuildSolvingModeButton(VariationTree.SolvingMode.NONE,
                         new BitmapImage(new Uri("pack://application:,,,/Resources/Images/solve_complete.png", UriKind.RelativeOrAbsolute)));
            canvas.Children.Add(btn);
            Canvas.SetLeft(btn, left);
            Canvas.SetTop(btn, top);

            btn.PreviewMouseDown += UiBtnGuessMoveDown_Click;
            btn.PreviewMouseUp += UiBtnGuessMoveUp_Click;
            _btnSubmitAnalysis = btn;

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
            _gbSolvingPanel.Width = 265;
            _gbSolvingPanel.Height = 130;
            _gbSolvingPanel.FontSize = 12; // not configurable!
            _gbSolvingPanel.BorderBrush = Brushes.Black;
            _gbSolvingPanel.Header = "";
            _gbSolvingPanel.FontWeight = FontWeights.Bold;
            canvas.Children.Add(_gbSolvingPanel);
        }

        /// <summary>
        /// Creats a label for a solving mode button
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
            switch (mode)
            {
                case VariationTree.SolvingMode.GUESS_MOVE:
                    lbl.Content = "Guess the next move";
                    _lblGuessMove = lbl;
                    break;
                case VariationTree.SolvingMode.ANALYSIS:
                    lbl.Content = "Enter your analysis and submit";
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
        /// Adds the Cancel Button's label to the panel
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        private void AddCancelButtonLabel(Canvas canvas, double left, double top)
        {
            Label lbl = new Label();
            lbl.FontSize = 12; // not configurable!
            lbl.Content = "Cancel";

            _lblCancel = lbl;

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
                lbl.Content = "Submit Your Analysis";
            }
            else
            {
                lbl.Content = "Done";
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
                    imgForButton = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/solve.png", UriKind.RelativeOrAbsolute));
                    break;
                case VariationTree.SolvingMode.GUESS_MOVE:
                    imgForButton = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/guess_move.png", UriKind.RelativeOrAbsolute));
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
            try
            {
                SetSolvingMode(mode);

                if (_mainVariationTree.AssociatedSecondary == null)
                {
                    _mainVariationTree.AssociatedSecondary = new VariationTree(GameData.ContentType.EXERCISE, _mainVariationTree.RootNode.CloneMe(true));
                    _mainVariationTree.AssociatedSecondary.Header = _mainVariationTree.Header.CloneMe();
                    _mainVariationTree.AssociatedSecondary.AssociatedPrimary = _mainVariationTree;
                }

                _mainVariationTree.IsAssociatedTreeActive = true;
                _shownVariationTree.ShowTreeLines = true;

                string lineId = _shownVariationTree.SelectedLineId;
                if (string.IsNullOrEmpty(lineId))
                {
                    lineId = "1";
                }

                int nodeId = _shownVariationTree.SelectedNodeId;
                if (nodeId < 0)
                {
                    nodeId = 0;
                }
                SelectLineAndMove(lineId, nodeId);

                ObservableCollection<TreeNode> lineToSelect = _shownVariationTree.SelectLine(lineId);
                _mainWin.SetActiveLine(lineToSelect, nodeId);

                SetupGuiForSolvingMode(mode);
                BuildFlowDocumentForVariationTree();
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
        protected void EventDummyBoardMouseDown(object sender, RoutedEventArgs e)
        {
            _dummyBoardLeftClicked = true;
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
        /// Looks like a user attempted to make a move on the dummy board.
        /// Give them some info.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void EventDummyBoardMouseUp(object sender, RoutedEventArgs e)
        {
            if (_dummyBoardInDrag)
            {
                AppStateManager.MainWin.BoardCommentBox.ShowFlashAnnouncement("This is just a picture! Make your moves on the big board.");
                _dummyBoardInDrag = false;
            }
            _dummyBoardLeftClicked = false;
            e.Handled = true;
        }

    }
}

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

namespace ChessForge
{
    /// <summary>
    /// Specialized subclass of VariationTreeView dealing
    /// with presentation of Exercises.
    /// </summary>
    public class ExerciseTreeView : VariationTreeView
    {
        /// <summary>
        /// Available Exercise Solving modes.
        /// </summary>
        public enum SolvingMode
        {
            NONE,
            EDITING,
            GUESS_MOVE,
            FULL_SOLUTION
        }

        /// <summary>
        /// The solving mode that the view is currently in.
        /// </summary>
        public SolvingMode CurrentSolvingMode
        { get; set; }

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
            CurrentSolvingMode = SolvingMode.NONE;
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
            Run rShowEdit = new Run();
            rShowEdit.Text = Constants.CharExpand.ToString() + " " + "Show/Edit Solution";
            rShowEdit.FontWeight = FontWeights.Normal;
            rShowEdit.FontSize = 12;

            rShowEdit.PreviewMouseDown += EventShowHideButtonClicked;
            rShowEdit.PreviewMouseUp += EventShowHideButtonMouseUp;
            rShowEdit.Cursor = Cursors.Hand;

            return rShowEdit;
        }

        /// <summary>
        /// Inserts controls into the Solving Modes panel
        /// </summary>
        /// <param name="canvas"></param>
        private void PopulateSolvingPanel(Canvas canvas)
        {
            int leftMargin = 280;
            int topMargin = 60;

            if (_mainVariationTree != null && _mainVariationTree.Header.GetContentType(out _) == GameData.ContentType.EXERCISE)
            {
                AddSolvingPanelGroupBox(canvas, leftMargin, topMargin);

                AddSolvingButton(canvas, SolvingMode.GUESS_MOVE, 15 + leftMargin, 30 + topMargin);
                AddSolvingButtonLabel(canvas, SolvingMode.GUESS_MOVE, 57 + leftMargin, 35 + topMargin);

                AddSolvingButton(canvas, SolvingMode.FULL_SOLUTION, 15 + leftMargin, 75 + topMargin);
                AddSolvingButtonLabel(canvas, SolvingMode.FULL_SOLUTION, 57 + leftMargin, 77 + topMargin);
            }
        }

        /// <summary>
        /// Creates a button for the requested Solving Mode.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="mode"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        private Button AddSolvingButton(Canvas canvas, SolvingMode mode, double left, double top)
        {
            Button btnGuess = BuildSolvingModeButton(mode);
            canvas.Children.Add(btnGuess);
            Canvas.SetLeft(btnGuess, left);
            Canvas.SetTop(btnGuess, top);

            // set event handlers
            switch (mode)
            {
                case SolvingMode.GUESS_MOVE:
                    btnGuess.PreviewMouseDown += UiBtnGuessMoveDown_Click;
                    btnGuess.PreviewMouseUp += UiBtnGuessMoveUp_Click;
                    break;
                case SolvingMode.FULL_SOLUTION:
                    btnGuess.PreviewMouseDown += UiBtnFullSolutionDown_Click;
                    btnGuess.PreviewMouseUp += UiBtnFullSolutionUp_Click;
                    break;
            }
            return btnGuess;
        }

        /// <summary>
        /// Creates a GroupBox frame for the Solving Modes panel.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        private void AddSolvingPanelGroupBox(Canvas canvas, double left, double top)
        {
            GroupBox gb = new GroupBox();
            gb.Margin = new Thickness(left, top, 0, 0);
            gb.Width = 265;
            gb.Height = 125;
            gb.FontSize = 12; // not configurable!
            gb.BorderBrush = Brushes.Black;
            gb.Header = "Select Solving Mode";
            gb.FontWeight = FontWeights.Bold;
            canvas.Children.Add(gb);
        }

        /// <summary>
        /// Creats a label for a solving mode button
        /// and adds it to the canvas.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="mode"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        private void AddSolvingButtonLabel(Canvas canvas, SolvingMode mode, double left, double top)
        {
            Label lblGuessMove = new Label();
            lblGuessMove.FontSize = 12; // not configurable!
            switch (mode)
            {
                case SolvingMode.GUESS_MOVE:
                    lblGuessMove.Content = "Guess the next move";
                    break;
                case SolvingMode.FULL_SOLUTION:
                    lblGuessMove.Content = "Enter the full solution and submit";
                    break;
                default:
                    lblGuessMove.Content = "";
                    break;
            }
            canvas.Children.Add(lblGuessMove);
            Canvas.SetLeft(lblGuessMove, left);
            Canvas.SetTop(lblGuessMove, top);
        }

        /// <summary>
        /// Creats a solving mode button
        /// and adds it to the canvas.
        /// </summary>
        /// <param name="solvingMode"></param>
        /// <returns></returns>
        private Button BuildSolvingModeButton(SolvingMode solvingMode)
        {
            BitmapImage imgForButton;
            switch (solvingMode)
            {
                case SolvingMode.FULL_SOLUTION:
                    imgForButton = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/solve.png", UriKind.RelativeOrAbsolute));
                    break;
                case SolvingMode.GUESS_MOVE:
                    imgForButton = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/guess_move.png", UriKind.RelativeOrAbsolute));
                    break;
                default:
                    imgForButton = null;
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
        private void ActivateSolvingMode(SolvingMode mode)
        {
            try
            {
                if (_mainVariationTree.AssociatedTree == null)
                {
                    _mainVariationTree.AssociatedTree = new VariationTree(GameData.ContentType.SOLVING, _mainVariationTree.RootNode.CloneMe(true));
                }

                _mainVariationTree.IsAssociatedTreeActive = true;
                
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
            ActivateSolvingMode(SolvingMode.GUESS_MOVE);
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
        /// Mouse Down event on the Full Solution button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnFullSolutionDown_Click(object sender, RoutedEventArgs e)
        {
            ActivateSolvingMode(SolvingMode.FULL_SOLUTION);
        }

        /// <summary>
        /// Mouse Up event on the Full Solution button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnFullSolutionUp_Click(object sender, RoutedEventArgs e)
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

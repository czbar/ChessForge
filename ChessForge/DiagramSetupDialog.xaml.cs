using ChessPosition;
using GameTree;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for DiagramSetupDialog.xaml
    /// </summary>
    public partial class DiagramSetupDialog : Window
    {
        // color of the side to move
        private PieceColor _sideToMove = PieceColor.White;

        /// <summary>
        /// Object representing the piece being dragged.
        /// </summary>
        private PositionSetupDraggedPiece _draggedPiece = new PositionSetupDraggedPiece();

        /// Holds the current position
        public BoardPosition PositionSetup = new BoardPosition();

        // string to show in the GUI indicating that White is on move
        private string WHITE_TO_MOVE = Properties.Resources.WhiteToMove;

        // string to show in the GUI indicating that Black is on move
        private string BLACK_TO_MOVE = Properties.Resources.BlackToMove;

        // The underlying ChessBoard object
        private ChessBoardSmall _chessboard;

        // The object handling mouse events related to piece drag and drop.
        PositionBoardDragEvents _dragEvents;

        /// <summary>
        /// Constructs the dialog.
        /// Sets up offset values.
        /// </summary>
        public DiagramSetupDialog(TreeNode node)
        {
            InitializeComponent();

            ShowCoordinates();

            if (node != null)
            {
                InitializePosition(node);
                SetSideToMove(node.ColorToMove);
            }

            PositionSetup.HalfMove50Clock = 1;
            SetFen();

            _chessboard = new ChessBoardSmall(UiCnvBoard, UiImgChessBoard, null, null, false, false);
            _chessboard.DisplayPosition(node, true);

            _dragEvents = new PositionBoardDragEvents(_chessboard, _draggedPiece, UiCnvSetup, ref PositionSetup);
        }

        /// <summary>
        /// Sets up the initial position if a non-null TreeNode
        /// was passed.
        /// </summary>
        /// <param name="nd"></param>
        private void InitializePosition(TreeNode nd)
        {
            PositionSetup = new BoardPosition(nd.Position);
        }

        /// <summary>
        /// Generates FEN from the positions and populates the FEN text box with the result.
        /// </summary>
        private void SetFen(bool checkEnpassant = true)
        {
            UiTbFen.Text = FenParser.GenerateFenFromPosition(PositionSetup);
        }

        /// <summary>
        /// Displays board's square coordinates
        /// </summary>
        private void ShowCoordinates()
        {
            for (int x = 0; x <= 7; x++)
            {
                Label lbl = CreateCoordinateLabel();
                lbl.Content = (char)('a' + x);

                UiCnvBoard.Children.Add(lbl);
                Canvas.SetLeft(lbl, 19 + (x * 30));
                Canvas.SetBottom(lbl, -1);
            }

            for (int y = 0; y <= 7; y++)
            {
                Label lbl = CreateCoordinateLabel();
                lbl.Content = (char)('1' + (7 - y));

                UiCnvBoard.Children.Add(lbl);
                Canvas.SetLeft(lbl, -2);
                Canvas.SetTop(lbl, 18 + (y * 30));
            }
        }

        /// <summary>
        /// Creates a Label object for a coordinate.
        /// </summary>
        /// <returns></returns>
        private Label CreateCoordinateLabel()
        {
            Label lbl = new Label();
            lbl.Foreground = Brushes.White;
            lbl.Width = 20;
            lbl.Height = 21;
            lbl.FontSize = 8;
            lbl.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
            lbl.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;

            return lbl;
        }

        /// <summary>
        /// If mouse was released while over the board
        /// during the drag process, drop the piece
        /// on the square after under the mouse.
        /// If there was a piece on the square already,
        /// replace it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _dragEvents.MouseUp(sender, e);

            SetFen();
            e.Handled = true;
        }

        /// <summary>
        /// A mouse click ocurred within the Setup Canvas.
        /// It was in the chessboard as otherwise, off-the-board
        /// pieces would have picked up the event
        /// Start the drag process for the piece on the clicked
        /// square, if any.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCnvSetup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragEvents.MouseDown(sender, e);
            e.Handled = true;
        }

        /// <summary>
        /// Mouse Move event is of interest during the dragging process.
        /// Paints the image at the new location.
        /// Cancel the dragging process and removes the piece from the board
        /// if we strayed off the SetupCanvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            _dragEvents.MouseMove(sender, e);
        }

        /// <summary>
        /// We moved mouse outside the SetupCanvas area.
        /// We stop moving and remove the dragged piece.
        /// This is same as in Window_MoveMouse which should
        /// be called as well so this is just in case.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            _dragEvents.MouseLeave(sender, e);
        }

        /// <summary>
        /// Clears the _boardPosition.Board object and removes
        /// all images from the board.
        /// </summary>
        private void ClearAll()
        {
            PositionUtils.ClearPosition(ref PositionSetup.Board);
            _chessboard.ClearBoard();
            //foreach (Image img in _pieceImagesOnBoard)
            //{
            //    if (img != null)
            //    {
            //        UiCnvSetup.Children.Remove(img);
            //        img.Source = null;
            //    }
            //}
        }

        //************************************************************
        //
        //  CONTROLS THAT AFFECT THE POSITION (FEN)
        //
        //************************************************************

        /// <summary>
        /// Responds to the text change in the FEN text box.
        /// If the FEN is invalid, ignores the change by catching the exception and exiting.
        /// If the FEN is valid, copies aside the current position and updates the PositionSetup 
        /// object per the FEN string.
        /// Checks what the differences are between the old and the new postions
        /// and issues relevant update requests to the GUI.
        /// We want to prevent mutual calls when the user changes a setting which then updates FEN which
        /// then invokes this method, which without these checks would attempt to set the changed control again.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbFen_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                bool isDiff = DiffPositionSetupWithFenText(UiTbFen.Text, out bool position, out bool colorToMove, out bool castling, out bool enpassant);
                if (isDiff)
                {
                    if (position)
                    {
                        _chessboard.DisplayPosition(null, PositionSetup);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Identifies differences betwee 2 BoardPositions.
        /// </summary>
        /// <param name="fen"></param>
        /// <param name="position"></param>
        /// <param name="colorToMove"></param>
        /// <param name="castling"></param>
        /// <param name="enpassant"></param>
        /// <returns></returns>
        private bool DiffPositionSetupWithFenText(string fen, out bool position, out bool colorToMove, out bool castling, out bool enpassant)
        {
            BoardPosition temp = new BoardPosition();
            FenParser.ParseFenIntoBoard(UiTbFen.Text, ref temp);

            bool isDiff = PositionChanges(temp, PositionSetup, out position, out colorToMove, out castling, out enpassant);
            if (isDiff)
            {
                PositionSetup = temp;
            }

            return isDiff;
        }

        /// <summary>
        /// Checks the differences between 2 positions except
        /// for move and half-move numbers.
        /// </summary>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        /// <param name="position"></param>
        /// <param name="colorToMove"></param>
        /// <param name="castling"></param>
        /// <param name="enpassant"></param>
        /// <returns></returns>
        private bool PositionChanges(BoardPosition pos1, BoardPosition pos2, out bool position, out bool colorToMove, out bool castling, out bool enpassant)
        {
            position = true;
            colorToMove = true;
            castling = true;
            enpassant = true;

            string fen1 = FenParser.GenerateFenFromPosition(pos1);
            string[] tokens1 = fen1.Split(' ');

            string fen2 = FenParser.GenerateFenFromPosition(pos2);
            string[] tokens2 = fen2.Split(' ');

            if (tokens1.Length < 4 || tokens2.Length < 4)
            {
                return true;
            }

            if (tokens1[0] == tokens2[0])
            {
                position = false;
            }

            return position;
        }

        /// <summary>
        /// Swaps the side to move.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLblSideToMove_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SwapSideToMove();
        }

        /// <summary>
        /// Swaps the side to move.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgSwapSides_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SwapSideToMove();
        }

        /// <summary>
        /// Swaps the color of the side on move.
        /// </summary>
        private void SwapSideToMove()
        {
            if (_sideToMove == PieceColor.Black)
            {
                SetSideToMove(PieceColor.White);
            }
            else
            {
                SetSideToMove(PieceColor.Black);
            }
        }

        /// <summary>
        /// Sets the color of the side on move.
        /// This may change the FEN and enpassant squares.
        /// </summary>
        /// <param name="color"></param>
        private void SetSideToMove(PieceColor color)
        {
            if (color == PieceColor.Black)
            {
                UiLblSideToMove.Content = BLACK_TO_MOVE;
                _sideToMove = PieceColor.Black;
            }
            else
            {
                UiLblSideToMove.Content = WHITE_TO_MOVE;
                _sideToMove = PieceColor.White;
            }
            PositionSetup.ColorToMove = _sideToMove;

            SetFen();
        }

        /// <summary>
        /// Sets up the starting position in the GUI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnStartingPos_Click(object sender, RoutedEventArgs e)
        {
            ClearAll();

            PositionSetup = PositionUtils.SetupStartingPosition();
            _chessboard.DisplayPosition(null, PositionSetup);

            SetFen();
        }

        /// <summary>
        /// Clears the psotion in the GUI.
        /// All pieces are removed from the board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearAll();

            SetFen();
        }


        //************************************************************
        //
        //  MAIN BUTTONS
        //
        //************************************************************

        /// <summary>
        /// Exits the dialog on user pressing the OK button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// Exists the dialog on user pressing the Cancel button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }


        //************************************************************
        //
        //  OFF THE BOARD IMAGE CLICKED
        //
        //************************************************************

        /// <summary>
        /// Off-the-board White King image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgWhiteKing_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragEvents.StartDrag(PieceType.King, PieceColor.White, e);
        }

        /// <summary>
        /// Off-the-board White Queen image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgWhiteQueen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragEvents.StartDrag(PieceType.Queen, PieceColor.White, e);
        }

        /// <summary>
        /// Off-the-board White Rook image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgWhiteRook_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragEvents.StartDrag(PieceType.Rook, PieceColor.White, e);
        }

        /// <summary>
        /// Off-the-board White Bishop image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgWhiteBishop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragEvents.StartDrag(PieceType.Bishop, PieceColor.White, e);
        }

        /// <summary>
        /// Off-the-board White Knight image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgWhiteKnight_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragEvents.StartDrag(PieceType.Knight, PieceColor.White, e);
        }

        /// <summary>
        /// Off-the-board White Pawn image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgWhitePawn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragEvents.StartDrag(PieceType.Pawn, PieceColor.White, e);
        }

        /// <summary>
        /// Off-the-board Black King image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgBlackKing_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragEvents.StartDrag(PieceType.King, PieceColor.Black, e);
        }

        /// <summary>
        /// Off-the-board Black Queen image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgBlackQueen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragEvents.StartDrag(PieceType.Queen, PieceColor.Black, e);
        }

        /// <summary>
        /// Off-the-board Black Rook image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgBlackRook_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragEvents.StartDrag(PieceType.Rook, PieceColor.Black, e);
        }

        /// <summary>
        /// Off-the-board Black Bishop image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgBlackBishop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragEvents.StartDrag(PieceType.Bishop, PieceColor.Black, e);
        }

        /// <summary>
        /// Off-the-board Black Knight image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgBlackKnight_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragEvents.StartDrag(PieceType.Knight, PieceColor.Black, e);
        }

        /// <summary>
        /// Off-the-board Black Pawn image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgBlackPawn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragEvents.StartDrag(PieceType.Pawn, PieceColor.Black, e);
        }

    }
}

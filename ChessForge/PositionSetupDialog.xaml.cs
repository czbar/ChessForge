using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for PositionSetupDialog.xaml
    /// </summary>
    public partial class PositionSetupDialog : Window
    {
        /// <summary>
        /// Whether exit occurred on user's pushing the OK button  
        /// </summary>
        public bool ExitOK = false;

        private int _squareSize = 30;

        private double boardCanvasLeftOffset;
        private double boardCanvasTopOffset;

        private double boardImageLeftOffset;
        private double boardImageTopOffset;

        private PositionSetupDraggedPiece _draggedPiece = new PositionSetupDraggedPiece();

        private ChessBoardSmall _chessBoard;

        public PositionSetupDialog()
        {
            InitializeComponent();

            _chessBoard = new ChessBoardSmall(UiCnvBoard, UiImgChessBoard, null, false, true);
            boardCanvasLeftOffset = Canvas.GetLeft(UiCnvBoard);
            boardCanvasTopOffset = Canvas.GetTop(UiCnvBoard);

            boardImageLeftOffset = Canvas.GetLeft(UiImgChessBoard);
            boardImageTopOffset = Canvas.GetTop(UiImgChessBoard);
        }

        /// <summary>
        /// Sets up the drag operation.
        /// </summary>
        /// <param name="piece"></param>
        /// <param name="color"></param>
        /// <param name="e"></param>
        private void StartDrag(PieceType piece, PieceColor color, MouseButtonEventArgs e)
        {
            _draggedPiece.ImageControl = new Image();
            _draggedPiece.ImageControl.Source = Pieces.GetImageForPieceSmall(piece, color);
            UiCnvSetup.Children.Add(_draggedPiece.ImageControl);

            Point mousePoint = e.GetPosition(UiCnvSetup);
            Canvas.SetZIndex(_draggedPiece.ImageControl, Constants.ZIndex_PieceInAnimation);
            Canvas.SetLeft(_draggedPiece.ImageControl, mousePoint.X - _squareSize / 2);
            Canvas.SetTop(_draggedPiece.ImageControl, mousePoint.Y - _squareSize / 2);

            _draggedPiece.Type = piece;
            _draggedPiece.Color = color;
            _draggedPiece.IsDragInProgress = true;
        }


        /// <summary>
        /// Off-the-board White King image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgWhiteKing_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartDrag(PieceType.King, PieceColor.White, e);
        }


        /// <summary>
        /// Off-the-board White Queen image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgWhiteQueen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartDrag(PieceType.Queen, PieceColor.White, e);
        }

        /// <summary>
        /// Off-the-board White Rook image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgWhiteRook_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartDrag(PieceType.Rook, PieceColor.White, e);
        }

        /// <summary>
        /// Off-the-board White Bishop image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgWhiteBishop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartDrag(PieceType.Bishop, PieceColor.White, e);
        }

        /// <summary>
        /// Off-the-board White Knight image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgWhiteKnight_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartDrag(PieceType.Knight, PieceColor.White, e);
        }

        /// <summary>
        /// Off-the-board White Pawn image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgWhitePawn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartDrag(PieceType.Pawn, PieceColor.White, e);
        }

        /// <summary>
        /// Off-the-board Black King image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgBlackKing_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartDrag(PieceType.King, PieceColor.Black, e);
        }

        /// <summary>
        /// Off-the-board Black Queen image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgBlackQueen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartDrag(PieceType.Queen, PieceColor.Black, e);
        }

        /// <summary>
        /// Off-the-board Black Rook image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgBlackRook_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartDrag(PieceType.Rook, PieceColor.Black, e);
        }

        /// <summary>
        /// Off-the-board Black Bishop image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgBlackBishop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartDrag(PieceType.Bishop, PieceColor.Black, e);
        }

        /// <summary>
        /// Off-the-board Black Knight image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgBlackKnight_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartDrag(PieceType.Knight, PieceColor.Black, e);
        }

        /// <summary>
        /// Off-the-board Black Pawn image was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgBlackPawn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartDrag(PieceType.Pawn, PieceColor.Black, e);
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
        private void UiImgBoard_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedPiece.IsDragInProgress)
            {
            }

            _draggedPiece.IsDragInProgress = false;
            e.Handled = true;
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _draggedPiece.IsDragInProgress = false;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePoint = e.GetPosition(UiCnvSetup);
            SquareCoords sq = MainChessBoardUtils.ClickedSquare(mousePoint);

            if (_draggedPiece.IsDragInProgress)
            {
                if (mousePoint.X < 0 || mousePoint.Y < 0
                    || mousePoint.X > UiCnvSetup.Width
                    || mousePoint.Y > UiCnvSetup.Height)
                {
                    UiCnvSetup.Children.Remove(_draggedPiece.ImageControl);
                    _draggedPiece.IsDragInProgress = false;
                }
                else
                {
                    Canvas.SetZIndex(_draggedPiece.ImageControl, Constants.ZIndex_PieceInAnimation);
                    //mousePoint.X += UiImgChessBoard.Margin.Left;
                    //mousePoint.Y += UiImgChessBoard.Margin.Top;

                    Canvas.SetLeft(_draggedPiece.ImageControl, mousePoint.X - _squareSize / 2);
                    Canvas.SetTop(_draggedPiece.ImageControl, mousePoint.Y - _squareSize / 2);
                }
            }
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_draggedPiece.IsDragInProgress)
            {
                UiCnvSetup.Children.Remove(_draggedPiece.ImageControl);
                _draggedPiece.IsDragInProgress = false;
            }
        }




        private void UiLblSideToMove_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void UiImgSwapSides_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void UiBtnStartingPos_Click(object sender, RoutedEventArgs e)
        {

        }

        private void UiBtnClear_Click(object sender, RoutedEventArgs e)
        {

        }

        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            ExitOK = true;
            Close();
        }

        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ExitOK = false;
            Close();
        }

        private void UiImgBoard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePoint = e.GetPosition(UiCnvSetup);
        }

    }
}

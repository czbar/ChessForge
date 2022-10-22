using ChessPosition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        private double _boardCanvasToSetupCanvasLeftOffset;
        private double _boardCanvasToSetupCanvasTopOffset;

        private double _boardImageToBoardCanvasLeftOffset;
        private double _boardImageToBoardCanvasTopOffset;

        private double _boardLeftOffset;
        private double _boardTopOffset;

        private PositionSetupDraggedPiece _draggedPiece = new PositionSetupDraggedPiece();

        private ChessBoardSmall _chessBoard;

        private BoardPosition _boardPosition = new BoardPosition();

        private Image[,] _pieceImagesOnBoard = new Image[8, 8];

        /// <summary>
        /// Constructs the dialog.
        /// Sets up offset values.
        /// </summary>
        public PositionSetupDialog()
        {
            InitializeComponent();

            _chessBoard = new ChessBoardSmall(UiCnvBoard, UiImgChessBoard, null, false, true);
            _boardCanvasToSetupCanvasLeftOffset = Canvas.GetLeft(UiCnvBoard);
            _boardCanvasToSetupCanvasTopOffset = Canvas.GetTop(UiCnvBoard);

            _boardImageToBoardCanvasLeftOffset = Canvas.GetLeft(UiImgChessBoard);
            _boardImageToBoardCanvasTopOffset = Canvas.GetTop(UiImgChessBoard);
            _boardLeftOffset = _boardCanvasToSetupCanvasLeftOffset + _boardImageToBoardCanvasLeftOffset;
            _boardTopOffset = _boardCanvasToSetupCanvasTopOffset + _boardImageToBoardCanvasTopOffset;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedPiece.IsDragInProgress)
            {
                Point mousePoint; ;

                if (_draggedPiece.OriginSquare == null)
                {
                    mousePoint = e.GetPosition(UiCnvSetup);
                }
                else
                {
                    mousePoint = e.GetPosition(UiCnvBoard);
                }

                if (mousePoint.X < 0 || mousePoint.Y < 0
                    || mousePoint.X > UiCnvSetup.Width
                    || mousePoint.Y > UiCnvSetup.Height)
                {
                    // TODO: if the piece was from the board, delete it
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

            _draggedPiece.Piece = piece;
            _draggedPiece.Color = color;
            _draggedPiece.IsDragInProgress = true;
        }

        /// <summary>
        /// Starts the dragging operation with a piece from the board.
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="e"></param>
        private void StartDrag(SquareCoords sc, MouseButtonEventArgs e)
        {
            if (sc != null && sc.IsValid())
            {
                byte square = _boardPosition.Board[sc.Xcoord, sc.Ycoord];

                _draggedPiece.Piece = PositionUtils.GetPieceType(square);
                _draggedPiece.Color = PositionUtils.GetPieceColor(square);

                _draggedPiece.ImageControl = new Image();
                _draggedPiece.ImageControl.Source = Pieces.GetImageForPieceSmall(_draggedPiece.Piece, _draggedPiece.Color);
                UiCnvSetup.Children.Add(_draggedPiece.ImageControl);

                RemovePieceFromBoard(sc);

                Point mousePoint = e.GetPosition(UiCnvSetup);
                Canvas.SetZIndex(_draggedPiece.ImageControl, Constants.ZIndex_PieceInAnimation);
                Canvas.SetLeft(_draggedPiece.ImageControl, mousePoint.X - _squareSize / 2);
                Canvas.SetTop(_draggedPiece.ImageControl, mousePoint.Y - _squareSize / 2);


                _draggedPiece.IsDragInProgress = true;
            }
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
        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedPiece.IsDragInProgress)
            {
                _draggedPiece.IsDragInProgress = false;
                SquareCoords sc = GetSquareCoordsFromSetupCanvasPoint(e.GetPosition(UiCnvSetup));
                if (sc != null)
                {
                    AddPieceToBoard(sc, _draggedPiece.Piece, _draggedPiece.Color, _draggedPiece.ImageControl);
                    _draggedPiece.ImageControl = new Image();
                }
                else
                {
                    RemovePieceFromBoard(sc);
                }
                _draggedPiece.Clear();
            }
            e.Handled = true;
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

        private void UiCnvSetup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePoint = e.GetPosition(UiCnvSetup);

            SquareCoords sc = GetSquareCoordsFromSetupCanvasPoint(mousePoint);
            StartDrag(sc, e);
            e.Handled = true;
        }

        /// <summary>
        /// Returns square coordinates of the clicked square or null
        /// if the click occured outside the board.
        /// The passed Point must be offset from the Setup Canvas.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        SquareCoords GetSquareCoordsFromSetupCanvasPoint(Point pt)
        {
            pt.X -= _boardLeftOffset;
            pt.Y -= _boardTopOffset;

            if (pt.X < 0 || pt.Y < 0)
            {
                return null;
            }

            int xPos = (int)pt.X / _squareSize;
            int yPos = 7 - (int)pt.Y / _squareSize;

            SquareCoords sc = new SquareCoords(xPos, yPos);
            if (sc.IsValid())
            {
                return sc;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Adds piece tp the board.
        /// Updates the _pieceImagesOnBoard array,
        /// and the BoardPosition.Board array.
        /// Removes any piece that may have been on the square before.
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="piece"></param>
        /// <param name="color"></param>
        /// <param name="img"></param>
        private void AddPieceToBoard(SquareCoords sc, PieceType piece, PieceColor color, Image img)
        {
            RemovePieceFromBoard(sc);
            PositionUtils.PlacePieceOnBoard(piece, color, (byte)sc.Xcoord, (byte)sc.Ycoord, ref _boardPosition.Board);
            _pieceImagesOnBoard[sc.Xcoord, sc.Ycoord] = img;
            PlacePieceOnSquare(sc, _pieceImagesOnBoard[sc.Xcoord, sc.Ycoord]);
        }

        /// <summary>
        /// If there is a piece on the indicated square, it will be removed
        /// in both _pieceImagesOnBoard and BoardPosition.Board arrays.
        /// </summary>
        /// <param name="sc"></param>
        private void RemovePieceFromBoard(SquareCoords sc)
        {
            if (sc != null && sc.IsValid())
            {
                PositionUtils.ClearSquare((byte)sc.Xcoord, (byte)sc.Ycoord, ref _boardPosition.Board);

                if (_pieceImagesOnBoard[sc.Xcoord, sc.Ycoord] != null)
                {
                    UiCnvSetup.Children.Remove(_pieceImagesOnBoard[sc.Xcoord, sc.Ycoord]);
                    _pieceImagesOnBoard[sc.Xcoord, sc.Ycoord].Source = null;
                    _pieceImagesOnBoard[sc.Xcoord, sc.Ycoord] = null;
                }
            }
            else
            {
                UiCnvSetup.Children.Remove(_draggedPiece.ImageControl);
                _draggedPiece.ImageControl.Source = null;
                _draggedPiece.ImageControl = new Image();
            }
        }


        private void PlacePieceOnSquare(SquareCoords sc, Image piece)
        {
            Point pt = GetSquareTopLeft(sc);
            Canvas.SetLeft(piece, pt.X);
            Canvas.SetTop(piece, pt.Y);
        }

        private Point GetSquareTopLeft(SquareCoords sc)
        {
            Point pt = new Point();

            pt.X = sc.Xcoord * 30 + _boardLeftOffset;
            pt.Y = (7 - sc.Ycoord) * 30 + _boardTopOffset;

            return pt;
        }

    }
}

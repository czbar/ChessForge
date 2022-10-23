using ChessPosition;
using GameTree;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

        // square side's size
        private int _squareSize = 30;


        // left offset between the SetupCanvas and BoardCanvas
        private double _boardCanvasToSetupCanvasLeftOffset;

        // top offset between the SetupCanvas and BoardCanvas
        private double _boardCanvasToSetupCanvasTopOffset;


        // left offset between the the Board Image and BoardCanvas
        private double _boardImageToBoardCanvasLeftOffset;
        // top offset between the the Board Image and BoardCanvas
        
        private double _boardImageToBoardCanvasTopOffset;


        // left offset between the the Board Image and Setup Canvas
        private double _boardLeftOffset;
        
        // top offset between the the Board Image and Setup Canvas
        private double _boardTopOffset;


        /// <summary>
        /// Object representing the piece being dragged.
        /// </summary>
        private PositionSetupDraggedPiece _draggedPiece = new PositionSetupDraggedPiece();

        /// Holds the current position
        public BoardPosition PositionSetup = new BoardPosition();

        // Holds piece images currently showing on the board
        private Image[,] _pieceImagesOnBoard = new Image[8, 8];

        // string to show in the GUI indicating that White is on move
        private const string WHITE_TO_MOVE = "White to move";

        // string to show in the GUI indicating that Black is on move
        private const string BLACK_TO_MOVE = "Black to move";

        // color of the side to move
        private PieceColor _sideToMove = PieceColor.White;

        /// <summary>
        /// Constructs the dialog.
        /// Sets up offset values.
        /// </summary>
        public PositionSetupDialog()
        {
            InitializeComponent();

            UiLblSideToMove.Content = WHITE_TO_MOVE;
            SetSideToMove(PieceColor.White);

            ShowDebugButton();

            _boardCanvasToSetupCanvasLeftOffset = Canvas.GetLeft(UiCnvBoard);
            _boardCanvasToSetupCanvasTopOffset = Canvas.GetTop(UiCnvBoard);

            _boardImageToBoardCanvasLeftOffset = Canvas.GetLeft(UiImgChessBoard);
            _boardImageToBoardCanvasTopOffset = Canvas.GetTop(UiImgChessBoard);
            _boardLeftOffset = _boardCanvasToSetupCanvasLeftOffset + _boardImageToBoardCanvasLeftOffset;
            _boardTopOffset = _boardCanvasToSetupCanvasTopOffset + _boardImageToBoardCanvasTopOffset;
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
                    if (_draggedPiece.Piece == PieceType.Pawn && (sc.Ycoord == 0 || sc.Ycoord == 7))
                    {
                        //RemovePieceFromBoard(sc);
                        UiCnvSetup.Children.Remove(_draggedPiece.ImageControl);
                        _draggedPiece.Clear();
                    }
                    else
                    {
                        AddPieceToBoard(sc, _draggedPiece.Piece, _draggedPiece.Color, _draggedPiece.ImageControl);
                        _draggedPiece.ImageControl = new Image();
                    }
                }
                else
                {
                    RemovePieceFromBoard(sc);
                }
                _draggedPiece.Clear();
            }
            e.Handled = true;
        }

        /// <summary>
        /// A mouse click ocurred within the Setup Canvas.
        /// It was in the chessboard as otherwise, off-the-board
        /// pieces woudl have picked up the event
        /// Start the drag process for the piece on the clicked
        /// square, if any.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCnvSetup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePoint = e.GetPosition(UiCnvSetup);

            SquareCoords sc = GetSquareCoordsFromSetupCanvasPoint(mousePoint);
            StartDrag(sc, e);
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

                    Canvas.SetLeft(_draggedPiece.ImageControl, mousePoint.X - _squareSize / 2);
                    Canvas.SetTop(_draggedPiece.ImageControl, mousePoint.Y - _squareSize / 2);
                }
            }
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
            if (_draggedPiece.IsDragInProgress)
            {
                RemovePieceFromBoard(null);
                _draggedPiece.IsDragInProgress = false;
            }
        }

        /// <summary>
        /// Sets up the drag operation using an off-the-board piece.
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

            e.Handled = true;
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
                byte square = PositionSetup.Board[sc.Xcoord, sc.Ycoord];

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
        /// Returns square coordinates of the clicked square or null
        /// if the click occured outside the board.
        /// The passed Point must be offset from the Setup Canvas.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private SquareCoords GetSquareCoordsFromSetupCanvasPoint(Point pt)
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
        /// Adds a piece to the board.
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
            PositionUtils.PlacePieceOnBoard(piece, color, (byte)sc.Xcoord, (byte)sc.Ycoord, ref PositionSetup.Board);
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
                PositionUtils.ClearSquare((byte)sc.Xcoord, (byte)sc.Ycoord, ref PositionSetup.Board);

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

        /// <summary>
        /// Places a piece image in the center of a square.
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="piece"></param>
        private void PlacePieceOnSquare(SquareCoords sc, Image piece)
        {
            Point pt = GetSquareTopLeft(sc);
            Canvas.SetLeft(piece, pt.X);
            Canvas.SetTop(piece, pt.Y);
        }

        /// <summary>
        /// Gets Top and Left offsets of the square
        /// with given coordinates.
        /// </summary>
        /// <param name="sc"></param>
        /// <returns></returns>
        private Point GetSquareTopLeft(SquareCoords sc)
        {
            Point pt = new Point();

            pt.X = sc.Xcoord * 30 + _boardLeftOffset;
            pt.Y = (7 - sc.Ycoord) * 30 + _boardTopOffset;

            return pt;
        }

        /// <summary>
        /// Given the position in _boardPosition,
        /// sets up the board's display.
        /// </summary>
        private void SetupImagesForPosition()
        {
            for (int x = 0; x <= 7; x++)
            {
                for (int y = 0; y <= 7; y++)
                {
                    byte square = PositionSetup.Board[x, y];
                    if (square != 0)
                    {
                        Image img = new Image();
                        PieceType piece = PositionUtils.GetPieceType(square);
                        PieceColor color = PositionUtils.GetPieceColor(square);
                        img.Source = Pieces.GetImageForPieceSmall(piece, color);
                        PlacePieceOnSquare(new SquareCoords(x, y), img);
                        _pieceImagesOnBoard[x, y] = img;
                        UiCnvSetup.Children.Add(img);
                    }
                    else
                    {
                        if (_pieceImagesOnBoard[x, y] != null)
                        {
                            UiCnvSetup.Children.Remove(_pieceImagesOnBoard[x, y]);
                            _pieceImagesOnBoard[x, y].Source = null;
                            _pieceImagesOnBoard[x, y] = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clears the _boardPosition.Board object and removes
        /// all images from the board.
        /// </summary>
        private void ClearAll()
        {
            PositionUtils.ClearPosition(ref PositionSetup.Board);
            foreach (Image img in _pieceImagesOnBoard)
            {
                if (img != null)
                {
                    UiCnvSetup.Children.Remove(img);
                    img.Source = null;
                }
            }
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
        }

        /// <summary>
        /// Resets all castling rights to either allowed
        /// or disallowed.
        /// </summary>
        /// <param name="allow"></param>
        private void ResetCastlingRights(bool allow)
        {
            UiCbWhiteCastleShort.IsChecked = allow;
            UiCbWhiteCastleLong.IsChecked = allow;
            UiCbBlackCastleShort.IsChecked = allow;
            UiCbBlackCastleLong.IsChecked = allow;
        }


        //************************************************************
        //
        // Button click handlers.
        //
        //************************************************************

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
        /// Sets up the starting position in the GUI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnStartingPos_Click(object sender, RoutedEventArgs e)
        {
            ClearAll();

            PositionSetup = PositionUtils.SetupStartingPosition();
            SetupImagesForPosition();
            
            SetSideToMove(PieceColor.White);
            ResetCastlingRights(true);
        }

        /// <summary>
        /// Handles the Clear button being pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearAll();

            SetSideToMove(PieceColor.White);
            ResetCastlingRights(false);
        }

        /// <summary>
        /// Exits the dialog on user pressing the OK button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {

            PositionUtils.SetCastlingRights(
                UiCbWhiteCastleShort.IsChecked == true,
                UiCbWhiteCastleLong.IsChecked == true,
                UiCbBlackCastleShort.IsChecked == true,
                UiCbBlackCastleLong.IsChecked == true,
                ref PositionSetup);

            PositionSetup.ColorToMove = _sideToMove;
            if (_sideToMove == PieceColor.Black)
            {
                PositionSetup.MoveNumber = 1;
            }
            else
            {
                PositionSetup.MoveNumber = 0;
            }

            if (PositionUtils.ValidatePosition(ref PositionSetup, out string errorText))
            {
                ExitOK = true;
                Close();
            }
            else
            {
                MessageBox.Show(errorText, "Invalid Position Setup", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Exists the dialog on user pressing the Cancel button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ExitOK = false;
            Close();
        }

        /// <summary>
        /// Makes the Debug button visible.
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        private void ShowDebugButton()
        {
            if (Configuration.DebugLevel >= 3)
            {
                UiDebug.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Shows the currently setup position on the main board.
        /// Useful for debugging this dialog's logic.
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        private void ShowChessBoard()
        {
            AppStateManager.MainWin.DisplayPosition(PositionSetup);
        }

        /// <summary>
        /// Debug Button click event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiDebug_Click(object sender, RoutedEventArgs e)
        {
            ShowChessBoard();
        }
    }
}

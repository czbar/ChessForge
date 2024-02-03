﻿using ChessPosition;
using GameTree;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SearchPositionDialog.xaml
    /// </summary>
    public partial class SearchPositionDialog : Window
    {
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

        // position to start with
        private BoardPosition _position;

        /// <summary>
        /// Constructs the dialog.
        /// Sets up offset values.
        /// </summary>
        public SearchPositionDialog(BoardPosition position)
        {
            InitializeComponent();
            _position = position;

            _boardCanvasToSetupCanvasLeftOffset = Canvas.GetLeft(UiCnvBoard);
            _boardCanvasToSetupCanvasTopOffset = Canvas.GetTop(UiCnvBoard);

            _boardImageToBoardCanvasLeftOffset = Canvas.GetLeft(UiImgChessBoard);
            _boardImageToBoardCanvasTopOffset = Canvas.GetTop(UiImgChessBoard);
            _boardLeftOffset = _boardCanvasToSetupCanvasLeftOffset + _boardImageToBoardCanvasLeftOffset;
            _boardTopOffset = _boardCanvasToSetupCanvasTopOffset + _boardImageToBoardCanvasTopOffset;

            ShowCoordinates();

            InitializePosition(_position);

            PositionSetup.HalfMove50Clock = 1;
            SetFen();
        }

        /// <summary>
        /// Sets up the initial position if a non-null TreeNode
        /// was passed.
        /// </summary>
        /// <param name="nd"></param>
        private void InitializePosition(BoardPosition position)
        {
            PositionSetup = new BoardPosition(position);
            SetupImagesForPosition();
        }

        /// <summary>
        /// Generates FEN from the positions and populates the FEN text box with the result.
        /// </summary>
        private void SetFen(bool checkEnpassant = true)
        {
            UiTbFen.Text = FenParser.GenerateFenFromPosition(PositionSetup, 0);
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

            SetFen();
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

                    // clear previous content
                    if (_pieceImagesOnBoard[x, y] != null)
                    {
                        UiCnvSetup.Children.Remove(_pieceImagesOnBoard[x, y]);
                        _pieceImagesOnBoard[x, y].Source = null;
                        _pieceImagesOnBoard[x, y] = null;
                    }

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

        //************************************************************
        //
        //  CONTROLS AFFECTING THE POSITION (FEN)
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
                        SetupImagesForPosition();
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

            if (tokens1[1] == tokens2[1])
            {
                colorToMove = false;
            }

            if (tokens1[2] == tokens2[2])
            {
                castling = false;
            }

            if (tokens1[3] == tokens2[3])
            {
                enpassant = false;
            }

            return position | colorToMove | castling | enpassant;
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
            if (GuiUtilities.ValidatePosition(ref PositionSetup, out string errorText))
            {
                DialogResult = true;
            }
            else
            {
                MessageBox.Show(errorText, Properties.Resources.InvalidPositionSetup, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Exists the dialog on user pressing the Cancel button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false ;
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Find-Positions");
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
    }
}

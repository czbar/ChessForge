using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Encapsulate the Chessboard GUI object
    /// including the hosting canvas.
    /// </summary>
    public class ChessBoard
    {
        /// <summary>
        /// Images for White pieces.
        /// </summary>
        private static Dictionary<PieceType, BitmapImage> WhitePieces =
            new Dictionary<PieceType, BitmapImage>()
            {
                [PieceType.Rook] = ChessForge.Pieces.WhiteRook,
                [PieceType.Bishop] = ChessForge.Pieces.WhiteBishop,
                [PieceType.Knight] = ChessForge.Pieces.WhiteKnight,
                [PieceType.Queen] = ChessForge.Pieces.WhiteQueen,
                [PieceType.King] = ChessForge.Pieces.WhiteKing,
                [PieceType.Pawn] = ChessForge.Pieces.WhitePawn
            };

        /// <summary>
        /// Images for Black pieces.
        /// </summary>
        private static Dictionary<PieceType, BitmapImage> BlackPieces =
            new Dictionary<PieceType, BitmapImage>()
            {
                [PieceType.Rook] = ChessForge.Pieces.BlackRook,
                [PieceType.Bishop] = ChessForge.Pieces.BlackBishop,
                [PieceType.Knight] = ChessForge.Pieces.BlackKnight,
                [PieceType.Queen] = ChessForge.Pieces.BlackQueen,
                [PieceType.King] = ChessForge.Pieces.BlackKing,
                [PieceType.Pawn] = ChessForge.Pieces.BlackPawn
            };

        // board position currently shown
        private BoardPosition _position;

        /// <summary>
        /// Size of an individual square in pixels
        /// </summary>
        private const int squareSize = 80;

        /// <summary>
        /// Hositing Canvas control
        /// </summary>
        private Canvas CanvasCtrl;

        /// <summary>
        /// Image control for the board.
        /// </summary>
        private Image BoardImgCtrl;

        /// <summary>
        /// Overlay image (yellow square) for the 
        /// move's origin.
        /// </summary>
        private Image _moveFromOverlay = new Image();

        /// <summary>
        /// Overlay image (yellow square) for the 
        /// move's destination.
        /// </summary>
        private Image _moveToOverlay = new Image();

        /// <summary>
        /// Label control to be optionally shown above the board.
        /// </summary>
        private Label LabelCtrl;

        /// <summary>
        /// Whether the board is displayed upside down.
        /// </summary>
        private bool _isFlipped;

        /// <summary>
        /// Image controls for ech square
        /// </summary>
        private Image[,] Pieces = new Image[8, 8];

        /// <summary>
        /// Coordinates of the square with an active overlay, 
        /// representing the move's origin.
        /// </summary>
        private SquareCoords _coloredSquareFrom = new SquareCoords(-1, -1);

        /// <summary>
        /// Coordinates of the square with an active overlay, 
        /// representing the move's destination.
        /// </summary>
        private SquareCoords _coloredSquareTo = new SquareCoords(-1, -1);

        /// <summary>
        /// Creates a ChessBoard object and initializes
        /// Image UI elements for each square.
        /// </summary>
        /// <param name="cnv"></param>
        /// <param name="BoardCtrl"></param>
        /// <param name="labelCtrl"></param>
        /// <param name="startPos"></param>
        public ChessBoard(Canvas cnv, Image BoardCtrl, Label labelCtrl, bool startPos)
        {
            CanvasCtrl = cnv;
            BoardImgCtrl = BoardCtrl;
            LabelCtrl = labelCtrl;

            Initialize(startPos);
        }

        /// <summary>
        /// Gets the image for regular size White piece
        /// of the requested type.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static BitmapImage GetWhitePieceRegImg(PieceType pt)
        {
            return WhitePieces[pt];
        }

        /// <summary>
        /// Gets the image for regular size Black piece
        /// of the requested type.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static BitmapImage GetBlackPieceRegImg(PieceType pt)
        {
            return BlackPieces[pt];
        }

        /// <summary>
        /// Sets the image for the board.
        /// </summary>
        /// <param name="imgBoard"></param>
        public void SetBoardSourceImage(BitmapImage imgBoard)
        {
            BoardImgCtrl.Source = imgBoard;
        }

        /// <summary>
        /// Sets the opacity of the board.
        /// </summary>
        /// <param name="opacity"></param>
        public void SetBoardOpacity(double opacity)
        {
            BoardImgCtrl.Opacity = opacity;
            LabelCtrl.Opacity = opacity;
        }

        /// <summary>
        /// Returns new SquareCoords object with the coordinates
        /// flipped if the board is flipped ot force == true.
        /// </summary>
        /// <param name="sq"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        public SquareCoords FlipCoords(SquareCoords sq, bool force = false)
        {
            if (_isFlipped || force)
            {
                return new SquareCoords(7 - sq.Xcoord, 7 - sq.Ycoord);
            }
            else
            {
                return sq;
            }
        }

        /// <summary>
        /// Places a yellow overlay to indicate the recently made move
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <param name="fromTo"></param>
        public void ColorMoveSquare(int xCoord, int yCoord, bool fromTo)
        {
            if (fromTo)
            {
                _coloredSquareFrom.Xcoord = xCoord;
                _coloredSquareFrom.Ycoord = yCoord;
            }
            else
            {
                _coloredSquareTo.Xcoord = xCoord;
                _coloredSquareTo.Ycoord = yCoord;
            }

            if (_isFlipped)
            {
                xCoord = 7 - xCoord;
                yCoord = 7 - yCoord;
            }

            double left = Canvas.GetLeft(Pieces[xCoord, yCoord]);
            double top = Canvas.GetTop(Pieces[xCoord, yCoord]);

            Image img = fromTo ? _moveFromOverlay : _moveToOverlay;
            img.Opacity = 0.3;
            CanvasCtrl.Children.Add(img);
            Canvas.SetLeft(img, left);
            Canvas.SetTop(img, top);
            Canvas.SetZIndex(img, Constants.ZIndex_SquareMoveOverlay);
        }

        /// <summary>
        /// Removes move overlay images.
        /// </summary>
        public void RemoveMoveSquareColors()
        {
            AppStateManager.MainWin.Dispatcher.Invoke(() =>
            {
                CanvasCtrl.Children.Remove(_moveFromOverlay);
                CanvasCtrl.Children.Remove(_moveToOverlay);
            });

            _coloredSquareTo.Xcoord = -1;
            _coloredSquareTo.Ycoord = -1;

            _coloredSquareFrom.Xcoord = -1;
            _coloredSquareFrom.Ycoord = -1;
        }

        /// <summary>
        /// Returns the image at a given coordinates.
        /// The caller may want to ignore the IsFlipped state
        /// in this is called in response to the user clicking
        /// a square.
        /// However, it this is part of making a move on the board
        /// based on the line notation or engine's move
        /// the coordinates may have to be flipped is IsFlipped is set.
        /// </summary>
        /// <param name="xcoord"></param>
        /// <param name="ycoord"></param>
        /// <param name="ignoreFlip"></param>
        /// <returns></returns>
        public Image GetPieceImage(int xcoord, int ycoord, bool ignoreFlip)
        {
            if (ignoreFlip)
            {
                return Pieces[xcoord, ycoord];
            }
            else
            {
                return Pieces[7 - xcoord, 7 - ycoord];
            }
        }

        /// <summary>
        /// Sets piece image on the requested square.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="xcoord"></param>
        /// <param name="ycoord"></param>
        /// <param name="ignoreFlip"></param>
        public void SetPieceImage(Image img, int xcoord, int ycoord, bool ignoreFlip)
        {
            if (ignoreFlip)
            {
                Pieces[xcoord, ycoord] = img;
            }
            else
            {
                Pieces[7 - xcoord, 7 - ycoord] = img;
            }
        }

        /// <summary>
        /// Sets the text on the label above
        /// the board.
        /// </summary>
        /// <param name="text"></param>
        public void SetLabelText(string text)
        {
            LabelCtrl.Content = text;
        }

        /// <summary>
        /// Flips the board upside down.
        /// If it is already flipped it will go back
        /// to normal.
        /// </summary>
        public void FlipBoard()
        {
            System.Windows.Media.ImageSource temp;

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    temp = Pieces[x, y].Source;
                    Pieces[x, y].Source = Pieces[7 - x, 7 - y].Source;
                    Pieces[7 - x, 7 - y].Source = temp;
                }
            }

            SquareCoords sqFrom = new SquareCoords(_coloredSquareFrom);
            SquareCoords sqTo = new SquareCoords(_coloredSquareTo);
            RemoveMoveSquareColors();

            if (sqFrom.Xcoord >= 0)
            {
                ColorMoveSquare(7 - sqFrom.Xcoord, 7 - sqFrom.Ycoord, true);
            }
            if (sqTo.Xcoord >= 0)
            {
                ColorMoveSquare(7 - sqTo.Xcoord, 7 - sqTo.Ycoord, false);
            }

            _isFlipped = !_isFlipped;
        }

        /// <summary>
        /// Determines whether the chessboard should be flipped to show
        /// the passed color at the bottom.
        /// If so, flips the board.
        /// </summary>
        /// <param name="sideAtBottom"></param>
        public void FlipBoard(PieceColor sideAtBottom)
        {
            if (sideAtBottom == PieceColor.White && _isFlipped
                || sideAtBottom == PieceColor.Black && !_isFlipped)
            {
                FlipBoard();
            }
        }

        /// <summary>
        /// Sets up the position on the board
        /// reflecting the passed Position object.
        /// </summary>
        /// <param name="pos"></param>
        public void DisplayPosition(BoardPosition pos)
        {
            _position = new BoardPosition(pos);

            for (int xcoord = 0; xcoord < 8; xcoord++)
            {
                for (int ycoord = 0; ycoord < 8; ycoord++)
                {
                    DisplayPiece(xcoord, ycoord, pos.Board[xcoord, ycoord]);
                }
            }

            RemoveMoveSquareColors();
        }

        /// <summary>
        /// Sets the position object.
        /// If display == true, shows the position in the GUI.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="display"></param>
        public void SetPosition(BoardPosition pos, bool display)
        {
            if (display)
            {
                DisplayPosition(pos);
            }
            else
            {
                _position = new BoardPosition(pos);
            }
        }

        /// <summary>
        /// Removes all piece images from the board.
        /// </summary>
        public void ClearBoard()
        {
            _position = new BoardPosition();

            for (int xcoord = 0; xcoord < 8; xcoord++)
            {
                for (int ycoord = 0; ycoord < 8; ycoord++)
                {
                    DisplayPiece(xcoord, ycoord, 0);
                }
            }

            RemoveMoveSquareColors();
        }

        /// <summary>
        /// Sets and shows the starting position.
        /// </summary>
        public void DisplayStartingPosition()
        {
            _position = PositionUtils.SetupStartingPosition();
            DisplayPosition(_position);
        }

        /// <summary>
        /// Sets and shows the starting position.
        /// </summary>
        public void SetStartingPosition()
        {
            DisplayStartingPosition();
        }

        /// <summary>
        /// Returns the type of the piece occupying the passed square.
        /// </summary>
        /// <param name="sq"></param>
        /// <returns></returns>
        public PieceType GetPieceType(SquareCoords sq)
        {
            if (PositionUtils.AreValidCoordinates(sq))
            {
                return PositionUtils.GetPieceType(_position.Board[sq.Xcoord, sq.Ycoord]);
            }
            else
            {
                return PieceType.None;
            }
        }

        /// <summary>
        /// Returns the color of the piece occupying the passed square.
        /// </summary>
        /// <param name="sq"></param>
        /// <returns></returns>
        public PieceColor GetPieceColor(SquareCoords sq)
        {
            if (PositionUtils.AreValidCoordinates(sq))
            {
                return PositionUtils.GetPieceColor(_position.Board[sq.Xcoord, sq.Ycoord]);
            }
            else
            {
                return PieceColor.None;
            }
        }

        /// <summary>
        /// Indicates whether the board is displayed upside down
        /// </summary>
        public bool IsFlipped { get => _isFlipped; set => _isFlipped = value; }

        /// <summary>
        /// This is the only method that handles the IsFlipped flag.
        /// All other methods displaying pieces and positions
        /// end up calling this one so they do not concern themselves
        /// with the position of the board.
        /// </summary>
        /// <param name="xcoord"></param>
        /// <param name="ycoord"></param>
        /// <param name="img"></param>
        private void PlacePiece(int xcoord, int ycoord, BitmapImage img)
        {
            Pieces[xcoord, ycoord].Dispatcher.Invoke(() =>
            {
                if (_isFlipped)
                {
                    xcoord = 7 - xcoord;
                    ycoord = 7 - ycoord;
                }

                Pieces[xcoord, ycoord].Source = img;
            });
        }

        /// <summary>
        /// Sets Image controls for every square.
        /// The position in the Canvas depends on the
        /// rank and file on the chessboard and on
        /// whether the board is flipped.
        /// </summary>
        /// <param name="startPosition"></param>
        private void Initialize(bool startPosition)
        {
            for (var i = 0; i < 8; i++)
            {
                for (var j = 0; j < 8; j++)
                {
                    int xPos = i;
                    int yPos = 7 - j;

                    Pieces[xPos, yPos] = new Image();
                    CanvasCtrl.Children.Add(Pieces[xPos, yPos]);
                    Canvas.SetLeft(Pieces[xPos, yPos], squareSize * xPos + BoardImgCtrl.Margin.Left);
                    Canvas.SetTop(Pieces[xPos, yPos], squareSize * (7 - yPos) + BoardImgCtrl.Margin.Top);
                    Canvas.SetZIndex(Pieces[xPos, yPos], Constants.ZIndex_PieceOnBoard);
                }
            }
            if (startPosition)
            {
                SetStartingPosition();
            }

            _moveFromOverlay.Source = ChessForge.Pieces.YellowOverlay;
            _moveToOverlay.Source = ChessForge.Pieces.YellowOverlay;
        }

        /// <summary>
        /// Puts a piece image on the board if one
        /// is encoded in "square".
        /// Otherwise, clears the square.
        /// </summary>
        /// <param name="xcoord"></param>
        /// <param name="ycoord"></param>
        /// <param name="square"></param>
        private void DisplayPiece(int xcoord, int ycoord, byte square)
        {
            AppStateManager.MainWin.Dispatcher.Invoke(() =>
            {
                if (PositionUtils.GetPieceColor(square) == PieceColor.White)
                {
                    PlacePiece(xcoord, ycoord, WhitePieces[PositionUtils.GetPieceType(square)]);
                }
                else if (PositionUtils.GetPieceColor(square) == PieceColor.Black)
                {
                    PlacePiece(xcoord, ycoord, BlackPieces[PositionUtils.GetPieceType(square)]);
                }
                else
                {
                    PlacePiece(xcoord, ycoord, null);
                }
            });
        }

    }
}

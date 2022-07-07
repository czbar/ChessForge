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
    /// Encapsualate the Chessboard GUI object
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
        /// Label control to be optionally shown above the board.
        /// </summary>
        private Label LabelCtrl;

        /// <summary>
        /// Whether the board is displayed upside down.
        /// </summary>
        private bool _isFlipped;

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

        public void SetBoardSourceImage(BitmapImage imgBoard)
        {
            BoardImgCtrl.Source = imgBoard;
        }

        public void SetBoardOpacity(double opacity)
        {
            BoardImgCtrl.Opacity = opacity;
            LabelCtrl.Opacity = opacity;
        }

        /// <summary>
        /// Image controls for ech square
        /// </summary>
        private Image[,] Pieces = new Image[8, 8];

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


        public void SetLabelText(string text)
        {
            LabelCtrl.Content = text;
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
                }
            }
            if (startPosition)
            {
                SetStartingPosition();
            }
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

            _isFlipped = !_isFlipped;
        }

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
        /// Sets up the position on the board
        /// reflecting the passed Position object.
        /// </summary>
        /// <param name="pos"></param>
        public void DisplayPosition(BoardPosition pos)
        {
            for (int xcoord = 0; xcoord < 8; xcoord++)
            {
                for (int ycoord = 0; ycoord < 8; ycoord++)
                {
                    DisplayPiece(xcoord, ycoord, pos.Board[xcoord, ycoord]);
                }
            }
        }

        public void ClearBoard()
        {
            for (int xcoord = 0; xcoord < 8; xcoord++)
            {
                for (int ycoord = 0; ycoord < 8; ycoord++)
                {
                    DisplayPiece(xcoord, ycoord, 0);
                }
            }
        }

        private void DisplayPiece(int xcoord, int ycoord, byte square)
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
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

        /// <summary>
        /// Sets up the position on the main GUI board
        /// reflecting the passed Fen string.
        /// </summary>
        /// <param name="fen"></param>
        public void DisplayPosition(string fen)
        {
            // TODO inmplement
        }

        public void DisplayStartingPosition()
        {
            DisplayPosition(PositionUtils.SetupStartingPosition());
        }

        public void SetStartingPosition()
        {
            DisplayStartingPosition();
        }

        /// <summary>
        /// Indicates whether the board is displayed upside down
        /// </summary>
        public bool IsFlipped { get => _isFlipped; set => _isFlipped = value; }
    }
}

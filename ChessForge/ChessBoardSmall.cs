using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ChessForge
{
    public class ChessBoardSmall : ChessBoard
    {
        // size of an individual square in pixels
        private const int _squareSize = 30;

        /// <summary>
        /// Size of an individual square in pixels
        /// </summary>
        override protected int SquareSize
        {
            get => _squareSize;
        }

        /// <summary>
        /// Images for White pieces 30x30.
        /// </summary>
        protected Dictionary<PieceType, BitmapImage> _dictWhitePiecesSmall =
            new Dictionary<PieceType, BitmapImage>()
            {
                [PieceType.Rook] = ChessForge.Pieces.WhiteRookSmall,
                [PieceType.Bishop] = ChessForge.Pieces.WhiteBishopSmall,
                [PieceType.Knight] = ChessForge.Pieces.WhiteKnightSmall,
                [PieceType.Queen] = ChessForge.Pieces.WhiteQueenSmall,
                [PieceType.King] = ChessForge.Pieces.WhiteKingSmall,
                [PieceType.Pawn] = ChessForge.Pieces.WhitePawnSmall
            };

        /// <summary>
        /// Images for Black pieces 80x80.
        /// </summary>
        protected Dictionary<PieceType, BitmapImage> _dictBlackPiecesSmall =
            new Dictionary<PieceType, BitmapImage>()
            {
                [PieceType.Rook] = ChessForge.Pieces.BlackRookSmall,
                [PieceType.Bishop] = ChessForge.Pieces.BlackBishopSmall,
                [PieceType.Knight] = ChessForge.Pieces.BlackKnightSmall,
                [PieceType.Queen] = ChessForge.Pieces.BlackQueenSmall,
                [PieceType.King] = ChessForge.Pieces.BlackKingSmall,
                [PieceType.Pawn] = ChessForge.Pieces.BlackPawnSmall
            };

        /// <summary>
        /// Accessor to White pieces images.
        /// </summary>
        override protected Dictionary<PieceType, BitmapImage> WhitePieces
        {
            get => _dictWhitePiecesSmall;
        }

        /// <summary>
        /// Accessor to Black pieces images.
        /// </summary>
        override protected Dictionary<PieceType, BitmapImage> BlackPieces
        {
            get => _dictBlackPiecesSmall;
        }

        /// <summary>
        /// Constructs a chess board with 30x30 squares.
        /// </summary>
        /// <param name="cnv"></param>
        /// <param name="BoardCtrl"></param>
        /// <param name="labelCtrl"></param>
        /// <param name="startPos"></param>
        /// <param name="includeCoords"></param>
        public ChessBoardSmall(Canvas cnv, Image BoardCtrl, Label labelCtrl, bool startPos, bool includeCoords)
            : base (cnv, BoardCtrl, labelCtrl, startPos, includeCoords)
        {
        }

        /// <summary>
        /// Places an image of a piece (or empty image object)
        /// on the request square.
        /// NOTE: we override in ChessBoardSmall because it uses different
        /// way of placing the board (TODO: make it consistent)
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        override protected void PlacePieceImageOnSquare(int xPos, int yPos)
        {
            Canvas.SetLeft(Pieces[xPos, yPos], SquareSize * xPos + Canvas.GetLeft(BoardImgCtrl));
            Canvas.SetTop(Pieces[xPos, yPos], SquareSize * (7 - yPos) + Canvas.GetTop(BoardImgCtrl));
            Canvas.SetZIndex(Pieces[xPos, yPos], Constants.ZIndex_PieceOnBoard);
        }


        /// <summary>
        /// Sizes and positions coordinate labels.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="lbl"></param>
        /// <param name="isVert"></param>
        override protected void SetCoordinateLabelProperties(int index, Label lbl, bool isVert)
        {
            lbl.Width = 20;
            lbl.Height = 21;
            lbl.FontSize = 8;

            if (isVert)
            {
                Canvas.SetLeft(lbl, -2);
                Canvas.SetTop(lbl, 18 + (index * 30));
            }
            else
            {
                Canvas.SetLeft(lbl, 19 + (index * 30));
                Canvas.SetBottom(lbl, -1);
            }
        }

    }
}

using ChessPosition;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace ChessForge
{
    public class PieceImageDictionaries
    {
        /// <summary>
        /// Images for White pieces 80x80.
        /// </summary>
        public static Dictionary<PieceType, BitmapImage> WhitePiecesLarge =
            new Dictionary<PieceType, BitmapImage>()
            {
                [PieceType.Rook] = Pieces.WhiteRook,
                [PieceType.Bishop] = Pieces.WhiteBishop,
                [PieceType.Knight] = Pieces.WhiteKnight,
                [PieceType.Queen] = Pieces.WhiteQueen,
                [PieceType.King] = Pieces.WhiteKing,
                [PieceType.Pawn] = Pieces.WhitePawn
            };

        /// <summary>
        /// Images for Black pieces 80x80.
        /// </summary>
        public static Dictionary<PieceType, BitmapImage> BlackPiecesLarge =
            new Dictionary<PieceType, BitmapImage>()
            {
                [PieceType.Rook] = Pieces.BlackRook,
                [PieceType.Bishop] = Pieces.BlackBishop,
                [PieceType.Knight] = Pieces.BlackKnight,
                [PieceType.Queen] = Pieces.BlackQueen,
                [PieceType.King] = Pieces.BlackKing,
                [PieceType.Pawn] = Pieces.BlackPawn
            };

        /// <summary>
        /// Images for White pieces 30x30.
        /// </summary>
        public static Dictionary<PieceType, BitmapImage> WhitePiecesSmall =
            new Dictionary<PieceType, BitmapImage>()
            {
                [PieceType.Rook] = Pieces.WhiteRookSmall,
                [PieceType.Bishop] = Pieces.WhiteBishopSmall,
                [PieceType.Knight] = Pieces.WhiteKnightSmall,
                [PieceType.Queen] = Pieces.WhiteQueenSmall,
                [PieceType.King] = Pieces.WhiteKingSmall,
                [PieceType.Pawn] = Pieces.WhitePawn
            };

        /// <summary>
        /// Images for Black pieces 30x30.
        /// </summary>
        public static Dictionary<PieceType, BitmapImage> BlackPiecesSmall =
            new Dictionary<PieceType, BitmapImage>()
            {
                [PieceType.Rook] = Pieces.BlackRookSmall,
                [PieceType.Bishop] = Pieces.BlackBishopSmall,
                [PieceType.Knight] = Pieces.BlackKnightSmall,
                [PieceType.Queen] = Pieces.BlackQueenSmall,
                [PieceType.King] = Pieces.BlackKingSmall,
                [PieceType.Pawn] = Pieces.BlackPawn
            };
    }
}

using ChessPosition;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ChessForge
{
    public class ChessBoardOtherSize : ChessBoardSmall
    {
        /// <summary>
        /// Constructor. 
        /// Sets size of an individual square in pixels.
        /// Calls initialization of the piece image dictionaries.
        /// </summary>
        /// <param name="cnv"></param>
        /// <param name="boardCtrl"></param>
        /// <param name="squareSize"></param>
        public ChessBoardOtherSize(Canvas cnv, Image boardCtrl, ChessBoards.SquareSize squareSize)
             : base(cnv, boardCtrl, null, null, false, false)
        {
            switch (squareSize)
            {
                case ChessBoards.SquareSize.SIZE_15:
                    _squareSize = 15;
                    break;
                case ChessBoards.SquareSize.SIZE_18:
                    _squareSize = 18;
                    break;
                case ChessBoards.SquareSize.SIZE_20:
                    _squareSize = 20;
                    break;
                case ChessBoards.SquareSize.SIZE_30:
                    _squareSize = 30;
                    break;
                case ChessBoards.SquareSize.SIZE_45:
                    _squareSize = 45;
                    break;
                case ChessBoards.SquareSize.SIZE_60:
                    _squareSize = 60;
                    break;
                case ChessBoards.SquareSize.SIZE_80:
                    _squareSize = 80;
                    break;
                default:
                    _squareSize = 30;
                    break;
            }

            BuildPieceDictionaries(squareSize);
        }

        /// <summary>
        /// Initializes the piece image dictionaries with images
        /// of appropriate sizes.
        /// </summary>
        /// <param name="squareSize"></param>
        private void BuildPieceDictionaries(ChessBoards.SquareSize squareSize)
        {
            _dictWhitePiecesSmall.Clear();

            switch (squareSize)
            {
                case ChessBoards.SquareSize.SIZE_15:
                    _dictWhitePiecesSmall = new Dictionary<PieceType, BitmapImage>()
                    {
                        [PieceType.Rook] = ChessForge.Pieces.WhiteRook_15,
                        [PieceType.Bishop] = ChessForge.Pieces.WhiteBishop_15,
                        [PieceType.Knight] = ChessForge.Pieces.WhiteKnight_15,
                        [PieceType.Queen] = ChessForge.Pieces.WhiteQueen_15,
                        [PieceType.King] = ChessForge.Pieces.WhiteKing_15,
                        [PieceType.Pawn] = ChessForge.Pieces.WhitePawn_15
                    };
                    _dictBlackPiecesSmall = new Dictionary<PieceType, BitmapImage>()
                    {
                        [PieceType.Rook] = ChessForge.Pieces.BlackRook_15,
                        [PieceType.Bishop] = ChessForge.Pieces.BlackBishop_15,
                        [PieceType.Knight] = ChessForge.Pieces.BlackKnight_15,
                        [PieceType.Queen] = ChessForge.Pieces.BlackQueen_15,
                        [PieceType.King] = ChessForge.Pieces.BlackKing_15,
                        [PieceType.Pawn] = ChessForge.Pieces.BlackPawn_15
                    };
                    break;
                case ChessBoards.SquareSize.SIZE_18:
                    _dictWhitePiecesSmall = new Dictionary<PieceType, BitmapImage>()
                    {
                        [PieceType.Rook] = ChessForge.Pieces.WhiteRook_18,
                        [PieceType.Bishop] = ChessForge.Pieces.WhiteBishop_18,
                        [PieceType.Knight] = ChessForge.Pieces.WhiteKnight_18,
                        [PieceType.Queen] = ChessForge.Pieces.WhiteQueen_18,
                        [PieceType.King] = ChessForge.Pieces.WhiteKing_18,
                        [PieceType.Pawn] = ChessForge.Pieces.WhitePawn_18
                    };
                    _dictBlackPiecesSmall = new Dictionary<PieceType, BitmapImage>()
                    {
                        [PieceType.Rook] = ChessForge.Pieces.BlackRook_18,
                        [PieceType.Bishop] = ChessForge.Pieces.BlackBishop_18,
                        [PieceType.Knight] = ChessForge.Pieces.BlackKnight_18,
                        [PieceType.Queen] = ChessForge.Pieces.BlackQueen_18,
                        [PieceType.King] = ChessForge.Pieces.BlackKing_18,
                        [PieceType.Pawn] = ChessForge.Pieces.BlackPawn_18
                    };
                    break;
                case ChessBoards.SquareSize.SIZE_20:
                    _dictWhitePiecesSmall = new Dictionary<PieceType, BitmapImage>()
                    {
                        [PieceType.Rook] = ChessForge.Pieces.WhiteRook_20,
                        [PieceType.Bishop] = ChessForge.Pieces.WhiteBishop_20,
                        [PieceType.Knight] = ChessForge.Pieces.WhiteKnight_20,
                        [PieceType.Queen] = ChessForge.Pieces.WhiteQueen_20,
                        [PieceType.King] = ChessForge.Pieces.WhiteKing_20,
                        [PieceType.Pawn] = ChessForge.Pieces.WhitePawn_20
                    };
                    _dictBlackPiecesSmall = new Dictionary<PieceType, BitmapImage>()
                    {
                        [PieceType.Rook] = ChessForge.Pieces.BlackRook_20,
                        [PieceType.Bishop] = ChessForge.Pieces.BlackBishop_20,
                        [PieceType.Knight] = ChessForge.Pieces.BlackKnight_20,
                        [PieceType.Queen] = ChessForge.Pieces.BlackQueen_20,
                        [PieceType.King] = ChessForge.Pieces.BlackKing_20,
                        [PieceType.Pawn] = ChessForge.Pieces.BlackPawn_20
                    };
                    break;
                case ChessBoards.SquareSize.SIZE_30:
                    _dictWhitePiecesSmall = new Dictionary<PieceType, BitmapImage>()
                    {
                        [PieceType.Rook] = ChessForge.Pieces.WhiteRookSmall,
                        [PieceType.Bishop] = ChessForge.Pieces.WhiteBishopSmall,
                        [PieceType.Knight] = ChessForge.Pieces.WhiteKnightSmall,
                        [PieceType.Queen] = ChessForge.Pieces.WhiteQueenSmall,
                        [PieceType.King] = ChessForge.Pieces.WhiteKingSmall,
                        [PieceType.Pawn] = ChessForge.Pieces.WhitePawnSmall
                    };
                    _dictBlackPiecesSmall = new Dictionary<PieceType, BitmapImage>()
                    {
                        [PieceType.Rook] = ChessForge.Pieces.BlackRookSmall,
                        [PieceType.Bishop] = ChessForge.Pieces.BlackBishopSmall,
                        [PieceType.Knight] = ChessForge.Pieces.BlackKnightSmall,
                        [PieceType.Queen] = ChessForge.Pieces.BlackQueenSmall,
                        [PieceType.King] = ChessForge.Pieces.BlackKingSmall,
                        [PieceType.Pawn] = ChessForge.Pieces.BlackPawnSmall
                    };
                    break;
                case ChessBoards.SquareSize.SIZE_45:
                    _dictWhitePiecesSmall = new Dictionary<PieceType, BitmapImage>()
                    {
                        [PieceType.Rook] = ChessForge.Pieces.WhiteRook_45,
                        [PieceType.Bishop] = ChessForge.Pieces.WhiteBishop_45,
                        [PieceType.Knight] = ChessForge.Pieces.WhiteKnight_45,
                        [PieceType.Queen] = ChessForge.Pieces.WhiteQueen_45,
                        [PieceType.King] = ChessForge.Pieces.WhiteKing_45,
                        [PieceType.Pawn] = ChessForge.Pieces.WhitePawn_45
                    };
                    _dictBlackPiecesSmall = new Dictionary<PieceType, BitmapImage>()
                    {
                        [PieceType.Rook] = ChessForge.Pieces.BlackRook_45,
                        [PieceType.Bishop] = ChessForge.Pieces.BlackBishop_45,
                        [PieceType.Knight] = ChessForge.Pieces.BlackKnight_45,
                        [PieceType.Queen] = ChessForge.Pieces.BlackQueen_45,
                        [PieceType.King] = ChessForge.Pieces.BlackKing_45,
                        [PieceType.Pawn] = ChessForge.Pieces.BlackPawn_45
                    };
                    break;
                case ChessBoards.SquareSize.SIZE_60:
                    _dictWhitePiecesSmall = new Dictionary<PieceType, BitmapImage>()
                    {
                        [PieceType.Rook] = ChessForge.Pieces.WhiteRook_60,
                        [PieceType.Bishop] = ChessForge.Pieces.WhiteBishop_60,
                        [PieceType.Knight] = ChessForge.Pieces.WhiteKnight_60,
                        [PieceType.Queen] = ChessForge.Pieces.WhiteQueen_60,
                        [PieceType.King] = ChessForge.Pieces.WhiteKing_60,
                        [PieceType.Pawn] = ChessForge.Pieces.WhitePawn_60
                    };
                    _dictBlackPiecesSmall = new Dictionary<PieceType, BitmapImage>()
                    {
                        [PieceType.Rook] = ChessForge.Pieces.BlackRook_60,
                        [PieceType.Bishop] = ChessForge.Pieces.BlackBishop_60,
                        [PieceType.Knight] = ChessForge.Pieces.BlackKnight_60,
                        [PieceType.Queen] = ChessForge.Pieces.BlackQueen_60,
                        [PieceType.King] = ChessForge.Pieces.BlackKing_60,
                        [PieceType.Pawn] = ChessForge.Pieces.BlackPawn_60
                    };
                    break;
                case ChessBoards.SquareSize.SIZE_80:
                    _dictWhitePiecesSmall = new Dictionary<PieceType, BitmapImage>()
                    {
                        [PieceType.Rook] = ChessForge.Pieces.WhiteRook,
                        [PieceType.Bishop] = ChessForge.Pieces.WhiteBishop,
                        [PieceType.Knight] = ChessForge.Pieces.WhiteKnight,
                        [PieceType.Queen] = ChessForge.Pieces.WhiteQueen,
                        [PieceType.King] = ChessForge.Pieces.WhiteKing,
                        [PieceType.Pawn] = ChessForge.Pieces.WhitePawn
                    };
                    _dictBlackPiecesSmall = new Dictionary<PieceType, BitmapImage>()
                    {
                        [PieceType.Rook] = ChessForge.Pieces.BlackRook,
                        [PieceType.Bishop] = ChessForge.Pieces.BlackBishop,
                        [PieceType.Knight] = ChessForge.Pieces.BlackKnight,
                        [PieceType.Queen] = ChessForge.Pieces.BlackQueen,
                        [PieceType.King] = ChessForge.Pieces.BlackKing,
                        [PieceType.Pawn] = ChessForge.Pieces.BlackPawn
                    };
                    break;
            }
        }

    }
}

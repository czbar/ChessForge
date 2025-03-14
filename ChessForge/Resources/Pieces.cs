using ChessPosition;
using System;
using System.Windows.Media.Imaging;

namespace ChessForge
{
    public class Pieces
    {
        public static BitmapImage EmptySquare = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/EmptySquare.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage WhiteKing = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhiteKing.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage WhiteQueen = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhiteQueen.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage WhiteBishop = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhiteBishop.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage WhiteKnight = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhiteKnight.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage WhiteRook = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhiteRook.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage WhitePawn = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhitePawn.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage BlackKing = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackKing.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlackQueen = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackQueen.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlackBishop = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackBishop.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlackKnight = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackKnight.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlackRook = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackRook.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlackPawn = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackPawn.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage WhiteKingSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhiteKingSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage WhiteQueenSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhiteQueenSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage WhiteBishopSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhiteBishopSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage WhiteKnightSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhiteKnightSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage WhiteRookSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhiteRookSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage WhitePawnSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhitePawnSmall.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage BlackKingSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackKingSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlackQueenSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackQueenSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlackBishopSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackBishopSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlackKnightSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackKnightSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlackRookSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackRookSmall.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlackPawnSmall = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackPawnSmall.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage YellowOverlay = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/YellowOverlay.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage WhitePromo = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhitePromo.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage WhitePromoInverted = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/WhitePromoInverted.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage BlackPromo = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackPromo.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage BlackPromoInverted = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/BlackPromoInverted.png", UriKind.RelativeOrAbsolute));

        /// <summary>
        /// Returns image for a piece of a given type and color
        /// </summary>
        /// <param name="piece"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static BitmapImage GetImageForPiece(PieceType piece, PieceColor color)
        {
            if (color == PieceColor.White)
            {
                return GetImageForPieceWhite(piece);
            }
            else if (color == PieceColor.Black)
            {
                return GetImageForPieceBlack(piece);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns image for a White piece of a given type
        /// </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        private static BitmapImage GetImageForPieceWhite(PieceType piece)
        {
            switch (piece)
            {
                case PieceType.King:
                    return WhiteKing;
                case PieceType.Queen:
                    return WhiteQueen;
                case PieceType.Rook:
                    return WhiteRook;
                case PieceType.Bishop:
                    return WhiteBishop;
                case PieceType.Knight:
                    return WhiteKnight;
                case PieceType.Pawn:
                    return WhitePawn;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns image for a Black piece of a given type
        /// </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        private static BitmapImage GetImageForPieceBlack(PieceType piece)
        {
            switch (piece)
            {
                case PieceType.King:
                    return BlackKing;
                case PieceType.Queen:
                    return BlackQueen;
                case PieceType.Rook:
                    return BlackRook;
                case PieceType.Bishop:
                    return BlackBishop;
                case PieceType.Knight:
                    return BlackKnight;
                case PieceType.Pawn:
                    return BlackPawn;
                default:
                    return null;
            }
        }


        /// <summary>
        /// Returns image for a "small" piece of a given type and color
        /// </summary>
        /// <param name="piece"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static BitmapImage GetImageForPieceSmall(PieceType piece, PieceColor color)
        {
            if (color == PieceColor.White)
            {
                return GetImageForPieceSmallWhite(piece);
            }
            else if (color == PieceColor.Black)
            {
                return GetImageForPieceSmallBlack(piece);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns image for a "small" White piece of a given type
        /// </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        private static BitmapImage GetImageForPieceSmallWhite(PieceType piece)
        {
            switch (piece)
            {
                case PieceType.King:
                    return WhiteKingSmall;
                case PieceType.Queen:
                    return WhiteQueenSmall;
                case PieceType.Rook:
                    return WhiteRookSmall;
                case PieceType.Bishop:
                    return WhiteBishopSmall;
                case PieceType.Knight:
                    return WhiteKnightSmall;
                case PieceType.Pawn:
                    return WhitePawnSmall;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns image for a "small" Black piece of a given type
        /// </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        private static BitmapImage GetImageForPieceSmallBlack(PieceType piece)
        {
            switch (piece)
            {
                case PieceType.King:
                    return BlackKingSmall;
                case PieceType.Queen:
                    return BlackQueenSmall;
                case PieceType.Rook:
                    return BlackRookSmall;
                case PieceType.Bishop:
                    return BlackBishopSmall;
                case PieceType.Knight:
                    return BlackKnightSmall;
                case PieceType.Pawn:
                    return BlackPawnSmall;
                default:
                    return null;
            }
        }
    }
}

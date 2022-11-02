using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessPosition.Utils;
using GameTree;

namespace ChessPosition
{
    /// <summary>
    /// A set of utilities for handling various aspects of the position,
    /// translating between notations etc.
    /// </summary>
    public class PositionUtils
    {
        /// <summary>
        /// Global definiton to enable more random number generation.
        /// </summary>
        public static Random GlobalRnd = new Random();

        /// <summary>
        /// Checks validity of a position.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="errorText"></param>
        /// <returns></returns>
        public static bool ValidatePosition(ref BoardPosition pos, out string errorText)
        {
            StringBuilder sb = new StringBuilder();

            bool result = true;

            KingCount(out int whiteKings, out int blackKings, pos);
            if (whiteKings != 1 || blackKings != 1)
            {
                result = false;
                if (whiteKings > 1)
                {
                    sb.AppendLine("Too many White Kings");
                }
                else if (whiteKings == 0)
                {
                    sb.AppendLine("The White King is missing.");
                }

                if (blackKings > 1)
                {
                    sb.AppendLine("Too many Black Kings");
                }
                else if (blackKings == 0)
                {
                    sb.AppendLine("The Black King is missing.");
                }
            }

            // only check if we know we have 1 king each side (otherwise we may get an exception)
            if (result == true)
            {
                if (pos.ColorToMove == PieceColor.White && IsKingInCheck(pos, PieceColor.Black))
                {
                    result = false;
                    sb.AppendLine("Black King cannot be in check on White\'s move");
                }
                if (pos.ColorToMove == PieceColor.Black && IsKingInCheck(pos, PieceColor.White))
                {
                    result = false;
                    sb.AppendLine("White King cannot be in check on Black\'s move");
                }
            }

            // remove any incorrect castling rights if we are good so far
            if (result)
            {
                CorrectCastlingRights(ref pos);
            }

            errorText = sb.ToString();
            return result;
        }

        /// <summary>
        /// Removes castling rights, if they conflict with the king or rook positions. 
        /// </summary>
        /// <param name="pos"></param>
        private static void CorrectCastlingRights(ref BoardPosition pos)
        {
            // Remove rights based on Kings' positions
            if (GetPieceType(pos.Board[4, 0]) != PieceType.King || GetPieceColor(pos.Board[4, 0]) != PieceColor.White)
            {
                pos.DynamicProperties = (byte)(pos.DynamicProperties & ~(Constants.WhiteKingsideCastle | Constants.WhiteQueensideCastle));
            }
            if (GetPieceType(pos.Board[4, 7]) != PieceType.King || GetPieceColor(pos.Board[4, 7]) != PieceColor.Black)
            {
                pos.DynamicProperties = (byte)(pos.DynamicProperties & ~(Constants.BlackKingsideCastle | Constants.BlackQueensideCastle));
            }

            // Remove rights based on White Rook's positions
            if (GetPieceType(pos.Board[0, 0]) != PieceType.Rook || GetPieceColor(pos.Board[0, 0]) != PieceColor.White)
            {
                pos.DynamicProperties = (byte)(pos.DynamicProperties & ~Constants.WhiteQueensideCastle);
            }
            if (GetPieceType(pos.Board[7, 0]) != PieceType.Rook || GetPieceColor(pos.Board[7, 0]) != PieceColor.White)
            {
                pos.DynamicProperties = (byte)(pos.DynamicProperties & ~Constants.WhiteKingsideCastle);
            }

            // Remove rights based on Black Rook's positions
            if (GetPieceType(pos.Board[0, 7]) != PieceType.Rook || GetPieceColor(pos.Board[0, 7]) != PieceColor.Black)
            {
                pos.DynamicProperties = (byte)(pos.DynamicProperties & ~Constants.BlackQueensideCastle);
            }
            if (GetPieceType(pos.Board[7, 7]) != PieceType.Rook || GetPieceColor(pos.Board[7, 7]) != PieceColor.Black)
            {
                pos.DynamicProperties = (byte)(pos.DynamicProperties & ~Constants.BlackKingsideCastle);
            }
        }

        /// <summary>
        /// Gets the number of Kings in a given position.
        /// </summary>
        /// <param name="whiteKings"></param>
        /// <param name="blackKings"></param>
        /// <param name="pos"></param>
        private static void KingCount(out int whiteKings, out int blackKings, BoardPosition pos)
        {
            whiteKings = 0;
            blackKings = 0;

            foreach (byte square in pos.Board)
            {
                if (GetPieceType(square) == PieceType.King)
                {
                    if (GetPieceColor(square) == PieceColor.White)
                    {
                        whiteKings++;
                    }
                    else if (GetPieceColor(square) == PieceColor.Black)
                    {
                        blackKings++;
                    }
                }
            }
        }

        /// <summary>
        /// Builds the starting position.
        /// </summary>
        /// <returns>BoardPosition object with the starting position.</returns>
        public static BoardPosition SetupStartingPosition()
        {
            BoardPosition pos = new BoardPosition();

            pos.MoveNumber = 0;
            pos.HalfMove50Clock = 0;
            pos.DynamicProperties = Constants.WhiteKingsideCastle | Constants.WhiteQueensideCastle | Constants.BlackKingsideCastle | Constants.BlackQueensideCastle
                    | Constants.Color;

            pos.Board[0, 0] = (byte)(Constants.PieceToFlag[PieceType.Rook] | Constants.Color);
            pos.Board[1, 0] = (byte)(Constants.PieceToFlag[PieceType.Knight] | Constants.Color);
            pos.Board[2, 0] = (byte)(Constants.PieceToFlag[PieceType.Bishop] | Constants.Color);
            pos.Board[3, 0] = (byte)(Constants.PieceToFlag[PieceType.Queen] | Constants.Color);
            pos.Board[4, 0] = (byte)(Constants.PieceToFlag[PieceType.King] | Constants.Color);
            pos.Board[5, 0] = (byte)(Constants.PieceToFlag[PieceType.Bishop] | Constants.Color);
            pos.Board[6, 0] = (byte)(Constants.PieceToFlag[PieceType.Knight] | Constants.Color);
            pos.Board[7, 0] = (byte)(Constants.PieceToFlag[PieceType.Rook] | Constants.Color);

            pos.Board[0, 1] = (byte)(Constants.PieceToFlag[PieceType.Pawn] | Constants.Color);
            pos.Board[1, 1] = (byte)(Constants.PieceToFlag[PieceType.Pawn] | Constants.Color);
            pos.Board[2, 1] = (byte)(Constants.PieceToFlag[PieceType.Pawn] | Constants.Color);
            pos.Board[3, 1] = (byte)(Constants.PieceToFlag[PieceType.Pawn] | Constants.Color);
            pos.Board[4, 1] = (byte)(Constants.PieceToFlag[PieceType.Pawn] | Constants.Color);
            pos.Board[5, 1] = (byte)(Constants.PieceToFlag[PieceType.Pawn] | Constants.Color);
            pos.Board[6, 1] = (byte)(Constants.PieceToFlag[PieceType.Pawn] | Constants.Color);
            pos.Board[7, 1] = (byte)(Constants.PieceToFlag[PieceType.Pawn] | Constants.Color);

            pos.Board[0, 6] = (byte)Constants.PieceToFlag[PieceType.Pawn];
            pos.Board[1, 6] = (byte)Constants.PieceToFlag[PieceType.Pawn];
            pos.Board[2, 6] = (byte)Constants.PieceToFlag[PieceType.Pawn];
            pos.Board[3, 6] = (byte)Constants.PieceToFlag[PieceType.Pawn];
            pos.Board[4, 6] = (byte)Constants.PieceToFlag[PieceType.Pawn];
            pos.Board[5, 6] = (byte)Constants.PieceToFlag[PieceType.Pawn];
            pos.Board[6, 6] = (byte)Constants.PieceToFlag[PieceType.Pawn];
            pos.Board[7, 6] = (byte)Constants.PieceToFlag[PieceType.Pawn];

            pos.Board[0, 7] = (byte)Constants.PieceToFlag[PieceType.Rook];
            pos.Board[1, 7] = (byte)Constants.PieceToFlag[PieceType.Knight];
            pos.Board[2, 7] = (byte)Constants.PieceToFlag[PieceType.Bishop];
            pos.Board[3, 7] = (byte)Constants.PieceToFlag[PieceType.Queen];
            pos.Board[4, 7] = (byte)Constants.PieceToFlag[PieceType.King];
            pos.Board[5, 7] = (byte)Constants.PieceToFlag[PieceType.Bishop];
            pos.Board[6, 7] = (byte)Constants.PieceToFlag[PieceType.Knight];
            pos.Board[7, 7] = (byte)Constants.PieceToFlag[PieceType.Rook];

            return pos;
        }


        /// <summary>
        /// Places a piece of a specified type
        /// and color on the board.
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="col"></param>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        /// <param name="pos"></param>
        public static void PlacePieceOnBoard(PieceType pt, PieceColor col, byte xPos, byte yPos, ref byte[,] board)
        {
            board[xPos, yPos] =
                (byte)(Constants.PieceToFlag[pt] | (col == PieceColor.White ? Constants.Color : 0));
        }

        /// <summary>
        /// Clears a specified square on the board
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        /// <param name="board"></param>
        public static void ClearSquare(byte xPos, byte yPos, ref byte[,] board)
        {
            board[xPos, yPos] = 0;
        }

        /// <summary>
        /// Removes all pieces from the baord.
        /// </summary>
        /// <param name="board"></param>
        public static void ClearPosition(ref byte[,] board)
        {
            for (int x = 0; x <= 7; x++)
            {
                for (int y = 0; y <= 7; y++)
                {
                    board[x, y] = 0;
                }
            }
        }

        /// <summary>
        /// Move a piece from one square to another.
        /// Does not perform checks of any kind.
        /// The caller must perform all such checks
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="dest"></param>
        /// <param name="board"></param>
        public static void MovePiece(SquareCoords orig, SquareCoords dest, ref byte[,] board)
        {
            board[dest.Xcoord, dest.Ycoord] = board[orig.Xcoord, orig.Ycoord];
            board[orig.Xcoord, orig.Ycoord] = 0;
        }

        /// <summary>
        /// Finds the king of a given color on the board.
        /// Assumes there is only one king of a given color so exits as
        /// soon as found.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public static SquareCoords GetKingPosition(ref BoardPosition pos, PieceColor col)
        {
            for (int x = 0; x <= 7; x++)
            {
                for (int y = 0; y <= 7; y++)
                {
                    if (GetPieceType(pos.Board[x, y]) == PieceType.King && GetPieceColor(pos.Board[x, y]) == col)
                    {
                        return new SquareCoords(x, y);
                    }
                }
            }

            throw new Exception("No King in the position");
        }

        /// <summary>
        /// Return the type of the piece encoded in the square's byte value. 
        /// </summary>
        /// <param name="square"></param>
        /// <returns></returns>
        public static PieceType GetPieceType(byte square)
        {
            return Constants.FlagToPiece[(byte)(square & 0x7F)];
        }

        /// <summary>
        /// Returns the color of the piece on a given square.
        /// </summary>
        /// <param name="square"></param>
        /// <returns></returns>
        public static PieceColor GetPieceColor(byte square)
        {
            if (square == 0)
            {
                return PieceColor.None;
            }

            return ((square & Constants.Color) != 0) ? PieceColor.White : PieceColor.Black;
        }


        /// <summary>
        /// Converts algebraic notation (FEN/PGN) of a square 
        /// to xy coordinates e.g. "a1" => 0,0; "h8" => 7,7
        /// </summary>
        /// <param name="alg"></param>
        /// <returns></returns>
        public static SquareCoords ConvertAlgebraicToXY(string alg)
        {
            try
            {
                int xpos = alg[0] - 'a';
                int ypos = alg[1] - '1';

                if (!AreValidCoordinates(xpos, ypos))
                {
                    throw new Exception("Invalid algebraic square notation");
                }
                return new SquareCoords(xpos, ypos);
            }
            catch
            {
                throw new Exception("Invalid algebraic square notation");
            }
        }

        /// <summary>
        /// Converts square coordinates to the algebraic (FEN/PGN)
        /// notation, e.g. 0,0 => "a1"; 7,7 => "h8"
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static string ConvertXYtoAlgebraic(int x, int y)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append((char)('a' + x));
            sb.Append((char)('1' + y));
            return sb.ToString();
        }

        /// <summary>
        /// Checks if passed coordinates are valid
        /// chessboard square coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool AreValidCoordinates(int x, int y)
        {
            if (x >= 0 && x <= 7 && y >= 0 && y <= 7)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if passed coordinates are valid
        /// chessboard square coordinates
        /// </summary>
        /// <param name="sq"></param>
        /// <returns></returns>
        public static bool AreValidCoordinates(SquareCoords sq)
        {
            return AreValidCoordinates(sq.Xcoord, sq.Ycoord);
        }


        /// <summary>
        /// Returns the color of the side that is on move.
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public static PieceColor SideToMove(BoardPosition board)
        {
            return (board.DynamicProperties & 0x80) != 0 ? PieceColor.White : PieceColor.Black;
        }

        /// <summary>
        /// Gets rank number from the perspective of 
        /// the specified color.
        /// E.g. rank number 2 is y=1 for White and y=6 for Black.
        /// This is used, for example, when we check if a pawn is
        /// on the second rank and therefore can move 2 squares up.
        /// </summary>
        /// <param name="yCoord"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static int GetRankNo(int yCoord, PieceColor color)
        {
            return color == PieceColor.White ? (yCoord + 1) : (8 - yCoord);
        }

        /// <summary>
        /// Gets Y cooridnate from the rank number.
        /// E.g. rank "1" for Black translate to Y position of 7.
        /// </summary>
        /// <param name="rank"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public static int GetYposFromRankNo(int rank, PieceColor col)
        {
            return col == PieceColor.White ? (rank - 1) : (8 - rank);
        }

        /// <summary>
        /// Gets file number in the range 1-8.
        /// Note that unlike rank no it does not
        /// depend on the side's color.
        /// </summary>
        /// <param name="xPos"></param>
        /// <returns></returns>
        public static int GetFileNo(int xPos)
        {
            return xPos + 1;
        }

        /// <summary>
        /// If the move is made by a pawn 
        /// to the en passant square,
        /// we know this is an en passant capture.
        /// (nothing else needs checking).
        /// </summary>
        /// <param name="position"></param>
        /// <param name="move"></param>
        /// <returns></returns>
        public static bool IsEnPassantCapture(BoardPosition position, MoveData move)
        {
            if (position.InheritedEnPassantSquare == (byte)(move.Destination.Xcoord << 4 | move.Destination.Ycoord) && move.MovingPiece == PieceType.Pawn)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Converts xy coords encoded into a byte 
        /// to a SquareCoords object.
        /// </summary>
        /// <param name="square"></param>
        /// <returns></returns>
        public static SquareCoords DecodeEnPassantSquare(byte square)
        {
            int x = square >> 4;
            int y = square & 0x000F;

            return new SquareCoords(x, y);
        }

        /// <summary>
        /// Checks if the position is legal in the sense of the King not being under attack.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="kingColorToCheck"></param>
        /// <returns></returns>
        public static bool IsKingSafe(BoardPosition pos, PieceColor kingColorToCheck)
        {
            SquareCoords square = GetKingPosition(ref pos, kingColorToCheck);
            // we are looking for king's attacker so change the color to the opposite of the king
            PiecesTargetingSquare sa = new PiecesTargetingSquare((byte)square.Xcoord, (byte)square.Ycoord, -1, -1,
               MoveUtils.ReverseColor(kingColorToCheck), ref pos, PieceType.None, true);

            return sa.Candidates.Count == 0;
        }

        /// <summary>
        /// Returns true if King of the given color is in check.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="kingColorToCheck"></param>
        /// <returns></returns>
        public static bool IsKingInCheck(BoardPosition pos, PieceColor kingColorToCheck)
        {
            pos.IsCheck = !IsKingSafe(pos, kingColorToCheck);
            return pos.IsCheck;
        }

        /// <summary>
        /// Determines if the passed position is checkmate.
        /// </summary>
        /// <returns></returns>
        public static bool IsCheckmate(BoardPosition pos)
        {
            // we are checking of the ColorToMove side is checkmates
            if (IsKingInCheck(pos, pos.ColorToMove))
            {
                var lst = PieceMoves.GetLegalMoves(pos.ColorToMove, pos, true);
                if (lst.Count == 0)
                {
                    pos.IsCheckmate = true;
                    return true;
                }
            }

            pos.IsCheckmate = false;
            return false;
        }


        /// <summary>
        /// Determines if the passed position is stalemate.
        /// </summary>
        /// <returns></returns>
        public static bool IsStalemate(BoardPosition pos)
        {
            // we are checking of the ColorToMove side is checkmates
            if (!IsKingInCheck(pos, pos.ColorToMove))
            {
                var lst = PieceMoves.GetLegalMoves(pos.ColorToMove, pos, true);
                if (lst.Count == 0)
                {
                    pos.IsStalemate = true;
                    return true;
                }
            }

            pos.IsStalemate = false;
            return false;
        }

        /// <summary>
        /// For castling to be legal, the king's origin, target, and the square between, cannot be attacked
        /// and all the squares between the king and the rook must be empty.
        /// </summary>
        /// <param name="castlingType"></param>
        /// <param name="col"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static bool IsCastlingLegal(byte castlingType, PieceColor col, BoardPosition pos)
        {
            if ((pos.DynamicProperties & castlingType) == 0)
                return false;

            // check that relevant squares are not under attack
            List<SquareCoords> squaresToCheck = new List<SquareCoords>();

            switch (castlingType)
            {
                case Constants.WhiteKingsideCastle:
                    squaresToCheck.Add(new SquareCoords(4, 0));
                    squaresToCheck.Add(new SquareCoords(5, 0));
                    squaresToCheck.Add(new SquareCoords(6, 0));
                    break;
                case Constants.WhiteQueensideCastle:
                    squaresToCheck.Add(new SquareCoords(4, 0));
                    squaresToCheck.Add(new SquareCoords(3, 0));
                    squaresToCheck.Add(new SquareCoords(2, 0));
                    break;
                case Constants.BlackKingsideCastle:
                    squaresToCheck.Add(new SquareCoords(4, 7));
                    squaresToCheck.Add(new SquareCoords(5, 7));
                    squaresToCheck.Add(new SquareCoords(6, 7));
                    break;
                case Constants.BlackQueensideCastle:
                    squaresToCheck.Add(new SquareCoords(4, 7));
                    squaresToCheck.Add(new SquareCoords(3, 7));
                    squaresToCheck.Add(new SquareCoords(2, 7));
                    break;
            }

            foreach (SquareCoords sc in squaresToCheck)
            {
                PiecesTargetingSquare sa = new PiecesTargetingSquare((byte)sc.Xcoord, (byte)sc.Ycoord, -1, -1, MoveUtils.ReverseColor(col), ref pos, PieceType.None, true);
                if (sa.Candidates.Count > 0)
                {
                    return false;
                }
            }

            // check that relevant squares are not occupied
            List<SquareCoords> emptySquares = new List<SquareCoords>();

            switch (castlingType)
            {
                case Constants.WhiteKingsideCastle:
                    squaresToCheck.Add(new SquareCoords(5, 0));
                    squaresToCheck.Add(new SquareCoords(6, 0));
                    break;
                case Constants.WhiteQueensideCastle:
                    squaresToCheck.Add(new SquareCoords(3, 0));
                    squaresToCheck.Add(new SquareCoords(2, 0));
                    squaresToCheck.Add(new SquareCoords(1, 0));
                    break;
                case Constants.BlackKingsideCastle:
                    squaresToCheck.Add(new SquareCoords(5, 7));
                    squaresToCheck.Add(new SquareCoords(6, 7));
                    break;
                case Constants.BlackQueensideCastle:
                    squaresToCheck.Add(new SquareCoords(3, 7));
                    squaresToCheck.Add(new SquareCoords(2, 7));
                    squaresToCheck.Add(new SquareCoords(1, 7));
                    break;
            }

            foreach (SquareCoords sc in emptySquares)
            {
                if (pos.Board[sc.Xcoord, sc.Ycoord] != 0)
                {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// If the move is castling, depending on the forSideMove value
        /// the side on the move on the opposite who just moved
        /// loses all castling rights.
        /// The pgn parser will call it for the move who moved previous
        /// while the engine line anlyser will call it for the side
        /// on move in the current position.
        /// Similarly, if it is a king move.
        /// If it was a rook moving from its initial position
        /// the right to castle in its direction will be removed.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="move"></param>
        public static void UpdateCastlingRights(ref BoardPosition pos, MoveData move, bool forSideOnMove)
        {
            PieceColor colorToProcess = forSideOnMove ? pos.ColorToMove : MoveUtils.ReverseColor(pos.ColorToMove);

            // if there are no castling rights left,
            // no need to process
            if (HasAnyCastlingRights(colorToProcess, ref pos))
            {

                // if king moved, the side loses all castling rights
                if (move.MovingPiece == PieceType.King || move.CastlingType != 0)
                {
                    RemoveAllCastlingRights(colorToProcess, ref pos);
                }
                else if (move.MovingPiece == PieceType.Rook)
                {
                    // the right to kingside or queenside castling
                    // will be lost, depending on which rook moved
                    UpdateCastlingRightsForRook(colorToProcess, move.Origin.Xcoord, move.Origin.Ycoord, ref pos);
                }
                else
                {
                    // a rook was captured so the side not-on-move is losing the castling right
                    UpdateCastlingRightsForRook(colorToProcess == PieceColor.White ? PieceColor.Black : PieceColor.White, move.Destination.Xcoord, move.Destination.Ycoord, ref pos);
                }
            }
        }

        /// <summary>
        /// Checks if the piece defined by the passed
        /// byte value describes an opposition piece i.e. a piece
        /// of the color opposite to "our color".
        /// </summary>
        /// <param name="square"></param>
        /// <returns></returns>
        public static bool IsOurPiece(byte square, PieceColor ourColor)
        {
            if (square == 0)
                return false;

            if (((square & Constants.Color) != 0) && ourColor == PieceColor.White
               ||
               ((square & Constants.Color) == 0) && ourColor == PieceColor.Black)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the piece defined by the passed
        /// byte value describes an opposition piece i.e. a piece
        /// of the color opposite to "our color".
        /// </summary>
        /// <param name="square"></param>
        /// <returns></returns>
        public static bool IsOppositionPiece(byte square, PieceColor ourColor)
        {
            if (square == 0)
                return false;

            if (((square & Constants.Color) == 0) && ourColor == PieceColor.White
               ||
               ((square & Constants.Color) != 0) && ourColor == PieceColor.Black)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// This method will be called after a rook's move.
        /// If the rook was on its original square, it will
        /// invalidate one of the castling rights.
        /// If the rool was not on its original square, it means
        /// that the relevant right must have been removed previously.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="xpos"></param>
        /// <param name="ypos"></param>
        /// <param name="pos"></param>
        private static void UpdateCastlingRightsForRook(PieceColor col, int xpos, int ypos, ref BoardPosition pos)
        {
            if (col == PieceColor.White)
            {
                if (xpos == 0 && ypos == 0)
                {
                    pos.DynamicProperties = (byte)(pos.DynamicProperties & ~Constants.WhiteQueensideCastle);
                }
                else if (xpos == 7 && ypos == 0)
                {
                    pos.DynamicProperties = (byte)(pos.DynamicProperties & ~Constants.WhiteKingsideCastle);
                }
            }
            else
            {
                if (xpos == 0 && ypos == 7)
                {
                    pos.DynamicProperties = (byte)(pos.DynamicProperties & ~Constants.BlackQueensideCastle);
                }
                else if (xpos == 7 && ypos == 7)
                {
                    pos.DynamicProperties = (byte)(pos.DynamicProperties & ~Constants.BlackKingsideCastle);
                }
            }
        }

        /// <summary>
        /// Clears all castling rights flags.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="pos"></param>
        private static void RemoveAllCastlingRights(PieceColor col, ref BoardPosition pos)
        {
            if (col == PieceColor.White)
            {
                pos.DynamicProperties = (byte)(pos.DynamicProperties & ~(Constants.WhiteKingsideCastle | Constants.WhiteQueensideCastle));
            }
            else
            {
                pos.DynamicProperties = (byte)(pos.DynamicProperties & ~(Constants.BlackKingsideCastle | Constants.BlackQueensideCastle));
            }
        }

        /// <summary>
        /// Checks if a given side has any castling rights left.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static bool HasAnyCastlingRights(PieceColor col, ref BoardPosition pos)
        {
            if (col == PieceColor.White && (pos.DynamicProperties & (Constants.WhiteKingsideCastle | Constants.WhiteQueensideCastle)) == 0)
            {
                return false;
            }
            if (col == PieceColor.Black && (pos.DynamicProperties & (Constants.BlackKingsideCastle | Constants.BlackQueensideCastle)) == 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sets the castling rights according to the boolean values passed.
        /// </summary>
        /// <param name="whiteKing"></param>
        /// <param name="whiteQueen"></param>
        /// <param name="blackKing"></param>
        /// <param name="blackQueen"></param>
        /// <param name="pos"></param>
        public static void SetCastlingRights(bool whiteKing, bool whiteQueen, bool blackKing, bool blackQueen, ref BoardPosition pos)
        {
            RemoveAllCastlingRights(PieceColor.White, ref pos);
            RemoveAllCastlingRights(PieceColor.Black, ref pos);

            if (whiteKing)
            {
                pos.DynamicProperties = (byte)(pos.DynamicProperties |= Constants.WhiteKingsideCastle);
            }
            if (whiteQueen)
            {
                pos.DynamicProperties = (byte)(pos.DynamicProperties |= Constants.WhiteQueensideCastle);
            }
            if (blackKing)
            {
                pos.DynamicProperties = (byte)(pos.DynamicProperties |= Constants.BlackKingsideCastle);
            }
            if (blackQueen)
            {
                pos.DynamicProperties = (byte)(pos.DynamicProperties |= Constants.BlackQueensideCastle);
            }
        }

        /// <summary>
        /// If a pawn moved by 2 squares, we need to set the EnPassant square
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="move"></param>
        public static void SetEnpassantSquare(ref BoardPosition pos, MoveData move)
        {
            if (move.MovingPiece == PieceType.Pawn && Math.Abs(move.Destination.Ycoord - move.Origin.Ycoord) == 2)
            {
                pos.EnPassantSquare = (byte)((move.Origin.Xcoord << 4) | GetYposFromRankNo(3, move.Color == PieceColor.White ? PieceColor.White : PieceColor.Black));
            }
        }

        /// <summary>
        /// Builds a list of MoveWithEval objects from the list of Nodes.
        /// This is useful e.g. when building a ScoreSheet object.
        /// Must handle the case when the first move is Black's.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static ObservableCollection<MoveWithEval> BuildMoveListFromLine(ObservableCollection<TreeNode> line)
        {
            var game = new ObservableCollection<MoveWithEval>();

            int i = 1;

            if (line.Count > 1)
            {
                // handle the first node first as it could be a White move or a Black move
                //  (e.g. in exercise starting from a black move

                MoveWithEval move = new MoveWithEval();
                TreeNode node = line[1];
                if (node.ColorToMove == PieceColor.White)
                {
                    move.Number = node.Position.MoveNumber.ToString() + ".";
                    SetWhiteMoveInMoveList(null, ref move);
                    SetBlackMoveInMoveList(node, ref move);

                    game.Add(move);
                    i++;
                }
            }

            while (i < line.Count)
            {
                MoveWithEval move = new MoveWithEval();

                TreeNode whiteNode = line[i];
                move.Number = whiteNode.Position.MoveNumber.ToString() + ".";
                SetWhiteMoveInMoveList(whiteNode, ref move);

                if (i + 1 < line.Count)
                {
                    TreeNode blackNode = line[i + 1];
                    SetBlackMoveInMoveList(blackNode, ref move);
                }

                game.Add(move);

                i += 2;
            }

            return game;
        }

        /// <summary>
        /// Sets White's move data in the MoveWithEval object.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="move"></param>
        private static void SetWhiteMoveInMoveList(TreeNode node, ref MoveWithEval move)
        {
            if (node != null)
            {
                move.WhitePly = node.GetPlyText(true);
                move.WhiteEval = node.EngineEvaluation;
                move.WhiteNodeId = node.NodeId;
            }
            else
            {
                move.WhitePly = "";
                move.WhiteEval = "";
                move.WhiteNodeId = -1;
            }
        }

        /// <summary>
        /// Sets Black's move data in the MoveWithEval object.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="move"></param>
        private static void SetBlackMoveInMoveList(TreeNode node, ref MoveWithEval move)
        {
            if (node != null)
            {
                move.BlackPly = node.GetPlyText(true);
                move.BlackEval = node.EngineEvaluation;
                move.BlackNodeId = node.NodeId;
            }
            else
            {
                move.BlackPly = "";
                move.BlackEval = "";
                move.BlackNodeId = -1;
            }
        }

    }
}

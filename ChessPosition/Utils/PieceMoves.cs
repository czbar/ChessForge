using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessPosition.Utils
{
    /// <summary>
    /// Examines all possible moves per piece type, color and position.
    /// </summary>
    public class PieceMoves
    {
        /// <summary>
        /// Finds all legal moves by a piece found on a specified square 
        /// and of a specified color.
        /// If stopAtFirstLegal is true, the function will return once the first legal 
        /// move has been found.  This is useful e.g. when checking if the position is
        /// a checkmate or a stalemate.
        /// </summary>
        /// <param name="piece"></param>
        /// <param name="color"></param>
        /// <param name="position"></param>
        /// <param name="stopAtFirstLegal"></param>
        /// <returns></returns>
        public static List<MoveOrigDest> GetLegalMoves(SquareCoords sq, PieceColor color, BoardPosition position, bool stopAtFirstLegal)
        {
            List<MoveOrigDest> moves = new List<MoveOrigDest>();

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (PositionUtils.GetPieceColor(position.Board[x, y]) == color)
                    {
                        PieceType pieceType = PositionUtils.GetPieceType(position.Board[x, y]);
                        switch (pieceType)
                        {
                            case PieceType.King:
                                GetLegalKingMoves(sq, color, position, stopAtFirstLegal, ref moves);
                                break;
                            case PieceType.Queen:
                                GetLegalQueenMoves(sq, color, position, stopAtFirstLegal, ref moves);
                                break;
                            case PieceType.Rook:
                                GetLegalRookMoves(sq, color, position, stopAtFirstLegal, ref moves);
                                break;
                            case PieceType.Bishop:
                                GetLegalBishopMoves(sq, color, position, stopAtFirstLegal, ref moves);
                                break;
                            case PieceType.Knight:
                                GetLegalKnightMoves(sq, color, position, stopAtFirstLegal, ref moves);
                                break;
                            case PieceType.Pawn:
                                GetLegalPawnMoves(sq, color, position, stopAtFirstLegal, ref moves);
                                break;
                        }

                        if (moves.Count > 0 && stopAtFirstLegal)
                            break;
                    }
                }
            }

            return moves;
        }

        /// <summary>
        /// For rooks, we need to check 4 directions: horizontal left and right, vertical up and down.
        /// </summary>
        /// <param name="sq"></param>
        /// <param name="color"></param>
        /// <param name="position"></param>
        /// <param name="stopAtFirstLegal"></param>
        /// <param name="moves"></param>
        /// <returns></returns>
        public static void GetLegalRookMoves(SquareCoords sq, PieceColor color, BoardPosition position, bool stopAtFirstLegal, ref List<MoveOrigDest> moves)
        {
            GetLegalRookSingleDirectionMoves(0, 1, sq, color, position, stopAtFirstLegal, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal) return;

            GetLegalRookSingleDirectionMoves(1, 0, sq, color, position, stopAtFirstLegal, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal) return;

            GetLegalRookSingleDirectionMoves(0, -1, sq, color, position, stopAtFirstLegal, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal) return;

            GetLegalRookSingleDirectionMoves(-1, 0, sq, color, position, stopAtFirstLegal, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal) return;
        }

        /// <summary>
        /// For bishops we need to check diagonals. 
        /// </summary>
        /// <param name="sq"></param>
        /// <param name="color"></param>
        /// <param name="position"></param>
        /// <param name="stopAtFirstLegal"></param>
        /// <param name="moves"></param>
        /// <returns></returns>
        public static void GetLegalBishopMoves(SquareCoords sq, PieceColor color, BoardPosition position, bool stopAtFirstLegal, ref List<MoveOrigDest> moves)
        {
            GetLegalBishopSingleDirectionMoves(1, 1, sq, color, position, stopAtFirstLegal, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal) return;

            GetLegalBishopSingleDirectionMoves(1, -1, sq, color, position, stopAtFirstLegal, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal) return;

            GetLegalBishopSingleDirectionMoves(-1, 1, sq, color, position, stopAtFirstLegal, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal) return;

            GetLegalBishopSingleDirectionMoves(-1, -1, sq, color, position, stopAtFirstLegal, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal) return;
        }

        /// <summary>
        /// For queens, we simply check the combination of Rook and Bishop moves
        /// which constitutes a set of Queen moves.
        /// </summary>
        /// <param name="sq"></param>
        /// <param name="color"></param>
        /// <param name="position"></param>
        /// <param name="stopAtFirstLegal"></param>
        /// <param name="moves"></param>
        /// <returns></returns>
        public static void GetLegalQueenMoves(SquareCoords sq, PieceColor color, BoardPosition position, bool stopAtFirstLegal, ref List<MoveOrigDest> moves)
        {
            GetLegalRookMoves(sq, color, position, stopAtFirstLegal, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal)
                return;

            GetLegalBishopMoves(sq, color, position, stopAtFirstLegal, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal)
                return;
        }


        /// <summary>
        /// For knights we need to check all 8 possible jumps. 
        /// </summary>
        /// <param name="sq"></param>
        /// <param name="color"></param>
        /// <param name="position"></param>
        /// <param name="stopAtFirstLegal"></param>
        /// <param name="moves"></param>
        /// <returns></returns>
        public static void GetLegalKnightMoves(SquareCoords sq, PieceColor color, BoardPosition position, bool stopAtFirstLegal, ref List<MoveOrigDest> moves)
        {
            GetLegalKnightSingleDirectionMoves(2, 1, sq, color, position, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal) return;

            GetLegalKnightSingleDirectionMoves(2, -1, sq, color, position, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal) return;

            GetLegalKnightSingleDirectionMoves(-2, 1, sq, color, position, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal) return;

            GetLegalKnightSingleDirectionMoves(-2, -1, sq, color, position, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal) return;

            GetLegalKnightSingleDirectionMoves(1, 2, sq, color, position, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal) return;

            GetLegalKnightSingleDirectionMoves(1, -2, sq, color, position, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal) return;

            GetLegalKnightSingleDirectionMoves(-1, 2, sq, color, position, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal) return;

            GetLegalKnightSingleDirectionMoves(-1, -2, sq, color, position, ref moves);
            if (moves.Count > 0 && stopAtFirstLegal) return;

        }

        /// <summary>
        /// For kings we check for possible moves to neighbouring squares
        /// and also any legal castling.
        /// </summary>
        /// <param name="sq"></param>
        /// <param name="color"></param>
        /// <param name="position"></param>
        /// <param name="stopAtFirstLegal"></param>
        /// <param name="moves"></param>
        public static void GetLegalKingMoves(SquareCoords sq, PieceColor color, BoardPosition position, bool stopAtFirstLegal, ref List<MoveOrigDest> moves)
        {
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    int x = sq.Xcoord + i;
                    int y = sq.Ycoord + j;
                    byte square = position.Board[x, y];
                    if (PositionUtils.AreValidCoordinates(x, y))
                    {
                        SquareCoords dest = new SquareCoords(x, y);
                        if (PositionUtils.IsOppositionPiece(square, color))
                        {
                            BoardPosition positionAfterMove = new BoardPosition(position.Board);
                            PositionUtils.MovePiece(sq, dest, ref positionAfterMove.Board);
                            if (PositionUtils.IsKingSafe(positionAfterMove, color))
                            {
                                moves.Add(new MoveOrigDest(sq, dest));
                                break;
                            }
                        }
                        else if (square == 0)
                        {
                            moves.Add(new MoveOrigDest(sq, new SquareCoords(x, y)));
                            if (stopAtFirstLegal)
                                break;
                        }
                    }
                }

                if (moves.Count > 0 && stopAtFirstLegal)
                    break;
            }

            // check possible castling
            if (moves.Count == 0 || !stopAtFirstLegal)
            {
                if (color == PieceColor.White)
                {
                    if (sq.Xcoord == 4 && sq.Ycoord == 0)
                    {
                        if ((position.DynamicProperties & Constants.WhiteKingsideCastle) != 0 && PositionUtils.IsCastlingLegal(Constants.WhiteKingsideCastle, color, position))
                        {
                            moves.Add(new MoveOrigDest(sq, new SquareCoords(6, 0)));
                        }
                        if ((position.DynamicProperties & Constants.WhiteQueensideCastle) != 0 && PositionUtils.IsCastlingLegal(Constants.WhiteQueensideCastle, color, position))
                        {
                            moves.Add(new MoveOrigDest(sq, new SquareCoords(2, 0)));
                        }
                    }
                }
                else
                {
                    if (sq.Xcoord == 4 && sq.Ycoord == 7)
                    {
                        if ((position.DynamicProperties & Constants.BlackKingsideCastle) != 0 && PositionUtils.IsCastlingLegal(Constants.BlackKingsideCastle, color, position))
                        {
                            moves.Add(new MoveOrigDest(sq, new SquareCoords(6, 7)));
                        }
                        if ((position.DynamicProperties & Constants.BlackQueensideCastle) != 0 && PositionUtils.IsCastlingLegal(Constants.BlackQueensideCastle, color, position))
                        {
                            moves.Add(new MoveOrigDest(sq, new SquareCoords(2, 7)));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks all possible pawn moves which includes moves
        /// by 1 square ahead, 2 squares ahead from the original square,
        /// captures and capture en passant.
        /// </summary>
        /// <param name="sq"></param>
        /// <param name="color"></param>
        /// <param name="position"></param>
        /// <param name="stopAtFirstLegal"></param>
        /// <param name="moves"></param>
        public static void GetLegalPawnMoves(SquareCoords sq, PieceColor color, BoardPosition position, bool stopAtFirstLegal, ref List<MoveOrigDest> moves)
        {
            int inc = color == PieceColor.White ? 1 : -1;

            // one square ahead
            int x = sq.Xcoord;
            int y = sq.Ycoord + inc;
            if (PositionUtils.AreValidCoordinates(x, y))
            {
                SquareCoords dest = new SquareCoords(x, y);
                if (position.Board[x, y] == 0)
                {
                    bool res = AddMoveIfValid(sq, dest, color, position, ref moves);
                    if (res && stopAtFirstLegal)
                    {
                        return;
                    }
                }
            }

            // two squares ahead
            x = sq.Xcoord;
            y = sq.Ycoord + 2 * inc;
            if (PositionUtils.AreValidCoordinates(x, y))
            {
                SquareCoords dest = new SquareCoords(x, y);
                if (position.Board[x, y] == 0)
                {
                    bool res = AddMoveIfValid(sq, dest, color, position, ref moves);
                    if (res && stopAtFirstLegal)
                    {
                        return;
                    }
                }
            }

            // capture left
            x = sq.Xcoord - 1;
            y = sq.Ycoord + inc;
            if (PositionUtils.AreValidCoordinates(x, y))
            {
                SquareCoords dest = new SquareCoords(x, y);
                if (PositionUtils.IsOppositionPiece(position.Board[x, y], color))
                {
                    bool res = AddMoveIfValid(sq, dest, color, position, ref moves);
                    if (res && stopAtFirstLegal)
                    {
                        return;
                    }
                }
            }

            // capture right
            x = sq.Xcoord + 1;
            y = sq.Ycoord + inc;
            if (PositionUtils.AreValidCoordinates(x, y))
            {
                SquareCoords dest = new SquareCoords(x, y);
                if (PositionUtils.IsOppositionPiece(position.Board[x, y], color))
                {
                    bool res = AddMoveIfValid(sq, dest, color, position, ref moves);
                    if (res && stopAtFirstLegal)
                    {
                        return;
                    }
                }
            }

            // capture en passant
            if (position.InheritedEnPassantSquare != 0)
            {
                SquareCoords ep = PositionUtils.DecodeEnPassantSquare(position.InheritedEnPassantSquare);
                if ((sq.Xcoord == ep.Xcoord + 1 || sq.Xcoord == ep.Xcoord - 1) && sq.Ycoord + inc == ep.Ycoord)
                {
                    bool res = AddMoveIfValid(sq, ep, color, position, ref moves);
                    if (res && stopAtFirstLegal)
                    {
                        return;
                    }
                }
            }

            return;
        }

        /// <summary>
        /// Checks possible Rook moves in one, specified direction.
        /// </summary>
        /// <param name="xIncr"></param>
        /// <param name="yIncr"></param>
        /// <param name="sq"></param>
        /// <param name="color"></param>
        /// <param name="position"></param>
        /// <param name="stopAtFirstLegal"></param>
        /// <param name="moves"></param>
        private static void GetLegalRookSingleDirectionMoves(int xIncr, int yIncr, SquareCoords sq, PieceColor color, BoardPosition position, bool stopAtFirstLegal, ref List<MoveOrigDest> moves)
        {
            int x = sq.Xcoord + xIncr;
            int y = sq.Ycoord + yIncr;

            while (PositionUtils.AreValidCoordinates(x, y))
            {
                byte square = position.Board[x, y];
                SquareCoords dest = new SquareCoords(x, y);
                if (PositionUtils.IsOppositionPiece(square, color))
                {
                    BoardPosition positionAfterMove = new BoardPosition(position.Board);
                    PositionUtils.MovePiece(sq, dest, ref positionAfterMove.Board);
                    if (PositionUtils.IsKingSafe(positionAfterMove, color))
                    {
                        moves.Add(new MoveOrigDest(sq, dest));
                        break;
                    }
                }
                else if (PositionUtils.IsOurPiece(square, color))
                {
                    break;
                }
                else
                {
                    moves.Add(new MoveOrigDest(sq, new SquareCoords(x, y)));
                    if (stopAtFirstLegal)
                        break;
                }

                x = sq.Xcoord + xIncr;
                y = sq.Ycoord + yIncr;
            }
        }

        /// <summary>
        /// Checks possible Bishop moves in one, specified direction.
        /// </summary>
        /// <param name="xIncr"></param>
        /// <param name="yIncr"></param>
        /// <param name="sq"></param>
        /// <param name="color"></param>
        /// <param name="position"></param>
        /// <param name="stopAtFirstLegal"></param>
        /// <param name="moves"></param>
        private static void GetLegalBishopSingleDirectionMoves(int xIncr, int yIncr, SquareCoords sq, PieceColor color, BoardPosition position, bool stopAtFirstLegal, ref List<MoveOrigDest> moves)
        {
            int x = sq.Xcoord + xIncr;
            int y = sq.Ycoord + yIncr;

            while (PositionUtils.AreValidCoordinates(x, y))
            {
                byte square = position.Board[x, y];
                SquareCoords dest = new SquareCoords(x, y);
                if (PositionUtils.IsOppositionPiece(square, color))
                {
                    BoardPosition positionAfterMove = new BoardPosition(position.Board);
                    PositionUtils.MovePiece(sq, dest, ref positionAfterMove.Board);
                    if (PositionUtils.IsKingSafe(positionAfterMove, color))
                    {
                        moves.Add(new MoveOrigDest(sq, dest));
                        break;
                    }
                }
                else if (PositionUtils.IsOurPiece(square, color))
                {
                    break;
                }
                else
                {
                    moves.Add(new MoveOrigDest(sq, new SquareCoords(x, y)));
                    if (stopAtFirstLegal)
                        break;
                }

                x = sq.Xcoord + xIncr;
                y = sq.Ycoord + yIncr;
            }
        }

        /// <summary>
        /// Checks possible Knight moves in one, specified direction.
        /// </summary>
        /// <param name="xIncr"></param>
        /// <param name="yIncr"></param>
        /// <param name="sq"></param>
        /// <param name="color"></param>
        /// <param name="position"></param>
        /// <param name="moves"></param>
        private static void GetLegalKnightSingleDirectionMoves(int xIncr, int yIncr, SquareCoords sq, PieceColor color, BoardPosition position, ref List<MoveOrigDest> moves)
        {
            int x = sq.Xcoord + xIncr;
            int y = sq.Ycoord + yIncr;

            if (PositionUtils.AreValidCoordinates(x, y))
            {
                byte square = position.Board[x, y];
                SquareCoords dest = new SquareCoords(x, y);
                if (PositionUtils.IsOppositionPiece(square, color))
                {
                    BoardPosition positionAfterMove = new BoardPosition(position.Board);
                    PositionUtils.MovePiece(sq, dest, ref positionAfterMove.Board);
                    if (PositionUtils.IsKingSafe(positionAfterMove, color))
                    {
                        moves.Add(new MoveOrigDest(sq, dest));
                    }
                }
                else if (PositionUtils.IsOurPiece(square, color))
                {
                }
                else
                {
                    moves.Add(new MoveOrigDest(sq, new SquareCoords(x, y)));
                }
            }
        }


        /// <summary>
        /// Checks if the move is valid in the sense of not leaving the king
        /// in check. If so, adds the move to the list and returns true.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="dest"></param>
        /// <param name="color"></param>
        /// <param name="position"></param>
        /// <param name="moves"></param>
        /// <returns></returns>
        private static bool AddMoveIfValid(SquareCoords orig, SquareCoords dest, PieceColor color, BoardPosition position, ref List<MoveOrigDest> moves)
        {
            BoardPosition positionAfterMove = new BoardPosition(position.Board);
            PositionUtils.MovePiece(orig, dest, ref positionAfterMove.Board);
            if (PositionUtils.IsKingSafe(positionAfterMove, color))
            {
                moves.Add(new MoveOrigDest(orig, dest));
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}

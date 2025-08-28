using ChessPosition.Utils;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

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
        /// The null move is allowed if there is no check/stalemate in the current position, 
        /// otherwise any move after the null move would be illegal.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public static bool IsNullMoveAllowed(TreeNode nd)
        {
            return nd != null && !nd.Position.IsCheck && !nd.Position.IsCheckmate && !nd.Position.IsStalemate;
        }

        /// <summary>
        /// Counts the number of pieces on the board.
        /// </summary>
        public static int GetPieceCount(BoardPosition position)
        {
            int count = 0;

            for (int x = 0; x <= 7; x++)
            {
                for (int y = 0; y <= 7; y++)
                {
                    if (position.Board[x, y] != 0)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Removes castling rights, if they conflict with the king or rook positions. 
        /// </summary>
        /// <param name="pos"></param>
        public static void CorrectCastlingRights(ref BoardPosition pos)
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
        /// Sets fill castling rights on the position.
        /// </summary>
        /// <param name="position"></param>
        public static void ResetCastlingRights(ref BoardPosition position)
        {
            position.DynamicProperties |= (
                Constants.WhiteKingsideCastle
                | Constants.WhiteQueensideCastle
                | Constants.BlackKingsideCastle
                | Constants.BlackQueensideCastle);
        }

        /// <summary>
        /// "Guesses" castling rights in the position based purely
        /// on the positions of kings and rooks.
        /// This is done when we don't know the history of the position
        /// and this is our best guess.
        /// </summary>
        /// <param name="pos"></param>
        public static void GuessCastlingRights(ref BoardPosition pos)
        {
            ResetCastlingRights(ref pos);
            CorrectCastlingRights(ref pos);
        }

        /// <summary>
        /// Determines whether the square at the passed coordinates is light.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool IsLightSquare(int x, int y)
        {
            return (x + y) % 2 == 0;
        }

        /// <summary>
        /// Gets the number of Kings in a given position.
        /// </summary>
        /// <param name="whiteKings"></param>
        /// <param name="blackKings"></param>
        /// <param name="pos"></param>
        public static void KingCount(out int whiteKings, out int blackKings, BoardPosition pos)
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
        /// Clears a specified square on the board
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="sq"></param>
        public static void ClearSquare(ref TreeNode nd, SquareCoords sq)
        {
            if (nd == null || !sq.IsValid())
            {
                return;
            }

            ClearSquare((byte)sq.Xcoord, (byte)sq.Ycoord, ref nd.Position.Board);
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
        /// Moves a piece from one square to another.
        /// Handles promotion, if required.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="dest"></param>
        /// <param name="promoteTo"></param>
        /// <param name="nd"></param>
        public static void RepositionPiece(SquareCoords orig, SquareCoords dest, PieceType promoteTo, ref TreeNode nd)
        {
            if (promoteTo != PieceType.None)
            {
                PieceColor color = GetPieceColor(nd, orig);
                PlacePieceOnBoard(promoteTo, color, (byte)dest.Xcoord, (byte)dest.Ycoord, ref nd.Position.Board);
            }
            else
            {
                nd.Position.Board[dest.Xcoord, dest.Ycoord] = nd.Position.Board[orig.Xcoord, orig.Ycoord];
            }

            nd.Position.Board[orig.Xcoord, orig.Ycoord] = 0;
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
        /// Return the type of the piece at given coordinates. 
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="sq"></param>
        /// <returns></returns>
        public static PieceType GetPieceType(TreeNode nd, SquareCoords sq)
        {
            if (nd == null || !sq.IsValid())
            {
                return PieceType.None;
            }

            return GetPieceType(nd.Position.Board[sq.Xcoord, sq.Ycoord]);
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
        /// Returns the color of the piece on a given square.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="sq"></param>
        /// <returns></returns>
        public static PieceColor GetPieceColor(TreeNode nd, SquareCoords sq)
        {
            if (nd == null || !sq.IsValid())
            {
                return PieceColor.None;
            }

            return GetPieceColor(nd.Position.Board[sq.Xcoord, sq.Ycoord]);
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
            if (sq == null)
            {
                return false;
            }

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
            if (position.InheritedEnPassantSquare == (byte)(move.Destination.Xcoord << 4 | move.Destination.Ycoord) && position.InheritedEnPassantSquare != 0 && move.MovingPiece == PieceType.Pawn)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Encodes xy coords into a byte.
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        /// <returns></returns>
        public static byte EncodeEnPassantSquare(int xPos, int yPos)
        {
            return (byte)((xPos << 4) | yPos);
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
        /// Checks if the passed move (in the engine notation) is valid
        /// in the passed position.
        /// </summary>
        /// <param name="engMove"></param>
        /// <returns></returns>
        public static bool IsMoveLegal(MoveData move, BoardPosition position)
        {
            bool valid = false;

            try
            {
                BoardPosition pos = new BoardPosition(position);
                // TODO need a simpler function verifying legality of a move
                MoveUtils.MakeMove(pos, move);
                valid = true;
            }
            catch { }

            return valid;
        }

        /// <summary>
        /// Checks if the orig square has a pawn of the right color to effect
        /// an enpassant capture on the dest square.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="dest"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static bool IsEnpassantAvailable(SquareCoords orig, SquareCoords dest, BoardPosition pos)
        {
            if (!orig.IsValid() || !dest.IsValid())
            {
                return false;
            }

            MoveData move = new MoveData();
            move.Origin = orig;
            move.Color = pos.ColorToMove;
            move.Destination = dest;
            move.MovingPiece = PieceType.Pawn;

            byte square = pos.Board[orig.Xcoord, orig.Ycoord];
            if (GetPieceType(square) == PieceType.Pawn && GetPieceColor(square) == pos.ColorToMove)
            {
                if (IsMoveLegal(move, pos))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the passed enpassant square 
        /// is "exploitable" i.e. whether the side on the move
        /// can capture there enpassant.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static bool IsEnPassantSquareActive(int x, int y, BoardPosition refPos)
        {
            bool result = false;
            SquareCoords dest = new SquareCoords(x, y);

            BoardPosition position = new BoardPosition(refPos);
            position.InheritedEnPassantSquare = EncodeEnPassantSquare(x, y);
            // check if the y coordinate is a valid enpassant rank for the color to move
            if (position.ColorToMove == PieceColor.White && y == 5)
            {
                result = IsEnpassantAvailable(new SquareCoords(x - 1, 4), dest, position);
                if (!result)
                {
                    result = IsEnpassantAvailable(new SquareCoords(x + 1, 4), dest, position);
                }
            }
            else if (position.ColorToMove == PieceColor.Black && y == 2)
            {
                result = IsEnpassantAvailable(new SquareCoords(x - 1, 3), dest, position);
                if (!result)
                {
                    result = IsEnpassantAvailable(new SquareCoords(x + 1, 3), dest, position);
                }
            }

            return result;
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
        public static bool IsCheckmate(BoardPosition pos, out bool isCheck)
        {
            isCheck = false;
            // we are checking of the ColorToMove side is checkmates
            if (IsKingInCheck(pos, pos.ColorToMove))
            {
                isCheck = true;
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
        /// Determines if there is insufficient mating material in the position.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static bool IsInsufficientMaterial(BoardPosition position)
        {
            bool insufficient = true;

            int whiteCount = 0;
            int blackCount = 0;

            bool whiteLightSquaredBishop = false;
            bool blackLightSquaredBishop = false;

            bool whiteDarkSquaredBishop = false;
            bool blackDarkSquaredBishop = false;

            try
            {
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        byte square = position.Board[i, j];

                        if (square != 0)
                        {
                            PieceColor color = GetPieceColor(square);
                            PieceType ptype = GetPieceType(square);

                            if (ptype != PieceType.None && ptype != PieceType.King)
                            {
                                if (ptype != PieceType.Knight && ptype != PieceType.Bishop)
                                {
                                    insufficient = false;
                                    break;
                                }
                                else
                                {
                                    if (color == PieceColor.White)
                                    {
                                        whiteCount++;
                                        if (ptype == PieceType.Bishop)
                                        {
                                            if (IsLightSquare(i, j))
                                            {
                                                whiteLightSquaredBishop = true;
                                            }
                                            else
                                            {
                                                whiteDarkSquaredBishop = true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        blackCount++;
                                        if (ptype == PieceType.Bishop)
                                        {
                                            if (IsLightSquare(i, j))
                                            {
                                                blackLightSquaredBishop = true;
                                            }
                                            else
                                            {
                                                blackDarkSquaredBishop = true;
                                            }
                                        }
                                    }

                                    if (whiteCount + blackCount > 1)
                                    {
                                        if (whiteCount == 1 && blackCount == 1 && (whiteLightSquaredBishop && blackLightSquaredBishop || whiteDarkSquaredBishop && blackDarkSquaredBishop))
                                        {
                                            // same colored bishops only so far; not mating material
                                        }
                                        else
                                        {
                                            insufficient = false;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                insufficient = false;
            }

            position.IsInsufficientMaterial = insufficient;
            return insufficient;
        }

        /// <summary>
        /// Sets check/checkmate and stalemate flags on the passed position.
        /// </summary>
        /// <param name="pos"></param>
        public static void SetCheckStaleMateFlags(ref BoardPosition pos)
        {
            if (IsKingInCheck(pos, pos.ColorToMove))
            {
                pos.IsCheck = true;
                pos.IsStalemate = false;
                pos.IsInsufficientMaterial = false;
                if (IsCheckmate(pos, out _))
                {
                    pos.IsCheckmate = true;
                }
            }
            else
            {
                if (IsStalemate(pos))
                {
                    pos.IsStalemate = true;
                    pos.IsCheck = false;
                    pos.IsCheckmate = false;
                    pos.IsInsufficientMaterial = false;
                }
                else if (IsInsufficientMaterial(pos))
                {
                    pos.IsStalemate = false;
                    pos.IsCheck = false;
                    pos.IsCheckmate = false;
                    pos.IsInsufficientMaterial = true;
                }
                else
                {
                    pos.IsStalemate = false;
                    pos.IsCheck = false;
                    pos.IsCheckmate = false;
                    pos.IsInsufficientMaterial = false;
                }
            }
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
                    emptySquares.Add(new SquareCoords(5, 0));
                    emptySquares.Add(new SquareCoords(6, 0));
                    break;
                case Constants.WhiteQueensideCastle:
                    emptySquares.Add(new SquareCoords(3, 0));
                    emptySquares.Add(new SquareCoords(2, 0));
                    emptySquares.Add(new SquareCoords(1, 0));
                    break;
                case Constants.BlackKingsideCastle:
                    emptySquares.Add(new SquareCoords(5, 7));
                    emptySquares.Add(new SquareCoords(6, 7));
                    break;
                case Constants.BlackQueensideCastle:
                    emptySquares.Add(new SquareCoords(3, 7));
                    emptySquares.Add(new SquareCoords(2, 7));
                    emptySquares.Add(new SquareCoords(1, 7));
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
        /// The pgn parser will call it for the side that moved previous
        /// while the engine line analyser will call it for the side
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

            // if there are no castling rights left, no need to process anything
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

                // check if the move was capturing a rook, then the side not-on-move is losing the castling right
                UpdateCastlingRightsForRook(colorToProcess == PieceColor.White ? PieceColor.Black : PieceColor.White, move.Destination.Xcoord, move.Destination.Ycoord, ref pos);
            }

            // for the opponent
            if (HasAnyCastlingRights(MoveUtils.ReverseColor(colorToProcess), ref pos))
            {
                // check if the move was capturing a rook, then the side not-on-move is losing the castling right
                UpdateCastlingRightsForRook(MoveUtils.ReverseColor(colorToProcess), move.Destination.Xcoord, move.Destination.Ycoord, ref pos);
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
        /// If the rook was not on its original square, it means
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
        /// Gets FEN from clipboard if it is there.
        /// </summary>
        /// <returns></returns>
        public static string GetFenFromClipboard()
        {
            string fen = "";

            try
            {
                if (Clipboard.ContainsData(DataFormats.Text))
                {
                    fen = Clipboard.GetData(DataFormats.Text) as string;
                    BoardPosition boardPosition = new BoardPosition();
                    FenParser.ParseFenIntoBoard(fen, ref boardPosition);
                }
            }
            catch
            {
                fen = "";
            }

            return fen;
        }


        /// <summary>
        /// Given a position identifies potential enpassant squares based on the pawn
        /// positions and which side is on move.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static List<SquareCoords> GetPotentialEnpassantSquares(BoardPosition pos)
        {
            int vertIncrement = (pos.ColorToMove == PieceColor.White ? 1 : -1);
            int enpassantRow = (pos.ColorToMove == PieceColor.White ? 5 : 2);
            byte pawnForCapture = (byte)(Constants.PieceToFlag[PieceType.Pawn] | (pos.ColorToMove == PieceColor.White ? 0 : Constants.Color));
            byte pawnAttacking = (byte)(Constants.PieceToFlag[PieceType.Pawn] | (pos.ColorToMove == PieceColor.White ? Constants.Color : 0));

            List<SquareCoords> res = new List<SquareCoords>();

            for (int x = 0; x <= 7; x++)
            {
                if (pos.Board[x, enpassantRow - vertIncrement] == pawnForCapture)
                {
                    // is there an attacking pawn next to it
                    if (x > 0 && pos.Board[x - 1, enpassantRow - vertIncrement] == pawnAttacking
                        || x < 7 && pos.Board[x + 1, enpassantRow - vertIncrement] == pawnAttacking)
                    {
                        // was last move by 2 squares possible?
                        if (pos.Board[x, enpassantRow] == 0 && pos.Board[x, enpassantRow + vertIncrement] == 0)
                        {
                            res.Add(new SquareCoords(x, enpassantRow));
                        }
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Builds a list of MoveWithEval objects from the list of Nodes.
        /// This is useful e.g. when building a ScoreSheet object.
        /// Must handle the case when the first move is Black's.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static ObservableCollection<MoveWithEval> BuildMoveListFromLine(ObservableCollection<TreeNode> line, uint moveNumberOffset)
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
                    move.Number = (node.Position.MoveNumber + moveNumberOffset).ToString() + ".";
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
                move.Number = (whiteNode.Position.MoveNumber + moveNumberOffset).ToString() + ".";
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
                move.WhitePly = node.GetGuiPlyText(true);
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
                move.BlackPly = node.GetGuiPlyText(true);
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

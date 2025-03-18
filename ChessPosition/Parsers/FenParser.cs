using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ChessPosition;
//
// Documentation: https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
//
// A FEN "record" defines a particular game position, all in one text line and using only the ASCII character set. A text file with only FEN data records should have the file extension ".fen".[4]
//
// A FEN record contains six fields. The separator between fields is a space. The fields are:
//
// [1] Piece placement(from White's perspective). Each rank is described, starting with rank 8 and ending with rank 1; within each rank,
// the contents of each square are described from file "a" through file "h".
// Following the Standard Algebraic Notation (SAN), each piece is identified by a single letter taken from the standard English names
// (pawn = "P", knight = "N", bishop = "B", rook = "R", queen = "Q" and king = "K").
// White pieces are designated using upper-case letters ("PNBRQK") while black pieces use lowercase ("pnbrqk").
// Empty squares are noted using digits 1 through 8 (the number of empty squares), and "/" separates ranks.
//
// [2] Active color. "w" means White moves next, "b" means Black moves next.
//
// [3] Castling availability. If neither side can castle, this is "-". Otherwise, this has one or more letters:
// "K"(White can castle kingside), "Q"(White can castle queenside), "k"(Black can castle kingside), and / or "q"(Black can castle queenside).
// A move that temporarily prevents castling does not negate this notation.
//
// [4] En passant target square in algebraic notation. If there's no en passant target square, this is "-".
// If a pawn has just made a two-square move, this is the position "behind" the pawn.
// This is recorded regardless of whether there is a pawn in position to make an en passant capture.
//
// [5] Halfmove clock: The number of halfmoves since the last capture or pawn advance, used for the fifty-move rule.
//
// [6] Fullmove number: The number of the full move. It starts at 1, and is incremented after Black's move.
//
// Examples:
//   The following example is from the FEN specification:[8]
//
//   Here 's the FEN for the starting position:
//     rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1
//
//   And after the move 1.e4:
//     rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1
//
//   And then after 1...c5:
//     rnbqkbnr/pp1ppppp/8/2p5/4P3/8/PPPP1PPP/RNBQKBNR w KQkq c6 0 2
//

namespace GameTree
{
    /// <summary>
    /// A set of methods to parse and generate FEN strings.
    /// Allows to populate the internal board based on the supplied
    /// FEN string and, conversely, generate a FEN string based on the
    /// state of the internal board.
    /// </summary>
    public class FenParser
    {
        /// <summary>
        /// FEn for the initial position.
        /// </summary>
        public const string FEN_INITIAL_POS = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        /// <summary>
        /// Indicates the standard variant of chess
        /// </summary>
        public const string VARIANT_STANDARD = "Standard";

        /// <summary>
        /// Indicates the standard variant of chess
        /// </summary>
        public const string VARIANT_CHESS = "Chess";

        /// <summary>
        /// Mapping of FEN/PGN symbols to the piece type 
        /// </summary>
        public static Dictionary<char, PieceType> FenCharToPiece
                = new Dictionary<char, PieceType>()
                {
                    ['R'] = PieceType.Rook,
                    ['N'] = PieceType.Knight,
                    ['B'] = PieceType.Bishop,
                    ['Q'] = PieceType.Queen,
                    ['K'] = PieceType.King,
                    ['P'] = PieceType.Pawn
                };

        /// <summary>
        /// Mapping of piece types to FEN symbols
        /// </summary>
        public static Dictionary<PieceType, char> PieceToFenChar
        = new Dictionary<PieceType, char>()
        {
            [PieceType.Rook] = 'R',
            [PieceType.Knight] = 'N',
            [PieceType.Bishop] = 'B',
            [PieceType.Queen] = 'Q',
            [PieceType.King] = 'K',
            [PieceType.Pawn] = 'P'
        };

        /// <summary>
        /// Based on the parsed FEN string, populates the Board object.
        /// Note that that there is no need to clear the Board because the parsing
        /// process will set every field.
        /// 
        /// Throws an exception if the string is found to be in the incorrect format
        /// </summary>
        /// <param name="fen">FEN string</param>
        /// <param name="board">reference to the Board object being set up</param>
        public static void ParseFenIntoBoard(string fen, ref BoardPosition board, bool positionOnly = false)
        {
            // parse the FEN string into 6 fields
            string[] fenFields = fen.Split(' ');

            if (fenFields.Length < 6 && !positionOnly || fenFields.Length < 1)
            {
                throw new Exception("Error: FEN string has too few fields");
            }

            // Field 1: locations of the pieces
            ParsePieceLocations(fenFields[0], ref board);

            if (!positionOnly)
            {
                // Field 2: which side is to move
                DetermineSideToMove(fenFields[1], ref board);

                // Field 3: permitted castling
                DetermineCastlingRights(fenFields[2], ref board);

                // Field 4: the en passant square
                SetEnpassantSquare(fenFields[3], ref board);

                // Field 5: the half moves count since the last capture or a pawn move
                SetHalfMove50Clock(fenFields[4], ref board);

                // Field 6: the number of the full move
                SetMoveNumber(fenFields[5], ref board);
            }
        }

        /// <summary>
        /// Generates a "short" version of fen to use when comparing positions
        /// for getting opening stats.
        /// This is the same as "regular" FEN except for move counters. 
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static string GenerateShortFen(BoardPosition pos)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(FenPieceLocations(pos) + " ");

            sb.Append(pos.ColorToMove == PieceColor.White ? "w " : "b ");

            sb.Append(FenCastleRights(pos) + " ");
            sb.Append(FenEnPassantSquare(pos));

            return sb.ToString();
        }

        /// <summary>
        /// Generates a FEN string from the supplied
        /// position.
        /// </summary>
        /// <returns>FEN string</returns>
        public static string GenerateFenFromPosition(BoardPosition pos, uint moveNumberOffset = 0)
        {
            StringBuilder sb = new StringBuilder();

            // Field 1: locations of the pieces
            sb.Append(FenPieceLocations(pos) + " ");

            // Field 2: which side is to move
            if (pos.ColorToMove == PieceColor.White)
            {
                sb.Append("w ");
            }
            else
            {
                sb.Append("b ");
            }

            // Field 3: permitted castling
            sb.Append(FenCastleRights(pos) + " ");

            // Field 4: the en passant square
            sb.Append(FenEnPassantSquare(pos) + " ");

            // Field 5: the half moves count since the last capture or a pawn move
            sb.Append(pos.HalfMove50Clock.ToString() + " ");

            // Field 6: the number of the full move
            if (pos.ColorToMove == PieceColor.White)
            {
                sb.Append(((pos.MoveNumber + moveNumberOffset) + 1).ToString());
            }
            else
            {
                sb.Append((pos.MoveNumber + moveNumberOffset).ToString());
            }

            return sb.ToString();
        }

        //*************************************************************
        //
        // Fuctions generating substring for the FEN string.
        //
        //*************************************************************

        /// <summary>
        /// Checks castling rights in the Position
        /// (binary flags in the DynamicProperties property)
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static string FenCastleRights(BoardPosition pos)
        {
            StringBuilder sb = new StringBuilder();

            if ((pos.DynamicProperties & Constants.WhiteKingsideCastle) != 0)
            {
                sb.Append("K");
            }

            if ((pos.DynamicProperties & Constants.WhiteQueensideCastle) != 0)
            {
                sb.Append("Q");
            }

            if ((pos.DynamicProperties & Constants.BlackKingsideCastle) != 0)
            {
                sb.Append("k");
            }

            if ((pos.DynamicProperties & Constants.BlackQueensideCastle) != 0)
            {
                sb.Append("q");
            }

            if (sb.Length == 0)
            {
                sb.Append("-");
            }

            return sb.ToString();
        }


        /// <summary>
        /// If the En Passant square is set,
        /// gets the algebraic notation for it.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static string FenEnPassantSquare(BoardPosition pos)
        {
            string dash = "-";
            if (pos.EnPassantSquare == 0)
            {
                return dash;
            }

            int x = pos.EnPassantSquare >> 4;
            int y = pos.EnPassantSquare & 0x0F;

            if (PositionUtils.IsEnPassantSquareActive(x, y, pos))
            {
                char columnCharCode = (char)((pos.EnPassantSquare >> 4) + (int)'a');
                char rowCharCode = (char)((pos.EnPassantSquare & 0x0F) + (int)'1');

                StringBuilder sb = new StringBuilder();
                sb.Append(columnCharCode);
                sb.Append(rowCharCode);
                return sb.ToString();
            }
            else
            {
                return dash;
            }
        }

        /// <summary>
        /// Iterates the board, row by row
        /// and outputs string with row content
        /// separated by '/' as per the FEN spec.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static string FenPieceLocations(BoardPosition pos)
        {
            StringBuilder sb = new StringBuilder();
            // process the position row by row starting from the top
            // i.e. row 7, i.e. Black's backline
            for (int y = 7; y >= 0; y--)
            {
                if (y < 7)
                {
                    sb.Append('/');
                }

                int emptyCount = 0;
                for (int x = 0; x <= 7; x++)
                {
                    if (pos.Board[x, y] == 0 || pos.Board[x, y] == 0xFF)
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount != 0)
                        {
                            sb.Append(emptyCount.ToString());
                            emptyCount = 0;
                        }

                        PieceType pt = PositionUtils.GetPieceType(pos.Board[x, y]);
                        char fenPiece = PieceToFenChar[pt];
                        if (PositionUtils.GetPieceColor(pos.Board[x, y]) != PieceColor.White)
                        {
                            fenPiece = Char.ToLower(fenPiece);
                        }
                        sb.Append(fenPiece);
                    }
                }
                if (emptyCount != 0)
                {
                    sb.Append(emptyCount.ToString());
                }
            }

            return sb.ToString();
        }



        //*************************************************************
        //
        // Fuctions parsing fields in the FEN string to set
        // the Position.
        //
        //*************************************************************

        /// <summary>
        /// Sets the "half move clock" that indicates
        /// the number of half moves (a.k.a. plies) since the 
        /// last capture or pawn move.
        /// </summary>
        /// <param name="clock"></param>
        /// <param name="board"></param>
        private static void SetHalfMove50Clock(string clock, ref BoardPosition board)
        {
            board.HalfMove50Clock = Convert.ToUInt32(clock);
        }

        /// <summary>
        /// Sets the move number.
        /// This is for full moves, as in regular chess notation,
        /// i.e. white and black moves together count as a single move
        /// (as opposed to half move a.k.a. ply that referes to one
        /// side moving.
        /// </summary>
        /// <param name="moveNo"></param>
        /// <param name="board"></param>
        private static void SetMoveNumber(string moveNo, ref BoardPosition board)
        {
            board.MoveNumber = Convert.ToUInt32(moveNo);
            // if White is on move, subtract one because the move number
            // in BoardPosition is one ply behind FEN
            if (board.ColorToMove == PieceColor.White)
            {
                board.MoveNumber--;
            }
        }

        /// <summary>
        /// Parses the en passant string and sets the InheritedEnPassantSquare
        /// square (if defined) on the board.
        /// </summary>
        /// <param name="enpassant"></param>
        /// <param name="board"></param>
        public static void SetInheritedEnpassantSquare(string enpassant, ref BoardPosition board)
        {
            if (string.IsNullOrEmpty(enpassant) || enpassant.Length > 2)
            {
                throw new Exception("Error: invalid enpassant field");
            }

            // do we have a '-'
            if (enpassant.Length == 1 && enpassant[0] == '-')
            {
                // while 0 represents a valid square ("a1") it is not
                // a valid en passant square, hence we can use it to
                // indicate that ther is no en passant square in the position. 
                board.InheritedEnPassantSquare = 0;
            }
            else if (enpassant.Length == 2)
            {
                // if we have an en passant square, it will be in the algebraic notation like "e4"
                byte xPos = (byte)(enpassant[0] - 'a');
                byte yPos = (byte)(enpassant[1] - '1');
                if (xPos >= 0 && xPos <= 7 && yPos >= 0 && yPos <= 7)
                {
                    board.InheritedEnPassantSquare = (byte)((xPos << 4) | yPos);
                }
                else
                {
                    throw new Exception("Error: invalid enpassant field");
                }
            }
            else
            {
                throw new Exception("Error: invalid enpassant field");
            }
        }

        /// <summary>
        /// Parses the en passant string and sets both the EnPassantSquare
        /// and the InheritedEnPassantSquare on the board.
        /// </summary>
        /// <param name="enpassant"></param>
        /// <param name="board"></param>
        public static void SetEnpassantSquare(string enpassant, ref BoardPosition board)
        {
            bool valid = false;

            // if we have an en passant square, it will be in the algebraic notation
            // like "e4"
            if (enpassant != null && enpassant.Length == 2)
            {
                byte xPos = (byte)(enpassant[0] - 'a');
                byte yPos = (byte)(enpassant[1] - '1');
                if (xPos >= 0 && xPos <= 7 && yPos >= 0 && yPos <= 7)
                {
                    board.EnPassantSquare = (byte)((xPos << 4) | yPos);
//                    board.InheritedEnPassantSquare = board.EnPassantSquare;
                    valid = true;
                }
            }

            if (!valid)
            {
                // while 0 represents a valid square ("a1") it is not
                // a valid en passant square, hence we can use it to
                // indicate that ther is no en passant square in the position. 
                board.EnPassantSquare = 0;
                board.InheritedEnPassantSquare = 0;
            }
        }

        /// <summary>
        /// Sets the castling rights based on the castling part of the fen string
        /// </summary>
        /// <param name="castling"></param>
        /// <param name="board"></param>
        private static void DetermineCastlingRights(string castling, ref BoardPosition board)
        {
            foreach (char c in castling)
            {
                switch (c)
                {
                    case 'K':
                        board.DynamicProperties |= Constants.WhiteKingsideCastle;
                        break;
                    case 'Q':
                        board.DynamicProperties |= Constants.WhiteQueensideCastle;
                        break;
                    case 'k':
                        board.DynamicProperties |= Constants.BlackKingsideCastle;
                        break;
                    case 'q':
                        board.DynamicProperties |= Constants.BlackQueensideCastle;
                        break;
                }
            }
        }

        /// <summary>
        /// Determines the color of the side to move.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="board"></param>
        private static void DetermineSideToMove(string side, ref BoardPosition board)
        {
            if (side.ToLower() == "w")
            {
                board.DynamicProperties |= Constants.Color;
            }
            else if (side.ToLower() != "b")
            {
                throw new Exception("Error: color to move not specified");
            }
        }

        /// <summary>
        /// Parses piece locations on the board
        /// </summary>
        /// <param name="fenPieces"></param>
        /// <param name="board"></param>
        /// <exception cref="Exception"></exception>
        private static void ParsePieceLocations(string fenPieces, ref BoardPosition board)
        {
            // the FEN's first field must represent exactly 8 rows, separated by a slash
            string[] fenRows = fenPieces.Split('/');
            if (fenRows.Length != 8)
            {
                throw new Exception("Error: FEN string has " + fenRows.Length.ToString() + " rows");
            }

            int fenRowNo = 7;
            // first token in lines represents the last row (black end of the board)
            // We will start from the last token in rows which represents row with YPos == 0
            // (i.e. where White pieces are in the starting position) in our internal board
            for (int i = (int)Constants.MIN_ROW_NO; i <= (int)Constants.MAX_ROW_NO; i++)
            {
                ParseRow(i, fenRows[fenRowNo], ref board);
                fenRowNo--;
            }
        }

        /// <summary>
        /// Parses a single row representation from the string
        /// </summary>
        /// <param name="row"></param>
        /// <param name="fenRow"></param>
        /// <param name="board"></param>
        /// <exception cref="Exception"></exception>
        private static void ParseRow(int row, string fenRow, ref BoardPosition board)
        {
            // process the string character by character
            int currentColumn = Constants.MIN_COLUMN_NO;
            foreach (char c in fenRow)
            {
                // if this is a digit, set the coresponding number
                // of empty squares
                if (char.IsDigit(c))
                {
                    int emptyCount = c - '0';
                    SetEmptySquares(currentColumn, row, emptyCount, ref board);
                    currentColumn += emptyCount;
                }
                else
                {
                    board.Board[currentColumn, row] = Constants.PieceToFlag[FenCharToPiece[Char.ToUpper(c)]];

                    if (Char.IsUpper(c))
                    {
                        board.Board[currentColumn, row] |= (byte)Constants.Color;
                    }
                    currentColumn++;
                }
            }

            if (currentColumn != 8)
            {
                throw new Exception("Error: row " + row.ToString() + " does not define 8 squares.");

            }
        }

        /// <summary>
        /// Sets empty squares on the board.
        /// </summary>
        /// <param name="fromColumn"></param>
        /// <param name="row"></param>
        /// <param name="emptyCount"></param>
        /// <param name="board"></param>
        private static void SetEmptySquares(int fromColumn, int row, int emptyCount, ref BoardPosition board)
        {
            for (int i = fromColumn; i < fromColumn + emptyCount; i++)
            {
                board.Board[i, row] = (byte)PieceType.None;
            }
        }
    }
}

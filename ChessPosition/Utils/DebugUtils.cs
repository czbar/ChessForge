using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;
using System.Diagnostics;
using ChessForge;

namespace ChessPosition
{
    /// <summary>
    /// Utilities for printing debug data.
    /// </summary>
    public class DebugUtils
    {
        /// <summary>
        /// The level of debugging detail.
        /// </summary>
        public static int DebugLevel = 0;

        /// <summary>
        /// Maps Pieces to letters for the debug output.
        /// </summary>
        public static Dictionary<PieceType, char> FenPieceToChar = new Dictionary<PieceType, char>()
        {
            [PieceType.Rook] = 'r',
            [PieceType.Knight] = 'n',
            [PieceType.Bishop] = 'b',
            [PieceType.Queen] = 'q',
            [PieceType.King] = 'k',
            [PieceType.Pawn] = 'p',
            [PieceType.None] = '-'
        };

        /// <summary>
        /// Displays an error identified in debug mode to the user.
        /// </summary>
        /// <param name="msg"></param>
        [Conditional("DEBUG")]
        public static void ShowDebugMessage(string msg)
        {
            if (DebugLevel >= 2)
            {
                MessageBox.Show(msg, "DEBUG", MessageBoxButton.OK, MessageBoxImage.Error);
                AppLog.Message("DEBUG MessageBox: " + msg);
            }
        }

        /// <summary>
        /// Builds a ChessForge log file name
        /// given the log type and distinct name part of any
        /// </summary>
        /// <returns></returns>
        public static string BuildLogFileName(string dir, string logType, string distinctPart, string extension = null)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("chf_" + logType);
            if (distinctPart != null)
            {
                sb.Append(distinctPart);
            }

            if (string.IsNullOrEmpty(extension))
            {
                sb.Append(".txt");
            }
            else
            {
                sb.Append("." + extension);
            }

            return Path.Combine(dir, sb.ToString());
        }

        /// <summary>
        /// Builds a list of strings representing the current position
        /// including the board.
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public static List<string> BuildStringForPosition(BoardPosition board)
        {
            List<string> list = new List<string>();

            list.Add("");

            for (int i = 7; i >= 0; i--)
            {
                list.Add(BuildStringForRow((int)i, board));
            }

            list.Add("   ........");

            StringBuilder sb = new StringBuilder();
            sb.Append("   ");
            for (int i = 0; i <= 7; i++)
            {
                sb.Append((char)('a' + i));
            }
            list.Add(sb.ToString());

            list.Add("");
            list.Add("Side to move: " + (((board.DynamicProperties & Constants.Color) > 0) ? "White" : "Black"));

            list.Add("Castling rights");
            list.Add("   White Kingside : " + (((board.DynamicProperties & Constants.WhiteKingsideCastle) > 0) ? "yes" : "no"));
            list.Add("   White Queenside: " + (((board.DynamicProperties & Constants.WhiteQueensideCastle) > 0) ? "yes" : "no"));
            list.Add("   Black Kingside : " + (((board.DynamicProperties & Constants.BlackKingsideCastle) > 0) ? "yes" : "no"));
            list.Add("   Black Queenside: " + (((board.DynamicProperties & Constants.BlackQueensideCastle) > 0) ? "yes" : "no"));

            sb = new StringBuilder();
            sb.Append("En Passant square: ");
            sb.Append(EnpassantString(board.EnPassantSquare));
            list.Add(sb.ToString());
            sb.Clear();

            sb.Append("Inherited En Passant square: ");
            sb.Append(EnpassantString(board.InheritedEnPassantSquare));
            list.Add(sb.ToString());
            sb.Clear();

            list.Add("Halfmove50 clock: " + board.HalfMove50Clock);
            list.Add("Move number     : " + board.MoveNumber);
            return list;
        }

        /// <summary>
        /// Prints the passed position to the Console in 
        /// a human readable format.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="fenString"></param>
        [Conditional("DEBUG")]
        public static void PrintPosition(BoardPosition board, string fenString = null)
        {
            if (!string.IsNullOrEmpty(fenString))
            {
                Console.WriteLine("PRINTING Board from FEN:");
                Console.WriteLine("     " + fenString);
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("");
            }

            for (int i = 7; i >= 0; i--)
            {
                PrintRow((int)i, board);
            }

            Console.WriteLine("   ........");
            Console.Write("   ");
            for (int i = 0; i <= 7; i++)
            {
                Console.Write((char)('a' + i));
            }
            Console.WriteLine("");

            Console.WriteLine("");
            Console.WriteLine("Side to move: " + (((board.DynamicProperties & Constants.Color) > 0) ? "White" : "Black"));

            Console.WriteLine("");
            Console.WriteLine("Castling rights");
            Console.WriteLine("   White Kingside : " + (((board.DynamicProperties & Constants.WhiteKingsideCastle) > 0) ? "yes" : "no"));
            Console.WriteLine("   White Queenside: " + (((board.DynamicProperties & Constants.WhiteQueensideCastle) > 0) ? "yes" : "no"));
            Console.WriteLine("   Black Kingside : " + (((board.DynamicProperties & Constants.BlackKingsideCastle) > 0) ? "yes" : "no"));
            Console.WriteLine("   Black Queenside: " + (((board.DynamicProperties & Constants.BlackQueensideCastle) > 0) ? "yes" : "no"));

            Console.WriteLine("");
            Console.Write("En Passant square: ");
            if (board.EnPassantSquare == 0)
            {
                Console.WriteLine("none");
            }
            else
            {
                char columnCharCode = (char)((board.EnPassantSquare >> 4) + (int)'a');
                char rowCharCode = (char)((board.EnPassantSquare & 0x0F) + (int)'1');
                Console.Write((char)columnCharCode);
                Console.WriteLine((char)rowCharCode);
            }

            Console.WriteLine("");
            Console.WriteLine("Halfmove50 clock: " + board.HalfMove50Clock);
            Console.WriteLine("Move number     : " + board.MoveNumber);

            Console.WriteLine("");
            Console.WriteLine("=====================");
            Console.WriteLine("");
        }

        /// <summary>
        /// Builds a string with the decoded enpassant square
        /// </summary>
        /// <param name="square"></param>
        /// <returns></returns>
        private static string EnpassantString(byte square)
        {
            if (square == 0)
            {
                return "none";
            }
            else
            {
                char columnCharCode = (char)((square >> 4) + (int)'a');
                char rowCharCode = (char)((square & 0x0F) + (int)'1');
                return ((char)columnCharCode).ToString() + ((char)rowCharCode).ToString();
            }
        }

        /// <summary>
        /// Generates a string for logging a single board row.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="board"></param>
        /// <returns></returns>
        private static string BuildStringForRow(int row, BoardPosition board)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append((row + 1) + ": ");
            for (int i = 0; i <= 7; i++)
            {
                char piece = FenPieceToChar[Constants.FlagToPiece[(byte)((board.Board[i, row] & ~Constants.Color))]];
                if ((board.Board[i, row] & Constants.Color) > 0)
                {
                    piece = char.ToUpper(piece);
                }

                sb.Append(piece);
            }
            sb.Append("");
            return sb.ToString();
        }

        /// <summary>
        /// Prints a single board row to the Console.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="board"></param>
        [Conditional("DEBUG")]
        private static void PrintRow(int row, BoardPosition board)
        {
            string rowStr = BuildStringForRow(row, board);
            Console.WriteLine(rowStr);
        }

        /// <summary>
        /// Pronts the list of "attackers" of a given square
        /// to the Console.
        /// </summary>
        /// <param name="algSquare"></param>
        /// <param name="col"></param>
        /// <param name="attackers"></param>
        [Conditional("DEBUG")]
        public static void PrintAttackers(string algSquare, PieceColor col, List<Square> attackers)
        {
            Console.WriteLine("");
            Console.WriteLine("*********");
            Console.WriteLine("ATTACKERS (" + (col == PieceColor.White ? "White" : "Black") + " pieces)");

            Console.WriteLine("   Square " + algSquare);
            if (attackers == null || attackers.Count == 0)
            {
                Console.WriteLine("   No attackers found");
            }
            else
            {
                for (int i = 0; i < attackers.Count; i++)
                {
                    Console.Write("     " + (i + 1).ToString() + ". " + attackers[i].pieceType);
                    Console.WriteLine(" " + PositionUtils.ConvertXYtoAlgebraic(attackers[i].Location.Xcoord, attackers[i].Location.Ycoord));
                }
            }

            Console.WriteLine("*********");
            Console.WriteLine("");
        }

        /// <summary>
        /// Prints a single move to the Console.
        /// </summary>
        /// <param name="move"></param>
        /// <param name="alg"></param>
        [Conditional("DEBUG")]
        public static void PrintMove(MoveData move, string alg)
        {
            Console.WriteLine("");
            Console.WriteLine("MOVE: " + alg);
        }
    }
}

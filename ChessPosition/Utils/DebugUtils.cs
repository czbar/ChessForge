using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace ChessPosition
{
    public class DebugUtils
    {
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
        public static void ShowDebugMessage(string msg)
        {
            MessageBox.Show(msg, "DEBUG", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static string DumpWorkbookTree(WorkbookTree tree)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < tree.Nodes.Count; i++)
            {
                TreeNode nd = tree.Nodes[i];
                sb.Append("Node index = " + i.ToString() + Environment.NewLine);
                sb.Append("Node Id = " + nd.NodeId.ToString() + Environment.NewLine);
                sb.Append("Parent Node Id = " + (nd.Parent == null ? "-" : nd.Parent.NodeId.ToString()) + Environment.NewLine);
                sb.Append("Move alg = " + nd.LastMoveAlgebraicNotation + Environment.NewLine);
                sb.Append("DistanceToLeaf = " + nd.DistanceToLeaf.ToString() + Environment.NewLine);
                sb.Append("DistanceToFork = " + nd.DistanceToNextFork.ToString() + Environment.NewLine  );
                for(int j = 0; j < nd.Children.Count; j++)
                {
                    sb.Append("    Child " + j.ToString() + " Node Id = " + nd.Children[j].NodeId.ToString() +  Environment.NewLine);
                }
                sb.Append(Environment.NewLine + Environment.NewLine);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Prints the passed position to the Console in 
        /// a human readable format.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="fenString"></param>
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
        /// Prints a single board row to the Console.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="board"></param>
        private static void PrintRow(int row, BoardPosition board)
        {
            Console.Write((row + 1) + ": ");
            for (int i = 0; i <= 7; i++)
            {
                char piece = FenPieceToChar[Constants.FlagToPiece[(byte)((board.Board[i, row] & ~Constants.Color))]];
                if ((board.Board[i, row] & Constants.Color) > 0)
                {
                    piece = char.ToUpper(piece);
                }

                Console.Write(piece);
            }
            Console.WriteLine("");
        }

        /// <summary>
        /// Pronts the list of "attackers" of a given square
        /// to the Console.
        /// </summary>
        /// <param name="algSquare"></param>
        /// <param name="col"></param>
        /// <param name="attackers"></param>
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
        public static void PrintMove(MoveData move, string alg)
        {
            Console.WriteLine("");
            Console.WriteLine("MOVE: " + alg);
        }
    }
}

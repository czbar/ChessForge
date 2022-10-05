using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GameTree.ChfCommands;

namespace ChessPosition
{
    public enum PieceColor
    {
        None = 0,
        White,
        Black
    }

    public enum PieceType
    {
        None = 0,
        King,
        Queen,
        Bishop,
        Knight,
        Rook,
        Pawn
    }

    /// <summary>
    /// Constants used coding/encoding
    /// position attributes.
    /// </summary>
    public class Constants
    {

        /// <summary>
        /// Maps Piece Type to binary flag
        /// used in the Position coding.
        /// </summary>
        public static Dictionary<PieceType, byte> PieceToFlag
                = new Dictionary<PieceType, byte>()
                {
                    [PieceType.None] = 0x00,
                    [PieceType.Pawn] = 0x01,
                    [PieceType.Knight] = 0x02,
                    [PieceType.Bishop] = 0x04,
                    [PieceType.Rook] = 0x08,
                    [PieceType.Queen] = 0x10,
                    [PieceType.King] = 0x20
                };

        /// <summary>
        /// Binary flag to Piece Type mapping
        /// </summary>
        public static Dictionary<byte, PieceType> FlagToPiece
                = new Dictionary<byte, PieceType>()
                {
                    [0x00] = PieceType.None,
                    [0x01] = PieceType.Pawn,
                    [0x02] = PieceType.Knight,
                    [0x04] = PieceType.Bishop,
                    [0x08] = PieceType.Rook,
                    [0x10] = PieceType.Queen,
                    [0x20] = PieceType.King
                };

        /// <summary>
        /// Maps Numeric Annotation Glyphs codes to Unicode characters. 
        /// </summary>
        public static Dictionary<int, string> NagsDict
                = new Dictionary<int, string>()
                {
                    [1] = "!",
                    [2] = "?",
                    [3] = "!!",
                    [4] = "??",
                    [5] = "!?",
                    [6] = "?!",
                    [11] = "=",
                    [12] = "=",
                    [13] = '\u221E'.ToString(), // '∞'
                    [14] = '\u2A72'.ToString(), // '⩲',
                    [15] = '\u2A71'.ToString(), // '⩱',
                    [16] = '\u00B1'.ToString(), // '±',
                    [17] = '\u2213'.ToString(), // '∓',
                    [18] = "+-",
                    [19] = "-+"
                };


        /// <summary>
        /// Returns a NAG id if the passed represents one.
        /// Returns 0 otherwise.
        /// </summary>
        /// <param name="nag"></param>
        /// <returns></returns>
        public static int GetNagIdFromString(string nag)
        {
            return NagsDict.FirstOrDefault(x => x.Value == nag).Key;
        }


        /// <summary>
        /// Min and Max dimensions of the chess board
        /// when starting from 0.
        /// </summary>
        public const int MIN_ROW_NO = 0;
        public const int MAX_ROW_NO = 7;
        public const int MIN_COLUMN_NO = 0;
        public const int MAX_COLUMN_NO = 7;

        /// <summary>
        /// Binary flags representing castling rights
        /// </summary>
        public const byte WhiteKingsideCastle = 0x08;
        public const byte WhiteQueensideCastle = 0x04;
        public const byte BlackKingsideCastle = 0x02;
        public const byte BlackQueensideCastle = 0x01;

        /// <summary>
        /// The bit to store the color in chessboard square's
        /// byte encoding
        /// (MSB).
        /// </summary>
        public const byte Color = 0x80;

        /// <summary>
        /// Zindex values for the main chessboard artefacts
        /// </summary>
        public const int ZIndex_SquareMoveOverlay = 1;
        public const int ZIndex_PieceOnBoard = 5;
        public const int ZIndex_BoardArrow = 6;
        public const int ZIndex_PieceInAnimation = 10;

        /// <summary>
        /// String for naming colors in contexts where using Brushes
        /// would be an overkill.
        /// The "CHAR"s are for external use in PGN so they cannot be changed to ensure
        /// compatiblity with lichess's PGN extensions
        /// </summary>
        public const string COLOR_GREEN = "green";
        public const char COLOR_GREEN_CHAR = 'G';

        public const string COLOR_BLUE = "blue";
        public const char COLOR_BLUE_CHAR = 'B';

        public const string COLOR_RED = "red";
        public const char COLOR_RED_CHAR = 'R';

        public const string COLOR_YELLOW = "yellow";
        public const char COLOR_YELLOW_CHAR = 'Y';
    }
}

﻿using System.Collections.Generic;
using System.Linq;

namespace ChessPosition
{
    /// <summary>
    /// Color of the chess pieces
    /// </summary>
    public enum PieceColor
    {
        None = 0,
        White,
        Black
    }

    /// <summary>
    /// Type of the chess piece
    /// </summary>
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
    /// Enumeration of possible process states.
    /// </summary>
    public enum ProcessState
    {
        UNKNOWN,
        NOT_STARTED,
        RUNNING,
        FINISHED,
        CANCELED
    };

    /// <summary>
    /// Scope to which to apply an operation.
    /// </summary>
    public enum OperationScope
    {
        NONE,
        ACTIVE_ITEM,
        CHAPTER,
        WORKBOOK
    };

    /// <summary>
    /// Flags to use e.g. when scoping certain operations.
    /// </summary>
    public enum ViewTypeScope
    {
        NONE = 0,
        INTRO = 0x01,
        STUDY = 0x02,
        MODEL_GAMES = 0x04,
        EXERCISES = 0x08,
    };

    /// <summary>
    /// Possible actions on a list of articles.
    /// </summary>
    public enum ArticlesAction
    {
        NONE,
        COPY,
        MOVE,
        COPY_OR_MOVE,
        DELETE
    }

    /// <summary>
    /// Move attribute types.
    /// </summary>
    public enum MoveAttribute
    {
        COMMENT_AND_NAGS  = 0x01,
        ENGINE_EVALUATION = 0x02,
        BAD_MOVE_ASSESSMENT = 0x04,
        SIDELINE = 0x08,
    };

    /// <summary>
    /// Article attribute types.
    /// </summary>
    public enum ArticleAttribute
    {
        ANNOTATOR = 0x01,
    };

    /// <summary>
    /// Type of the tab control.
    /// </summary>
    public enum TabViewType
    {
        NONE,
        INTRO,
        CHAPTERS,
        STUDY,
        BOOKMARKS,
        MODEL_GAME,
        EXERCISE,
        TRAINING,
        ENGINE_GAME
    }

    /// <summary>
    /// Sizes in which the TabControls may be displayed.
    /// </summary>
    public enum TabControlSizeMode
    {
        SHOW_ACTIVE_LINE,
        HIDE_ACTIVE_LINE,
        SHOW_ACTIVE_LINE_NO_EVAL,
        SHOW_ENGINE_GAME_LINE,
        HIDE_ENGINE_GAME_LINE
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
        /// Binary flag to Piece Type mapping.
        /// The 0x7F and 0xFF are used for searches 
        /// with mandatory empty squares, where mandatory space is coded as 0xFF.
        /// The 0x7F is needed because GetPieceType() applies 0x7F mask.
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
                    [0x20] = PieceType.King,
                    [0x7F] = PieceType.None,
                    [0xFF] = PieceType.None
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
        /// Character used to start the Comment.
        /// </summary>
        public const char START_COMMENT = '{';

        /// <summary>
        /// Character used to end the Comment.
        /// </summary>
        public const char END_COMMENT = '}';

        /// <summary>
        /// Minimum size of the diagram image.
        /// </summary>
        public static int MIN_DIAGRAM_SIZE = 120;

        /// <summary>
        /// Maximum size of the diagram image.
        /// </summary>
        public static int MAX_DIAGRAM_SIZE = 960;

        /// <summary>
        /// Maximum width of the border around the diagram in the image.
        /// </summary>
        public static int MAX_DIAGRAM_IMAGE_BORDER_WIDTH = 50;

        /// <summary>
        /// Normal menu fony size
        /// </summary>
        public static double DEAFULT_MENU_FONT_SIZE = 12;

        /// <summary>
        /// Increased menu font size
        /// </summary>
        public static double LARGE_MENU_FONT_SIZE = 14;

        /// <summary>
        /// Width of the narrow scrollbar
        /// </summary>
        public static double NARROW_SCROLLBAR_WIDTH = 5;

        /// <summary>
        /// Width of the wide scrollbar
        /// </summary>
        public static double WIDE_SCROLLBAR_WIDTH = 10;

        /// <summary>
        /// Min id of the move NAG
        /// </summary>
        public static int MinMoveNagId = 1;

        /// <summary>
        /// Max id of the move NAG
        /// </summary>
        public static int MaxMoveNagId = 6;

        /// <summary>
        /// Min id of the position NAG
        /// </summary>
        public static int MinPositionNagId = 11;

        /// <summary>
        /// Max id of the position NAG
        /// </summary>
        public static int MaxPositionNagId = 19;

        /// <summary>
        /// Base size of the font when fixed size font is selected for the views.
        /// </summary>
        public const int BASE_FIXED_FONT_SIZE = 14;

        /// <summary>
        /// Base size of the font for the engine lines.
        /// </summary>
        public const int BASE_ENGINE_LINES_FONT_SIZE = 12;

        /// <summary>
        /// Base size of the font in the Top Games view.
        /// </summary>
        public const int BASE_TOP_GAMES_FONT_SIZE = 11;

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
        /// Last move number considered to part of the opening.  
        /// We won't be querying Lichess for opening name if the move
        /// number is greater than this.
        /// </summary>
        public static int OPENING_MAX_MOVE = 15;

        /// <summary>
        /// In some places we need a string indication that an exception occurred.
        /// </summary>
        public static string EXCEPTION = "Exception";

        /// <summary>
        /// Text for a null move in the algebraic notation.
        /// </summary>
        public static string NULL_MOVE_NOTATION = "---";

        /// <summary>
        /// String to use in order to create extra spacing for game's main line comments.
        /// </summary>
        public static string PSEUDO_LINE_SPACING = "\n\t\n";

        /// <summary>
        /// Name of the empty paragraph deliberately created to provide extra spacing
        /// </summary>
        public static string DUMMY_PARA_NAME = "dummy_para";

        /// <summary>
        /// Name of the paragraph to use for the title of the workbook.
        /// </summary>
        public static string WORKBOOK_TITLE_PARAGRAPH_NAME = "para_title";

        /// <summary>
        /// Name to use for lichess in configuration for selecting a web site
        /// </summary>
        public static string LichessNameId = "lichess.org";

        /// <summary>
        /// Name to use for chess.com in configuration for selecting a web site
        /// </summary>
        public static string ChesscomNameId = "chess.com";

        /// <summary>
        /// Character to use as an "expand" symbol in the tree/table views.
        /// </summary>
        public const char CharExpand = '\u229E';

        /// <summary>
        /// Character to use as an "collapse" symbol in the tree/table views.
        /// </summary>
        public const char CharCollapse = '\u229F';

        /// <summary>
        /// Character for a White Square to use as a White side indication in the Game view.
        /// </summary>
        public const char CharWhiteSquare = '\u2B1C';

        /// <summary>
        /// Character for a Black Square to use as a Black side indication in the Game view.
        /// </summary>
        public const char CharBlackSquare = '\u2B1B';

        /// <summary>
        /// Trade Mark (TM) symbol.
        /// </summary>
        public const char CHAR_TRADE_MARK = '\u2122';

        /// <summary>
        /// Superscript left parenthesis.
        /// </summary>
        public const char CHAR_SUPER_LEFT_PARENTHESIS = '\u207D';

        /// <summary>
        /// Superscript right parenthesis.
        /// </summary>
        public const char CHAR_SUPER_RIGHT_PARENTHESIS = '\u207E';

        /// <summary>
        /// Character for a Large White Triangle Up.
        /// </summary>
        public const char CHAR_WHITE_LARGE_TRIANGLE_UP = '\u25B3';

        /// <summary>
        /// Character for a Large Blacke Triangle Down.
        /// </summary>
        public const char CHAR_BLACK_LARGE_TRIANGLE_DOWN = '\u25BC';

        /// <summary>
        /// Character for a Black Triangle Up to use in Online Library.
        /// </summary>
        public const char CHAR_BLACK_TRIANGLE_UP = '\u25BE';

        /// <summary>
        /// Character for a Black Triangle Down to use in Online Library.
        /// </summary>
        public const char CHAR_BLACK_TRIANGLE_DOWN = '\u25B4';

        /// <summary>
        /// Check mark character
        /// </summary>
        public const char CharCheckMark = '\u2713';

        /// <summary>
        /// Check mark character
        /// </summary>
        public const char CharCrossMark = '\u2715';

        /// <summary>
        /// The half point notation ('1/2')
        /// </summary>
        public const char CharHalfPoint = '\u00BD';

        /// <summary>
        /// The Fork character
        /// </summary>
        public const char CharFork = '\u2442';

        /// <summary>
        /// The response (left arrow with hook) character
        /// </summary>
        public const char CHAR_RESPONSE = '\u21A9';

        /// <summary>
        /// The left arrow character
        /// </summary>
        public const char CHAR_LEFT_ARROW = '\u2190';

        /// <summary>
        /// The up arrow character
        /// </summary>
        public const char CHAR_UP_ARROW = '\u2191';

        /// <summary>
        /// The right arrow character
        /// </summary>
        public const char CHAR_RIGHT_ARROW = '\u2192';

        /// <summary>
        /// The down arrow character
        /// </summary>
        public const char CHAR_DOWN_ARROW = '\u2193';

        /// <summary>
        /// The low asterisk character
        /// </summary>
        public const char CHAR_LOW_ASTERISK = '\u204E';

        /// <summary>
        /// Square inside a square
        /// </summary>
        public const char CHAR_SQUARED_SQUARE = '\u29C8';

        /// <summary>
        /// Asterisk to indicate a thumbnail
        /// </summary>
        public const char CHAR_THUMBNAIL = '\u002A';

        /// <summary>
        /// The reference mark character
        /// </summary>
        public const char CHAR_REFERENCE_MARK = '\u203B';

        /// <summary>
        /// The reference mark character
        /// </summary>
        public const char CHAR_SELECTED = '\u27A4';

        /// <summary>
        /// The pencil icon
        /// </summary>
        public const char CHAR_PENCIL = '\u270E';

        /// <summary>
        /// The "small t" icon
        /// </summary>
        public const char CHAR_SMALL_T = '\u0442';

        /// <summary>
        /// The small question mark
        /// </summary>
        public const char CHAR_EXCLAM_QUESTION = '\u2049';

        /// <summary>
        /// Evaluation symbols
        /// </summary>
        public const char CHAR_WHITE_ADVANTAGE = '\u00B1';
        public const char CHAR_WHITE_EDGE = '\u2A72';
        public const char CHAR_POSITION_UNCLEAR = '\u221E';
        public const char CHAR_BLACK_ADVANTAGE = '\u2213';
        public const char CHAR_BLACK_EDGE = '\u2A71';

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
        /// Strings representing game results
        /// </summary>
        public const string PGN_WHITE_WIN_RESULT = "1-0";
        public const string PGN_BLACK_WIN_RESULT = "0-1";
        public const string PGN_DRAW_RESULT = "1/2-1/2";
        public const string PGN_DRAW_SHORT_RESULT = "1/2";
        public const string PGN_NO_RESULT = "*";

        // handle variants with long dashes too
        public const string PGN_WHITE_WIN_RESULT_EX = "1–0";
        public const string PGN_BLACK_WIN_RESULT_EX = "0–1";

        /// <summary>
        /// The bit to store the color in chessboard square's
        /// byte encoding
        /// (MSB).
        /// </summary>
        public const byte Color = 0x80;

        /// <summary>
        /// A string to use when there is no date in the PGN file
        /// </summary>
        public const string EMPTY_PGN_DATE = "????.??.??";

        /// <summary>
        /// Zindex values for the main chessboard artefacts
        /// </summary>
        public const int ZIndex_SquareMoveOverlay = 1;
        public const int ZIndex_PieceOnBoard = 5;
        public const int ZIndex_BoardArrow = 6;
        public const int ZIndex_PieceInAnimation = 10;
        public const int ZIndex_PromoTray = 12;

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

        public const string COLOR_ORANGE = "orange";
        public const char COLOR_ORANGE_CHAR = 'O';

        public const string COLOR_PURPLE = "purple";
        public const char COLOR_PURPLE_CHAR = 'P';

        public const string COLOR_DARKRED = "darkred";
        public const char COLOR_DARKRED_CHAR = 'D';
    }
}

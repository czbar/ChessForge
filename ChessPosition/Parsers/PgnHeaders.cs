using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;
using ChessPosition;

namespace GameTree
{
    /// <summary>
    /// Handles PGN headers for either the Workbook
    /// or a chapter.
    /// </summary>
    public class PgnHeaders
    {
        /// <summary>
        /// The list of headers. 
        /// </summary>
        private List<KeyValuePair<string, string>> _headers = new List<KeyValuePair<string, string>>();

        /// <summary>
        /// This must be the first header in the Chess Forge PGN file
        /// so that the file can be recognized as Chess Forge workbook.
        /// If absent, the file will be considered a third-party PGN.
        /// The value will be considered to be the Workbook's title.
        /// </summary>
        public const string KEY_WORKBOOK_TITLE = "ChessForgeWorkbook";

        /// <summary>
        /// Version of the Workbook/
        /// </summary>
        public const string KEY_WORKBOOK_VERSION = "WorkbookVersion";

        /// <summary>
        /// Determines which side is the default training side in 
        /// the Workbook. The value can be "White", "Black" or "None".
        /// This is part of the first set of headers (i.e. for the entire Workbook)
        /// only.
        /// </summary>
        public const string KEY_TRAINING_SIDE = "TrainingSide";

        /// <summary>
        /// Determines the initial board orientation in the Study view.
        /// </summary>
        public const string KEY_STUDY_BOARD_ORIENTATION = "StudyBoardOrientation";

        /// <summary>
        /// Determines the initial board orientation in the Games view.
        /// </summary>
        public const string KEY_GAME_BOARD_ORIENTATION = "GameBoardOrientation";

        /// <summary>
        /// Determines the initial board orientation in the Exercises view.
        /// </summary>
        public const string KEY_EXERCISE_BOARD_ORIENTATION = "ExerciseBoardOrientation";

        /// <summary>
        /// Event Name.
        /// </summary>
        public const string KEY_EVENT = "Event";

        /// <summary>
        /// Encyclopedia of Chess Openings code.
        /// </summary>
        public const string KEY_ECO = "ECO";

        /// <summary>
        /// Lichess id of the game downloaded from lichess.org.
        /// </summary>
        public const string KEY_LICHESS_ID = "LichessId";

        /// <summary>
        /// Chess.com id of the game downloaded from chess.com.
        /// </summary>
        public const string KEY_CHESSCOM_ID = "ChessComId";

        /// <summary>
        /// Link to the game on the chess.com site
        /// </summary>
        public const string KEY_LINK = "Link";

        /// <summary>
        /// Kink to the game on the lichess.org site
        /// </summary>
        public const string KEY_SITE = "Site";

        /// <summary>
        /// Event round number.
        /// </summary>
        public const string KEY_ROUND = "Round";

        /// <summary>
        /// Position in the FEN format.
        /// </summary>
        public const string KEY_FEN_STRING = "FEN";

        /// <summary>
        /// Guid as a unique identifier of a ChessForge element.
        /// </summary>
        public const string KEY_GUID = "Guid";

        /// <summary>
        /// The title of a chapter.
        /// </summary>
        public const string KEY_CHAPTER_TITLE = "ChapterTitle";

        /// <summary>
        /// Type of the game which can be "Study Tree", "Model Game" or "Exercise".
        /// </summary>
        public const string KEY_CONTENT_TYPE = "ContentType";

        /// <summary>
        /// Basically, the same meaning as in the standard PGN.
        /// It will be "*" for the chapter's variation tree or
        /// game result for games and combinations.
        /// </summary>
        public const string KEY_RESULT = "Result";

        /// <summary>
        /// Date in the yyyy.MM.dd format.
        /// </summary>
        public const string KEY_DATE = "Date";

        /// <summary>
        /// UTCDate
        /// </summary>
        public const string KEY_UTC_DATE = "UTCDate";

        /// <summary>
        /// UTCDate
        /// </summary>
        public const string KEY_UTC_TIME = "UTCTime";

        /// <summary>
        /// Store White's name in model games 
        /// and dummy values in non-game Variation Trees
        /// to keep some PGN viewers happy.
        /// </summary>
        public const string KEY_WHITE = "White";

        /// <summary>
        /// Store Black's name in model games 
        /// and dummy values in non-game Variation Trees
        /// to keep some PGN viewers happy.
        /// </summary>
        public const string KEY_BLACK = "Black";

        /// <summary>
        /// Annotator or Author of the content.
        /// </summary>
        public const string KEY_ANNOTATOR = "Annotator";

        /// <summary>
        /// Chess variant
        /// </summary>
        public const string KEY_VARIANT = "Variant";

        /// <summary>
        /// Elo of the White player
        /// </summary>
        public const string KEY_WHITE_ELO = "WhiteElo";

        /// <summary>
        /// Elo of the Black player
        /// </summary>
        public const string KEY_BLACK_ELO = "BlackElo";

        /// <summary>
        /// Depth of the index in the Study View
        /// </summary>
        public const string KEY_INDEX_DEPTH = "IndexDepth";

        /// <summary>
        /// Depth of the index in the Study View
        /// </summary>
        public const string KEY_SHOW_SOLUTIONS_ON_OPEN = "ShowSolutionsOnOpen";

        /// <summary>
        /// A preamble line. There can be many per header and will be combined
        /// together into a preamble.
        /// </summary>
        public const string KEY_PREAMBLE = "Preamble";



        public const string VALUE_WHITE = "White";
        public const string VALUE_BLACK = "Black";
        public const string VALUE_NO_COLOR = "None";

        public const string VALUE_INTRO = "Intro";
        public const string VALUE_STUDY_TREE = "Study Tree";
        public const string VALUE_MODEL_GAME = "Model Game";
        public const string VALUE_EXERCISE = "Exercise";

        /// <summary>
        /// Builds header line for the Workbook Title
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public static string GetWorkbookTitleText(string title)
        {
            return BuildHeaderLine(KEY_WORKBOOK_TITLE, title);
        }

        /// <summary>
        /// Builds header line for the Author's name
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public static string GetAuthorText(string author)
        {
            return BuildHeaderLine(KEY_ANNOTATOR, author);
        }

        /// <summary>
        /// Builds header line for the Workbook Version
        /// </summary>
        /// <param name="ver"></param>
        /// <returns></returns>
        public static string GetWorkbookVersionText(string ver)
        {
            return BuildHeaderLine(KEY_WORKBOOK_VERSION, ver);
        }

        /// <summary>
        /// Builds header line for the Workbook's guid
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static string GetWorkbookGuidText(string guid)
        {
            return BuildHeaderLine(KEY_GUID, guid);
        }

        /// <summary>
        /// Checks if the passed string looks like a header line and if so
        /// returns the name and value of the header.
        /// </summary>
        /// <param name=""></param>
        /// <returns>Null if this is not a header line otherwise the name of the header.</returns>
        public static string ParsePgnHeaderLine(string line, out string val)
        {
            string header = null;
            val = "";
            line = line.Trim();

            if (line.Length > 0 && line[0] == '[' && line[line.Length - 1] == ']')
            {
                line = line.Substring(1, line.Length - 2);
                string[] tokens = line.Split('\"');
                if (tokens.Length >= 2)
                {
                    header = tokens[0].Trim();
                    val = tokens[1].Trim();
                }
            }

            return header;
        }

        /// <summary>
        /// Returns the header text for the color of the training side
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string GetTrainingSideText(PieceColor color)
        {
            string sColor;
            switch (color)
            {
                case PieceColor.White:
                    sColor = VALUE_WHITE;
                    break;
                case PieceColor.Black:
                    sColor = VALUE_BLACK;
                    break;
                default:
                    sColor = VALUE_NO_COLOR;
                    break;
            }
            return BuildHeaderLine(KEY_TRAINING_SIDE, sColor);
        }

        /// <summary>
        /// Returns the header text for the initial board orientation in the Study view.
        /// </summary>
        public static string GetStudyBoardOrientationText(PieceColor color)
        {
            return BuildHeaderLine(KEY_STUDY_BOARD_ORIENTATION, GetColorValueText(color));
        }

        /// <summary>
        /// Returns the header text for the initial board orientation in the Games view.
        /// </summary>
        public static string GetGameBoardOrientationText(PieceColor color)
        {
            return BuildHeaderLine(KEY_GAME_BOARD_ORIENTATION, GetColorValueText(color));
        }

        /// <summary>
        /// Returns the header text for the initial board orientation in the Study view.
        /// </summary>
        public static string GetExerciseBoardOrientationText(PieceColor color)
        {
            return BuildHeaderLine(KEY_EXERCISE_BOARD_ORIENTATION, GetColorValueText(color));
        }

        /// <summary>
        /// Builds a string (a word) to represent
        /// the color value in the output file.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private static string GetColorValueText(PieceColor color)
        {
            string sColor;
            switch (color)
            {
                case PieceColor.White:
                    sColor = VALUE_WHITE;
                    break;
                case PieceColor.Black:
                    sColor = VALUE_BLACK;
                    break;
                default:
                    sColor = VALUE_NO_COLOR;
                    break;
            }

            return sColor;
        }

        /// <summary>
        /// Return the Date in the PGN format
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string GetDateText(DateTime? dt)
        {
            if (dt == null)
            {
                return "";
            }

            return BuildHeaderLine(KEY_DATE, FormatPgnDateString(dt));
        }

        /// <summary>
        /// Returns the fixed string for the White field in the Workbook's 
        /// pgn "game".  
        /// </summary>
        /// <returns></returns>
        public static string GetWorkbookWhiteText()
        {
            return BuildHeaderLine(KEY_WHITE, "CHESS FORGE");
        }

        public static string GetWorkbookBlackText()
        {
            return BuildHeaderLine(KEY_BLACK, "WORKBOOK");
        }

        public static string GetLineResultHeader()
        {
            return BuildHeaderLine(KEY_RESULT, Constants.PGN_NO_RESULT);
        }

        /// <summary>
        /// Formats date in PGN format.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string FormatPgnDateString(DateTime? dt)
        {
            if (dt == null)
            {
                return "";
            }

            return dt.Value.ToString("yyyy.MM.dd");
        }

        /// <summary>
        /// Builds a header line string in the pgn format.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string BuildHeaderLine(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "";
            }

            return "[" + key + " \"" + (value ?? "") + "\"]";
        }

        private static string BuildHeaderLine(KeyValuePair<string, string> header)
        {
            return BuildHeaderLine(header.Key, header.Value);
        }
    }
}

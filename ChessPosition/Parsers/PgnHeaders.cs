using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ChessPosition
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
        public const string NAME_WORKBOOK_TITLE = "ChessForgeWorkbook";

        /// <summary>
        /// Determines which side is the default training side in 
        /// the Workbook. The value can be "White", "Black" or "None".
        /// This is part of the first set of headers (i.e. for the entire Workbook)
        /// only.
        /// </summary>
        public const string NAME_TRAINING_SIDE = "TrainingSide";

        /// <summary>
        /// In Chess Forge this header is repurposed as a chapter's title.
        /// </summary>
        public const string NAME_EVENT_ = "Event";

        /// <summary>
        /// Position in the FEN format.
        /// </summary>
        public const string NAME_FEN = "Fen";

        /// <summary>
        /// The number of a chapter. The same number may appear in multiple
        /// Variation Trees thus organizing them into chapters.
        /// </summary>
        public const string NAME_CHAPTER_ID = "ChapterId";

        /// <summary>
        /// The number of a chapter. The same number may appear in multiple
        /// Variation Trees thus organizing them into chapters.
        /// </summary>
        public const string NAME_CHAPTER_TITLE = "ChapterTitle";

        /// <summary>
        /// Type of the game which can be "Study Tree", "Model Game" or "Exercise".
        /// </summary>
        public const string NAME_CONTENT_TYPE = "ContentType";

        /// <summary>
        /// Basically, the same meaning as in the standard PGN.
        /// It will be "*" for the chapter's variation tree or
        /// game result for games and combinations.
        /// </summary>
        public const string NAME_RESULT = "Result";

        /// <summary>
        /// Date in the yyyy.MM.dd format.
        /// </summary>
        public const string NAME_DATE = "Date";

        /// <summary>
        /// Store White's name in model games 
        /// and dummy values in non-game Variation Trees
        /// to keep some PGN viewers happy.
        /// </summary>
        public const string NAME_WHITE = "White";

        /// <summary>
        /// Store Black's name in model games 
        /// and dummy values in non-game Variation Trees
        /// to keep some PGN viewers happy.
        /// </summary>
        public const string NAME_BLACK = "Black";

        public const string VALUE_WHITE = "White";
        public const string VALUE_BLACK = "Black";
        public const string VALUE_NO_COLOR = "None";

        public const string VALUE_STUDY_TREE = "Study Tree";
        public const string VALUE_MODEL_GAME = "Model Game";
        public const string VALUE_EXERCISE = "Exercise";

        public static string GetWorkbookTitleText(string title)
        {
            return BuildHeaderLine(NAME_WORKBOOK_TITLE, title);
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


        public static string GetTrainingSideText(PieceColor color)
        {
            string sColor;
            switch (color)
            {
                case PieceColor.White:
                    sColor = NAME_WHITE;
                    break;
                case PieceColor.Black:
                    sColor = NAME_BLACK;
                    break;
                default:
                    sColor = VALUE_NO_COLOR;
                    break;

            }
            return BuildHeaderLine(NAME_TRAINING_SIDE, sColor);
        }

        public static string GetDateText(DateTime? dt)
        {
            if (dt == null)
            {
                return "";
            }

            return BuildHeaderLine(NAME_DATE, dt.Value.ToString("yyyy.MM.dd"));
        }

        public static string GetWorkbookWhiteText()
        {
            return BuildHeaderLine(NAME_WHITE, "Chess Forge");
        }

        public static string GetWorkbookBlackText()
        {
            return BuildHeaderLine(NAME_BLACK, "Workbook File");
        }

        public static string GetLineResultHeader()
        {
            return BuildHeaderLine(NAME_RESULT, "*");
        }


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

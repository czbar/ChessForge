using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GameTree
{
    /// <summary>
    /// Encapsulates headers of a single PGN game.
    /// It is used by VariationTree and GameMetadata.
    /// </summary>
    public class GameHeader
    {
        /// <summary>
        /// The list of headers. 
        /// </summary>
        private List<KeyValuePair<string, string>> _headers = new List<KeyValuePair<string, string>>();

        /// <summary>
        /// The preamble assembled from individual [Preamble "Text"] headers
        /// </summary>
        private List<string> _preamble = new List<string>();

        /// <summary>
        /// Clears any stored header data.
        /// </summary>
        public void Clear()
        {
            _preamble.Clear();
            _headers.Clear();
        }

        /// <summary>
        /// Shallow copy to use in Associated Tree.
        /// </summary>
        /// <returns></returns>
        public GameHeader CloneMe(bool deep)
        {
            GameHeader header = this.MemberwiseClone() as GameHeader;
            if (deep)
            {
                header._headers = new List<KeyValuePair<string, string>>();
                foreach (KeyValuePair<string, string> pair in this._headers)
                {
                    header._headers.Add(pair);
                }
            }
            return header;
        }

        /// <summary>
        /// Checks if the header represents a standard chess PGN Game/Exercise
        /// </summary>
        /// <returns></returns>
        public bool IsStandardChess()
        {
            string variant = GetVariant(out _);
            return string.IsNullOrEmpty(variant) || variant == FenParser.VARIANT_STANDARD || variant == FenParser.VARIANT_CHESS;
        }

        /// <summary>
        /// Checks if the header represents a standard chess game
        /// </summary>
        /// <returns></returns>
        public bool IsGame()
        {
            return IsStandardChess() && !IsExercise();
        }

        /// <summary>
        /// For the header to represent an Exercise
        /// the FEN string cannot be empty and cannot
        /// represent the starting position
        /// </summary>
        /// <returns></returns>
        public bool IsExercise()
        {
            string fen = GetFenString();
            if (string.IsNullOrEmpty(fen) || fen == FenParser.FEN_INITIAL_POS)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Divides the passed string into lines and stores them in the _preamble list
        /// </summary>
        /// <param name="text"></param>
        public void SetPreamble(string text)
        {
            _preamble.Clear();
            if (!string.IsNullOrEmpty(text))
            {
                string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                foreach (string line in lines)
                {
                    _preamble.Add(line);
                }
            }
        }

        /// <summary>
        /// Combines Preamble strings inserting NewLines between them. 
        /// </summary>
        /// <returns></returns>
        public string BuildPreambleText()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < _preamble.Count; i++)
            {
                if (i < _preamble.Count - 1)
                {
                    sb.AppendLine(_preamble[i]);
                }
                else
                {
                    sb.Append(_preamble[i]);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Copies strings from a passed list into the preamble
        /// </summary>
        /// <param name="preamble"></param>
        public void SetPreamble(List<string> preamble)
        {
            _preamble.Clear();
            foreach (string line in preamble)
            {
                _preamble.Add(line);
            }
        }

        /// <summary>
        /// Returns the Preamble.
        /// </summary>
        /// <returns></returns>
        public List<string> GetPreamble()
        {
            return _preamble;
        }

        /// <summary>
        /// Checks if there is at least one header line processed 
        /// for this game.
        /// </summary>
        /// <returns></returns>
        public bool HasAnyHeader()
        {
            return _headers.Count > 0;
        }

        /// <summary>
        /// Builds text for the column with the name of the game.
        /// </summary>
        public string BuildGameHeaderLine(bool simplified, bool includeResult = true, bool includeECO = true, bool includeYear = false, bool includeElo = true)
        {
            StringBuilder sb = new StringBuilder();

            string white = GetWhitePlayer(out _);
            string black = GetBlackPlayer(out _);

            bool hasWhite = !string.IsNullOrEmpty(white);
            bool hasBlack = !string.IsNullOrEmpty(black);

            string whiteElo = includeElo ? GetWhitePlayerElo(out _) : "";
            string blackElo = includeElo ? GetBlackPlayerElo(out _) : "";

            string eco = GetECO(out _);

            if (simplified)
            {
                if (hasWhite || hasBlack)
                {
                    if (includeECO && !string.IsNullOrWhiteSpace(eco))
                    {
                        sb.Append(eco + " ");
                    }

                    if (hasWhite)
                    {
                        sb.Append(white);
                        sb.Append(string.IsNullOrWhiteSpace(whiteElo) ? "" : (" (" + whiteElo + ")"));
                    }

                    sb.Append(" - ");

                    if (hasBlack)
                    {
                        sb.Append(black);
                        sb.Append(string.IsNullOrWhiteSpace(blackElo) ? "" : (" (" + blackElo + ")"));
                    }
                }
            }
            else
            {
                if (includeECO && !string.IsNullOrWhiteSpace(eco))
                {
                    sb.Append(eco + " ");
                }
                sb.Append(white ?? "NN");
                sb.Append(string.IsNullOrWhiteSpace(whiteElo) ? "" : (" (" + whiteElo + ")"));
                sb.Append(" - ");
                sb.Append(black ?? "NN");
                sb.Append(string.IsNullOrWhiteSpace(blackElo) ? "" : (" (" + blackElo + ")"));
            }

            if (includeResult)
            {
                string result = GetResult(out _);
                if (!string.IsNullOrEmpty(result))
                {
                    sb.Append(' ' + result);
                }
            }

            string eventName = GetEventName(out _);
            if (!string.IsNullOrEmpty(eventName) && eventName != "?")
            {
                if (simplified && !hasWhite && !hasBlack)
                {
                    sb.Append(eventName);
                }
                else
                {
                    sb.Append(", " + eventName + "");
                }
            }

            string round = GetRound(out _);
            if (!string.IsNullOrEmpty(round) && round != "?")
            {
                sb.Append(" (" + round + ") ");
            }

            if (includeYear)
            {
                int year = GetYear();
                if (year != 0)
                {
                    sb.Append(", " + year.ToString());
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns a value for the passed key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetValueForKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            return _headers.Where(kvp => kvp.Key == key).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the title of the Workbook
        /// </summary>
        /// <returns></returns>
        public string GetWorkbookTitle()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.KEY_WORKBOOK_TITLE).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the name of the author/annotator
        /// </summary>
        /// <returns></returns>
        public string GetAnnotator()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.KEY_ANNOTATOR).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the version of this workbook.
        /// </summary>
        /// <returns></returns>
        public string GetWorkbookVersion()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.KEY_WORKBOOK_VERSION).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the title of the chapter.
        /// </summary>
        /// <returns></returns>
        public string GetChapterTitle()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.KEY_CHAPTER_TITLE).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the value of the "White" header.
        /// </summary>
        /// <returns></returns>
        public string GetWhitePlayer(out string key, string value = null)
        {
            string headerKey = PgnHeaders.KEY_WHITE;
            key = headerKey;

            if (value == null)
            {
                return _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Returns the value of the "Black" header.
        /// </summary>
        /// <returns></returns>
        public string GetBlackPlayer(out string key, string value = null)
        {
            string headerKey = PgnHeaders.KEY_BLACK;
            key = headerKey;

            if (value == null)
            {
                return _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Returns the value of the "Variant" header.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string GetVariant(out string key, string value = null)
        {
            string headerKey = PgnHeaders.KEY_VARIANT;
            key = headerKey;

            if (value == null)
            {
                return _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            }
            else
            {
                return value;
            }
        }
        /// <summary>
        /// Returns the value of the "WhiteElo" header.
        /// </summary>
        /// <returns></returns>
        public string GetWhitePlayerElo(out string key, string value = null)
        {
            string headerKey = PgnHeaders.KEY_WHITE_ELO;
            key = headerKey;

            if (value == null)
            {
                return _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Returns the value of the "BlackElo" header.
        /// </summary>
        /// <returns></returns>
        public string GetBlackPlayerElo(out string key, string value = null)
        {
            string headerKey = PgnHeaders.KEY_BLACK_ELO;
            key = headerKey;

            if (value == null)
            {
                return _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Returns the value of the "Annotator" header.
        /// </summary>
        /// <returns></returns>
        public string GetAnnotator(out string key, string value = null)
        {
            string headerKey = PgnHeaders.KEY_ANNOTATOR;
            key = headerKey;

            if (value == null)
            {
                return _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Returns the value of the "IndexDepth" header.
        /// </summary>
        /// <returns></returns>
        public string GetIndexDepth(out string key, string value = null)
        {
            string headerKey = PgnHeaders.KEY_INDEX_DEPTH;
            key = headerKey;

            if (value == null)
            {
                return _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Returns the value of the "ShowAllSolutions" header.
        /// </summary>
        /// <returns></returns>
        public string GetShowSolutionsOnOpen()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.KEY_SHOW_SOLUTIONS_ON_OPEN).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the value of the "Date" header.
        /// </summary>
        /// <returns></returns>
        public string GetDate(out string key, string value = null)
        {
            string headerKey = PgnHeaders.KEY_DATE;
            key = headerKey;

            if (value == null)
            {
                value = _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            }

            return TextUtils.AdjustPgnDateString(value, out _, out _);
        }

        /// <summary>
        /// Returns the year part of the date.
        /// If invalid, returns 0;
        /// </summary>
        /// <returns></returns>
        public int GetYear()
        {
            int year = 0;

            string date = GetDate(out _);
            if (!string.IsNullOrEmpty(date))
            {
                string[] tokens = date.Split('.');
                if (!int.TryParse(tokens[0], out year))
                {
                    year = 0;
                }
            }

            return year;
        }

        /// <summary>
        /// Returns the result of a tree/game/exercise.
        /// Never returns null.
        /// </summary>
        /// <returns></returns>
        public string GetResult(out string key)
        {
            string headerKey = PgnHeaders.KEY_RESULT;
            key = headerKey;

            string value = _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            if (string.IsNullOrEmpty(value))
            {
                value = Constants.PGN_NO_RESULT;
            }

            return value;
        }

        /// <summary>
        /// Returns the Round number
        /// </summary>
        /// <returns></returns>
        public string GetRound(out string key)
        {
            string headerKey = PgnHeaders.KEY_ROUND;
            key = headerKey;

            string value = _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            if (string.IsNullOrEmpty(value))
            {
                value = "";
            }

            return value;
        }

        /// <summary>
        /// Returns the Event Name
        /// </summary>
        /// <returns></returns>
        public string GetEventName(out string key)
        {
            string headerKey = PgnHeaders.KEY_EVENT;
            key = headerKey;

            string value = _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            if (string.IsNullOrEmpty(value))
            {
                value = "";
            }

            return value;
        }

        /// <summary>
        /// Returns the game's ECO code
        /// </summary>
        /// <returns></returns>
        public string GetECO(out string key)
        {
            string headerKey = PgnHeaders.KEY_ECO;
            key = headerKey;

            string value = _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            if (string.IsNullOrEmpty(value))
            {
                value = "";
            }

            return value;
        }

        /// <summary>
        /// Returns the Lichess Id
        /// </summary>
        /// <returns></returns>
        public string GetLichessId(out string key)
        {
            string headerKey = PgnHeaders.KEY_LICHESS_ID;
            key = headerKey;

            string value = _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            if (string.IsNullOrEmpty(value))
            {
                value = "";
            }

            return value;
        }

        /// <summary>
        /// Returns the Chess.com Id
        /// </summary>
        /// <returns></returns>
        public string GetChessComId(out string key)
        {
            string headerKey = PgnHeaders.KEY_CHESSCOM_ID;
            key = headerKey;

            string value = _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            if (string.IsNullOrEmpty(value))
            {
                value = "";
            }

            return value;
        }

        /// <summary>
        /// Returns the Guid.
        /// If there is no Guid, generates and saves it.
        /// </summary>
        /// <returns></returns>
        public string GetGuid(out string key)
        {
            string headerKey = PgnHeaders.KEY_GUID;
            key = headerKey;

            string value = _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            if (string.IsNullOrEmpty(value))
            {
                value = TextUtils.GenerateRandomElementName();
                SetHeaderValue(PgnHeaders.KEY_GUID, value);
            }

            return value;
        }

        /// <summary>
        /// Returns the existing GUID or generates one if not found.
        /// </summary>
        /// <param name="generated">whether guid existed or was generated</param>
        /// <returns>guid string</returns>
        public string GetOrGenerateGuid(out bool generated)
        {
            generated = false;

            string headerKey = PgnHeaders.KEY_GUID;

            string value = _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            if (string.IsNullOrEmpty(value))
            {
                value = SetNewTreeGuid();
                generated = true;
            }

            return value;
        }

        /// <summary>
        /// Generates GUID in the format used in Trees/Articles
        /// </summary>
        /// <returns></returns>
        public string SetNewTreeGuid()
        {
            string guid = TextUtils.GenerateRandomElementName();
            SetHeaderValue(PgnHeaders.KEY_GUID, guid);
            return guid;
        }

        /// <summary>
        /// Returns the training side value.
        /// </summary>
        /// <returns></returns>
        public string GetTrainingSide(out string key)
        {
            string headerKey = PgnHeaders.KEY_TRAINING_SIDE;

            key = headerKey;
            return _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the default chessboard orientation for the Study view.
        /// </summary>
        /// <returns></returns>
        public string GetStudyBoardOrientation(out string key)
        {
            string headerKey = PgnHeaders.KEY_STUDY_BOARD_ORIENTATION;

            key = headerKey;
            return _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the default chessboard orientation for the Games view.
        /// </summary>
        /// <returns></returns>
        public string GetGameBoardOrientation(out string key)
        {
            string headerKey = PgnHeaders.KEY_GAME_BOARD_ORIENTATION;

            key = headerKey;
            return _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
        }


        /// <summary>
        /// Returns the default chessboard orientation for the Exercises view.
        /// </summary>
        /// <returns></returns>
        public string GetExerciseBoardOrientation(out string key)
        {
            string headerKey = PgnHeaders.KEY_EXERCISE_BOARD_ORIENTATION;

            key = headerKey;
            return _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the Content Type string value.
        /// </summary>
        /// <returns></returns>
        public string GetContentTypeString(out string key, string value = null)
        {
            string headerKey = PgnHeaders.KEY_CONTENT_TYPE;
            key = headerKey;

            if (value == null)
            {
                return _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Returns the Content Type value.
        /// </summary>
        /// <returns></returns>
        public GameData.ContentType GetContentType(out string key, string value = null)
        {
            string headerKey = PgnHeaders.KEY_CONTENT_TYPE;
            key = headerKey;

            string val;
            if (value == null)
            {
                val = _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            }
            else
            {
                val = value;
            }

            return GetContentTypeFromString(val);
        }

        /// <summary>
        /// Sets the string value of the ContentType header item
        /// according to the passed ContentType
        /// </summary>
        /// <param name="contentType"></param>
        public void SetContentType(GameData.ContentType contentType)
        {
            switch (contentType)
            {
                case GameData.ContentType.STUDY_TREE:
                    SetHeaderValue(PgnHeaders.KEY_CONTENT_TYPE, PgnHeaders.VALUE_STUDY_TREE);
                    break;
                case GameData.ContentType.MODEL_GAME:
                    SetHeaderValue(PgnHeaders.KEY_CONTENT_TYPE, PgnHeaders.VALUE_MODEL_GAME);
                    break;
                case GameData.ContentType.EXERCISE:
                    SetHeaderValue(PgnHeaders.KEY_CONTENT_TYPE, PgnHeaders.VALUE_EXERCISE);
                    break;
            }
        }

        /// <summary>
        /// Based on the collected values determines and sets the type
        /// </summary>
        /// <returns></returns>
        public GameData.ContentType DetermineContentType()
        {
            if (!IsStandardChess())
            {
                return GameData.ContentType.UNKNOWN;
            }
            else if (!string.IsNullOrEmpty(GetContentTypeString(out _)))
            {
                return GetContentType(out _);
            }
            else if (IsExercise())
            {
                SetHeaderValue(PgnHeaders.KEY_CONTENT_TYPE, PgnHeaders.VALUE_EXERCISE);
                return GameData.ContentType.EXERCISE;
            }
            else
            {
                return GameData.ContentType.GENERIC;
            }
        }

        /// <summary>
        /// Converts a string value to ContentType. 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private GameData.ContentType GetContentTypeFromString(string s)
        {
            GameData.ContentType typ = GameData.ContentType.GENERIC;

            if (!string.IsNullOrEmpty(s))
            {
                switch (s)
                {
                    case PgnHeaders.VALUE_MODEL_GAME:
                        typ = GameData.ContentType.MODEL_GAME;
                        break;
                    case PgnHeaders.VALUE_STUDY_TREE:
                        typ = GameData.ContentType.STUDY_TREE;
                        break;
                    case PgnHeaders.VALUE_INTRO:
                        typ = GameData.ContentType.INTRO;
                        break;
                    case PgnHeaders.VALUE_EXERCISE:
                        typ = GameData.ContentType.EXERCISE;
                        break;
                    default:
                        if (!IsStandardChess())
                        {
                            typ = GameData.ContentType.UNKNOWN;
                        }
                        break;
                }
            }

            return typ;
        }

        /// <summary>
        /// Returns the FEN string from the header.
        /// </summary>
        /// <returns></returns>
        public string GetFenString()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.KEY_FEN_STRING).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the value of header with a given name.
        /// Null if header not found.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetHeaderValue(string name)
        {
            var header = _headers.Where(kvp => kvp.Key == name).FirstOrDefault();
            return header.Value;
        }

        /// <summary>
        /// Saves header item's name and value. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetHeaderValue(string name, string value)
        {
            if (name == PgnHeaders.KEY_PREAMBLE)
            {
                _preamble.Add(value ?? "");
            }
            else
            {
                if (!string.IsNullOrEmpty(name))
                {
                    var header = _headers.Where(kvp => kvp.Key == name).FirstOrDefault();
                    if (!header.Equals(default(KeyValuePair<string, string>)))
                    {
                        _headers.Remove(header);
                    }

                    header = new KeyValuePair<string, string>(name, value);
                    _headers.Add(header);
                }
            }
        }
    }
}

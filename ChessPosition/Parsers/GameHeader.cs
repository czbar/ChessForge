using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace GameTree
{
    /// <summary>
    /// Encaplsulates headers of a single PGN game.
    /// It is used by VariationTree and GameMetadata.
    /// </summary>
    public class GameHeader
    {
        /// <summary>
        /// The list of headers. 
        /// </summary>
        private List<KeyValuePair<string, string>> _headers = new List<KeyValuePair<string, string>>();

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
        public string BuildGameHeaderLine(bool simplified = false)
        {
            StringBuilder sb = new StringBuilder();

            string white = GetWhitePlayer(out _);
            string black = GetBlackPlayer(out _);

            bool hasWhite = !string.IsNullOrEmpty(white);
            bool hasBlack = !string.IsNullOrEmpty(black);

            if (simplified)
            {
                if (hasWhite)
                {
                    sb.Append(white);
                }
                if (hasWhite || hasBlack)
                {
                    sb.Append(" - ");
                }
                if (hasBlack)
                {
                    sb.Append(black);
                }
            }
            else
            {
                sb.Append((white ?? "NN") + " - " + (black ?? "NN"));
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
                    sb.Append(" at " + eventName + "");
                }
            }

            string round = GetRound(out _);
            if (!string.IsNullOrEmpty(round) && round != "?")
            {
                sb.Append(" Rd." + round + " ");
            }
            return sb.ToString();
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
        /// Returns the result of a tree/game/exercise
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

            return _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
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
        /// Returns the training side value.
        /// </summary>
        /// <returns></returns>
        public string GetChapterId()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.KEY_CHAPTER_ID).FirstOrDefault().Value;
        }


        /// <summary>
        /// Returns the Legacy Title value.
        /// </summary>
        /// <returns></returns>
        public string GetLegacyTitle()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.KEY_LEGACY_TITLE).FirstOrDefault().Value;
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
            if (!string.IsNullOrEmpty(GetContentTypeString(out _)))
            {
                return GetContentType(out _);
            }
            else if (!string.IsNullOrEmpty(GetFenString()))
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
                    case PgnHeaders.VALUE_EXERCISE:
                        typ = GameData.ContentType.EXERCISE;
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
        /// Returns the number of the chapter or 0 if not found or invalid.
        /// </summary>
        /// <returns></returns>
        public int GetChapterNumber()
        {
            string sChapterNo = _headers.Where(kvp => kvp.Key == PgnHeaders.KEY_CHAPTER_ID).FirstOrDefault().Value;
            if (int.TryParse(sChapterNo, out int chapterNumber))
            {
                return chapterNumber;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns the value of header with a given name.
        /// Null if header not found.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetHeaderValue(string name)
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

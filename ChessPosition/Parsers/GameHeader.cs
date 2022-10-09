using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ChessPosition.GameTree
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
        /// Returns the title of the Workbook
        /// </summary>
        /// <returns></returns>
        public string GetWorkbookTitle()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.NAME_WORKBOOK_TITLE).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the title of the chapter.
        /// </summary>
        /// <returns></returns>
        public string GetChapterTitle()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.NAME_CHAPTER_TITLE).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the value of the "White" header.
        /// </summary>
        /// <returns></returns>
        public string GetWhitePlayer(out string key, string value = null)
        {
            string headerKey = PgnHeaders.NAME_WHITE;
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
            string headerKey = PgnHeaders.NAME_BLACK;
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
            string headerKey = PgnHeaders.NAME_DATE;
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
        /// Returns the result of a tree/game/combinattion
        /// </summary>
        /// <returns></returns>
        public string GetResult(out string key)
        {
            string headerKey = PgnHeaders.NAME_RESULT;
            key = headerKey;

            string value = _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
            if (string.IsNullOrEmpty(value))
            {
                value = "*";
            }

            return value;
        }

        /// <summary>
        /// Returns the training side value.
        /// </summary>
        /// <returns></returns>
        public string GetChapterId()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.NAME_CHAPTER_ID).FirstOrDefault().Value;
        }


        /// <summary>
        /// Returns the Legacy Title value.
        /// </summary>
        /// <returns></returns>
        public string GetLegacyTitle()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.NAME_LEGACY_TITLE).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the training side value.
        /// </summary>
        /// <returns></returns>
        public string GetTrainingSide(out string key)
        {
            string headerKey = PgnHeaders.NAME_TRAINING_SIDE;

            key = headerKey;
            return _headers.Where(kvp => kvp.Key == headerKey).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the Content Type value.
        /// </summary>
        /// <returns></returns>
        public string GetContentType(out string key, string value = null)
        {
            string headerKey = PgnHeaders.NAME_CONTENT_TYPE;
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
        /// Returns the FEN string from the header.
        /// </summary>
        /// <returns></returns>
        public string GetFenString()
        {
            return _headers.Where(kvp => kvp.Key == PgnHeaders.NAME_FEN).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the number of the chapter or 0 if not found or invalid.
        /// </summary>
        /// <returns></returns>
        public int GetChapterNumber()
        {
            string sChapterNo = _headers.Where(kvp => kvp.Key == PgnHeaders.NAME_CHAPTER_ID).FirstOrDefault().Value;
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
        /// Saves header's name and value. 
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

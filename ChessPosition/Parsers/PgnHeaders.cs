using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ChessPosition.Parsers
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
        private const string TITLE = "ChessForgeWorkbook";

        /// <summary>
        /// Determines which side is the default training side in 
        /// the Workbook. The value can be "White", "Black" or "None".
        /// This is part of the first set of headers (i.e. for the entire Workbook)
        /// only.
        /// </summary>
        private const string TRAINING_SIDE = "TrainingSide";

        /// <summary>
        /// In Chess Forge this header is repurposed as a chapter's title.
        /// </summary>
        private const string EVENT = "Event";

        /// <summary>
        /// The number of a chapter. The same number may appear in multiple
        /// Variation Trees thus organizing them into chapters.
        /// </summary>
        private const string CHAPTER_NUMBER = "ChapterNumber";

        /// <summary>
        /// Basically, the same meaning as in the standard PGN.
        /// It will be "*" for the chapter's variation tree or
        /// game result for games and combinations.
        /// </summary>
        private const string RESULT = "Result";

        /// <summary>
        /// Has no meaning in ChessForge but will be included in
        /// output files to keep some PGN viewers happy
        /// </summary>
        private const string WHITE = "White";

        /// <summary>
        /// Has no meaning in ChessForge but will be included in
        /// output files to keep some PGN viewers happy
        /// </summary>
        private const string BLACK = "Black";

        /// <summary>
        /// Saves header's name and value. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetHeaderVlaue(string name, string value)
        {
            var header = _headers.Where(kvp => kvp.Key == name).FirstOrDefault();
            if (!header.Equals(default(KeyValuePair<string, string>)))
            {
                _headers.Remove(header);
            }

            header = new KeyValuePair<string, string>(name, value);
            _headers.Add(header);
        }

        /// <summary>
        /// Returns the title of the Workbook
        /// </summary>
        /// <returns></returns>
        public string GetWorkbookTitle()
        {
            return _headers.Where(kvp => kvp.Key == TITLE).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the title of the chapter.
        /// </summary>
        /// <returns></returns>
        public string GetChapterTitle()
        {
            return _headers.Where(kvp => kvp.Key == EVENT).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the result of a tree/game/combinattion
        /// </summary>
        /// <returns></returns>
        public string GetResult()
        {
            return _headers.Where(kvp => kvp.Key == RESULT).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the training side value.
        /// </summary>
        /// <returns></returns>
        public string GetTrainingSide()
        {
            return _headers.Where(kvp => kvp.Key == TRAINING_SIDE).FirstOrDefault().Value;
        }

        /// <summary>
        /// Returns the number of the chapter or 0 if not found or invalid.
        /// </summary>
        /// <returns></returns>
        public int GetChapterNumber()
        {
            string sChapterNo = _headers.Where(kvp => kvp.Key == CHAPTER_NUMBER).FirstOrDefault().Value;
            if (int.TryParse(sChapterNo, out int chapterNumber))
            {
                return chapterNumber;
            }
            else
            {
                return 0;
            }
        }
    }
}

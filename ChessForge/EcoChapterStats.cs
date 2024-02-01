using System;
using System.Collections.Generic;
using System.Text;

namespace ChessForge
{
    /// <summary>
    /// Holds statistics of ECO codes in a chapter.
    /// For more convenient and faster processing,
    /// ECO's are coverted to HEX numbers.
    /// </summary>
    public class EcoChapterStats
    {
        // chapter for which the stats are collected
        private Chapter _chapter;

        // min ECO found in the chapter (-1 if no games with ECO) 
        private int _minEco = -1;

        // max ECO found in the chapter (-1 if no games with ECO) 
        private int _maxEco = -1;

        // holds ECO as keys with number of instances as values.
        private Dictionary<int, int> _dictEcoCounts = new Dictionary<int, int>();

        // games that have been assigned to this chapter after analysis
        private List<ArticleListItem> _games = new List<ArticleListItem>();

        // whether this is the chapter that we are splitting
        private bool _isSourceChapter;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="chapter"></param>
        public EcoChapterStats(Chapter chapter, bool sourceChapter)
        {
            _chapter = chapter;
            _isSourceChapter = sourceChapter;
        }

        /// <summary>
        /// Add a game's ECO to the stats.
        /// </summary>
        /// <param name="eco"></param>
        public void AddEco(string eco)
        {
            int nEco = EcoToInt(eco);
            if (nEco > 0)
            {
                if (nEco < _minEco || _minEco == -1)
                {
                    _minEco = nEco;
                }
                if (nEco > _maxEco)
                {
                    _maxEco = nEco;
                }
                if (!_dictEcoCounts.ContainsKey(nEco))
                {
                    _dictEcoCounts[nEco] = 0;
                }
                _dictEcoCounts[nEco]++;
            }
        }

        /// <summary>
        /// Associate a game with this chapter.
        /// </summary>
        /// <param name="game"></param>
        public void AddGame(ArticleListItem game)
        {
            if (game != null)
            {
                _games.Add(game);
            }
        }

        /// <summary>
        /// Returns the difference between the max and min ECO found in this chapter.
        /// </summary>
        public int EcoRange
        {
            get
            {
                return _maxEco - _minEco;
            }
        }

        /// <summary>
        /// List of games captured in this object.
        /// </summary>
        public List<ArticleListItem> Games
        {
            get
            {
                return _games;
            }
        }

        /// <summary>
        /// Chapter for which this object has been created
        /// </summary>
        public Chapter Chapter
        {
            get { return _chapter; }
        }

        /// <summary>
        /// Number of games with the passed ECO
        /// found in the chapter.
        /// </summary>
        /// <param name="eco"></param>
        /// <returns></returns>
        public int GetEcoCount(int eco)
        {
            if (_dictEcoCounts.ContainsKey(eco))
            {
                return _dictEcoCounts[eco];
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns true if the passed ECO falls within the range
        /// found in this chapter.
        /// </summary>
        /// <param name="eco"></param>
        /// <returns></returns>
        public bool IsEcoInRange(int eco)
        {
            return (eco >= _minEco && eco <= _maxEco);
        }

        /// <summary>
        /// Converts an ECO string to a value interpreting
        /// ECO as a Hex number.
        /// </summary>
        /// <param name="eco"></param>
        /// <returns></returns>
        public static int EcoToInt(string eco)
        {
            int num = -1;
            if (!string.IsNullOrEmpty(eco) && eco.Length == 3)
            {
                try
                {
                    num = Int32.Parse(eco, System.Globalization.NumberStyles.HexNumber);
                }
                catch
                {
                    num = -1;
                }
            }

            return num;
        }
    }
}

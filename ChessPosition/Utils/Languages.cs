using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace ChessPosition
{
    /// <summary>
    /// Handles GUI language settings.
    /// </summary>
    public class Languages
    {
        /// <summary>
        /// The list of supported languages and their culture codes
        /// It is initialized from MainWindows.xaml.cs when the program starts.
        /// </summary>
        public static List<Language> AvailableLanguages = new List<Language>();

        /// <summary>
        /// Current mapping of the Chess Symbols set when setting the language.
        /// </summary>
        public static Dictionary<char, char> ChessSymbolMapping = new Dictionary<char, char>();

        /// <summary>
        /// Reverse mapping of chess symbols i.e. from the local language to the PGN (i.e. English)
        /// </summary>
        public static Dictionary<char, char> ReverseChessSymbolMapping = new Dictionary<char, char>();

        /// <summary>
        /// Whether to use Unicode symbols
        /// </summary>
        public static bool UseFigurines = false;

        /// <summary>
        /// Mapping of Unicode chess symbols.
        /// </summary>
        public static Dictionary<char, char> WhiteFigurinesMapping = new Dictionary<char, char>()
        {
            ['K'] = '♚',
            ['Q'] = '♕',
            ['R'] = '♖',
            ['B'] = '♗',
            ['N'] = '♘',
        };

        /// <summary>
        /// Reverse mapping of Unicode chess symbols
        /// </summary>
        public static Dictionary<char, char> ReverseWhiteFigurinesMapping = new Dictionary<char, char>();

        /// <summary>
        /// Mapping of Unicode chess symbols.
        /// </summary>
        public static Dictionary<char, char> BlackFigurinesMapping = new Dictionary<char, char>()
        {
            ['K'] = '♚',
            ['Q'] = '♛',
            ['R'] = '♜',
            ['B'] = '♝',
            ['N'] = '♞',
        };

        /// <summary>
        /// Reverse mapping of Unicode chess symbols
        /// </summary>
        public static Dictionary<char, char> ReverseBlackFigurinesMapping = new Dictionary<char, char>();

        /// <summary>
        /// Whether the default mapping is used.
        /// If so the callers do not need to call MapPieceSymbols
        /// before displaying moves. 
        /// </summary>
        public static bool IsDefaultMapping = true;

        // Number of symbols expected in the mapping string
        private static readonly int SYMBOL_COUNT = 5;

        //Unicode symbols for White Pieces
        private static readonly string UnicodeWhitePieces = "♔♕♖♗♘";

        //Unicode symbols for Black Pieces
        private static readonly string UnicodeBlackPieces = "♚♛♜♝♞";

        /// <summary>
        /// Saves the session language based in the passed string.
        /// If it is empty, no language has been selected and the program uses
        /// the default which is the system language if it is one of the supported
        /// languages or English.
        /// </summary>
        /// <param name="code"></param>
        public static void SetSessionLanguage(string code)
        {
            // code is a string from config so it should match one of the configured languages ... but it may not
            // first check if we have a full match.
            foreach (var language in Languages.AvailableLanguages)
            {
                language.IsSelected = language.Code == code;
            }
        }

        /// <summary>
        /// Adds a language to the list.
        /// This is called from MainWindows.xaml.cs when the program starts, for each
        /// supported language.
        /// It is used to display the language names in the GUI
        /// for the user to choose from.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="name"></param>
        public static void AddLanguage(string code, string name)
        {
            Language language = new Language();
            language.Code = code;
            language.Name = name;

            AvailableLanguages.Add(language);
        }

        /// <summary>
        /// Method to replace upper case letter with mapped letters (courtesy ChatGPT :)).
        /// Used to replace PGN piece symbols with localized ones.
        /// </summary>
        /// <param name="inputString"></param>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public static string MapPieceSymbols(string inputString, PieceColor color = PieceColor.None)
        {
            if (!UseFigurines)
            {
                return Regex.Replace(inputString, "[A-Z]", m =>
                {
                    char c = m.Value[0];
                    return ChessSymbolMapping.ContainsKey(c) ? ChessSymbolMapping[c].ToString() : c.ToString();
                });
            }
            else
            {
                if (color == PieceColor.Black)
                {
                    return Regex.Replace(inputString, "[A-Z]", m =>
                    {
                        char c = m.Value[0];
                        return BlackFigurinesMapping.ContainsKey(c) ? BlackFigurinesMapping[c].ToString() : c.ToString();
                    });
                }
                else
                {
                    return Regex.Replace(inputString, "[A-Z]", m =>
                    {
                        char c = m.Value[0];
                        return WhiteFigurinesMapping.ContainsKey(c) ? WhiteFigurinesMapping[c].ToString() : c.ToString();
                    });
                }
            }
        }

        /// <summary>
        /// Initializes the symbols map.
        /// Invoked from MainWindows.xaml.cs when the program starts.
        /// </summary>
        /// <param name="symbols"></param>
        public static void InitializeChessSymbolMapping(string symbols)
        {
            if (symbols.Length != SYMBOL_COUNT)
            {
                SetDefaultMapping();
            }
            else
            {
                IsDefaultMapping = false;
                ChessSymbolMapping.Clear();
                ReverseChessSymbolMapping.Clear();

                ChessSymbolMapping['K'] = symbols[0];
                ChessSymbolMapping['Q'] = symbols[1];
                ChessSymbolMapping['R'] = symbols[2];
                ChessSymbolMapping['B'] = symbols[3];
                ChessSymbolMapping['N'] = symbols[4];

                ReverseChessSymbolMapping[symbols[0]] = 'K';
                ReverseChessSymbolMapping[symbols[1]] = 'Q';
                ReverseChessSymbolMapping[symbols[2]] = 'R';
                ReverseChessSymbolMapping[symbols[3]] = 'B';
                ReverseChessSymbolMapping[symbols[4]] = 'N';
            }
        }

        /// <summary>
        /// Sets the default, "dummy" mapping.
        /// </summary>
        public static void SetDefaultMapping()
        {
            IsDefaultMapping = true;

            ChessSymbolMapping['K'] = 'K';
            ChessSymbolMapping['Q'] = 'Q';
            ChessSymbolMapping['R'] = 'R';
            ChessSymbolMapping['B'] = 'B';
            ChessSymbolMapping['N'] = 'N';
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
        public static Dictionary<string, string> AvailableLanguages = new Dictionary<string, string>();

        /// <summary>
        /// Current mapping of the Chess Symbols set when setting the language.
        /// </summary>
        public static Dictionary<char, char> ChessSymbolMapping = new Dictionary<char, char>();

        /// <summary>
        /// Whether the default mapping is used.
        /// If so the callers do not need to call MapPieceSymbols
        /// before displaying moves. 
        /// </summary>
        public static bool IsDefaultMapping = true;

        // Number of symbols expected in the mapping string
        private static readonly int SYMBOL_COUNT = 5;

        /// <summary>
        /// Adds a language to the list.
        /// This is called from MainWindows.xaml.cs when the program starts, for each
        /// supported language.
        /// It is used to display the langauge names in the GUI
        /// for the user to choose from.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="name"></param>
        public static void AddLanguage(string code, string name)
        {
            AvailableLanguages.Add(code, name);
        }

        /// <summary>
        /// Method to replace upper case letter with mapped letters (courtesy ChatGPT :)).
        /// Used to replace PGN piece symbols with localized ones.
        /// </summary>
        /// <param name="inputString"></param>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public static string MapPieceSymbols(string inputString)
        {
            return Regex.Replace(inputString, "[A-Z]", m =>
            {
                char c = m.Value[0];
                return ChessSymbolMapping.ContainsKey(c) ? ChessSymbolMapping[c].ToString() : c.ToString();
            });
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

                ChessSymbolMapping['K'] = symbols[0];
                ChessSymbolMapping['Q'] = symbols[1];
                ChessSymbolMapping['R'] = symbols[2];
                ChessSymbolMapping['B'] = symbols[3];
                ChessSymbolMapping['N'] = symbols[4];
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

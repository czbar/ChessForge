using System;
using System.Collections.Generic;
using static ChessForge.SoundPlayer;
using System.Text;

namespace ChessForge
{
    /// <summary>
    /// Configuration items for the RTF export.
    /// </summary>
    public class ConfigurationRtfExport
    {
        /// <summary>
        /// Prefix idetifying a config item as pertaining to RTF Export.
        /// </summary>
        public const string ItemPrefix = "RtfExport_";


        /// <summary>
        /// A string representing Workbook Scope in the config file.
        /// </summary>
        public static string WorkbookScopeCoded = "1";

        /// <summary>
        /// A string representing Chapter Scope in the config file.
        /// </summary>
        public static string ChapterScopeCoded = "2";

        /// <summary>
        /// A string representing Article Scope in the config file.
        /// </summary>
        public static string ArticleScopeCoded = "3";

        /// <summary>
        /// Gets currently configured PrintScope. 
        /// </summary>
        /// <returns></returns>
        public static PrintScope GetScope()
        {
            string scopeString = GetStringValue(CFG_SCOPE);

            return GetScopeFromString(scopeString);
        }

        /// <summary>
        /// Converts encoded scope into PrintScope enum.
        /// </summary>
        /// <param name="scopeString"></param>
        /// <returns></returns>
        public static PrintScope GetScopeFromString(string scopeString)
        {
            PrintScope scope = PrintScope.WORKBOOK;

            if (scopeString == ConfigurationRtfExport.ChapterScopeCoded)
            {
                scope = PrintScope.CHAPTER;
            }
            else if (scopeString == ConfigurationRtfExport.ArticleScopeCoded)
            {
                scope = PrintScope.ARTICLE;
            }

            return scope;
        }

        //*********************************
        // CONFIGURATION ITEM NAMES
        //*********************************

        /// <summary>
        /// Scope of the export: Workbook, Chapter or Current View (which is ActiveArticle or Contents.
        /// </summary>
        public const string CFG_SCOPE = ItemPrefix + "Scope";

        /// <summary>
        /// Whether to include Contents table.
        /// </summary>
        public const string INCLUDE_CONTENTS = ItemPrefix + "IncludeContents";

        /// <summary>
        /// Whether to include Games index.
        /// </summary>
        public const string INCLUDE_GAME_INDEX = ItemPrefix + "IncludeGameIndex";

        /// <summary>
        /// Whether to include Exercise Index.
        /// </summary>
        public const string INCLUDE_EXERCISE_INDEX = ItemPrefix + "IncludeExerciseIndex";

        /// <summary>
        /// Whether to include Intro.
        /// </summary>
        public const string INCLUDE_INTRO = ItemPrefix + "IncludeIntro";

        /// <summary>
        /// Whether to include Study.
        /// </summary>
        public const string INCLUDE_STUDY = ItemPrefix + "IncludeStudy";

        /// <summary>
        /// Whether to include Games.
        /// </summary>
        public const string INCLUDE_GAMES = ItemPrefix + "IncludeGames";

        /// <summary>
        /// Whether to include Exercises.
        /// </summary>
        public const string INCLUDE_EXERCISES = ItemPrefix + "IncludeExercises";

        /// <summary>
        /// Whether to use 2 column format in Intro.
        /// </summary>
        public const string TWO_COLUMN_INTRO = ItemPrefix + "TwoColsIntro";

        /// <summary>
        /// Whether to use 2 column format in Study.
        /// </summary>
        public const string TWO_COLUMN_STUDY = ItemPrefix + "TwoColsStudy";

        /// <summary>
        /// Whether to use 2 column format in Games.
        /// </summary>
        public const string TWO_COLUMN_GAMES = ItemPrefix + "TwoColsGames";

        /// <summary>
        /// Whether to use 2 column format in Exercises.
        /// </summary>
        public const string TWO_COLUMN_EXERCISES = ItemPrefix + "TwoColsExercises";

        /// <summary>
        /// Whether to a use a custom term for Study
        /// </summary>
        public const string USE_CUSTOM_STUDY = ItemPrefix + "UseCustomStudy";

        /// <summary>
        /// Whether to a use a custom term for Games
        /// </summary>
        public const string USE_CUSTOM_GAMES = ItemPrefix + "UseCustomGames";

        /// <summary>
        /// Whether to a use a custom term for Game
        /// </summary>
        public const string USE_CUSTOM_GAME = ItemPrefix + "UseCustomGame";

        /// <summary>
        /// Whether to a use a custom term for Exercises
        /// </summary>
        public const string USE_CUSTOM_EXERCISES = ItemPrefix + "UseCustomExercises";

        /// <summary>
        /// Whether to a use a custom term for Exercise
        /// </summary>
        public const string USE_CUSTOM_EXERCISE = ItemPrefix + "UseCustomExercise";

        /// <summary>
        /// The custom term for Study
        /// </summary>
        public const string CUSTOM_TERM_STUDY = ItemPrefix + "CustomTermStudy";

        /// <summary>
        /// The custom term for Games
        /// </summary>
        public const string CUSTOM_TERM_GAMES = ItemPrefix + "CustomTermGames";

        /// <summary>
        /// The custom term for Game
        /// </summary>
        public const string CUSTOM_TERM_GAME = ItemPrefix + "CustomTermGame";

        /// <summary>
        /// The custom term for Exercises
        /// </summary>
        public const string CUSTOM_TERM_EXERCISES = ItemPrefix + "CustomTermExercises";

        /// <summary>
        /// The custom term for Exercise
        /// </summary>
        public const string CUSTOM_TERM_EXERCISE = ItemPrefix + "CustomTermExercise";

        /// <summary>
        /// Whether to include the FEN string under diagrams.
        /// </summary>
        public const string FEN_UNDER_DIAGRAMS = ItemPrefix + "FenUnderDiagrams";

        // the dictionary of configuration items
        private static Dictionary<string, string> _rtfConfigItems = new Dictionary<string, string>();

        /// <summary>
        /// Gets the string value of a given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string GetStringValue(string item)
        {
            if (_rtfConfigItems.ContainsKey(item))
            {
                return _rtfConfigItems[item];
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Gets the bool value representation of a given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool GetBoolValue(string item)
        {
            if (_rtfConfigItems.ContainsKey(item))
            {
                return _rtfConfigItems[item] == "1";
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Sets string value of an item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="value"></param>
        public static void SetValue(string item, string value)
        {
            _rtfConfigItems[item] = value;
        }

        /// <summary>
        /// Sets bool value of an item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="value"></param>
        public static void SetValue(string item, bool value)
        {
            _rtfConfigItems[item] = value ? "1" : "0";
        }

        /// <summary>
        /// Initializes the dictionary of configuration items.
        /// </summary>
        public static void InitializeRtfConfig()
        {
            _rtfConfigItems[CFG_SCOPE] = "1";

            _rtfConfigItems[INCLUDE_CONTENTS] = "1";
            _rtfConfigItems[INCLUDE_GAME_INDEX] = "1";
            _rtfConfigItems[INCLUDE_EXERCISE_INDEX] = "1";

            _rtfConfigItems[INCLUDE_INTRO] = "1";
            _rtfConfigItems[INCLUDE_STUDY] = "1";
            _rtfConfigItems[INCLUDE_GAMES] = "1";
            _rtfConfigItems[INCLUDE_EXERCISES] = "1";

            _rtfConfigItems[TWO_COLUMN_INTRO] = "1";
            _rtfConfigItems[TWO_COLUMN_STUDY] = "1";
            _rtfConfigItems[TWO_COLUMN_GAMES] = "1";
            _rtfConfigItems[TWO_COLUMN_EXERCISES] = "1";

            _rtfConfigItems[USE_CUSTOM_STUDY] = "0";
            _rtfConfigItems[USE_CUSTOM_GAMES] = "0";
            _rtfConfigItems[USE_CUSTOM_GAME] = "0";
            _rtfConfigItems[USE_CUSTOM_EXERCISES] = "0";
            _rtfConfigItems[USE_CUSTOM_EXERCISE] = "0";

            _rtfConfigItems[CUSTOM_TERM_STUDY] = "";
            _rtfConfigItems[CUSTOM_TERM_GAMES] = "";
            _rtfConfigItems[CUSTOM_TERM_GAME] = "";
            _rtfConfigItems[CUSTOM_TERM_EXERCISES] = "";
            _rtfConfigItems[CUSTOM_TERM_EXERCISE] = "";

            _rtfConfigItems[FEN_UNDER_DIAGRAMS] = "0";
        }

        /// <summary>
        /// Appends all items to the ChessForge's configuration file text.
        /// </summary>
        /// <param name="sb"></param>
        public static void AppendToConfigurationFile(StringBuilder sb)
        {
            try
            {
                foreach (var item in _rtfConfigItems)
                {
                    sb.Append(item.Key + "=" + (item.Value ?? "") + Environment.NewLine);
                }
            }
            catch { }
        }

        /// <summary>
        /// Process a single configuration name/value pair
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void ProcessConfigurationItem(string name, string value)
        {
            if (_rtfConfigItems.ContainsKey(name))
            {
                _rtfConfigItems[name] = value;
            }
        }

    }
}

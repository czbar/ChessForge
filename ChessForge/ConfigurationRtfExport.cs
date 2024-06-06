using System.Collections.Generic;

namespace ChessForge
{
    public class ConfigurationRtfExport
    {
        /// <summary>
        /// Prefix idetifying a config item as pertaining to RTF Export.
        /// </summary>
        public const string ItemPrefix = "RtfExport_";


        //*********************************
        // CONFIGURATION ITEM NAMES
        //*********************************

        /// <summary>
        /// Scope of the export: Workbook, Chapter or Current View (which is ActiveArticle or Contents.
        /// </summary>
        public const string CFG_SCOPE = "MoveSpeed";

        /// <summary>
        /// Whether to include Contents table.
        /// </summary>
        public const string INCLUDE_CONTENTS = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// Whether to include Games index.
        /// </summary>
        public const string INCLUDE_GAMES_INDEX = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// Whether to include Exercise Index.
        /// </summary>
        public const string INCLUDE_EXRCISE_INDEX = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// Whether to include Intro.
        /// </summary>
        public const string INCLUDE_INTRO = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// Whether to include Study.
        /// </summary>
        public const string INCLUDE_STUDY = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// Whether to include Games.
        /// </summary>
        public const string INCLUDE_GAMES = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// Whether to include Exercises.
        /// </summary>
        public const string INCLUDE_EXERCISES = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// Whether to use 2 column format in Intro.
        /// </summary>
        public const string TWO_COLUMN_INTRO = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// Whether to use 2 column format in Study.
        /// </summary>
        public const string TWO_COLUMN_STUDY = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// Whether to use 2 column format in Games.
        /// </summary>
        public const string TWO_COLUMN_GAMES = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// Whether to use 2 column format in Exercises.
        /// </summary>
        public const string TWO_COLUMN_EXERCISES = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// Whether to a use a custom term for Study
        /// </summary>
        public const string USE_CUSTOM_STUDY = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// Whether to a use a custom term for Games
        /// </summary>
        public const string USE_CUSTOM_GAMES = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// Whether to a use a custom term for Game
        /// </summary>
        public const string USE_CUSTOM_GAME = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// Whether to a use a custom term for Exercises
        /// </summary>
        public const string USE_CUSTOM_EXERCISES = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// Whether to a use a custom term for Exercise
        /// </summary>
        public const string USE_CUSTOM_EXERCISE = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// The custom term for Study
        /// </summary>
        public const string CUSTOM_TERM_STUDY = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// The custom term for Games
        /// </summary>
        public const string CUSTOM_TERM_GAMES = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// The custom term for Game
        /// </summary>
        public const string CUSTOM_TERM_GAME = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// The custom term for Exercises
        /// </summary>
        public const string CUSTOM_TERM_EXERCISES = ItemPrefix + "DefaultIndexDepth";

        /// <summary>
        /// The custom term for Exercise
        /// </summary>
        public const string CUSTOM_TERM_EXERCISE = ItemPrefix + "DefaultIndexDepth";

        // the dictionary of configuration items
        private static Dictionary<string, string> _rtfConfigItems = new Dictionary<string, string>();

        // whether the dictionary has been initilized
        private static bool _initialized = false;

        /// <summary>
        /// Gets the string value of a given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public string GetStringValue(string item)
        {
            if (!_initialized)
            {
                InitializeRtfConfig();
            }

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
        public bool GetBoolValue(string item)
        {
            if (!_initialized)
            {
                InitializeRtfConfig();
            }

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
        public void SetValue(string item, string value)
        {
            _rtfConfigItems[item] = value;
        }

        /// <summary>
        /// Sets bool value of an item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="value"></param>
        public void SetValue(string item, bool value)
        {
            _rtfConfigItems[item] = value ? "1" : "0";
        }

        /// <summary>
        /// Initializes the dictionary of configuration items.
        /// </summary>
        private static void InitializeRtfConfig()
        {
            _rtfConfigItems[INCLUDE_CONTENTS] = "1";
            _rtfConfigItems[INCLUDE_GAMES_INDEX] = "1";
            _rtfConfigItems[INCLUDE_EXRCISE_INDEX] = "1";

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

            _initialized = true;
        }

    }
}

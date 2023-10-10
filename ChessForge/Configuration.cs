using ChessPosition;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using static ChessForge.ChessBoards;

namespace ChessForge
{
    public class Configuration
    {
        //*********************************
        //
        //   SPECIAL/SYSTEM DATA
        //
        //*********************************

        /// <summary>
        /// Physical memory on the system.
        /// </summary>
        public static long TotalMemory;

        /// <summary>
        /// Number of processor cores in the system.
        /// </summary>
        public static int CoreCount;


        //*********************************
        //
        //   CONFIGURATION ITEMS
        //
        //*********************************

        /// <summary>
        /// Last directory from which a Workbook PGN file was read.
        /// </summary>
        public static string LastOpenDirectory = "";

        /// <summary>
        /// Last directory from which a a PGN file for import was read.
        /// </summary>
        public static string LastImportDirectory = "";

        /// <summary>
        /// Last read Workbook file.
        /// </summary>
        public static string LastWorkbookFile = "";

        /// <summary>
        /// Path to the engine executable
        /// </summary>
        public static string EngineExePath = "";

        /// <summary>
        /// Version for which not show the update alert
        /// </summary>
        public static string DoNotShowVersion = "";

        /// <summary>
        /// Culture name, if not using System's
        /// </summary>
        public static string CultureName = "";

        /// <summary>
        /// The time in milliseconds that it takes
        /// to animate a move on the board
        /// </summary>
        public static int MoveSpeed = 200;

        /// <summary>
        /// Depth of the study tree automatically built
        /// from the games of the chapter or the downloaded list.
        /// </summary>
        public static uint AutogenTreeDepth
        {
            get => _autogenTreeDepth;
            set => _autogenTreeDepth = value;
        }

        /// <summary>
        /// Evaluation drop triggering detection 
        /// </summary>
        public static uint BlunderDetectEvalDrop = 200;

        /// <summary>
        /// Threshold beyond which blunders are ignored.
        /// </summary>
        public static uint BlunderNoDetectThresh = 500;

        /// <summary>
        /// By how many pixels to adjust font size
        /// in the views.
        /// (in milliseconds)
        /// </summary>
        public static int FontSizeDiff = 0;

        /// <summary>
        /// How often AutoSave is called
        /// (in seconds)
        /// </summary>
        public static int AutoSaveFrequency = 60;

        /// <summary>
        /// Time given to the engine to evaluate a single move
        /// (in milliseconds)
        /// </summary>
        public static int EngineEvaluationTime
        {
            get => Math.Max(100, _engineEvaluationTime);
            set => _engineEvaluationTime = value;
        }

        /// <summary>
        /// Time given to the engine to respond
        /// during a training game.
        /// (in milliseconds)
        /// </summary>
        public static int EngineMoveTime
        {
            get => Math.Max(100, _engineMoveTime);
            set => _engineMoveTime = value;
        }

        // Max allowed number of options to return
        public const int MAX_ENGINE_MPV = 20;

        // Default number of options to return
        private const int DEFAULT_ENGIME_MPV = 5;


        //***************** WEB GAMES ITEMS *************************

        /// <summary>
        /// Site to download the games from: lichess or chesscom
        /// </summary>
        public static string WebGamesSite = Constants.LichessNameId;

        /// <summary>
        /// User name for the lichess site
        /// </summary>
        public static string WebGamesLichessUser = "";

        /// <summary>
        /// User name for the chess.com site
        /// </summary>
        public static string WebGamesChesscomUser = "";

        /// <summary>
        /// User name for the chess.com site
        /// </summary>
        public static int WebGamesMaxCount = 50;

        /// <summary>
        /// Whether to download the most recent games 
        /// or use specified dates.
        /// </summary>
        public static bool WebGamesMostRecent = true;

        /// <summary>
        /// Start date for downloading games
        /// </summary>
        public static DateTime? WebGamesStartDate = null;

        /// <summary>
        /// End date for downloading games.
        /// </summary>
        public static DateTime? WebGamesEndDate = null;

        // time per move for engine evaluation
        private static int _engineEvaluationTime = 1000;

        // time per move in engine game
        private static int _engineMoveTime = 1000;

        /// <summary>
        /// Number of threads for the engine to use. 
        /// </summary>
        public static int EngineThreads
        {
            get
            {
                if (_engineThreads > 0)
                {
                    return _engineThreads;
                }
                else
                {
                    return Math.Max((int)(CoreCount / 2), 1);
                }
            }
            set
            {
                _engineThreads = Math.Max(1, value);
            }
        }

        /// <summary>
        /// The size in MB of the engine's hash table memory
        /// </summary>
        public static long EngineHashSize
        {
            get
            {
                long ret;
                if (_engineHashSize > 0)
                {
                    ret = _engineHashSize;
                }
                else
                {
                    if (TotalMemory > 0)
                    {
                        ret = (long)(((double)TotalMemory / (double)(1024 * 1024)) / (double)5);
                    }
                    else
                    {
                        ret = 16; // stockfish default
                    }
                }
                return Math.Max(1, ret);
            }
            set
            {
                _engineHashSize = Math.Max(1, value);
            }
        }


        /// <summary>
        /// When choosing "viable" repsonsed from the engine
        /// during a game, moves under consideration must not
        /// be worse than by this centipawn value from the
        /// best move.
        /// </summary>
        public static int ViableMoveCpDiff
        {
            get
            {
                return Math.Max(0, _viableMoveCpDiff);
            }
            set
            {
                _viableMoveCpDiff = Math.Max(0, value);
            }
        }

        /// <summary>
        /// Number of moves to return with evaluations.
        /// </summary>
        public static int EngineMpv
        {
            get
            {
                if (_engineMpv < 1)
                {
                    _engineMpv = 1;
                }
                else
                {
                    _engineMpv = Math.Min(MAX_ENGINE_MPV, _engineMpv);
                }
                return _engineMpv;
            }
            set { _engineMpv = value; }
        }

        /// <summary>
        /// Whether the main window was maximized
        /// when Chess Forge closed last time.
        /// </summary>
        public static bool MainWinMaximized = false;

        /// <summary>
        /// Whether to show the generic PGN file info
        /// when opening a non-Chess Forge file.
        /// </summary>
        public static bool ShowGenericPgnInfo = true;

        /// <summary>
        /// Whether to show move options at a fork.
        /// </summary>
        public static bool ShowMovesAtFork = true;

        /// <summary>
        /// Whether to show the Explorers.
        /// </summary>
        public static bool ShowExplorers = true;

        /// <summary>
        /// Whether to show the Intro tab.
        /// </summary>
        public static bool ShowIntroTab = false;

        /// <summary>
        /// Whether AutoSave is On.
        /// </summary>
        public static bool AutoSave = false;

        /// <summary>
        /// Whether sound in turned on
        /// </summary>
        public static bool SoundOn = true;

        /// <summary>
        /// Whether to use figurines in chess notation
        /// </summary>
        public static bool UseFigurines = false;

        /// <summary>
        /// Whether fixed size font should be used.
        /// </summary>
        public static bool UseFixedFont = false;

        /// <summary>
        /// Whether to allow replaying moves with the mouse wheel. 
        /// </summary>
        public static bool AllowMouseWheelForMoves = false;

        /// <summary>
        /// Whether to include bookmark locations in the PGN export
        /// </summary>
        public static bool PgnExportBookmarks = true;

        /// <summary>
        /// Whether to include evaluations in the PGN export
        /// </summary>
        public static bool PgnExportEvaluations = true;

        /// <summary>
        /// Debug message level.
        /// 0 - no debug messaging of logging
        /// 1 - logging for the app and the engine is enabled.
        /// 2 - messages boxes may pop up when some errors or exceptions ar caught
        /// 3 - some heavy debug tools are enabled (e.g. an extra button in Position Setup to show the current setup in the main window)
        /// </summary>
        public static int DebugLevel = 1;

        // depth of the auto-generated tree
        private static uint _autogenTreeDepth = 12;

        // allowed diff between the chosen engine move and the best move
        private static int _viableMoveCpDiff = 50;

        // the size of engine hash table in MB
        private static long _engineHashSize = 0;

        // number of allowed threads
        private static int _engineThreads = 0;

        // number of options to return
        private static int _engineMpv = DEFAULT_ENGIME_MPV;

        // max value by which a font size can be increased from the standard size
        private const int MAX_UP_FONT_SIZE_DIFF = 4;

        // max value by which a font size can be decreased from the standard size
        private const int MAX_DOWN_FONT_SIZE_DIFF = -2;

        /// <summary>
        /// Board sets for the Study.
        /// </summary>
        public static BoardSet StudyBoardSet = ChessBoards.BoardSets[ColorSet.BLUE];

        /// <summary>
        /// Board sets for Games.
        /// </summary>
        public static BoardSet GameBoardSet = ChessBoards.BoardSets[ColorSet.LIGHT_BLUE];

        /// <summary>
        /// Board sets for Exercises.
        /// </summary>
        public static BoardSet ExerciseBoardSet = ChessBoards.BoardSets[ColorSet.LIGH_GREEN];

        //*********************************
        // CONFIGURATION ITEM NAMES
        //*********************************

        private const string CFG_MOVE_SPEED = "MoveSpeed";
        private const string CFG_AUTOGEN_TREE_DEPTH = "AutogenTreeDepth";
        private const string CFG_LAST_DIRECTORY = "LastDirectory";
        private const string CFG_LAST_IMPORT_DIRECTORY = "LastImportDirectory";
        private const string CFG_LAST_FILE = "LastFile";
        private const string CFG_RECENT_FILES = "RecentFiles";
        private const string CFG_MAIN_WINDOW_POS = "MainWindowPosition";
        private const string CFG_ENGINE_EXE = "EngineExe";
        private const string CFG_DO_NOT_SHOW_VERSION = "DoNotShowVersion";
        private const string CFG_CULTURE_NAME = "CultureName";

        /// <summary>
        /// Time the engine has to make a move in a training game
        /// </summary>
        private const string CFG_ENGINE_MOVE_TIME = "EngineMoveTime";

        /// <summary>
        /// Time for the engine to evaluate position in the evaluation mode.
        /// </summary>
        private const string CFG_ENGINE_EVALUATION_TIME = "EngineEvaluationTime";
        private const string CFG_ENGINE_THREADS = "EngineThreads";
        private const string CFG_ENGINE_HASH_SIZE = "EngineHashSize";
        private const string CFG_ENGINE_MPV = "EngineMpv";
        private const string CFG_VIABLE_MOVE_CP_DIFF = "ViableMoveCpDiff";
        private const string CFG_BLUNDER_DET_EVAL_DROP = "BlunderDetEvalDrop";
        private const string CFG_BLUNDER_NO_DET_THRESH = "BlunderNoDetThresh";

        private const string CFG_FONT_SIZE_DIFF = "FontSizeDiff";
        private const string CFG_AUTO_SAVE_FREQ = "AutoSaveFrequency";

        /// <summary>
        /// PGN export configuration.
        /// What to include
        /// </summary>
        private const string CFG_PGN_EXP_BOOKMARKS = "PgnExportBookmarks";
        private const string CFG_PGN_EXP_EVALS = "PgnExportEvals";

        private const string CFG_AUTO_SAVE = "AutoSave";
        private const string CFG_SOUND_ON = "SoundOn";
        private const string CFG_USE_FIGURINES = "UseFigurines";
        private const string CFG_USE_FIXED_FONT = "UseFixedFont";
        private const string CFG_SHOW_MOVES_AT_FORK = "ShowMovesAtFork";
        private const string CFG_SHOW_EXPLORERS = "ShowExplorers";
        private const string CFG_SHOW_INTRO_TAB = "ShowIntroTab";
        private const string CFG_MAIN_WIN_MAXIMIZED = "MainWinMaximized";
        private const string CFG_SHOW_GENERIC_PGN_INFO = "ShowGenericPgnInfo";
        private const string CFG_ALLOW_MOUSE_WHEEL_FOR_MOVES = "AllowMouseWheelForMoves";


        //***************** WEB GAMES ITEMS *************************

        private const string CFG_WG_SITE = "WebGamesSite";
        private const string CFG_WG_LICHESS_USER = "WebGamesLichessUser";
        private const string CFG_WG_CHESSCOM_USER = "WebGamesChessComUser";
        private const string CFG_WG_MAX_GAMES = "WebGamesMaxCount";
        private const string CFG_WG_MOST_RECENT = "WebGamesMostRecent";
        private const string CFG_WG_START_DATE = "WebGamesStartDate";
        private const string CFG_WG_END_DATE = "WebGamesEndDate";


        public static string StartDirectory = "";

        // name of the file in which this configuration is stored.
        public static string ConfigurationFile = "config.txt";

        private const string CFG_DEBUG_MODE = "DebugMode";

        // position of the main application window
        public static Thickness MainWinPos = new Thickness();

        // List of recently opened files
        public static List<string> RecentFiles = new List<string>();

        private static int MAX_RECENT_FILES = 12;


        /// <summary>
        /// Returns true if the font size is set to its max allowed value
        /// </summary>
        public static bool IsFontSizeAtMax
        {
            get => FontSizeDiff >= MAX_UP_FONT_SIZE_DIFF;
        }

        /// <summary>
        /// Returns true if the font size is set to its min allowed value
        /// </summary>
        public static bool IsFontSizeAtMin
        {
            get => FontSizeDiff <= MAX_DOWN_FONT_SIZE_DIFF;
        }

        /// <summary>
        /// Adds a file to the list of recently opened files.
        /// Removes a previous duplicate entry if exists.
        /// Removes oldes item if size of the list exceeded.
        /// </summary>
        /// <param name="path"></param>
        public static void AddRecentFile(string path)
        {
            // remove dupe if any
            RecentFiles.Remove(path);

            // remove the latest file if the list is too long
            if (RecentFiles.Count >= MAX_RECENT_FILES)
            {
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
            }

            // insert the new file
            RecentFiles.Insert(0, path);
        }

        /// <summary>
        /// Removes an item from a list of recent files.
        /// This will be called, if the app failed go open
        /// a file on the recent files list.
        /// </summary>
        /// <param name="path"></param>
        public static void RemoveFromRecentFiles(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                RecentFiles.Remove(path);
                if (LastWorkbookFile == path)
                {
                    LastWorkbookFile = "";
                }
            }
        }

        /// <summary>
        /// Maps configurable items to strings
        /// representing them in the configuration file.
        /// </summary>
        private static Dictionary<object, string> ItemToName = new Dictionary<object, string>();

        /// <summary>
        /// Reads configuration key/value pairs from the configuration file
        /// </summary>
        public static void ReadConfigurationFile()
        {
            // defaults for board colors
            StudyBoardSet = ChessBoards.BoardSets[ColorSet.BLUE];
            GameBoardSet = ChessBoards.BoardSets[ColorSet.LIGHT_BLUE];
            ExerciseBoardSet = ChessBoards.BoardSets[ColorSet.LIGH_GREEN];

            try
            {
                string fileName = Path.Combine(StartDirectory, ConfigurationFile);
                if (File.Exists(fileName))
                {
                    string[] lines = File.ReadAllLines(fileName);
                    foreach (string line in lines)
                    {
                        ProcessConfigurationItem(line);
                    }
                }
            }
            // we don't want to derail the program beacause something went wrong here
            // we'll just use defaults
            catch { }
        }

        /// <summary>
        /// Writes configuration key/value pairs to the configuration file.
        /// </summary>
        public static void WriteOutConfiguration()
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                string fileName = Path.Combine(StartDirectory, ConfigurationFile);

                sb.Append(CFG_DEBUG_MODE + "=" + DebugLevel.ToString() + Environment.NewLine);

                sb.Append(CFG_AUTOGEN_TREE_DEPTH + "=" + AutogenTreeDepth.ToString() + Environment.NewLine);
                sb.Append(CFG_MOVE_SPEED + "=" + MoveSpeed.ToString() + Environment.NewLine);
                sb.Append(CFG_LAST_DIRECTORY + "=" + LastOpenDirectory.ToString() + Environment.NewLine);
                sb.Append(CFG_LAST_IMPORT_DIRECTORY + "=" + LastImportDirectory.ToString() + Environment.NewLine);
                sb.Append(CFG_LAST_FILE + "=" + LastWorkbookFile.ToString() + Environment.NewLine);

                sb.Append(Environment.NewLine);

                sb.Append(CFG_ENGINE_EXE + "=" + EngineExePath + Environment.NewLine);
                sb.Append(CFG_DO_NOT_SHOW_VERSION + "=" + DoNotShowVersion + Environment.NewLine);
                sb.Append(CFG_CULTURE_NAME + "=" + CultureName + Environment.NewLine);

                sb.Append(CFG_ENGINE_MOVE_TIME + "=" + EngineMoveTime.ToString() + Environment.NewLine);
                sb.Append(CFG_ENGINE_EVALUATION_TIME + "=" + EngineEvaluationTime.ToString() + Environment.NewLine);
                sb.Append(CFG_ENGINE_THREADS + "=" + EngineThreads.ToString() + Environment.NewLine);
                sb.Append(CFG_ENGINE_HASH_SIZE + "=" + EngineHashSize.ToString() + Environment.NewLine);
                sb.Append(CFG_ENGINE_MPV + "=" + EngineMpv.ToString() + Environment.NewLine);

                sb.Append(CFG_FONT_SIZE_DIFF + "=" + FontSizeDiff.ToString() + Environment.NewLine);
                sb.Append(CFG_AUTO_SAVE_FREQ + "=" + AutoSaveFrequency.ToString() + Environment.NewLine);

                sb.Append(CFG_VIABLE_MOVE_CP_DIFF + "=" + ViableMoveCpDiff.ToString() + Environment.NewLine);
                sb.Append(CFG_BLUNDER_DET_EVAL_DROP + "=" + BlunderDetectEvalDrop.ToString() + Environment.NewLine);
                sb.Append(CFG_BLUNDER_NO_DET_THRESH + "=" + BlunderNoDetectThresh.ToString() + Environment.NewLine);

                sb.Append(CFG_PGN_EXP_BOOKMARKS + "=" + (PgnExportBookmarks ? "1" : "0") + Environment.NewLine);
                sb.Append(CFG_PGN_EXP_EVALS + "=" + (PgnExportEvaluations ? "1" : "0") + Environment.NewLine);

                sb.Append(CFG_AUTO_SAVE + "=" + (AutoSave ? "1" : "0") + Environment.NewLine);
                sb.Append(CFG_SOUND_ON + "=" + (SoundOn ? "1" : "0") + Environment.NewLine);
                sb.Append(CFG_USE_FIGURINES + "=" + (UseFigurines ? "1" : "0") + Environment.NewLine);
                sb.Append(CFG_USE_FIXED_FONT + "=" + (UseFixedFont ? "1" : "0") + Environment.NewLine);
                sb.Append(CFG_SHOW_MOVES_AT_FORK + "=" + (ShowMovesAtFork ? "1" : "0") + Environment.NewLine);
                sb.Append(CFG_SHOW_EXPLORERS + "=" + (ShowExplorers ? "1" : "0") + Environment.NewLine);
                sb.Append(CFG_SHOW_INTRO_TAB + "=" + (ShowIntroTab ? "1" : "0") + Environment.NewLine);
                sb.Append(CFG_SHOW_GENERIC_PGN_INFO + "=" + (ShowGenericPgnInfo ? "1" : "0") + Environment.NewLine);
                sb.Append(CFG_ALLOW_MOUSE_WHEEL_FOR_MOVES + "=" + (AllowMouseWheelForMoves ? "1" : "0") + Environment.NewLine);

                sb.Append(Environment.NewLine);

                sb.AppendLine(CFG_WG_SITE + "=" + WebGamesSite);
                sb.AppendLine(CFG_WG_LICHESS_USER + "=" + WebGamesLichessUser);
                sb.AppendLine(CFG_WG_CHESSCOM_USER + "=" + WebGamesChesscomUser);
                sb.AppendLine(CFG_WG_MAX_GAMES + "=" + WebGamesMaxCount);
                sb.AppendLine(CFG_WG_MOST_RECENT + "=" + (WebGamesMostRecent ? "1" : "0"));
                sb.AppendLine(CFG_WG_START_DATE + "=" + WebGamesStartDate);
                sb.AppendLine(CFG_WG_END_DATE + "=" + WebGamesEndDate);

                sb.Append(Environment.NewLine);

                sb.Append(GetRecentFiles());
                sb.Append(Environment.NewLine);

                sb.Append(GetWindowPosition(out bool isMaximized));
                sb.Append(CFG_MAIN_WIN_MAXIMIZED + "=" + (isMaximized ? "1" : "0") + Environment.NewLine);

                sb.Append(Environment.NewLine);


                File.WriteAllText(fileName, sb.ToString());
            }
            catch { }
        }

        /// <summary>
        /// Gets the position of the Main Window and encodes it.
        /// Detects maximized state.
        /// for saving.
        /// </summary>
        /// <returns></returns>
        public static string GetWindowPosition(out bool isMaximized)
        {
            isMaximized = Application.Current.MainWindow.WindowState == WindowState.Maximized;

            double left = Application.Current.MainWindow.Left;
            double top = Application.Current.MainWindow.Top;
            double right = left + Application.Current.MainWindow.Width;
            double bottom = top + Application.Current.MainWindow.Height;

            // if is maximized adjust the coordinates so they are within the virtual screen and do not cause
            // IsValidPosition to return false.
            if (isMaximized)
            {
                left = Math.Max(left, SystemParameters.VirtualScreenLeft);
                top = Math.Max(top, SystemParameters.VirtualScreenTop);
                right = Math.Min(left + Application.Current.MainWindow.Width, SystemParameters.VirtualScreenWidth);
                bottom = Math.Min(top + Application.Current.MainWindow.Height, SystemParameters.VirtualScreenHeight);
            }

            return CFG_MAIN_WINDOW_POS + " = " + left.ToString() + "," + top.ToString() + ","
                + right.ToString() + "," + bottom.ToString() + Environment.NewLine;
        }

        /// <summary>
        /// Checks if the saved Window position is valid and can be used
        /// when reopening the application.
        /// We consider the position valid if both width and length are greater than 100
        /// and it is fully within the Virtual Screen.
        /// </summary>
        /// <returns></returns>
        public static bool IsMainWinPosValid()
        {
            if (MainWinPos.Right > MainWinPos.Left + 400 && MainWinPos.Bottom > MainWinPos.Top + 200)
            {
                if (SystemParameters.VirtualScreenLeft <= MainWinPos.Left && SystemParameters.VirtualScreenWidth >= MainWinPos.Right
                    && SystemParameters.VirtualScreenTop <= MainWinPos.Top && SystemParameters.VirtualScreenHeight >= MainWinPos.Bottom)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the last Window position was recorded as maximized.
        /// In the older versions this was expressed by having all coords == 0.
        /// </summary>
        /// <returns></returns>
        public static bool IsMainWinMaximized()
        {
            return MainWinMaximized || (MainWinPos.Right == 0 && MainWinPos.Left == 0 && MainWinPos.Bottom == 0 && MainWinPos.Top == 0);
        }

        /// <summary>
        /// Gets the list of recently opend files and combines it
        /// in one string for saving.
        /// </summary>
        /// <returns></returns>
        public static string GetRecentFiles()
        {
            string itemName = CFG_RECENT_FILES;

            StringBuilder sbFiles = new StringBuilder();

            for (int i = 0; i < RecentFiles.Count; i++)
            {
                sbFiles.Append(itemName + i.ToString() + " = " + RecentFiles.ElementAt(i) + Environment.NewLine);
            }

            return sbFiles.ToString();
        }

        /// <summary>
        /// Called in response to user clicking one of the RecentFile
        /// menu items.  
        /// The index of the file in the list of recent files is encoded
        /// in the name of the manu item e.g. "RecentFiles2" hence the way
        /// the MenuItem's name is processed here.
        /// </summary>
        /// <param name="menuItemName"></param>
        /// <returns></returns>
        public static string GetRecentFile(string menuItemName)
        {
            if (menuItemName.StartsWith(AppState.MainWin.MENUITEM_RECENT_FILES_PREFIX))
            {
                try
                {
                    int index = int.Parse(menuItemName.Substring(AppState.MainWin.MENUITEM_RECENT_FILES_PREFIX.Length));
                    return RecentFiles[index];
                }
                catch
                {
                    return "";
                }
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Finds engine executable by checking if it is already set
        /// and if not, if we have it in the current directory.
        /// If all fails, asks the user to find it.
        /// </summary>
        /// <returns></returns>
        public static string EngineExecutableFilePath()
        {
            if (!File.Exists(EngineExePath))
            {
                string searchPath = "";
                try
                {
                    searchPath = Path.GetDirectoryName(EngineExePath);
                }
                catch { };
                EngineExePath = "";
                DirectoryInfo info = new DirectoryInfo(".");
                FileInfo[] files = info.GetFiles();

                int latestVer = -1;
                foreach (FileInfo file in files)
                {
                    string fileLowerCase = file.Name.ToLower();
                    if (file.Name.StartsWith("stockfish", StringComparison.OrdinalIgnoreCase) && fileLowerCase.IndexOf(".exe") > 0)
                    {
                        string[] tokens = file.Name.Split('_');
                        if (tokens.Length >= 2)
                        {
                            int ver;
                            if (int.TryParse(tokens[1], out ver))
                            {
                                if (ver > latestVer)
                                {
                                    latestVer = ver;
                                    EngineExePath = file.FullName;
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(EngineExePath))
                {
                    EngineExePath = SelectEngineExecutable(searchPath);
                }
            }

            return EngineExePath;
        }

        /// <summary>
        /// Opens a dialog allowing the user to choose engine executable.
        /// If selected, the configuration is saved.
        /// </summary>
        /// <returns></returns>
        public static string SelectEngineExecutable(string searchPath)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "Chess engine (*.exe)|*.exe";

            if (!string.IsNullOrEmpty(searchPath))
            {
                openFileDialog.InitialDirectory = searchPath;
            }
            else
            {
                openFileDialog.InitialDirectory = "";
            }

            bool? result;
            result = openFileDialog.ShowDialog();

            if (result == true)
            {
                EngineExePath = openFileDialog.FileName;
                WriteOutConfiguration();
                return EngineExePath;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Processes a single line/item from the configuration file.
        /// </summary>
        /// <param name="line"></param>
        private static void ProcessConfigurationItem(string line)
        {
            string[] tokens = line.Split('=');
            if (tokens.Length >= 2)
            {
                string name = tokens[0].Trim();
                string value = tokens[1].Trim();

                if (name.StartsWith(CFG_RECENT_FILES))
                {
                    RecentFiles.Add(value.Trim());
                }
                else
                {
                    switch (name)
                    {
                        case CFG_AUTOGEN_TREE_DEPTH:
                            uint.TryParse(value, out _autogenTreeDepth);
                            break;
                        case CFG_MOVE_SPEED:
                            int.TryParse(value, out MoveSpeed);
                            break;
                        case CFG_DEBUG_MODE:
                            int.TryParse(value, out DebugLevel);
                            break;
                        case CFG_LAST_DIRECTORY:
                            LastOpenDirectory = value;
                            break;
                        case CFG_LAST_IMPORT_DIRECTORY:
                            LastImportDirectory = value;
                            break;
                        case CFG_LAST_FILE:
                            LastWorkbookFile = value;
                            break;
                        case CFG_ENGINE_EXE:
                            EngineExePath = value;
                            break;
                        case CFG_DO_NOT_SHOW_VERSION:
                            DoNotShowVersion = value;
                            break;
                        case CFG_CULTURE_NAME:
                            CultureName = value;
                            break;
                        case CFG_FONT_SIZE_DIFF:
                            int.TryParse(value, out FontSizeDiff);
                            break;
                        case CFG_AUTO_SAVE_FREQ:
                            int.TryParse(value, out AutoSaveFrequency);
                            break;
                        case CFG_BLUNDER_DET_EVAL_DROP:
                            uint.TryParse(value, out BlunderDetectEvalDrop);
                            break;
                        case CFG_BLUNDER_NO_DET_THRESH:
                            uint.TryParse(value, out BlunderNoDetectThresh);
                            break;
                        case CFG_ENGINE_EVALUATION_TIME:
                            int.TryParse(value, out _engineEvaluationTime);
                            break;
                        case CFG_ENGINE_MOVE_TIME:
                            int.TryParse(value, out _engineMoveTime);
                            break;
                        case CFG_ENGINE_THREADS:
                            int.TryParse(value, out _engineThreads);
                            break;
                        case CFG_ENGINE_MPV:
                            int.TryParse(value, out _engineMpv);
                            break;
                        case CFG_ENGINE_HASH_SIZE:
                            long.TryParse(value, out _engineHashSize);
                            break;
                        case CFG_VIABLE_MOVE_CP_DIFF:
                            int.TryParse(value, out _viableMoveCpDiff);
                            // make sure this is not negative
                            ViableMoveCpDiff = Math.Abs(_viableMoveCpDiff);
                            break;
                        case CFG_PGN_EXP_BOOKMARKS:
                            PgnExportBookmarks = value != "0" ? true : false;
                            break;
                        case CFG_PGN_EXP_EVALS:
                            PgnExportEvaluations = value != "0" ? true : false;
                            break;
                        case CFG_SHOW_GENERIC_PGN_INFO:
                            ShowGenericPgnInfo = value != "0" ? true : false;
                            break;
                        case CFG_SHOW_MOVES_AT_FORK:
                            ShowMovesAtFork = value != "0" ? true : false;
                            break;
                        case CFG_SHOW_EXPLORERS:
                            ShowExplorers = value != "0" ? true : false;
                            break;
                        case CFG_SHOW_INTRO_TAB:
                            ShowIntroTab = value != "0" ? true : false;
                            break;
                        case CFG_MAIN_WIN_MAXIMIZED:
                            MainWinMaximized = value != "0" ? true : false;
                            break;
                        case CFG_AUTO_SAVE:
                            AutoSave = value != "0" ? true : false;
                            break;
                        case CFG_SOUND_ON:
                            SoundOn = value != "0" ? true : false;
                            break;
                        case CFG_USE_FIGURINES:
                            UseFigurines = value != "0" ? true : false;
                            break;
                        case CFG_USE_FIXED_FONT:
                            UseFixedFont = value != "0" ? true : false;
                            break;
                        case CFG_ALLOW_MOUSE_WHEEL_FOR_MOVES:
                            AllowMouseWheelForMoves = value != "0" ? true : false;
                            break;
                        case CFG_WG_SITE:
                            WebGamesSite = value;
                            break;
                        case CFG_WG_LICHESS_USER:
                            WebGamesLichessUser = value;
                            break;
                        case CFG_WG_CHESSCOM_USER:
                            WebGamesChesscomUser = value;
                            break;
                        case CFG_WG_MAX_GAMES:
                            int.TryParse(value, out WebGamesMaxCount);
                            break;
                        case CFG_WG_MOST_RECENT:
                            WebGamesMostRecent = value != "0" ? true : false;
                            break;
                        case CFG_WG_START_DATE:
                            WebGamesStartDate = GetDate(value);
                            break;
                        case CFG_WG_END_DATE:
                            WebGamesEndDate = GetDate(value);
                            break;
                        case CFG_MAIN_WINDOW_POS:
                            string[] sizes = value.Split(',');
                            if (sizes.Length == 4)
                            {
                                try
                                {
                                    MainWinPos.Left = double.Parse(sizes[0]);
                                    MainWinPos.Top = double.Parse(sizes[1]);
                                    MainWinPos.Right = double.Parse(sizes[2]);
                                    MainWinPos.Bottom = double.Parse(sizes[3]);
                                }
                                catch
                                {
                                }
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// If the passed string represents a date, that date will be returned.
        /// Otherwise it will return null;
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static DateTime? GetDate(string value)
        {
            if (DateTime.TryParse(value, out var date))
            {
                return date;
            }
            else
            {
                return null;
            }
        }

    }

}

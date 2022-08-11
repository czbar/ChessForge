using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace ChessForge
{
    public class Configuration
    {
        //*********************************
        // CONFIGURATION ITEMS
        //*********************************
        /// <summary>
        /// The time in milliseconds that it takes
        /// to animate a move on the board
        /// </summary>
        public static int MoveSpeed = 200;

        /// <summary>
        /// Last directory from which a PGN file was read.
        /// </summary>
        public static string LastOpenDirectory = "";

        /// <summary>
        /// Last read Workbook file.
        /// </summary>
        public static string LastWorkbookFile = "";

        /// <summary>
        /// Time given to the engine to evaluate a single move
        /// (in milliseconds)
        /// </summary>
        public static int EngineEvaluationTime = 1000;

        /// <summary>
        /// Time given to the engine to respond
        /// during a training game.
        /// (in milliseconds)
        /// </summary>
        public static int EngineMoveTime = 1000;

        /// <summary>
        /// When choosing "viable" repsonsed from the engine
        /// during a game, moves under consideration must not
        /// be worse than by this centipawn value from the
        /// best move.
        /// </summary>
        public static int ViableMoveCpDiff = 100;

        /// <summary>
        /// Number of moves to return with evaluations.
        /// </summary>
        public static int EngineMpv = 5;

        public static int DebugMode = 0;

        //*********************************
        // CONFIGUARTION ITEM NAMES
        //*********************************

        private const string CFG_MOVE_SPEED = "MoveSpeed";
        private const string CFG_LAST_DIRECTORY = "LastDirectory";
        private const string CFG_LAST_FILE = "LastFile";
        private const string CFG_RECENT_FILES = "RecentFiles";
        private const string CFG_MAIN_WINDOW_POS = "MainWindowPosition";

        /// <summary>
        /// Time the engine has to make a move in a training game
        /// </summary>
        private const string CFG_ENGINE_MOVE_TIME = "EngineMoveTime";

        /// <summary>
        /// Time for the engine to evaluate position in the evaluation mode.
        /// </summary>
        private const string CFG_ENGINE_EVALUATION_TIME = "EngineEvaluationTime";
        private const string CFG_ENGINE_MPV = "EngineMpv";
        private const string CFG_VIABLE_MOVE_CP_DIFF = "ViableMoveCpDiff";

        public static string StartDirectory = "";
        public static string ConfigurationFile = "config.txt";

        private const string CFG_DEBUG_MODE = "DebugMode";

        public static Rectangle MainWinPos = new Rectangle();

        public static List<string> RecentFiles = new List<string>();

        private static int MAX_RECENT_FILES = 5;

        // application's main window
        private static MainWindow MainWin;

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
            RecentFiles.Remove(path);
        }

        /// <summary>
        /// Maps configurable items to strings
        /// representing them in the configuration file.
        /// </summary>
        private static Dictionary<object, string> ItemToName = new Dictionary<object, string>();

        public static void Initialize(MainWindow mainWin)
        {
            MainWin = mainWin;

            MoveSpeed = 200;
            LastOpenDirectory = "";
            LastWorkbookFile = "";
        }

        /// <summary>
        /// Reads configuration key/value pairs from the configuration file
        /// </summary>
        public static void ReadConfigurationFile()
        {
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

                sb.Append(CFG_DEBUG_MODE + "=" + DebugMode.ToString() + Environment.NewLine);

                sb.Append(CFG_MOVE_SPEED + "=" + MoveSpeed.ToString() + Environment.NewLine);
                sb.Append(CFG_LAST_DIRECTORY + "=" + LastOpenDirectory.ToString() + Environment.NewLine);
                sb.Append(CFG_LAST_FILE + "=" + LastWorkbookFile.ToString() + Environment.NewLine);

                sb.Append(Environment.NewLine);

                sb.Append(CFG_ENGINE_MOVE_TIME + "=" + EngineMoveTime.ToString() + Environment.NewLine);
                sb.Append(CFG_ENGINE_EVALUATION_TIME + "=" + EngineEvaluationTime.ToString() + Environment.NewLine);
                sb.Append(CFG_ENGINE_MPV + "=" + EngineMpv.ToString() + Environment.NewLine);

                sb.Append(CFG_VIABLE_MOVE_CP_DIFF + "=" + ViableMoveCpDiff.ToString() + Environment.NewLine);

                sb.Append(Environment.NewLine);

                sb.Append(GetRecentFiles());
                sb.Append(Environment.NewLine);

                sb.Append(GetWindowPosition());
                sb.Append(Environment.NewLine);


                File.WriteAllText(fileName, sb.ToString());
            }
            catch { }
        }

        /// <summary>
        /// Gets the position of the Main Window and encodes it
        /// for saving.
        /// </summary>
        /// <returns></returns>
        public static string GetWindowPosition()
        {
            double left = Application.Current.MainWindow.Left;
            double top = Application.Current.MainWindow.Top;
            double width = Application.Current.MainWindow.Width;
            double height = Application.Current.MainWindow.Height;

            return CFG_MAIN_WINDOW_POS + " = " + left.ToString() + "," + top.ToString() + ","
                + width.ToString() + "," + height.ToString() + Environment.NewLine;
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
            if (menuItemName.StartsWith(MainWin.MENUITEM_RECENT_FILES_PREFIX))
            {
                try
                {
                    int index = int.Parse(menuItemName.Substring(MainWin.MENUITEM_RECENT_FILES_PREFIX.Length));
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
                        case CFG_MOVE_SPEED:
                            int.TryParse(value, out MoveSpeed);
                            break;
                        case CFG_DEBUG_MODE:
                            int.TryParse(value, out DebugMode);
                            break;
                        case CFG_LAST_DIRECTORY:
                            LastOpenDirectory = value;
                            break;
                        case CFG_LAST_FILE:
                            LastWorkbookFile = value;
                            break;
                        case CFG_ENGINE_EVALUATION_TIME:
                            int.TryParse(value, out EngineEvaluationTime);
                            break;
                        case CFG_ENGINE_MOVE_TIME:
                            int.TryParse(value, out EngineMoveTime);
                            break;
                        case CFG_ENGINE_MPV:
                            int.TryParse(value, out EngineMpv);
                            break;
                        case CFG_VIABLE_MOVE_CP_DIFF:
                            int.TryParse(value, out ViableMoveCpDiff);
                            // make sure this is not negative
                            ViableMoveCpDiff = Math.Abs(ViableMoveCpDiff);
                            break;
                        case CFG_MAIN_WINDOW_POS:
                            string[] sizes = value.Split(',');
                            if (sizes.Length == 4)
                            {
                                try
                                {
                                    MainWinPos.Left = double.Parse(sizes[0]);
                                    MainWinPos.Top = double.Parse(sizes[1]);
                                    MainWinPos.Width = double.Parse(sizes[2]);
                                    MainWinPos.Height = double.Parse(sizes[3]);
                                    MainWinPos.CheckValid();
                                }
                                catch
                                {
                                    MainWinPos.IsValid = false;
                                }
                            }
                            break;
                    }
                }
            }
        }

    }

    public struct Rectangle
    {
        public bool IsValid;

        public double Left;
        public double Top;
        public double Width;
        public double Height;

        /// <summary>
        /// Check basic sanity.
        /// </summary>
        /// <returns></returns>
        public void CheckValid()
        {
            if (Width > 100 && Height > 100)
            {
                IsValid = true;
            }
            else
            {
                IsValid = false;
            }
        }
    }
}

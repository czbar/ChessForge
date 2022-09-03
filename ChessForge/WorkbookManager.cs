using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using ChessPosition.GameTree;
using System.Collections.ObjectModel;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Manages Workbook states and file manipulations.
    /// </summary>
    public class WorkbookManager
    {
        /// <summary>
        /// The list of game metadata from the currently read PGN file.
        /// </summary>
        public static ObservableCollection<GameMetadata> GamesHeaders = new ObservableCollection<GameMetadata>();

        /// <summary>
        /// Check if file exists or is already open 
        /// and advises the user accordingly.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="isLastOpen"></param>
        /// <returns></returns>
        public static bool CheckFileExists(string fileName, bool isLastOpen)
        {
            // check for idle just in case (should never be the case if WorkbookFilePath is not empty
            if (fileName == AppStateManager.WorkbookFilePath && AppStateManager.CurrentLearningMode != LearningMode.Mode.IDLE)
            {
                MessageBox.Show(Path.GetFileName(fileName) + " is already open.", "Chess Forge File", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (File.Exists(fileName))
            {
                return true;
            }
            else
            {
                AppStateManager.MainWin.BoardCommentBox.OpenFile();

                if (isLastOpen)
                {
                    MessageBox.Show("Most recent file " + fileName + " could not be found.", "File Not Found", MessageBoxButton.OK);
                }
                else
                {
                    MessageBox.Show("File " + fileName + " could not be found.", "File Not Found", MessageBoxButton.OK);
                }

                Configuration.RemoveFromRecentFiles(fileName);
                AppStateManager.MainWin.RecreateRecentFilesMenuItems();

                return false;
            }
        }

        /// <summary>
        /// Reads a PGN file that may have 1 or multiple games in it.
        /// If there are multiple games, the user will be asked to select
        /// games for merging into a Workbook.
        /// Returns true if at least one game was found in the file.
        /// </summary>
        public static int ReadPgnFile(string path)
        {
            GamesHeaders.Clear();

            // read line by line, fishing for lines with PGN headers i.e. beginning with "[" followed by a keyword.
            // Note we may accidentally hit a comment formatted that way, so make sure that the last char on the line is "]".
            GameMetadata gm = new GameMetadata();
            gm.FirstLineInFile = 1;
            
            using (StreamReader sr = new StreamReader(path))
            {
                StringBuilder gameText = new StringBuilder();
                int lineNo = 0;
                bool headerLine = true;

                while (sr.Peek() >= 0)
                {
                    lineNo++;
                    headerLine = true;

                    string line = sr.ReadLine();
                    string header = ProcessPgnHeaderLine(line, out string val);
                    switch (header)
                    {
                        case "Event":
                            gm.Event = val;
                            break;
                        case "Site":
                            gm.Site = val;
                            break;
                        case "Date":
                            gm.Date = val;
                            break;
                        case "Round":
                            gm.Round = val;
                            break;
                        case "White":
                            gm.White = val;
                            break;
                        case "Black":
                            gm.Black = val;
                            break;
                        case "Result":
                            gm.Result = val;
                            break;
                        default:
                            headerLine = false;
                            // if no header then this is the end of the header lines if we do have any data
                            if (header.Length == 0 && (gm.White != null || gm.Black != null || gm.Result != null))
                            {
                                GamesHeaders.Add(gm);
                                gm = new GameMetadata();
                            }
                            break;
                    }
                    if (headerLine == true && gm.FirstLineInFile == 0)
                    {
                        gm.FirstLineInFile = lineNo - 1;
                        // added game text to the PREVIOUS game object 
                        GamesHeaders[GamesHeaders.Count - 1].GameText = gameText.ToString();
                        gameText.Clear();
                    }

                    gameText.AppendLine(line);                    
                }

                if (GamesHeaders.Count > 0)
                {
                    // add game text to the last object
                    GamesHeaders[GamesHeaders.Count - 1].GameText = gameText.ToString();
                }
            }

            int mergedGames = 0;
            // if there is more than 1 game, ask the user to select
            if (GamesHeaders.Count > 1)
            {
                mergedGames = MergeGames();
            }
            else if (GamesHeaders.Count == 1)
            {
                PgnGameParser pgp = new PgnGameParser(GamesHeaders[0].GameText, AppStateManager.MainWin.Workbook, out bool multi);
                mergedGames = 1;
            }

            return mergedGames;
        }

        /// <summary>
        /// Asks the user to select games before merging.
        /// Returns the number of games merged, or -1 if the user
        /// canceled the selection dialog.
        /// </summary>
        /// <returns></returns>
        private static int MergeGames()
        {
            SelectGamesDialog dlg = new SelectGamesDialog();
            dlg.ShowDialog();

            int mergedCount = 0;

            if (dlg.Result)
            {
                // merge workbooks
                for (int i = 0; i < GamesHeaders.Count; i++)
                {
                    if (GamesHeaders[i].IsSelected)
                    {
                        if (mergedCount == 0)
                        {
                            // special treatment for the first one
                            PgnGameParser pgp = new PgnGameParser(GamesHeaders[i].GameText, AppStateManager.MainWin.Workbook, out bool multi);
                            mergedCount++;
                        }
                        else
                        {
                            WorkbookTree workbook2 = new WorkbookTree();
                            PgnGameParser pgp = new PgnGameParser(GamesHeaders[i].GameText, workbook2, out bool multi);
                            AppStateManager.MainWin.Workbook = WorkbookTreeMerge.MergeWorkbooks(AppStateManager.MainWin.Workbook, workbook2);
                            mergedCount++;
                        }
                    }
                }
                return mergedCount;
            }
            else
            {
                //MessageBox.Show("The Workbook will be created from the first game only.", "Chess Forge Workbook",
                //    MessageBoxButton.OK, MessageBoxImage.Information);
                return 0;
            }
        }

        /// <summary>
        /// Checks if the passed string looks like a header line and if so
        /// returns the name and value of the header.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        private static string ProcessPgnHeaderLine(string line, out string val)
        {
            string header = "";
            val = "";
            line = line.Trim();

            if (line.Length > 0 && line[0] == '[' && line[line.Length - 1] == ']')
            {
                line = line.Substring(1, line.Length - 2);
                string[] tokens = line.Split('\"');
                if (tokens.Length >= 2)
                {
                    header = tokens[0].Trim();
                    val = tokens[1].Trim();
                }
            }

            return header;
        }

        /// <summary>
        /// Prompts the user to decide whether they want to convert/save 
        /// PGN file as a CHF Workbook.
        /// Invoked when the app or the Workbook is being closed.
        /// </summary>
        /// <returns></returns>
        public static int PromptUserToConvertPGNToCHF()
        {
            bool hasBookmarks = AppStateManager.MainWin.Workbook.Bookmarks.Count > 0;

            string msg = "Your edits " + (hasBookmarks ? "and bookmarks " : "")
                + "will be lost unless you save this Workbook as a ChessForge (.chf) file.\n\n Convert and save?";
            if (MessageBox.Show(msg, "Chess Forge File Closing", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                WorkbookManager.SaveWorkbookToNewFile(AppStateManager.WorkbookFilePath, true);
                return 0;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// This function will be called when:
        /// 1. the user selects File->Save (userRequest == true)
        /// 2. the user exists a Training session
        /// 3. the user selectes File->Close
        /// 4. the user closes the application.
        /// First, we check if there are any training moves in the Tree which
        /// means that we have not exited the training session yet.
        /// If there are, we ask the user to save the training moves and upon
        /// confirmation we save the entire Workbook.
        /// If not, or the user declines, and if the Workbook is "dirty", we offer to save the workbook without 
        /// training moves.
        /// In addition, if the user does want to save the file but there is no file name, we aks them to choose one.
        /// </summary>
        /// <returns> Returns true if the user chooses yes or no,
        /// returns false if the user cancels. </returns>
        public static bool PromptAndSaveWorkbook(bool userRequest, bool isAppClosing = false)
        {
            MessageBoxResult res = MessageBoxResult.None;

            if (AppStateManager.MainWin.Workbook.HasTrainingMoves())
            {
                res = PromptAndSaveTrainingMoves(userRequest, isAppClosing);
            }

            if (res != MessageBoxResult.OK && res != MessageBoxResult.Cancel)
            {
                // not saved yet
                if (userRequest)
                {
                    // user requested File->Save so proceed...
                    AppStateManager.SaveWorkbookFile();
                }
                else
                {
                    if (AppStateManager.IsDirty)
                    {
                        // this was prompted by an action other than File->Save so ask...
                        MessageBoxButton mbb = isAppClosing ? MessageBoxButton.YesNo : MessageBoxButton.YesNoCancel;
                        res = MessageBox.Show("Save the Workbook?", "Workbook not saved", mbb, MessageBoxImage.Question);
                        if (res == MessageBoxResult.Yes)
                        {
                            AppStateManager.SaveWorkbookFile();
                        }
                    }
                    else
                    {
                        // not dirty and not user request so this is on close. Return Yes in order not to prevent closing 
                        res = MessageBoxResult.Yes;
                    }
                }
            }

            AppStateManager.ConfigureSaveMenus();

            return res == MessageBoxResult.Yes || res == MessageBoxResult.No;
        }

        /// <summary>
        /// Prompts for and saves training moves with the Workbook.
        /// If this is invoked due to the app closing, the Cancel option is not offered.
        /// </summary>
        /// <param name="userRequest"></param>
        /// <param name="isAppClosing"></param>
        /// <returns></returns>
        private static MessageBoxResult PromptAndSaveTrainingMoves(bool userRequest, bool isAppClosing)
        {
            MessageBoxResult res;

            MessageBoxButton buttons;
            if (!isAppClosing)
            {
                buttons = MessageBoxButton.YesNoCancel;
            }
            else
            {
                buttons = MessageBoxButton.YesNo;
            }

            res = MessageBox.Show("Merge and Save new moves from this session into the Workbook?", "Chess Forge Save Workbook",
                buttons, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                AppStateManager.MainWin.Workbook.ClearTrainingFlags();
                AppStateManager.MainWin.Workbook.BuildLines();
                AppStateManager.SaveWorkbookFile();
                AppStateManager.MainWin.RebuildWorkbookView();
                AppStateManager.MainWin.RefreshSelectedActiveLineAndNode();
            //    saved = true;
            }
            else if (res == MessageBoxResult.No)
            {
                AppStateManager.MainWin.Workbook.RemoveTrainingMoves();
            }

            return res;
        }

        /// <summary>
        /// Ask the user whether they intend to close the Wokbook
        /// e.g. because they selected File->New while there is a workbook currently open.
        /// </summary>
        /// <returns></returns>
        public static bool AskToCloseWorkbook()
        {
            // if a workbook is open ask whether to close it first
            if (AppStateManager.CurrentLearningMode != LearningMode.Mode.IDLE)
            {
                if (MessageBox.Show("Close the current Workbook?", "Workbook", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return false;
                }
                else
                {
                    WorkbookManager.AskToSaveWorkbook();
                    // if we are not in IDLE mode then the user did not close
                    if (AppStateManager.CurrentLearningMode != LearningMode.Mode.IDLE)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Asks the user whether to save the currently open Workbook.
        /// </summary>
        /// <returns></returns>
        public static bool AskToSaveWorkbook()
        {
            if (AppStateManager.WorkbookFileType == AppStateManager.FileType.PGN)
            {
                PromptUserToConvertPGNToCHF();
            }
            else
            {
                if (!PromptAndSaveWorkbook(false))
                {
                    // the user chose cancel so we are not closing after all
                    return false;
                }
            }
            AppStateManager.RestartInIdleMode();
            return true;
        }

        /// <summary>
        /// Updates the list of recent files and LAstWorkbookFile
        /// </summary>
        /// <param name="fileName"></param>
        public static void UpdateRecentFilesList(string fileName)
        {
            Configuration.AddRecentFile(fileName);
            AppStateManager.MainWin.RecreateRecentFilesMenuItems();
            Configuration.LastWorkbookFile = fileName;
        }

        /// <summary>
        /// Allows the user to save the Workbook in a new file.
        /// This method will be called when converting a PGN file to CHF
        /// or in reponse to File->Save As menu selection
        /// or in response when saving a file that has no name yet (i.e. saving
        /// first time since creation).
        /// </summary>
        /// <param name="pgnFileName"></param>
        public static bool SaveWorkbookToNewFile(string pgnFileName, bool typeConversion)
        {
            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = "chf Workbook files (*.chf)|*.chf"
            };

            if (typeConversion)
            {
                saveDlg.Title = " Save Workbook converted from " + Path.GetFileName(pgnFileName);
            }
            else
            {
                if (!string.IsNullOrEmpty(pgnFileName))
                {
                    saveDlg.Title = " Save Workbook " + Path.GetFileName(pgnFileName) + " As...";
                }
                else
                {
                    saveDlg.Title = " Save New Workbook As...";
                }
            }

            if (!string.IsNullOrEmpty(pgnFileName))
            {
                saveDlg.FileName = Path.GetFileNameWithoutExtension(pgnFileName) + ".chf";
            }
            else if (!typeConversion && !string.IsNullOrWhiteSpace(AppStateManager.MainWin.Workbook.Title))
            {
                saveDlg.FileName = AppStateManager.MainWin.Workbook.Title + ".chf";
            }

            saveDlg.OverwritePrompt = true;
            if (saveDlg.ShowDialog() == true)
            {
                string chfFileName = saveDlg.FileName;
                AppStateManager.SaveWorkbookToNewFile(pgnFileName, chfFileName, typeConversion);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Saves current workbook to a PGN file.
        /// </summary>
        /// <returns></returns>
        public static bool SaveWorkbookToPgn()
        {
            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = "PGN files (*.pgn)|*.pgn",
                Title = "Export Workbook to a PGN file"
            };

            if (!string.IsNullOrEmpty(AppStateManager.WorkbookFilePath))
            {
                saveDlg.FileName = Path.GetFileNameWithoutExtension(AppStateManager.WorkbookFilePath) + ".pgn";
            }

            saveDlg.OverwritePrompt = true;
            if (saveDlg.ShowDialog() == true)
            {
                string pgnFileName = saveDlg.FileName;
                AppStateManager.ExportToPgn(pgnFileName);
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace ChessForge
{
    /// <summary>
    /// Manages Workbook states and file manipulations.
    /// </summary>
    public class WorkbookManager
    {
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
        public static bool PromptAndSaveWorkbook(bool userRequest)
        {
            bool saved = false;
            MessageBoxResult res = MessageBoxResult.None;

            if (AppStateManager.MainWin.Workbook.HasTrainingMoves())
            {
                res = MessageBox.Show("Merge and Save new moves from this session into the Workbook?", "Chess Forge Save Workbook",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    AppStateManager.MainWin.Workbook.ClearTrainingFlags();
                    AppStateManager.MainWin.Workbook.BuildLines();
                    AppStateManager.SaveWorkbookFile();
                    AppStateManager.MainWin.RebuildWorkbookView();
                    saved = true;
                }
                else
                {
                    AppStateManager.MainWin.Workbook.RemoveTrainingMoves();
                }
            }

            if (!saved)
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
                        res = MessageBox.Show("Save the Workbook?", "Workbook not saved", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
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
            if (res == MessageBoxResult.Yes || res == MessageBoxResult.No)
            {
                return true;
            }
            else
            {
                return false;
            }
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
            else if (!typeConversion && !string.IsNullOrWhiteSpace(AppStateManager.MainWin.Workbook.Title) )
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
    }
}

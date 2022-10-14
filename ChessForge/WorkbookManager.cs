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
using ChessPosition;
using System.Windows.Controls;
using System.Windows.Input;
using System.Runtime.CompilerServices;

namespace ChessForge
{
    /// <summary>
    /// Manages Workbook states and file manipulations.
    /// </summary>
    public class WorkbookManager
    {
        /// <summary>
        /// Workbook for the current session.
        /// </summary>
        public static Workbook SessionWorkbook;

        /// <summary>
        /// Id of the chapter which was last clicked in the Chapters view.
        /// </summary>
        public static int LastClickedChapterId = -1;

        /// <summary>
        /// The list of Variation Trees (a.k.a. PGN Games) for the SessionWorkbook.
        /// This includes all types i.e. Study Tree, Model Games and Exercises.
        /// </summary>
        public static ObservableCollection<GameMetadata> VariationTreeList = new ObservableCollection<GameMetadata>();

        /// <summary>
        /// Creates and stores a new Workbook object.
        /// </summary>
        public static void CreateNewWorkbook()
        {
            SessionWorkbook = new Workbook();
            SessionWorkbook.CreateNewChapter();
            AssignChaptersIds();
        }

        /// <summary>
        /// Assigns Chapter Ids per their position on the Chapters list.
        /// The first chapter gets the id of 1.
        /// </summary>
        public static void AssignChaptersIds()
        {
            for (int i = 0; i < SessionWorkbook.Chapters.Count; i++)
            {
                SessionWorkbook.Chapters[i].Id = i + 1;
            }
        }

        /// <summary>
        /// Checks if the GameList represents a Chess Forge Workbook.
        /// This is determined by the presence of the ChessForgeWorkbook
        /// header in the first game.
        /// </summary>
        /// <returns></returns>
        public static bool IsChessForgeWorkbook(ref ObservableCollection<GameMetadata> GameList)
        {
            if (GameList.Count > 0)
            {
                return GameList[0].GetWorkbookTitle() != null;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Processes a legacy CHF file.
        /// The content of the file is considered to be a study tree
        /// within a single chapter.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool ReadLegacyChfFile(string fileName)
        {
            try
            {
                string studyText = File.ReadAllText(fileName);
                VariationTree tree = new VariationTree(GameMetadata.ContentType.STUDY_TREE);

                // parse the variation tree and create a new chapter.
                PgnGameParser pgnGame = new PgnGameParser(studyText, tree, out bool isMulti, true);

                SessionWorkbook = new Workbook();
                SessionWorkbook.Title = tree.Title;

                Chapter chapter = SessionWorkbook.CreateNewChapter(tree);
                chapter.Title = tree.Title;

                // ask the name of the file to save the converted workbook to
                return SaveWorkbookToNewFileV2(fileName, true);
            }
            catch
            {
                MessageBox.Show("Error processing file: " + fileName, "Legacy CHF File", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Checks if file exists or is already open 
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
        /// Parses a PGN file that may be a Chess Forge PGN or a generic PGN.
        /// The file is split into games that are stored with the headers
        /// and content separated.
        /// This method does not check the validity of the text of the game. 
        /// Returns the number of games in the file.
        /// </summary>
        public static int ReadPgnFile(string path, ref ObservableCollection<GameMetadata> games, GameMetadata.ContentType contentType)
        {
            games.Clear();

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
                    string header = PgnHeaders.ParsePgnHeaderLine(line, out string val);
                    if (header != null)
                    {
                        // ignore headers with no name
                        if (header.Length > 0)
                        {
                            gm.Header.SetHeaderValue(header, val);
                        }
                    }
                    else
                    {
                        headerLine = false;
                        // if no header then this is the end of the header lines
                        // if we do have any header data we add a new game to the list
                        if (gm.HasAnyHeader())
                        {
                            gm.Header.DetermineContentType();
                            games.Add(gm);
                            gm = new GameMetadata();
                        }
                    }

                    // If this was the first header line, the gameText variable
                    // holds the complete text of the previous game
                    if (headerLine == true && gm.FirstLineInFile == 0)
                    {
                        gm.FirstLineInFile = lineNo - 1;
                        // add game text to the previous game object 
                        games[games.Count - 1].GameText = gameText.ToString();
                        gameText.Clear();
                    }

                    if (!headerLine)
                    {
                        gameText.AppendLine(line);
                    }
                }

                if (games.Count > 0)
                {
                    // add game text to the last object
                    games[games.Count - 1].GameText = gameText.ToString();
                }
            }

            if (contentType != GameMetadata.ContentType.GENERIC)
            {
                RemoveGamesOfWrongType(ref games, contentType);
            }
             
            return games.Count;
        }

        private static void RemoveGamesOfWrongType(ref ObservableCollection<GameMetadata> games, GameMetadata.ContentType contentType)
        {
            List<GameMetadata> gamesToRemove = new List<GameMetadata>();
            foreach (GameMetadata game in games)
            {
                if (game.Header.GetContentType(out _) != contentType)
                {
                    gamesToRemove.Add(game);
                }
            }

            foreach (GameMetadata game in gamesToRemove)
            {
                games.Remove(game);
            }
        }

        /// <summary>
        /// Manages state of the Chapters context menu.
        /// The isEnabled argument is true if the user's last click
        /// was on a chapter rather than elsewhere in the view.
        /// Some items are enabled according to the value of isEnable
        /// while some have a different logic (e.g. Delete Chapter
        /// is only enabled if there is more than one chapter in the workbook.
        /// </summary>
        /// <param name="cmn"></param>
        /// <param name="isEnabled"></param>
        public static void EnableChaptersMenus(ContextMenu cmn, bool isEnabled)
        {
            // ClickedIndex should be in sync with isEnabled but double check just in case
            if (LastClickedChapterId < 0)
            {
                isEnabled = false;
            }

            foreach (var item in cmn.Items)
            {
                if (item is MenuItem)
                {
                    MenuItem menuItem = item as MenuItem;
                    switch (menuItem.Name)
                    {
                        case "_mnSelectChapter":
                            menuItem.IsEnabled = isEnabled;
                            break;
                        case "_mnRenameChapter":
                            menuItem.IsEnabled = isEnabled;
                            break;
                        case "_mnAddChapter":
                            menuItem.IsEnabled = true;
                            break;
                        case "_mnDeleteChapter":
                            menuItem.IsEnabled = SessionWorkbook.Chapters.Count > 1;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the file that was read in is a Chess Forge workbook or
        /// a generic PGN file.
        /// If the former, populates the SessionWorkbook object.
        /// If the latter checks with the use what action to take:
        /// - merge selected games into a single Study Tree,
        /// - create chapters out of selected games.
        /// </summary>
        public static bool PrepareWorkbook(ref ObservableCollection<GameMetadata> games, out bool isChessForgeFile)
        {
            if (IsChessForgeWorkbook(ref games))
            {
                isChessForgeFile = true;
                return CreateWorkbookFromGameList(ref games);
            }
            else
            {
                isChessForgeFile = true;
                return CreateWorkbookFromGenericGames(ref games);
            }
        }

        /// <summary>
        /// Creates the Workbook object and populates it based on
        /// the content of the GameList.
        /// </summary>
        private static bool CreateWorkbookFromGameList(ref ObservableCollection<GameMetadata> GameList)
        {
            try
            {
                // the first "game" identifies the file as Chess Forge Workbook
                // while the rest are Study Trees, Model Games and Exercises.
                SessionWorkbook = new Workbook();

                VariationTree preface = new VariationTree(GameMetadata.ContentType.STUDY_TREE);
                PgnGameParser pp = new PgnGameParser(GameList[0].GameText, preface);
                SessionWorkbook.Description = preface.Nodes[0].Comment;

                SessionWorkbook.Title = GameList[0].GetWorkbookTitle();
                SessionWorkbook.TrainingSide = TextUtils.ConvertStringToPieceColor(GameList[0].Header.GetTrainingSide(out _));

                ProcessGames(ref WorkbookManager.VariationTreeList);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Asks the user to select games before merging.
        /// Returns the number of games merged, or -1 if the user
        /// canceled the selection dialog.
        /// </summary>
        /// <returns></returns>
        private static bool CreateWorkbookFromGenericGames(ref ObservableCollection<GameMetadata> games)
        {
            SessionWorkbook = new Workbook();
            Chapter chapter = SessionWorkbook.CreateDefaultChapter();

            int processedGames = MergeGames(ref chapter.StudyTree, ref games);

            if (processedGames == 0)
            {
                MessageBox.Show("No valid games found. No Workbook has been created.", "PGN Import", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                AppStateManager.MainWin.ShowWorkbookOptionsDialog(false);
                if (!SaveWorkbookToNewFileV2("", false))
                {
                    processedGames = 0;
                }
            }

            return processedGames > 0;
        }


        /// <summary>
        /// Processes all games in the file creating chapters as required.
        /// </summary>
        private static void ProcessGames(ref ObservableCollection<GameMetadata> GameList)
        {
            Chapter chapter = null;

            for (int i = 1; i < GameList.Count; i++)
            {
                GameMetadata gm = GameList[i];

                GameMetadata.ContentType contentType = gm.GetContentType();
                string sChapter = gm.Header.GetChapterId();
                if (IsNextChapter(chapter, i, sChapter, ref GameList))
                {
                    chapter = SessionWorkbook.CreateNewChapter();
                    chapter.Title = gm.Header.GetChapterTitle();
                }

                try
                {
                    chapter.AddGame(gm);
                }
                catch (Exception ex)
                {
                    //TODO: report errors
                }
            }
        }


        /// <summary>
        /// If the current Chapter object is null or we have a second Study Tree
        /// for the current chapter then we need to create a new Chapter.
        /// Otherwise we check the current and received chapter numbers.
        /// If the received one is non-null and different, we need a new chapter.
        /// Otherwise we continue with the current chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="gameIndex"></param>
        /// <param name="sChapterNumber"></param>
        /// <returns></returns>
        private static bool IsNextChapter(Chapter chapter, int gameIndex, string sChapterNumber, ref ObservableCollection<GameMetadata> GameList)
        {
            if (chapter == null || GameList[gameIndex].IsStudyTree() && chapter.StudyTree != null)
            {
                return true;
            }

            if (int.TryParse(sChapterNumber, out int no))
            {
                return no != chapter.Id;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Merges the passed list of games into a single Variation Tree.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="games"></param>
        /// <returns></returns>
        private static int MergeGames(ref VariationTree tree, ref ObservableCollection<GameMetadata> games)
        {
            StringBuilder sbErrors = new StringBuilder();
            int errorCount = 0;

            SelectGamesDialog dlg = new SelectGamesDialog(ref games, "Select Games to Merge into the Study Tree");
            dlg.ShowDialog();

            int mergedCount = 0;

            if (dlg.Result)
            {
                Mouse.SetCursor(Cursors.Wait);
                try
                {
                    // merge workbooks
                    for (int i = 0; i < games.Count; i++)
                    {
                        if (games[i].IsSelected)
                        {
                            if (mergedCount == 0)
                            {
                                try
                                {
                                    // special treatment for the first one
                                    PgnGameParser pgp = new PgnGameParser(games[i].GameText, AppStateManager.MainWin.ActiveVariationTree, out bool multi);
                                    // make sure it is not a FEN position
                                    if (string.IsNullOrEmpty(AppStateManager.MainWin.ActiveVariationTree.Header.GetFenString()))
                                    {
                                        mergedCount++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    sbErrors.Append("Game #" + (i + 1).ToString() + " : " + games[i].Header.BuildGameHeaderLine());
                                    sbErrors.Append(Environment.NewLine);
                                    sbErrors.Append("     " + ex.Message);
                                    sbErrors.Append(Environment.NewLine);
                                    errorCount++;
                                }
                            }
                            else
                            {
                                VariationTree workbook2 = new VariationTree(GameMetadata.ContentType.STUDY_TREE);
                                try
                                {
                                    PgnGameParser pgp = new PgnGameParser(games[i].GameText, workbook2, out bool multi);
                                    // make sure it is not a FEN position
                                    if (string.IsNullOrEmpty(workbook2.Header.GetFenString()))
                                    {
                                        tree = WorkbookTreeMerge.MergeWorkbooks(tree, workbook2);
                                        mergedCount++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    sbErrors.Append("Game #" + (i + 1).ToString() + " : " + games[i].Header.BuildGameHeaderLine());
                                    sbErrors.Append(Environment.NewLine);
                                    sbErrors.Append("     " + ex.Message);
                                    sbErrors.Append(Environment.NewLine);
                                    errorCount++;
                                }
                            }
                        }
                    }
                    if (errorCount > 0)
                    {
                        ShowMergeErrors(errorCount, mergedCount, ref sbErrors);
                    }
                }
                catch
                {
                }

                Mouse.SetCursor(Cursors.Arrow);
            }

            return mergedCount;
        }

        /// <summary>
        /// Reports errors encountered while merging
        /// </summary>
        /// <param name="errorCount"></param>
        /// <param name="mergedCount"></param>
        private static void ShowMergeErrors(int errorCount, int mergedCount, ref StringBuilder sb)
        {
            TextBoxDialog dlg = new TextBoxDialog("Merge Errors", sb.ToString());
            dlg.ShowDialog();
        }


        /// <summary>
        /// This function will be called when:
        /// 1. the user selects File->Save (userRequest == true)
        /// 2. the user exits a Training session
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

            if (AppStateManager.MainWin.ActiveVariationTree.HasTrainingMoves())
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
                AppStateManager.MainWin.ActiveVariationTree.ClearTrainingFlags();
                AppStateManager.MainWin.ActiveVariationTree.BuildLines();
                AppStateManager.SaveWorkbookFile();
                AppStateManager.MainWin.RebuildWorkbookView();
                AppStateManager.MainWin.RefreshSelectedActiveLineAndNode();
                //    saved = true;
            }
            else if (res == MessageBoxResult.No)
            {
                AppStateManager.MainWin.ActiveVariationTree.RemoveTrainingMoves();
            }

            return res;
        }

        /// <summary>
        /// Ask the user whether they intend to close the Workbook
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
                    WorkbookManager.AskToSaveWorkbookOnClose();
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
        /// This method must only be invoked when the user is closing the Workbook
        /// as it puts the app in the IDLE mode.
        /// </summary>
        /// <returns></returns>
        public static bool AskToSaveWorkbookOnClose()
        {
            if (!PromptAndSaveWorkbook(false))
            {
                // the user chose cancel so we are not closing after all
                return false;
            }

            AppStateManager.RestartInIdleMode();
            return true;
        }

        /// <summary>
        /// Updates the list of recent files and LastWorkbookFile
        /// </summary>
        /// <param name="fileName"></param>
        public static void UpdateRecentFilesList(string fileName)
        {
            Configuration.AddRecentFile(fileName);
            AppStateManager.MainWin.RecreateRecentFilesMenuItems();
            Configuration.LastWorkbookFile = fileName;
        }

        public static bool SaveWorkbookToNewFileV2(string chfFileName, bool typeConversion)
        {
            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = "pgn Workbook files (*.pgn)|*.pgn"
            };

            if (typeConversion)
            {
                saveDlg.Title = " Save Workbook converted from " + Path.GetFileName(chfFileName);
            }
            else
            {
                if (!string.IsNullOrEmpty(chfFileName))
                {
                    saveDlg.Title = " Save Workbook " + Path.GetFileName(chfFileName) + " As...";
                }
                else
                {
                    saveDlg.Title = " Save New Workbook As...";
                }
            }

            if (!string.IsNullOrEmpty(chfFileName))
            {
                saveDlg.FileName = Path.GetFileNameWithoutExtension(chfFileName) + ".pgn";
            }
            else if (!typeConversion && !string.IsNullOrWhiteSpace(AppStateManager.MainWin.SessionWorkbook.Title))
            {
                saveDlg.FileName = AppStateManager.MainWin.SessionWorkbook.Title + ".pgn";
            }

            saveDlg.OverwritePrompt = true;
            if (saveDlg.ShowDialog() == true)
            {
                string pgnFileName = saveDlg.FileName;
                AppStateManager.SaveWorkbookToNewFile(chfFileName, pgnFileName, typeConversion);
                return true;
            }
            else
            {
                return false;
            }
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
                Filter = "Workbook files (*.pgn)|*.pgn"
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

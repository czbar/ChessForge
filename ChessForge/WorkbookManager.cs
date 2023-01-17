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
using System.Reflection;
using System.Diagnostics;

namespace ChessForge
{
    /// <summary>
    /// Manages Workbook states and file manipulations.
    /// </summary>
    public class WorkbookManager
    {
        /// <summary>
        /// Type of the tab control.
        /// </summary>
        public enum TabViewType
        {
            NONE,
            CHAPTERS,
            STUDY,
            BOOKMARKS,
            MODEL_GAME,
            EXERCISE,
            TRAINING
        }

        /// <summary>
        /// Types of items that can be manipulated in the GUI
        /// </summary>
        public enum ItemType
        {
            NONE,
            CHAPTER,
            STUDY,
            MODEL_GAME,
            EXERCISE
        }

        // which tab control had focus most recently
        private static TabViewType _activeTab = TabViewType.NONE;

        /// <summary>
        /// Which tab control got focus most recently and is therefore
        /// visible in the GUI
        /// </summary>
        public static TabViewType ActiveTab
        {
            get { return _activeTab; }
            set { _activeTab = value; }
        }

        /// <summary>
        /// Determines if any of the tabs hosting an Article (study, game, exercise) is active and if there is an ActiveTree
        /// </summary>
        public static bool IsAnyArticleTabActive
        {
            get
            {
                return (SessionWorkbook != null && SessionWorkbook.ActiveVariationTree != null
                    && (_activeTab == TabViewType.STUDY || _activeTab == TabViewType.MODEL_GAME || _activeTab == TabViewType.EXERCISE));
            }
        }

        /// <summary>
        /// Workbook for the current session.
        /// </summary>
        public static Workbook SessionWorkbook;

        /// <summary>
        /// Id of the chapter which was last clicked in the Chapters view.
        /// </summary>
        public static int LastClickedChapterId = -1;

        /// <summary>
        /// Index  of the model game was last clicked in the Chapters view.
        /// </summary>
        public static int LastClickedModelGameIndex = -1;

        /// <summary>
        /// Index of the exercise which was last clicked in the Chapters view.
        /// </summary>
        public static int LastClickedExerciseIndex = -1;

        /// <summary>
        /// The list of Variation Trees (a.k.a. PGN Games) for the SessionWorkbook.
        /// This includes all types i.e. Study Tree, Model Games and Exercises.
        /// </summary>
        public static ObservableCollection<GameData> VariationTreeList = new ObservableCollection<GameData>();

        /// <summary>
        /// Resets properties.
        /// </summary>
        public static void ClearAll()
        {
            SessionWorkbook = null;
        }

        /// <summary>
        /// Creates and stores a new Workbook object.
        /// </summary>
        public static void CreateNewWorkbook()
        {
            SessionWorkbook = new Workbook();
            Chapter chapter = SessionWorkbook.CreateNewChapter();
            SessionWorkbook.ActiveChapter = chapter;
            SessionWorkbook.ActiveChapter.SetActiveVariationTree(GameData.ContentType.STUDY_TREE);
            AssignChaptersIds(ref SessionWorkbook);
        }

        /// <summary>
        /// Assigns Chapter Ids per their position on the Chapters list.
        /// The first chapter gets the id of 1.
        /// </summary>
        public static void AssignChaptersIds(ref Workbook workbook)
        {
            for (int i = 0; i < workbook.Chapters.Count; i++)
            {
                workbook.Chapters[i].Id = i + 1;
            }
        }

        /// <summary>
        /// Returns id of the last chapter
        /// </summary>
        /// <returns></returns>
        public static int GetLastChapterId(Workbook workbook)
        {
            return workbook.Chapters[workbook.Chapters.Count - 1].Id;
        }

        /// <summary>
        /// Checks if the GameList represents a Chess Forge Workbook.
        /// This is determined by the presence of the ChessForgeWorkbook
        /// header in the first game.
        /// </summary>
        /// <returns></returns>
        public static bool IsChessForgeWorkbook(ref ObservableCollection<GameData> GameList)
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
        public static int ReadPgnFile(string path, ref ObservableCollection<GameData> games,
                                      GameData.ContentType contentType,
                                      GameData.ContentType targetContentType)
        {
            games.Clear();

            // read line by line, fishing for lines with PGN headers i.e. beginning with "[" followed by a keyword.
            // Note we may accidentally hit a comment formatted that way, so make sure that the last char on the line is "]".
            GameData gm = new GameData();
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
                            gm = new GameData();
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

            if (contentType != GameData.ContentType.GENERIC || targetContentType != GameData.ContentType.NONE)
            {
                RemoveGamesOfWrongType(ref games, contentType, targetContentType);
            }

            return games.Count;
        }

        /// <summary>
        /// Removes games of certain type from the list.
        /// If targetContentType == NONE then all games of type 
        /// other than contentType will be removed.
        /// If targetContentType == MODEL_GAME then GENERIC and MODEL_GAMES
        /// will be retained.
        /// </summary>
        /// <param name="games"></param>
        /// <param name="contentType"></param>
        /// <param name="targetContentType"></param>
        public static void RemoveGamesOfWrongType(ref ObservableCollection<GameData> games,
                                                   GameData.ContentType contentType,
                                                   GameData.ContentType targetContentType)
        {
            List<GameData> gamesToRemove = new List<GameData>();
            foreach (GameData game in games)
            {
                bool keep = false;
                GameData.ContentType gameType = game.Header.GetContentType(out _);

                if (string.IsNullOrEmpty(game.GetWorkbookTitle()))
                {
                    if (gameType == contentType || gameType == targetContentType)
                    {
                        keep = true;
                    }
                    else if (gameType == GameData.ContentType.GENERIC && targetContentType == GameData.ContentType.MODEL_GAME)
                    {
                        keep = true;
                    }
                }

                if (!keep)
                {
                    gamesToRemove.Add(game);
                }
            }

            foreach (GameData game in gamesToRemove)
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
        public static void EnableChaptersContextMenuItems(ContextMenu cmn, bool isEnabled, GameData.ContentType contentType, bool isMini = false)
        {
            try
            {
                SetupChapterMenuItems(cmn, isEnabled, contentType, isMini);
                SetupModelGameMenuItems(cmn, isEnabled, contentType, isMini);
                SetupExerciseMenuItems(cmn, isEnabled, contentType, isMini);
            }
            catch (Exception ex)
            {
                AppLog.Message("Exception in EnableChaptersContextMenuItems(): " + ex.Message);
            }
        }

        /// <summary>
        /// Sets up visibility of chapter related menu items in the Chapters context menu.
        /// </summary>
        /// <param name="cmn"></param>
        /// <param name="isEnabled"></param>
        /// <param name="contentType"></param>
        private static void SetupChapterMenuItems(ContextMenu cmn, bool isEnabled, GameData.ContentType contentType, bool isMini)
        {
            // ClickedIndex should be in sync with isEnabled but double check just in case
            if (LastClickedChapterId < 0 || SessionWorkbook == null)
            {
                isEnabled = false;
            }

            bool isChaptersMenu = contentType == GameData.ContentType.GENERIC || contentType == GameData.ContentType.STUDY_TREE;

            int index = SessionWorkbook == null ? -1 : SessionWorkbook.GetChapterIndexFromId(LastClickedChapterId);

            foreach (var item in cmn.Items)
            {
                if (item is MenuItem)
                {
                    MenuItem menuItem = item as MenuItem;
                    if (isMini)
                    {
                        menuItem.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        switch (menuItem.Name)
                        {
                            case "_mnAddChapter":
                                menuItem.IsEnabled = true;
                                menuItem.Visibility = isChaptersMenu ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "_mnSelectChapter":
                                menuItem.IsEnabled = isEnabled;
                                menuItem.Visibility = isChaptersMenu ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "_mnRenameChapter":
                                menuItem.IsEnabled = isEnabled;
                                menuItem.Visibility = isChaptersMenu ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "_mnChapterUp":
                                menuItem.IsEnabled = isEnabled && SessionWorkbook != null && SessionWorkbook.Chapters.Count > 0 && index > 0;
                                menuItem.Visibility = isChaptersMenu ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "_mnChapterDown":
                                menuItem.IsEnabled = isEnabled && SessionWorkbook != null && SessionWorkbook.Chapters.Count > 0 && index < SessionWorkbook.Chapters.Count - 1;
                                menuItem.Visibility = isChaptersMenu ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "_mnImportModelGames":
                                menuItem.IsEnabled = isEnabled;
                                menuItem.Visibility = isChaptersMenu ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "_mnImportExercises":
                                menuItem.IsEnabled = isEnabled;
                                menuItem.Visibility = isChaptersMenu ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "_mnDeleteChapter":
                                menuItem.IsEnabled = isEnabled && SessionWorkbook != null && SessionWorkbook.Chapters.Count > 1;
                                menuItem.Visibility = isChaptersMenu ? Visibility.Visible : Visibility.Collapsed;
                                break;
                        }
                    }
                }
                else if (item is Separator)
                {
                    if (isMini)
                    {
                        (item as Separator).Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        if ((item as Separator).Name == "_mnChapterSepar_2")
                        {
                            (item as Separator).Visibility = isChaptersMenu ? Visibility.Visible : Visibility.Collapsed;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets up visibility of Model Game related menu items in the Chapters context menu.
        /// </summary>
        /// <param name="cmn"></param>
        /// <param name="isEnabled"></param>
        /// <param name="contentType"></param>
        private static void SetupModelGameMenuItems(ContextMenu cmn, bool isEnabled, GameData.ContentType contentType, bool isMini)
        {
            bool isGamesMenu = contentType == GameData.ContentType.MODEL_GAME;
            int index = LastClickedModelGameIndex;
            if (index < 0 || SessionWorkbook == null)
            {
                isEnabled = false;
            }

            int chapterCount = SessionWorkbook == null ? 0 : SessionWorkbook.GetChapterCount();

            foreach (var item in cmn.Items)
            {
                if (item is MenuItem)
                {
                    MenuItem menuItem = item as MenuItem;
                    switch (menuItem.Name)
                    {
                        case "_mnAddGame":
                            menuItem.IsEnabled = true;
                            menuItem.Visibility = isGamesMenu ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "_mnImportGame":
                            menuItem.IsEnabled = true;
                            menuItem.Visibility = isGamesMenu ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "_mnSelectGame":
                            menuItem.IsEnabled = isEnabled;
                            menuItem.Visibility = (isGamesMenu && !isMini) ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "_mnGameToChapter":
                            menuItem.IsEnabled = isEnabled;
                            menuItem.Visibility = (isGamesMenu && !isMini && chapterCount > 1) ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "_mnRenameGame":
                            menuItem.IsEnabled = isEnabled;
                            menuItem.Visibility = (isGamesMenu && !isMini) ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "_mnGameUp":
                            menuItem.IsEnabled = isEnabled && SessionWorkbook != null
                                && SessionWorkbook.ActiveChapter.GetModelGameCount() > 0 && index > 0;
                            menuItem.Visibility = (isGamesMenu && !isMini) ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "_mnGameDown":
                            menuItem.IsEnabled = isEnabled && SessionWorkbook != null
                                && SessionWorkbook.ActiveChapter.GetModelGameCount() > 0 && index < SessionWorkbook.ActiveChapter.GetModelGameCount() - 1;
                            menuItem.Visibility = (isGamesMenu && !isMini) ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "_mnDeleteGame":
                            menuItem.IsEnabled = isEnabled && SessionWorkbook != null && SessionWorkbook.ActiveChapter.GetModelGameCount() > 0;
                            menuItem.Visibility = (isGamesMenu && !isMini) ? Visibility.Visible : Visibility.Collapsed;
                            break;
                    }
                }
                else if (item is Separator)
                {
                    if (isMini)
                    {
                        (item as Separator).Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        /// <summary>
        /// Sets up visibility of Exercise related menu items in the Chapters context menu.
        /// </summary>
        /// <param name="cmn"></param>
        /// <param name="isEnabled"></param>
        /// <param name="contentType"></param>
        private static void SetupExerciseMenuItems(ContextMenu cmn, bool isEnabled, GameData.ContentType contentType, bool isMini)
        {
            bool isExercisesMenu = contentType == GameData.ContentType.EXERCISE;
            int index = LastClickedExerciseIndex;
            if (index < 0 || SessionWorkbook == null)
            {
                isEnabled = false;
            }

            int chapterCount = SessionWorkbook == null ? 0 : SessionWorkbook.GetChapterCount();

            foreach (var item in cmn.Items)
            {
                if (item is MenuItem)
                {
                    MenuItem menuItem = item as MenuItem;
                    switch (menuItem.Name)
                    {
                        case "_mnAddExercise":
                            menuItem.IsEnabled = true;
                            menuItem.Visibility = isExercisesMenu ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "_mnImportExercise":
                            menuItem.IsEnabled = true;
                            menuItem.Visibility = isExercisesMenu ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "_mnSelectExercise":
                            menuItem.IsEnabled = isEnabled;
                            menuItem.Visibility = (isExercisesMenu && !isMini) ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "_mnExerciseToChapter":
                            menuItem.IsEnabled = isEnabled;
                            menuItem.Visibility = (isExercisesMenu && !isMini && chapterCount > 1) ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "_mnRenameExercise":
                            menuItem.IsEnabled = isEnabled;
                            menuItem.Visibility = (isExercisesMenu && !isMini) ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "_mnExerciseUp":
                            menuItem.IsEnabled = isEnabled && SessionWorkbook != null
                                && SessionWorkbook.ActiveChapter.GetExerciseCount() > 0 && index > 0;
                            menuItem.Visibility = (isExercisesMenu && !isMini) ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "_mnExerciseDown":
                            menuItem.IsEnabled = isEnabled && SessionWorkbook != null
                                && SessionWorkbook.ActiveChapter.GetExerciseCount() > 0 && index < SessionWorkbook.ActiveChapter.GetExerciseCount() - 1;
                            menuItem.Visibility = (isExercisesMenu && !isMini) ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "_mnDeleteExercise":
                            menuItem.IsEnabled = isEnabled && SessionWorkbook != null && SessionWorkbook.ActiveChapter.GetExerciseCount() > 0;
                            menuItem.Visibility = (isExercisesMenu && !isMini) ? Visibility.Visible : Visibility.Collapsed;
                            break;
                    }
                }
                else if (item is Separator)
                {
                    if (isMini)
                    {
                        (item as Separator).Visibility = Visibility.Collapsed;
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
        public static bool PrepareWorkbook(ref ObservableCollection<GameData> games, out bool isChessForgeFile)
        {
            bool res = false;

            if (IsChessForgeWorkbook(ref games))
            {
                isChessForgeFile = true;
                SessionWorkbook = new Workbook();
                res = CreateWorkbookFromGameList(ref SessionWorkbook, ref games);
            }
            else
            {
                isChessForgeFile = true;
                res = CreateWorkbookFromGenericGames(ref games);
                if (res)
                {
                    // After import from PGN let's have a good visual indication of success
                    SessionWorkbook.ActiveChapter = SessionWorkbook.Chapters[0];
                    SessionWorkbook.ActiveChapter.IsViewExpanded = true;
                    SessionWorkbook.ActiveChapter.IsModelGamesListExpanded = true;
                    SessionWorkbook.ActiveChapter.IsExercisesListExpanded = true;
                }
            }

            return res;
        }

        /// <summary>
        /// Creates the Workbook object and populates it based on
        /// the content of the GameList.
        /// </summary>
        public static bool CreateWorkbookFromGameList(ref Workbook workbook, ref ObservableCollection<GameData> GameList)
        {
            try
            {
                // the first "game" identifies the file as Chess Forge Workbook
                // while the rest are Study Trees, Model Games and Exercises.

                VariationTree preface = new VariationTree(GameData.ContentType.STUDY_TREE);
                PgnGameParser pp = new PgnGameParser(GameList[0].GameText, preface);
                workbook.Description = preface.Nodes[0].Comment;

                workbook.Title = GameList[0].GetWorkbookTitle();
                workbook.SetVersion(GameList[0].GetWorkbookVersion());
                workbook.TrainingSideConfig = TextUtils.ConvertStringToPieceColor(GameList[0].Header.GetTrainingSide(out _));
                workbook.TrainingSideCurrent = workbook.TrainingSideConfig;

                workbook.StudyBoardOrientationConfig = TextUtils.ConvertStringToPieceColor(GameList[0].Header.GetStudyBoardOrientation(out _));
                workbook.GameBoardOrientationConfig = TextUtils.ConvertStringToPieceColor(GameList[0].Header.GetGameBoardOrientation(out _));
                workbook.ExerciseBoardOrientationConfig = TextUtils.ConvertStringToPieceColor(GameList[0].Header.GetExerciseBoardOrientation(out _));

                ProcessGames(ref GameList, ref workbook);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a Workbook from the passed list of Games.
        /// The user will select Games and/or exercises from
        /// the list.
        /// </summary>
        /// <returns></returns>
        private static bool CreateWorkbookFromGenericGames(ref ObservableCollection<GameData> games)
        {
            bool success = false;
            try
            {
                SessionWorkbook = new Workbook();
                Chapter chapter = SessionWorkbook.CreateDefaultChapter();

                if (AppStateManager.MainWin.SelectArticlesFromPgnFile(ref games, 
                                                                      SelectGamesDialog.Mode.CREATE_WORKBOOK, 
                                                                      out bool createStudy, out bool copyGames, out bool multiChapter))
                {
                    if (createStudy && !multiChapter)
                    {
                        MergeGames(ref chapter.StudyTree.Tree, ref games);
                        // the content type may have been reset to generic
                        chapter.StudyTree.Tree.ContentType = GameData.ContentType.STUDY_TREE;
                        AppStateManager.MainWin.CopySelectedItemsToChapter(chapter, copyGames, games);
                    }
                    else
                    {
                        CreateChaptersFromSelectedItems(chapter, multiChapter, createStudy, copyGames, games);
                        SessionWorkbook.ActiveChapter = chapter;
                        chapter.SetActiveVariationTree(GameData.ContentType.STUDY_TREE);
                    }

                    if (AppStateManager.MainWin.ShowWorkbookOptionsDialog(false))
                    {
                        if (SaveWorkbookToNewFileV2("", false))
                        {
                            success= true;
                        }
                    }
                }
            }
            catch
            {
                success = false;
            }

            if (success)
            {
                AppStateManager.IsDirty = true;
            }

            return success;
        }

        /// <summary>
        /// Walks the list of games and exercise, creating a new chapter
        /// for every encountered game if multiChapter is true.
        /// If multiChapter is false we assume createStudy is false too (this method should
        /// not have been called if multiChapter was false createStudy was true).
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="copyGames"></param>
        /// <param name="games"></param>
        public static void CreateChaptersFromSelectedItems(Chapter chapter, bool multiChapter, bool createStudy, bool copyGames, ObservableCollection<GameData> games)
        {
            bool firstChapter = true;

            foreach (GameData gd in games)
            {
                if (gd.IsSelected)
                {
                    if ((gd.GetContentType() == GameData.ContentType.GENERIC || gd.GetContentType() == GameData.ContentType.MODEL_GAME))
                    {
                        if (!firstChapter && multiChapter)
                        {
                            chapter = SessionWorkbook.CreateNewChapter();
                        }

                        if (createStudy)
                        {
                            chapter.AddArticle(gd, GameData.ContentType.STUDY_TREE, GameData.ContentType.STUDY_TREE);
                            chapter.StudyTree.Tree.ContentType = GameData.ContentType.STUDY_TREE;
                        }

                        if (copyGames)
                        {
                            chapter.AddArticle(gd, GameData.ContentType.MODEL_GAME, GameData.ContentType.MODEL_GAME);
                        }
                        firstChapter = false;
                    }
                    else if (gd.GetContentType() == GameData.ContentType.EXERCISE)
                    {
                        chapter.AddArticle(gd, GameData.ContentType.EXERCISE, GameData.ContentType.EXERCISE);
                    }
                }
            }
        }

        /// <summary>
        /// Processes all games in the file creating chapters as required.
        /// </summary>
        private static void ProcessGames(ref ObservableCollection<GameData> GameList, ref Workbook workbook)
        {
            Chapter chapter = null;

            StringBuilder sbErrors = new StringBuilder();
            int errorCount = 0;

            for (int i = 1; i < GameList.Count; i++)
            {
                GameData gm = GameList[i];

                if (gm.GetContentType() == GameData.ContentType.STUDY_TREE)
                {
                    chapter = workbook.CreateNewChapter();
                    chapter.SetTitle(gm.Header.GetChapterTitle());
                }

                try
                {
                    // force creation of GUID if absent
                    gm.Header.GetGuid(out _);
                    chapter.AddArticle(gm, GameData.ContentType.GENERIC);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    sbErrors.AppendLine(BuildGameParseErrorText(chapter, i + 1, GameList[i], ex));
                }
            }

            if (errorCount > 0)
            {
                ShowPgnProcessingErrors("Workbook Parsing Errors", ref sbErrors);
            }
        }

        /// <summary>
        /// Merges the selected games from the passed list into a single Variation Tree.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="games"></param>
        /// <returns></returns>
        public static int MergeGames(ref VariationTree tree, ref ObservableCollection<GameData> games)
        {
            StringBuilder sbErrors = new StringBuilder();
            int errorCount = 0;
            int mergedCount = 0;

            Mouse.SetCursor(Cursors.Wait);
            try
            {
                // merge workbooks
                for (int i = 0; i < games.Count; i++)
                {
                    // check if this a game, not an exercise
                    if (games[i].IsSelected && string.IsNullOrEmpty(games[i].Header.GetFenString()))
                    {
                        if (mergedCount == 0)
                        {
                            try
                            {
                                // special treatment for the first one
                                PgnGameParser pgp = new PgnGameParser(games[i].GameText, WorkbookManager.SessionWorkbook.ActiveChapter.StudyTree.Tree, out bool multi);
                            }
                            catch (Exception ex)
                            {
                                sbErrors.AppendLine(BuildGameParseErrorText(null, i + 1, games[i], ex));
                                errorCount++;
                            }
                            mergedCount++;
                        }
                        else
                        {
                            VariationTree workbook2 = new VariationTree(GameData.ContentType.STUDY_TREE);
                            try
                            {
                                PgnGameParser pgp = new PgnGameParser(games[i].GameText, workbook2, out bool multi);
                            }
                            catch (Exception ex)
                            {
                                sbErrors.AppendLine(BuildGameParseErrorText(null, i + 1, games[i], ex));
                                errorCount++;
                            }
                            tree = WorkbookTreeMerge.MergeWorkbooks(tree, workbook2);
                            mergedCount++;
                        }
                    }
                }
                if (errorCount > 0)
                {
                    ShowPgnProcessingErrors("Merge Errors", ref sbErrors);
                }
            }
            catch
            {
            }

            Mouse.SetCursor(Cursors.Arrow);

            return mergedCount;
        }

        /// <summary>
        /// Builds text for the individual parsing error.
        /// If chapter != null this is occurring while reading a Workbook
        /// otherwise while reading generic PGN (e.g. importing)
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="gameNo"></param>
        /// <param name="game"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        private static string BuildGameParseErrorText(Chapter chapter, int gameNo, GameData game, Exception ex)
        {
            StringBuilder sb = new StringBuilder();

            if (chapter != null)
            {
                sb.Append("Chapter " + chapter.Id.ToString() + ": " + game.GetContentType().ToString() + ": " + game.Header.BuildGameHeaderLine());
            }
            else
            {
                sb.Append("PGN Item #" + gameNo.ToString() + " : " + game.Header.BuildGameHeaderLine());
            }

            sb.Append(Environment.NewLine);
            sb.Append("     " + ex.Message);
            sb.Append(Environment.NewLine);

            return sb.ToString();
        }

        /// <summary>
        /// Reports errors encountered while merging
        /// </summary>
        private static void ShowPgnProcessingErrors(string dlgTitle, ref StringBuilder sb)
        {
            TextBoxDialog dlg = new TextBoxDialog(dlgTitle, sb.ToString())
            {
                Left = AppStateManager.MainWin.ChessForgeMain.Left + 100,
                Top = AppStateManager.MainWin.ChessForgeMain.Top + 100,
                Topmost = false,
                Owner = AppStateManager.MainWin
            };
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

            if (AppStateManager.ActiveVariationTree != null && AppStateManager.ActiveVariationTree.HasTrainingMoves())
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
                        // this was prompted by an action other than File->Save 
                        // Ask, or proceed without asking of AutoSave is enabled
                        if (!Configuration.AutoSave)
                        {
                            MessageBoxButton mbb = isAppClosing ? MessageBoxButton.YesNo : MessageBoxButton.YesNoCancel;
                            res = MessageBox.Show("Save the Workbook?", "Workbook not saved", mbb, MessageBoxImage.Question);
                        }
                        if (Configuration.AutoSave || res == MessageBoxResult.Yes)
                        {
                            AppStateManager.SaveWorkbookFile();
                            res = MessageBoxResult.Yes;
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

            string origin;

            switch (AppStateManager.ActiveVariationTree.ContentType)
            {
                case GameData.ContentType.STUDY_TREE:
                    origin = "Study";
                    break;
                case GameData.ContentType.MODEL_GAME:
                    origin = "Game";
                    break;
                case GameData.ContentType.EXERCISE:
                    origin = "Exercise";
                    break;
                default:
                    origin = "Workbook";
                    break;
            }

            string message = string.Format("Merge and Save new moves from this session into the {0}?", origin);

            res = MessageBox.Show(message, "Chess Forge Save Workbook",
                buttons, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                VariationTree activeTree = AppStateManager.ActiveVariationTree;

                // prepare data for undo
                EditOperation op = new EditOperation(EditOperation.EditType.SAVE_TRAINING_MOVES, activeTree.GetListOfNodeIds(false), null);
                activeTree.OpsManager.PushOperation(op);

                activeTree.ClearTrainingFlags();
                activeTree.BuildLines();
                AppStateManager.SaveWorkbookFile();
                AppStateManager.MainWin.RebuildActiveTreeView();
                AppStateManager.MainWin.RefreshSelectedActiveLineAndNode();
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

            // if this is a new Workbook suggest file name based on title.
            if (pgnFileName == null && WorkbookManager.SessionWorkbook != null && !string.IsNullOrWhiteSpace(WorkbookManager.SessionWorkbook.Title))
            {
                string title = TextUtils.RemoveInvalidCharsFromFileName(WorkbookManager.SessionWorkbook.Title);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    saveDlg.FileName = title + ".pgn";
                }
            }

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

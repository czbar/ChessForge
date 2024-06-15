using ChessPosition;
using ChessPosition.GameTree;
using GameTree;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Manages Workbook states and file manipulations.
    /// </summary>
    public class WorkbookManager
    {
        /// <summary>
        /// Types of items that can be manipulated in the GUI
        /// </summary>
        public enum ItemType
        {
            NONE,
            CHAPTER,
            INTRO,
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
        public static int LastClickedChapterIndex = -1;

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
            SessionWorkbook.IsReady = true;
            chapter.StudyTree.IsReady = true;
            SessionWorkbook.ActiveChapter = chapter;
            SessionWorkbook.ActiveChapter.SetActiveVariationTree(GameData.ContentType.STUDY_TREE);
            BookmarkManager.ResetSelections();
        }

        /// <summary>
        /// Removes all ArticleRefs to the Articles with the passed guid.
        /// Returns the list of all affected nodes.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static List<FullNodeId> RemoveArticleReferences(string guid)
        {
            try
            {
                return SessionWorkbook.RemoveArticleReferences(guid);
            }
            catch
            {
                return null;
            }
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
            if (fileName == AppState.WorkbookFilePath && AppState.CurrentLearningMode != LearningMode.Mode.IDLE)
            {
                string s = Properties.Resources.FileAlreadyOpen;
                s = s.Replace("$0", Path.GetFileName(fileName));
                MessageBox.Show(s, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (File.Exists(fileName))
            {
                return true;
            }
            else
            {
                AppState.MainWin.BoardCommentBox.OpenFile();

                if (isLastOpen)
                {
                    string s = Properties.Resources.LastFileNotFound;
                    s = s.Replace("$0", fileName);
                    MessageBox.Show(s, Properties.Resources.Information, MessageBoxButton.OK);
                }
                else
                {
                    string s = Properties.Resources.FileNotFound;
                    s = s.Replace("$0", fileName);
                    MessageBox.Show(s, Properties.Resources.Information, MessageBoxButton.OK);
                }

                Configuration.RemoveFromRecentFiles(fileName);
                AppState.MainWin.RecreateRecentFilesMenuItems();

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

            try
            {
                Mouse.SetCursor(Cursors.Wait);
                // read line by line, fishing for lines with PGN headers i.e. beginning with "[" followed by a keyword.
                // Note we may accidentally hit a comment formatted that way, so make sure that the last char on the line is "]".
                GameData gm = new GameData();
                gm.FirstLineInFile = 1;

                // TODO: need something more elegant here, perhpas another parameter
                bool isFile = !path.Contains("\n");

                using (TextReader sr = isFile ? (new StreamReader(path) as TextReader) : (new StringReader(path) as TextReader))
                {
                    StringBuilder gameText = new StringBuilder();
                    int lineNo = 0;
                    bool headerLine = true;

                    while (sr.Peek() >= 0)
                    {
                        string line = sr.ReadLine();

                        // TODO: switch to using PgnMultiGameParser.ParsePgnMultiGameText
                        lineNo++;
                        headerLine = true;

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
            }
            catch (Exception ex)
            {
                AppLog.Message("ReadPgnFile()", ex);
            }

            Mouse.SetCursor(Cursors.Wait);
            return games.Count;
        }

        /// <summary>
        /// Inserts articles from the passed list into the current Workbook. 
        /// </summary>
        /// <param name="games"></param>
        public static void InsertArticles(ObservableCollection<GameData> games)
        {
            Chapter currChapter = AppState.ActiveChapter;
            int firstAddedIndex = -1;
            GameData.ContentType firstAddedType = GameData.ContentType.NONE;

            bool hasStudyBeforeIntro = false;

            List<ArticleListItem> undoArticleList = new List<ArticleListItem>();

            foreach (GameData game in games)
            {
                ArticleListItem undoItem = null;

                int index = -1;

                GameData.ContentType currContentType = game.GetContentType(false);

                if (currContentType == GameData.ContentType.STUDY_TREE)
                {
                    currChapter = AppState.Workbook.CreateNewChapter();
                    currChapter.SetTitle(game.Header.GetChapterTitle());

                    undoItem = new ArticleListItem(currChapter);
                    undoArticleList.Add(undoItem);

                    PgnArticleUtils.AddArticle(currChapter, game, GameData.ContentType.STUDY_TREE, out _);
                    if (firstAddedType == GameData.ContentType.NONE)
                    {
                        firstAddedType = GameData.ContentType.STUDY_TREE;
                    }

                    undoItem = new ArticleListItem(currChapter, currChapter.Index, currChapter.StudyTree, -1);
                    undoArticleList.Add(undoItem);

                    hasStudyBeforeIntro = true;
                }
                else if (currContentType == GameData.ContentType.INTRO)
                {
                    if (!hasStudyBeforeIntro)
                    {
                        currChapter = AppState.Workbook.CreateNewChapter();
                        if (firstAddedType == GameData.ContentType.NONE)
                        {
                            firstAddedType = GameData.ContentType.INTRO;
                        }
                    }

                    undoItem = new ArticleListItem(currChapter);
                    undoArticleList.Add(undoItem);

                    PgnArticleUtils.AddArticle(currChapter, game, GameData.ContentType.INTRO, out _);
                    hasStudyBeforeIntro = false;

                    undoItem = new ArticleListItem(currChapter, currChapter.Index, currChapter.Intro, -1);
                    undoArticleList.Add(undoItem);

                }
                else
                {
                    GameData.ContentType contentType = game.GetContentType(true);
                    if (game.GetWorkbookTitle() == null)
                    {
                        index = PgnArticleUtils.AddArticle(currChapter, game, contentType, out _);
                        if (firstAddedType == GameData.ContentType.NONE)
                        {
                            firstAddedType = game.GetContentType(false);
                        }
                        if (firstAddedIndex < 0)
                        {
                            firstAddedIndex = index;
                        }

                        undoItem = new ArticleListItem(currChapter, currChapter.Index, currChapter.GetArticleAtIndex(contentType, index), index);
                        undoArticleList.Add(undoItem);
                    }
                }
            }
            AppState.IsDirty = true;
            AppState.MainWin.SelectArticle(currChapter.Index, firstAddedType, firstAddedIndex);

            WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.INSERT_ARTICLES, (object)undoArticleList);
            WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
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
        /// is only enabled if there is more than one chapter in the workbook.)
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
            if (LastClickedChapterIndex < 0 || SessionWorkbook == null)
            {
                isEnabled = false;
            }

            bool isChaptersMenu = contentType == GameData.ContentType.GENERIC || contentType == GameData.ContentType.STUDY_TREE || contentType == GameData.ContentType.INTRO;

            int index = SessionWorkbook == null ? -1 : LastClickedChapterIndex;

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
                            case "_mnImportChapter":
                                menuItem.IsEnabled = true;
                                menuItem.Visibility = Visibility.Visible;
                                break;
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
                            case "UiMnChptCreateIntro":
                                menuItem.IsEnabled = isEnabled;
                                if (index >= 0 && index <= SessionWorkbook.Chapters.Count
                                    && !SessionWorkbook.Chapters[index].ShowIntro)
                                {
                                    menuItem.Visibility = isChaptersMenu ? Visibility.Visible : Visibility.Collapsed;
                                }
                                else
                                {
                                    menuItem.Visibility = Visibility.Collapsed;
                                }
                                break;
                            case "_mnMergeChapters":
                                menuItem.IsEnabled = isEnabled;
                                menuItem.Visibility = Visibility.Visible;
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
                        (item as Separator).Visibility = Visibility.Visible;

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
        /// <param name="showEnabled"></param>
        /// <param name="contentType"></param>
        private static void SetupModelGameMenuItems(ContextMenu cmn, bool showEnabled, GameData.ContentType contentType, bool isMini)
        {
            // TODO: showEnabled is NEVER false, Is this historical then?
            bool isEnabled = showEnabled;

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
                            menuItem.Visibility = (isGamesMenu && !isMini) ? Visibility.Visible : Visibility.Collapsed;
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
                        case "UiMnDeleteGames":
                            menuItem.IsEnabled = showEnabled && SessionWorkbook != null && SessionWorkbook.HasAnyModelGames;
                            menuItem.Visibility = !isMini ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "UiMnChapterScores":
                            menuItem.IsEnabled = showEnabled && SessionWorkbook != null;
                            menuItem.Visibility = !isMini ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "UiMnWorkbookScores":
                            menuItem.IsEnabled = showEnabled && SessionWorkbook != null;
                            menuItem.Visibility = !isMini ? Visibility.Visible : Visibility.Collapsed;
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
        private static void SetupExerciseMenuItems(ContextMenu cmn, bool showEnabled, GameData.ContentType contentType, bool isMini)
        {
            // TODO: showEnabled is NEVER false, Is this historical then?
            bool isEnabled = showEnabled;

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
                        case "UiMnDeleteExercises":
                            menuItem.IsEnabled = showEnabled && SessionWorkbook != null && SessionWorkbook.HasAnyExercises;
                            menuItem.Visibility = !isMini ? Visibility.Visible : Visibility.Collapsed;
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
            bool res;

            if (IsChessForgeWorkbook(ref games))
            {
                isChessForgeFile = true;
                SessionWorkbook = new Workbook();
                res = CreateWorkbookFromGameList(ref SessionWorkbook, ref games);
                BookmarkManager.ResetSelections();
            }
            else
            {
                isChessForgeFile = false;
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
            SessionWorkbook.OpsManager.Reset();
            WorkbookLocationNavigator.Reset();

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
                PgnGameParser pp = new PgnGameParser(GameList[0].GameText, preface, null);
                workbook.Description = preface.Nodes[0].Comment;

                workbook.Title = GameList[0].GetWorkbookTitle();
                workbook.Author = GameList[0].GetAnnotator();
                workbook.SetVersion(GameList[0].GetWorkbookVersion());
                workbook.Guid = GameList[0].GetGuid();
                workbook.TrainingSideConfig = TextUtils.ConvertStringToPieceColor(GameList[0].Header.GetTrainingSide(out _));
                workbook.TrainingSideCurrent = workbook.TrainingSideConfig;

                workbook.StudyBoardOrientationConfig = TextUtils.ConvertStringToPieceColor(GameList[0].Header.GetStudyBoardOrientation(out _));
                workbook.GameBoardOrientationConfig = TextUtils.ConvertStringToPieceColor(GameList[0].Header.GetGameBoardOrientation(out _));
                workbook.ExerciseBoardOrientationConfig = TextUtils.ConvertStringToPieceColor(GameList[0].Header.GetExerciseBoardOrientation(out _));

                ProcessGamesInBackground(ref GameList, ref workbook);

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
                BookmarkManager.ResetSelections();

                if (AppState.MainWin.SelectArticlesFromPgnFile(ref games, SelectGamesDialog.Mode.CREATE_WORKBOOK))
                {
                    // insert a dummy article at position 0
                    games.Insert(0, null);
                    ProcessGamesInBackground(ref games, ref SessionWorkbook, chapter);

                    if (AppState.MainWin.ShowWorkbookOptionsDialog())
                    {
                        if (SaveWorkbookToNewFileV2(""))
                        {
                            success = true;
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
                AppState.IsDirty = true;
            }

            return success;
        }

#if false
        /// <summary>
        /// Walks the list of games and exercises, creating a new chapter
        /// for every encountered game if multiChapter is true.
        /// If multiChapter is false we assume createStudy is false too (this method should
        /// not have been called if multiChapter was false createStudy was true).
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="copyGames"></param>
        /// <param name="games"></param>
        public static void CreateChaptersFromSelectedItems(Chapter chapter, bool multiChapter, bool createStudy, bool copyGames, ObservableCollection<GameData> games)
        {
            StringBuilder sbErrors = new StringBuilder();
            bool firstChapter = true;

            string errorString = "";
            int errorCount = 0;
            int chapterIndex = 0;
            int itemIndex = 0;

            foreach (GameData gd in games)
            {
                if (gd.IsSelected)
                {
                    GameData.ContentType contentType = gd.GetContentType(true);
                    if (contentType == GameData.ContentType.MODEL_GAME)
                    {
                        if (!firstChapter && multiChapter)
                        {
                            chapter = SessionWorkbook.CreateNewChapter();
                            chapterIndex++;
                        }

                        if (createStudy)
                        {
                            chapter.AddArticle(gd, GameData.ContentType.STUDY_TREE, out errorString, GameData.ContentType.STUDY_TREE);
                            chapter.StudyTree.Tree.ContentType = GameData.ContentType.STUDY_TREE;
                            if (!string.IsNullOrEmpty(errorString))
                            {
                                if (multiChapter)
                                {
                                    sbErrors.AppendLine(Properties.Resources.Chapter + " " + (chapterIndex - 1).ToString() + ": ");
                                }
                                sbErrors.AppendLine(Properties.Resources.Study + ": " + errorString);
                                errorCount++;
                            }
                        }

                        if (copyGames)
                        {
                            chapter.AddArticle(gd, GameData.ContentType.MODEL_GAME, out errorString, GameData.ContentType.MODEL_GAME);
                            if (!string.IsNullOrEmpty(errorString))
                            {
                                sbErrors.AppendLine(BuildGameParseErrorText(chapter, itemIndex + 1, gd, errorString));
                                errorCount++;
                            }
                        }
                        firstChapter = false;
                    }
                    else if (contentType == GameData.ContentType.EXERCISE)
                    {
                        chapter.AddArticle(gd, GameData.ContentType.EXERCISE, out errorString, GameData.ContentType.EXERCISE);
                        if (!string.IsNullOrEmpty(errorString))
                        {
                            sbErrors.AppendLine(BuildGameParseErrorText(chapter, itemIndex + 1, gd, errorString));
                            errorCount++;
                        }
                    }
                    itemIndex++;
                }
            }

            if (errorCount > 0)
            {
                ShowPgnProcessingErrors(Properties.Resources.DlgParseErrors, ref sbErrors);
            }
        }
#endif

        /// <summary>
        /// Processes the list games in the background.
        /// First, creates a list of unprocessed articles,
        /// and then invokes the Background Manager to process them.
        /// </summary>
        /// <param name="rawPgnArticles"></param>
        /// <param name="workbook"></param>
        private static void ProcessGamesInBackground(ref ObservableCollection<GameData> rawPgnArticles, ref Workbook workbook, Chapter chapter = null)
        {
            List<Article> outArticles = workbook.CreateArticlePlaceholders(rawPgnArticles, chapter);
            workbook.GamesManager.Execute(rawPgnArticles, outArticles);
        }

#if false
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

                if (gm.GetContentType(true) == GameData.ContentType.STUDY_TREE)
                {
                    chapter = workbook.CreateNewChapter();
                    chapter.SetTitle(gm.Header.GetChapterTitle());
                    chapter.Guid = gm.Header.GetOrGenerateGuid(out bool generated);
                    if (generated)
                    {
                        AppState.IsDirty = true;
                    }
                }

                try
                {
                    // force creation of GUID if absent
                    gm.Header.GetGuid(out _);
                    chapter.AddArticle(gm, GameData.ContentType.GENERIC, out string error);
                    if (!string.IsNullOrEmpty(error))
                    {
                        errorCount++;
                        sbErrors.AppendLine(BuildGameParseErrorText(chapter, i + 1, GameList[i], error));
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    string message;

                    if (ex is ParserException)
                    {
                        message = GuiUtilities.TranslateParseException(ex as ParserException);
                    }
                    else
                    {
                        message = ex.Message;
                    }
                    sbErrors.AppendLine(BuildGameParseErrorText(chapter, i + 1, GameList[i], message));
                }
            }

            if (errorCount > 0)
            {
                ShowPgnProcessingErrors(Properties.Resources.DlgMergeErrors, ref sbErrors);
            }
        }
#endif

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
                    // check if this is a game, not an exercise
                    if (games[i].IsSelected && games[i].Header.IsGame())
                    {
                        if (mergedCount == 0)
                        {
                            try
                            {
                                // special treatment for the first one
                                GameHeader gh = WorkbookManager.SessionWorkbook.ActiveChapter.StudyTree.Tree.Header.CloneMe(true);
                                PgnGameParser pgp = new PgnGameParser(games[i].GameText, WorkbookManager.SessionWorkbook.ActiveChapter.StudyTree.Tree, null);
                                WorkbookManager.SessionWorkbook.ActiveChapter.StudyTree.Tree.Header = gh;
                            }
                            catch (Exception ex)
                            {
                                sbErrors.AppendLine(BuildGameParseErrorText(null, i + 1, games[i], ex.Message));
                                errorCount++;
                            }
                            mergedCount++;
                        }
                        else
                        {
                            VariationTree workbook2 = new VariationTree(GameData.ContentType.STUDY_TREE);
                            try
                            {
                                PgnGameParser pgp = new PgnGameParser(games[i].GameText, workbook2, null);
                            }
                            catch (Exception ex)
                            {
                                string message;
                                errorCount++;
                                if (ex is ParserException)
                                {
                                    message = GuiUtilities.TranslateParseException(ex as ParserException);
                                }
                                else
                                {
                                    message = ex.Message;
                                }
                                sbErrors.AppendLine(BuildGameParseErrorText(null, i + 1, games[i], message));
                            }
                            tree = TreeMerge.MergeVariationTrees(tree, workbook2);
                            mergedCount++;
                        }
                    }
                }
                if (errorCount > 0)
                {
                    ShowPgnProcessingErrors(Properties.Resources.DlgMergeErrors, ref sbErrors);
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
        /// If chapterIndex >= 0 this is occurring while reading a Workbook
        /// otherwise while reading generic PGN (e.g. importing)
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="gameNo"></param>
        /// <param name="game"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static string BuildGameParseErrorText(int chapterIndex, int gameNo, GameData game, string msg)
        {
            StringBuilder sb = new StringBuilder();

            if (chapterIndex >= 0)
            {
                sb.Append(Properties.Resources.Chapter + " " + (chapterIndex + 1).ToString() + ": " + game.GetContentType(false).ToString() + ": " + game.Header.BuildGameHeaderLine(false));
            }
            else
            {
                sb.Append("PGN " + Properties.Resources.Item + " #" + gameNo.ToString() + " : " + game.Header.BuildGameHeaderLine(false));
            }

            sb.Append(Environment.NewLine);
            sb.Append("     " + msg);
            sb.Append(Environment.NewLine);

            return sb.ToString();
        }

        /// <summary>
        /// Reports errors encountered while merging
        /// </summary>
        public static void ShowPgnProcessingErrors(string dlgTitle, ref StringBuilder sb)
        {
            TextBoxDialog dlg = new TextBoxDialog(dlgTitle, sb.ToString());
            //{
            //    Left = AppState.MainWin.ChessForgeMain.Left + 100,
            //    Top = AppState.MainWin.ChessForgeMain.Top + 100,
            //    Topmost = false,
            //    Owner = AppState.MainWin
            //};
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);
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
        public static bool PromptAndSaveWorkbook(bool userRequest, out bool saved, bool isAppClosing = false)
        {
            saved = false;

            MessageBoxResult res = MessageBoxResult.None;

            if (AppState.ActiveVariationTree != null && AppState.ActiveVariationTree.HasTrainingMoves())
            {
                res = PromptAndSaveTrainingMoves(userRequest, isAppClosing);
                if (res == MessageBoxResult.Yes)
                {
                    saved = true;
                }
            }

            if (res != MessageBoxResult.OK && res != MessageBoxResult.Cancel)
            {
                // not saved yet
                if (userRequest)
                {
                    // user requested File->Save so proceed...
                    AppState.SaveWorkbookFile(null);
                }
                else
                {
                    if (AppState.IsDirty && (isAppClosing || !TrainingSession.IsTrainingInProgress))
                    {
                        // this was prompted by an action other than File->Save 
                        // Ask, or proceed without asking of AutoSave is enabled
                        if (!Configuration.AutoSave)
                        {
                            MessageBoxButton mbb = isAppClosing ? MessageBoxButton.YesNo : MessageBoxButton.YesNoCancel;
                            res = MessageBox.Show(Properties.Resources.SaveWorkbook, Properties.Resources.WorkbookNotSaved, mbb, MessageBoxImage.Question);
                        }
                        if (Configuration.AutoSave || res == MessageBoxResult.Yes)
                        {
                            bool saveResult = AppState.SaveWorkbookFile(null);
                            if (saveResult)
                            {
                                res = MessageBoxResult.Yes;
                            }
                            else
                            {
                                if (isAppClosing)
                                {
                                    // if app is closing and we failed to save, alert the user via exception
                                    throw new Exception("Failed to save on app exit.");
                                }
                                else
                                {
                                    res = MessageBoxResult.Cancel;
                                }
                            }
                        }
                    }
                    else
                    {
                        // not dirty and not user request so this is on close. Return Yes in order not to prevent closing 
                        res = MessageBoxResult.Yes;
                    }
                }
            }

            AppState.ConfigureSaveMenus();

            return res == MessageBoxResult.Yes || res == MessageBoxResult.No;
        }

        /// <summary>
        /// No real merge happening here, since the moves are already in the tree.
        /// They just need to have their training flag removed.
        /// The source view must be rebuilt.
        /// </summary>
        /// <param name="isAppClosing"></param>
        public static void MergeLineFromTraining(bool isAppClosing = false)
        {
            VariationTree activeTree = AppState.ActiveVariationTree;

            // prepare data for undo
            EditOperation op = new EditOperation(EditOperation.EditType.SAVE_TRAINING_MOVES, activeTree.GetListOfNodeIds(false), null);
            activeTree.OpsManager.PushOperation(op);

            activeTree.ClearTrainingFlags();
            activeTree.BuildLines();
            if (isAppClosing)
            {
                AppState.SaveWorkbookFile(null);
            }
            else
            {
                AppState.IsDirty = true;
            }
            AppState.MainWin.RebuildActiveTreeView();
            AppState.MainWin.RefreshSelectedActiveLineAndNode();
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

            switch (AppState.ActiveVariationTree.ContentType)
            {
                case GameData.ContentType.STUDY_TREE:
                    origin = Properties.Resources.Study;
                    break;
                case GameData.ContentType.MODEL_GAME:
                    origin = Properties.Resources.Game;
                    break;
                case GameData.ContentType.EXERCISE:
                    origin = Properties.Resources.Exercise;
                    break;
                default:
                    origin = Properties.Resources.Workbook;
                    break;
            }

            string message = Properties.Resources.MergeTrainingIntoStudy + " (" + origin + ")?";

            res = MessageBox.Show(message, Properties.Resources.SaveWorkbook,
                buttons, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                MergeLineFromTraining(isAppClosing);
            }
            else if (res == MessageBoxResult.No)
            {
                AppState.MainWin.ActiveVariationTree.RemoveTrainingMoves();
            }

            return res;
        }

        /// <summary>
        /// Check/ask whether to save the current workbook.
        /// </summary>
        /// <returns></returns>
        public static bool AskToCloseWorkbook()
        {
            if (AppState.CurrentLearningMode != LearningMode.Mode.IDLE)
            {
                AskToSaveWorkbookOnClose();
                // if we are not in IDLE mode then the user did not close
                if (AppState.CurrentLearningMode != LearningMode.Mode.IDLE)
                {
                    return false;
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
            if (SessionWorkbook != null)
            {
                if (!PromptAndSaveWorkbook(false, out _))
                {
                    // the user chose cancel so we are not closing after all
                    return false;
                }
                else
                {
                    SessionWorkbook.GamesManager.CancelAll();
                }

                WorkbookViewState wvs = new WorkbookViewState(SessionWorkbook);
                wvs.SaveState();

                AppState.RestartInIdleMode();
            }
            return true;
        }

        /// <summary>
        /// Updates the list of recent files and LastWorkbookFile
        /// </summary>
        /// <param name="fileName"></param>
        public static void UpdateRecentFilesList(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                Configuration.AddRecentFile(fileName);
                AppState.MainWin.RecreateRecentFilesMenuItems();
                Configuration.LastWorkbookFile = fileName;
            }
        }

        public static bool SaveWorkbookToNewFileV2(string chfFileName)
        {
            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = Properties.Resources.WorkbookFiles + " (*.pgn)|*.pgn"
            };


            if (!string.IsNullOrEmpty(chfFileName))
            {
                string s = " " + Properties.Resources.SaveWorkbookAs;
                s = s.Replace("$0", Path.GetFileName(chfFileName));
                saveDlg.Title = s;
            }
            else
            {
                saveDlg.Title = " " + Properties.Resources.SaveNewWorkbookAs;
            }

            if (!string.IsNullOrEmpty(chfFileName))
            {
                saveDlg.FileName = Path.GetFileNameWithoutExtension(chfFileName) + ".pgn";
            }
            else if (!string.IsNullOrWhiteSpace(AppState.MainWin.SessionWorkbook.Title))
            {
                saveDlg.FileName = AppState.MainWin.SessionWorkbook.Title + ".pgn";
            }

            saveDlg.OverwritePrompt = true;
            if (saveDlg.ShowDialog() == true)
            {
                string pgnFileName = saveDlg.FileName;
                AppState.SaveWorkbookToNewFile(chfFileName, pgnFileName);
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
        public static bool SaveWorkbookToNewFile(string pgnFileName)
        {
            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = Properties.Resources.WorkbookFiles + " (*.pgn)|*.pgn"
            };

            // if this is a new Workbook, suggest file name based on title.
            if (pgnFileName == null && WorkbookManager.SessionWorkbook != null && !string.IsNullOrWhiteSpace(WorkbookManager.SessionWorkbook.Title))
            {
                string title = TextUtils.RemoveInvalidCharsFromFileName(WorkbookManager.SessionWorkbook.Title);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    saveDlg.FileName = title + ".pgn";
                }
            }

            if (!string.IsNullOrEmpty(pgnFileName))
            {
                string s = " " + Properties.Resources.SaveWorkbookAs;
                s = s.Replace("$0", Path.GetFileName(pgnFileName));
                saveDlg.Title = s;
            }
            else
            {
                saveDlg.Title = " " + Properties.Resources.SaveNewWorkbookAs;
            }

            saveDlg.OverwritePrompt = true;
            if (saveDlg.ShowDialog() == true)
            {
                string chfFileName = saveDlg.FileName;
                AppState.SaveWorkbookToNewFile(pgnFileName, chfFileName);
                return true;
            }
            else
            {
                return false;
            }
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
        private static string BuildGameParseErrorText(Chapter chapter, int gameNo, GameData game, string msg)
        {
            int chapterIndex = chapter == null ? -1 : chapter.Index;
            return BuildGameParseErrorText(chapterIndex, gameNo, game, msg);
        }

    }
}

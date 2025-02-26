using ChessPosition;
using GameTree;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Static methods for importing articles from PGN files.
    /// </summary>
    public class ImportFromPgn
    {
        /// <summary>
        /// Invokes the Import PGN dialog to allow the user to select games
        /// to merge into a new Study Tree from which a Chapter will be created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void ImportChapter()
        {
            string[] fileNames = ImportFromPgn.SelectPgnFile(false);
            string fileName = null;
            if (fileNames.Length == 1)
            {
                fileName = fileNames[0];
            }
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                ObservableCollection<GameData> games = new ObservableCollection<GameData>();

                int gamesCount = WorkbookManager.ReadPgnFile(fileName, ref games, GameData.ContentType.GENERIC, GameData.ContentType.NONE);

                // if this is a ChessForge Workbook, list the chapters and allow the user to copy them over
                if (WorkbookManager.IsChessForgeWorkbook(ref games))
                {
                    Workbook workbook = new Workbook();
                    WorkbookManager.CreateWorkbookFromGameList(ref workbook, ref games);
                    SelectChaptersDialog dlg = new SelectChaptersDialog(workbook);
                    GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                    dlg.ShowDialog();
                    if (dlg.ExitOK)
                    {
                        List<ArticleListItem> undoArticleList = new List<ArticleListItem>();

                        int importedChapters = 0;

                        foreach (SelectedChapter ch in dlg.ChapterList)
                        {
                            if (ch.IsSelected)
                            {
                                WorkbookManager.SessionWorkbook.Chapters.Add(ch.Chapter);
                                undoArticleList.Add(new ArticleListItem(ch.Chapter));

                                importedChapters++;

                                if (AppState.MainWin.ChaptersView != null)
                                {
                                    AppState.MainWin.ChaptersView.BuildFlowDocumentForChaptersView(false);
                                    PulseManager.ChapterIndexToBringIntoView = WorkbookManager.SessionWorkbook.GetChapterCount() - 1;
                                }
                                AppState.IsDirty = true;

                                if (undoArticleList.Count > 0)
                                {
                                    WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.IMPORT_CHAPTERS, (object)undoArticleList);
                                    WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
                                }
                            }
                        }

                        if (importedChapters > 0)
                        {
                            AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(
                                Properties.Resources.FlMsgChaptersImported + " (" + importedChapters.ToString() + ")", CommentBox.HintType.INFO);
                        }

                    }
                }
                else
                {
                    AppState.MainWin.CreateChapterFromNewGames(gamesCount, ref games, fileName);
                    AppState.MainWin.BoardCommentBox.ShowFlashAnnouncement(
                        Properties.Resources.FlMsgChapterImported, CommentBox.HintType.INFO);
                }
            }
        }

        /// <summary>
        /// Imports Model Games or Exercises from a PGN file.
        /// </summary>
        /// <param name="contentType"></param>
        public static int ImportArticlesFromPgn(GameData.ContentType contentType, GameData.ContentType targetcontentType, out int gameCount, out int exerciseCount)
        {
            gameCount = 0;
            exerciseCount = 0;

            if ((contentType == GameData.ContentType.GENERIC
                || contentType == GameData.ContentType.MODEL_GAME
                || contentType == GameData.ContentType.EXERCISE)
                && WorkbookManager.SessionWorkbook.ActiveChapter != null)
            {
                StringBuilder sb = new StringBuilder();

                string[] fileNames = SelectPgnFile(true);

                if (fileNames != null && fileNames.Length > 0)
                {
                    foreach (string fileName in fileNames)
                    {
                        if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
                        {
                            sb.Append(File.ReadAllText(fileName));
                            sb.AppendLine();
                        }
                    }

                    if (sb.Length > 0)
                    {
                        bool gamesFound = ProcessArticlesText(sb.ToString(), contentType, targetcontentType, out gameCount, out exerciseCount);
                        if (!gamesFound)
                        {
                            StringBuilder sbFileNames = new StringBuilder();
                            for (int i = 0; i < fileNames.Length; i++) 
                            {
                                if (i > 0)
                                {
                                    sb.Append("; ");
                                }
                                sb.Append(fileNames[i]);
                            }
                            ShowNoGamesError(contentType, fileNames[0]);
                        }
                    }
                }
            }
            return gameCount + exerciseCount;
        }

        /// <summary>
        /// Processes text from the PGN file/files.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="contentType"></param>
        /// <param name="targetcontentType"></param>
        /// <param name="gameCount"></param>
        /// <param name="exerciseCount"></param>
        /// <returns></returns>
        private static bool ProcessArticlesText(string sb, GameData.ContentType contentType, GameData.ContentType targetcontentType, out int gameCount, out int exerciseCount)
        {
            bool gamesFound = true;

            gameCount = 0;
            exerciseCount = 0;

            int skippedDueToType = 0;
            int firstImportedGameIndex = -1;

            Chapter activeChapter = WorkbookManager.SessionWorkbook.ActiveChapter;
            ObservableCollection<GameData> games = new ObservableCollection<GameData>();
            gameCount = WorkbookManager.ReadPgnFile(sb, ref games, contentType, targetcontentType);

            // clear the default selections
            foreach (GameData gd in games)
            {
                gd.IsSelected = false;
            }

            int errorCount = 0;
            StringBuilder sbErrors = new StringBuilder();

            ArticleListItem undoItem;
            List<ArticleListItem> undoArticleList = new List<ArticleListItem>();

            if (gameCount > 0)
            {
                if (AppState.MainWin.ShowSelectGamesDialog(contentType, ref games))
                {
                    int chapterIndex = ChapterUtils.InvokeSelectSingleChapterDialog(activeChapter.Index, out bool newChapter);

                    bool proceed = true;

                    if (chapterIndex >= 0)
                    {
                        Chapter targetChapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(chapterIndex);
                        if (newChapter)
                        {
                            proceed = ChapterUtils.NameNeWChapter(targetChapter, activeChapter);
                        }
                        else
                        {
                            AppState.Workbook.ActiveChapter = targetChapter;
                        }

                        if (proceed)
                        {
                            Mouse.SetCursor(Cursors.Wait);
                            try
                            {

                                for (int i = 0; i < games.Count; i++)
                                {
                                    if (games[i].IsSelected)
                                    {
                                        Article article = null;
                                        try
                                        {
                                            int index = PgnArticleUtils.AddArticle(targetChapter, games[i], contentType, out string error, out article, targetcontentType);
                                            if (index < 0)
                                            {
                                                if (string.IsNullOrEmpty(error))
                                                {
                                                    skippedDueToType++;
                                                }
                                            }
                                            else
                                            {
                                                undoItem = new ArticleListItem(targetChapter, targetChapter.Index, targetChapter.GetArticleAtIndex(article.ContentType, index), index);
                                                if (undoItem.Article != null)
                                                {
                                                    undoArticleList.Add(undoItem);
                                                }

                                                if (firstImportedGameIndex < 0)
                                                {
                                                    firstImportedGameIndex = index;
                                                }
                                            }

                                            AppState.IsDirty = true;
                                            if (!string.IsNullOrEmpty(error))
                                            {
                                                errorCount++;
                                                sbErrors.Append(GuiUtilities.BuildGameProcessingErrorText(games[i], i + 1, error));
                                            }
                                            if (article?.ContentType == GameData.ContentType.MODEL_GAME)
                                            {
                                                gameCount++;
                                            }
                                            else if (article?.ContentType == GameData.ContentType.EXERCISE)
                                            {
                                                exerciseCount++;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            errorCount++;
                                            sbErrors.Append(GuiUtilities.BuildGameProcessingErrorText(games[i], i + 1, ex.Message));
                                        }
                                    }
                                }
                            }
                            catch { }

                            if (undoArticleList.Count > 0)
                            {
                                WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.INSERT_ARTICLES, (object)undoArticleList);
                                WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
                            }

                            if (AppState.ActiveTab == TabViewType.CHAPTERS)
                            {
                                AppState.MainWin.ChaptersView.BringActiveChapterIntoView();
                            }
                            Mouse.SetCursor(Cursors.Arrow);
                        }
                    }
                }
                else
                {
                    gameCount = 0;
                    exerciseCount = 0;
                }
            }
            else
            {
                gamesFound = false;
            }

            if (errorCount > 0 || skippedDueToType > 0)
            {
                if (skippedDueToType > 0)
                {
                    string invalidEntities = Properties.Resources.WrongTypeEntitiesNotImported + ", ";
                    invalidEntities += (Properties.Resources.Count + " " + skippedDueToType.ToString() + ".");
                    sbErrors.AppendLine(invalidEntities);
                }
                TextBoxDialog tbDlg = new TextBoxDialog(Properties.Resources.PgnErrors, sbErrors.ToString());
                GuiUtilities.PositionDialog(tbDlg, AppState.MainWin, 100);
                tbDlg.ShowDialog();
            }

            return gamesFound;
        }

        /// <summary>
        /// Shows the OpenFileDialog to let the user
        /// select a PGN file.
        /// </summary>
        /// <returns></returns>
        public static string[] SelectPgnFile(bool multiselect)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = multiselect,
                Filter = Properties.Resources.PgnFile + " (*.pgn)|*.pgn;*.pgn|" + Properties.Resources.AllFiles + " (*.*)|*.*"
            };

            string initDir;
            if (!string.IsNullOrEmpty(Configuration.LastImportDirectory))
            {
                initDir = Configuration.LastImportDirectory;
            }
            else
            {
                initDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            openFileDialog.InitialDirectory = initDir;

            bool? result;

            try
            {
                result = openFileDialog.ShowDialog();
            }
            catch
            {
                openFileDialog.InitialDirectory = "";
                result = openFileDialog.ShowDialog();
            }
            ;

            if (result == true)
            {
                Configuration.LastImportDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                return openFileDialog.FileNames;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Show the error when no games were found in the file.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="fileName"></param>
        public static void ShowNoGamesError(GameData.ContentType contentType, string fileName)
        {
            string sError;
            if (contentType == GameData.ContentType.EXERCISE)
            {
                sError = Properties.Resources.NoExerciseInFile + " ";
            }
            else
            {
                sError = Properties.Resources.NoGamesInFile + " ";
            }
            MessageBox.Show(sError + fileName, Properties.Resources.ImportPgn, MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }
}

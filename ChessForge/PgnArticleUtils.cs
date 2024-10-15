using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ChessForge
{
    public class PgnArticleUtils
    {
        /// <summary>
        /// Adds a new game/exercise to a chapter.
        /// The caller must handle errors if returned index is -1.
        /// </summary>
        /// <param name="gm"></param>
        public static int AddArticle(Chapter chapter, 
                                    GameData gm, 
                                    GameData.ContentType typ, 
                                    out string errorText, 
                                    out Article article, 
                                    GameData.ContentType targetcontentType = GameData.ContentType.GENERIC)
        {
            article = null; 
            if (!gm.Header.IsStandardChess())
            {
                errorText = Properties.Resources.ErrNotStandardChessVariant;
                return -1;
            }

            int index = -1;
            errorText = string.Empty;

            article = new Article(typ);
            try
            {
                string fen = gm.Header.GetFenString();
                if (!gm.Header.IsExercise())
                {
                    fen = null;
                }

                PgnGameParser pp = new PgnGameParser(gm.GameText, article.Tree, fen);

                article.Tree.Header = gm.Header.CloneMe(true);

                if (typ == GameData.ContentType.GENERIC)
                {
                    typ = gm.GetContentType(true);
                }
                article.Tree.ContentType = typ;

                switch (typ)
                {
                    case GameData.ContentType.STUDY_TREE:
                        chapter.StudyTree = article;
                        break;
                    case GameData.ContentType.INTRO:
                        chapter.Intro = article;
                        break;
                    case GameData.ContentType.MODEL_GAME:
                        if (targetcontentType == GameData.ContentType.GENERIC 
                            || targetcontentType == GameData.ContentType.ANY
                            || targetcontentType == GameData.ContentType.MODEL_GAME)
                        {
                            chapter.ModelGames.Add(article);
                            index = chapter.ModelGames.Count - 1;
                        }
                        else
                        {
                            index = -1;
                        }
                        break;
                    case GameData.ContentType.EXERCISE:
                        if (targetcontentType == GameData.ContentType.GENERIC
                            || targetcontentType == GameData.ContentType.ANY
                            || targetcontentType == GameData.ContentType.EXERCISE)
                        {
                            TreeUtils.RestartMoveNumbering(article.Tree);
                            chapter.Exercises.Add(article);
                            index = chapter.Exercises.Count - 1;
                        }
                        else
                        {
                            index = -1;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                errorText = ex.Message;
                AppLog.Message("AddArticle()", ex);
                index = -1;
            }

            return index;
        }

        /// <summary>
        /// Parses the passed PGN text and identifies games and exercises in it.
        /// </summary>
        /// <param name="text"></param>
        public static int PasteArticlesFromPgn(string text, out bool addedChapters, out bool cancelled)
        {
            addedChapters = false;
            cancelled = false;

            int articleCount = 0;

            ObservableCollection<GameData> games = new ObservableCollection<GameData>();
            WorkbookManager.ReadPgnFile(text, ref games, GameData.ContentType.GENERIC, GameData.ContentType.NONE);
            if (games.Count > 0)
            {
                int studyCount = 0;
                int introCount = 0;
                int gameCount = 0;
                int exerciseCount = 0;
                int addedChapterCount = 0;

                bool first = true;

                foreach (GameData game in games)
                {
                    GameData.ContentType contentType = game.Header.DetermineContentType();
                    if (contentType == GameData.ContentType.MODEL_GAME || contentType == GameData.ContentType.GENERIC)
                    {
                        // make sure to skip ChessForge workbook header
                        if (!first || game.GetWorkbookTitle() != null)
                        {
                            game.Header.SetContentType(GameData.ContentType.MODEL_GAME);
                            gameCount++;
                        }
                    }
                    else if (contentType == GameData.ContentType.EXERCISE)
                    {
                        exerciseCount++;
                    }
                    else if (contentType == GameData.ContentType.STUDY_TREE)
                    {
                        addedChapterCount++;
                        studyCount++;
                        addedChapters = true;
                    }
                    else if (contentType == GameData.ContentType.INTRO)
                    {
                        if (studyCount == 0)
                        {
                            addedChapterCount++;
                        }
                        introCount++;
                        addedChapters = true;
                    }

                    first = false;
                }

                if (gameCount > 0 || exerciseCount > 0 || addedChapterCount > 0)
                {
                    bool done = InsertArticlesIntoWorkbook(games, addedChapterCount, gameCount, exerciseCount);
                    cancelled = !done;
                }

                articleCount = addedChapterCount + gameCount + exerciseCount;
            }

            return articleCount;
        }

        /// <summary>
        /// Inserts articles in the workbook, having asked the user.
        /// </summary>
        /// <param name="games"></param>
        /// <param name="gameCount"></param>
        /// <param name="exerciseCount"></param>
        private static bool InsertArticlesIntoWorkbook(ObservableCollection<GameData> games, int addedChapterCount, int gameCount, int exerciseCount)
        {
            bool done = false;

            StringBuilder sb = new StringBuilder(Properties.Resources.MsgClipboardContainsPgn + ":\n\n");
            if (addedChapterCount > 0)
            {
                sb.Append("    " + Properties.Resources.ChapterCount + " = " + addedChapterCount.ToString() + '\n');
            }

            if (gameCount > 0)
            {
                sb.Append("    " + Properties.Resources.GameCount + " = " + gameCount.ToString() + '\n');
            }

            if (exerciseCount > 0)
            {
                sb.Append("    " + Properties.Resources.ExerciseCount + " = " + exerciseCount.ToString() + '\n');
            }

            sb.Append('\n' + Properties.Resources.ProceedAndPaste + "?");

            if (MessageBox.Show(sb.ToString(), Properties.Resources.ClipboardOperation, MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    Mouse.SetCursor(Cursors.Wait);
                    WorkbookManager.InsertArticles(games);
                }
                catch { }

                Mouse.SetCursor(Cursors.Arrow);
                done = true;
            }

            Mouse.SetCursor(Cursors.Arrow);
            return done;
        }
    }
}

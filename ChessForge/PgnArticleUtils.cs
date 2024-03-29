using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace ChessForge
{
    public class PgnArticleUtils
    {
        /// <summary>
        /// Adds a new game/exercise to a chapter.
        /// The caller must handle errors if returned index is -1.
        /// </summary>
        /// <param name="gm"></param>
        public static int AddArticle(Chapter chapter, GameData gm, GameData.ContentType typ, out string errorText, GameData.ContentType targetcontentType = GameData.ContentType.GENERIC)
        {
            if (!gm.Header.IsStandardChess())
            {
                errorText = Properties.Resources.ErrNotStandardChessVariant;
                return -1;
            }

            int index = -1;
            errorText = string.Empty;

            Article article = new Article(typ);
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
                        if (targetcontentType == GameData.ContentType.GENERIC || targetcontentType == GameData.ContentType.MODEL_GAME)
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
                        if (targetcontentType == GameData.ContentType.GENERIC || targetcontentType == GameData.ContentType.EXERCISE)
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
                    bool done = InsertArticlesIntoChapter(games, addedChapterCount, gameCount, exerciseCount);
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
        private static bool InsertArticlesIntoChapter(ObservableCollection<GameData> games, int addedChapterCount, int gameCount, int exerciseCount)
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
                Chapter currChapter = AppState.ActiveChapter;
                int firstAddedIndex = -1;
                GameData.ContentType firstAddedType = GameData.ContentType.NONE;

                bool hasStudyBeforeIntro = false;

                foreach (GameData game in games)
                {
                    int index = -1;

                    GameData.ContentType currContentType = game.GetContentType(false);

                    if (currContentType == GameData.ContentType.STUDY_TREE)
                    {
                        currChapter = AppState.Workbook.CreateNewChapter();
                        currChapter.SetTitle(game.Header.GetChapterTitle());
                        AddArticle(currChapter, game, GameData.ContentType.STUDY_TREE, out _);
                        if (firstAddedType == GameData.ContentType.NONE)
                        {
                            firstAddedType = GameData.ContentType.STUDY_TREE;
                        }
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

                        AddArticle(currChapter, game, GameData.ContentType.INTRO, out _);
                        hasStudyBeforeIntro = false;
                    }
                    else
                    {
                        index = AddArticle(currChapter, game, game.GetContentType(true), out _);
                        if (firstAddedType == GameData.ContentType.NONE)
                        {
                            firstAddedType = game.GetContentType(false);
                        }
                        if (firstAddedIndex < 0)
                        {
                            firstAddedIndex = index;
                        }
                    }
                }
                AppState.IsDirty = true;
                AppState.MainWin.SelectArticle(currChapter.Index, firstAddedType, firstAddedIndex);

                done = true;
            }

            return done;
        }
    }
}

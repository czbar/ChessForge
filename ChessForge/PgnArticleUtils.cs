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
        public static void PasteArticlesFromPgn(string text)
        {
            ObservableCollection<GameData> games = new ObservableCollection<GameData>();
            WorkbookManager.ReadPgnFile(text, ref games, GameData.ContentType.GENERIC, GameData.ContentType.NONE);
            if (games.Count > 0)
            {
                int gameCount = 0;
                int exerciseCount = 0;

                foreach (GameData game in games)
                {
                    GameData.ContentType contentType = game.Header.DetermineContentType();
                    if (contentType == GameData.ContentType.MODEL_GAME || contentType == GameData.ContentType.GENERIC)
                    {
                        gameCount++;
                    }
                    else if (contentType == GameData.ContentType.EXERCISE)
                    {
                        exerciseCount++;
                    }
                }

                if (gameCount > 0 || exerciseCount > 0)
                {
                    InsertArticlesIntoChapter(games, gameCount, exerciseCount);
                }
            }
        }

        /// <summary>
        /// Inserts articles in the workbook, having asked the user.
        /// </summary>
        /// <param name="games"></param>
        /// <param name="gameCount"></param>
        /// <param name="exerciseCount"></param>
        private static void InsertArticlesIntoChapter(ObservableCollection<GameData> games, int gameCount, int exerciseCount)
        {
            StringBuilder sb = new StringBuilder(Properties.Resources.MsgClipboardContainsPgn + " (");
            if (gameCount > 0)
            {
                sb.Append(Properties.Resources.GameCount + ": " + gameCount.ToString());
                if (exerciseCount > 0)
                {
                    sb.Append(", ");
                }
            }

            if (exerciseCount > 0)
            {
                sb.Append(Properties.Resources.ExerciseCount + ": " + exerciseCount.ToString());
            }

            sb.Append("). " + Properties.Resources.Paste + "?");

            if (MessageBox.Show(sb.ToString(), Properties.Resources.ClipboardOperation, MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                int firstAddedIndex = -1;
                GameData.ContentType firstAddedType = GameData.ContentType.NONE;
                foreach (GameData game in games)
                {
                    int index = PgnArticleUtils.AddArticle(AppState.ActiveChapter, game, game.GetContentType(true), out _);
                    if (firstAddedType == GameData.ContentType.NONE)
                    {
                        firstAddedType = game.GetContentType(false);
                    }
                    if (firstAddedIndex < 0)
                    {
                        firstAddedIndex = index;
                    }
                }
                AppState.IsDirty = true;
                AppState.MainWin.SelectArticle(AppState.ActiveChapter.Index, firstAddedType, firstAddedIndex);
            }
        }
    }
}

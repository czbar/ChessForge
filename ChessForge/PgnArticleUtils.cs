using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;

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
    }
}

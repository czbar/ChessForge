using ChessPosition;
using GameTree;
using System.Collections.Generic;

namespace ChessForge
{
    public class CleanSidelinesComments
    {
        /// <summary>
        /// Invokes a dialog allowing the user to select the scope 
        /// and type of notes to delete.
        /// The "notes" can be comments (before and after moves), engine evaluations
        /// and sidelines.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void CleanLinesAndComments(OperationScope scope, int moveAttrsFlags, int articleAttrsFlags,
                                                 bool applyToStudies, bool applyToGames, bool applyToExercises)
        {
            Dictionary<Article, List<MoveAttributes>> dictUndoMoveAttrs = new Dictionary<Article, List<MoveAttributes>>();
            List<ArticleAttributes> lstUndoArticlesAttrs = new List<ArticleAttributes>();

            if (scope == OperationScope.ACTIVE_ITEM)
            {
                if (AppState.MainWin.ActiveTreeView != null && AppState.IsTreeViewTabActive() && AppState.Workbook.ActiveArticle != null)
                {
                    List<MoveAttributes> lstMoveAttrs = DeleteMoveAttributesInArticle(AppState.Workbook.ActiveArticle, moveAttrsFlags);
                    if (lstMoveAttrs.Count > 0)
                    {
                        dictUndoMoveAttrs[AppState.Workbook.ActiveArticle] = lstMoveAttrs;
                    }

                    ArticleAttributes articleAttrs = DeleteArticleAttributes(AppState.Workbook.ActiveArticle, articleAttrsFlags);
                    if (articleAttrs != null)
                    {
                        lstUndoArticlesAttrs.Add(articleAttrs);
                    }
                }
            }
            else if (scope == OperationScope.CHAPTER)
            {
                DeleteMoveAttributesInChapter(moveAttrsFlags, AppState.ActiveChapter, applyToStudies, applyToGames, applyToExercises, dictUndoMoveAttrs);
                DeleteArticleAttributesInChapter(articleAttrsFlags, AppState.ActiveChapter, applyToStudies, applyToGames, applyToExercises, lstUndoArticlesAttrs);
            }
            else if (scope == OperationScope.WORKBOOK)
            {
                foreach (Chapter chapter in AppState.Workbook.Chapters)
                {
                    DeleteMoveAttributesInChapter(moveAttrsFlags, chapter, applyToStudies, applyToGames, applyToExercises, dictUndoMoveAttrs);
                    DeleteArticleAttributesInChapter(articleAttrsFlags, chapter, applyToStudies, applyToGames, applyToExercises, lstUndoArticlesAttrs);
                }
            }

            if (AppState.MainWin.ActiveTreeView != null && AppState.IsTreeViewTabActive())
            {
                AppState.MainWin.ActiveTreeView.BuildFlowDocumentForVariationTree(false);
                if ((moveAttrsFlags & ((int)MoveAttribute.ENGINE_EVALUATION) | (int)MoveAttribute.BAD_MOVE_ASSESSMENT) != 0)
                {
                    // there may have been "assessments" so need to refresh this
                    AppState.MainWin.ActiveLine.RefreshNodeList(true);
                }
            }

            if (dictUndoMoveAttrs.Keys.Count > 0 || lstUndoArticlesAttrs.Count > 0)
            {
                WorkbookOperationType wot = WorkbookOperationType.CLEAN_LINES_AND_COMMENTS;

                WorkbookOperation op = new WorkbookOperation(wot, dictUndoMoveAttrs, lstUndoArticlesAttrs);
                AppState.Workbook.OpsManager.PushOperation(op);

                AppState.IsDirty = true;
            }

            AppState.MainWin.ActiveTreeView.RestoreSelectedLineAndNode();
        }

        /// <summary>
        /// Deletes article attributes of the specified type from all articles in a chapter.
        /// </summary>
        /// <param name="attrsFlags"></param>
        /// <param name="chapter"></param>
        /// <param name="study"></param>
        /// <param name="games"></param>
        /// <param name="exercises"></param>
        /// <param name="lst"></param>
        private static void DeleteArticleAttributesInChapter(int attrsFlags,
            Chapter chapter,
            bool study,
            bool games,
            bool exercises,
            List<ArticleAttributes> lst)
        {
            if (chapter != null)
            {
                if (study)
                {
                    ArticleAttributes attrs = DeleteArticleAttributes(chapter.StudyTree, attrsFlags);
                    if (attrs != null)
                    {
                        lst.Add(attrs);
                    }
                }
                if (games)
                {
                    foreach (Article game in chapter.ModelGames)
                    {
                        ArticleAttributes attrs = DeleteArticleAttributes(game, attrsFlags);
                        if (attrs != null)
                        {
                            lst.Add(attrs);
                        }
                    }
                }
                if (exercises)
                {
                    foreach (Article exercise in chapter.Exercises)
                    {
                        ArticleAttributes attrs = DeleteArticleAttributes(exercise, attrsFlags);
                        if (attrs != null)
                        {
                            lst.Add(attrs);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Deletes move attributes of the specified type from all articles in a chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="study"></param>
        /// <param name="games"></param>
        /// <param name="exercises"></param>
        private static void DeleteMoveAttributesInChapter(int attrsFlags,
            Chapter chapter,
            bool study,
            bool games,
            bool exercises,
            Dictionary<Article, List<MoveAttributes>> dict)
        {
            if (chapter != null)
            {
                if (study)
                {
                    var list = DeleteMoveAttributesInArticle(chapter.StudyTree, attrsFlags);
                    if (list.Count > 0)
                    {
                        dict[chapter.StudyTree] = list;
                    }
                }
                if (games)
                {
                    foreach (Article game in chapter.ModelGames)
                    {
                        var list = DeleteMoveAttributesInArticle(game, attrsFlags);
                        if (list.Count > 0)
                        {
                            dict[game] = list;
                        }
                    }
                }
                if (exercises)
                {
                    foreach (Article exercise in chapter.Exercises)
                    {
                        var list = DeleteMoveAttributesInArticle(exercise, attrsFlags);
                        if (list.Count > 0)
                        {
                            dict[exercise] = list;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Deletes move attributes of the specified type from the Article.
        /// Returns the list of removed comments for the Undo operation.
        /// </summary>
        /// <param name="article"></param>
        /// <returns></returns>
        private static List<MoveAttributes> DeleteMoveAttributesInArticle(Article article, int attrsFlags)
        {
            List<MoveAttributes> attrsList = new List<MoveAttributes>();

            attrsList = TreeUtils.BuildMoveAttributesList(article.Tree, (int)attrsFlags);
            if ((attrsFlags & (int)MoveAttribute.COMMENT_AND_NAGS) != 0)
            {
                article.Tree.DeleteCommentsAndNags();
            }

            if ((attrsFlags & (int)MoveAttribute.ENGINE_EVALUATION) != 0)
            {
                article.Tree.DeleteEngineEvaluations();
            }

            if ((attrsFlags & (int)MoveAttribute.BAD_MOVE_ASSESSMENT) != 0)
            {
                article.Tree.DeleteMoveAssessments();
            }

            if ((attrsFlags & (int)MoveAttribute.SIDELINE) != 0)
            {
                List<TreeNode> toDelete = new List<TreeNode>();
                foreach (MoveAttributes attrs in attrsList)
                {
                    if (attrs.IsDeleted)
                    {
                        toDelete.Add(attrs.Node);
                    }
                }
                article.Tree.DeleteNodes(toDelete);
                BookmarkManager.ResyncBookmarks(1);
            }

            return attrsList;
        }

        /// <summary>
        /// Deletes article attributes of the specified type from the Article.
        /// Returns the list of removed comments for the Undo operation.
        /// </summary>
        /// <param name="article"></param>
        /// <returns></returns>
        private static ArticleAttributes DeleteArticleAttributes(Article article, int attrsFlags)
        {
            ArticleAttributes attrs = BuildArticleAttributes(article, attrsFlags);

            if ((attrsFlags & (int)ArticleAttribute.ANNOTATOR) != 0)
            {
                article.Tree.Header.SetHeaderValue(PgnHeaders.KEY_ANNOTATOR, "");
            }

            return attrs;
        }

        /// <summary>
        /// Builds a list of article attributes based on the specified attribute types.
        /// </summary>
        /// <param name="article">The article for which attributes are to be built.</param>
        /// <param name="attrsFlags">The types of attributes to include in the list.</param>
        /// <returns>A list of article attributes.</returns>
        private static ArticleAttributes BuildArticleAttributes(Article article, int attrsFlags)
        {
            ArticleAttributes articleAttrs = null;

            if ((attrsFlags & (int)ArticleAttribute.ANNOTATOR) != 0)
            {
                articleAttrs = new ArticleAttributes(article);
            }

            return articleAttrs;
        }

    }
}

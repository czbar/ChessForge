using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Monitors users operations for the purpose
    /// of undoing them on request.
    /// Objects of this type will be placed in:
    /// - VariationTrees: for the purpose of undoing move editing
    /// - Chapters: for undoing Game/Exercise deletions
    /// - Workbook: for undoing Chapter deletions
    /// </summary>
    public class WorkbookOperationsManager : OperationsManager
    {
        // parent tree if hosted in a VariationTree
        private VariationTree _owningTree;

        // parent chapter if hosted in a chapter
        private Chapter _owningChapter;

        // parent workbook if hosted in a workbook
        private Workbook _owningWorkbook;

        /// <summary>
        /// Contructor for OperationsManager created in a VariationTree
        /// </summary>
        /// <param name="tree"></param>
        public WorkbookOperationsManager(VariationTree tree)
        {
            _owningTree = tree;
        }

        /// <summary>
        /// Contructor for OperationsManager created in a Chapter
        /// </summary>
        /// <param name="tree"></param>
        public WorkbookOperationsManager(Chapter chapter)
        {
            _owningChapter = chapter;
        }

        /// <summary>
        /// Contructor for OperationsManager created in a Workbook
        /// </summary>
        /// <param name="tree"></param>
        public WorkbookOperationsManager(Workbook workbook)
        {
            _owningWorkbook = workbook;
        }

        /// <summary>
        /// Returns the operation at the top of the stack.
        /// </summary>
        /// <returns></returns>
        public WorkbookOperation Peek()
        {
            if (_operations.Count == 0)
            {
                return null;
            }
            else
            {
                return _operations.Peek() as WorkbookOperation;
            }
        }

        /// <summary>
        /// Performs the undo of the Operation in the queue.
        /// </summary>
        public bool Undo(out WorkbookOperationType tp, out int selectedChapterIndex, out int selectedArticleIndex)
        {
            bool done = false;

            tp = WorkbookOperationType.NONE;
            selectedChapterIndex = -1;
            selectedArticleIndex = -1;
            if (_operations.Count == 0)
            {
                return false;
            }

            try
            {
                WorkbookOperation op = _operations.Pop() as WorkbookOperation;
                done = true;
                tp = op.OpType;
                switch (tp)
                {
                    case WorkbookOperationType.RENAME_CHAPTER:
                        WorkbookManager.SessionWorkbook.UndoRenameChapter(op.Chapter, op.OpData_1);
                        break;
                    case WorkbookOperationType.DELETE_CHAPTER:
                        selectedChapterIndex = op.ChapterIndex;
                        WorkbookManager.SessionWorkbook.UndoDeleteChapter(op.Chapter, op.ChapterIndex);
                        break;
                    case WorkbookOperationType.DELETE_CHAPTERS:
                        WorkbookManager.SessionWorkbook.UndoDeleteChapters(op.OpData_1, op.OpData_2);
                        if (op.OpData_1 is List<Chapter> chapters)
                        {
                            if (chapters.Count > 0)
                            {
                                WorkbookManager.SessionWorkbook.ActiveChapter = chapters[0];
                            }
                        }
                        break;
                    case WorkbookOperationType.MERGE_CHAPTERS:
                        WorkbookManager.SessionWorkbook.UndoMergeChapters(op.Chapter, op.OpData_1, op.OpData_2);
                        if (op.OpData_1 is List<Chapter> sourceChapters)
                        {
                            if (sourceChapters.Count > 0)
                            {
                                WorkbookManager.SessionWorkbook.ActiveChapter = sourceChapters[0];
                            }
                        }
                        break;
                    case WorkbookOperationType.SPLIT_CHAPTER:
                        WorkbookManager.SessionWorkbook.UndoSplitChapter(op.Chapter, op.OpData_1);
                        if (op.Chapter != null)
                        {
                            WorkbookManager.SessionWorkbook.ActiveChapter = op.Chapter;
                        }
                        break;
                    case WorkbookOperationType.CREATE_CHAPTER:
                        if (WorkbookManager.SessionWorkbook.GetChapterCount() > 1)
                        {
                            selectedChapterIndex = WorkbookManager.SessionWorkbook.GetChapterIndex(op.Chapter);
                            WorkbookManager.SessionWorkbook.Chapters.Remove(op.Chapter);
                            if (selectedChapterIndex >= WorkbookManager.SessionWorkbook.GetChapterCount())
                            {
                                selectedChapterIndex--;
                            }
                            WorkbookManager.SessionWorkbook.ActiveChapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(selectedChapterIndex);
                        }
                        break;
                    case WorkbookOperationType.CREATE_ARTICLE:
                        WorkbookManager.SessionWorkbook.ActiveChapter = op.Chapter;
                        selectedChapterIndex = WorkbookManager.SessionWorkbook.GetChapterIndex(op.Chapter);
                        WorkbookManager.SessionWorkbook.UndoCreateArticle(op.Chapter, op.OpData_1 as Article);
                        selectedArticleIndex = op.Chapter.AdjustActiveArticleIndex((op.OpData_1 as Article).ContentType);
                        break;
                    case WorkbookOperationType.DELETE_MODEL_GAMES:
                    case WorkbookOperationType.DELETE_EXERCISES:
                    case WorkbookOperationType.DELETE_ARTICLES:
                        WorkbookManager.SessionWorkbook.UndoDeleteArticles(op.OpData_1, op.OpData_2, op.OpData_3);
                        SetActiveArticlePostArticlesUndelete(op.OpData_1);
                        AppState.MainWin.RebuildAllTreeViews();
                        break;
                    case WorkbookOperationType.REGENERATE_STUDIES:
                        WorkbookManager.SessionWorkbook.UndoRegenerateStudies(op.OpData_1, op.OpData_2);
                        AppState.MainWin.StudyTreeView.BuildFlowDocumentForVariationTree(false);
                        AppState.MainWin.UiTabStudyTree.Focus();
                        break;
                    case WorkbookOperationType.COPY_ARTICLES:
                        ChapterUtils.UndoCopyArticles(op.Chapter, op.OpData_1);
                        break;
                    case WorkbookOperationType.INSERT_ARTICLES:
                        ChapterUtils.UndoInsertArticles(op.Chapter, op.OpData_1);
                        break;
                    case WorkbookOperationType.IMPORT_CHAPTERS:
                        ChapterUtils.UndoImportChapters(op.OpData_1);
                        break;
                    case WorkbookOperationType.MOVE_ARTICLES:
                        ChapterUtils.UndoMoveArticles(op.Chapter, op.OpData_1);
                        break;
                    case WorkbookOperationType.MOVE_ARTICLES_MULTI_CHAPTER:
                        ChapterUtils.UndoMoveMultiChapterArticles(op.Chapter, op.OpData_1);
                        break;
                    case WorkbookOperationType.DELETE_COMMENTS:
                        AppState.Workbook.UndoDeleteComments(op.OpData_1);
                        break;
                    case WorkbookOperationType.DELETE_ENGINE_EVALS:
                        AppState.Workbook.UndoDeleteEngineEvals(op.OpData_1);
                        break;
                    case WorkbookOperationType.ASSIGN_ECO:
                        Tools.UndoAssignEco(op.OpData_1);
                        break;
                }
            }
            catch
            {
            }

            AppState.SetupGuiForCurrentStates();

            return done;
        }

        /// <summary>
        /// After undelete, set the active article index for chapters into which
        /// we were inserting.
        /// In particular if there was just one item, it is the one we want to set.
        /// </summary>
        /// <param name="objArticleList"></param>
        private void SetActiveArticlePostArticlesUndelete(object objArticleList)
        {
            try
            {
                if (objArticleList is List<ArticleListItem> lstItems && lstItems.Count > 0)
                {
                    int articleIndex = -1;
                    int chapterIndex = -1;

                    Chapter activeChapter = AppState.ActiveChapter;
                    foreach (var item in lstItems)
                    {
                        AppState.Workbook.GetArticleByGuid(item.Article.Guid, out chapterIndex, out articleIndex);
                        if (chapterIndex >= 0 && articleIndex >= 0)
                        {
                            if (item.Article.ContentType == GameData.ContentType.MODEL_GAME)
                            {
                                AppState.Workbook.Chapters[chapterIndex].ActiveModelGameIndex = articleIndex;
                                if (AppState.Workbook.Chapters[chapterIndex] == activeChapter)
                                {
                                    AppState.Workbook.ActiveChapter.SetActiveVariationTree(GameData.ContentType.MODEL_GAME, articleIndex);
                                }
                            }
                            else if (item.Article.ContentType == GameData.ContentType.EXERCISE)
                            {
                                AppState.Workbook.Chapters[chapterIndex].ActiveExerciseIndex = articleIndex;
                                if (AppState.Workbook.Chapters[chapterIndex] == activeChapter)
                                {
                                    AppState.Workbook.ActiveChapter.SetActiveVariationTree(GameData.ContentType.EXERCISE, articleIndex);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

    }
}

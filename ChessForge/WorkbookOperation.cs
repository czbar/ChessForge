using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static GameTree.EditOperation;

namespace ChessForge
{
    public class WorkbookOperation : Operation
    {
        /// <summary>
        /// Types of supported operations.
        /// </summary>
        public enum WorkbookOperationType
        {
            NONE,
            DELETE_CHAPTER,
            RENAME_CHAPTER,
            DELETE_MODEL_GAME,
            DELETE_EXERCISE,
            EDIT_MODEL_GAME_HEADER,
            EDIT_EXERCISE_HEADER
        }

        /// <summary>
        /// Operation type.
        /// </summary>
        public WorkbookOperationType OpType { get { return _opType; } }

        /// <summary>
        /// Chapter object reference.
        /// </summary>
        public Chapter Chapter { get { return _chapter; } }

        /// <summary>
        /// Index of the chapter in the Workbook's chapter list.
        /// </summary>
        public int ChapterIndex { get { return _chapterIndex; } }

        /// <summary>
        /// Article to operate on.
        /// </summary>
        public Article Article { get { return _article; } }

        /// <summary>
        /// Index of the Model Game or Exercise in the Chapter's list.
        /// </summary>
        public int ArticleIndex { get { return _articleIndex; } }

        /// <summary>
        /// Type of this operation.
        /// </summary>
        private WorkbookOperationType _opType;

        /// <summary>
        /// Index of the chapter on which the operation occured.
        /// </summary>
        private int _chapterIndex;

        /// <summary>
        /// Index of the Article (game or exercise) on which the operation
        /// was performed.
        /// </summary>
        private int _articleIndex;

        /// <summary>
        /// Saved Chapter do that it can be restored after deletion.
        /// </summary>
        private Chapter _chapter;

        /// <summary>
        /// Saved Article so that a Game or Exercise can be restored.
        /// </summary>
        private Article _article;

        /// <summary>
        /// Constructor for RENAME_CHAPTER. The object data holds
        /// the previous title.
        /// </summary>
        public WorkbookOperation(WorkbookOperationType tp, Chapter ch, object data) : base()
        {
            _opType = tp;
            _chapter = ch;
            _opData_1= data;
        }

        /// <summary>
        /// Constructor for DELETE_CHAPTER.
        /// </summary>
        public WorkbookOperation(WorkbookOperationType tp, Chapter ch, int chapterIndex) : base()
        {
            _opType = tp;
            _chapter = ch;
            _chapterIndex = chapterIndex;
        }

        /// <summary>
        /// Constructor for operations on Model Games and Exercises.
        /// </summary>
        public WorkbookOperation(WorkbookOperationType tp, Chapter ch, Article article, int gameIndex) : base()
        {
            _opType = tp;
            _chapter = ch;
            _article = article;
            _articleIndex = gameIndex;
        }
    }
}

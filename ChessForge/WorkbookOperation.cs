using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Types of supported operations.
    /// </summary>
    public enum WorkbookOperationType
    {
        NONE,
        DELETE_CHAPTER,
        DELETE_CHAPTERS,
        MERGE_CHAPTERS,
        SPLIT_CHAPTER,
        CREATE_CHAPTER,
        IMPORT_CHAPTERS,
        RENAME_CHAPTER,
        CREATE_ARTICLE,
        DELETE_MODEL_GAMES,
        DELETE_EXERCISES,
        DELETE_ARTICLES,
        EDIT_MODEL_GAME_HEADER,
        EDIT_EXERCISE_HEADER,
        MOVE_ARTICLES_MULTI_CHAPTER,
        MOVE_ARTICLES,
        COPY_ARTICLES,
        INSERT_ARTICLES,
        DELETE_COMMENTS,
        DELETE_ENGINE_EVALS,
        ASSIGN_ECO,
    }

    /// <summary>
    /// A workbook operation than can be registered and then undone if requeired.
    /// </summary>
    public class WorkbookOperation : Operation
    {
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
            _opData_1 = data;
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

        /// <summary>
        /// Constructor for multi-article deletions.
        /// </summary>
        /// <param name="tp">Operation type</param>
        /// <param name="ch"></param>
        /// <param name="articleIndex"></param>
        /// <param name="data1">List<Articles> list of Articles to undelete</param>
        /// <param name="data2">List<int> original indices of the deleted articles </param>
        public WorkbookOperation(WorkbookOperationType tp, Chapter ch, int articleIndex, object data1, object data2, object data3) : base()
        {
            _opType = tp;
            _chapter = ch;
            _articleIndex = articleIndex;
            _opData_1 = data1;
            _opData_2 = data2;
            _opData_3 = data3;
        }

        /// <summary>
        /// Constructor for actions:
        ///    DELETE_COMMENTS
        ///    DELETE_ENGINE_EVALS
        /// The data parameter is a dictionary with Articles as keys
        /// and deleted data as values.
        /// </summary>
        /// <param name="tp"></param>
        /// <param name="data"></param>
        public WorkbookOperation(WorkbookOperationType tp, object data) : base()
        {
            _opType = tp;
            _opData_1 = data;
        }
    }
}

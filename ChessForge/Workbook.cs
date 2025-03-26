using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ChessForge
{
    /// <summary>
    /// A Workbook is the highest level ChessForge data entity
    /// and there can only be one open at any time. 
    /// 
    /// A Workbook consists of one or more chapters;
    /// Each chapter can hold one or more Variation Tree.
    /// </summary>
    public class Workbook
    {
        /// <summary>
        /// The list of chapters.
        /// </summary>
        private List<Chapter> _chapters = new List<Chapter>();

        /// <summary>
        /// Returns the list of chapters.
        /// </summary>
        public List<Chapter> Chapters
        {
            get
            {
                return _chapters;
            }
        }

        /// <summary>
        /// The training side as configured.
        /// </summary>
        public PieceColor TrainingSideConfig = PieceColor.None;

        /// <summary>
        /// The current training side.
        /// </summary>
        public PieceColor TrainingSideCurrent = PieceColor.None;

        private PieceColor _studyBoardOrientationConfig = PieceColor.None;
        private PieceColor _gameBoardOrientationConfig = PieceColor.None;
        private PieceColor _exerciseBoardOrientationConfig = PieceColor.None;

        /// <summary>
        /// Determines the initial board orientation in the Study view.
        /// </summary>
        public PieceColor StudyBoardOrientationConfig
        {
            get
            {
                return _studyBoardOrientationConfig != PieceColor.None ? _studyBoardOrientationConfig : TrainingSideConfig;
            }
            set
            {
                _studyBoardOrientationConfig = value;
            }
        }

        /// <summary>
        /// Determines the initial board orientation in the Games view.
        /// </summary>
        public PieceColor GameBoardOrientationConfig
        {
            get
            {
                return _gameBoardOrientationConfig != PieceColor.None ? _gameBoardOrientationConfig : TrainingSideConfig;
            }
            set
            {
                _gameBoardOrientationConfig = value;
            }
        }


        /// <summary>
        /// Determines the initial board orientation in the Exercises view.
        /// Piece.None is valid as it indicates "side-to-move"
        /// </summary>
        public PieceColor ExerciseBoardOrientationConfig
        {
            get
            {
                return _exerciseBoardOrientationConfig;
            }
            set
            {
                _exerciseBoardOrientationConfig = value;
            }
        }

        // chapter currently open in the session
        private Chapter _activeChapter;

        // last update date
        private DateTime? _lastUpdate;

        // workbook description string
        private string _description;

        // workbook title
        private string _title = "";

        // annotator's name
        private string _author;

        // Version object for this Workbook
        private WorkbookVersion _version;

        // Guid of the Workbook
        private string _guid;

        /// <summary>
        /// An object managing identities of the trees.
        /// </summary>
        private TreeManager _treeManager = new TreeManager();

        /// <summary>
        /// An object managing reading games in the background
        /// </summary>
        public BackgroundPgnProcessingManager GamesManager;

        /// <summary>
        // Associated OperationsManager
        /// </summary>
        public WorkbookOperationsManager OpsManager;

        /// <summary>
        /// Whether the content of the Workbook has been read in.
        /// </summary>
        public bool IsReady = false;

        /// <summary>
        /// The constructor.
        /// Resets the TreeManager. 
        /// Creates Operations Manager,
        /// </summary>
        public Workbook()
        {
            TreeManager.Reset();
            OpsManager = new WorkbookOperationsManager(this);
            GamesManager = new BackgroundPgnProcessingManager(this);
            _version = new WorkbookVersion();
        }

        /// <summary>
        /// Returns true of background processing is in progress.
        /// </summary>
        public bool IsBackgroundLoadingInProgress
        {
            get => GamesManager.State == ProcessState.RUNNING;
        }

        /// <summary>
        /// Moves chapter from one index position to another.
        /// </summary>
        /// <param name="sourceIndex"></param>
        /// <param name="targetIndex"></param>
        public bool MoveChapter(int sourceIndex, int targetIndex)
        {
            bool ret = false;

            if (sourceIndex != targetIndex
                && sourceIndex >= 0 && targetIndex >= 0
                && sourceIndex < Chapters.Count && targetIndex < Chapters.Count)
            {
                try
                {
                    Chapter hold = WorkbookManager.SessionWorkbook.Chapters[sourceIndex];
                    AppState.Workbook.Chapters.Remove(hold);
                    if (sourceIndex < targetIndex)
                    {
                        targetIndex--;
                    }
                    AppState.Workbook.Chapters.Insert(targetIndex, hold);
                    AppState.IsDirty = true;
                }
                catch { }
                ret = true;
            }

            return ret;
        }

        /// <summary>
        /// Moves a game to a different index, optionally a different chapter.
        /// </summary>
        /// <param name="sourceChapterIndex"></param>
        /// <param name="articleIndex"></param>
        /// <param name="targetChapterIndex"></param>
        /// <param name="insertBeforeArticle"></param>
        public void MoveArticle(GameData.ContentType content, int sourceChapterIndex, int articleIndex, int targetChapterIndex, int insertBeforeArticle)
        {
            try
            {
                Chapter sourceChapter = Chapters[sourceChapterIndex];
                Article article = null;

                switch (content)
                {
                    case GameData.ContentType.MODEL_GAME:
                        article = sourceChapter.ModelGames[articleIndex];
                        break;
                    case GameData.ContentType.EXERCISE:
                        article = sourceChapter.Exercises[articleIndex];
                        break;
                }

                Chapter targetChapter = Chapters[targetChapterIndex];

                if (sourceChapter != targetChapter || articleIndex != insertBeforeArticle)
                {
                    sourceChapter.DeleteArticle(article);

                    if (sourceChapter == targetChapter && articleIndex < insertBeforeArticle)
                    {
                        insertBeforeArticle--;
                    }
                    targetChapter.InsertArticle(article, insertBeforeArticle);
                }
            }
            catch { }
        }

        /// <summary>
        /// Called when creating a Workbook to populate article lists across the workbook.
        /// The created list must have the exact same number of elements as the passed collection
        /// and they must match their source by index.
        /// </summary>
        /// <param name="articleDataList"></param>
        public List<Article> CreateArticlePlaceholders(ObservableCollection<GameData> articleDataList, Chapter chapter = null)
        {
            List<Article> lstArticles = new List<Article>(articleDataList.Count);

            // put a null value in the first element
            lstArticles.Add(null);

            for (int i = 1; i < articleDataList.Count; i++)
            {
                GameData gm = articleDataList[i];
                GameData.ContentType contentType = gm.GetContentType(true);
                if (contentType == GameData.ContentType.STUDY_TREE)
                {
                    chapter = CreateNewChapter();
                    chapter.SetTitle(gm.Header.GetChapterTitle());
                    chapter.Guid = gm.Header.GetOrGenerateGuid(out bool generated);
                    if (generated)
                    {
                        AppState.IsDirty = true;
                    }
                }

                if (chapter != null)
                {
                    Article article = new Article(contentType, i);
                    article.Tree.Header = gm.Header.CloneMe(true);
                    switch (contentType)
                    {
                        case GameData.ContentType.STUDY_TREE:
                            chapter.StudyTree = article;
                            break;
                        case GameData.ContentType.INTRO:
                            chapter.Intro = article;
                            chapter.AlwaysShowIntroTab = true;
                            break;
                        case GameData.ContentType.MODEL_GAME:
                            chapter.ModelGames.Add(article);
                            break;
                        case GameData.ContentType.EXERCISE:
                            chapter.Exercises.Add(article);
                            article.ShowSolutionByDefault = chapter.ShowSolutionsOnOpen;
                            article.Tree.ShowTreeLines = chapter.ShowSolutionsOnOpen;
                            break;
                    }

                    // force creation of GUID if absent
                    gm.Header.GetGuid(out _);
                    lstArticles.Add(article);
                }
                else
                {
                    lstArticles.Add(null);
                }
            }

            return lstArticles;
        }

        /// <summary>
        /// Builds an Article list (Games and/or Exercises) for use in the Select Articles dialog.
        /// </summary>
        /// <param name="contentType">Type of articles to return. If GENERIC return both Games and Exercises</param>
        /// <returns></returns>
        public ObservableCollection<ArticleListItem> GenerateArticleList(Chapter selectedChapter = null, GameData.ContentType contentType = GameData.ContentType.GENERIC)
        {
            ObservableCollection<ArticleListItem> articleList = new ObservableCollection<ArticleListItem>();

            foreach (Chapter chapter in Chapters)
            {
                int chapterIndex = chapter.Index;

                if (selectedChapter == null)
                {
                    // an item for the Chapter itself
                    ArticleListItem chaptItem = new ArticleListItem(chapter);
                    articleList.Add(chaptItem);
                }
                else
                {
                    if (chapter != selectedChapter)
                    {
                        continue;
                    }
                }

                if (contentType == GameData.ContentType.MODEL_GAME || contentType == GameData.ContentType.GENERIC)
                {
                    for (int i = 0; i < chapter.ModelGames.Count; i++)
                    {
                        ArticleListItem artItem = new ArticleListItem(chapter, chapterIndex, chapter.ModelGames[i], i);
                        articleList.Add(artItem);
                    }
                }

                if (contentType == GameData.ContentType.EXERCISE || contentType == GameData.ContentType.GENERIC)
                {
                    for (int i = 0; i < chapter.Exercises.Count; i++)
                    {
                        ArticleListItem artItem = new ArticleListItem(chapter, chapterIndex, chapter.Exercises[i], i);
                        articleList.Add(artItem);
                    }
                }
            }

            return articleList;
        }

        /// <summary>
        /// Finds and returns a Game or an Exercise with the requested Guid.
        /// Returns null if not found.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="chapterIndex"></param>
        /// <param name="articleIndex"></param>
        /// <returns></returns>
        /// 
        public Article GetArticleByGuid(string guid, out int chapterIndex, out int articleIndex, bool includeStudy = false)
        {
            chapterIndex = -1;
            articleIndex = -1;

            for (int i = 0; i < _chapters.Count; i++)
            {
                for (int j = 0; j < Chapters[i].ModelGames.Count; j++)
                {
                    if (Chapters[i].ModelGames[j].Tree.Header.GetGuid(out _) == guid)
                    {
                        chapterIndex = i;
                        articleIndex = j;
                        return Chapters[i].ModelGames[j];
                    }
                }

                for (int j = 0; j < Chapters[i].Exercises.Count; j++)
                {
                    if (Chapters[i].Exercises[j].Tree.Header.GetGuid(out _) == guid)
                    {
                        chapterIndex = i;
                        articleIndex = j;
                        return Chapters[i].Exercises[j];
                    }
                }

                if (includeStudy)
                {
                    if (Chapters[i].StudyTree.Tree.Header.GetGuid(out _) == guid)
                    {
                        chapterIndex = Chapters[i].Index;
                        return Chapters[i].StudyTree;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Looks for a tree with the requested id in the workbook.
        /// (Ignores Intro trees.)
        /// </summary>
        /// <param name="treeId"></param>
        /// <returns></returns>
        public VariationTree GetTreeByTreeId(int treeId)
        {
            VariationTree tree = null;

            foreach (Chapter chapter in _chapters)
            {
                if (chapter.StudyTree.Tree.TreeId == treeId)
                {
                    tree = chapter.StudyTree.Tree;
                    break;
                }
                foreach (Article game in chapter.ModelGames)
                {
                    if (game.Tree.TreeId == treeId)
                    {
                        tree = game.Tree;
                        break;
                    }
                }
                foreach (Article exercise in chapter.Exercises)
                {
                    if (exercise.Tree.TreeId == treeId)
                    {
                        tree = exercise.Tree;
                        break;
                    }
                }
            }

            return tree;
        }

        /// <summary>
        /// Removes all ArticleRefs to the Articles with the passed guid.
        /// Returns the list of all affected nodes.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public List<FullNodeId> RemoveArticleReferences(string guid)
        {
            List<FullNodeId> nodes = new List<FullNodeId>();

            for (int i = 0; i < _chapters.Count; i++)
            {
                Chapter chapter = _chapters[i];
                chapter.StudyTree.Tree.RemoveArticleReferences(guid, ref nodes);
                foreach (Article game in chapter.ModelGames)
                {
                    game.Tree.RemoveArticleReferences(guid, ref nodes);
                }
                foreach (Article exercise in chapter.Exercises)
                {
                    exercise.Tree.RemoveArticleReferences(guid, ref nodes);
                }
            }

            if (nodes.Count > 0)
            {
                AppState.MainWin.RebuildAllTreeViews();
            }

            return nodes;
        }

        /// <summary>
        /// The chapter currently open in the session.
        /// </summary>
        public Chapter ActiveChapter
        {
            get
            {
                if (_activeChapter == null)
                {
                    return SelectDefaultActiveChapter();
                }
                return _activeChapter;
            }
            set
            {
                SetActiveChapter(value);
            }
        }

        /// <summary>
        ///  Returns 0-based Active Chapter index.
        ///  Returns -1 if there is no active chapter.
        /// </summary>
        public int ActiveChapterIndex
        {
            get
            {
                return GetChapterIndex(_activeChapter);
            }
        }

        /// <summary>
        ///  Returns 0-based chapter index.
        ///  Returns 0 if not found.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        public int GetChapterIndex(Chapter chapter)
        {
            for (int i = 0; i < _chapters.Count; i++)
            {
                if (_chapters[i] == chapter)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Returns the number of chapters in this workbook.
        /// </summary>
        /// <returns></returns>
        public int GetChapterCount()
        {
            return _chapters.Count;
        }

        /// <summary>
        /// Returns the total number of study trees, games and exercises
        /// in this Workbook
        /// </summary>
        /// <returns></returns>
        public int GetArticleCount()
        {
            int count = 0;

            foreach (Chapter chapter in _chapters)
            {
                count++; // for the study tree
                count += chapter.GetModelGameCount();
                count += chapter.GetExerciseCount();
            }

            return count;
        }

        /// <summary>
        /// Returns true if the Workbook has at least one Model Game
        /// </summary>
        public bool HasAnyModelGames
        {
            get
            {
                bool has = false;
                foreach (Chapter chapter in _chapters)
                {
                    if (chapter.GetModelGameCount() > 0)
                    {
                        has = true;
                        break;
                    }
                }

                return has;
            }
        }

        /// <summary>
        /// Returns true if the Workbook has at least one Exercise
        /// </summary>
        public bool HasAnyExercises
        {
            get
            {
                bool has = false;
                foreach (Chapter chapter in _chapters)
                {
                    if (chapter.GetExerciseCount() > 0)
                    {
                        has = true;
                        break;
                    }
                }

                return has;
            }
        }

        /// <summary>
        /// Selects the default (first in the list) chapter as ActiveChapter
        /// </summary>
        /// <returns></returns>
        public Chapter SelectDefaultActiveChapter()
        {
            if (Chapters.Count == 0)
            {
                SetActiveChapter(null);
            }
            else
            {
                SetActiveChapter(Chapters[0]);
            }

            return _activeChapter;
        }

        /// <summary>
        /// Selects ActiveChapter based on the passed index.
        /// </summary>
        /// <param name="index"></param>
        public void SelectActiveChapter(int index)
        {
            if (index < 0 || index >= Chapters.Count)
            {
                SelectDefaultActiveChapter();
            }
            else
            {
                _activeChapter = Chapters[index];
            }
        }

        /// <summary>
        /// Creates a new "default" chapter
        /// </summary>
        /// <returns></returns>
        public Chapter CreateDefaultChapter()
        {
            return CreateNewChapter();
        }

        /// <summary>
        /// Sets Active Chapter and Tree given the index of the chapter in the Chapters list.
        /// </summary>
        /// <param name="chapterIndex">index of the requested chapter in the Chapters list.</param>
        /// <param name="gameType"></param>
        /// <param name="gameIndex">index in the list of elements of the requested type i.e. Model Games or Exercises </param>
        public Chapter SetActiveChapterTreeByIndex(int chapterIndex, GameData.ContentType gameType, int gameIndex = 0, bool saveLocation = true)
        {
            // TODO: replace with SelectGame/Exercise somehow
            if (chapterIndex < 0 || chapterIndex >= Chapters.Count)
            {
                return null;
            }

            SetActiveChapter(Chapters[chapterIndex]);
            _activeChapter.SetActiveVariationTree(gameType, gameIndex);

            if (saveLocation)
            {
                WorkbookLocationNavigator.SaveNewLocation(_activeChapter, gameType, gameIndex);
            }
            return _activeChapter;
        }

        /// <summary>
        /// Returns the Active Tree which
        /// is the Active Tree of the active chapter.
        /// </summary>
        public VariationTree ActiveVariationTree
        {
            get
            {
                if (_activeChapter == null)
                {
                    SelectDefaultActiveChapter();
                }

                if (_activeChapter == null)
                {
                    return null;
                }
                else
                {
                    return _activeChapter.ActiveVariationTree;
                }
            }
        }

        /// <summary>
        /// Returns the Active Tree which
        /// is the Active Tree of the active chapter.
        /// </summary>
        public Article ActiveArticle
        {
            get
            {
                if (_activeChapter == null)
                {
                    SelectDefaultActiveChapter();
                }

                if (_activeChapter == null)
                {
                    return null;
                }
                else
                {
                    return _activeChapter.ActiveArticle;
                }
            }
        }

        /// <summary>
        /// Returns the Content Type of the current tab.
        /// </summary>
        public GameData.ContentType ActiveContentType
        {
            get
            {
                GameData.ContentType contentType = GameData.ContentType.NONE;

                switch (WorkbookManager.ActiveTab)
                {
                    case TabViewType.STUDY:
                        contentType = GameData.ContentType.STUDY_TREE;
                        break;
                    case TabViewType.MODEL_GAME:
                        contentType = GameData.ContentType.MODEL_GAME;
                        break;
                    case TabViewType.EXERCISE:
                        contentType = GameData.ContentType.EXERCISE;
                        break;
                    default:
                        contentType = GameData.ContentType.NONE;
                        break;
                }

                return contentType;
            }
        }

        /// <summary>
        /// Workbook's last update date.
        /// </summary>
        public DateTime? LastUpdate
        {
            get => _lastUpdate;
            set => _lastUpdate = value;
        }

        /// <summary>
        /// Description of the Workbook.
        /// In the file, this will be stored as the comment in the Workbook Preface.
        /// </summary>
        public string Description
        {
            get => _description;
            set => _description = value;
        }

        /// <summary>
        /// The title of this Workbook.
        /// </summary>
        public string Title
        {
            get
            {
                if (string.IsNullOrEmpty(_title))
                {
                    return Properties.Resources.UntitledWorkbook;
                }
                else
                {
                    return _title;
                }
            }
            set => _title = (value ?? "");
        }

        /// <summary>
        /// Workbook author's name
        /// </summary>
        public string Author
        {
            get => _author;
            set => _author = value;
        }

        /// <summary>
        /// The Workbook Version object
        /// </summary>
        public WorkbookVersion Version
        {
            get => _version;
        }

        /// <summary>
        /// The Guid of the Workbook
        /// </summary>
        public string Guid
        {
            get
            {
                if (string.IsNullOrEmpty(_guid))
                {
                    _guid = TextUtils.GenerateRandomElementName();
                }
                return _guid;
            }
            set
            {
                _guid = value;
            }
        }

        /// <summary>
        /// Creates a WorkbookVersion object from the passed string
        /// </summary>
        /// <param name="ver"></param>
        public void SetVersion(string ver)
        {
            _version = new WorkbookVersion(ver);
        }

        /// <summary>
        /// Undo renaming of a chapter,
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="previousName"></param>
        public void UndoRenameChapter(Chapter chapter, object previousName)
        {
            try
            {
                string prevName = (previousName ?? "") as string;
                chapter.SetTitle(prevName);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Undo deletion of a chapter.
        /// Inserts the chapter at its original index.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="index"></param>
        public void UndoDeleteChapter(Chapter chapter, int index)
        {
            try
            {
                if (index < 0 || index > Chapters.Count)
                {
                    index = Chapters.Count;
                }
                Chapters.Insert(index, chapter);
                ActiveChapter = chapter;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Undo deletion of multiple chapters.
        /// </summary>
        /// <param name="chapters"></param>
        /// <param name="indices"></param>
        public void UndoDeleteChapters(object objChapterList, object objIndexList)
        {
            try
            {
                List<Chapter> chapters = objChapterList as List<Chapter>;
                List<int> indices = objIndexList as List<int>;

                if (chapters != null && indices != null && chapters.Count == indices.Count)
                {
                    // reverse order to when we were deleting so that indices work correctly
                    for (int i = chapters.Count - 1; i >= 0; i--)
                    {
                        UndoDeleteChapter(chapters[i], indices[i]);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Undo merging of chapters.
        /// Delete created chapter and restore deleted (source) ones
        /// </summary>
        /// <param name="chapters"></param>
        /// <param name="indices"></param>
        public void UndoMergeChapters(Chapter chapter, object objChapterList, object objIndexList)
        {
            try
            {
                Chapters.Remove(chapter);

                List<Chapter> chapters = objChapterList as List<Chapter>;
                List<int> indices = objIndexList as List<int>;

                if (chapters != null && indices != null && chapters.Count == indices.Count)
                {
                    // reverse order to when we were deleting so that indices work correctly
                    for (int i = chapters.Count - 1; i >= 0; i--)
                    {
                        UndoDeleteChapter(chapters[i], indices[i]);
                    }
                }

                if (indices.Count > 0)
                {
                    PulseManager.ChapterIndexToBringIntoView = indices[0];
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Undo splitting of a chpater.
        /// The chapters from the objChapterList will be deleted and the "chapter"
        /// will be inserted in place of the first chapter from the list.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="objChapterList"></param>
        public void UndoSplitChapter(Chapter chapter, object objChapterList)
        {
            try
            {
                List<Chapter> chapters = objChapterList as List<Chapter>;
                int index = chapters[0].Index;

                List<Chapter> chaptersToDelete = new List<Chapter>();
                foreach (Chapter ch in Chapters)
                {
                    chaptersToDelete.Add(ch);
                }

                foreach (Chapter ch in chaptersToDelete)
                {
                    Chapters.Remove(ch);
                }

                Chapters.Insert(index, chapter);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Undo creation/addition of an Article.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="article"></param>
        public void UndoCreateArticle(Chapter chapter, Article article)
        {
            try
            {
                if (chapter != null && article != null)
                {
                    chapter.DeleteArticle(article);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Undo deletion of a Model Game
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="article"></param>
        /// <param name="index"></param>
        public void UndoDeleteModelGame(Chapter chapter, Article article, int index, object nodeTreeIdList)
        {
            try
            {
                chapter.InsertModelGame(article, index);
                chapter.ActiveModelGameIndex = index;
                if (nodeTreeIdList is List<FullNodeId> nodeTreeIds)
                {
                    RestoreReferences(article.Guid, nodeTreeIds);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Undo deletion of a Model Game
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="article"></param>
        /// <param name="index"></param>
        public void UndoDeleteExercise(Chapter chapter, Article article, int index, object nodeTreeIdList)
        {
            try
            {
                chapter.InsertExercise(article, index);
                chapter.ActiveExerciseIndex = index;
                if (nodeTreeIdList is List<FullNodeId> nodeTreeIds)
                {
                    RestoreReferences(article.Guid, nodeTreeIds);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Undo deletion of multiple articles (games or exercises)
        /// </summary>
        /// <param name="objArticleList"></param>
        /// <param name="objIndices"></param>
        public void UndoDeleteArticles(object objArticleList, object objIndexList, object objRefNodesList)
        {
            try
            {
                List<ArticleListItem> articleList = objArticleList as List<ArticleListItem>;
                List<int> indexList = objIndexList as List<int>;
                List<List<FullNodeId>> refNodesList = objRefNodesList as List<List<FullNodeId>>;

                // undo in the reverse order to how they were deleted
                for (int i = articleList.Count - 1; i >= 0; i--)
                {
                    Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(articleList[i].ChapterIndex);
                    if (articleList[i].Article.ContentType == GameData.ContentType.MODEL_GAME)
                    {
                        chapter.InsertModelGame(articleList[i].Article, indexList[i]);
                        chapter.ActiveModelGameIndex = indexList[i];
                    }
                    else if (articleList[i].Article.ContentType == GameData.ContentType.EXERCISE)
                    {
                        chapter.InsertExercise(articleList[i].Article, indexList[i]);
                        chapter.ActiveExerciseIndex = indexList[i];
                    }

                    if (refNodesList != null)
                    {
                        RestoreReferences(articleList[i].Article.Guid, refNodesList[i]);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Restores previous versions of the study trees.
        /// </summary>
        /// <param name="objChapterList"></param>
        /// <param name="ReplacedTreesList"></param>
        public void UndoRegenerateStudies(object objChapterList, object objReplacedTreesList)
        {
            try
            {
                if (objChapterList is List<Chapter> chapters && objReplacedTreesList is List<VariationTree> trees)
                {
                    for (int i = 0; i < chapters.Count; i++)
                    {
                        chapters[i].StudyTree.Tree = trees[i];
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Restore references to the passed guid in the nodes from the passed list.
        /// </summary>
        /// <param name="guidToRestore"></param>
        /// <param name="nodeTreeIds"></param>
        public void RestoreReferences(string guidToRestore, List<FullNodeId> nodeTreeIds)
        {
            foreach (FullNodeId fullId in nodeTreeIds)
            {
                var tree = GetTreeByTreeId(fullId.TreeId);
                if (tree != null)
                {
                    TreeNode nd = tree.GetNodeFromNodeId(fullId.NodeId);
                    if (nd != null)
                    {
                        ReferenceUtils.AddReferenceToNode(nd, guidToRestore);
                    }
                }
            }
        }

        /// <summary>
        /// Undo deletion of comments and NAGs from articles
        /// </summary>
        /// <param name="dictMoveAttributes"></param>
        public void UndoDeleteComments(object dictMoveAttributes)
        {
            Dictionary<Article, List<MoveAttributes>> dictUndoData = dictMoveAttributes as Dictionary<Article, List<MoveAttributes>>;
            foreach (Article article in dictUndoData.Keys)
            {
                TreeUtils.InsertCommentsAndNags(article.Tree, dictUndoData[article]);
            }
        }

        /// <summary>
        /// Notes can be comments, NAGs, engine evaluation or nodes.
        /// First the nodes must be restored, then then added to their parents children list
        /// and then notes and evals can be added.
        /// </summary>
        /// <param name="dictMoveAttributes"></param>
        public void UndoCleanLinesAndComments(object dictMoveAttributes, object lstArticleAttributes)
        {
            Dictionary<Article, List<MoveAttributes>> dictMoveAttrs = dictMoveAttributes as Dictionary<Article, List<MoveAttributes>>;
            List<ArticleAttributes> lstArticleAttrs = lstArticleAttributes as List<ArticleAttributes>;
            
            // restore article attributes
            foreach (ArticleAttributes attrs in lstArticleAttrs)
            {
                Article article = AppState.Workbook.GetArticleByGuid(attrs.Guid, out _, out _);
                if (article != null)
                {
                    article.Tree.Header.SetHeaderValue(PgnHeaders.KEY_ANNOTATOR, attrs.Annotator);
                }
            }

            // restore move attributes
            foreach (Article article in dictMoveAttrs.Keys)
            {
                List<MoveAttributes> toRestore = new List<MoveAttributes>();

                List<MoveAttributes> moveAttrs = dictMoveAttrs[article];
                foreach (var item in moveAttrs)
                {
                    if (item.IsDeleted && item.Node != null)
                    {
                        article.Tree.AddNode(item.Node);
                        toRestore.Add(item);
                    }
                }

                // the toRestore list must be sorted by ChildIndexInParent (ascending),
                // so that the nodes are inserted in the correct order
                toRestore.OrderBy(x => x.ChildIndexInParent);
                for (int i = 0; i < toRestore.Count; i++)
                {
                    MoveAttributes attrs = toRestore[i];
                    TreeNode parent = article.Tree.GetNodeFromNodeId(attrs.ParentId);

                    // perform some defensive checks
                    if (parent != null)
                    {
                        if (parent.Children.Count > attrs.ChildIndexInParent)
                        {
                            parent.Children.Insert(attrs.ChildIndexInParent, attrs.Node);
                        }
                        else
                        {
                            parent.Children.Add(attrs.Node);
                        }
                    }
                }

                TreeUtils.InsertCommentsAndNags(article.Tree, dictMoveAttrs[article]);
                TreeUtils.InsertEngineEvals(article.Tree, dictMoveAttrs[article]);
            }

            foreach (Article article in dictMoveAttrs.Keys)
            {
                TreeUtils.InsertCommentsAndNags(article.Tree, dictMoveAttrs[article]);
                TreeUtils.InsertEngineEvals(article.Tree, dictMoveAttrs[article]);
            }
        }

        /// <summary>
        /// Undo deletion of engine evaluations from articles
        /// </summary>
        /// <param name="dictMoveAttributes"></param>
        public void UndoDeleteEngineEvals(object dictMoveAttributes)
        {
            Dictionary<Article, List<MoveAttributes>> dictUndoData = dictMoveAttributes as Dictionary<Article, List<MoveAttributes>>;

            foreach (Article article in dictUndoData.Keys)
            {
                TreeUtils.InsertEngineEvals(article.Tree, dictUndoData[article]);
            }
        }

        /// <summary>
        /// Return chapter from a given position in the Chapters list
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Chapter GetChapterByIndex(int idx)
        {
            if (idx >= 0 && idx < Chapters.Count)
            {
                return Chapters[idx];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns chapter with a given guid and its index.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public Chapter GetChapterByGuid(string guid, out int index)
        {
            index = -1;

            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            Chapter chapter = null;
            for (int i = 0; i < Chapters.Count; i++)
            {
                if (Chapters[i].Guid == guid)
                {
                    chapter = Chapters[i];
                    index = i;
                    break;
                }
            }

            return chapter;
        }

        /// <summary>
        /// Creates a new chapter.
        /// </summary>
        /// <param name="tree"></param>
        public Chapter CreateNewChapter()
        {
            Chapter chapter = new Chapter();
            chapter.StudyTree = new Article(GameData.ContentType.STUDY_TREE);
            chapter.StudyTree.Tree.CreateNew();
            //TODO: we need to have a chapter specific version of SetupGuiForNewSession 

            Chapters.Add(chapter);
            SetActiveChapter(chapter);

            WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.CREATE_CHAPTER, chapter, 0);
            WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);

            _activeChapter.SetActiveVariationTree(GameData.ContentType.STUDY_TREE);

            AppState.ConfigureMenusForManualReview();
            return chapter;
        }

        /// <summary>
        /// Creates a new chapter and adds the passed tree as the chapter's Study Tree.
        /// </summary>
        /// <param name="tree"></param>
        public Chapter CreateNewChapter(VariationTree tree, bool makeActive = true)
        {
            Chapter chapter = new Chapter();
            chapter.StudyTree = new Article(tree);

            Chapters.Add(chapter);

            WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.CREATE_CHAPTER, chapter, 0);
            WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);

            if (makeActive)
            {
                SetActiveChapter(chapter);
            }

            AppState.ConfigureMenusForManualReview();
            return chapter;
        }

        /// <summary>
        /// Deletes a chapter from this workbook
        /// </summary>
        /// <param name="ch"></param>
        public void DeleteChapter(Chapter ch)
        {
            try
            {
                int index = Chapters.IndexOf(ch);
                if (index >= 0)
                {
                    Chapters.Remove(ch);
                    WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.DELETE_CHAPTER, ch, index);
                    WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
                    AppState.ConfigureMenusForManualReview();
                }
            }
            catch { }
        }

        /// <summary>
        /// Deletes a chapter from this workbook
        /// </summary>
        /// <param name="ch"></param>
        public void DeleteChapters(List<Chapter> chapters)
        {
            if (chapters != null && chapters.Count > 0)
            {
                List<int> indices = new List<int>();
                foreach (Chapter chapter in chapters)
                {
                    indices.Add(chapter.Index);
                    Chapters.Remove(chapter);
                }

                WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.DELETE_CHAPTERS, null, -1, chapters, indices, null);
                WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
                AppState.ConfigureMenusForManualReview();
            }
        }

        /// <summary>
        /// Creates a new chapter from the passed tree and deletes the source chapters
        /// </summary>
        /// <param name="mergedTree"></param>
        /// <param name="title"></param>
        /// <param name="sourceChapters"></param>
        public void MergeChapters(VariationTree mergedTree, string title, List<Chapter> sourceChapters)
        {
            // create new chapter
            Chapter mergedChapter = new Chapter();
            mergedChapter.SetTitle(title);

            mergedChapter.StudyTree = new Article(mergedTree);
            Chapters.Add(mergedChapter);
            SetActiveChapter(mergedChapter);

            CopyGamesToChapter(mergedChapter, sourceChapters);
            CopyExercisesToChapter(mergedChapter, sourceChapters);

            // delete source chapters
            List<int> indices = new List<int>();
            foreach (Chapter ch in sourceChapters)
            {
                indices.Add(ch.Index);
                Chapters.Remove(ch);
            }

            WorkbookOperation op = new WorkbookOperation(WorkbookOperationType.MERGE_CHAPTERS, mergedChapter, -1, sourceChapters, indices, null);
            WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
            AppState.ConfigureMenusForManualReview();
        }

        /// <summary>
        /// Returns the expand/collapse status of the chapter in the ChaptersView.
        /// If true the chapter view is expanded, if false, the chapter view is collapsed,
        /// null if chapter not found.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <returns></returns>
        public bool? IsChapterViewExpanded(int chapterIndex)
        {
            bool? ret = null;

            foreach (Chapter chapter in Chapters)
            {
                if (chapter.Index == chapterIndex)
                {
                    ret = chapter.IsViewExpanded;
                    break;
                }
            }

            return ret;
        }

        /// <summary>
        /// Deletes an Article (a ModelGame or an Exercise) from the Workbook.
        /// Note that we do not rely on the article's GUID as there may be duplicates
        /// due to bugs etc.
        /// </summary>
        /// <param name="article"></param>
        public void DeleteArticle(Article article)
        {
            if (article != null)
            {
                try
                {
                    foreach (Chapter chapter in Chapters)
                    {
                        List<Article> articles = null;
                        if (article.ContentType == GameData.ContentType.MODEL_GAME)
                        {
                            articles = chapter.ModelGames;
                        }
                        else if (article.ContentType == GameData.ContentType.EXERCISE)
                        {
                            articles = chapter.Exercises;
                        }

                        foreach (Article item in articles)
                        {
                            if (item == article)
                            {
                                chapter.ModelGames.Remove(item);
                                break;
                            }
                        }
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Copies all games from the selected chapters in the list to the target chapter
        /// </summary>
        /// <param name="target"></param>
        /// <param name="chapters"></param>
        private void CopyGamesToChapter(Chapter target, List<Chapter> chapters)
        {
            foreach (Chapter ch in chapters)
            {
                foreach (Article game in ch.ModelGames)
                {
                    target.AddModelGame(game.Tree);
                }
            }
        }

        /// <summary>
        /// Copies all exercises from the selected chapters in the list to the target chapter
        /// </summary>
        /// <param name="target"></param>
        /// <param name="sources"></param>
        private void CopyExercisesToChapter(Chapter target, List<Chapter> sources)
        {
            foreach (Chapter ch in sources)
            {
                foreach (Article item in ch.Exercises)
                {
                    target.AddExercise(item.Tree);
                }
            }
        }

        /// <summary>
        /// Method to be used for setting the value of _activeChapter within this class.
        /// </summary>
        /// <param name="chapter"></param>
        private Chapter SetActiveChapter(Chapter chapter)
        {
            _activeChapter = chapter;
            AppState.ShowIntroTab(_activeChapter);
            GuiUtilities.SetShowSolutionsMenuCheckMark(_activeChapter);
            return _activeChapter;
        }
    }
}

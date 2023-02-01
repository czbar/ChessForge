using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using ChessPosition;
using ChessPosition.GameTree;
using GameTree;

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
        /// The training side.
        /// </summary>
        public PieceColor TrainingSideConfig = PieceColor.None;

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
        private string _title;

        // Version object for this Workbook
        private WorkbookVersion _version;

        // Guid of the Workbook
        private string _guid;

        /// <summary>
        /// An object managing identities of the trees.
        /// </summary>
        private TreeManager _treeManager = new TreeManager();

        // associated OperationsManager
        public WorkbookOperationsManager OpsManager;

        /// <summary>
        /// The constructor.
        /// Resets the TreeManager. 
        /// Creates Operations Manager,
        /// </summary>
        public Workbook()
        {
            TreeManager.Reset();
            OpsManager = new WorkbookOperationsManager(this);
            _version = new WorkbookVersion();
        }

        /// <summary>
        /// Builds an Article list for use in the Select Article dialog.
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<ArticleListItem> GenerateArticleList()
        {
            ObservableCollection<ArticleListItem> articleList = new ObservableCollection<ArticleListItem>();

            foreach (Chapter chapter in Chapters)
            {
                // an item for the Chapter itself
                ArticleListItem chaptItem = new ArticleListItem(chapter);
                articleList.Add(chaptItem);
                for (int i = 0; i < chapter.ModelGames.Count; i++)
                {
                    ArticleListItem artItem = new ArticleListItem(chapter, chapter.ModelGames[i], i);
                    articleList.Add(artItem);
                }
                for (int i = 0; i < chapter.Exercises.Count; i++)
                {
                    ArticleListItem artItem = new ArticleListItem(chapter, chapter.Exercises[i], i);
                    articleList.Add(artItem);
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
        public Article GetArticleByGuid(string guid, out int chapterIndex, out int articleIndex)
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
            }

            return null;
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
                Chapters[i].StudyTree.Tree.RemoveArticleReferences(guid, ref nodes);
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
            set => _activeChapter = value;
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
        /// Selects the default (first in the list) chapter as ActiveChapter
        /// </summary>
        /// <returns></returns>
        public Chapter SelectDefaultActiveChapter()
        {
            if (Chapters.Count == 0)
            {
                _activeChapter = null;
            }
            else
            {
                _activeChapter = Chapters[0];
            }

            return _activeChapter;
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
        public void SetActiveChapterTreeByIndex(int chapterIndex, GameData.ContentType gameType, int gameIndex = 0)
        {
            if (chapterIndex < 0 || chapterIndex >= Chapters.Count)
            {
                return;
            }

            _activeChapter = Chapters[chapterIndex];
            _activeChapter.SetActiveVariationTree(gameType, gameIndex);
        }

        /// <summary>
        /// Sets Active Chapter and Tree given the Id of the chapter.
        /// </summary>
        /// <param name="chapterId"></param>
        /// <param name="gameType"></param>
        /// <param name="gameIndex"></param>
        public void SetActiveChapterTreeById(int chapterId, GameData.ContentType gameType, int gameIndex = 0)
        {
            foreach (Chapter chapter in Chapters)
            {
                if (chapter.Id == chapterId)
                {
                    _activeChapter = chapter;
                    _activeChapter.SetActiveVariationTree(gameType, gameIndex);
                    break;
                }
            }

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
                    case WorkbookManager.TabViewType.STUDY:
                        contentType = GameData.ContentType.STUDY_TREE;
                        break;
                    case WorkbookManager.TabViewType.MODEL_GAME:
                        contentType = GameData.ContentType.MODEL_GAME;
                        break;
                    case WorkbookManager.TabViewType.EXERCISE:
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
            set => _title = value;
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
                    _guid = System.Guid.NewGuid().ToString();
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
                Chapters.Insert(index, chapter);
                ActiveChapter = chapter;
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
        public void UndoDeleteModelGame(Chapter chapter, Article article, int index)
        {
            try
            {
                chapter.InsertModelGame(article, index);
                chapter.ActiveModelGameIndex = index;
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
        public void UndoDeleteExercise(Chapter chapter, Article article, int index)
        {
            try
            {
                chapter.InsertExercise(article, index);
                chapter.ActiveExerciseIndex = index;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Returns the Chapter object with the passed id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Chapter GetChapterById(int id)
        {
            return Chapters.FirstOrDefault(ch => ch.Id == id);
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
        /// Returns position of a chapter with a given id
        /// in the Chapters list.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int GetChapterIndexFromId(int id)
        {
            int idx = -1;

            for (int i = 0; i < Chapters.Count; i++)
            {
                if (Chapters[i].Id == id)
                {
                    idx = i;
                    break;
                }
            }

            return idx;
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
            chapter.Id = GenerateChapterId();

            Chapters.Add(chapter);
            _activeChapter = chapter;
            _activeChapter.SetActiveVariationTree(GameData.ContentType.STUDY_TREE);

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
            chapter.Id = GenerateChapterId();

            Chapters.Add(chapter);

            if (makeActive)
            {
                _activeChapter = chapter;
            }

            //TrainingSideConfig = tree.TrainingSide;

            return chapter;
        }

        /// <summary>
        /// Deletes a chapter from this workbook
        /// </summary>
        /// <param name="ch"></param>
        public void DeleteChapter(Chapter ch)
        {
            int index = Chapters.IndexOf(ch);
            if (index >= 0)
            {
                Chapters.Remove(ch);
                WorkbookOperation op = new WorkbookOperation(WorkbookOperation.WorkbookOperationType.DELETE_CHAPTER, ch, index);
                WorkbookManager.SessionWorkbook.OpsManager.PushOperation(op);
            }
        }

        /// <summary>
        /// Returns the expand/collapse status of the chapter in the ChaptersView.
        /// If true the chapter view is expanded, if false, the chapter view is collapsed,
        /// null if chapter not found.
        /// </summary>
        /// <param name="chapterId"></param>
        /// <returns></returns>
        public bool? IsChapterViewExpanded(int chapterId)
        {
            bool? ret = null;

            foreach (Chapter chapter in Chapters)
            {
                if (chapter.Id == chapterId)
                {
                    ret = chapter.IsViewExpanded;
                    break;
                }
            }

            return ret;
        }

        /// <summary>
        /// Finds the highest chapter id in the list
        /// and increments it by one.
        /// </summary>
        /// <returns></returns>
        private int GenerateChapterId()
        {
            int id = 1;

            foreach (Chapter chapter in Chapters)
            {
                if (chapter.Id >= id)
                {
                    id = chapter.Id + 1;
                }
            }

            return id;
        }
    }
}

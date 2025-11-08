using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Markup;

namespace ChessForge
{
    /// <summary>
    /// A chapter within a Workbook.
    /// A chapter comprises one or more VariationTrees. 
    /// </summary>
    public class Chapter
    {
        /// <summary>
        /// The Study Tree of the chapter. There is exactly one Study Tree in a chapter.
        /// </summary>
        public Article StudyTree = new Article(GameData.ContentType.STUDY_TREE);

        /// <summary>
        /// The Intro article. There is exactly one Intro in a chapter.
        /// </summary>
        public Article Intro;

        /// <summary>
        /// The list of Model Games Trees
        /// </summary>
        public List<Article> ModelGames = new List<Article>();

        /// <summary>
        /// The list of Exercises Tress.
        /// </summary>
        public List<Article> Exercises = new List<Article>();

        /// <summary>
        /// Whether the IntroTab should be shown even if empty.
        /// This is set to true when the user requested creation of the Intro
        /// tab for the current chapter.
        /// </summary>
        public bool AlwaysShowIntroTab = false;

        /// <summary>
        /// Whether solution to the Exercises should be shown or hidden when opening.
        /// </summary>
        public bool ShowSolutionsOnOpen
        {
            get
            {
                return StudyTree.Tree.Header.GetShowSolutionsOnOpen() == "1";
            }
            set
            {
                StudyTree.Tree.Header.SetHeaderValue(PgnHeaders.KEY_SHOW_SOLUTIONS_ON_OPEN, value ? "1" : "0");
            }
        }

        /// <summary>
        /// Whether the Intro tab is to be shown
        /// </summary>
        public bool ShowIntro
        {
            get => AlwaysShowIntroTab || !IsSavedIntroEmpty();
        }

        /// <summary>
        /// The Guid of the Chapter which is the GUI of the Study Tree.
        /// Generates a guid if empty.
        /// </summary>
        public string Guid
        {
            get
            {
                string guid = StudyTree.Guid;
                if (string.IsNullOrEmpty(guid))
                {
                    guid = TextUtils.GenerateGuid();
                }
                return guid;
            }
            set
            {
                StudyTree.Guid = value;
            }
        }

        // title of this chapter
        private string _title;

        // VariationTree to be used when this chapter becomes active.
        private VariationTree _activeTree;

        // Article to be used when this chapter becomes active.
        private Article _activeArticle;

        // index of the currently shown game in the Model Games list
        private int _activeModelGameIndex = -1;

        // index of the currently shown exercise in the Exercises list
        private int _activeExerciseIndex = -1;

        // number of levels in the variation index of the Study view.
        private int? _variationIndexDepth = null;

        // whether the chapter is expanded in the ChaptersView
        private bool _isViewExpanded = true;

        // whether the Model Games list is expanded in the ChaptersView
        private bool _isModelGamesListExpanded;

        // whether the Exercises list is expanded in the ChaptersView
        private bool _isExercisesListExpanded;

        // associated OperationsManager
        private WorkbookOperationsManager _opsManager;

        // lock object for ModelGames
        private object _lockModelGames = new object();

        /// <summary>
        /// Creates the object. Initializes Operations Manager
        /// </summary>
        public Chapter()
        {
            _opsManager = new WorkbookOperationsManager(this);

            Intro = new Article(GameData.ContentType.INTRO);
            Intro.Tree.AddNode(new TreeNode(null, "", 0));
        }

        /// <summary>
        /// Determines whether the Study is empty (mainly for Export/Print purposes).
        /// The study is considered empty if it only has a root node and no comment on it.
        /// </summary>
        /// <returns></returns>
        public bool IsStudyEmpty()
        {
            return StudyTree.Tree.Nodes.Count == 0 || 
                StudyTree.Tree.Nodes.Count == 1 && string.IsNullOrWhiteSpace(StudyTree.Tree.Nodes[0].Comment);
        }

        /// <summary>
        /// Checks if both the current intro view and the saved intro
        /// is empty.
        /// It does this by forcing to save the Intro view if currently active
        /// and them calling IsSavedIntroEmpty().
        /// </summary>
        /// <returns></returns>
        public bool IsIntroEmpty()
        {
            if (AppState.ActiveTab == TabViewType.INTRO)
            {
                AppState.MainWin.SaveIntro();
            }

            return IsSavedIntroEmpty();
        }

        /// <summary>
        /// Checks if the content of the saved Intro article is empty.
        /// It is empty if there is nothing in the CodedContent property
        /// or no Paragraph in the decoded content.
        /// </summary>
        /// <returns></returns>
        public bool IsSavedIntroEmpty()
        {
            string content = EncodingUtils.Base64Decode(Intro.CodedContent);
            int paraPos = content.IndexOf("<Paragraph");
            if (paraPos < 0)
            {
                return true;
            }
            else
            {
                bool result = true;
                // it will still be empty if there is no other paragraph, and it only has an empty Run element
                if (content.IndexOf("<Paragraph", paraPos + 1) < 0)
                {
                    // has just 1 para so check if it all it has are empty runs
                    FlowDocument doc = XamlReader.Parse(content) as FlowDocument;
                    foreach (Block block in doc.Blocks)
                    {
                        if (block is Paragraph)
                        {
                            foreach (Inline inl in (block as Paragraph).Inlines)
                            {
                                if (inl is Run && !string.IsNullOrEmpty((inl as Run).Text)
                                    || !(inl is Run))
                                {
                                    result = false;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // has more than 1 paragraph
                    result = false;
                }
                return result;
            }
        }

        /// <summary>
        /// Deletes an article if found, and returns true.
        /// Returns false if the article was not found.
        /// Adjusts the active index for the affected list.
        /// The caller is responsible for setting up an UNDO operation.
        /// </summary>
        /// <param name="article"></param>
        /// <returns></returns>
        public bool DeleteArticle(Article article)
        {
            if (article == null)
            {
                return false;
            }

            bool result = false;

            if (article.ContentType == GameData.ContentType.MODEL_GAME)
            {
                result = ModelGames.Remove(article);
                if (result && _activeModelGameIndex >= ModelGames.Count)
                {
                    _activeModelGameIndex = ModelGames.Count - 1;
                }
            }
            else if (article.ContentType == GameData.ContentType.EXERCISE)
            {
                result = Exercises.Remove(article);
                if (result && _activeExerciseIndex >= Exercises.Count)
                {
                    _activeExerciseIndex = Exercises.Count - 1;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the index of the article in the list of articles
        /// of its type.
        /// </summary>
        /// <param name="article"></param>
        /// <returns></returns>
        public int GetArticleIndex(Article article)
        {
            int index = -1;
            if (article != null)
            {
                if (article.ContentType == GameData.ContentType.MODEL_GAME)
                {
                    for (int i = 0; i < ModelGames.Count; i++)
                    {
                        if (ModelGames[i] == article)
                        {
                            index = i;
                        }
                    }
                }
                else if (article.ContentType == GameData.ContentType.EXERCISE)
                {
                    for (int i = 0; i < Exercises.Count; i++)
                    {
                        if (Exercises[i] == article)
                        {
                            index = i;
                        }
                    }
                }
            }

            return index;
        }

        /// <summary>
        /// Returns an article of the requested type
        /// at the requested index.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public Article GetArticleAtIndex(GameData.ContentType contentType, int index)
        {
            Article article = null;

            try
            {
                switch (contentType)
                {
                    case GameData.ContentType.STUDY_TREE:
                        article = StudyTree;
                        break;
                    case GameData.ContentType.INTRO:
                        article = Intro;
                        break;
                    case GameData.ContentType.MODEL_GAME:
                        article = GetModelGameAtIndex(index);
                        break;
                    case GameData.ContentType.EXERCISE:
                        article = GetExerciseAtIndex(index);
                        break;
                }
            }
            catch { }

            return article;
        }

        /// <summary>
        /// Returns a Model Game stored at a given index.
        /// Null if invalid index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Article GetModelGameAtIndex(int index)
        {
            if (index >= 0 && index < ModelGames.Count)
            {
                return ModelGames[index];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a Model Game with a given guid and its index.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public Article GetModelGameByGuid(string guid, out int index)
        {
            index = -1;

            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            Article article = null;

            for (int i = 0; i < ModelGames.Count; i++)
            {
                if (ModelGames[i].Guid == guid)
                {
                    article = ModelGames[i];
                    index = i;
                }
            }

            return article;
        }

        /// <summary>
        /// Returns Exercise stored at a given index.
        /// Null if invalid index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Article GetExerciseAtIndex(int index)
        {
            if (index >= 0 && index < Exercises.Count)
            {
                return Exercises[index];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a Exercise with a given guid and its index.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public Article GetExerciseByGuid(string guid, out int index)
        {
            index = -1;

            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            Article article = null;

            for (int i = 0; i < Exercises.Count; i++)
            {
                if (Exercises[i].Guid == guid)
                {
                    article = Exercises[i];
                    index = i;
                }
            }

            return article;
        }

        /// <summary>
        // Index of the currently shown Game in the Model Games list
        /// </summary>
        public int ActiveModelGameIndex
        {
            get
            {
                if (_activeModelGameIndex < 0 && ModelGames.Count > 0)
                {
                    _activeModelGameIndex = 0;
                }
                return _activeModelGameIndex;
            }
            set => _activeModelGameIndex = value;
        }


        /// <summary>
        // Index of the currently shown Exercise in the Exercises list
        /// </summary>
        public int ActiveExerciseIndex
        {
            get
            {
                if (_activeExerciseIndex < 0 && Exercises.Count > 0)
                {
                    _activeExerciseIndex = 0;
                }
                return _activeExerciseIndex;
            }
            set => _activeExerciseIndex = value;
        }

        /// <summary>
        /// Holds the depth of the variation index in the study tree.
        /// Ensures that the return value is within -1 to Configuration.MAX_INDEX_DEPTH range.
        /// </summary>
        public int? VariationIndexDepth
        {
            get
            {
                if (_variationIndexDepth == null)
                {
                    // if the last set depth is null, check if we have a value from the PGN header
                    string sDepth = StudyTree.Tree.Header.GetIndexDepth(out _);
                    if (!string.IsNullOrEmpty(sDepth) && int.TryParse(sDepth, out int depth))
                    {
                        _variationIndexDepth = depth;
                    }
                }

                if (_variationIndexDepth == null)
                {
                    _variationIndexDepth = Configuration.DefaultIndexDepth;
                }
                else if (_variationIndexDepth < -1)
                {
                    _variationIndexDepth = -1;
                }
                else if (_variationIndexDepth > Configuration.MAX_INDEX_DEPTH)
                {
                    _variationIndexDepth = Configuration.MAX_INDEX_DEPTH;
                }

                return _variationIndexDepth;
            }

            set => _variationIndexDepth = value;
        }

        /// <summary>
        /// Corrects the index if it is out of range e.g. after deletion.
        /// </summary>
        public void CorrectActiveModelGameIndex()
        {
            if (_activeModelGameIndex >= ModelGames.Count)
            {
                _activeModelGameIndex = ModelGames.Count - 1;
            }
        }

        /// <summary>
        /// Corrects the index if it is out of range e.g. after deletion.
        /// </summary>
        public void CorrectActiveExerciseIndex()
        {
            if (_activeExerciseIndex >= Exercises.Count)
            {
                _activeExerciseIndex = Exercises.Count - 1;
            }
        }

        /// <summary>
        /// Returns Tree "active" in this chapter.
        /// </summary>
        public VariationTree ActiveVariationTree
        {
            get
            {
                if (_activeTree != null && _activeTree.IsAssociatedTreeActive && _activeTree.AssociatedSecondary != null)
                {

                    return _activeTree.AssociatedSecondary;
                }
                else
                {
                    return _activeTree;
                }
            }
        }

        /// <summary>
        /// Returns Article "active" in this chapter.
        /// </summary>
        public Article ActiveArticle
        {
            get
            {
                return _activeArticle;
            }
        }

        /// <summary>
        /// Returns type of the active article
        /// </summary>
        /// <returns></returns>
        public GameData.ContentType GetActiveArticleType()
        {
            if (ActiveArticle == null)
            {
                return GameData.ContentType.NONE;
            }
            else
            {
                return ActiveArticle.Tree.ContentType;
            }
        }

        /// <summary>
        /// Returns the index of the active article in the list is in
        /// </summary>
        /// <returns></returns>
        public int GetActiveArticleIndex()
        {
            GameData.ContentType type = GetActiveArticleType();
            if (type == GameData.ContentType.MODEL_GAME)
            {
                return _activeModelGameIndex;
            }
            else if (type == GameData.ContentType.EXERCISE)
            {
                return _activeExerciseIndex;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Returns reference to the Active Game's header.
        /// </summary>
        /// <returns></returns>
        public GameHeader GetActiveModelGameHeader()
        {
            GameHeader gameHeader = null;
            try
            {
                if (ModelGames.Count > 0)
                    gameHeader = ModelGames[ActiveModelGameIndex].Tree.Header;
            }
            catch (Exception ex)
            {
                AppLog.Message("GetActiveModelGameHeader()", ex);
            }

            return gameHeader;
        }

        /// <summary>
        /// Sets the ActiveVariationTree based on the passed type and index.
        /// </summary>
        /// <param name="gameType"></param>
        /// <param name="gameIndex"></param>
        public void SetActiveVariationTree(GameData.ContentType gameType, int gameIndex = 0)
        {
            switch (gameType)
            {
                case GameData.ContentType.STUDY_TREE:
                    _activeTree = StudyTree.Tree;
                    _activeArticle = StudyTree;
                    break;
                case GameData.ContentType.INTRO:
                    _activeTree = Intro.Tree;
                    _activeArticle = Intro;
                    break;
                case GameData.ContentType.MODEL_GAME:
                    if (gameIndex >= 0 && gameIndex < ModelGames.Count)
                    {
                        _activeTree = ModelGames[gameIndex].Tree;
                        _activeArticle = ModelGames[gameIndex];
                    }
                    break;
                case GameData.ContentType.EXERCISE:
                    if (gameIndex >= 0 && gameIndex < Exercises.Count)
                    {
                        _activeTree = Exercises[gameIndex].Tree;
                        _activeArticle = Exercises[gameIndex];
                    }
                    break;
                default:
                    _activeTree = null;
                    _activeArticle = null;
                    break;
            }
        }

        /// <summary>
        /// Position of this chapter on the workbook's list of chapters.
        /// </summary>
        public int Index
        {
            get => WorkbookManager.SessionWorkbook.GetChapterIndex(this);
        }

        /// <summary>
        /// Unadorned chapter title
        /// </summary>
        public string Title
        {
            get => _title ?? "";
        }

        /// <summary>
        /// Get the title prefixed by the chapter's number (i.e. index + 1)
        /// </summary>
        public string TitleWithNumber
        {
            get => "[" + (Index + 1).ToString() + "] " + (GetTitle());
        }

        /// <summary>
        /// The Title of this chapter.
        /// If raw is set to false and the title is empty
        /// it returns the default title.
        /// </summary>
        public string GetTitle(bool raw = false)
        {
            if (raw || !string.IsNullOrWhiteSpace(_title))
            {
                return _title ?? "";
            }
            else
            {
                return Properties.Resources.Chapter + " " + (Index + 1).ToString();
            }
        }

        /// <summary>
        /// Returns the author of this chapter.
        /// </summary>
        /// <returns></returns>
        public string GetAuthor()
        {
            return StudyTree.Tree.Header.GetAnnotator(out _);
        }

        /// <summary>
        /// Sets the title of the Chapter.
        /// </summary>
        /// <param name="title"></param>
        public void SetTitle(string title)
        {
            _title = title;
            StudyTree.Tree.Header.SetHeaderValue(PgnHeaders.KEY_CHAPTER_TITLE, title);
        }

        /// <summary>
        /// Sets the title of the Chapter.
        /// </summary>
        /// <param name="author"></param>
        public void SetAuthor(string author)
        {
            StudyTree.Tree.Header.SetHeaderValue(PgnHeaders.KEY_ANNOTATOR, author);
        }

        /// <summary>
        /// Returns the numer of model games in this chapter
        /// </summary>
        /// <returns></returns>
        public int GetModelGameCount()
        {
            return ModelGames.Count();
        }

        /// <summary>
        /// Returns the numer of exercises in this chapter
        /// </summary>
        /// <returns></returns>
        public int GetExerciseCount()
        {
            return Exercises.Count();
        }

        /// <summary>
        /// Flag indictating whether this chapter is expanded in the ChaptersView
        /// </summary>
        public bool IsViewExpanded
        {
            get => _isViewExpanded;
            set => _isViewExpanded = value;
        }

        /// <summary>
        /// Flag indictating whether the Model Games list is expanded in the ChaptersView
        /// </summary>
        public bool IsModelGamesListExpanded
        {
            get => _isModelGamesListExpanded;
            set => _isModelGamesListExpanded = value;
        }

        /// <summary>
        /// Flag indictating whether the Model Games list is expanded in the ChaptersView
        /// </summary>
        public bool IsExercisesListExpanded
        {
            get => _isExercisesListExpanded;
            set => _isExercisesListExpanded = value;
        }

        /// <summary>
        /// Returns true if the chapter has at least one Model Game
        /// </summary>
        public bool HasAnyModelGame
        {
            get
            {
                return ModelGames.Count > 0;
            }
        }

        /// <summary>
        /// Returns true if the chapter has at least one Exercise
        /// </summary>
        public bool HasAnyExercise
        {
            get
            {
                return Exercises.Count > 0;
            }
        }

        /// <summary>
        /// Returns the color of the side to move first in the exercise.
        /// </summary>
        /// <param name="exerciseIndex"></param>
        /// <returns></returns>
        public PieceColor GetSideToSolveExercise(int? exerciseIndex = null)
        {
            int index;

            if (exerciseIndex == null)
            {
                index = _activeExerciseIndex;
            }
            else
            {
                index = exerciseIndex.Value;
            }

            if (index >= 0 && index < Exercises.Count)
            {
                return Exercises[index].Tree.Nodes[0].ColorToMove;
            }
            else
            {
                return PieceColor.None;
            }
        }

        /// <summary>
        /// Adds a VariationTree to the list of Model Games
        /// </summary>
        /// <param name="game"></param>
        public Article AddModelGame(VariationTree game)
        {
            lock (_lockModelGames)
            {
                Article article = new Article(game);
                ModelGames.Add(article);
                return article;
            }
        }

        /// <summary>
        /// Adds an Article to the list of Model Games
        /// </summary>
        /// <param name="game"></param>
        public Article AddModelGame(Article article)
        {
            lock (_lockModelGames)
            {
                ModelGames.Add(article);
                return article;
            }
        }

        /// <summary>
        /// Checks if the current active article index is in the valid range.
        /// If not, returns the corrected value;
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public int AdjustActiveArticleIndex(GameData.ContentType contentType)
        {
            int index = -1;

            switch (contentType)
            {
                case GameData.ContentType.MODEL_GAME:
                    index = VerifyGameIndex(ActiveModelGameIndex);
                    break;
                case GameData.ContentType.EXERCISE:
                    index = VerifyExerciseIndex(ActiveExerciseIndex);
                    break;
            }

            return index;
        }

        /// <summary>
        /// Returns adjusted Game index if it is ivalid.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int VerifyGameIndex(int index)
        {
            if (ModelGames.Count == 0)
            {
                index = -1;
            }
            else
            {
                if (index < 0)
                {
                    index = 0;
                }
                else if (index >= ModelGames.Count)
                {
                    index = ModelGames.Count - 1;
                }
            }

            return index;
        }

        /// <summary>
        /// Returns adjusted Exercise index if it is ivalid.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int VerifyExerciseIndex(int index)
        {
            if (Exercises.Count == 0)
            {
                index = -1;
            }
            else
            {
                if (index < 0)
                {
                    index = 0;
                }
                else if (index >= Exercises.Count)
                {
                    index = Exercises.Count - 1;
                }
            }

            return index;
        }

        /// <summary>
        /// Inserts Game Article at a requested index.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="index"></param>
        public void InsertModelGame(Article article, int index)
        {
            try
            {
                if (index >= 0 && index < ModelGames.Count)
                {
                    ModelGames.Insert(index, article);
                }
                else
                {
                    ModelGames.Add(article);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("InsertModelGame()", ex);
            }
        }

        /// <summary>
        /// Adds a VariationTree to the list of Exercises
        /// </summary>
        /// <param name="game"></param>
        public Article AddExercise(VariationTree game)
        {
            Article article = new Article(game);
            Exercises.Add(article);

            return article;
        }

        /// <summary>
        /// Adds an Artticle to the list of Exercises
        /// </summary>
        /// <param name="game"></param>
        public Article AddExercise(Article article)
        {
            Exercises.Add(article);
            return article;
        }

        /// <summary>
        /// Inserts Exercise at a requested index.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="index"></param>
        public void InsertExercise(Article article, int index)
        {
            try
            {
                if (index >= 0 && index < Exercises.Count)
                {
                    Exercises.Insert(index, article);
                }
                else
                {
                    Exercises.Add(article);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("InsertExercise()", ex);
            }
        }

        /// <summary>
        /// Inserts at an article at the specified preferred position.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="index">Index at which the Artcle was actually inserted</param>
        /// <returns></returns>
        public int InsertArticle(Article article, int index)
        {
            GameData.ContentType contentType = article.ContentType;
            if (contentType == GameData.ContentType.MODEL_GAME)
            {
                if (index < 0 || index >= ModelGames.Count)
                {
                    index = ModelGames.Count;
                }
                ModelGames.Insert(index, article);
            }
            else if (contentType == GameData.ContentType.EXERCISE)
            {
                if (index < 0 || index >= Exercises.Count)
                {
                    index = Exercises.Count;
                }
                Exercises.Insert(index, article);
            }

            return index;
        }

#if false
        /// <summary>
        /// Adds a new game to this chapter.
        /// The caller must handle errors if returned index is -1.
        /// </summary>
        /// <param name="gm"></param>
        public int AddArticle(GameData gm, GameData.ContentType typ, out string errorText, GameData.ContentType targetcontentType = GameData.ContentType.GENERIC)
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
                        StudyTree = article;
                        break;
                    case GameData.ContentType.INTRO:
                        Intro = article;
                        break;
                    case GameData.ContentType.MODEL_GAME:
                        if (targetcontentType == GameData.ContentType.GENERIC || targetcontentType == GameData.ContentType.MODEL_GAME)
                        {
                            ModelGames.Add(article);
                            index = ModelGames.Count - 1;
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
                            Exercises.Add(article);
                            index = Exercises.Count - 1;
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

#endif
    }
}

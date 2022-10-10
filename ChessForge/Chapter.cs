using ChessPosition.GameTree;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChessPosition.GameTree.GameMetadata;

namespace ChessForge
{
    /// <summary>
    /// A chapter within a Workbook.
    /// A chapter comprises one or more VariationTrees. 
    /// </summary>
    public class Chapter
    {
        // number of this chapter
        private int _id;

        // title of this chapter
        private string _title;

        // VariationTree to be used when this chapter becomes active.
        private VariationTree _activeTree;

        // whether the chapter is expanded in the ChaptersView
        private bool _isViewExpanded;

        // whether the Model Games list is expanded in the ChaptersView
        private bool _isModelGamesListExpanded;

        // whether the Exercises list is expanded in the ChaptersView
        private bool _isExercisesListExpanded;

        /// <summary>
        /// Returns Tree "active" in this chapter.
        /// If none set, StudyTree is returned as default.
        /// </summary>
        public VariationTree ActiveVariationTree
        {
            get
            {
                if (_activeTree != null)
                {
                    return _activeTree;
                }
                else
                {
                    return StudyTree;
                }
            }
        }

        /// <summary>
        /// Sets the ActiveVariationTree based on the passed type and index.
        /// </summary>
        /// <param name="gameType"></param>
        /// <param name="gameIndex"></param>
        public void SetActiveVariationTree(GameMetadata.GameType gameType, int gameIndex = 0)
        {
            switch (gameType)
            {
                case GameType.STUDY_TREE:
                    _activeTree = StudyTree;
                    break;
                case GameType.MODEL_GAME:
                    if (gameIndex >= 0 && gameIndex < ModelGames.Count)
                    {
                        _activeTree = ModelGames[gameIndex];
                    }
                    break;
                case GameType.EXERCISE:
                    if (gameIndex >= 0 && gameIndex < Exercises.Count)
                    {
                        _activeTree = Exercises[gameIndex];
                    }
                    break;
            }
        }

        /// <summary>
        /// Number of this chapter.
        /// </summary>
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        /// <summary>
        /// The Title of this chapter.
        /// Returns default text if empty.
        /// </summary>
        public string Title
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_title))
                {
                    return "Chapter " + Id.ToString();
                }
                else
                {
                    return _title;
                }
            }
            set => _title = value;
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
        /// The analysis tree of the chapter. There is exactly one
        /// analysis tree in a chapter.
        /// </summary>
        public VariationTree StudyTree = new VariationTree(GameMetadata.GameType.STUDY_TREE);

        /// <summary>
        /// The list of Model Games
        /// </summary>
        public List<VariationTree> ModelGames = new List<VariationTree>();

        /// <summary>
        /// The list of combinations.
        /// </summary>
        public List<VariationTree> Exercises = new List<VariationTree>();

        /// <summary>
        /// Adds a new game to this chapter.
        /// The caller must handle exceptions.
        /// </summary>
        /// <param name="gm"></param>
        public void AddGame(GameMetadata gm, GameMetadata.GameType typ = GameType.INVALID)
        {
            VariationTree tree = new VariationTree(typ);
            PgnGameParser pp = new PgnGameParser(gm.GameText, tree);
            tree.Header = gm.Header;

            if (typ == GameType.INVALID)
            {
                typ = gm.GetContentType();
            }
            tree.ContentType = typ;

            switch (typ)
            {
                case GameMetadata.GameType.STUDY_TREE:
                    StudyTree = tree;
                    break;
                case GameMetadata.GameType.MODEL_GAME:
                    ModelGames.Add(tree);
                    break;
                case GameMetadata.GameType.EXERCISE:
                    Exercises.Add(tree);
                    break;
            }
        }
    }
}

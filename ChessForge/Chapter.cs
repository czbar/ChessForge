using ChessPosition.GameTree;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        // index of the currently shown game in the Model Games list
        private int _activeModelGameIndex = -1;

        // index of the currently shown exercise in the Exercises list
        private int _activeExerciseIndex = -1;

        // whether the chapter is expanded in the ChaptersView
        private bool _isViewExpanded;

        // whether the Model Games list is expanded in the ChaptersView
        private bool _isModelGamesListExpanded;

        // whether the Exercises list is expanded in the ChaptersView
        private bool _isExercisesListExpanded;

        /// <summary>
        // Index of the currently shown Game in the Model Games list
        /// </summary>
        public int ActiveModelGameIndex
        {
            get => _activeModelGameIndex; 
            set => _activeModelGameIndex = value;
        }


        /// <summary>
        // Index of the currently shown Exercise in the Exercises list
        /// </summary>
        public int ActiveExerciseIndex
        {
            get => _activeExerciseIndex;
            set => _activeExerciseIndex = value;
        }


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
        public void SetActiveVariationTree(GameData.ContentType gameType, int gameIndex = 0)
        {
            switch (gameType)
            {
                case GameData.ContentType.STUDY_TREE:
                    _activeTree = StudyTree;
                    break;
                case GameData.ContentType.MODEL_GAME:
                    if (gameIndex >= 0 && gameIndex < ModelGames.Count)
                    {
                        _activeTree = ModelGames[gameIndex];
                    }
                    break;
                case GameData.ContentType.EXERCISE:
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
        /// Adds a VariationTree to the list of Model Games
        /// </summary>
        /// <param name="game"></param>
        public void AddModelGame(VariationTree game)
        {
            ModelGames.Add(game);
        }

        /// <summary>
        /// Adds a VariationTree to the list of Exercises
        /// </summary>
        /// <param name="game"></param>
        public void AddExercise(VariationTree game)
        {
            Exercises.Add(game);
        }

        /// <summary>
        /// The analysis tree of the chapter. There is exactly one
        /// analysis tree in a chapter.
        /// </summary>
        public VariationTree StudyTree = new VariationTree(GameData.ContentType.STUDY_TREE);

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
        public void AddGame(GameData gm, GameData.ContentType typ = GameData.ContentType.GENERIC)
        {
            VariationTree tree = new VariationTree(typ);
            PgnGameParser pp = new PgnGameParser(gm.GameText, tree, gm.Header.GetFenString());
            tree.Header = gm.Header;

            if (typ == GameData.ContentType.GENERIC)
            {
                typ = gm.GetContentType();
            }
            tree.ContentType = typ;

            switch (typ)
            {
                case GameData.ContentType.STUDY_TREE:
                    StudyTree = tree;
                    break;
                case GameData.ContentType.MODEL_GAME:
                    ModelGames.Add(tree);
                    break;
                case GameData.ContentType.EXERCISE:
                    Exercises.Add(tree);
                    break;
            }
        }
    }
}

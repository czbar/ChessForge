using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace ChessForge
{
    /// <summary>
    /// A chapter within a Workbook.
    /// A chapter comprises one or more VariationTrees. 
    /// </summary>
    public class Chapter
    {
        /// <summary>
        /// The Study Tree of the chapter. There is exactly one
        /// Study Tree in a chapter.
        /// </summary>
        public GameUnit StudyTree = new GameUnit(GameData.ContentType.STUDY_TREE);

        /// <summary>
        /// The list of Model Games Trees
        /// </summary>
        public List<GameUnit> ModelGames = new List<GameUnit>();

        /// <summary>
        /// The list of Exercises Tress.
        /// </summary>
        public List<GameUnit> Exercises = new List<GameUnit>();

        // number of this chapter
        private int _id;

        // title of this chapter
        private string _title;

        // VariationTree to be used when this chapter becomes active.
        private VariationTree _activeTree;

        // GameUnit to be used when this chapter becomes active.
        private GameUnit _activeGameUnit;

        // index of the currently shown game in the Model Games list
        private int _activeModelGameIndex = -1;

        // index of the currently shown exercise in the Exercises list
        private int _activeExerciseIndex = -1;

        // whether the chapter is expanded in the ChaptersView
        private bool _isViewExpanded = true;

        // whether the Model Games list is expanded in the ChaptersView
        private bool _isModelGamesListExpanded;

        // whether the Exercises list is expanded in the ChaptersView
        private bool _isExercisesListExpanded;

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
        /// Returns Unit "active" in this chapter.
        /// </summary>
        public GameUnit ActiveGameUnit
        {
            get
            {
                return _activeGameUnit;
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
                    _activeGameUnit = StudyTree;
                    break;
                case GameData.ContentType.MODEL_GAME:
                    if (gameIndex >= 0 && gameIndex < ModelGames.Count)
                    {
                        _activeTree = ModelGames[gameIndex].Tree;
                        _activeGameUnit = ModelGames[gameIndex];
                    }
                    break;
                case GameData.ContentType.EXERCISE:
                    if (gameIndex >= 0 && gameIndex < Exercises.Count)
                    {
                        _activeTree = Exercises[gameIndex].Tree;
                        _activeGameUnit = Exercises[gameIndex];
                    }
                    break;
                default:
                    _activeTree = null;
                    _activeGameUnit = null;
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
                return "Chapter " + Id.ToString();
            }
        }

        /// <summary>
        /// Sets the title of the Chapter.
        /// </summary>
        /// <param name="title"></param>
        public void SetTitle(string title)
        {
            _title = title;
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
        public void AddModelGame(VariationTree game)
        {
            GameUnit unit = new GameUnit(game);
            ModelGames.Add(unit);
        }

        /// <summary>
        /// Adds a VariationTree to the list of Exercises
        /// </summary>
        /// <param name="game"></param>
        public void AddExercise(VariationTree game)
        {
            GameUnit unit = new GameUnit(game);
            Exercises.Add(unit);
        }

        /// <summary>
        /// Adds a new game to this chapter.
        /// The caller must handle exceptions.
        /// </summary>
        /// <param name="gm"></param>
        public int AddGame(GameData gm, GameData.ContentType typ = GameData.ContentType.GENERIC)
        {
            int index = -1;

            GameUnit unit = new GameUnit(typ);
            //VariationTree tree = new VariationTree(typ);
            PgnGameParser pp = new PgnGameParser(gm.GameText, unit.Tree, gm.Header.GetFenString());
            unit.Tree.Header = gm.Header;

            if (typ == GameData.ContentType.GENERIC)
            {
                typ = gm.GetContentType();
            }
            unit.Tree.ContentType = typ;

            switch (typ)
            {
                case GameData.ContentType.STUDY_TREE:
                    StudyTree = unit;
                    break;
                case GameData.ContentType.MODEL_GAME:
                    ModelGames.Add(unit);
                    index = ModelGames.Count - 1;
                    break;
                case GameData.ContentType.EXERCISE:
                    TreeUtils.RestartMoveNumbering(unit.Tree);
                    Exercises.Add(unit);
                    index = Exercises.Count - 1;
                    break;
            }

            return index;
        }
    }
}

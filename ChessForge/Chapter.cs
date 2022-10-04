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
        private int _number;

        // title of this chapter
        private string _title;

        /// <summary>
        /// Number of this chapter.
        /// </summary>
        public int Number
        {
            get => _number; 
            set => _number = value;
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
                    return "Untitled Chapter";
                }
                else
                {
                    return _title;
                }
            }
            set => _title = value;
        }

        /// <summary>
        /// The analysis tree of the chapter. There is exactly one
        /// analysis tree in a chapter.
        /// </summary>
        public VariationTree StudyTree = new VariationTree();

        /// <summary>
        /// The list of Model Games
        /// </summary>
        public List<VariationTree> ModelGames = new List<VariationTree>();

        /// <summary>
        /// The list of combinations.
        /// </summary>
        public List<VariationTree> Exercises = new List<VariationTree>();

        /// <summary>
        /// Adds new game to this chapter.
        /// </summary>
        /// <param name="gm"></param>
        public void AddGame(GameMetadata gm)
        {
            VariationTree tree = new VariationTree();
            PgnGameParser pp = new PgnGameParser(gm.GameText, tree);
            GameMetadata.GameType typ = gm.GetContentType();
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

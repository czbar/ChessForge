using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

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
            DELETE_GAME,
            DELETE_EXERCISE
        }

        /// <summary>
        /// Type of this operation.
        /// </summary>
        private WorkbookOperationType _opType;

        /// <summary>
        /// Index of the chapter on which the operation occured.
        /// </summary>
        private int _chapterIndex;

        /// <summary>
        /// Index of the unit (game or exercise) on which the operation
        /// was performed.
        /// </summary>
        private int _gameUnitIndex;

        /// <summary>
        /// Saved Chapter do that it can be restored after deletion.
        /// </summary>
        private Chapter _chapter;

        /// <summary>
        /// Saved VariationTree so that a Game or Exercise can be retored.
        /// </summary>
        private VariationTree _tree;

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
        /// Constructor for DELETE_GAME or DELETE_EXERCISE.
        /// </summary>
        public WorkbookOperation(WorkbookOperationType tp, Chapter ch, VariationTree tree, int gameIndex) : base()
        {
            _opType = tp;
            _chapter = ch;
            _tree = tree;
            _gameUnitIndex = gameIndex;
        }
    }
}

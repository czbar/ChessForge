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
        /// Game Unit to operate on.
        /// </summary>
        public GameUnit GameUnit { get { return _gameUnit; } }

        /// <summary>
        /// Index of the Model Game or Exercise in the Chapter's list.
        /// </summary>
        public int GameUnitIndex { get { return _gameUnitIndex; } }

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
        /// Saved GameUnit so that a Game or Exercise can be restored.
        /// </summary>
        private GameUnit _gameUnit;

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
        public WorkbookOperation(WorkbookOperationType tp, Chapter ch, GameUnit unit, int gameIndex) : base()
        {
            _opType = tp;
            _chapter = ch;
            _gameUnit = unit;
            _gameUnitIndex = gameIndex;
        }
    }
}

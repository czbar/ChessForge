using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChessPosition;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates the state of the views of the chapter so that it can be saved
    /// and reflected upon re-opening of the workbook.
    /// </summary>
    public class ChapterViewState
    {
        // Key strings for key/value configuration items
        private readonly string IS_EXPANDED = "IsExpanded";
        private readonly string IS_GAME_LIST_EXPANDED = "IsGameListExpanded";
        private readonly string IS_EXERCISE_LIST_EXPANDED = "IsExerciseListExpanded";
        private readonly string CHAPTERS_VIEW_SELECTED_ARTICLE_INDEX = "ChaptersViewSelectedArticleIndex";
        private readonly string CHAPTERS_VIEW_SELECTED_ARTICLE_TYPE = "ChaptersViewSelectedArticleType";

        private readonly string ACTIVE_GAME_INDEX = "ActiveGameIndex";
        private readonly string ACTIVE_EXERCISE_INDEX = "ActiveExerciseIndex";

        // Chapter object represented by this object
        private Chapter _chapter;

        // Flags if this is the Active Chapter
        private bool _isActiveChapter;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="isActive"></param>
        public ChapterViewState(Chapter chapter, bool isActive)
        {
            _chapter = chapter;
            _isActiveChapter = isActive;
        }

        /// <summary>
        /// Whether the chapter's paragraph in the Chapters view is expanded.
        /// </summary>
        public bool IsExpanded { get; set; }

        /// <summary>
        /// Whether the games list in the Chapters view is expanded.
        /// </summary>
        public bool IsGameListExpanded { get; set; }

        /// <summary>
        /// Whether the exercise list in the Chapters view is expanded.
        /// </summary>
        public bool IsExerciseListExpanded { get; set; }

        /// <summary>
        /// The type of the active article.
        /// </summary>
        public GameData.ContentType ActiveArticleType { get; set; }

        /// <summary>
        /// Active article index
        /// </summary>
        public int ArticleIndex { get; set; }

        /// <summary>
        /// Builds the text to put into the view configuration file for the chapter.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(TextUtils.BuildKeyValueLine(IS_EXPANDED, _chapter.IsViewExpanded));
            sb.AppendLine(TextUtils.BuildKeyValueLine(IS_GAME_LIST_EXPANDED, _chapter.IsModelGamesListExpanded));
            sb.AppendLine(TextUtils.BuildKeyValueLine(IS_EXERCISE_LIST_EXPANDED, _chapter.IsExercisesListExpanded));

            sb.AppendLine(TextUtils.BuildKeyValueLine(ACTIVE_GAME_INDEX, _chapter.ActiveModelGameIndex));
            sb.AppendLine(TextUtils.BuildKeyValueLine(ACTIVE_EXERCISE_INDEX, _chapter.ActiveExerciseIndex));


            if (_isActiveChapter)
            {
                WorkbookManager.ItemType itemType = AppState.MainWin.ChaptersView.LastClickedItemType;
                sb.AppendLine(TextUtils.BuildKeyValueLine(CHAPTERS_VIEW_SELECTED_ARTICLE_TYPE, itemType));
                if (itemType == WorkbookManager.ItemType.MODEL_GAME)
                {
                    sb.AppendLine(TextUtils.BuildKeyValueLine(CHAPTERS_VIEW_SELECTED_ARTICLE_INDEX, _chapter.ActiveModelGameIndex));
                }
                else if (itemType == WorkbookManager.ItemType.MODEL_GAME)
                {
                    sb.AppendLine(TextUtils.BuildKeyValueLine(CHAPTERS_VIEW_SELECTED_ARTICLE_INDEX, _chapter.ActiveExerciseIndex));
                }
            }

            return sb.ToString();
        }
    }
}

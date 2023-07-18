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
        private const string IS_EXPANDED = "IsExpanded";
        private const string IS_GAME_LIST_EXPANDED = "IsGameListExpanded";
        private const string IS_EXERCISE_LIST_EXPANDED = "IsExerciseListExpanded";
        private const string CHAPTERS_VIEW_SELECTED_ARTICLE_INDEX = "ChaptersViewSelectedArticleIndex";
        private const string CHAPTERS_VIEW_SELECTED_ARTICLE_TYPE = "ChaptersViewSelectedArticleType";

        private const string ACTIVE_GAME_INDEX = "ActiveGameIndex";
        private const string ACTIVE_EXERCISE_INDEX = "ActiveExerciseIndex";

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
        /// Index of the selected article in its list.
        /// </summary>
        public int ChaptersViewSelectedArticleIndex
        {
            get => _chaptersViewSelectedArticleIndex;
            set => _chaptersViewSelectedArticleIndex = value;
        }

        /// <summary>
        /// Type of the selected article
        /// </summary>
        public WorkbookManager.ItemType ChaptersViewSelectedArticleType
        {
            get => _chaptersViewSelectedArticleType;
            set => _chaptersViewSelectedArticleType = value;
        }

        /// <summary>
        /// Index of the game active in the chapter
        /// </summary>
        public int ActiveGameIndex
        {
            get => _activeGameIndex;
            set => _activeGameIndex = value;
        }

        /// <summary>
        /// Index of the exercise active in the chapter
        /// </summary>
        public int ActiveExerciseIndex
        {
            get => _activeExerciseIndex;
            set => _activeExerciseIndex = value;
        }

        // typ eof the selected article
        private WorkbookManager.ItemType _chaptersViewSelectedArticleType;

        // index of the active game
        private int _activeGameIndex = -1;

        // index of the active exercise
        private int _activeExerciseIndex = -1;

        // index of the selected article 
        private int _chaptersViewSelectedArticleIndex = -1;

        /// <summary>
        /// Process a chapter specific line in the config file
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void ProcessConfigLine(string key, string value)
        {
            switch (key)
            {
                case IS_EXPANDED:
                    IsExpanded = GetBool(value);
                    break;
                case IS_GAME_LIST_EXPANDED:
                    IsGameListExpanded = GetBool(value);
                    break;
                case IS_EXERCISE_LIST_EXPANDED:
                    IsExerciseListExpanded = GetBool(value);
                    break;
                case ACTIVE_GAME_INDEX:
                    int.TryParse(value, out _activeGameIndex);
                    break;
                case ACTIVE_EXERCISE_INDEX:
                    int.TryParse(value, out _activeExerciseIndex);
                    break;
                case CHAPTERS_VIEW_SELECTED_ARTICLE_INDEX:
                    int.TryParse(value, out _chaptersViewSelectedArticleIndex);
                    break;
                case CHAPTERS_VIEW_SELECTED_ARTICLE_TYPE:
                    Enum.TryParse(value, out _chaptersViewSelectedArticleType);
                    break;
            }
        }

        /// <summary>
        /// Translates string into a boolean value.
        /// The string must begin with "1" to be considered as representing "true".
        /// Otherwise it is considered to represent false.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool GetBool(string value)
        {
            return value != null && value.StartsWith("1");
        }

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

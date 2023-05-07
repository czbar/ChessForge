using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Monitors and manages moving between tabs and articles.
    /// </summary>
    public class WorkbookLocationNavigator
    {
        // The list of location history in this workbook session
        private static List<WorkbookLocation> _locations = new List<WorkbookLocation>();

        // index of the current location in the _locations list
        private static int _currentLocationIndex = -1;

        /// <summary>
        /// Returns true if there is no newer location after the current one
        /// </summary>
        public static bool IsLastLocation
        {
            get { return _currentLocationIndex == _locations.Count - 1; }
        }

        /// <summary>
        /// Returns true if there is no older location before the current one
        /// </summary>
        public static bool IsFirstLocation
        {
            get { return _currentLocationIndex <= 0; }
        }

        /// <summary>
        /// Opens a view for the specified article.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="contentType"></param>
        /// <param name="articleIndex"></param>
        public static void GotoArticle(int chapterIndex, GameData.ContentType contentType, int articleIndex)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.SetActiveChapterTreeByIndex(chapterIndex, contentType, articleIndex);
                if (contentType == GameData.ContentType.MODEL_GAME)
                {
                    AppState.MainWin.SelectModelGame(articleIndex, true);
                }
                else if (contentType == GameData.ContentType.EXERCISE)
                {
                    AppState.MainWin.SelectExercise(articleIndex, true);
                }

                SaveNewLocation(chapter, contentType, articleIndex);
            }
            catch { }
        }

        /// <summary>
        /// Moves to the next location in the list if there is one.
        /// </summary>
        public static void MoveToNextLocation()
        {
        }

        /// <summary>
        /// Moves to the previous location if there is one.
        /// </summary>
        public static void MoveToPreviousLocation()
        {
        }

        /// <summary>
        /// Creates a new location object and saves it to the list of locations.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="contentType"></param>
        /// <param name="articleIndex"></param>
        private static void SaveNewLocation(Chapter chapter, GameData.ContentType contentType, int articleIndex = -1)
        {
            WorkbookManager.TabViewType tab = WorkbookManager.TabViewType.NONE;

            string articleGuid = null;

            switch (contentType)
            {
                case GameData.ContentType.STUDY_TREE:
                    tab = WorkbookManager.TabViewType.STUDY;
                    break;
                case GameData.ContentType.MODEL_GAME:
                    tab = WorkbookManager.TabViewType.MODEL_GAME;
                    articleGuid = chapter.ModelGames[articleIndex].Guid;
                    break;
                case GameData.ContentType.EXERCISE:
                    tab = WorkbookManager.TabViewType.EXERCISE;
                    articleGuid = chapter.Exercises[articleIndex].Guid;
                    break;
            }

            WorkbookLocation location = new WorkbookLocation(chapter.Guid, tab, articleGuid);
            AppendLocation(location);
        }

        /// <summary>
        /// Adds a new location after the current location. 
        /// Removes all the later locations.
        /// </summary>
        /// <param name="location"></param>
        private static void AppendLocation(WorkbookLocation location)
        {
            RemoveForwardLocations();
            _locations.Add(location);
            _currentLocationIndex = _locations.Count - 1;
        }

        /// <summary>
        /// Go to the location encapsulated in the passed object.
        /// </summary>
        /// <param name="location"></param>
        private static void GotoLocation(WorkbookLocation location)
        {
            // if the tab type is a Tree holding tab identify chapter and article by guid
            Chapter chapter = null;
            Article article = null;
            int articleIndex = -1;
            GameData.ContentType contentType = GameData.ContentType.NONE;

            if (AppState.IsVariationTreeTabType)
            {
                chapter = WorkbookManager.SessionWorkbook.GetChapterByGuid(location.ChapterGuid, out int chapterIndex);
                if (chapter != null)
                {
                    if (location.ViewType == WorkbookManager.TabViewType.STUDY)
                    {
                        article = chapter.StudyTree;
                        contentType = GameData.ContentType.STUDY_TREE;
                    }
                    else if (location.ViewType == WorkbookManager.TabViewType.MODEL_GAME)
                    {
                        article = chapter.GetModelGameByGuid(location.ArticleGuid, out articleIndex);
                        contentType = GameData.ContentType.MODEL_GAME;
                    }
                    else if (location.ViewType == WorkbookManager.TabViewType.EXERCISE)
                    {
                        article = chapter.GetExerciseByGuid(location.ArticleGuid, out articleIndex);
                        contentType = GameData.ContentType.EXERCISE;
                    }
                    GotoArticle(chapterIndex, contentType, articleIndex);
                }
            }
        }

        /// <summary>
        /// Removes all locations after the current one
        /// </summary>
        private static void RemoveForwardLocations()
        {
            if (_currentLocationIndex < _locations.Count - 1)
            {
                _locations.RemoveRange(_currentLocationIndex + 1, (_locations.Count - 1) - _currentLocationIndex);
            }
        }
    }
}

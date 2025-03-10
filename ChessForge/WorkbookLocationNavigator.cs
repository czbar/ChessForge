using GameTree;
using ChessPosition;
using System;
using System.Collections.Generic;

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

        // whether the location changes are being tracked
        private static bool _isNavigationTrackingOn = true;

        /// <summary>
        /// Clears the locations cache
        /// </summary>
        public static void Reset()
        {
            _currentLocationIndex = -1;
            _locations.Clear();
        }

        /// <summary>
        /// Whether the location changes are being tracked.
        /// For example, we don't want to track them if we are navigating in response
        /// to the user asking for previous/next location. Doing so would cause 
        /// dupes/confusion in navigation tracking.
        /// </summary>
        public static bool IsNavigationTrackingOn
        {
            get => _isNavigationTrackingOn;
            set => _isNavigationTrackingOn = value;
        }

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
        /// Opens a view for the specified chapter and specified content (tab) type.
        /// The active article of the requested type will be open if exists.
        /// If the type is Intro and there the Intro tab is not visible in the target
        /// chapter, the Study view will open.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="tabType"></param>
        public static void GotoArticle(int chapterIndex, TabViewType tabType)
        {
            Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(chapterIndex);
            if (chapter != null)
            {
                if (tabType == TabViewType.INTRO && !chapter.ShowIntro)
                {
                    tabType = TabViewType.STUDY;
                }

                WorkbookManager.SessionWorkbook.ActiveChapter = chapter;
                GameData.ContentType contentType = GameData.ContentType.NONE;
                int articleIndex = -1;
                switch (tabType)
                {
                    case TabViewType.STUDY:
                        contentType = GameData.ContentType.STUDY_TREE;
                        WorkbookManager.SessionWorkbook.ActiveChapter.SetActiveVariationTree(GameData.ContentType.STUDY_TREE);
                        AppState.MainWin.SetupGuiForActiveStudyTree(true);
                        break;
                    case TabViewType.INTRO:
                        // Intro is a special case where we need to save the current one
                        AppState.MainWin.SaveIntro();
                        contentType = GameData.ContentType.INTRO;
                        AppState.MainWin.SetupGuiForIntro(true);
                        break;
                    case TabViewType.MODEL_GAME:
                        contentType = GameData.ContentType.MODEL_GAME;
                        articleIndex = chapter.ActiveModelGameIndex;
                        AppState.MainWin.SelectModelGame(articleIndex, true);
                        break;
                    case TabViewType.EXERCISE:
                        contentType = GameData.ContentType.EXERCISE;
                        articleIndex = chapter.ActiveExerciseIndex;
                        AppState.MainWin.SelectExercise(articleIndex, true);
                        break;
                }

                SaveNewLocation(chapter, contentType, articleIndex);
                AppState.EnableNavigationArrows();
            }
        }

        /// <summary>
        /// Opens a view for the specified article.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="contentType"></param>
        /// <param name="articleIndex"></param>
        public static void GotoArticle(int chapterIndex, GameData.ContentType contentType, int articleIndex, bool save = true)
        {
            try
            {
                Chapter chapter = WorkbookManager.SessionWorkbook.SetActiveChapterTreeByIndex(chapterIndex, contentType, articleIndex, save);
                if (chapter != null)
                {
                    if (contentType == GameData.ContentType.MODEL_GAME)
                    {
                        // TODO: should SelectModelGame/Exercise be part of SetActiveChapterTreeByIndex above?
                        AppState.MainWin.SelectModelGame(articleIndex, true);
                    }
                    else if (contentType == GameData.ContentType.EXERCISE)
                    {
                        AppState.MainWin.SelectExercise(articleIndex, true);
                    }
                    else if (contentType == GameData.ContentType.STUDY_TREE)
                    {
                        AppState.MainWin.SetupGuiForActiveStudyTree(true);
                    }
                    else if (contentType == GameData.ContentType.INTRO)
                    {
                        AppState.MainWin.SetupGuiForIntro(true);
                    }
                }

                AppState.EnableNavigationArrows();
            }
            catch { }
        }

        /// <summary>
        /// Moves to the next location in the list if there is one.
        /// </summary>
        public static void MoveToNextLocation()
        {
            try
            {
                GotoLocation(GetNextLocation());
                AppState.EnableNavigationArrows();
            }
            catch { }
        }

        /// <summary>
        /// Moves to the previous location if there is one.
        /// </summary>
        public static void MoveToPreviousLocation()
        {
            try
            {
                GotoLocation(GetPreviousLocation());
                AppState.EnableNavigationArrows();
            }
            catch { }
        }
        /// <summary>
        /// Creates a new location object and saves it to the list of locations.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="contentType"></param>
        /// <param name="articleIndex"></param>
        public static void SaveNewLocation(Chapter chapter, GameData.ContentType contentType, int articleIndex = -1)
        {
            if (_isNavigationTrackingOn)
            {
                try
                {
                    if (chapter != null)
                    {
                        TabViewType tab = TabViewType.NONE;

                        string articleGuid = null;

                        switch (contentType)
                        {
                            case GameData.ContentType.INTRO:
                                tab = TabViewType.INTRO;
                                break;
                            case GameData.ContentType.STUDY_TREE:
                                tab = TabViewType.STUDY;
                                break;
                            case GameData.ContentType.MODEL_GAME:
                                tab = TabViewType.MODEL_GAME;
                                if (articleIndex >= 0)
                                {
                                    articleGuid = chapter.ModelGames[articleIndex].Guid;
                                }
                                break;
                            case GameData.ContentType.EXERCISE:
                                tab = TabViewType.EXERCISE;
                                if (articleIndex >= 0)
                                {
                                    articleGuid = chapter.Exercises[articleIndex].Guid;
                                }
                                break;
                        }

                        WorkbookLocation location = new WorkbookLocation(chapter.Guid, tab, articleGuid, articleIndex);
                        VerifyNewLocation(location);
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Overloaded for convenience.
        /// </summary>
        /// <param name="tabType"></param>
        public static void SaveNewLocation(TabViewType tabType)
        {
            if (_isNavigationTrackingOn)
            {
                WorkbookLocation location = new WorkbookLocation(null, tabType, null, -1);
                VerifyNewLocation(location);
            }
        }

        /// <summary>
        /// Checks if the location needs to be added
        /// and if so appends it.
        /// </summary>
        /// <param name="location"></param>
        private static void VerifyNewLocation(WorkbookLocation location)
        {
            // if different than last location, append to the list
            WorkbookLocation lastLocation = null;

            if (_currentLocationIndex >= 0)
            {
                lastLocation = _locations[_currentLocationIndex];
            }

            if (lastLocation == null || !AreLocationsIdentical(lastLocation, location))
            {
                AppendLocation(location);
            }

            AppState.EnableNavigationArrows();
        }

        /// <summary>
        /// Compares 2 locations and returns true if the are
        /// identical.
        /// </summary>
        /// <returns></returns>
        private static bool AreLocationsIdentical(WorkbookLocation loc1, WorkbookLocation loc2)
        {
            bool identical = false;

            if (loc1 != null && loc2 != null
                && loc1.ChapterGuid == loc2.ChapterGuid
                && loc1.ViewType == loc2.ViewType
                && loc1.ArticleGuid == loc2.ArticleGuid
                && (loc1.ArticleIndex == loc2.ArticleIndex || loc1.ViewType == TabViewType.STUDY))
            // the last condition is just in case we use a different (irrelevant!) article index for the Study tree.
            {
                identical = true;
            }

            return identical;
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
            if (location == null)
            {
                return;
            }

            _isNavigationTrackingOn = false;

            try
            {
                if (location.ViewType == TabViewType.CHAPTERS)
                {
                    AppState.MainWin.UiTabChapters.Focus();
                }
                else
                {
                    // if the tab type is a Tree holding tab identify the chapter by guid
                    Chapter chapter = WorkbookManager.SessionWorkbook.GetChapterByGuid(location.ChapterGuid, out int chapterIndex);
                    if (chapter != null)
                    {
                        GameData.ContentType contentType = GameData.ContentType.NONE;
                        switch (location.ViewType)
                        {
                            case TabViewType.INTRO:
                                contentType = GameData.ContentType.INTRO;
                                GotoArticle(chapterIndex, contentType, -1, false);
                                break;
                            case TabViewType.STUDY:
                                contentType = GameData.ContentType.STUDY_TREE;
                                GotoArticle(chapterIndex, contentType, -1, false);
                                break;
                            case TabViewType.MODEL_GAME:
                                contentType = GameData.ContentType.MODEL_GAME;
                                GotoArticle(chapterIndex, contentType, location.ArticleIndex, false);
                                break;
                            case TabViewType.EXERCISE:
                                contentType = GameData.ContentType.EXERCISE;
                                GotoArticle(chapterIndex, contentType, location.ArticleIndex, false);
                                break;
                        }
                    }
                }
            }
            catch
            {
            }

            _isNavigationTrackingOn = true;
        }

        /// <summary>
        /// Identifies the next valid location in the list.
        /// Returns null if not found.
        /// </summary>
        /// <returns></returns>
        private static WorkbookLocation GetNextLocation()
        {
            WorkbookLocation location = null;

            List<WorkbookLocation> locationsToDelete = new List<WorkbookLocation>();
            if (_currentLocationIndex < _locations.Count - 1)
            {
                while (_currentLocationIndex < _locations.Count - 1)
                {
                    _currentLocationIndex++;
                    WorkbookLocation nextLoc = _locations[_currentLocationIndex];
                    if (IsLocationValid(nextLoc))
                    {
                        location = nextLoc;
                        break;
                    }
                    else
                    {
                        locationsToDelete.Add(nextLoc);
                    }
                }
            }

            foreach (WorkbookLocation loc in locationsToDelete)
            {
                _locations.Remove(loc);
                _currentLocationIndex--;
            }

            return location;
        }

        /// <summary>
        /// Identifies the previous valid location in the list.
        /// Returns null if not found.
        /// </summary>
        /// <returns></returns>
        private static WorkbookLocation GetPreviousLocation()
        {
            WorkbookLocation location = null;

            List<WorkbookLocation> locationsToDelete = new List<WorkbookLocation>();
            if (_currentLocationIndex > 0)
            {
                while (_currentLocationIndex > 0)
                {
                    _currentLocationIndex--;
                    WorkbookLocation previousLoc = _locations[_currentLocationIndex];
                    if (IsLocationValid(previousLoc))
                    {
                        location = previousLoc;
                        break;
                    }
                    else
                    {
                        locationsToDelete.Add(previousLoc);
                    }
                }
            }

            foreach (WorkbookLocation loc in locationsToDelete)
            {
                _locations.Remove(loc);
            }

            return location;
        }

        /// <summary>
        /// Checks if the specified location exists.
        /// It may not exists, e.g. after an article was removed
        /// somewhere along the line.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        private static bool IsLocationValid(WorkbookLocation location)
        {
            bool exists = false;

            if (AppState.Workbook != null)
            {
                if (location.ViewType == TabViewType.CHAPTERS
                    || location.ViewType == TabViewType.STUDY
                    || location.ViewType == TabViewType.STUDY
                    || location.ViewType == TabViewType.BOOKMARKS)
                {
                    exists = true;
                }
                else
                {
                    Chapter chapter = AppState.Workbook.GetChapterByGuid(location.ChapterGuid, out _);
                    bool hasArticleId = location.ArticleGuid != null || location.ArticleIndex >= 0;
                    if (chapter != null)
                    {
                        // location without article being specified is valid if no article exists in the view
                        if (location.ViewType == TabViewType.MODEL_GAME)
                        {
                            if (!hasArticleId && chapter.GetModelGameCount() == 0
                                || chapter.GetModelGameByGuid(location.ArticleGuid, out _) != null)
                            {
                                exists = true;
                            }
                        }
                        else if (location.ViewType == TabViewType.EXERCISE)
                        {
                            if (!hasArticleId && chapter.GetExerciseCount() == 0
                                || chapter.GetExerciseByGuid(location.ArticleGuid, out _) != null)
                            {
                                exists = true;
                            }
                        }
                    }
                }
            }

            return exists;
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

using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Reflects the object/action the last click was registered for.
    /// </summary>
    public enum MouseClickAction
    {
        NONE,

        NEXT_CHAPTER,
        PREVIOUS_CHAPTER,

        NEXT_GAME,
        PREVIOUS_GAME,

        NEXT_EXERCISE,
        PREVIOUS_EXERCISE
    }

    /// <summary>
    /// Monitors mouse click so that in case of a series of rapid clicks on the same item,
    /// a special action can be taken, e.g. ignoring the clicks until the series stops.
    /// 
    /// The series startes when this objects receives a click notification for a given
    /// type of onject-action e.g. NEXT_GAME.
    /// The caller performs the required action (e.g. Select the Next Game and sends a notification
    /// to this object. At the next click the caller checks if there is a series in progress
    /// and if so refrains from performing an action and only reports the click.
    /// Eventually, when the series this object will perform the desired action based on the type
    /// of action and the click count.
    /// </summary>
    public class MouseClickMonitor
    {
        // maximum time between clicks after which we decide that the series is over
        private static long MAX_PAUSE_DURATION_TICKS = 400 * TimeSpan.TicksPerMillisecond;

        // last time the click was received, in ticks.
        private static long _lastClickTime = -1;

        // MouseClickAction associated with the last click.
        private static MouseClickAction _lastClickAction = MouseClickAction.NONE;

        // number of the clicks in the current series.
        private static int _clickCount = 0;

        /// <summary>
        /// A click was received. Check if this is the current series
        /// or a new one.
        /// If the series is in progress return true so that the caller does not perform the action.
        /// </summary>
        public static bool RegisterClick(MouseClickAction action)
        {
            bool seriesInProgress = false;

            if (_lastClickAction == MouseClickAction.NONE)
            {
                // this is new series and no series was active
                StartNewSeries(action);
            }
            else if (_lastClickAction == action)
            {
                seriesInProgress = true;
                // the current series continues
                _clickCount++;
                // perform any action if necessary
                PostClickAction();
                // update the last click time
                _lastClickTime = DateTime.Now.Ticks;
            }
            else
            {
                // a new mouse action received: the current series must be closed
                EndCurrentSeries();
                // and a new series must be started
                StartNewSeries(action);
            }

            return seriesInProgress;
        }

        /// <summary>
        /// Handles the timer event.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void CheckClickSeriesStatus(object source, ElapsedEventArgs e)
        {
            // check if the distance since the last click exceeds the allowed duration of a pause in a series
            long diff = DateTime.Now.Ticks - _lastClickTime;
            if (diff > MAX_PAUSE_DURATION_TICKS)
            {
                // we haven't had a recent click so end the series
                EndCurrentSeries();
            }
            else
            {
                AppState.MainWin.Dispatcher.Invoke(() =>
                {
                    if (Mouse.LeftButton == MouseButtonState.Pressed)
                    {
                        RegisterClick(_lastClickAction);
                    }
                });
            }
        }

        /// <summary>
        /// Checks if a series of the given type is in progress.
        /// </summary>
        /// <returns></returns>
        public static bool IsSeriesInProgress(MouseClickAction action)
        {
            return action != MouseClickAction.NONE && action == _lastClickAction;
        }

        /// <summary>
        /// Start a new series by resetting the variables
        /// and starting the timer.
        /// </summary>
        /// <param name="action"></param>
        private static void StartNewSeries(MouseClickAction action)
        {
            _clickCount = 0;
            _lastClickAction = action;
            _lastClickTime = DateTime.Now.Ticks;

            AppState.MainWin.Timers.Start(AppTimers.TimerId.MOUSE_CLICK_MONITOR);
        }

        /// <summary>
        /// Based on the pause in clicking as detected by the timer event,
        /// perform an appropriate actionm, reset the counter and stop the timer.
        /// </summary>
        private static void EndCurrentSeries()
        {
            try
            {
                AppState.MainWin.Timers.Stop(AppTimers.TimerId.MOUSE_CLICK_MONITOR);
                EndSeriesAction();
            }
            catch { }

            _clickCount = 0;
            _lastClickAction = MouseClickAction.NONE;
        }

        /// <summary>
        /// Performs an action after the click. 
        /// It can be nothing, or it can force stop the series
        /// when, for example in the NEXT_CHAPTER event we reached
        /// the last Chapter.
        /// </summary>
        private static void PostClickAction()
        {
            bool endSeries = false;

            try
            {
                Chapter activeChapter = AppState.ActiveChapter;
                int activeChapterIndex = activeChapter.Index;
                int activeGameIndex = activeChapter.ActiveModelGameIndex;
                int activeExerciseIndex = activeChapter.ActiveExerciseIndex;

                int chapterIndex = -1;
                int gameIndex = -1;
                int exerciseIndex = -1;

                GameData.ContentType contentType = AppState.GetContentTypeForActiveTab();

                AppState.MainWin.Dispatcher.Invoke(() =>
                {
                    switch (_lastClickAction)
                    {
                        case MouseClickAction.NEXT_CHAPTER:
                            AppState.MainWin.ClearViewForQuickSkip(contentType);
                            chapterIndex = _clickCount + activeChapterIndex;
                            endSeries = _clickCount + activeChapterIndex >= AppState.Workbook.Chapters.Count - 1;
                            PreviousNextViewBars.SetChapterCounterControls(contentType, chapterIndex);
                            break;
                        case MouseClickAction.PREVIOUS_CHAPTER:
                            AppState.MainWin.ClearViewForQuickSkip(contentType);
                            chapterIndex = activeChapterIndex - _clickCount;
                            endSeries = activeChapterIndex - _clickCount <= 0;
                            PreviousNextViewBars.SetChapterCounterControls(contentType, chapterIndex);
                            break;
                        case MouseClickAction.NEXT_GAME:
                            AppState.MainWin.ClearViewForQuickSkip(contentType);
                            gameIndex = _clickCount + activeGameIndex;
                            endSeries = _clickCount + activeGameIndex >= activeChapter.GetModelGameCount() - 1;
                            PreviousNextViewBars.SetModelGameCounterControls(gameIndex);
                            break;
                        case MouseClickAction.PREVIOUS_GAME:
                            AppState.MainWin.ClearViewForQuickSkip(contentType);
                            gameIndex = activeGameIndex - _clickCount;
                            endSeries = activeGameIndex - _clickCount <= 0;
                            PreviousNextViewBars.SetModelGameCounterControls(gameIndex);
                            break;
                        case MouseClickAction.NEXT_EXERCISE:
                            AppState.MainWin.ClearViewForQuickSkip(contentType);
                            exerciseIndex = _clickCount + activeExerciseIndex;
                            endSeries = _clickCount + activeExerciseIndex >= activeChapter.GetExerciseCount() - 1;
                            PreviousNextViewBars.SetExerciseCounterControls(exerciseIndex);
                            break;
                        case MouseClickAction.PREVIOUS_EXERCISE:
                            AppState.MainWin.ClearViewForQuickSkip(contentType);
                            exerciseIndex = activeExerciseIndex - _clickCount;
                            endSeries = activeExerciseIndex - _clickCount <= 0;
                            PreviousNextViewBars.SetExerciseCounterControls(exerciseIndex);
                            break;
                    }
                });
            }
            catch { }

            if (endSeries)
            {
                EndSeriesAction();
            }
        }

        /// <summary>
        /// Invokes the action for the end of series of the current type.
        /// </summary>
        private static void EndSeriesAction()
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                switch (_lastClickAction)
                {
                    case MouseClickAction.NEXT_CHAPTER:
                        WorkbookLocationNavigator.GotoArticle(Math.Min(_clickCount + AppState.ActiveChapter.Index, AppState.Workbook.Chapters.Count - 1),
                                                              AppState.ActiveTab);
                        break;
                    case MouseClickAction.PREVIOUS_CHAPTER:
                        WorkbookLocationNavigator.GotoArticle(Math.Max(AppState.ActiveChapter.Index - _clickCount, 0),
                                                              AppState.ActiveTab);
                        break;
                    case MouseClickAction.NEXT_GAME:
                        AppState.MainWin.SelectModelGame(
                            Math.Min(_clickCount + AppState.ActiveChapter.ActiveModelGameIndex, AppState.ActiveChapter.GetModelGameCount() - 1), 
                            true);
                        break;
                    case MouseClickAction.PREVIOUS_GAME:
                        AppState.MainWin.SelectModelGame(
                            Math.Max(AppState.ActiveChapter.ActiveModelGameIndex - _clickCount, 0),
                            true);
                        break;
                    case MouseClickAction.NEXT_EXERCISE:
                        AppState.MainWin.SelectExercise(
                            Math.Min(_clickCount + AppState.ActiveChapter.ActiveExerciseIndex, AppState.ActiveChapter.GetExerciseCount() - 1),
                            true);
                        break;
                    case MouseClickAction.PREVIOUS_EXERCISE:
                        AppState.MainWin.SelectExercise(
                            Math.Max(AppState.ActiveChapter.ActiveExerciseIndex - _clickCount, 0),
                            true);
                        break;
                }
            });
        }
    }
}

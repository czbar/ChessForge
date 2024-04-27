using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates all timers and stopwatches used in the application.
    /// The timer for polling engine messages can be accessed from here too for
    /// interface consistency. However, it is created and manipulated within the
    /// Engine Service.
    /// </summary>
    public class AppTimers
    {
        /// <summary>
        /// Ids of all timers used by the application
        /// </summary>
        public enum TimerId
        {
            DUMMY,
            PULSE,
            AUTO_SAVE,
            EVALUATION_LINE_DISPLAY,
            GAMES_EVALUATION,
            CHECK_FOR_USER_MOVE,
            CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE,
            REQUEST_WORKBOOK_MOVE,
            SOLVING_GUESS_MOVE_MADE,
            SHOW_TRAINING_PROGRESS_POPUP_MENU,
            MOUSE_CLICK_MONITOR,
            FLASH_ANNOUNCEMENT,
            WORKBOOK_READ_PROGRESS,
            APP_START,
        };

        /// <summary>
        /// Ids of all Stopwatch's used by the application
        /// </summary>
        public enum StopwatchId
        {
            DUMMY,
            EVALUATION_ELAPSED_TIME
        };

        /// <summary>
        /// Controls the frequency of automatic save.
        /// </summary>
        private Timer _autoSaveTimer;

        /// <summary>
        /// This timer invokes the method that processes engine messages and displays
        /// engine lines in the GUI.
        /// </summary>
        private Timer _evaluationLinesDisplayTimer;

        /// <summary>
        /// This timer starts and monitors the game evaluation process
        /// </summary>
        private Timer _gamesEvaluationTimer;

        /// <summary>
        /// Triggers refresh of the evaluation bar
        /// </summary>
        private Timer _pulseTimer;

        /// <summary>
        /// This timer invokes the method checking if a user made their move and if so
        /// requests appropriate processing.
        /// </summary>
        private Timer _checkForUserMoveTimer;

        /// <summary>
        /// This timer is started after the user made a move in the Solving
        /// Guess Move mode.
        /// </summary>
        private Timer _solvingGuessMoveMadeTimer;

        /// <summary>
        /// Checks if the program responded with a workbook-based move yet.
        /// </summary>
        private Timer _checkForTrainingWorkbookMoveMade;

        /// <summary>
        /// Checks if a workbook-based response has been requested.
        /// </summary>
        private Timer _requestWorkbookMove;

        /// <summary>
        /// Monitors mouse click series.
        /// </summary>
        private Timer _mouseClickMontitor;

        /// <summary>
        /// Monitors workbook read progress.
        /// </summary>
        private Timer _workbookReadMontitor;

        /// <summary>
        /// When elapsed, it invokes the Training Move context menu.
        /// NOTE: this is for the workaround where the context menu
        /// closes itself immediately when invoked "normally".
        /// </summary>
        private Timer _showTrainingProgressPopupMenu;

        /// <summary>
        /// Controls the time, a "flash announcement" is
        /// shown in the Comment Box.
        /// </summary>
        private Timer _flashAnnouncementTimer;

        /// <summary>
        /// Used when the app is starting to allow
        /// display of messages before engine load
        /// and file read complete.
        /// shown in the Comment Box.
        /// </summary>
        private Timer _appStartTimer;

        /// <summary>
        /// Tracks time that evaluation of a move/position is taking.
        /// </summary>
        private Stopwatch _evaluationProgressStopwatch;

        /// <summary>
        /// Maps TimerId to actual Timer object.
        /// </summary>
        private Dictionary<TimerId, Timer> _dictTimers = new Dictionary<TimerId, Timer>();

        /// <summary>
        /// Maps StopwatchId to actual Stopwatch object.
        /// </summary>
        private Dictionary<StopwatchId, Stopwatch> _dictStopwatches = new Dictionary<StopwatchId, Stopwatch>();

        // main application window
        private MainWindow _mainWin;

        /// <summary>
        /// Constructs all application timers.
        /// </summary>
        public AppTimers(MainWindow mainWin)
        {
            _mainWin = mainWin;

            _evaluationLinesDisplayTimer = new Timer();
            InitEvaluationLinesDisplayTimer();
            _dictTimers.Add(TimerId.EVALUATION_LINE_DISPLAY, _evaluationLinesDisplayTimer);

            _gamesEvaluationTimer = new Timer();
            InitGamesEvaluationTimer();
            _dictTimers.Add(TimerId.GAMES_EVALUATION, _gamesEvaluationTimer);

            _pulseTimer = new Timer();
            InitPulseTimer();
            _dictTimers.Add(TimerId.PULSE, _pulseTimer);

            _autoSaveTimer = new Timer();
            InitAutoSaveTimer();
            _dictTimers.Add(TimerId.AUTO_SAVE, _autoSaveTimer);

            _checkForUserMoveTimer = new Timer();
            InitCheckForUserMoveTimer();
            _dictTimers.Add(TimerId.CHECK_FOR_USER_MOVE, _checkForUserMoveTimer);

            _solvingGuessMoveMadeTimer = new Timer();
            InitSolvingGuessMoveMadeTimer();
            _dictTimers.Add(TimerId.SOLVING_GUESS_MOVE_MADE, _solvingGuessMoveMadeTimer);

            _checkForTrainingWorkbookMoveMade = new Timer();
            InitCheckForTrainingWorkbookMoveMade();
            _dictTimers.Add(TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE, _checkForTrainingWorkbookMoveMade);

            _requestWorkbookMove = new Timer();
            InitRequestWorkbookMove();
            _dictTimers.Add(TimerId.REQUEST_WORKBOOK_MOVE, _requestWorkbookMove);

            _mouseClickMontitor = new Timer();
            InitMouseClickMonitor();
            _dictTimers.Add(TimerId.MOUSE_CLICK_MONITOR, _mouseClickMontitor);

            _workbookReadMontitor = new Timer();
            InitWorkbookReadMonitor();
            _dictTimers.Add(TimerId.WORKBOOK_READ_PROGRESS, _workbookReadMontitor);

            _showTrainingProgressPopupMenu = new Timer();
            InitShowTrainingProgressPopupMenu();
            _dictTimers.Add(TimerId.SHOW_TRAINING_PROGRESS_POPUP_MENU, _showTrainingProgressPopupMenu);

            _flashAnnouncementTimer = new Timer();
            InitFlashAnnouncementTimer();
            _dictTimers.Add(TimerId.FLASH_ANNOUNCEMENT, _flashAnnouncementTimer);

            _appStartTimer = new Timer();
            InitAppStartTimer();
            _dictTimers.Add(TimerId.APP_START, _appStartTimer);

            _evaluationProgressStopwatch = new Stopwatch();
            _dictStopwatches.Add(StopwatchId.EVALUATION_ELAPSED_TIME, _evaluationProgressStopwatch);
        }

        /// <summary>
        /// Starts a timer.
        /// </summary>
        /// <param name="tt"></param>
        public void Start(TimerId tt)
        {
            _dictTimers[tt].Enabled = true;
            AppLog.Message("Start timer:" + tt.ToString());
        }

        /// <summary>
        /// Stops a timer.
        /// </summary>
        /// <param name="tt"></param>
        public void Stop(TimerId tt)
        {
            _dictTimers[tt].Enabled = false;
            AppLog.Message("Stop timer:" + tt.ToString());
        }

        /// <summary>
        /// Returns the IsEnabled status of a timer.
        /// </summary>
        /// <param name="tt"></param>
        /// <returns></returns>
        public bool IsEnabled(TimerId tt)
        {
            return _dictTimers.ContainsKey(tt) ? _dictTimers[tt].Enabled : false;
        }

        /// <summary>
        /// Returns the IsRunning status of a stopwatch.
        /// </summary>
        /// <param name="sw"></param>
        /// <returns></returns>
        public bool IsRunning(StopwatchId sw)
        {
            return _dictStopwatches.ContainsKey(sw) ? _dictStopwatches[sw].IsRunning : false;
        }

        /// <summary>
        /// Sets the interval for the timer.
        /// </summary>
        /// <param name="tt"></param>
        /// <param name="millisec"></param>
        public void SetInterval(TimerId tt, double millisec)
        {
            _dictTimers[tt].Interval = millisec;
        }

        /// <summary>
        /// Called on application exit.
        /// Stops all timers.
        /// </summary>
        public void StopAll()
        {
            foreach (var timer in _dictTimers.Values)
            {
                timer.Stop();
            }

            foreach (var stopWatch in _dictStopwatches.Values)
            {
                stopWatch.Stop();
            }
        }

        /// <summary>
        /// Stops all timers related to evaluations
        /// </summary>
        public void StopAllEvalTimers()
        {
            foreach (var id in _dictTimers.Keys)
            {
                var timer = _dictTimers[id];
                if (id == TimerId.EVALUATION_LINE_DISPLAY || id == TimerId.GAMES_EVALUATION)
                {
                    timer.Stop();
                }
            }

            foreach (var id in _dictStopwatches.Keys)
            {
                var stopwatch = _dictStopwatches[id];
                if (id == StopwatchId.EVALUATION_ELAPSED_TIME)
                {
                    stopwatch.Stop();
                }
            }
        }

        /// <summary>
        /// Starts a Stopwatch
        /// </summary>
        /// <param name="sw"></param>
        public void Start(StopwatchId sw)
        {
            _dictStopwatches[sw].Start();
            AppLog.Message("Start Stopwatch:" + sw.ToString());
        }

        /// <summary>
        /// Stops a Stopwatch
        /// </summary>
        /// <param name="sw"></param>
        public void Stop(StopwatchId sw)
        {
            _dictStopwatches[sw].Stop();
            _dictStopwatches[sw].Reset();
            AppLog.Message("Stop Stopwatch:" + sw.ToString());
        }

        /// <summary>
        /// Gets the elapsed time for the specified Stopwatch.
        /// </summary>
        /// <param name="sw"></param>
        /// <returns></returns>
        public long GetElapsedTime(StopwatchId sw)
        {
            return _dictStopwatches[sw].ElapsedMilliseconds;
        }

        /// <summary>
        /// Configures the timer for Lines display.
        /// </summary>
        private void InitEvaluationLinesDisplayTimer()
        {
            _evaluationLinesDisplayTimer.Elapsed += new ElapsedEventHandler(EngineLinesBox.ShowEngineLinesEx);
            _evaluationLinesDisplayTimer.Interval = 150;
            _evaluationLinesDisplayTimer.Enabled = false;
        }

        /// <summary>
        /// Configures the timer reponsible for controlling multi game evaluation
        /// </summary>
        private void InitGamesEvaluationTimer()
        {
            _gamesEvaluationTimer.Elapsed += new ElapsedEventHandler(GamesEvaluationManager.StartGamesEvaluation);
            _gamesEvaluationTimer.Interval = 100;
            _gamesEvaluationTimer.Enabled = false;
        }

        /// <summary>
        /// Configures the "pulse" timer that will run throughout the lifetime of the app.
        /// In particular it will trigger refresh of the evaluation bar's position.
        /// </summary>
        private void InitPulseTimer()
        {
            _pulseTimer.Elapsed += new ElapsedEventHandler(PulseManager.PulseEventHandler);
            _pulseTimer.Interval = 100;
            _pulseTimer.Enabled = false;
        }

        private void InitAutoSaveTimer()
        {
            _autoSaveTimer.Elapsed += new ElapsedEventHandler(AppState.AutoSaveEvent);
            // take the configured value with a sanity check
            _autoSaveTimer.Interval = Math.Max(Configuration.AutoSaveFrequency, 15) * 1000;
            _autoSaveTimer.Enabled = Configuration.AutoSave;
        }


        private void InitCheckForUserMoveTimer()
        {
            _checkForUserMoveTimer.Elapsed += new ElapsedEventHandler(_mainWin.CheckForUserMoveTimerEvent);
            _checkForUserMoveTimer.Interval = 50;
            _checkForUserMoveTimer.Enabled = false;
        }

        private void InitSolvingGuessMoveMadeTimer()
        {
            _solvingGuessMoveMadeTimer.Elapsed += new ElapsedEventHandler(_mainWin.SolvingGuessMoveMadeTimerEvent);
            _solvingGuessMoveMadeTimer.Interval = 100;
            _solvingGuessMoveMadeTimer.Enabled = false;
        }

        private void InitCheckForTrainingWorkbookMoveMade()
        {
            _checkForTrainingWorkbookMoveMade.Elapsed += new ElapsedEventHandler(EngineGame.CheckForTrainingWorkbookMoveMade);
            _checkForTrainingWorkbookMoveMade.Interval = 600;
            _checkForTrainingWorkbookMoveMade.Enabled = false;
        }

        private void InitRequestWorkbookMove()
        {
            _requestWorkbookMove.Elapsed += new ElapsedEventHandler(_mainWin.InvokeRequestWorkbookResponse);
            _requestWorkbookMove.Interval = 300;
            _requestWorkbookMove.Enabled = false;
        }

        private void InitMouseClickMonitor()
        {
            _mouseClickMontitor.Elapsed += new ElapsedEventHandler(MouseClickMonitor.TimerClickSeriesStatus);
            _mouseClickMontitor.Interval = 200;
            _mouseClickMontitor.Enabled = false;
        }

        private void InitWorkbookReadMonitor()
        {
            _workbookReadMontitor.Elapsed += new ElapsedEventHandler(BackgroundPgnProcessingManager.ReportReadProgress);
            _workbookReadMontitor.Interval = 500;
            _workbookReadMontitor.Enabled = false;
        }

        private void InitShowTrainingProgressPopupMenu()
        {
            _showTrainingProgressPopupMenu.Elapsed += new ElapsedEventHandler(_mainWin.ShowTrainingProgressPopupMenu);
            _showTrainingProgressPopupMenu.Interval = 100;
            _showTrainingProgressPopupMenu.Enabled = false;
        }

        private void InitFlashAnnouncementTimer()
        {
            _flashAnnouncementTimer.Elapsed += new ElapsedEventHandler(_mainWin.FlashAnnouncementTimeUp);
            _flashAnnouncementTimer.Interval = 1800;
            _flashAnnouncementTimer.Enabled = false;
        }

        private void InitAppStartTimer()
        {
            _appStartTimer.Elapsed += new ElapsedEventHandler(_mainWin.AppStartTimeUp);
            _appStartTimer.Interval = 100;
            _appStartTimer.Enabled = false;
        }
    }
}

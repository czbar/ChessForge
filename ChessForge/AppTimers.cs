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
            EVALUATION_LINE_DISPLAY,
            CHECK_FOR_USER_MOVE,
            ENGINE_MESSAGE_POLL,
            ENGINE_EVALUATION_STOP,
            CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE,
            REQUEST_WORKBOOK_MOVE,
            SHOW_TRAINING_PROGRESS_POPUP_MENU,
            STOP_MESSAGE_POLL,
            FLASH_ANNOUNCEMENT,
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
        /// This timer invokes the method that processes engine messages and displays
        /// engine lines in the GUI.
        /// </summary>
        private Timer _evaluationLinesDisplayTimer;

        /// <summary>
        /// Triggers a Stop request to the engine. Used to control the time
        /// the engine takes to evaluate since "go movetime" seems to act weirdly.
        /// </summary>
        private Timer _engineEvaluationStopTimer;

        /// <summary>
        /// This timer invokes the method checking if a user made their move and if so
        /// requests appropriate processing.
        /// </summary>
        private Timer _checkForUserMoveTimer;

        /// <summary>
        /// Checks if the program responded with a workbook-based move yet.
        /// </summary>
        private Timer _checkForTrainingWorkbookMoveMade;

        /// <summary>
        /// Checks if a workbook-based response has been requested.
        /// </summary>
        private Timer _requestWorkbookMove;

        /// <summary>
        /// When elapsed, it invokes the Training Move context menu.
        /// NOTE: this is for the workaround where the context menu
        /// closes itself immediately when invoked "normally".
        /// </summary>
        private Timer _showTrainingProgressPopupMenu;

        /// <summary>
        /// Used to check if engine has any messages coming after
        /// a stop command was issued.
        /// We do not want to stop polling for engine messages
        /// prematurely.
        /// </summary>
        private Timer _stopMessagePollTimer;

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
        /// <param name="gui"></param>
        public AppTimers(EngineLinesBox gui, MainWindow mainWin)
        {
            _mainWin = mainWin;

            _evaluationLinesDisplayTimer = new Timer();
            InitEvaluationLinesDisplayTimer(gui);
            _dictTimers.Add(TimerId.EVALUATION_LINE_DISPLAY, _evaluationLinesDisplayTimer);

            _engineEvaluationStopTimer = new Timer();
            InitEngineEvaluationStopTimer();
            _dictTimers.Add(TimerId.ENGINE_EVALUATION_STOP, _engineEvaluationStopTimer);

            _checkForUserMoveTimer = new Timer();
            InitCheckForUserMoveTimer();
            _dictTimers.Add(TimerId.CHECK_FOR_USER_MOVE, _checkForUserMoveTimer);

            _checkForTrainingWorkbookMoveMade = new Timer();
            InitCheckForTrainingWorkbookMoveMade();
            _dictTimers.Add(TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE, _checkForTrainingWorkbookMoveMade);

            _requestWorkbookMove = new Timer();
            InitRequestWorkbookMove();
            _dictTimers.Add(TimerId.REQUEST_WORKBOOK_MOVE, _requestWorkbookMove);

            _showTrainingProgressPopupMenu = new Timer();
            InitShowTrainingProgressPopupMenu();
            _dictTimers.Add(TimerId.SHOW_TRAINING_PROGRESS_POPUP_MENU, _showTrainingProgressPopupMenu);

            _flashAnnouncementTimer = new Timer();
            InitFlashAnnouncementTimer();
            _dictTimers.Add(TimerId.FLASH_ANNOUNCEMENT, _flashAnnouncementTimer);

            _appStartTimer = new Timer();
            InitAppStartTimer();
            _dictTimers.Add(TimerId.APP_START, _appStartTimer);

            _stopMessagePollTimer = new Timer();
            InitStopMessagePoll();
            _dictTimers.Add(TimerId.STOP_MESSAGE_POLL, _stopMessagePollTimer);

            _evaluationProgressStopwatch = new Stopwatch();
            _dictStopwatches.Add(StopwatchId.EVALUATION_ELAPSED_TIME, _evaluationProgressStopwatch);
        }

        /// <summary>
        /// Starts a timer.
        /// Handles ENGINE_MESSAGE_POLL timer as a special case.
        /// </summary>
        /// <param name="tt"></param>
        public void Start(TimerId tt)
        {
            if (tt == TimerId.ENGINE_MESSAGE_POLL)
            {
                EngineMessageProcessor.ChessEngineService.StartMessagePollTimer();
            }
            else
            {
                _dictTimers[tt].Enabled = true;
            }
            AppLog.Message("Start timer:" + tt.ToString());
        }

        /// <summary>
        /// Stops a timer.
        /// Handles ENGINE_MESSAGE_POLL timer as a special case.
        /// </summary>
        /// <param name="tt"></param>
        public void Stop(TimerId tt)
        {
            if (tt == TimerId.ENGINE_MESSAGE_POLL)
            {
                EngineMessageProcessor.ChessEngineService.StopMessagePollTimer();
            }
            else
            {
                _dictTimers[tt].Enabled = false;
            }
            AppLog.Message("Stop timer:" + tt.ToString());
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
        /// Handles ENGINE_MESSAGE_POLL timer as a special case.
        /// </summary>
        public void StopAll(bool excludeMessagePoll)
        {
            if (!excludeMessagePoll)
            {
                EngineMessageProcessor.ChessEngineService.StopMessagePollTimer();
            }

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
        /// Starts a Stopwatch
        /// </summary>
        /// <param name="sw"></param>
        public void Start(StopwatchId sw)
        {
            _dictStopwatches[sw].Start();
        }

        /// <summary>
        /// Stops a Stopwatch
        /// </summary>
        /// <param name="sw"></param>
        public void Stop(StopwatchId sw)
        {
            _dictStopwatches[sw].Stop();
            _dictStopwatches[sw].Reset();
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

        private void InitEvaluationLinesDisplayTimer(EngineLinesBox gui)
        {
            _evaluationLinesDisplayTimer.Elapsed += new ElapsedEventHandler(gui.ShowEngineLines);
            _evaluationLinesDisplayTimer.Interval = 100;
            _evaluationLinesDisplayTimer.Enabled = false;
        }

        private void InitEngineEvaluationStopTimer()
        {
            _engineEvaluationStopTimer.Elapsed += new ElapsedEventHandler(EngineMessageProcessor.StopEngineEvaluation);
            _engineEvaluationStopTimer.Interval = 1000;
            _engineEvaluationStopTimer.Enabled = false;
            _engineEvaluationStopTimer.AutoReset = false;
        }

        private void InitCheckForUserMoveTimer()
        {
            _checkForUserMoveTimer.Elapsed += new ElapsedEventHandler(_mainWin.ProcessUserMoveEvent);
            _checkForUserMoveTimer.Interval = 50;
            _checkForUserMoveTimer.Enabled = false;
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
        
        private void InitShowTrainingProgressPopupMenu()
        {
            _showTrainingProgressPopupMenu.Elapsed += new ElapsedEventHandler(_mainWin.ShowTrainingProgressPopupMenu);
            _showTrainingProgressPopupMenu.Interval = 100;
            _showTrainingProgressPopupMenu.Enabled = false;
        }

        private void InitStopMessagePoll()
        {
            _stopMessagePollTimer.Elapsed += new ElapsedEventHandler(EngineMessageProcessor.MessageQueueTimeout);
            _stopMessagePollTimer.Interval = 200;
            _stopMessagePollTimer.Enabled = false;
        }

        private void InitFlashAnnouncementTimer()
        {
            _flashAnnouncementTimer.Elapsed += new ElapsedEventHandler(_mainWin.FlashAnnouncementTimeUp);
            _flashAnnouncementTimer.Interval = 1000;
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

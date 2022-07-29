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
            CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE,
            REQUEST_WORKBOOK_MOVE,
            SHOW_TRAINING_PROGRESS_POPUP_MENU,
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

        /// <summary>
        /// Constructs all application timers.
        /// </summary>
        /// <param name="gui"></param>
        public AppTimers(EngineEvaluationGUI gui)
        {
            _evaluationLinesDisplayTimer = new Timer();
            InitEvaluationLinesDisplayTimer(gui);
            _dictTimers.Add(TimerId.EVALUATION_LINE_DISPLAY, _evaluationLinesDisplayTimer);

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
        }

        /// <summary>
        /// Called on application exit.
        /// Stops all timers.
        /// Handles ENGINE_MESSAGE_POLL timer as a special case.
        /// </summary>
        public void StopAll()
        {
            EngineMessageProcessor.ChessEngineService.StopMessagePollTimer();

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

        private void InitEvaluationLinesDisplayTimer(EngineEvaluationGUI gui)
        {
            _evaluationLinesDisplayTimer.Elapsed += new ElapsedEventHandler(gui.ShowEngineLines);
            _evaluationLinesDisplayTimer.Interval = 100;
            _evaluationLinesDisplayTimer.Enabled = false;
        }

        private void InitCheckForUserMoveTimer()
        {
            _checkForUserMoveTimer.Elapsed += new ElapsedEventHandler(AppState.MainWin.ProcessUserGameMoveEvent);
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
            _requestWorkbookMove.Elapsed += new ElapsedEventHandler(AppState.MainWin.InvokeRequestWorkbookResponse);
            _requestWorkbookMove.Interval = 300;
            _requestWorkbookMove.Enabled = false;
        }
        
        private void InitShowTrainingProgressPopupMenu()
        {
            _showTrainingProgressPopupMenu.Elapsed += new ElapsedEventHandler(AppState.MainWin.ShowTrainingProgressPopupMenu);
            _showTrainingProgressPopupMenu.Interval = 100;
            _showTrainingProgressPopupMenu.Enabled = false;
        }
    }
}

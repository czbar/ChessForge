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
    /// Encapsulates all timers and stopwatches used in the application
    /// </summary>
    internal class AppTimers
    {
        /// <summary>
        /// Ids of all timers used by the application
        /// </summary>
        internal enum TimerId{
            DUMMY,
            EVALUTION_LINE_DISPLAY,
            CHECK_FOR_USER_MOVE
        };

        internal enum StopwatchId
        {
            DUMMY,
            EVALUTION_PROGRESS
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
        private Timer _checkForUserMoveTimer = new Timer();

        /// <summary>
        /// This timer invokes the method checking for new messages
        /// from the engine.
        /// </summary>
        private Timer _engineMessagePollTimer = new Timer();

        /// <summary>
        /// Tracks time that evaluation of a move/position is taking.
        /// </summary>
        private Stopwatch _evaluationProgressStopwatch = new Stopwatch();

        /// <summary>
        /// Maps TimerId to actual Timer object.
        /// </summary>
        private Dictionary<TimerId, Timer> _dictTimers = new System.Collections.Generic.Dictionary<TimerId, Timer>();

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
            _dictTimers.Add(TimerId.EVALUTION_LINE_DISPLAY, _evaluationLinesDisplayTimer);

            _checkForUserMoveTimer = new Timer();
            InitCheckForUserMoveTimer();
            _dictTimers.Add(TimerId.CHECK_FOR_USER_MOVE, _checkForUserMoveTimer);

            _evaluationProgressStopwatch = new Stopwatch();
            _dictStopwatches.Add(StopwatchId.EVALUTION_PROGRESS, _evaluationProgressStopwatch);
        }

        public void Start(TimerId tt)
        {
            _dictTimers[tt].Enabled = true;
        }

        public void Stop(TimerId tt)
        {
            _dictTimers[tt].Enabled = false;
        }

        public void Start(StopwatchId sw)
        {
            _dictStopwatches[sw].Start();
        }

        public void Stop(StopwatchId sw)
        {
            _dictStopwatches[sw].Stop();
            _dictStopwatches[sw].Reset();
        }

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

    }
}

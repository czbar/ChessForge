using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Application wide settings.
    /// </summary>
    public class LearningMode
    {
        /// <summary>
        /// The program is always in one, and only one, of these modes.
        /// 
        /// Changing between modes requires a number of
        /// steps to be performed, in particular blocking certain 
        /// activities. E.g. going from manual analysis to game play
        /// requires that the position is set appropriately
        /// and user inputs, other than request to stop analysis
        /// are blocked.
        /// 
        /// </summary>
        public enum Mode : uint
        {
            /// <summary>
            /// No workbook loaded, no session started, the program is waiting.
            /// </summary>
            IDLE = 0x0001,

            /// <summary>
            /// A training session is in progress
            /// </summary>
            TRAINING = 0x0002,

            /// <summary>
            /// The user is playing against the engine.
            /// This mode can only be entered from the 
            /// MANUAL_REVIEW_MODE and that is the mode the program
            /// will return to when the game is finished.
            /// </summary>
            ENGINE_GAME = 0x0004,

            /// <summary>
            /// The user is reviewing the workbook.
            /// Can switch between different lines.
            /// </summary>
            MANUAL_REVIEW = 0x0010
        }

        /// <summary>
        /// Lock object to use when accessing evaluation related
        /// variables.
        /// </summary>
        public static object EvalLock = new object();

        // current appliction mode
        private static Mode _currentMode = Mode.IDLE;

        // previous application mode
        private static Mode _previousMode = Mode.IDLE;

        /// <summary>
        /// Switches application to another mode.
        /// </summary>
        public static void ChangeCurrentMode(LearningMode.Mode mode, bool updateGui = true)
        {
            _previousMode = _currentMode;
            _currentMode = mode;

            if (updateGui)
            {
                AppState.SetupGuiForCurrentStates();
            }
        }

        /// <summary>
        /// The side, White or Black, that is training in 
        /// this session.
        /// </summary>
        public static PieceColor TrainingSideConfig
        {
            get { return AppState.MainWin.SessionWorkbook.TrainingSideConfig; }
            set { AppState.MainWin.SessionWorkbook.TrainingSideConfig = value; }
        }

        public static PieceColor TrainingSideCurrent
        {
            get { return AppState.MainWin.SessionWorkbook.TrainingSideCurrent; }
            set { AppState.MainWin.SessionWorkbook.TrainingSideCurrent = value; }
        }

        /// <summary>
        /// The current Learning mode of the application.
        /// </summary>
        public static Mode CurrentMode { get => _currentMode;}

        /// <summary>
        /// The previous mode of the application.
        /// This is applicable when an exit from the current mode is requested.
        /// </summary>
        public static Mode PreviousMode { get => _previousMode; set => _previousMode = value; }

        /// <summary>
        /// Horizontal animation object.
        /// </summary>
        public static DoubleAnimation CurrentAnimationX;

        /// <summary>
        /// Vertical animation object.
        /// </summary>
        public static DoubleAnimation CurrentAnimationY;

        /// <summary>
        /// Animation translation object.
        /// </summary>
        public static TranslateTransform CurrentTranslateTransform;

        /// <summary>
        /// Currently active (selected) line.
        /// There can only be one (or none) line selected in the Workbook at any time
        /// </summary>
        public static string ActiveLineId;

    }
}

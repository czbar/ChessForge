using System.IO;
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
            /// No workbook loaded, 
            /// no session started,
            /// the program is waiting.
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
        /// Types of files that Chess Forge can handle.
        /// PGN can only be viewed, not edited.
        /// CHF can be viewed and edited.
        /// </summary>
        public enum FileType
        {
            CHF,
            PGN
        }

        /// <summary>
        /// Indicates whether there are any unsaved changes in the Workbook
        /// </summary>
        public static bool IsDirty;

        /// <summary>
        /// Lock object to use whan accessing evaluation related
        /// variables.
        /// </summary>
        public static object EvalLock = new object();

        // current appliction mode
        private static Mode _currentMode = Mode.IDLE;

        // previous application mode
        private static Mode _previousMode = Mode.IDLE;

        // path to the current workbook file
        private static string _workbookFilePath;

        // type of the current workbook (chf or pgn)
        private static FileType _workbookFileType;

        /// <summary>
        /// Switches application to another mode.
        /// </summary>
        public static void ChangeCurrentMode(LearningMode.Mode mode)
        {
            TidyUpOnModeExit(_previousMode);

            _previousMode = _currentMode;
            _currentMode = mode;

            // TODO: we need to sort out this condition using submode,
            // introduce a new mode, or ... something
            if (mode != Mode.ENGINE_GAME || _previousMode != Mode.TRAINING)
            {
                AppStateManager.SetupGuiForCurrentStates();
            }
        }

        /// <summary>
        /// The side, White or Black, that is training in 
        /// this session.
        /// </summary>
        public static PieceColor TrainingSide
        {
            get { return AppStateManager.MainWin.Workbook.TrainingSide; }
            set { AppStateManager.MainWin.Workbook.TrainingSide = value; }
        }

        /// <summary>
        /// Tidies up what ever necessary when
        /// exiting a mode.
        /// E.g. stoping appropriate timers.
        /// </summary>
        /// <param name="mode"></param>
        private static void TidyUpOnModeExit(LearningMode.Mode previousMode)
        {
        }

        /// <summary>
        /// Exits the mode the application is currently in
        /// and returns to the previous mode.
        /// </summary>
        public static void ExitCurrentMode()
        {
            ChangeCurrentMode(_previousMode);
        }

        /// <summary>
        /// The current mode of the application.
        /// </summary>
        public static Mode CurrentMode { get => _currentMode; set => _currentMode = value; }

        /// <summary>
        /// The previous mode of the application.
        /// This is applicable when an exit from the current mode is requested.
        /// </summary>
        public static Mode PreviousMode { get => _previousMode; set => _previousMode = value; }

        /// <summary>
        /// The file path of the current Workbook file.
        /// When set, checks if there was a different value previously, and if
        /// so, if it should be saved.
        /// </summary>
        public static string WorkbookFilePath
        {
            get => _workbookFilePath;
            set {
                if (!string.IsNullOrEmpty(_workbookFilePath) && WorkbookFileType == FileType.CHF && IsDirty)
                {
                    SaveWorkbookFile();
                }
                _workbookFilePath = value;
                if (Path.GetExtension(_workbookFilePath).ToLower() == ".chf")
                {
                    _workbookFileType = FileType.CHF;
                }
                else
                {
                    _workbookFileType = FileType.PGN;
                }
            }
        }

        /// <summary>
        /// Type of the file currently open as
        /// the Workbook.
        /// </summary>
        public static FileType WorkbookFileType { get => _workbookFileType;}

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

        /// <summary>
        /// Saves the workbook to a file.
        /// It will only write to the file if the 
        /// session's file type is CHF
        /// </summary>
        public static void SaveWorkbookFile(bool checkDirty = false)
        {
            if (checkDirty && !IsDirty)
                return;

            if (WorkbookFileType == FileType.CHF)
            {
                string chfText = ChfTextBuilder.BuildText(AppStateManager.MainWin.Workbook);
                File.WriteAllText(WorkbookFilePath, chfText);
            }
        }

    }
}

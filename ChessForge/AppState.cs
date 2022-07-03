using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ChessForge
{
    /// <summary>
    /// Application wide settings.
    /// </summary>
    public class AppState
    {
        /// <summary>
        /// The program is always in one or more of these modes.
        /// For example, if the user is playing against the computer
        /// as part of a training session, the TRAINING and PLAY_VS_COMPUTER
        /// flags will be raised.
        /// 
        /// Changing between modes requires a number of
        /// steps to be performed, in particular blocking certain 
        /// activities. E.g. going from manual analysis to game replay
        /// requires that the position is set appropriately
        /// and user inputs, other than request to stop analysis
        /// are blocked.
        /// </summary>
        public enum Mode
        {
            IDLE = 0x00,              // no data read in, nothing's happening
            TRAINING = 0x01,          // the user is in a training session
            PLAY_VS_COMPUTER = 0x02,  // the user is playing against the computer
                                      // this is only available if there is a game loaded
            GAME_REPLAY = 0x04,       // auto-replaying a line from the currently loaded game
            MANUAL_REVIEW = 0x08,     // the user is manually replaying the game, may make some new moves
            ENGINE_EVALUATION = 0x10  // the program is evaluating a move or a line
        }

        /// <summary>
        /// Main application window.
        /// Exposing the public reference through this object
        /// for convenient access/reference.
        /// </summary>
        public static MainWindow MainWin;

        /// <summary>
        /// The current mode of the application.
        /// </summary>
        public static Mode CurrentMode { get; set; }

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
        /// The list of bookmarks.
        /// </summary>
        public static List<BookmarkView> Bookmarks = new List<BookmarkView>();

        /// <summary>
        /// Currently selected line.
        /// There can only be one (or none) line selected in the Workbook at any time
        /// </summary>
        public static string SelectedLine;

        /// <summary>
        /// Currently selected Tree Node (ply) in the Workbook.
        /// There can only be one (or none) node selected in the Workbook at any time
        /// </summary>
        public static int NodeId;

        /// <summary>
        /// Index in the list of bookmarks of the bookmark currently being
        /// active in a training session.
        /// Precisely one bookmark can be active during a session. 
        /// </summary>
        public static int ActiveBookmarkInTraining = -1;
    }
}

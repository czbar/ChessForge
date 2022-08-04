using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    public class ManualReviewState
    {
        /// <summary>
        /// Submodes for the MANUAL_REVIEW Learning mode.
        /// </summary>
        public enum Mode : uint
        {
            /// <summary>
            /// The user is browsing lines
            /// </summary>
            NORMAL = 0x0000,

            /// <summary>
            /// The program is auto-replaying a line
            /// </summary>
            AUTO_REPLAY = 0x0001,
        }

        /// <summary>
        /// The current submode in the MANUAL_REVIEW mode.
        /// </summary>
        public static Mode CurrentMode { get; set; }


    }
}

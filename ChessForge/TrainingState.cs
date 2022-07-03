using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    public class TrainingState
    {
        private static bool isTrainingInProgress;

        public enum Mode
        {
            UNKNOWN = 0x00,
            AWAITING_USER_MOVE = 0x01,
            ENGINE_EVALUATION_IN_PROGRESS = 0x02,
            PAUSED = 0x04,
            USER_MOVE_COMPLETED = 0x08
        }

        public static bool IsTrainingInProgress
        { get => isTrainingInProgress; set => isTrainingInProgress = value; }

        /// <summary>
        /// The current mode of the application.
        /// </summary>
        public static Mode CurrentMode { get; set; }
    }
}

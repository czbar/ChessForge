using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Holds attributes of the current training state. 
    /// </summary>
    public class TrainingState
    {
        public enum Mode
        {
            UNKNOWN = 0x00,
            AWAITING_USER_TRAINING_MOVE = 0x01,
            ENGINE_EVALUATION_IN_PROGRESS = 0x02,
            PAUSED = 0x04,
            USER_MOVE_COMPLETED = 0x08
        }

        /// <summary>
        /// Whether a training session is in progress.
        /// </summary>
        public static bool IsTrainingInProgress
        { get => _isTrainingInProgress; set => _isTrainingInProgress = value; }

        /// <summary>
        /// The current mode of the application.
        /// </summary>
        public static Mode CurrentMode { get; set; }

        /// <summary>
        /// Indicates if the training is in a game mode i.e. off the Workbook.
        /// </summary>
        public static bool IsEngineGameInProgress { get => _isEngineGameInProgress; set => _isEngineGameInProgress = value; }

        private static bool _isTrainingInProgress;
        private static bool _isEngineGameInProgress;
    }
}

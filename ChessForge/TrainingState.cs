using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Holds attributes of the current training state. 
    /// </summary>
    public class TrainingState
    {
        private static bool _isTrainingInProgress;
        private static bool _isEngineGameInProgress;

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

        /// <summary>
        /// The current training line.
        /// </summary>
        public static List<TreeNode> TrainingLine = new List<TreeNode>();

        /// <summary>
        /// Resets the current training line and sets
        /// the starting position node.
        /// </summary>
        /// <param name="startPos"></param>
        public static void ResetTrainingLine(TreeNode startPos)
        {
            TrainingLine.Clear();
            TrainingLine.Add(startPos);
        }

        /// <summary>
        /// Returns the starting position of this training line.
        /// </summary>
        public static TreeNode StartPosition
        {
            get { return TrainingLine[0]; }
        }

        /// <summary>
        /// Rolls back training line to the move corresponding
        /// the passed nd.
        /// Once found, replace that move with the passed node.
        /// Returns true if successful, false if the node was not found.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public static bool RollbackTrainingLine(TreeNode nd)
        {
            int idx = -1;
            for (int i = 0; i < TrainingLine.Count; i++)
            {
                if (TrainingLine[i].MoveNumber == nd.MoveNumber && TrainingLine[i].ColorToMove == nd.ColorToMove)
                {
                    idx = i;
                    break;
                }
            }

            if (idx > 0)
            {
                if (idx < TrainingLine.Count - 1)
                {
                    TrainingLine.RemoveRange(idx, TrainingLine.Count - idx);
                }
                TrainingLine.Add(nd);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Adds the passed noe to the Training Line.
        /// </summary>
        /// <returns></returns>
        public static void AddNodeToTrainingLine(TreeNode nd)
        {
            TrainingLine.Add(nd);
        }
    }
}

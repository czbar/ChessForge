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
        private static bool _isBrowseActive;

        /// <summary>
        /// Object to lock examining of the user move vs Workbook.
        /// We will be setting a Training State to new value
        /// and don't want another timer response to interfere.
        /// </summary>
        public static object UserVsWorkbookMoveLock = new object();

        /// <summary>
        /// Possible Training states.
        /// </summary>
        public enum Mode
        {
            UNKNOWN = 0x00,

            // all is idle, awaiting the user to make a move
            AWAITING_USER_TRAINING_MOVE = 0x01,

            // user's move accepted, awaiting a workboook-based response
            AWAITING_WORKBOOK_RESPONSE = 0x02,

            // the engine is evalauting a move or a line
            ENGINE_EVALUATION_IN_PROGRESS = 0x04,
            
            // user move completed, the program will pick it up
            USER_MOVE_COMPLETED = 0x08,

            // a game vs engine is in progress
            ENGINE_GAME = 0x10
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
        /// Indicates if the user is using the Browsing view of the training lines. 
        /// </summary>
        public static bool IsBrowseActive { get => _isBrowseActive; set => _isBrowseActive = value; }

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

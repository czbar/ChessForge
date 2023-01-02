using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Holds attributes of the current training session. 
    /// </summary>
    public class TrainingSession
    {
        /// <summary>
        /// Object to lock examining of the user move vs Workbook.
        /// We will be setting a Training State to new value
        /// and don't want another timer response to interfere.
        /// </summary>
        public static object UserVsWorkbookMoveLock = new object();

        /// <summary>
        /// Possible Training states.
        /// </summary>
        public enum State
        {
            // no training session is open
            INACTIVE,

            // all is idle, awaiting the user to make a move
            AWAITING_USER_TRAINING_MOVE,

            // user's move accepted, awaiting a workboook-based response
            AWAITING_WORKBOOK_RESPONSE,

            // user move completed, the program will pick it up
            USER_MOVE_COMPLETED,
        }

        /// <summary>
        /// Whether a training session is in progress.
        /// </summary>
        public static bool IsTrainingInProgress
        { get => _isTrainingInProgress; set => _isTrainingInProgress = value; }

        /// <summary>
        /// Whether continuous engine evaluation is on during Training.
        /// </summary>
        public static bool IsContinuousEvaluation
        { get => _isContinuousEvaluation; set => _isContinuousEvaluation = value; }

        /// <summary>
        /// The current state of the Training session.
        /// </summary>
        public static State CurrentState { get => _currentState; }

        /// <summary>
        /// Indicates if the user is using the Browsing view of the training lines. 
        /// </summary>
        public static bool IsBrowseActive { get => _isBrowseActive; set => _isBrowseActive = value; }

        /// <summary>
        /// The current training line.
        /// </summary>
        public static List<TreeNode> TrainingLine = new List<TreeNode>();

        /// <summary>
        /// Sets the state of the Training session.
        /// </summary>
        /// <param name="state"></param>
        public static void ChangeCurrentState(State state)
        {
            _currentState = state;
        }

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

        // whether continuous evaluation is enabled
        private static bool _isContinuousEvaluation;

        // Flags if a Training Session is in progress
        private static bool _isTrainingInProgress;

        // Flags if the Browse view is active.
        private static bool _isBrowseActive;

        // The current state of the Training sessioin.
        private static State _currentState;

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

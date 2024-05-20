using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessPosition;
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
        { 
            get => _isTrainingInProgress; 
            set => _isTrainingInProgress = value; 
        }

        /// <summary>
        /// Whether continuous engine evaluation is on during Training.
        /// </summary>
        public static bool IsContinuousEvaluation
        {
            get => _isContinuousEvaluation; 
            set => _isContinuousEvaluation = value;
        }

        /// <summary>
        /// Whether takeback is currently available. 
        /// It is only set to true when the user made a move not in the workbook.
        /// </summary>
        public static bool IsTakebackAvailable
        { get => _isTakebackAvailable; set => _isTakebackAvailable = value; }

        /// <summary>
        /// The current state of the Training session.
        /// </summary>
        public static State CurrentState { get => _currentState; }

        /// <summary>
        /// The current training line.
        /// </summary>
        public static List<TreeNode> TrainingLine = new List<TreeNode>();

        /// <summary>
        /// The side that is training. It can be different from the Workbook's training side.
        /// </summary>
        public static PieceColor TrainingSide;

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

        // The current state of the Training sessioin.
        private static State _currentState;

        // Whether takeback is available at this moment
        private static bool _isTakebackAvailable = false;

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
        /// Adds the passed node to the Training Line.
        /// </summary>
        /// <returns></returns>
        public static void AddNodeToTrainingLine(TreeNode nd)
        {
            TrainingLine.Add(nd);
        }
    }
}

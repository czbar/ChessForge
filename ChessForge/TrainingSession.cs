using ChessPosition;
using GameTree;
using System.Collections.Generic;
using System.Linq;

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

        // whether continuous evaluation is enabled
        private static bool _isContinuousEvaluation;

        // Flags if a Training Session is in progress
        private static bool _isTrainingInProgress;

        // The current state of the Training session.
        private static State _currentState;

        // Whether takeback is available at this moment
        private static bool _isTakebackAvailable = false;

        /// <summary>
        /// The current training line.
        /// Node 0 is the node AFTER which the training line starts. So, the first user's move
        /// is the response to the position in this node.
        /// The Training Side is the side on the move in Node 0.
        /// </summary>
        private static List<TreeNode> TrainingLine = new List<TreeNode>();

        /// <summary>
        /// The side that is training. It can be different from the Workbook's training side.
        /// </summary>
        public static PieceColor TrainingSide
        {
            get { return StartPosition.ColorToMove; }
        }

        /// <summary>
        /// Sets the state of the Training session.
        /// </summary>
        /// <param name="state"></param>
        public static void ChangeCurrentState(State state)
        {
            _currentState = state;
        }

        /// <summary>
        /// Starting position of this training session.
        /// </summary>
        public static TreeNode StartPosition;

        /// <summary>
        /// Builds a training line that follows the content of the EngineGame
        /// (which should be just initialized at this point) and the follows
        /// the local main line.
        /// </summary>
        public static void BuildFirstTrainingLine()
        {
            TrainingLine.Clear();

            // The stem line from move 0 to the starting position.
            for (int i = 0; i < EngineGame.Line.NodeList.Count; i++)
            {
                TreeNode nd = EngineGame.Line.NodeList[i];
                TrainingLine.Add(nd);

                // this check should not be nesessary, if the flow is as expected
                if (nd == StartPosition)
                {
                    break;
                }
            }

            CompleteTrainingLine();
        }

        /// <summary>
        /// Obtains the current game line and starting from the last node, traverses
        /// it to the first node until it finds a node that has a sibling NEXT to it 
        /// (i.e. at the next index in the parent's list).
        /// Once found, replaces the node in the TrainingLine with that sibling
        /// and builds the rest of the training line from that node down 
        /// (following the local main line).
        /// </summary>
        public static void BuildNextTrainingLine()
        {
            int moveToUpdateIndex = FindMoveToUpdateIndex();

            if (moveToUpdateIndex <= 0)
            {
                BuildFirstTrainingLine();
            }
            else
            {
                TrainingLine.Clear();
                // 1. Add all the moves up to the move that is to be updated 
                for (int i = 0; i < moveToUpdateIndex; i++)
                {
                    TrainingLine.Add(EngineGame.Line.NodeList[i]);
                }

                // 2. Update the move at the given index
                UpdateMoveAtIndex(moveToUpdateIndex);

                // 3. Add the rest of the training line from the updated move
                CompleteTrainingLine();
            }
        }

        /// <summary>
        /// Replaces the move at the given index with the next sibling.
        /// </summary>
        /// <param name="moveToUpdateIndex"></param>
        private static void UpdateMoveAtIndex(int moveToUpdateIndex)
        {
            TreeNode moveToUpdate = EngineGame.Line.NodeList[moveToUpdateIndex];
            TreeNode moveToUpdateParent = moveToUpdate.Parent;

            int currChildIndex = moveToUpdateParent.Children.IndexOf(moveToUpdate);
            if (currChildIndex < moveToUpdateParent.Children.Count - 1)
            {
                TreeNode updatedNode = moveToUpdateParent.Children[currChildIndex + 1];
                TrainingLine[moveToUpdateIndex] = updatedNode;
            }
        }

        /// <summary>
        /// Finds the index of the furthest move in the Workbook
        /// that can be "updated" i.e. has a sibling at a higher Children index.
        /// Traverses the engine game backwards from the last Workbook move
        /// to find an "updateable" one.
        /// </summary>
        /// <returns></returns>
        private static int FindMoveToUpdateIndex()
        {
            int lastWorkbookMoveIndex = FindLastWorkbookMoveIndex();

            int moveToUpdateIndex = -1;
            for (int i = lastWorkbookMoveIndex; i >= 0; i--)
            {
                TreeNode node = EngineGame.Line.NodeList[i];
                if (node == StartPosition)
                {
                    break;
                }

                if (node.Parent != null && node.Parent.Children.IndexOf(node) < node.Parent.Children.Count - 1)
                {
                    moveToUpdateIndex = i;
                }
            }

            return moveToUpdateIndex;
        }

        /// <summary>
        /// Finds the index of the last move in the Workbook.
        /// Traverses the engine game backwards until it finds a node
        /// that is not marked as a NewTrainingMove.
        /// </summary>
        /// <returns></returns>
        private static int FindLastWorkbookMoveIndex()
        {
            var engineGame = EngineGame.Line.NodeList;
            int lastWorkbookMoveIndex = 0;

            for (int i = engineGame.Count - 1; i >= 0; i--)
            {
                if (!engineGame[i].IsNewTrainingMove)
                {
                    lastWorkbookMoveIndex = i;
                    break;
                }
            }

            return lastWorkbookMoveIndex;
        }

        /// <summary>
        /// Adds the first child of the last node and then 
        /// the first children of the descendant nodes.
        /// Stop if a NewTrainingMove node is encountered
        /// as it is a node from an earlier training game rather than
        /// the Workbook.
        /// </summary>
        private static void CompleteTrainingLine()
        {
            TreeNode node = EngineGame.Line.NodeList.Last();

            // follow the first child nodes until we reach a node that has no children
            while (node != null)
            {
                if (node.Children.Count > 0 && !node.IsNewTrainingMove)
                {
                    TrainingLine.Add(node.Children[0]);
                }
                else
                {
                    break;
                }
            }
        }

    }
}

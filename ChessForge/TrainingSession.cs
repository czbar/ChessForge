using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessForge
{
    /// <summary>
    /// Holds attributes of the current training session. 
    /// </summary>
    public partial class TrainingSession
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
        /// How the training lines are sequenced.
        /// </summary>
        public enum SequenceType
        {
            // Goes from line to line in order starting from the current move.
            METHODIC_CURRENT_MOVE,

            // Goes from line to line in order from the starting position.
            METHODIC_STARTING_POSITION,
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
        /// The current sequence type for training lines.
        /// </summary>
        public static SequenceType CurrentSequenceType = SequenceType.METHODIC_CURRENT_MOVE;

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
        /// Prepares the GUI for training mode.
        /// These actions will be performed when the user starts or restarts a training session.
        /// On restart some of them may be spurious but performance is not an issue here.
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="isContinuousEvaluation"></param>
        public static void PrepareGuiForTraining(TreeNode startNode, bool isContinuousEvaluation = false)
        {
            if (AppState.MainWin.ActiveVariationTree == null || startNode == null)
            {
                return;
            }

            AppLog.Message("PrepareGuiForTraining()");

            // Set up the training mode
            AppState.MainWin.StopEvaluation(true);
            AppState.MainWin.StopReplayIfActive();

            LearningMode.ChangeCurrentMode(LearningMode.Mode.TRAINING);
            IsTrainingInProgress = true;
            StartPosition = startNode;
            ChangeCurrentState(TrainingSession.State.AWAITING_USER_TRAINING_MOVE);

            AppState.EnableNavigationArrows();

            if (isContinuousEvaluation)
            {
                IsContinuousEvaluation = true;
            }
            else
            {
                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);
            }

            LearningMode.TrainingSideCurrent = startNode.ColorToMove;
            AppState.MainWin.MainChessBoard.DisplayPosition(startNode, true);

            AppState.ShowMoveEvaluationControls(isContinuousEvaluation, isContinuousEvaluation);
            AppState.ShowExplorers(false, false);
            AppState.MainWin.BoardCommentBox.TrainingSessionStart();

            RemoveTrainingMoves(startNode);
            InitializeRandomLines();
        }

        /// <summary>
        /// Removes all training moves below the specified node.
        /// There should be at most one training node child under the node.
        /// </summary>
        /// <param name="nd"></param>
        public static void RemoveTrainingMoves(TreeNode nd)
        {
            if (nd != null)
            {
                foreach (TreeNode child in nd.Children)
                {
                    if (child.IsNewTrainingMove)
                    {
                        AppState.MainWin.ActiveVariationTree.DeleteRemainingMoves(child);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Returns the first node in the training line that follows the passed node.
        /// </summary>
        /// <param name="currNode"></param>
        /// <returns></returns>
        public static TreeNode GetNextTrainingLineMove(TreeNode currNode)
        {
            TreeNode nextNode = null;

            int index = TrainingLine.IndexOf(currNode);

            if (index >= 0 && index < TrainingLine.Count - 1)
            {
                nextNode = TrainingLine[index + 1];
            }

            return nextNode;
        }

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

                // if we are restarting in the middle of training rather than
                // at the very beginning , we need to remove moves after the StartPosition.
                if (nd == StartPosition)
                {
                    EngineGame.Line.RollbackToNode(i);
                    break;
                }
            }

            CompleteTrainingLine();
        }

        /// <summary>
        /// Builds the next training line.
        /// </summary>
        public static TreeNode BuildNextTrainingLine()
        {
            return BuildNextPrevTrainingLine(true);
        }

        /// <summary>
        /// Builds the previous training line.
        /// </summary>
        public static TreeNode BuildPreviousTrainingLine()
        {
            return BuildNextPrevTrainingLine(false);
        }

        /// <summary>
        /// This will be called when the user made a move that was not in the current training line.
        /// We will need to adjust the training line to insert the new move in the right
        /// place and build the rest of the updated training line moves from there.
        /// Must check if the updated node is already in the training line. If so, do nothing
        /// as we do not want to reset the trailing nodes.
        /// </summary>
        /// <param name="updatedNode"></param>
        public static void AdjustTrainingLine(TreeNode updatedNode)
        {
            if (TrainingLine.IndexOf(updatedNode) >= 0)
            {
                return;
            }

            // the parent of the updated node is the last node that we will keep
            TreeNode lastGoodNode = updatedNode.Parent;
            int index = TrainingLine.IndexOf(lastGoodNode);

            // remove trailing nodes
            TrainingLine.RemoveRange(index + 1, (TrainingLine.Count - index) - 1);

            // appand the updated node
            TrainingLine.Add(updatedNode);

            // complete the training line from the updated node
            CompleteTrainingLine();
        }

        /// <summary>
        /// Identifies a node that will be a junction when switching to the next or previous training line.
        /// </summary>
        /// <param name="nextOrPrevLine"></param>
        /// <returns></returns>
        public static TreeNode FindTrainingLineJunctionNode(bool nextOrPrevLine)
        {
            TreeNode junctionNode = null;

            try
            {
                int index = FindMoveToUpdateIndex(nextOrPrevLine);

                if (index > 0)
                {
                    TreeNode moveToUpdate = TrainingLine[index];
                    int childIndex = moveToUpdate.Parent.Children.IndexOf(moveToUpdate);
                    if (nextOrPrevLine)
                    {
                        junctionNode = moveToUpdate.Parent.Children[childIndex + 1];
                    }
                    else
                    {
                        junctionNode = moveToUpdate.Parent.Children[childIndex - 1];
                    }
                }
            }
            catch
            {
                AppLog.Message(LogLevel.ERROR, "Exception in FindTrainingLineJunctionNode()");
            }

            return junctionNode;
        }

        /// <summary>
        /// Obtains the current game line and starting from the last node, traverses
        /// it to the first node until it finds a node that has a sibling next to it 
        /// (i.e. at the after or before - depending on nextOrPrevLine param -
        /// index in the parent's list).
        /// Once found, replaces the node in the TrainingLine with that sibling
        /// and builds the rest of the training line from that node down 
        /// (following the local main line).
        /// </summary>
        /// <param name="nextOrPrevLine"></param>
        private static TreeNode BuildNextPrevTrainingLine(bool nextOrPrevLine)
        {
            int moveToUpdateIndex = FindMoveToUpdateIndex(nextOrPrevLine);

            if (moveToUpdateIndex < 0)
            {
                return null;
            }

            TrainingLine.Clear();
            // 1. Add all the moves up to the move that is to be updated 
            for (int i = 0; i < moveToUpdateIndex; i++)
            {
                TrainingLine.Add(EngineGame.Line.NodeList[i]);
            }

            // necessary to bring back the engine game line to the move
            EngineGame.Line.RollbackToNode(moveToUpdateIndex);

            // 2. Update the move at the given index
            TreeNode updatedNode = UpdateMoveAtIndex(moveToUpdateIndex, nextOrPrevLine);

            // 3. Add the rest of the training line from the updated move
            CompleteTrainingLine();

            return updatedNode;
        }

        /// <summary>
        /// Replaces the move at the given index with the next sibling.
        /// </summary>
        /// <param name="moveToUpdateIndex"></param>
        private static TreeNode UpdateMoveAtIndex(int moveToUpdateIndex, bool nextOrPrevLine)
        {
            TreeNode updatedNode = null;

            TreeNode moveToUpdate = EngineGame.Line.NodeList[moveToUpdateIndex];
            TreeNode moveToUpdateParent = moveToUpdate.Parent;

            int currChildIndex = moveToUpdateParent.Children.IndexOf(moveToUpdate);

            int childIndex = GetNonNullLeafSiblingIndex(moveToUpdate, nextOrPrevLine);
            if (childIndex >= 0)
            {
                updatedNode = moveToUpdateParent.Children[childIndex];
                if (moveToUpdateIndex > TrainingLine.Count - 1)
                {
                    TrainingLine.Add(updatedNode);
                }
                else
                {
                    TrainingLine[moveToUpdateIndex] = updatedNode;
                }
            }

            return updatedNode;
        }

        /// <summary>
        /// Finds the index of the furthest move in the Workbook
        /// that can be "updated" i.e. has a sibling at a higher Children index.
        /// Traverses the engine game backwards from the last Workbook move
        /// to find an "updateable" one.
        /// </summary>
        /// <returns></returns>
        private static int FindMoveToUpdateIndex(bool nextOrPrevLine)
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

                if (node.ColorToMove == TrainingSide && GetNonNullLeafSiblingIndex(node, nextOrPrevLine) >= 0)
                {
                    moveToUpdateIndex = i;
                    break;
                }
            }

            return moveToUpdateIndex;
        }

        /// <summary>
        /// Finds the index of the next or previous non-null sibling.
        /// Returns -1 if no such sibling is found.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="nextOtPrev"></param>
        /// <returns></returns>
        private static int GetNonNullLeafSiblingIndex(TreeNode node, bool nextOtPrev)
        {
            int index = -1;

            TreeNode parent = node.Parent;

            if (parent != null)
            {
                int idx = parent.Children.IndexOf(node);

                if (idx >= 0)
                {
                    if (nextOtPrev)
                    {
                        for (int i = idx + 1; i <= parent.Children.Count - 1; i++)
                        {
                            if (!MoveUtils.IsNullLeafMove(parent.Children[i]))
                            {
                                index = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (int i = idx - 1; i >= 0; i--)
                        {
                            if (!MoveUtils.IsNullLeafMove(parent.Children[i]))
                            {
                                index = i;
                                break;
                            }
                        }
                    }
                }
            }

            return index;
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
                TreeNode firstNonNullChild = TreeUtils.GetFirstNonNullChild(node);
                if (firstNonNullChild != null && !node.IsNewTrainingMove)
                {
                    node = firstNonNullChild;
                    TrainingLine.Add(node);
                }
                else
                {
                    break;
                }
            }
        }
    }
}

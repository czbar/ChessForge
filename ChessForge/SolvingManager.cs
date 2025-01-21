using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using ChessPosition;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Monitors the solving process.
    /// </summary>
    public class SolvingManager
    {
        // Quiz points available in the main line.
        private int _mainQuizPoints = 0;

        // Quiz points available in sidelines.
        private int _sideLineQuizPoints = 0;

        // Points scored in the solution.
        private int _pointsScored = 0;

        /// <summary>
        /// Quiz points available in the main line.
        /// </summary>
        public int MainQuizPoints
        {
            get => _mainQuizPoints;
            set => _mainQuizPoints = value;
        }

        /// <summary>
        /// Quiz points available in sidelines.
        /// </summary>
        public int SideLineQuizPoints
        {
            get => _sideLineQuizPoints;
            set => _sideLineQuizPoints = value;
        }

        /// <summary>
        /// Points scored in the solution.
        /// </summary>
        public int PointsScored
        {
            get => _pointsScored;
            set => _pointsScored = value;
        }

        // whether the guessing exercise is finished
        private bool _guessingFinished = false;

        /// <summary>
        // Whether the solving has started yet
        /// </summary>
        public bool SolvingStarted { get; set; }

        /// <summary>
        /// Whether solution has been submitted.
        /// </summary>
        public bool IsAnalysisSubmitted { get; set; }

        /// <summary>
        /// Whether the solving has been completed
        /// </summary>
        public bool IsSolvingFinished
        {
            get
            {
                if (AppState.MainWin.ActiveVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.GUESS_MOVE
                    && IsGuessingFinished)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// While solving the moves are allowed when the Tree is showing lines
        /// plus, if in GUESS mode, when the guessing side is on the move and the
        /// current move is last in ActiveLine
        /// </summary>
        /// <returns></returns>
        public bool IsMovingAllowed()
        {
            if (AppState.MainWin.ActiveVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.GUESS_MOVE)
            {
                bool awaitingMoveinGuessMode =
                    AppState.MainWin.ActiveLine.IsLastMoveSelected()
                    && AppState.MainWin.ActiveLine.GetStartingColor() == AppState.MainWin.ActiveLine.GetLastNode().ColorToMove
                    && !_guessingFinished;
                return awaitingMoveinGuessMode;
            }
            else
            {
                if (AppState.MainWin.ActiveVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.ANALYSIS
                    && IsAnalysisSubmitted)
                {
                    return false;
                }
                else
                {
                    // do not allow moves if tree is not shown unless we went into training
                    return AppState.MainWin.ActiveVariationTree.ShowTreeLines || AppState.CurrentLearningMode == LearningMode.Mode.TRAINING;
                }
            }
        }

        /// <summary>
        /// Returns the current solving mode i.e. the solving mode
        /// of the current active tree.
        /// </summary>
        /// <returns></returns>
        public VariationTree.SolvingMode GetAppSolvingMode()
        {
            if (AppState.MainWin.ActiveVariationTree != null)
            {
                return AppState.MainWin.ActiveVariationTree.CurrentSolvingMode;
            }

            return VariationTree.SolvingMode.NONE;
        }

        /// <summary>
        /// Which side is doing the solving.
        /// The user will be prevented from making moves for the other side.
        /// </summary>
        public PieceColor SolvingSide
        {
            get
            {
                return AppState.MainWin.ActiveVariationTree.RootNode.ColorToMove;
            }
        }

        /// <summary>
        /// Whether the guessing exercise is finished
        /// </summary>
        public bool IsGuessingFinished
        {
            get => _guessingFinished;
            set => _guessingFinished = value;
        }

        /// <summary>
        /// This must be called when the user made the move and the move was added to the ActiveLine.
        /// It should not be added yet to the view but we will make a defensive check for that in this method.
        /// If the move agrees with the Workbook, it will be added to the view with a check mark.
        /// If not the comment for the previous move will be updated with the red cross and a note.
        /// </summary>
        public void ProcessUserMoveInGuessMode()
        {
            SolvingStarted = true;
            try
            {
                VariationTree secondaryTree = AppState.MainWin.ActiveVariationTree;
                if (secondaryTree != null && secondaryTree.AssociatedPrimary != null)
                {
                    // get the last move from active line
                    TreeNode guess = AppState.MainWin.ActiveLine.GetLastNode();
                    TreeNode inPrimaryTree = secondaryTree.AssociatedPrimary.FindIdenticalNode(guess, true);
                    // must be the first child
                    if (inPrimaryTree == null || inPrimaryTree.Parent.Children[0].NodeId != inPrimaryTree.NodeId)
                    {
                        HandleIncorrectGuess(guess, secondaryTree);
                    }
                    else
                    {
                        // clear previous comments on the non moving side
                        foreach (TreeNode prevNode in secondaryTree.Nodes)
                        {
                            if (prevNode.ColorToMove != guess.ColorToMove)
                            {
                                prevNode.Comment = "";
                            }
                        }

                        // report the correct move 
                        guess.Comment = Constants.CharCheckMark.ToString();

                        // now make the move for the Workbook
                        if (inPrimaryTree.Children.Count == 0)
                        {
                            _guessingFinished = true;
                        }
                        else
                        {
                            HandleCorrectGuess(guess, inPrimaryTree, secondaryTree);
                        }
                    }

                    //TODO: optimize as we only need to update solution paragraph
                    AppState.MainWin.Dispatcher.Invoke(() =>
                    {
                        AppState.MainWin.ActiveTreeView.BuildFlowDocumentForVariationTree(false);
                    });
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("ProcessUserMoveInGuessMode()", ex);
            }
        }

        /// <summary>
        /// Calculates quiz points available in the exercise.
        /// The "main line" points are those specified for the moves of the
        /// solving side where the move is the first child of its parent (LineId is "1").
        /// The "side line" points are those specified for the moves of the
        /// solving side where the move is not the first child of its parent i.e.
        /// the solver must find alternative response from the opponent.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="sideLine"></param>
        /// <returns></returns>
        public int CalculateAvailableQuizPoints(VariationTree tree)
        {
            PieceColor sideToMove = tree.RootNode.ColorToMove;
            TreeNode node = tree.RootNode;
            int mainPoints = 0;
            CountQuizPoints(node, sideToMove, ref _mainQuizPoints, ref _sideLineQuizPoints);

            return mainPoints;
        }

        /// <summary>
        /// Resets all quiz related data.
        /// </summary>
        public void ResetQuizPoints()
        {
            _mainQuizPoints = 0;
            _sideLineQuizPoints = 0;
            _pointsScored = 0;
        }

        /// <summary>
        /// Recursively count available quiz points.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="sideToMove"></param>
        /// <param name="main"></param>
        /// <param name="side"></param>
        private void CountQuizPoints(TreeNode node, PieceColor sideToMove, ref int main, ref int side)
        {
            if (node.Parent != null && node.Parent.ColorToMove == sideToMove && node.QuizPoints != 0)
            {
                if (node.IsMainLine())
                {
                    main += node.QuizPoints;
                }
                else
                {
                    side += node.QuizPoints;
                }
            }
            foreach (TreeNode child in node.Children)
            {
                CountQuizPoints(child, sideToMove, ref main, ref side);
            }
        }

        /// <summary>
        /// Handles the correct user's guess.
        /// </summary>
        /// <param name="guess"></param>
        /// <param name="inPrimaryTree"></param>
        /// <param name="secondaryTree"></param>
        private void HandleCorrectGuess(TreeNode guess, TreeNode inPrimaryTree, VariationTree secondaryTree)
        {
            try
            {
                // clear previous comments on the non moving side
                foreach (TreeNode prevNode in secondaryTree.Nodes)
                {
                    if (prevNode.ColorToMove != guess.ColorToMove)
                    {
                        prevNode.Comment = "";
                    }
                }

                // report the correct move 
                guess.Comment = Constants.CharCheckMark.ToString();

                // now make the move for the Workbook
                if (inPrimaryTree.Children.Count == 0)
                {
                    _guessingFinished = true;
                    AppState.MainWin.ActiveVariationTree.SetSelectedNodeId(guess.NodeId);
                }
                else
                {
                    TreeNode response = inPrimaryTree.Children[0].CloneMe(true);
                    AppState.MainWin.Dispatcher.Invoke(() =>
                    {
                        SoundPlayer.PlayMoveSound(response.LastMoveAlgebraicNotation);
                        response.Parent = guess;
                        guess.Children.Add(response);
                        secondaryTree.AddNode(response);
                        AppState.MainWin.ActiveLine.Line.AddPlyAndMove(response);
                        AppState.MainWin.ActiveLine.SelectPly((int)response.Parent.MoveNumber, response.Parent.ColorToMove);
                        AppState.MainWin.DisplayPosition(response);
                    });
                    AppState.MainWin.ActiveVariationTree.SetSelectedNodeId(response.NodeId);

                    if (inPrimaryTree.Children[0].Children.Count == 0)
                    {
                        _guessingFinished = true;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("HandleCorrectGuess()", ex);
            }
        }

        /// <summary>
        /// Handles the incorrect user's guess.
        /// </summary>
        /// <param name="guess"></param>
        /// <param name="secondaryTree"></param>
        private void HandleIncorrectGuess(TreeNode guess, VariationTree secondaryTree)
        {
            //TODO: make sure that secondary tree gets move number offset from primary
            // report incorrect move and (defensively) remove from the view it is there
            guess.Parent.Comment = Constants.CharCrossMark.ToString() + " " + MoveUtils.BuildSingleMoveText(guess, true, false, secondaryTree.MoveNumberOffset) + " is not correct.";
            AppState.MainWin.ActiveVariationTree.SetSelectedNodeId(guess.Parent.NodeId);
            AppState.MainWin.ActiveLine.Line.RemoveLastPly();

            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                secondaryTree.DeleteRemainingMoves(guess);
                TreeNode lastNode = AppState.MainWin.ActiveLine.GetLastNode();
                if (lastNode.Parent != null)
                {
                    AppState.MainWin.ActiveLine.SelectPly((int)lastNode.Parent.MoveNumber, lastNode.Parent.ColorToMove);
                }
                else
                {
                    // select node 0
                    int moveNumber = lastNode.ColorToMove == PieceColor.White ? 0 : 1;
                    AppState.MainWin.ActiveLine.SelectPly(moveNumber, MoveUtils.ReverseColor(lastNode.ColorToMove));
                }

                AppState.MainWin.DisplayPosition(guess.Parent);
            });
            SoundPlayer.PlayWrongMoveSound();
        }

    }
}

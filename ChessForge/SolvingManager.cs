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
        // whether the guessing exercise is finished
        private bool _guessingFinished = false;

        // whether the solving has started yet
        public bool SolvingStarted { get; set; } 

        /// <summary>
        /// While solving the moves are allowed when the Tree is showing lines
        /// plus, if in GUESS mode, when the guessing side is on the move and the
        /// current move is last in ActiveLine
        /// </summary>
        /// <returns></returns>
        public bool IsMovingAllowed()
        {
            if (AppStateManager.MainWin.ActiveVariationTree.CurrentSolvingMode != VariationTree.SolvingMode.GUESS_MOVE)
            {
                return AppStateManager.MainWin.ActiveVariationTree.ShowTreeLines;
            }
            else
            {
                return
                AppStateManager.MainWin.ActiveLine.IsLastMoveSelected()
                    && AppStateManager.MainWin.ActiveLine.GetStartingColor() == AppStateManager.MainWin.ActiveLine.GetLastNode().ColorToMove
                    && !_guessingFinished;

                //TODO or there are no more moves
            }
        }

        /// <summary>
        /// Returns the current solving mode i.e. the solving mode
        /// of the current active tree.
        /// </summary>
        /// <returns></returns>
        public VariationTree.SolvingMode GetAppSolvingMode()
        {
            if (AppStateManager.MainWin.ActiveVariationTree != null)
            {
                return AppStateManager.MainWin.ActiveVariationTree.CurrentSolvingMode;
            }

            return VariationTree.SolvingMode.NONE;
        }

        /// <summary>
        /// Which side is doing the solving.
        /// The user will be prevented from making moved for the other side.
        /// </summary>
        public PieceColor SolvingSide
        {
            get
            {
                return AppStateManager.MainWin.ActiveVariationTree.RootNode.ColorToMove;
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

            VariationTree secondaryTree = AppStateManager.MainWin.ActiveVariationTree;
            if (secondaryTree != null && secondaryTree.AssociatedPrimary != null)
            {
                // get the last move from active line
                TreeNode guess = AppStateManager.MainWin.ActiveLine.GetLastNode();
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
                AppStateManager.MainWin.Dispatcher.Invoke(() =>
                {
                    AppStateManager.MainWin.ActiveTreeView.BuildFlowDocumentForVariationTree();
                });
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
                TreeNode response = inPrimaryTree.Children[0].CloneMe(true);
                response.Parent = guess;
                guess.Children.Add(response);
                secondaryTree.AddNode(response);
                AppStateManager.MainWin.ActiveLine.Line.AddPlyAndMove(response);
                AppStateManager.MainWin.Dispatcher.Invoke(() =>
                {
                    AppStateManager.MainWin.ActiveLine.SelectPly((int)response.Parent.MoveNumber, response.Parent.ColorToMove);
                    AppStateManager.MainWin.DisplayPosition(response);
                    // AppStateManager.MainWin.ActiveTreeView.SelectLineAndMove("", response.NodeId);
                });

                if (inPrimaryTree.Children[0].Children.Count == 0)
                {
                    _guessingFinished = true;
                }
            }
        }

        /// <summary>
        /// Handles the incorrect user's guess.
        /// </summary>
        /// <param name="guess"></param>
        /// <param name="secondaryTree"></param>
        private void HandleIncorrectGuess(TreeNode guess, VariationTree secondaryTree)
        {
            // report incorrect move and (defensively) remove from the view it is there
            guess.Parent.Comment = Constants.CharCrossMark.ToString() + " " + MoveUtils.BuildSingleMoveText(guess, true) + " is not correct.";
            AppStateManager.MainWin.ActiveLine.Line.RemoveLastPly();
            AppStateManager.MainWin.Dispatcher.Invoke(() =>
            {
                secondaryTree.DeleteRemainingMoves(guess);
                TreeNode lastNode = AppStateManager.MainWin.ActiveLine.GetLastNode();
                if (lastNode.Parent != null)
                {
                    AppStateManager.MainWin.ActiveLine.SelectPly((int)lastNode.Parent.MoveNumber, lastNode.Parent.ColorToMove);
                }
                else
                {
                    // select node 0
                    int moveNumber = lastNode.ColorToMove == PieceColor.White ? 0 : 1;
                    AppStateManager.MainWin.ActiveLine.SelectPly(moveNumber, MoveUtils.ReverseColor(lastNode.ColorToMove));
                }

                AppStateManager.MainWin.DisplayPosition(guess.Parent);
            });
            SoundPlayer.PlayWrongMoveSound();
        }

    }
}

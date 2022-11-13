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
    public class SolvingManager
    {
        /// <summary>
        /// Returns false if the lines are currently hidden.
        /// </summary>
        /// <returns></returns>
        public static bool IsMovingAllowed()
        {
            return AppStateManager.MainWin.ActiveVariationTree.ShowTreeLines;
        }

        /// <summary>
        /// Returns the current solving mode i.e. the solving mode
        /// of the current active tree.
        /// </summary>
        /// <returns></returns>
        public static VariationTree.SolvingMode GetAppSolvingMode()
        {
            if (AppStateManager.MainWin.ActiveVariationTree != null)
            {
                return AppStateManager.MainWin.ActiveVariationTree.CurrentSolvingMode;
            }

            return VariationTree.SolvingMode.NONE;
        }

        /// <summary>
        /// This must be called when the user made the move and the move was added to the ActiveLine.
        /// It should not be added yet to the view but we will make a defensive check for that in this method.
        /// If the move agrees with the Workbook, it will be added to the view with a check mark.
        /// If not the comment for the previous move will be updated with the red cross and a note.
        /// </summary>
        public static void ProcessUserMoveInGuessMode()
        {
            VariationTree secondaryTree = AppStateManager.MainWin.ActiveVariationTree;
            if (secondaryTree != null && secondaryTree.AssociatedPrimary != null)
            {
                // get the last move from active line
                TreeNode guess = AppStateManager.MainWin.ActiveLine.GetLastNode();
                TreeNode inPrimaryTree = secondaryTree.AssociatedPrimary.FindIdenticalNode(guess, true);
                // must be the first child
                if (inPrimaryTree == null || inPrimaryTree.Parent.Children[0].NodeId != inPrimaryTree.NodeId)
                {
                    // report incorrect move and (defensively) remove from the view it is there
                    guess.Parent.Comment = Constants.CharCrossMark.ToString() + " " + MoveUtils.BuildSingleMoveText(guess, true) + " is not correct.";
                    AppStateManager.MainWin.ActiveLine.Line.RemoveLastPly();
                    AppStateManager.MainWin.Dispatcher.Invoke(() =>
                    {
                        secondaryTree.DeleteRemainingMoves(guess);
                        AppStateManager.MainWin.DisplayPosition(guess.Parent);
                    });
                }
                else
                {
                    // report the correct move 
                    guess.Comment = Constants.CharCheckMark.ToString();
                    
                    // now make the move for the Workbook
                    if (inPrimaryTree.Children.Count == 0)
                    {
                        // this is the end TODO: announce
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
                            //                        AppStateManager.MainWin.ActiveTreeView.SelectLineAndMove("", response.NodeId);
                        });

                        // TODO: check if this an unexpected end
                    }
                }

                //TODO: optimize as we only need to update solution paragraph
                AppStateManager.MainWin.Dispatcher.Invoke(() =>
                {
                    AppStateManager.MainWin.ActiveTreeView.BuildFlowDocumentForVariationTree();
                });


            }

        }
    }
}

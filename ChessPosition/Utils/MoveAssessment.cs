using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessPosition.Utils
{
    /// <summary>
    /// Utilities for assessing the quality of a move,
    /// in particular whether it is blunder.
    /// </summary>
    public class MoveAssessment
    {
        /// <summary>
        /// Determines the assessment of the passed move by comparing its evaluation
        /// against the evaluation of the parent.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public static ChfCommands.Assessment GetMoveAssessment(TreeNode nd)
        {
            ChfCommands.Assessment ass = ChfCommands.Assessment.NONE;
            if (!IsPotentialBlunder(nd))
            {
                return ChfCommands.Assessment.NONE;
            }

            // check if the parent had a checkmate by us but we have relatively low eval
            if (IsMateByColor(nd.Parent.EngineEvaluation, nd.Parent.ColorToMove))
            {
                // we know that we don't have a checkmate in the child node (that was filtered above)
                // so the eval  must parse unless something got corrupted.
                if (double.TryParse(nd.EngineEvaluation, out double eval))
                {
                    if (nd.Parent.ColorToMove == PieceColor.Black && eval > -5.0
                        || nd.Parent.ColorToMove == PieceColor.White && eval < 5.0)
                    {
                        // eval dropped from mate to less than 5, so we consider this a blunder
                        ass = ChfCommands.Assessment.BLUNDER;
                    }
                }
            }
            // if we now have mate by our opponent but parent's eval was not so bad.
            else if (IsMateByColor(nd.EngineEvaluation, nd.ColorToMove))
            {
                if (double.TryParse(nd.EngineEvaluation, out double eval))
                {
                    if (nd.Parent.ColorToMove == PieceColor.White && eval > -5.0
                        || nd.Parent.ColorToMove == PieceColor.Black && eval < 5.0)
                    {
                        ass = ChfCommands.Assessment.BLUNDER;
                    }
                }
            }
            else
            {
                // now we have 2 numerical evals, assess the difference
                if (double.TryParse(nd.Parent.EngineEvaluation, out double evalParent))
                {
                    if (double.TryParse(nd.EngineEvaluation, out double evalChild))
                    {
                        if (IsBlunder(evalParent, evalChild, nd.ColorToMove))
                        {
                            ass = ChfCommands.Assessment.BLUNDER;
                        }
                    }
                }
            }

            return ass;
        }

        /// <summary>
        /// Eliminates obvious cases when we definitely can't have a blunder.  
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private static bool IsPotentialBlunder(TreeNode nd)
        {
            bool res = false;

            // first of all we need to have evals
            if (nd != null && nd.Parent != null && !string.IsNullOrEmpty(nd.EngineEvaluation) && !string.IsNullOrEmpty(nd.Parent.EngineEvaluation))
            {
                // parent's eval as checkmate by the opponent prevents evaluating a move as a blunder (since we don't care if the mate comes sooner)
                if (!IsMateByColor(nd.Parent.EngineEvaluation, nd.ColorToMove))
                {
                    res = true;
                }
                // current (child) node eval as checkmate by us cannot be a blunder
                else if (!IsMateByColor(nd.EngineEvaluation, nd.Parent.ColorToMove))
                {
                    res = true;
                }
            }

            return res;
        }

        /// <summary>
        /// Checks whether the evaluation string represents checkmate
        /// by the side specified by color.
        /// </summary>
        /// <param name="eval"></param>
        /// <param name="color">The side for which we are checking the mating evaluation.</param>
        /// <returns></returns>
        private static bool IsMateByColor(string eval, PieceColor color)
        {
            bool res = false;

            if (color == PieceColor.White)
            {
                // for White, it could start with # or +# so hedge our bets
                if (eval.Contains("#") && !eval.Contains("-#"))
                {
                    res = true;
                }
            }
            else
            {
                if (eval.Contains("-#"))
                {
                    res = true;
                }
            }

            return res;
        }

        /// <summary>
        /// Check if the difference in numeric evaluations of the parent
        /// and the current (child) node justify calling the child's move
        /// a blunder.
        /// </summary>
        /// <param name="parentEval"></param>
        /// <param name="childEval"></param>
        /// <param name="color">Color to move in the child node</param>
        /// <returns></returns>
        private static bool IsBlunder(double parentEval, double childEval, PieceColor color)
        {
            bool res = false;

            if (color == PieceColor.White)
            {
                if (childEval - parentEval > 2.0 || parentEval > 0.5 && childEval < -0.8)
                {
                    res = true;
                }
            }
            else
            {
                if (childEval - parentEval < -2.0 || parentEval < -0.5 && childEval > 0.8)
                {
                    res = true;
                }
            }
            return res;
        }
    }
}

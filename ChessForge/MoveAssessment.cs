using ChessForge;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessPosition.Utils
{
    /// <summary>
    /// Utilities for assessing the quality of a move,
    /// in particular whether it is blunder.
    /// </summary>
    public class MoveAssessment
    {
        // bad eval above beyond which we no longer care about blunders 
        private static double IGNORE_BLUNDER_THRESHOLD = 5;

        // the drop in evaluation that triggers blunder determination.
        private static double BLUNDER_EVAL_DETECTION_DIFF = 2;

        /// <summary>
        /// Determines the assessment of the passed move by comparing its evaluation
        /// against the evaluation of the parent.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public static ChfCommands.Assessment GetMoveAssessment(TreeNode nd)
        {
            BLUNDER_EVAL_DETECTION_DIFF = ((double)Configuration.BlunderDetectEvalDrop)/100;
            IGNORE_BLUNDER_THRESHOLD = ((double)Configuration.BlunderNoDetectThresh) / 100;

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
                    if (nd.Parent.ColorToMove == PieceColor.Black && eval > -IGNORE_BLUNDER_THRESHOLD
                        || nd.Parent.ColorToMove == PieceColor.White && eval < +IGNORE_BLUNDER_THRESHOLD)
                    {
                        // eval dropped from mate to less than MATE_TO_LOW_ADV, so we consider this a blunder
                        ass = ChfCommands.Assessment.BLUNDER;
                    }
                }
            }
            // if we now have a mate by our opponent but parent's eval was not so bad.
            else if (IsMateByColor(nd.EngineEvaluation, nd.ColorToMove))
            {
                if (double.TryParse(nd.Parent.EngineEvaluation, out double evalParent))
                {
                    if (nd.Parent.ColorToMove == PieceColor.White && evalParent > -IGNORE_BLUNDER_THRESHOLD
                        || nd.Parent.ColorToMove == PieceColor.Black && evalParent < +IGNORE_BLUNDER_THRESHOLD)
                    {
                        ass = ChfCommands.Assessment.BLUNDER;
                    }
                }
            }
            else
            {
                // now we have two numerical evals, assess the difference
                if (double.TryParse(nd.Parent.EngineEvaluation, out double evalParent))
                {
                    if (double.TryParse(nd.EngineEvaluation, out double evalChild))
                    {
                        if (IsBlunder(evalParent, evalChild, nd.Parent.ColorToMove))
                        {
                            ass = ChfCommands.Assessment.BLUNDER;
                        }
                    }
                }
            }

            return ass;
        }

        /// <summary>
        /// Builds the string with a symbol representing the assessment value.
        /// </summary>
        /// <param name="ass"></param>
        /// <returns></returns>
        public static string AssesssmentSymbol(uint ass)
        {
            string symbol = "";

            switch ((ChfCommands.Assessment)ass)
            {
                case ChfCommands.Assessment.BLUNDER:
                    symbol = "??";
                    break;
                case ChfCommands.Assessment.MISTAKE:
                    symbol = "?";
                    break;
            }

            return symbol;
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
        /// <param name="prevEval"></param>
        /// <param name="currEval"></param>
        /// <returns></returns>
        private static bool IsBlunder(double prevEval, double currEval, PieceColor colorToMove)
        {
            bool res = false;

            if (colorToMove == PieceColor.White)
            {
                if ((currEval - prevEval < -BLUNDER_EVAL_DETECTION_DIFF) && ((Math.Abs(prevEval) <= IGNORE_BLUNDER_THRESHOLD) || Math.Abs(currEval) <= IGNORE_BLUNDER_THRESHOLD))
                {
                    res = true;
                }
            }
            else
            {
                if ((currEval - prevEval > BLUNDER_EVAL_DETECTION_DIFF) && ((Math.Abs(prevEval) <= IGNORE_BLUNDER_THRESHOLD) || Math.Abs(currEval) <= IGNORE_BLUNDER_THRESHOLD))
                {
                    res = true;
                }
            }

            return res;
        }

    }
}

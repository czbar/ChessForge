using ChessPosition;
using System;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates all info from the engine for
    /// a single candidate move (first move in the "Line"). 
    /// </summary>
    public class MoveEvaluation : IComparable<MoveEvaluation>
    {
        /// <summary>
        /// The string with moves from the eninge single
        /// evaluation line e.g. "e2e4 e7e5 g1f3 b8c6"
        /// </summary>
        public string Line = "";

        /// <summary>
        /// Engine evaluation score in centipawns.
        /// It is not valid if IsMateDetected == true. 
        /// </summary>
        public int ScoreCp;

        /// <summary>
        /// Depth of the search in plies.
        /// </summary>
        public int Depth;

        /// <summary>
        /// Selective depth of the search in plies.
        /// </summary>
        public int SelDepth;

        /// <summary>
        /// True if the evaluation is checkmate
        /// </summary>
        public bool IsMateDetected;

        /// <summary>
        /// Number of moves (not half-moves/plies)
        /// to checkmate.
        /// Only valid if IsMateDetected == true;
        /// </summary>
        public int MovesToMate;

        /// <summary>
        /// Position for which this evaluation applies
        /// (NOT USED currently)
        /// </summary>
        public string Fen = "";

        /// <summary>
        /// Side to move in the position being evaluated
        /// (NOT USED currently)
        /// </summary>
        public PieceColor Color;

        /// <summary>
        /// Number of the move to be made in the position
        /// (NOT USED currently)
        /// </summary>
        public int MoveNumber;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MoveEvaluation()
        {}

        /// <summary>
        /// Copy constructor;
        /// </summary>
        /// <param name="moveEval"></param>
        public MoveEvaluation(MoveEvaluation moveEval, PieceColor color)
        {
            this.Color = color;
            this.Fen = string.Copy(moveEval.Fen);
            this.Line = string.Copy(moveEval.Line);
            this.MoveNumber = moveEval.MoveNumber;
            this.ScoreCp = moveEval.ScoreCp;
            this.Depth = moveEval.Depth;
            this.SelDepth = moveEval.SelDepth;
            this.IsMateDetected = moveEval.IsMateDetected;
            this.MovesToMate = moveEval.MovesToMate;
        }

        /// <summary>
        /// Compares 2 move evaluations.
        /// Detected mate wins over ScoreCp. If both are mates then
        /// the lower number of moves to mate wins.
        /// </summary>
        /// <param name="mev"></param>
        /// <returns></returns>
        public int CompareTo(MoveEvaluation mev)
        {
            int res;

            if (mev == null)
            {
                return -1;
            }

            if (this.IsMateDetected)
            {
                if (mev.IsMateDetected)
                {
                    if (this.MovesToMate >= 0 && mev.MovesToMate >= 0 || this.MovesToMate < 0 && mev.MovesToMate < 0)
                    {
                        res = this.MovesToMate - mev.MovesToMate;
                    }
                    else
                    {
                        res = this.MovesToMate >= 0 ? -1 : 1;
                    }
                }
                else
                {
                    res = -1;
                    if (this.MovesToMate < 0)
                    {
                        res = 1;
                    }
                }
            }
            else if (mev.IsMateDetected)
            {
                res = 1;
                if (mev.MovesToMate < 0)
                {
                    res = -1;
                }
            }
            else
            {
                res = mev.ScoreCp - this.ScoreCp;
            }

            return res;
        }

        /// <summary>
        /// Returns the first move in the Line string which is the move
        /// for which the evaluation applies.
        /// </summary>
        /// <returns></returns>
        public string GetCandidateMove()
        {
            string[] moves = Line.Split(' ');
            return moves.Length == 0 ? "" : moves[0];
        }
    }
}

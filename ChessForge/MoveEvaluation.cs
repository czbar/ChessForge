using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessPosition;

namespace ChessForge
{
    public class MoveEvaluation
    {
        public MoveEvaluation()
        {
        }

        public MoveEvaluation(MoveEvaluation moveEval)
        {
            this.Fen = string.Copy(moveEval.Fen);
            this.Line = string.Copy(moveEval.Line);
            this.MoveNumber = moveEval.MoveNumber;
            this.Color = moveEval.Color;
            this.ScoreCp = moveEval.ScoreCp;
        }

        public string Fen = "";
        public string Line = "";
        public int MoveNumber;
        public PieceColor Color;
        public int ScoreCp;

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

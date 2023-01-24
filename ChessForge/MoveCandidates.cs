using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// A structure to hold the evaluated node together with the engine evaluation lines.
    /// </summary>
    public class MoveCandidates
    {
        /// <summary>
        /// Node for which the evaluation lines were produced.
        /// </summary>
        public TreeNode EvalNode{ get; set; }

        /// <summary>
        /// Evaluation lines.
        /// </summary>
        public List<MoveEvaluation> Lines = new List<MoveEvaluation>();

        /// <summary>
        /// Adds a line to the Lines list
        /// </summary>
        /// <param name="eval"></param>
        public void AddEvaluation(MoveEvaluation eval)
        {
            Lines.Add(eval);
        }

        /// <summary>
        /// Clears the list of lines.
        /// </summary>
        public void Clear()
        {
            Lines.Clear();
        }
    }
}

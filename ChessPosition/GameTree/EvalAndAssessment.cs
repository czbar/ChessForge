using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameTree
{
    /// <summary>
    /// Holds node Id along with the Engine Evals and Assessments.
    /// This was created to supported Undo for DeleteEngineEvals
    /// </summary>
    public class EvalAndAssessment
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public EvalAndAssessment()
        {
        }
        /// <summary>
        /// Constructor setting the properties.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="comment"></param>
        /// <param name="nags"></param>
        public EvalAndAssessment(int nodeId, string eval, uint assess)
        {
            NodeId = nodeId;
            EngineEvaluation = eval;
            Assessment = assess;
        }

        /// <summary>
        /// Node id.
        /// </summary>
        public int NodeId;

        /// <summary>
        /// Engine Evaluation.
        /// </summary>
        public string EngineEvaluation;

        /// <summary>
        /// Assessment code.
        /// </summary>
        public uint Assessment;
    }
}

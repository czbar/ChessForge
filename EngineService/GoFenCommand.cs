using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineService
{
    /// <summary>
    /// Holds the data for the "go" and "position" commands to be sent
    /// one after another.
    /// Associates FEN with the NodeId.
    /// </summary>
    public class GoFenCommand
    {
        /// <summary>
        /// Evaluation mode passed by the caller
        /// to be returned with the result.
        /// </summary>
        public enum EvaluationMode
        {
            NONE = 0,
            CONTINUOUS = 1,
            LINE = 2,
            GAME = 3
        }

        /// <summary>
        /// Evaluation mode to be returned back
        /// to the caller so it does not confused
        /// if it gets back evaluation for a node
        /// but it is already in a game mode
        /// </summary>
        public EvaluationMode EvalMode;

        /// <summary>
        /// The Fen string to be sent with the position command
        /// </summary>
        public string Fen { get; set; }

        /// <summary>
        /// The string to be sent after the position comment
        /// </summary>
        public string GoCommandString { get; set; }


        /// <summary>
        /// Id of the tree from which the Node 
        /// under evaluation comes.
        /// </summary>
        public int TreeId { get; set; }

        /// <summary>
        /// Id of the Tree Node for which we will be sending 
        /// the "position fen" command.
        /// </summary>
        public int NodeId { get; set; }

        /// <summary>
        /// Number of options to return.
        /// Ignore if set to 0 or < 0.
        /// </summary>
        public int Mpv {get; set; }

        /// <summary>
        /// Constructor.
        /// Initializes NodeId to -1 as it may never get set
        /// to meaningful value.
        /// </summary>
        public GoFenCommand()
        {
            TreeId = -1;
            NodeId = -1;
        }
    }
}

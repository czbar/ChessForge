using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessPosition
{
    /// <summary>
    /// Event arguments for the Move Evaluation event.
    /// </summary>
    public class MoveEvalEventArgs : EventArgs
    {
        /// <summary>
        /// Index of the move on the list being evaluated
        /// </summary>
        public int MoveIndex { get; set; }

        /// <summary>
        /// Whether the evaluated move was last in the evalauted line
        /// </summary>
        public bool IsLastMove{ get; set; }

        /// <summary>
        /// Id of the tree to which the handled Node belongs
        /// </summary>
        public int TreeId { get; set; }

        /// <summary>
        /// Id of the Node being handled.
        /// </summary>
        public int NodeId { get; set; }

        /// <summary>
        /// Lichess Id of a game 
        /// </summary>
        public string GameId { get; set; }
    }
}

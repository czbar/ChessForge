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
        /// The Fen string to be sent with the position command
        /// </summary>
        public string Fen { get; set; }

        /// <summary>
        /// The string to be sent after the position comment
        /// </summary>
        public string GoCommandString { get; set; }

        /// <summary>
        /// The Id of the Workbook node for which we will be sending 
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
            NodeId = -1;
        }
    }
}

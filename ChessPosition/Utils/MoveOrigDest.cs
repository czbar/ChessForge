using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessPosition.Utils
{
    /// <summary>
    /// A convenience class holding
    /// the move's origin and destination coordinates.
    /// </summary>
    public struct MoveOrigDest
    {
        /// <summary>
        /// Move's origin coordinates.
        /// </summary>
        public SquareCoords Origin;

        /// <summary>
        /// Move's destination coordinates.
        /// </summary>
        public SquareCoords Destination;

        /// <summary>
        /// Construct the object and sets the coordinates.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="dest"></param>
        public MoveOrigDest(SquareCoords orig, SquareCoords dest)
        {
            Origin = orig;
            Destination = dest;
        }
    }
}

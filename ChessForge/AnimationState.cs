using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Holds the state of a move animation in progress
    /// </summary>
    public class AnimationState
    {
        /// <summary>
        /// The duration of time that the animation should take
        /// (in milliseconds).
        /// </summary>
        public int MoveDuration = 250; 

        /// <summary>
        /// Coordinates of the square the piece is moving to.
        /// </summary>
        public SquareCoords Destination;

        /// <summary>
        /// Coordinates of the square the piece is moving from.
        /// </summary>
        public SquareCoords Origin;

        /// <summary>
        /// Image of the piece that is being moved.
        /// </summary>
        public Image Piece;
    }
}

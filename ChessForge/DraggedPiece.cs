using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Attributes and monitoring
    /// of the dragged piece.
    /// </summary>
    public class DraggedPiece
    {
        /// <summary>
        /// Image being dragged.
        /// </summary>
        public static Image ImageControl;

        /// <summary>
        /// Square from which the piece is being dragged.
        /// </summary>
        public static SquareCoords OriginSquare;

        /// <summary>
        /// Whether we are currently dragging a piece
        /// </summary>
        public static bool isDragInProgress = false;

        /// <summary>
        /// The point we clicked to start the dragging
        /// </summary>
        public static Point PtStartDragLocation;

        /// <summary>
        /// Left and Top of the Image control when we started dragging
        /// so that we can return it to the right spot.
        /// </summary>
        public static Point PtDraggedPieceOrigin; // Left and Top of the Image control

    }
}

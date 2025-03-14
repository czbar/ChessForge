using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace ChessForge
{
    public class PositionSetupDraggedPiece
    {
        // whether there is a drag process in progress
        private bool _isDragInProgress;

        /// <summary>
        /// Whether we are currently dragging a piece
        /// </summary>
        public bool IsDragInProgress
        {
            get { return _isDragInProgress; }
            set { _isDragInProgress = value; }
        }

        /// <summary>
        /// Resets all properties.
        /// </summary>
        public void Clear()
        {
            OriginSquare = null;
            Piece = PieceType.None;
            Color = PieceColor.None;
            ImageControl = new Image();
        }

        /// <summary>
        /// Image control being dragged.
        /// </summary>
        public Image ImageControl;

        /// <summary>
        /// The type of piece being dragged.
        /// </summary>
        public PieceType Piece;

        /// <summary>
        /// Whether the dragged image is that of an empty square.
        /// If so then the Piece value must be None.
        /// </summary>
        public bool IsEmptySquare {get; set; }

        /// <summary>
        /// The color of the piece being dragged.
        /// </summary>
        public PieceColor Color;

        /// <summary>
        /// Coordinates from which the piece was dragging.
        /// Null if this was an off-board piece.
        /// </summary>
        public SquareCoords OriginSquare;
    }
}

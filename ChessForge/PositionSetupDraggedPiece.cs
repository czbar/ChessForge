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
        /// <summary>
        /// Image control being dragged.
        /// </summary>
        public Image ImageControl;

        public PieceType Type;

        public PieceColor Color;

        /// <summary>
        /// Whether we are currently dragging a piece
        /// </summary>
        public bool IsDragInProgress = false;
    }
}

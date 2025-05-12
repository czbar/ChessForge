using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameTree;

namespace ChessPosition
{
    /// <summary>
    /// Contains the minimum information required to pass to the UI
    /// so that the move can be correctly visualized.
    /// Note that GUI layer has no move related logic. It will rely entirely
    /// on this data to visualize the move.
    /// </summary>
    [Serializable()]
    public class MoveUI
    {
        public MoveUI() 
        {
        }

        public MoveUI(int xOrigin, int yOrigin, int xDestination, int yDestination)
        {
            Origin = new SquareCoords(xOrigin, yOrigin);
            Destination = new SquareCoords(xDestination, yDestination);
        }

        public MoveUI CloneMe()
        {
            return this.MemberwiseClone() as MoveUI;
        }

        /// <summary>
        /// The square from which the piece is moving.
        /// The GUI will leave this square empty after the move.
        /// </summary>
        public SquareCoords Origin;

        /// <summary>
        /// The square to which the piece is moving.
        /// The GUI will place the piece from Origin
        /// here unless PiecePromotedTo is set to 
        /// something other than None.
        /// </summary>
        public SquareCoords Destination;

        /// <summary>
        /// In the case of castling, there are two pieces moving (King and Rook).
        /// This is the Origin of the second piece (i.e. Rook).
        /// </summary>
        public SquareCoords OriginSecondary;

        /// <summary>
        /// In the case of castling, there are two pieces moving (King and Rook).
        /// This is the Destination of the second piece (i.e. Rook).
        /// </summary>
        public SquareCoords DestinationSecondary;

        /// <summary>
        /// If this is set to a value other than None,
        /// the move is a promotion so the piece specified 
        /// here will be placed on the destination square
        /// </summary>
        public PieceType PiecePromotedTo;

        /// <summary>
        /// Specified the color of the piece moving.
        /// This is only used if the move is a promotion
        /// so the GUI knows what color of the piece to put
        /// on the promotion square.
        /// Otherwise, the GUI does not care of the color.
        /// </summary>
        public PieceColor Color;

        /// <summary>
        /// Returns the engine-format notation for the move.
        /// </summary>
        /// <returns></returns>
        public string GetEngineNotation()
        {
            StringBuilder sb = new StringBuilder();

            if (Origin != null && Destination != null)
            {
                sb.Append(PositionUtils.ConvertXYtoAlgebraic(Origin.Xcoord, Origin.Ycoord) + PositionUtils.ConvertXYtoAlgebraic(Destination.Xcoord, Destination.Ycoord));

                if (PiecePromotedTo != PieceType.None)
                {
                    sb.Append(FenParser.PieceToFenChar[PiecePromotedTo]);
                }
            }

            return sb.ToString();
        }
    }
}

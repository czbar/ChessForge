using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessPosition;

namespace ChessPosition
{
    /// <summary>
    /// Holds all data that we have for a given half move
    /// </summary>
    public class MoveData
    {
        // Coordinates of the "to" square.
        // Must be populated.
        public SquareCoords Destination = new SquareCoords(-1, -1);

        // Coordinates of the "from" square.
        // May or may not be populated depending on what's
        // included in the move's notation.
        // E.g. 'b' in Nbd2 will be recorded as the origin's file (xPos).
        public SquareCoords Origin = new SquareCoords(-1, -1);

        // What type of piece is making the move.
        public PieceType MovingPiece;

        // non-zero, if the move is a castling
        public byte CastlingType;

        // What color is the piece making the move.
        public PieceColor Color;

        // Does the move's notation indicate a check?
        public bool IsCheck;

        // Does the move's notation indicate a checkmate?
        public bool IsCheckmate;

        // If the move is promotion, what type of piece
        // are we promoting to.
        public PieceType PromoteToPiece = PieceType.None;

        // Does the move notation indicate a capture
        public bool IsCapture;

        // A NAG string if an uncoded NAG was encountered during processing.
        public string Nag;

        // Description of the error, if encountered.
        public string ErrorText = "";

        public bool IsCaptureOrPawnMove()
        {
            return IsCapture || MovingPiece == PieceType.Pawn;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessPosition
{
    /// <summary>
    ///  Represents a single square on a chess board.
    /// </summary>
    public class Square
    {
        public SquareCoords Location;
        public PieceType pieceType { get; set; }
        public PieceColor pieceColor { get; set; }


        //private List<SquareCoords> whiteAttackingPieces;
        //private List<SquareCoords> blackAttackingPieces;

        public Square(int xpos, int ypos)
        {
            Location = new SquareCoords(xpos, ypos);
        }

        public Square(int xpos, int ypos, PieceType pt)
        {
            Location = new SquareCoords(xpos, ypos);
            pieceType = pt;
        }
    }
}

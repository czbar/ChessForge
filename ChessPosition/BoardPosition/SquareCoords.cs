using System;

namespace ChessPosition
{
    // Chess board's square coordinates where
    // Xcoord and Ycoords have values between 0 and 7
    // with the former corresponding to file/column (0 is the 'a'-file
    // and 7 is the 'h'-file, and the latter corresponding to rank
    // (0 is the first rank and 7 is the 8th rank).
    // For example: SquareCoords [0,0] represents square 'a1' and [3,4] is 'd5'
    // in the algebraic notation.
    [Serializable()]
    public class SquareCoords
    {
        /// <summary>
        /// x coordinate a.k.a column a.k.a file:
        /// values 0-7 coresponding to 'a'-'h' in the algebraic notation
        ///  if not set the value is -1
        /// </summary>
        public int Xcoord { get; set; }

        /// <summary>
        /// y coordinate a.k.a row a.k.a rank:
        /// values 0-7 coresponding to '1'-'8' in the algebraic notation
        /// if not set the value is -1        
        /// </summary>
        public int Ycoord { get; set; }  

        /// <summary>
        /// Creates an instance of this class with
        /// coordinates specified in the arguments.
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        public SquareCoords(int xCoord, int yCoord)
        {
            Ycoord = yCoord;
            Xcoord = xCoord;
        }

        /// <summary>
        /// Copy contstructor
        /// </summary>
        /// <param name="sq"></param>
        public SquareCoords(SquareCoords sq)
        {
            if (sq != null)
            {
                Ycoord = sq.Ycoord;
                Xcoord = sq.Xcoord;
            }
        }

        /// <summary>
        /// Flips the coordinates to correspond to
        /// a flipped board;
        /// </summary>
        public void Flip()
        {
            Xcoord = 7 - Xcoord;
            Ycoord = 7 - Ycoord;
        }

        /// <summary>
        /// Returns true if the current coordinates correspond
        /// to a square on the board.
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return Xcoord >= 0 && Xcoord <= 7 && Ycoord >= 0 && Ycoord <= 7;
        }

        /// <summary>
        /// Returns true if the two passed objects represent the same square.
        /// </summary>
        /// <param name="sc1"></param>
        /// <param name="sc2"></param>
        /// <returns></returns>
        public static bool AreSameCoords(SquareCoords sc1, SquareCoords sc2)
        {
            return sc1.Xcoord == sc2.Xcoord && sc1.Ycoord == sc2.Ycoord;
        }
    }
}

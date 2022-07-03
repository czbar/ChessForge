
namespace ChessPosition
{
    // Chess board's square coordinates where
    // Xcoord and Ycoords have values between 0 and 7
    // with the former corresponding to file/column (0 is the 'a'-file
    // and 7 is the 'h'-file, and the latter corresponding to rank
    // (0 is the first rank and 7 is the 8th rank).
    // For example: SquareCoords [0,0] represents square 'a1' and [3,4] is 'd5'
    // in the algebraic notation.
    public class SquareCoords
    {
        public int Xcoord { get; set; }  // x coordinate a.k.a column a.k.a file:
                                         // values 0-7 coresponding to 'a'-'h' in the algebraic notation
                                         // if not set the value is -1
        public int Ycoord { get; set; }  // y coordinate a.k.a row a.k.a rank:
                                         // values 0-7 coresponding to '1'-'8' in the algebraic notation
                                         // if not set the value is -1

        public SquareCoords(int xCoord, int yCoord)
        {
            Ycoord = yCoord;
            Xcoord = xCoord;
        }
    }
}

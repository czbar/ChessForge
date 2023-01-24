using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Utilities for the Main Chess Board object.
    /// </summary>
    public class MainChessBoardUtils
    {
        /// <summary>
        /// Size of the side of the individual square
        /// </summary>
        public static int SquareSize { get => _squareSize; set => _squareSize = value; }

        // square side's length
        private static int _squareSize = 80;

        /// <summary>
        /// Get position of a chessboard's square
        /// </summary>
        /// <param name="sq">XY coordinates of the square</param>
        /// <returns></returns>
        public static Point GetSquareTopLeftPoint(SquareCoords sq)
        {
            double left = SquareSize * sq.Xcoord + AppState.MainWin.UiImgMainChessboard.Margin.Left;
            double top = SquareSize * (7 - sq.Ycoord) + AppState.MainWin.UiImgMainChessboard.Margin.Top;

            return new Point(left, top);
        }

        /// <summary>
        /// Get the center point of a chessboard's square
        /// </summary>
        /// <param name="sq">XY coordinates of the square</param>
        /// <returns></returns>
        public static Point GetSquareCenterPoint(SquareCoords sq)
        {
            Point pt = GetSquareTopLeftPoint(sq);
            return new Point(pt.X + SquareSize / 2, pt.Y + SquareSize / 2);
        }

        /// <summary>
        /// Get Image control at a given point.
        /// Invoked when the user clicks on the chessboard
        /// preparing to make a move.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Image GetImageFromPoint(Point p)
        {
            SquareCoords sq = ClickedSquare(p);
            if (sq == null)
            {
                return null;
            }
            else
            {
                return AppState.MainWin.MainChessBoard.GetPieceImage(sq.Xcoord, sq.Ycoord, true);
            }
        }

        /// <summary>
        /// Get XY coordinates of clicked square.
        /// </summary>
        /// <param name="p">Location of the clicked point.</param>
        /// <returns></returns>
        public static SquareCoords ClickedSquare(Point p)
        {
            double squareSide = AppState.MainWin.UiImgMainChessboard.Width / 8.0;
            double xPos = p.X / squareSide;
            double yPos = p.Y / squareSide;

            if (xPos > 0 && xPos < 8 && yPos > 0 && yPos < 8)
            {
                return new SquareCoords((int)xPos, 7 - (int)yPos);
            }
            else
            {
                return null;
            }
        }
    }
}

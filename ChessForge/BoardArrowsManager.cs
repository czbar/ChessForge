using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Manages arrows and circles in the current position.
    /// Monitors the process of drawing a new arrow.
    /// </summary>
    public class BoardArrowsManager
    {
        // completed arrows 
        private static List<BoardArrow> _boardArrows = new List<BoardArrow>();

        // flags if there is a new arrow being built
        private static bool _isArrowBuildInProgress;

        // arrow currently being drawn
        private static BoardArrow _arrowInProgress;

        // start square for the arrow being drawn
        private static SquareCoords _startSquare;

        // end square for the arrow being drawn
        private static SquareCoords _endSquare;


        /// <summary>
        /// Resets the object and creates new arrows based 
        /// on the passed coded string
        /// </summary>
        /// <param name="arrows"></param>
        public static void Reset(string arrows)
        {
            Reset();
            if (!string.IsNullOrWhiteSpace(arrows))
            {
                string[] tokens = arrows.Split(',');
                foreach (string token in tokens)
                {
                    // should be exactly 5 chars
                    if (token.Length == 5)
                    {
                        string color = GetColorName(token[0]);
                        SquareCoords start = PositionUtils.ConvertAlgebraicToXY(token.Substring(1, 2));
                        SquareCoords end = PositionUtils.ConvertAlgebraicToXY(token.Substring(3, 2));
                        if (AppStateManager.MainWin.IsMainChessboardFlipped())
                        {
                            start.Flip();
                            end.Flip();
                        }
                        StartArrowDraw(start, color);
                        FinalizeArrow(end);
                    }
                }
            }
        }

        private static string GetColorName(char c)
        {
            switch (c)
            {
                case 'G':
                    return "green";
                case 'B':
                    return "blue";
                case 'R':
                    return "red";
                case 'Y':
                    return "yellow";
                default:
                    return "yellow";
            }
        }

        /// <summary>
        /// Removes all created arrows from the board
        /// </summary>
        public static void Reset()
        {
            foreach (BoardArrow arrow in _boardArrows)
            {
                arrow.RemoveFromBoard();
            }

            _boardArrows.Clear();
            CancelArrowDraw();

            _isArrowBuildInProgress = false;
        }
        /// <summary>
        // Flags if there is a new arrow being built
        /// </summary>
        public static bool IsArrowBuildInProgress
        {
            get => _isArrowBuildInProgress;
            set => _isArrowBuildInProgress = value;
        }

        /// <summary>
        /// Flips the arrow (called when the board flips)
        /// </summary>
        public static void Flip()
        {
            foreach (BoardArrow arrow in _boardArrows)
            {
                arrow.Flip();
            }
        }

        /// <summary>
        /// Starts drawing a new arrow
        /// </summary>
        /// <param name="start"></param>
        /// <param name="color"></param>
        public static void StartArrowDraw(SquareCoords start, string color)
        {
            _startSquare = new SquareCoords(start);
            _isArrowBuildInProgress = true;
            _arrowInProgress = new BoardArrow(start, color);

            // TODO: draw circle
        }

        /// <summary>
        /// Finishes drawing the arrow and saves it in the list.
        /// </summary>
        /// <param name="current"></param>
        public static void FinalizeArrow(SquareCoords current)
        {
            _endSquare = new SquareCoords(current);
            UpdateArrowDraw(current);
            _boardArrows.Add(_arrowInProgress);
            CancelArrowDraw();
        }

        /// <summary>
        /// Redraws the existing arrow as the user changes the position of the mouse.
        /// </summary>
        /// <param name="current"></param>
        public static void UpdateArrowDraw(SquareCoords current)
        {
            if (!_isArrowBuildInProgress || !current.IsValid())
            {
                return;
            }

            if (current.Xcoord == _startSquare.Xcoord && current.Ycoord == _startSquare.Ycoord)
            {
                // no arrow to draw yet
                return;
            }
            else
            {
                _arrowInProgress.DrawArrow(current);
            }
        }

        /// <summary>
        /// Cancels the arrow currently being drawn.
        /// </summary>
        public static void CancelArrowDraw()
        {
            _startSquare = null;
            _endSquare = null;
            _isArrowBuildInProgress = false;
            _arrowInProgress = null;
        }
    }
}

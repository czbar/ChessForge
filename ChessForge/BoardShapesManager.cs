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
    public class BoardShapesManager
    {
        // completed arrows 
        private static List<BoardArrow> _boardArrows = new List<BoardArrow>();

        // completed circles 
        private static List<BoardCircle> _boardCircles = new List<BoardCircle>();

        // flags if there is a new arrow being built
        private static bool _isShapeBuildInProgress;

        // Arrow currently being drawn
        private static BoardArrow _arrowInProgress;

        // Circle currently being drawn
        private static BoardCircle _circleInProgress;

        // start square for the arrow being drawn
        private static SquareCoords _startSquare;

        // end square for the arrow being drawn
        private static SquareCoords _endSquare;


        /// <summary>
        /// Resets the object and creates new arrows based 
        /// on the passed coded string
        /// </summary>
        /// <param name="arrows"></param>
        public static void Reset(string arrows, string circles)
        {
            Reset();
            if (!string.IsNullOrWhiteSpace(arrows))
            {
                string[] tokens = arrows.Split(',');
                foreach (string token in tokens)
                {
                    if (DecodeArrowsString(token, out string color, out SquareCoords start, out SquareCoords end))
                    {
                        StartShapeDraw(start, color);
                        FinalizeShape(end, false);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(circles))
            {
                string[] tokens = circles.Split(',');
                foreach (string token in tokens)
                {
                    if (DecodeCirclesString(token, out string color, out SquareCoords square))
                    {
                        StartShapeDraw(square, color);
                        FinalizeShape(square, false);
                    }
                }
            }
        }

        /// <summary>
        /// Removes all created arrows and circles from the board
        /// </summary>
        public static void Reset()
        {
            foreach (BoardArrow arrow in _boardArrows)
            {
                arrow.RemoveFromBoard();
            }
            _boardArrows.Clear();

            foreach (BoardCircle circle in _boardCircles)
            {
                circle.RemoveFromBoard();
            }
            _boardCircles.Clear();

            CancelShapeDraw();
            _isShapeBuildInProgress = false;
        }

        /// <summary>
        // Flags if there is a new arrow being built
        /// </summary>
        public static bool IsShapeBuildInProgress
        {
            get => _isShapeBuildInProgress;
            set => _isShapeBuildInProgress = value;
        }

        /// <summary>
        /// Flips the shapes (called when the board flips)
        /// </summary>
        public static void Flip()
        {
            foreach (BoardArrow arrow in _boardArrows)
            {
                arrow.Flip();
            }

            foreach (BoardCircle circle in _boardCircles)
            {
                circle.Flip();
            }
        }

        /// <summary>
        /// Starts drawing a new shape.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="color"></param>
        public static void StartShapeDraw(SquareCoords start, string color)
        {
            _startSquare = new SquareCoords(start);
            _isShapeBuildInProgress = true;
            _arrowInProgress = new BoardArrow(start, color);
            _circleInProgress = new BoardCircle(start, color);
        }

        /// <summary>
        /// Finishes drawing the shape and saves it in the list.
        /// </summary>
        /// <param name="current"></param>
        public static void FinalizeShape(SquareCoords current, bool isNew)
        {
            _endSquare = new SquareCoords(current);
            UpdateShapeDraw(current);

            if (SquareCoords.AreSameCoords(_startSquare, _endSquare))
            {
                _boardCircles.Add(_circleInProgress);
                _arrowInProgress.RemoveFromBoard();
            }
            else
            {
                _boardArrows.Add(_arrowInProgress);
                _circleInProgress.RemoveFromBoard();
            }
            CancelShapeDraw();

            SaveShapesStrings();
            if (isNew)
            {
                AppStateManager.IsDirty = true;
            }
        }

        /// <summary>
        /// Redraws the existing shape as the user changes the position of the mouse.
        /// </summary>
        /// <param name="current"></param>
        public static void UpdateShapeDraw(SquareCoords current)
        {
            if (!_isShapeBuildInProgress || !current.IsValid())
            {
                return;
            }

            //is this an arrow or a circle
            if (SquareCoords.AreSameCoords(current, _startSquare))
            {
                _circleInProgress.Draw(current);
                _arrowInProgress.RemoveFromBoard();
            }
            else
            {
                _arrowInProgress.Draw(current);
                _circleInProgress.RemoveFromBoard();
            }
        }

        /// <summary>
        /// Cancels the shape currently being drawn.
        /// </summary>
        public static void CancelShapeDraw()
        {
            _startSquare = null;
            _endSquare = null;
            _isShapeBuildInProgress = false;
            _arrowInProgress = null;
            _circleInProgress = null;
        }

        /// <summary>
        /// Saves the shape positions to the Node.
        /// </summary>
        private static void SaveShapesStrings()
        {
            AppStateManager.MainWin.SaveArrowsStringInCurrentNode(CodeArrowsString());
            AppStateManager.MainWin.SaveCirclesStringInCurrentNode(CodeCirclesString());
        }

        /// <summary>
        /// Decodes the Arrow data string. 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="color"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private static bool DecodeArrowsString(string code, out string color, out SquareCoords start, out SquareCoords end)
        {
            start = end = null;
            color = "";

            // must be exactly 5 chars
            if (code.Length == 5)
            {
                color = GetColorName(code[0]);
                start = PositionUtils.ConvertAlgebraicToXY(code.Substring(1, 2));
                end = PositionUtils.ConvertAlgebraicToXY(code.Substring(3, 2));
                if (AppStateManager.MainWin.IsMainChessboardFlipped())
                {
                    start.Flip();
                    end.Flip();
                }
            }

            return start != null && end != null;
        }

        /// <summary>
        /// Decodes the Arrow data string. 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="color"></param>
        /// <param name="square"></param>
        /// <returns></returns>
        private static bool DecodeCirclesString(string code, out string color, out SquareCoords square)
        {
            square = null;
            color = "";

            // must be exactly 3 chars
            if (code.Length == 3)
            {
                color = GetColorName(code[0]);
                square = PositionUtils.ConvertAlgebraicToXY(code.Substring(1, 2));
                if (AppStateManager.MainWin.IsMainChessboardFlipped())
                {
                    square.Flip();
                }
            }

            return square != null;
        }

        /// <summary>
        /// Encodes Arrow positions into a string.
        /// </summary>
        /// <returns></returns>
        private static string CodeArrowsString()
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (BoardArrow arrow in _boardArrows)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(',');
                }
                sb.Append(GetCharForColor(arrow.Color));
                SquareCoords start = new SquareCoords(arrow.StartSquare);
                SquareCoords end = new SquareCoords(arrow.EndSquare);
                if (AppStateManager.MainWin.IsMainChessboardFlipped())
                {
                    start.Flip();
                    end.Flip();
                }
                sb.Append(PositionUtils.ConvertXYtoAlgebraic(start.Xcoord, start.Ycoord));
                sb.Append(PositionUtils.ConvertXYtoAlgebraic(end.Xcoord, end.Ycoord));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Encodes Circle positions into a string.
        /// </summary>
        /// <returns></returns>
        private static string CodeCirclesString()
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (BoardCircle circle in _boardCircles)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(',');
                }
                sb.Append(GetCharForColor(circle.Color));
                SquareCoords square = new SquareCoords(circle.Square);
                if (AppStateManager.MainWin.IsMainChessboardFlipped())
                {
                    square.Flip();
                }
                sb.Append(PositionUtils.ConvertXYtoAlgebraic(square.Xcoord, square.Ycoord));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts the PGN char to the name of a color that
        /// BoardArrow object understands.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static string GetColorName(char c)
        {
            switch (c)
            {
                case Constants.COLOR_GREEN_CHAR:
                    return Constants.COLOR_GREEN;
                case Constants.COLOR_BLUE_CHAR:
                    return Constants.COLOR_BLUE;
                case Constants.COLOR_RED_CHAR:
                    return Constants.COLOR_RED;
                case Constants.COLOR_YELLOW_CHAR:
                    return Constants.COLOR_YELLOW;
                default:
                    return Constants.COLOR_YELLOW;
            }
        }

        /// <summary>
        /// Converts the name of a color to a PGN char.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private static char GetCharForColor(string color)
        {
            switch (color)
            {
                case Constants.COLOR_GREEN:
                    return 'G';
                case Constants.COLOR_BLUE:
                    return Constants.COLOR_BLUE_CHAR;
                case Constants.COLOR_RED:
                    return Constants.COLOR_RED_CHAR;
                case Constants.COLOR_YELLOW:
                    return Constants.COLOR_YELLOW_CHAR;
                default:
                    return Constants.COLOR_YELLOW_CHAR;
            }
        }
    }
}

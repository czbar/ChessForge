using ChessPosition;
using System;
using System.Collections.Generic;
using System.Data;
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

        // flags if the shape build is started tentatively
        private static bool _isShapeBuildTentative;

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
                        StartShapeDraw(start, color, false);
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
                        StartShapeDraw(square, color, false);
                        FinalizeShape(square, false);
                    }
                }
            }

            if (SaveShapesStrings())
            {
                AppStateManager.IsDirty = true;
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

            CancelShapeDraw(true);
            _isShapeBuildInProgress = false;

            if (SaveShapesStrings())
            {
                AppStateManager.IsDirty = true;
            }
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
        // Flags if there is a new arrow being built
        /// </summary>
        public static bool IsShapeBuildTentative
        {
            get => _isShapeBuildTentative;
            set => _isShapeBuildTentative = value;
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
        public static void StartShapeDraw(SquareCoords start, string color, bool isTentative)
        {
            _startSquare = new SquareCoords(start);
            _isShapeBuildInProgress = true;
            _isShapeBuildTentative = isTentative;

            _arrowInProgress = new BoardArrow(start, color);
            _circleInProgress = new BoardCircle(start, color);
        }

        /// <summary>
        /// Finishes drawing the shape and saves it in the list.
        /// </summary>
        /// <param name="current"></param>
        public static void FinalizeShape(SquareCoords current, bool isNew)
        {
            if (current == null)
            {
                CancelShapeDraw(true);
                return;
            }

            _endSquare = new SquareCoords(current);
            UpdateShapeDraw(current);

            if (SquareCoords.AreSameCoords(_startSquare, _endSquare))
            {
                RemoveDuplicate(_circleInProgress, out bool isSameColor);
                if (isSameColor)
                {
                    _circleInProgress.RemoveFromBoard();
                }
                else
                {
                    _boardCircles.Add(_circleInProgress);
                }

                _arrowInProgress.RemoveFromBoard();
            }
            else
            {
                RemoveDuplicate(_arrowInProgress, out bool isSameColor);
                if (isSameColor)
                {
                    _arrowInProgress.RemoveFromBoard();
                }
                else
                {
                    _boardArrows.Add(_arrowInProgress);
                }

                _circleInProgress.RemoveFromBoard();
            }
            CancelShapeDraw(false);

            bool isChanged = SaveShapesStrings();
            if (isNew || isChanged)
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
            try
            {
                if (!_isShapeBuildInProgress)
                {
                    return;
                }
                else if (current == null || !current.IsValid())
                {
                    CancelShapeDraw(true);
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
            catch { }
        }

        /// <summary>
        /// Cancels the shape currently being drawn.
        /// </summary>
        public static void CancelShapeDraw(bool removeCurrent)
        {
            _startSquare = null;
            _endSquare = null;
            _isShapeBuildInProgress = false;
            _isShapeBuildTentative = false;

            if (_arrowInProgress != null && removeCurrent)
            {
                _arrowInProgress.RemoveFromBoard();
            }

            if (_circleInProgress != null && removeCurrent)
            {
                _circleInProgress.RemoveFromBoard();
            }

            _arrowInProgress = null;
            _circleInProgress = null;
        }

        /// <summary>
        /// Checks if already have an arrow with the same start and end.
        /// If so, checks the color. If the color is the same, we will remove both
        /// arrows from the board, if not we delete the duplicate and replace it
        /// with the new one.
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="isSameColor"></param>
        /// <returns></returns>
        private static bool RemoveDuplicate(BoardArrow arr, out bool isSameColor)
        {
            isSameColor = false;

            bool found = false;

            // due to bugs, there may be more than one dupe so identify before deleting
            List<BoardArrow> toRemove = new List<BoardArrow>();

            for (int i = 0; i < _boardArrows.Count; i++)
            {
                BoardArrow b = _boardArrows[i];
                if (SquareCoords.AreSameCoords(b.StartSquare, arr.StartSquare) && SquareCoords.AreSameCoords(b.EndSquare, arr.EndSquare))
                {
                    isSameColor = b.Color == arr.Color;
                    toRemove.Add(b);
                    found = true;
                    break;
                }
            }

            foreach (BoardArrow b in toRemove)
            {
                b.RemoveFromBoard();
                _boardArrows.Remove(b);
            }
            return found;
        }

        /// <summary>
        /// Checks if already have a circle with the same start and end.
        /// If so, checks the color. If the color is the same, we will remove both
        /// arrows from the board, if not we delete the duplicate and replace it
        /// with the new one.
        /// </summary>
        /// <param name="cir"></param>
        /// <param name="isSameColor"></param>
        /// <returns></returns>
        private static bool RemoveDuplicate(BoardCircle cir, out bool isSameColor)
        {
            isSameColor = false;

            bool found = false;

            // due to bugs, there may be more than one dupe so identify before deleting
            List<BoardCircle> toRemove = new List<BoardCircle>();

            for (int i = 0; i < _boardCircles.Count; i++)
            {
                BoardCircle b = _boardCircles[i];
                if (SquareCoords.AreSameCoords(b.Square, cir.Square))
                {
                    isSameColor = b.Color == cir.Color;
                    toRemove.Add(b);
                    found = true;
                    break;
                }
            }

            foreach (BoardCircle b in toRemove)
            {
                b.RemoveFromBoard();
                _boardCircles.Remove(b);
            }

            return found;
        }

        /// <summary>
        /// Saves the shape positions to the Node.
        /// </summary>
        private static bool SaveShapesStrings()
        {
            bool arrRes = AppStateManager.MainWin.SaveArrowsStringInCurrentNode(CodeArrowsString());
            bool cirRes = AppStateManager.MainWin.SaveCirclesStringInCurrentNode(CodeCirclesString());

            return arrRes || cirRes;
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

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
                    if (DecodeArrowString(token, out string color, out SquareCoords start, out SquareCoords end))
                    {
                        StartArrowDraw(start, color);
                        FinalizeArrow(end, false);
                    }
                }
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
        public static void FinalizeArrow(SquareCoords current, bool isNew)
        {
            _endSquare = new SquareCoords(current);
            UpdateArrowDraw(current);
            _boardArrows.Add(_arrowInProgress);
            CancelArrowDraw();

            SaveArrowsString();
            if (isNew)
            {
                AppStateManager.IsDirty = true;
            }
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

        /// <summary>
        /// Saves the Arrow positions to the Node.
        /// </summary>
        private static void SaveArrowsString()
        {
            AppStateManager.MainWin.SaveArrowsStringInCurrentNode(CodeArrowsString());
        }

        /// <summary>
        /// Decodes the Arrow data string. 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="color"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private static bool DecodeArrowString(string code, out string color, out SquareCoords start, out SquareCoords end)
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
        /// Converts the PGN char to the name of a color that
        /// BoardArrow object understands.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
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
        /// Converts the name of a color to a PGN char.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private static char GetCharForColor(string color)
        {
            switch (color)
            {
                case "green":
                    return 'G';
                case "blue":
                    return 'B';
                case "red":
                    return 'R';
                case "yellow":
                    return 'Y';
                default:
                    return 'Y';
            }
        }
    }
}

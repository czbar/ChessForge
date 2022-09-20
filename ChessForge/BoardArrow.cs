using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Security.Policy;
using System.Diagnostics;
using ChessPosition;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Windows.Controls.Image;

namespace ChessForge
{
    /// <summary>
    /// Creates and manipulates a single arrow to be shown on the board.
    /// The arrow consists of 3 elements.
    /// 1. The circle that is located in the middle of the start square and
    /// is not scaled.
    /// 2. The stem line that begins in the middle of the start square and finishes
    /// some distance from the middle of the end square. Its length is scaled
    /// and it is rotated as required.
    /// 3. The triangle that points to the middle of the end square. It is not scaled
    /// but it is rotated as required.
    /// </summary>
    public class BoardArrow
    {
        /// <summary>
        /// Name of the color for the arrow
        /// </summary>
        public string Color;

        // square from which to start the arrow
        public SquareCoords StartSquare;

        // square at which the arrow ends
        public SquareCoords EndSquare;

        // angle of the arrow
        private double _angle;

        // distanve between start and end squares
        private double _distance;

        // scale for the length of the arrow
        private double _scaleFactor;

        // center point of the start square
        private Point _startPoint;

        // center point of the end of the square
        private Point _endPoint;

        // image for the arrow's top triangle
        private Image _triangle = new Image();

        // image for the arrow's line part
        private Image _stem = new Image();

        // image for the arrow's bottm circle
        private Image _circle = new Image();

        // scaling transform
        private ScaleTransform _scaleTrans;

        // rotating transform
        private RotateTransform _rotateTrans;

        // transform group
        private TransformGroup _transGroup;

        /// <summary>
        /// Constructs the arrow object.
        /// Creates necessary objects for drawing and transforming.
        /// Paints the arrow at the requested location.
        /// </summary>
        /// <param name="start">Start point of the arrow</param>
        /// <param name="end">End point of the arrow.</param>
        public BoardArrow(SquareCoords start, string color)
        {
            Color = color.ToLower();

            StartSquare = new SquareCoords(start);

            switch (Color)
            {
                case Constants.COLOR_RED:
                    _triangle.Source = ChessBoardArrows.RedTriangle;
                    _stem.Source = ChessBoardArrows.RedStem;
                    _circle.Source = ChessBoardArrows.RedHalfCircle;
                    break;
                case Constants.COLOR_GREEN:
                    _triangle.Source = ChessBoardArrows.GreenTriangle;
                    _stem.Source = ChessBoardArrows.GreenStem;
                    _circle.Source = ChessBoardArrows.GreenHalfCircle;
                    break;
                case Constants.COLOR_BLUE:
                    _triangle.Source = ChessBoardArrows.BlueTriangle;
                    _stem.Source = ChessBoardArrows.BlueStem;
                    _circle.Source = ChessBoardArrows.BlueHalfCircle;
                    break;
                default:
                    _triangle.Source = ChessBoardArrows.YellowTriangle;
                    _stem.Source = ChessBoardArrows.YellowStem;
                    _circle.Source = ChessBoardArrows.YellowHalfCircle;

                    break;
            }

            _triangle.Opacity = 0.5;
            _stem.Opacity = 0.5;
            _circle.Opacity = 0.5;

        }

        /// <summary>
        /// Removes the arrow from the board.
        /// </summary>
        public void RemoveFromBoard()
        {
            AppStateManager.MainWin.MainCanvas.Children.Remove(_triangle);
            AppStateManager.MainWin.MainCanvas.Children.Remove(_stem);
            AppStateManager.MainWin.MainCanvas.Children.Remove(_circle);
        }

        /// <summary>
        /// Draws all components of the arrow.
        /// </summary>
        /// <param name="end"></param>
        public void Draw(SquareCoords end)
        {
            EndSquare = new SquareCoords(end);

            _startPoint = MainChessBoardUtils.GetSquareCenterPoint(StartSquare);
            _endPoint = MainChessBoardUtils.GetSquareCenterPoint(end);

            _angle = CalculateAngle(_startPoint, _endPoint);
            _distance = CalculateDistance(_startPoint, _endPoint);

            _scaleFactor = (_distance + 1 - _triangle.Source.Height) / _stem.Source.Height;

            CreateTransforms();

            Draw();
        }

        /// <summary>
        /// Flips the arrow (called if the main chessboard flips)
        /// </summary>
        public void Flip()
        {
            StartSquare.Flip();
            EndSquare.Flip();

            Draw(EndSquare);
        }

        /// <summary>
        /// Create transforms that will be needed.
        /// </summary>
        private void CreateTransforms()
        {
            _scaleTrans = new ScaleTransform(1, _scaleFactor);
            _rotateTrans = new RotateTransform(-1 * (_angle - 90));

            _transGroup = new TransformGroup();
            _transGroup.Children.Add(_scaleTrans);
            _transGroup.Children.Add(_rotateTrans);
        }

        /// <summary>
        /// Calculates the rotation angle of the arrow.
        /// </summary>
        /// <param name="pStart"></param>
        /// <param name="pEnd"></param>
        /// <returns></returns>
        private double CalculateAngle(Point pStart, Point pEnd)
        {
            float xDiff = (float)(pEnd.X - pStart.X);
            float yDiff = (float)(pStart.Y - pEnd.Y);

            return Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;
        }

        /// <summary>
        /// Calculates the distance between the centers of the start and end
        /// squares. The arrow's length will be slightly shorter as it is not
        /// supposed to reach the center of the end square (so it plays nicely
        /// with other arrows that may be reaching there).
        /// </summary>
        /// <param name="pStart"></param>
        /// <param name="pEnd"></param>
        /// <returns></returns>
        private double CalculateDistance(Point pStart, Point pEnd)
        {
            return Point.Subtract(pEnd, pStart).Length;
        }

        /// <summary>
        /// Draws all parts of the arrow.
        /// </summary>
        private void Draw()
        {
            DrawTriangle();
            DrawStem();
            DrawCircle();
        }

        /// <summary>
        /// Draws the top triangle of the arrow.
        /// </summary>
        private void DrawTriangle()
        {
            AppStateManager.MainWin.MainCanvas.Children.Remove(_triangle);
            AppStateManager.MainWin.MainCanvas.Children.Add(_triangle);
            Canvas.SetLeft(_triangle, _endPoint.X - (_triangle.Source.Width / 2));
            Canvas.SetTop(_triangle, _endPoint.Y + 0);
            _triangle.RenderTransformOrigin = new Point(0.5,0);
            _triangle.RenderTransform = _rotateTrans;
            Panel.SetZIndex(_triangle, Constants.ZIndex_BoardArrow);
        }

        /// <summary>
        /// Draws the line part of the arrow.
        /// </summary>
        private void DrawStem()
        {
            AppStateManager.MainWin.MainCanvas.Children.Remove(_stem);
            AppStateManager.MainWin.MainCanvas.Children.Add(_stem);
            Canvas.SetLeft(_stem, _startPoint.X - (_stem.Source.Width / 2));
            Canvas.SetTop(_stem, _startPoint.Y - (_stem.Source.Height));
            _stem.RenderTransformOrigin = new Point(0.5, 1);
            _stem.RenderTransform = _transGroup;
            Panel.SetZIndex(_stem, Constants.ZIndex_BoardArrow);
        }

        /// <summary>
        /// Draws the circle at the bottom of the arrow.
        /// </summary>
        private void DrawCircle()
        {
            AppStateManager.MainWin.MainCanvas.Children.Remove(_circle);
            AppStateManager.MainWin.MainCanvas.Children.Add(_circle);
            Canvas.SetLeft(_circle, _startPoint.X - (_circle.Source.Width / 2));
            Canvas.SetTop(_circle, _startPoint.Y - (_circle.Source.Height / 2));
            _circle.RenderTransformOrigin = new Point(0.5, 0.5);
            _circle.RenderTransform = _rotateTrans;
            Panel.SetZIndex(_circle, Constants.ZIndex_BoardArrow);
        }
    }
}

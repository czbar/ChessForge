using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChessPosition;
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

        // parent chessboard
        private ChessBoard _chessboard;

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

        // size of the square divided by the "canonical" (defualt) size
        private double _squareSizeScale;

        /// <summary>
        /// Constructs the arrow object.
        /// Creates necessary objects for drawing and transforming.
        /// Paints the arrow at the requested location.
        /// </summary>
        /// <param name="start">Start point of the arrow</param>
        /// <param name="end">End point of the arrow.</param>
        public BoardArrow(ChessBoard chessboard, SquareCoords start, string color)
        {
            _chessboard = chessboard;
            _squareSizeScale = _chessboard.SizeScaleFactor;

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
                case Constants.COLOR_ORANGE:
                    _triangle.Source = ChessBoardArrows.OrangeTriangle;
                    _stem.Source = ChessBoardArrows.OrangeStem;
                    _circle.Source = ChessBoardArrows.OrangeHalfCircle;
                    break;
                case Constants.COLOR_PURPLE:
                    _triangle.Source = ChessBoardArrows.PurpleTriangle;
                    _stem.Source = ChessBoardArrows.PurpleStem;
                    _circle.Source = ChessBoardArrows.PurpleHalfCircle;
                    break;
                case Constants.COLOR_DARKRED:
                    _triangle.Source = ChessBoardArrows.DarkredTriangle;
                    _stem.Source = ChessBoardArrows.DarkredStem;
                    _circle.Source = ChessBoardArrows.DarkredHalfCircle;
                    break;
                default:
                    _triangle.Source = ChessBoardArrows.YellowTriangle;
                    _stem.Source = ChessBoardArrows.YellowStem;
                    _circle.Source = ChessBoardArrows.YellowHalfCircle;

                    break;
            }

            TransformedBitmap trfTriangle = _chessboard.ScaleSource(_triangle.Source as BitmapImage);
            _triangle.Source = trfTriangle;

            TransformedBitmap trfStem = _chessboard.ScaleSource(_stem.Source as BitmapImage);
            _stem.Source = trfStem;

            TransformedBitmap trfCircle = _chessboard.ScaleSource(_circle.Source as BitmapImage);
            _circle.Source = trfCircle;

            _triangle.Opacity = 0.8;
            _stem.Opacity = 0.8;
            _circle.Opacity = 0.8;

        }

        /// <summary>
        /// Removes the arrow from the board.
        /// </summary>
        public void RemoveFromBoard()
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                _chessboard.CanvasCtrl.Children.Remove(_triangle);
                _chessboard.CanvasCtrl.Children.Remove(_stem);
                _chessboard.CanvasCtrl.Children.Remove(_circle);
            });
        }

        /// <summary>
        /// Draws all components of the arrow.
        /// </summary>
        /// <param name="end"></param>
        public void Draw(SquareCoords end)
        {
            double heightAdjustment = 12 * _squareSizeScale;

            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                EndSquare = new SquareCoords(end);

                _startPoint = _chessboard.GetSquareCenterPoint(StartSquare);
                _endPoint = _chessboard.GetSquareCenterPoint(end);

                _angle = CalculateAngle(_startPoint, _endPoint);
                _distance = GuiUtilities.CalculateDistance(_startPoint, _endPoint);

                _scaleFactor = (_distance + 1 - (_triangle.Source.Height + heightAdjustment)) / _stem.Source.Height;

                CreateTransforms();

                Draw();
            });
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
        /// Calculates the start of the stem component by moving it off center
        /// in the direction of the arrow.
        /// </summary>
        /// <param name="pStart"></param>
        /// <param name="pEnd"></param>
        /// <returns></returns>
        private Point CalculateStemStart(Point pStart, Point pEnd)
        {
            double OFFSET = 20 * _squareSizeScale;

            double percentOfDistance = OFFSET / _distance;

            Point pt = new Point();
            pt.X = pStart.X + (pEnd.X - pStart.X) * percentOfDistance;
            pt.Y = pStart.Y + (pEnd.Y - pStart.Y) * percentOfDistance;

            return pt;
        }

        /// <summary>
        /// Calculates the end of the triangle (arrow head) component by moving it off center
        /// back in the direction of the arrow.
        /// </summary>
        /// <param name="pStart"></param>
        /// <param name="pEnd"></param>
        /// <returns></returns>
        private Point CalculateArrowEnd(Point pStart, Point pEnd)
        {
            double OFFSET = 3 * _squareSizeScale;

            double percentOfDistance = OFFSET / _distance;

            Point pt = new Point();
            pt.X = pEnd.X + (pStart.X - pEnd.X) * percentOfDistance;
            pt.Y = pEnd.Y + (pStart.Y - pEnd.Y) * percentOfDistance;

            return pt;
        }

        /// <summary>
        /// Draws all parts of the arrow.
        /// </summary>
        private void Draw()
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                DrawTriangle();
                DrawStem();
                DrawCircle();
            });
        }

        /// <summary>
        /// Draws the top triangle of the arrow.
        /// </summary>
        private void DrawTriangle()
        {
            _chessboard.CanvasCtrl.Children.Remove(_triangle);
            _chessboard.CanvasCtrl.Children.Add(_triangle);

            Point ptArrowTop = CalculateArrowEnd(_startPoint, _endPoint);
            Canvas.SetLeft(_triangle, ptArrowTop.X - (_triangle.Source.Width / 2));
            Canvas.SetTop(_triangle, ptArrowTop.Y + 0);

            _triangle.RenderTransformOrigin = new Point(0.5,0);
            _triangle.RenderTransform = _rotateTrans;
            Panel.SetZIndex(_triangle, Constants.ZIndex_BoardArrow);
        }

        /// <summary>
        /// Draws the line part of the arrow.
        /// </summary>
        private void DrawStem()
        {
            _chessboard.CanvasCtrl.Children.Remove(_stem);
            _chessboard.CanvasCtrl.Children.Add(_stem);

            Point ptStemBottom = CalculateStemStart(_startPoint, _endPoint);
            Canvas.SetLeft(_stem, ptStemBottom.X - (_stem.Source.Width / 2));
            Canvas.SetTop(_stem, ptStemBottom.Y - (_stem.Source.Height));

            _stem.RenderTransformOrigin = new Point(0.5, 1);
            _stem.RenderTransform = _transGroup;
            Panel.SetZIndex(_stem, Constants.ZIndex_BoardArrow);
        }

        /// <summary>
        /// Draws the circle at the bottom of the arrow.
        /// </summary>
        private void DrawCircle()
        {
            _chessboard.CanvasCtrl.Children.Remove(_circle);
            _chessboard.CanvasCtrl.Children.Add(_circle);

            Point ptAdj = CalculateStemStart(_startPoint, _endPoint);
            Canvas.SetLeft(_circle, ptAdj.X - (_circle.Source.Width / 2));
            Canvas.SetTop(_circle, ptAdj.Y - (_circle.Source.Height / 2));
            _circle.RenderTransformOrigin = new Point(0.5, 0.5);
            _circle.RenderTransform = _rotateTrans;
            Panel.SetZIndex(_circle, Constants.ZIndex_BoardArrow);
        }
    }
}

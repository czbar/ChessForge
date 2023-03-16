using ChessPosition;
using System.Windows.Media;
using System.Windows;
using Image = System.Windows.Controls.Image;
using System.Windows.Controls;
using System;
using System.Windows.Media.Imaging;

namespace ChessForge
{
    /// <summary>
    /// Creates a circle within a square.
    /// </summary>
    public class BoardCircle
    {
        /// <summary>
        /// Name of the color for the arrow
        /// </summary>
        public string Color;

        // square from which to start the arrow
        public SquareCoords Square;

        // parent chessboard
        private ChessBoard _chessboard;

        // center point of the square
        private Point _centerPoint;

        // image for the arrow's bottm circle
        private Image _circle = new Image();

        /// <summary>
        /// Constructs the Circle object.
        /// </summary>
        /// <param name="square">Square for the Circle</param>
        public BoardCircle(ChessBoard chessboard, SquareCoords square, string color)
        {
            _chessboard = chessboard;

            Color = color.ToLower();

            Square = new SquareCoords(square);

            switch (Color)
            {
                case Constants.COLOR_RED:
                    _circle.Source = ChessBoardCircles.RedCircle;
                    break;
                case Constants.COLOR_GREEN:
                    _circle.Source = ChessBoardCircles.GreenCircle;
                    break;
                case Constants.COLOR_BLUE:
                    _circle.Source = ChessBoardCircles.BlueCircle;
                    break;
                default:
                    _circle.Source = ChessBoardCircles.YellowCircle;
                    break;
            }

            TransformedBitmap transformedBitmap = _chessboard.ScaleSource(_circle.Source as BitmapImage);
            _circle.Source = transformedBitmap;

            _circle.Opacity = 0.7;
        }

        /// <summary>
        /// Removes the arrow from the board.
        /// </summary>
        public void RemoveFromBoard()
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                _chessboard.CanvasCtrl.Children.Remove(_circle);
            });
        }

        /// <summary>
        /// Draws all components of the arrow.
        /// </summary>
        /// <param name="end"></param>
        public void Draw(SquareCoords end)
        {
            _centerPoint = _chessboard.GetSquareCenterPoint(Square);
            Draw();
        }

        /// <summary>
        /// Flips the arrow (called if the main chessboard flips)
        /// </summary>
        public void Flip()
        {
            Square.Flip();
            Draw(Square);
        }

        /// <summary>
        /// Draws the Circle.
        /// </summary>
        private void Draw()
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                _chessboard.CanvasCtrl.Children.Remove(_circle);
                _chessboard.CanvasCtrl.Children.Add(_circle);
                Canvas.SetLeft(_circle, _centerPoint.X - (_circle.Source.Width / 2));
                Canvas.SetTop(_circle, _centerPoint.Y - (_circle.Source.Height / 2));
                Panel.SetZIndex(_circle, Constants.ZIndex_BoardArrow);
            });
        }
    }
}

using ChessPosition;
using System.Windows.Media;
using System.Windows;
using Image = System.Windows.Controls.Image;
using System.Windows.Controls;
using System;

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

        // center point of the square
        private Point _centerPoint;

        // image for the arrow's bottm circle
        private Image _circle = new Image();

        /// <summary>
        /// Constructs the Circle object.
        /// </summary>
        /// <param name="square">Square for the Circle</param>
        public BoardCircle(SquareCoords square, string color)
        {
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

            _circle.Opacity = 0.5;
        }

        /// <summary>
        /// Removes the arrow from the board.
        /// </summary>
        public void RemoveFromBoard()
        {
            AppStateManager.MainWin.MainCanvas.Children.Remove(_circle);
        }

        /// <summary>
        /// Draws all components of the arrow.
        /// </summary>
        /// <param name="end"></param>
        public void Draw(SquareCoords end)
        {
            _centerPoint = MainChessBoardUtils.GetSquareCenterPoint(Square);
            Draw();
        }

        /// <summary>
        /// Flips the arrow (called if the main chessboard flips)
        /// </summary>
        public void Flip()
        {
            Square.Flip();
            Draw();
        }

        /// <summary>
        /// Draws the Circle.
        /// </summary>
        private void Draw()
        {
            AppStateManager.MainWin.MainCanvas.Children.Remove(_circle);
            AppStateManager.MainWin.MainCanvas.Children.Add(_circle);
            Canvas.SetLeft(_circle, _centerPoint.X - (_circle.Source.Width / 2));
            Canvas.SetTop(_circle, _centerPoint.Y - (_circle.Source.Height / 2));
            Panel.SetZIndex(_circle, Constants.ZIndex_BoardArrow);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows;
using GameTree;
using ChessPosition;
using WebAccess;
using System.Diagnostics.Tracing;

namespace ChessForge
{
    /// <summary>
    /// Controls animation of a move.
    /// </summary>
    public class MoveAnimator
    {
        // time per move in auto replay
        private int _moveSpeed = 200;

        // node with the move being animated.
        private TreeNode _nodeAnimated;

        // image that is being animated
        private Image _animatedImage;

        // square from which the animation starts
        private SquareCoords _origin = new SquareCoords(-1, -1);

        // square at which animation ends
        private SquareCoords _destination = new SquareCoords(-1, -1);

        // chess board on which to animate move
        private ChessBoard _chessBoard;

        /// <summary>
        /// Horizontal animation object.
        /// </summary>
        private DoubleAnimation _currentAnimationX;

        /// <summary>
        /// Vertical animation object.
        /// </summary>
        private DoubleAnimation _currentAnimationY;

        /// <summary>
        /// Animation translation object.
        /// </summary>
        private TranslateTransform _currentTranslateTransform;

        /// <summary>
        /// Animation Completed event.
        /// </summary>
        public event EventHandler<EventArgs> AnimationCompleted;

        /// <summary>
        /// Creates this object and initializes the ChessBoard object.
        /// </summary>
        /// <param name="chessboard"></param>
        public MoveAnimator(ChessBoard chessboard)
        {
            _chessBoard = chessboard;
        }

        public void SetAnimationSpeed(int millisec)
        {
            _moveSpeed = millisec;
        }

        /// <summary>
        /// Animates the move by:
        /// - identifying the origin and destination squares.
        /// - obtaining the reference to the image to animate
        /// - starting the animation
        /// Registers the MoveAnimationFinished handler.
        /// The handler will return the image to its
        /// original location and call DisplayPosition for the node. 
        /// </summary>
        /// <param name="node"></param>
        public void AnimateMove(TreeNode node)
        {
            _nodeAnimated = node;

            MoveUtils.EngineNotationToCoords(node.LastMoveEngineNotation, out _origin, out _destination, out _);

            // caller already accounted for a possible flipped board so call with ignoreFlip = true
            _animatedImage = _chessBoard.GetPieceImage(_origin.Xcoord, _origin.Ycoord, true);

            Canvas.SetZIndex(_animatedImage, Constants.ZIndex_PieceInAnimation);

            Point orig = _chessBoard.GetSquareTopLeftPoint(_origin);
            Point dest = _chessBoard.GetSquareTopLeftPoint(_destination);

            TranslateTransform trans = new TranslateTransform();
            if (_animatedImage.RenderTransform != null)
                _animatedImage.RenderTransform = trans;

            _currentAnimationX = new DoubleAnimation(0, dest.X - orig.X, TimeSpan.FromMilliseconds(_moveSpeed));
            _currentAnimationY = new DoubleAnimation(0, dest.Y - orig.Y, TimeSpan.FromMilliseconds(_moveSpeed));

            _currentTranslateTransform = trans;

            _currentAnimationX.Completed += new EventHandler(MoveAnimationCompleted);
            _currentTranslateTransform.BeginAnimation(TranslateTransform.XProperty, _currentAnimationX);
            _currentTranslateTransform.BeginAnimation(TranslateTransform.YProperty, _currentAnimationY);
        }

        /// <summary>
        /// Called when animation completes.
        /// The coords saved in the MoveAnimation object
        /// are absolute as a possible flipped state of the board was
        /// taken into account at the start fo the animation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MoveAnimationCompleted(object sender, EventArgs e)
        {
            _currentTranslateTransform = null;
            _currentAnimationX = null;
            _currentAnimationY = null;

            // put the dragged image on the destination square
            _chessBoard.GetPieceImage(_destination.Xcoord, _destination.Ycoord, true).Source = _animatedImage.Source;

            _animatedImage.Source = null;
            // reconstruct the Image object at origin and set its Source to null
            _chessBoard.ReconstructSquareImage(_origin.Xcoord, _origin.Ycoord, true);

            AnimationCompleted?.Invoke(null, null);
        }

    }
}

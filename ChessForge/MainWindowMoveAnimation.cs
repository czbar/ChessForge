using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace ChessForge
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Completes a castling move. King would have already been moved.
        /// </summary>
        /// <param name="move"></param>
        public void MoveCastlingRook(string move)
        {
            SquareCoords orig = null;
            SquareCoords dest = null;
            switch (move)
            {
                case "e1g1":
                    orig = !MainChessBoard.IsFlipped ? new SquareCoords(7, 0) : new SquareCoords(0, 7);
                    dest = !MainChessBoard.IsFlipped ? new SquareCoords(5, 0) : new SquareCoords(2, 7);
                    break;
                case "e8g8":
                    orig = !MainChessBoard.IsFlipped ? new SquareCoords(7, 7) : new SquareCoords(0, 0);
                    dest = !MainChessBoard.IsFlipped ? new SquareCoords(5, 7) : new SquareCoords(2, 0);
                    break;
                case "e1c1":
                    orig = !MainChessBoard.IsFlipped ? new SquareCoords(0, 0) : new SquareCoords(7, 7);
                    dest = !MainChessBoard.IsFlipped ? new SquareCoords(3, 0) : new SquareCoords(4, 7);
                    break;
                case "e8c8":
                    orig = !MainChessBoard.IsFlipped ? new SquareCoords(0, 7) : new SquareCoords(7, 0);
                    dest = !MainChessBoard.IsFlipped ? new SquareCoords(3, 7) : new SquareCoords(4, 0);
                    break;
            }

            MovePiece(orig, dest);
        }

        /// <summary>
        /// Moving a piece from square to square.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="dest"></param>
        private void MovePiece(SquareCoords orig, SquareCoords dest)
        {
            if (orig == null || dest == null)
                return;

            MainChessBoard.GetPieceImage(dest.Xcoord, dest.Ycoord, true).Source = MainChessBoard.GetPieceImage(orig.Xcoord, orig.Ycoord, true).Source;
            MainChessBoard.GetPieceImage(orig.Xcoord, orig.Ycoord, true).Source = null;
        }

        /// <summary>
        /// Returns the dragged piece's Image control to
        /// the square it started from.
        /// If clearImage == true, the image in the control
        /// will be cleared (e.g. because the move was successfully
        /// executed and the image has been transferred to the control
        /// on the target square.
        /// </summary>
        /// <param name="clearImage"></param>
        public void ReturnDraggedPiece(bool clearImage)
        {
            if (clearImage)
            {
                DraggedPiece.ImageControl.Source = null;
            }
            Canvas.SetLeft(DraggedPiece.ImageControl, DraggedPiece.ptDraggedPieceOrigin.X);
            Canvas.SetTop(DraggedPiece.ImageControl, DraggedPiece.ptDraggedPieceOrigin.Y);
        }

        /// <summary>
        /// Move animation requested as part of auto-replay.
        /// As such we need to flip the coordinates if
        /// the board is flipped.
        /// </summary>
        /// <param name="move"></param>
        public void RequestMoveAnimation(MoveUI move)
        {
            SquareCoords origin = MainChessBoard.FlipCoords(move.Origin);
            SquareCoords destination = MainChessBoard.FlipCoords(move.Destination);
            AnimateMove(origin, destination);
        }

        /// <summary>
        /// Caller must handle a possible flipped stated of the board.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        private void AnimateMove(SquareCoords origin, SquareCoords destination)
        {
            // caller already accounted for a possible flipped board so call with ignoreFlip = true
            Image img = MainChessBoard.GetPieceImage(origin.Xcoord, origin.Ycoord, true);
            MoveAnimation.Piece = img;
            MoveAnimation.Origin = origin;
            MoveAnimation.Destination = destination;

            Canvas.SetZIndex(img, Constants.ZIndex_PieceInAnimation);

            Point orig = MainChessBoardUtils.GetSquareTopLeftPoint(origin);
            Point dest = MainChessBoardUtils.GetSquareTopLeftPoint(destination);

            TranslateTransform trans = new TranslateTransform();
            if (img.RenderTransform != null)
                img.RenderTransform = trans;

            DoubleAnimation animX = new DoubleAnimation(0, dest.X - orig.X, TimeSpan.FromMilliseconds(Configuration.MoveSpeed));
            DoubleAnimation animY = new DoubleAnimation(0, dest.Y - orig.Y, TimeSpan.FromMilliseconds(Configuration.MoveSpeed));

            LearningMode.CurrentTranslateTransform = trans;
            LearningMode.CurrentAnimationX = animX;
            LearningMode.CurrentAnimationY = animY;

            animX.Completed += new EventHandler(MoveAnimationCompleted);
            trans.BeginAnimation(TranslateTransform.XProperty, animX);
            trans.BeginAnimation(TranslateTransform.YProperty, animY);

        }

        /// <summary>
        /// Stops move animation if there is one in progress.
        /// </summary>
        public void StopMoveAnimation()
        {
            // TODO Apparently, there are 2 methods to stop animation.
            // Method 1 below keeps the animated image at the spot it was when the stop request came.
            // Method 2 returns it to the initial position.
            // Neither works fully to our satisfaction. They seem to not be exiting immediately and are leaving some garbage
            // behind which prevents us from immediatey changing the speed of animation on user's request 
            if (LearningMode.CurrentAnimationX != null && LearningMode.CurrentAnimationY != null && LearningMode.CurrentTranslateTransform != null)
            {
                // *** Method 1.
                //AppState.CurrentAnimationX.BeginTime = null;
                //AppState.CurrentAnimationY.BeginTime = null;
                //AppState.CurrentTranslateTransform.BeginAnimation(TranslateTransform.XProperty, AppState.CurrentAnimationX);
                //AppState.CurrentTranslateTransform.BeginAnimation(TranslateTransform.YProperty, AppState.CurrentAnimationY);

                // *** Method 2.
                LearningMode.CurrentTranslateTransform.BeginAnimation(TranslateTransform.XProperty, null);
                LearningMode.CurrentTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
            }
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
            LearningMode.CurrentTranslateTransform = null;
            LearningMode.CurrentAnimationX = null;
            LearningMode.CurrentAnimationY = null;

            MainChessBoard.GetPieceImage(MoveAnimation.Destination.Xcoord, MoveAnimation.Destination.Ycoord, true).Source = MoveAnimation.Piece.Source;

            Point orig = MainChessBoardUtils.GetSquareTopLeftPoint(MoveAnimation.Origin);
            //_pieces[AnimationOrigin.Xcoord, AnimationOrigin.Ycoord].Source = AnimationPiece.Source;

            Canvas.SetLeft(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true), orig.X);
            Canvas.SetTop(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true), orig.Y);

            //TODO: there should be a better way than having to recreate the image control.
            //   but it seems the image would no longer show (tested when not removing
            //   the image from the origin square, the image won't show seemingly due to
            // RenderTransfrom being set.)
            //
            // This seems to work but re-shows the last moved piece on its origin square???
            // _pieces[AnimationOrigin.Xcoord, AnimationOrigin.Ycoord].RenderTransform = null;
            //

            Image old = MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true);
            MainCanvas.Children.Remove(old);
            MainChessBoard.SetPieceImage(new Image(), MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true);
            MainCanvas.Children.Add(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true));
            Canvas.SetLeft(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true), squareSize * MoveAnimation.Origin.Xcoord + UiImgMainChessboard.Margin.Left);
            Canvas.SetTop(MainChessBoard.GetPieceImage(MoveAnimation.Origin.Xcoord, MoveAnimation.Origin.Ycoord, true), squareSize * (7 - MoveAnimation.Origin.Ycoord) + UiImgMainChessboard.Margin.Top);

            ActiveLineReplay.PrepareNextMoveForAnimation(ActiveLineReplay.LastAnimatedMoveIndex, false);
        }

    }
}

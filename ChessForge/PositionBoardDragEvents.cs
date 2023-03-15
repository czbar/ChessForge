using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Manages mouse drag events on the Position setup board.
    /// Starts the drag operation if MouseDown event occured on a piece image on the board
    /// or in the setup rows.
    /// Monitors the MouseMove events.
    /// On MouseUp, if the event occured over the board the dragged piece will be dropped there.
    /// otherwise, if the piece was from the board it will be removed from the board.
    /// On MouseLeave, if the piece was from the board it will be removed from the board. 
    /// </summary>
    public class PositionBoardDragEvents
    {
        // square side's size
        private int _squareSize = 30;

        // the ChessBoard object
        private ChessBoard _chessboard;

        // represents the piece being dragged
        private PositionSetupDraggedPiece _draggedPiece;

        // the parent canvas of the board canvas
        private Canvas _hostCanvas;

        // left offset between the SetupCanvas and BoardCanvas
        private double _boardCanvasToSetupCanvasLeftOffset;

        // top offset between the SetupCanvas and BoardCanvas
        private double _boardCanvasToSetupCanvasTopOffset;

        // left offset between the the Board Image and BoardCanvas
        private double _boardImageToBoardCanvasLeftOffset;

        // top offset between the the Board Image and BoardCanvas
        private double _boardImageToBoardCanvasTopOffset;

        // reference to the BoardPosition object that is kept in sync with the _chessboard.
        private BoardPosition _positionSetup;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="chessBoard"></param>
        public PositionBoardDragEvents(ChessBoard chessBoard, PositionSetupDraggedPiece draggedPiece, Canvas hostCanvas, ref BoardPosition pos)
        {
            _chessboard = chessBoard;
            _chessboard.EnableShapes(true);

            _draggedPiece = draggedPiece;
            _hostCanvas = hostCanvas;
            _positionSetup = pos;

            _boardCanvasToSetupCanvasLeftOffset = _chessboard.CanvasCtrl.Margin.Left;
            _boardCanvasToSetupCanvasTopOffset = _chessboard.CanvasCtrl.Margin.Top;

            _boardImageToBoardCanvasLeftOffset = _chessboard.BoardImgCtrl.Margin.Left;
            _boardImageToBoardCanvasTopOffset = _chessboard.BoardImgCtrl.Margin.Top;
        }

        //***********************************************************
        //
        //  MOUSE EVENTS
        //
        //***********************************************************

        /// <summary>
        /// The mouse click occured within the hosting canvas.
        /// If it hit a piece on the board we will start a drag operation
        /// within the board.
        /// Note: if a piece from the setup row, outside the board was clicked,
        /// it would have been handled in the parent and the StartDrag method
        /// will be invoked from there directly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePoint = e.GetPosition(_chessboard.CanvasCtrl);
            SquareCoords sc = GetSquareCoordsFromBoardCanvasPoint(mousePoint);

            if (e.ChangedButton == MouseButton.Left)
            {
                if (sc != null && sc.IsValid() && _chessboard.GetPieceColor(sc) != PieceColor.None)
                {
                    StartDrag(sc, e);
                }
                else
                {
                    if (_chessboard.GetPieceColor(sc) == PieceColor.None)
                    {
                        _chessboard.Shapes.Reset(true);
                    }
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                _chessboard.Shapes.StartShapeDraw(sc, "", false);
            }
        }

        /// <summary>
        /// Move the dragged piece to the new position unless it is not within the expected canvas.
        /// If we are dragging a piece from the setup row and the mouse is outside the hosting canvas
        /// cancel dragging.
        /// If we are dragging a piece from the diagram and it is now outside the board canvas
        /// remove it from the position and finish dragging.
        /// Otherwise reposition the image.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePoint;
            if (_draggedPiece.IsDragInProgress)
            {
                bool isDiagramPiece = _draggedPiece.OriginSquare != null;
                if (!isDiagramPiece)
                {
                    mousePoint = e.GetPosition(_hostCanvas);
                }
                else
                {
                    mousePoint = e.GetPosition(_chessboard.CanvasCtrl);
                }

                if (mousePoint.X < 0 || mousePoint.Y < 0
                    || (isDiagramPiece && (mousePoint.X > _chessboard.CanvasCtrl.Width || mousePoint.Y > _chessboard.CanvasCtrl.Height))
                    || (!isDiagramPiece && (mousePoint.X > _hostCanvas.Width || mousePoint.Y > _hostCanvas.Height))
                    )
                {
                    StopDrag();
                }
                else
                {
                    Canvas.SetLeft(_draggedPiece.ImageControl, mousePoint.X - _squareSize / 2);
                    Canvas.SetTop(_draggedPiece.ImageControl, mousePoint.Y - _squareSize / 2);
                    Canvas.SetZIndex(_draggedPiece.ImageControl, Constants.ZIndex_PieceInAnimation);
                }
            }
            else
            {
                if (e.RightButton == MouseButtonState.Pressed)
                {
                    if (_chessboard.Shapes.IsShapeBuildInProgress)
                    {
                        mousePoint = e.GetPosition(_chessboard.CanvasCtrl);
                        SquareCoords sc = GetSquareCoordsFromBoardCanvasPoint(mousePoint);
                        _chessboard.Shapes.UpdateShapeDraw(sc);
                    }
                }
            }
        }

        /// <summary>
        /// The drag process has been completed.
        /// If it is finished over the board, put the dragged piece on the square under the mouse.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedPiece.IsDragInProgress)
            {
                _draggedPiece.IsDragInProgress = false;
                SquareCoords sc = GetSquareCoordsFromBoardCanvasPoint(e.GetPosition(_chessboard.CanvasCtrl));
                if (sc != null)
                {
                    AddPieceToBoard(sc, _draggedPiece.Piece, _draggedPiece.Color, _draggedPiece.ImageControl);
                }
                else
                {
                    RemovePieceFromBoard(sc);
                }
                StopDrag();
            }
        }

        /// <summary>
        /// Mouse has left the hosting canvas so we cancel the drag process.
        /// If a diagram piece was dragged it will be removed from the board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MouseLeave(object sender, MouseEventArgs e)
        {
            if (_draggedPiece.IsDragInProgress)
            {
                RemovePieceFromBoard(_draggedPiece.OriginSquare);
                _draggedPiece.IsDragInProgress = false;
            }
        }

        //***********************************************************
        //
        //  DRAG PROCESSING METHODS
        //
        //***********************************************************


        /// <summary>
        /// Sets up the drag operation using an off-the-board piece.
        /// </summary>
        /// <param name="piece"></param>
        /// <param name="color"></param>
        /// <param name="e"></param>
        public void StartDrag(PieceType piece, PieceColor color, MouseButtonEventArgs e)
        {
            _draggedPiece.ImageControl = new Image();
            _draggedPiece.ImageControl.Source = Pieces.GetImageForPieceSmall(piece, color);
            _hostCanvas.Children.Add(_draggedPiece.ImageControl);

            Point mousePoint = e.GetPosition(_hostCanvas);
            Canvas.SetZIndex(_draggedPiece.ImageControl, Constants.ZIndex_PieceInAnimation);
            Canvas.SetLeft(_draggedPiece.ImageControl, mousePoint.X - _squareSize / 2);
            Canvas.SetTop(_draggedPiece.ImageControl, mousePoint.Y - _squareSize / 2);

            _draggedPiece.Piece = piece;
            _draggedPiece.Color = color;
            _draggedPiece.IsDragInProgress = true;

            e.Handled = true;
        }

        /// <summary>
        /// Starts the dragging operation with a piece from the board.
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="e"></param>
        public void StartDrag(SquareCoords sc, MouseButtonEventArgs e)
        {
            if (sc != null && sc.IsValid())
            {
                byte square = _positionSetup.Board[sc.Xcoord, sc.Ycoord];

                _draggedPiece.OriginSquare = sc;
                _draggedPiece.Piece = PositionUtils.GetPieceType(square);
                _draggedPiece.Color = PositionUtils.GetPieceColor(square);

                _draggedPiece.ImageControl = new Image();
                _draggedPiece.ImageControl.Source = Pieces.GetImageForPieceSmall(_draggedPiece.Piece, _draggedPiece.Color);
                _chessboard.CanvasCtrl.Children.Add(_draggedPiece.ImageControl);

                RemovePieceFromBoard(sc);

                Point mousePoint = e.GetPosition(_hostCanvas);
                Canvas.SetZIndex(_draggedPiece.ImageControl, Constants.ZIndex_PieceInAnimation);
                Canvas.SetLeft(_draggedPiece.ImageControl, mousePoint.X - _squareSize / 2);
                Canvas.SetTop(_draggedPiece.ImageControl, mousePoint.Y - _squareSize / 2);

                _draggedPiece.IsDragInProgress = true;
            }
        }

        /// <summary>
        /// If there is a piece on the indicated square, it will be removed
        /// in both _pieceImagesOnBoard and BoardPosition.Board arrays.
        /// </summary>
        /// <param name="sc"></param>
        private void RemovePieceFromBoard(SquareCoords sc)
        {
            if (sc != null && sc.IsValid())
            {
                PositionUtils.ClearSquare((byte)sc.Xcoord, (byte)sc.Ycoord, ref _positionSetup.Board);
                _chessboard.ClearPieceImage(sc.Xcoord, sc.Ycoord, true);
                //_chessboard.ReconstructSquareImage(sc.Xcoord, sc.Ycoord, true);
            }
            else
            {
                StopDrag();
            }
        }

        /// <summary>
        /// Adds a piece to the board.
        /// Updates the _pieceImagesOnBoard array,
        /// and the BoardPosition.Board array.
        /// Removes any piece that may have been on the square before.
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="piece"></param>
        /// <param name="color"></param>
        /// <param name="img"></param>
        private void AddPieceToBoard(SquareCoords sc, PieceType piece, PieceColor color, Image img)
        {
            //RemovePieceFromBoard(sc);
            PositionUtils.PlacePieceOnBoard(piece, color, (byte)sc.Xcoord, (byte)sc.Ycoord, ref _positionSetup.Board);
            _chessboard.SetPieceImage(piece, color, sc.Xcoord, sc.Ycoord, true);

            //_pieceImagesOnBoard[sc.Xcoord, sc.Ycoord] = img;
            //PlacePieceOnSquare(sc, _pieceImagesOnBoard[sc.Xcoord, sc.Ycoord]);
        }

        /// <summary>
        /// Returns square coordinates of the clicked square or null
        /// if the click occured outside the board.
        /// The passed Point must be offset from the Setup Canvas.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private SquareCoords GetSquareCoordsFromBoardCanvasPoint(Point pt)
        {
            pt.X -= _boardImageToBoardCanvasLeftOffset;
            pt.Y -= _boardImageToBoardCanvasTopOffset;

            if (pt.X < 0 || pt.Y < 0)
            {
                return null;
            }

            int xPos = (int)pt.X / _squareSize;
            int yPos = 7 - (int)pt.Y / _squareSize;

            SquareCoords sc = new SquareCoords(xPos, yPos);
            if (sc.IsValid())
            {
                return sc;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Removed the dragged piece image from the parent canvas.
        /// Clears the _draggedPiece object.
        /// </summary>
        private void StopDrag()
        {
            if (_draggedPiece.OriginSquare == null)
            {
                _hostCanvas.Children.Remove(_draggedPiece.ImageControl);
            }
            else
            {
                _chessboard.CanvasCtrl.Children.Remove(_draggedPiece.ImageControl);
            }
            _draggedPiece.Clear();
        }

    }
}

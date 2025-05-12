using ChessPosition;
using GameTree;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChessForge
{
    /// <summary>
    /// Encapsulate the Chessboard GUI object
    /// including the hosting canvas.
    /// </summary>
    public class ChessBoard
    {
        /// <summary>
        /// The node represented on the board.
        /// Can be null.
        /// </summary>
        public TreeNode DisplayedNode;

        /// <summary>
        /// The position shown on the board.
        /// </summary>
        public BoardPosition DisplayedPosition
        {
            get { return DisplayedNode != null ? DisplayedNode.Position : _position; }
        }

        /// <summary>
        /// Manager for arrows and circles.
        /// </summary>
        public BoardShapesManager Shapes;

        /// <summary>
        /// Reference square size. All scaling is done using this as reference.
        /// </summary>
        protected const int _canonicalSquareSize = 80;

        /// <summary>
        /// Images for White pieces 80x80.
        /// </summary>
        protected Dictionary<PieceType, BitmapImage> _dictWhitePieces =
            new Dictionary<PieceType, BitmapImage>()
            {
                [PieceType.Rook] = ChessForge.Pieces.WhiteRook,
                [PieceType.Bishop] = ChessForge.Pieces.WhiteBishop,
                [PieceType.Knight] = ChessForge.Pieces.WhiteKnight,
                [PieceType.Queen] = ChessForge.Pieces.WhiteQueen,
                [PieceType.King] = ChessForge.Pieces.WhiteKing,
                [PieceType.Pawn] = ChessForge.Pieces.WhitePawn
            };

        /// <summary>
        /// Images for Black pieces 80x80.
        /// </summary>
        protected Dictionary<PieceType, BitmapImage> _dictBlackPieces =
            new Dictionary<PieceType, BitmapImage>()
            {
                [PieceType.Rook] = ChessForge.Pieces.BlackRook,
                [PieceType.Bishop] = ChessForge.Pieces.BlackBishop,
                [PieceType.Knight] = ChessForge.Pieces.BlackKnight,
                [PieceType.Queen] = ChessForge.Pieces.BlackQueen,
                [PieceType.King] = ChessForge.Pieces.BlackKing,
                [PieceType.Pawn] = ChessForge.Pieces.BlackPawn
            };

        virtual protected Dictionary<PieceType, BitmapImage> WhitePieces
        {
            get => _dictWhitePieces;
        }

        virtual protected Dictionary<PieceType, BitmapImage> BlackPieces
        {
            get => _dictBlackPieces;
        }

        // board position currently shown
        private BoardPosition _position;

        // size of an individual square in pixels
        private const int _squareSize = 80;

        /// <summary>
        /// Size of an individual square in pixels
        /// </summary>
        virtual public int SquareSize
        {
            get => _squareSize;
        }

        /// <summary>
        /// Hosting Canvas control
        /// </summary>
        public Canvas CanvasCtrl;

        /// <summary>
        /// Image control for the board.
        /// </summary>
        public Image BoardImgCtrl;

        /// <summary>
        /// Overlay image (yellow square) for the 
        /// move's origin.
        /// </summary>
        private Image _moveFromOverlay = new Image();

        /// <summary>
        /// Overlay image (yellow square) for the 
        /// move's destination.
        /// </summary>
        private Image _moveToOverlay = new Image();

        /// <summary>
        /// Promotion "tray" image. 
        /// </summary>
        private Image _promoTray = new Image();

        /// <summary>
        /// Position of the left top corner of the promo tray, if displayed
        /// </summary>
        private Point _promoTrayLeftTop = new Point();

        /// <summary>
        /// Whether the promo tray is inverted.
        /// </summary>
        private bool _promoTrayInverted;

        /// <summary>
        /// Label control to be optionally shown above the board.
        /// </summary>
        private Label _mainLabel;

        /// <summary>
        /// Label control to show the material imbalance in White's favor.
        /// </summary>
        private Label _lblWhiteMaterial;

        /// <summary>
        /// Label control to show the material imbalance in Black's favor.
        /// </summary>
        private Label _lblBlackMaterial;

        /// <summary>
        /// Whether the board is displayed upside down.
        /// </summary>
        private bool _isFlipped;

        /// <summary>
        /// Image controls for ech square
        /// </summary>
        protected Image[,] Pieces = new Image[8, 8];

        /// <summary>
        /// Coordinates of the square with an active overlay, 
        /// representing the move's origin.
        /// </summary>
        private SquareCoords _coloredSquareFrom = new SquareCoords(-1, -1);

        /// <summary>
        /// Whether to show material imbalance labels.
        /// </summary>
        private bool _showMaterial;

        /// <summary>
        /// Whether to show corrdinates
        /// </summary>
        private bool _includeCoords;

        /// <summary>
        /// Labels for vertical coordinates
        /// </summary>
        protected Label[] _vertCoords = new Label[8];

        /// <summary>
        /// Labels for horizontal coordinates
        /// </summary>
        private Label[] _horizCoords = new Label[8];

        /// <summary>
        /// Coordinates of the square with an active overlay, 
        /// representing the move's destination.
        /// </summary>
        private SquareCoords _coloredSquareTo = new SquareCoords(-1, -1);

        /// <summary>
        /// Creates a ChessBoard object and initializes
        /// Image UI elements for each square.
        /// </summary>
        /// <param name="cnv"></param>
        /// <param name="BoardCtrl"></param>
        /// <param name="labelCtrl"></param>
        /// <param name="startPos"></param>
        public ChessBoard(bool hasShapes, Canvas cnv, Image BoardCtrl, Label labelCtrl, bool startPos, bool includeCoords, bool showMaterial = false)
        {
            if (hasShapes)
            {
                Shapes = new BoardShapesManager(this);
            }
            CanvasCtrl = cnv;
            BoardImgCtrl = BoardCtrl;
            _mainLabel = labelCtrl;
            _includeCoords = includeCoords;
            _showMaterial = showMaterial;

            InitializeCoordinateLabels();
            InitializeMaterialLabels();

            Initialize(startPos);
        }

        /// <summary>
        /// Enables or disables drawing of shapes.
        /// </summary>
        /// <param name="enable"></param>
        /// <returns></returns>
        public void EnableShapes(bool enable, TreeNode nd)
        {
            if (enable)
            {
                if (Shapes == null)
                {
                    Shapes = new BoardShapesManager(this);
                    Shapes.SetActiveNode(nd);
                }
            }
            else
            {
                Shapes = null;
            }
        }

        /// <summary>
        /// Returns Square Coordinates of the square with the
        /// passed point coordinates.
        /// Takes the flip state of the board into account.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public SquareCoords GetSquareCoordsFromPoint(Point pt)
        {
            int xCoord = (int)pt.X / SquareSize;
            int yCoord = 7 - (int)pt.Y / SquareSize;

            if (IsFlipped)
            {
                xCoord = 7 - xCoord;
                yCoord = 7 - yCoord;
            }

            var sc = new SquareCoords(xCoord, yCoord);
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
        /// Get Image control at a given point.
        /// Invoked when the user clicks on the chessboard
        /// preparing to make a move.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public Image GetImageFromPoint(Point p)
        {
            SquareCoords sq = ClickedSquare(p);
            if (sq == null)
            {
                return null;
            }
            else
            {
                return GetPieceImage(sq.Xcoord, sq.Ycoord, true);
            }
        }

        /// <summary>
        /// Get the center point of a chessboard's square
        /// </summary>
        /// <param name="sq">XY coordinates of the square</param>
        /// <returns></returns>
        public Point GetSquareCenterPoint(SquareCoords sq)
        {
            Point pt = GetSquareTopLeftPointOffCanvas(sq);
            return new Point(pt.X + SquareSize / 2, pt.Y + SquareSize / 2);
        }

        /// <summary>
        /// Get XY coordinates of the clicked square.
        /// The passed point coordinates are relative to the board image.
        /// </summary>
        /// <param name="p">Location of the clicked point.</param>
        /// <returns></returns>
        public SquareCoords ClickedSquare(Point p)
        {
            double squareSide = BoardImgCtrl.Width / 8.0;
            double xPos = p.X / squareSide;
            double yPos = p.Y / squareSide;

            if (xPos > 0 && xPos < 8 && yPos > 0 && yPos < 8)
            {
                return new SquareCoords((int)xPos, 7 - (int)yPos);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returned Point position of a top left corner of a square.
        /// </summary>
        /// <param name="sq"></param>
        /// <returns></returns>
        public Point GetSquareTopLeftPoint(SquareCoords sq)
        {
            double left = SquareSize * sq.Xcoord;
            double top = SquareSize * (7 - sq.Ycoord);

            return new Point(left, top);
        }

        /// <summary>
        /// Get position of a chessboard's square
        /// </summary>
        /// <param name="sq">XY coordinates of the square</param>
        /// <returns></returns>
        public Point GetSquareTopLeftPointOffCanvas(SquareCoords sq)
        {
            double left = SquareSize * sq.Xcoord + BoardImgCtrl.Margin.Left;
            double top = SquareSize * (7 - sq.Ycoord) + BoardImgCtrl.Margin.Top;

            return new Point(left, top);
        }

        /// <summary>
        /// Sets the color of the label's text.
        /// </summary>
        /// <param name="br"></param>
        public void SetMainLabelColor(SolidColorBrush br)
        {
            _mainLabel.Foreground = br;
        }

        /// <summary>
        /// Scales the source image to the size of the hosting chessboard.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public TransformedBitmap ScaleSource(BitmapImage source)
        {
            double scale = (double)SquareSize / (double)_canonicalSquareSize;
            var transform = new ScaleTransform(scale, scale);

            TransformedBitmap transformedBitmap = new TransformedBitmap(source, transform);
            return transformedBitmap;
        }

        /// <summary>
        /// What's the scale factor to apply in transforms for this instance of the ChesBoard.
        /// </summary>
        public double SizeScaleFactor
        {
            get => (double)SquareSize / (double)_canonicalSquareSize;
        }

        /// <summary>
        /// Sets the text on the label for white's material advantage.
        /// </summary>
        /// <param name="text"></param>
        public void SetWhiteMaterialLabelText(string text)
        {
            if (_lblWhiteMaterial != null)
            {
                _lblWhiteMaterial.Content = text;
            }
        }

        /// <summary>
        /// Sets the text on the label for black's material advantage.
        /// </summary>
        /// <param name="text"></param>
        public void SetBlackMaterialLabelText(string text)
        {
            if (_lblBlackMaterial != null)
            {
                _lblBlackMaterial.Content = text;
            }
        }

        /// <summary>
        /// Creates the coordinates labels.
        /// </summary>
        private void InitializeCoordinateLabels()
        {
            if (!_includeCoords)
            {
                return;
            }

            for (int i = 0; i <= 7; i++)
            {
                Label lbl = _vertCoords[i] = new Label();
                lbl.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
                lbl.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
                lbl.Foreground = Brushes.White;

                CanvasCtrl.Children.Add(lbl);
                SetCoordinateLabelProperties(i, lbl, true);
            }

            for (int i = 0; i <= 7; i++)
            {
                Label lbl = _horizCoords[i] = new Label();
                lbl.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
                lbl.VerticalContentAlignment = System.Windows.VerticalAlignment.Bottom;
                lbl.Foreground = Brushes.White;

                CanvasCtrl.Children.Add(lbl);
                SetCoordinateLabelProperties(i, lbl, false);
            }

            SetCoordinateLabelsText(false);
        }

        /// <summary>
        /// Creates the material labels.
        /// </summary>
        private void InitializeMaterialLabels()
        {
            if (!_showMaterial)
            {
                return;
            }   

            // set up the "white imbalance" label in the top left corner above the board
            _lblWhiteMaterial = new Label();
            CanvasCtrl.Children.Add(_lblWhiteMaterial);

            Canvas.SetLeft(_lblWhiteMaterial, 15);
            Canvas.SetTop(_lblWhiteMaterial, -2);

            _lblWhiteMaterial.Width = 300;
            _lblWhiteMaterial.Height = 28;
            _lblWhiteMaterial.FontSize = 12;
            _lblWhiteMaterial.Foreground = Brushes.White;


            // set up the "black imbalance" label in the top right corner above the board
            _lblBlackMaterial = new Label();
            CanvasCtrl.Children.Add(_lblBlackMaterial);

            Canvas.SetRight(_lblBlackMaterial, 15);
            Canvas.SetTop(_lblBlackMaterial, -2);

            _lblBlackMaterial.Width = 300;
            _lblBlackMaterial.Height = 28;
            _lblBlackMaterial.FontSize = 12;
            _lblBlackMaterial.Foreground = Brushes.Goldenrod;
            _lblBlackMaterial.HorizontalContentAlignment = HorizontalAlignment.Right;
        }

        /// <summary>
        /// Sizes and positions coordinate labels.
        /// Note that this will be overridden in the 
        /// derived classes.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="lbl"></param>
        /// <param name="isVert"></param>
        virtual protected void SetCoordinateLabelProperties(int index, Label lbl, bool isVert)
        {
            lbl.Width = 20;
            lbl.Height = 28;
            lbl.FontSize = 12;

            if (isVert)
            {
                Canvas.SetLeft(lbl, 0);
                Canvas.SetTop(lbl, 45 + (index * 80));
            }
            else
            {
                Canvas.SetLeft(lbl, 45 + (index * 80));
                Canvas.SetBottom(lbl, -2);
            }
        }

        /// <summary>
        /// Sets coordinates labels text.
        /// </summary>
        /// <param name="isFlipped"></param>
        private void SetCoordinateLabelsText(bool isFlipped)
        {
            if (_includeCoords)
            {
                for (int i = 0; i <= 7; i++)
                {
                    _vertCoords[i].Content = isFlipped ? (char)('1' + i) : (char)('1' + (7 - i));
                    _horizCoords[i].Content = isFlipped ? (char)('a' + (7 - i)) : (char)('a' + i);
                }
            }
        }

        /// <summary>
        /// Gets the image for regular size White piece
        /// of the requested type.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public BitmapImage GetWhitePieceRegImg(PieceType pt)
        {
            return WhitePieces[pt];
        }

        /// <summary>
        /// Gets the image for regular size Black piece
        /// of the requested type.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public BitmapImage GetBlackPieceRegImg(PieceType pt)
        {
            return BlackPieces[pt];
        }

        /// <summary>
        /// Sets the image for the board.
        /// </summary>
        /// <param name="imgBoard"></param>
        public void SetBoardSourceImage(BitmapImage imgBoard)
        {
            BoardImgCtrl.Source = imgBoard;
        }

        /// <summary>
        /// Sets the opacity of the board.
        /// </summary>
        /// <param name="opacity"></param>
        public void SetBoardOpacity(double opacity)
        {
            BoardImgCtrl.Opacity = opacity;
            _mainLabel.Opacity = opacity;
        }

        /// <summary>
        /// Returns new SquareCoords object with the coordinates
        /// flipped if the board is flipped ot force == true.
        /// </summary>
        /// <param name="sq"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        public SquareCoords FlipCoords(SquareCoords sq, bool force = false)
        {
            if (_isFlipped || force)
            {
                return new SquareCoords(7 - sq.Xcoord, 7 - sq.Ycoord);
            }
            else
            {
                return sq;
            }
        }

        /// <summary>
        /// Based on the passed point coordinates, saved promo tray position
        /// and the inverted status of the tray, determines which piece was clicked.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public PieceType CalculatePromotionPieceFromPoint(Point pt)
        {
            PieceType piece = PieceType.None;

            if (pt.X >= _promoTrayLeftTop.X && pt.X <= _promoTrayLeftTop.X + SquareSize && pt.Y >= _promoTrayLeftTop.Y)
            {
                int index = (int)((pt.Y - _promoTrayLeftTop.Y) / (double)SquareSize);
                if (index >= 0 && index <= 5)
                {
                    if (_promoTrayInverted)
                    {
                        index = 4 - index;
                    }
                    switch (index)
                    {
                        case 0:
                            piece = PieceType.Queen;
                            break;
                        case 1:
                            piece = PieceType.Knight;
                            break;
                        case 2:
                            piece = PieceType.Rook;
                            break;
                        case 3:
                            piece = PieceType.Bishop;
                            break;
                        default:
                            piece = PieceType.None;
                            break;
                    }
                }
            }

            return piece;
        }

        /// <summary>
        /// Places the promotion tray of appropriate type at the appropriate square.
        /// </summary>
        /// <param name="sidePromoting">Color of the pawn getting promoted.</param>
        /// <param name="promoSquareNorm">Normalized promotion square.</param>
        public void PlacePromoImage(PieceColor sidePromoting, SquareCoords promoSquareNorm)
        {
            SquareCoords promoSquare = new SquareCoords(promoSquareNorm);
            if (_isFlipped)
            {
                promoSquare.Flip();
            }

            Point pt = GetSquareTopLeftPointOffCanvas(promoSquare);
            double left = pt.X;
            double top = pt.Y;
            if (promoSquare.Ycoord == 0)
            {
                top = top - 4 * SquareSize;
            }

            if (sidePromoting == PieceColor.White && _isFlipped || sidePromoting == PieceColor.Black && !_isFlipped)
            {
                _promoTrayInverted = true;
            }
            else
            {
                _promoTrayInverted = false;
            }

            if (sidePromoting == PieceColor.White)
            {
                _promoTray.Source = _isFlipped ? ChessForge.Pieces.WhitePromoInverted : ChessForge.Pieces.WhitePromo;
            }
            else
            {
                _promoTray.Source = _isFlipped ? ChessForge.Pieces.BlackPromo : ChessForge.Pieces.BlackPromoInverted;
            }

            CanvasCtrl.Children.Add(_promoTray);
            Canvas.SetLeft(_promoTray, left);
            Canvas.SetTop(_promoTray, top);
            Canvas.SetZIndex(_promoTray, Constants.ZIndex_PromoTray);

            _promoTrayLeftTop.X = left;
            _promoTrayLeftTop.Y = top;
        }

        /// <summary>
        /// Removes the promotion tray from the Canvas.
        /// </summary>
        public void RemovePromoImage()
        {
            CanvasCtrl.Children.Remove(_promoTray);
        }

        /// <summary>
        /// Places a yellow overlay to indicate the recently made move
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <param name="fromTo"></param>
        public void ColorMoveSquare(int xCoord, int yCoord, bool fromTo)
        {
            if (fromTo)
            {
                _coloredSquareFrom.Xcoord = xCoord;
                _coloredSquareFrom.Ycoord = yCoord;
            }
            else
            {
                _coloredSquareTo.Xcoord = xCoord;
                _coloredSquareTo.Ycoord = yCoord;
            }

            if (_isFlipped)
            {
                xCoord = 7 - xCoord;
                yCoord = 7 - yCoord;
            }

            double left = Canvas.GetLeft(Pieces[xCoord, yCoord]);
            double top = Canvas.GetTop(Pieces[xCoord, yCoord]);

            Image img = fromTo ? _moveFromOverlay : _moveToOverlay;
            img.Opacity = 0.3;
            CanvasCtrl.Children.Add(img);
            Canvas.SetLeft(img, left);
            Canvas.SetTop(img, top);
            Canvas.SetZIndex(img, Constants.ZIndex_SquareMoveOverlay);
        }

        /// <summary>
        /// Removes move overlay images.
        /// </summary>
        public void RemoveMoveSquareColors()
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                CanvasCtrl.Children.Remove(_moveFromOverlay);
                CanvasCtrl.Children.Remove(_moveToOverlay);
            });

            _coloredSquareTo.Xcoord = -1;
            _coloredSquareTo.Ycoord = -1;

            _coloredSquareFrom.Xcoord = -1;
            _coloredSquareFrom.Ycoord = -1;
        }

        /// <summary>
        /// Returns the image at a given coordinates.
        /// The caller may want to ignore the IsFlipped state
        /// in this is called in response to the user clicking
        /// a square.
        /// However, it this is part of making a move on the board
        /// based on the line notation or engine's move
        /// the coordinates may have to be flipped is IsFlipped is set.
        /// </summary>
        /// <param name="xcoord"></param>
        /// <param name="ycoord"></param>
        /// <param name="ignoreFlip"></param>
        /// <returns></returns>
        public Image GetPieceImage(int xcoord, int ycoord, bool ignoreFlip)
        {
            if (ignoreFlip)
            {
                return Pieces[xcoord, ycoord];
            }
            else
            {
                return Pieces[7 - xcoord, 7 - ycoord];
            }
        }

        /// <summary>
        /// Sets piece image on the requested square.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="xcoord"></param>
        /// <param name="ycoord"></param>
        /// <param name="ignoreFlip"></param>
        public void SetPieceImage(Image img, int xcoord, int ycoord, bool ignoreFlip)
        {
            if (ignoreFlip)
            {
                Pieces[xcoord, ycoord] = img;
            }
            else
            {
                Pieces[7 - xcoord, 7 - ycoord] = img;
            }
        }

        /// <summary>
        /// Sets an image for the requested piece on the specified square.
        /// </summary>
        /// <param name="piece"></param>
        /// <param name="color"></param>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <param name="ignoreFlip"></param>
        public void SetPieceImage(PieceType piece, PieceColor color, int xCoord, int yCoord, bool ignoreFlip)
        {
            if (piece == PieceType.None)
            {
                return;
            }

            BitmapImage img;
            if (color == PieceColor.White)
            {
                img = WhitePieces[piece];
            }
            else
            {
                img = BlackPieces[piece];
            }

            if (ignoreFlip)
            {
                Pieces[xCoord, yCoord].Source = img;
            }
            else
            {
                Pieces[7 - xCoord, 7 - yCoord].Source = img;
            }
        }

        /// <summary>
        /// Nulls the image on the given square.
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <param name="ignoreFlip"></param>
        public void ClearPieceImage(int xCoord, int yCoord, bool ignoreFlip)
        {
            if (ignoreFlip)
            {
                Pieces[xCoord, yCoord].Source = null;
            }
            else
            {
                Pieces[7 - xCoord, 7 - yCoord].Source = null;
            }
        }

        /// <summary>
        /// Removes from Canvas the existing image object
        /// and replaces it with a new one.
        /// This is required after animation.
        /// </summary>
        /// <param name="xCoord"></param>
        /// <param name="yCoord"></param>
        /// <param name="ignoreFlip"></param>
        public void ReconstructSquareImage(int xCoord, int yCoord, bool ignoreFlip)
        {
            Image old = GetPieceImage(xCoord, yCoord, ignoreFlip);
            CanvasCtrl.Children.Remove(old);

            Image reconstructed = new Image();
            SetPieceImage(reconstructed, xCoord, yCoord, ignoreFlip);
            CanvasCtrl.Children.Add(reconstructed);
            Canvas.SetLeft(reconstructed, SquareSize * xCoord + BoardImgCtrl.Margin.Left);
            Canvas.SetTop(reconstructed, SquareSize * (7 - yCoord) + BoardImgCtrl.Margin.Top);
        }

        /// <summary>
        /// Sets the text on the label above
        /// the board.
        /// </summary>
        /// <param name="text"></param>
        public void SetLabelText(string text)
        {
            _mainLabel.Content = text;
        }

        /// <summary>
        /// Flips the board upside down.
        /// If it is already flipped it will go back
        /// to normal.
        /// </summary>
        public bool FlipBoard()
        {
            System.Windows.Media.ImageSource temp;

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    temp = Pieces[x, y].Source;
                    Pieces[x, y].Source = Pieces[7 - x, 7 - y].Source;
                    Pieces[7 - x, 7 - y].Source = temp;
                }
            }

            SquareCoords sqFrom = new SquareCoords(_coloredSquareFrom);
            SquareCoords sqTo = new SquareCoords(_coloredSquareTo);
            RemoveMoveSquareColors();

            if (sqFrom.Xcoord >= 0)
            {
                ColorMoveSquare(7 - sqFrom.Xcoord, 7 - sqFrom.Ycoord, true);
            }
            if (sqTo.Xcoord >= 0)
            {
                ColorMoveSquare(7 - sqTo.Xcoord, 7 - sqTo.Ycoord, false);
            }

            _isFlipped = !_isFlipped;

            SetCoordinateLabelsText(_isFlipped);

            if (Shapes != null)
            {
                Shapes.Flip();
            }

            return _isFlipped;
        }

        /// <summary>
        /// Determines whether the chessboard should be flipped to show
        /// the passed color at the bottom.
        /// If so, flips the board.
        /// </summary>
        /// <param name="sideAtBottom"></param>
        public void FlipBoard(PieceColor sideAtBottom)
        {
            if ((sideAtBottom == PieceColor.White || sideAtBottom == PieceColor.None) && _isFlipped
                || sideAtBottom == PieceColor.Black && !_isFlipped)
            {
                FlipBoard();
            }
        }

        /// <summary>
        /// Sets the flipped state according to the passed boolean
        /// </summary>
        /// <param name="setFlipped"></param>
        public void FlipBoard(bool setFlipped)
        {
            if (!setFlipped && _isFlipped || setFlipped && !_isFlipped)
            {
                FlipBoard();
            }
        }

        /// <summary>
        /// Returns the side/color shown at the bottom of the board.
        /// </summary>
        public PieceColor SideAtBottom
        {
            get => _isFlipped ? PieceColor.Black : PieceColor.White;
        }

        /// <summary>
        /// Sets up the position on the board
        /// reflecting the passed TreeNode object.
        /// </summary>
        /// <param name="pos"></param>
        public void DisplayPosition(TreeNode node, bool isActiveBoard = true)
        {
            if (node != null)
            {
                DisplayedNode = node;

                _position = new BoardPosition(node.Position);
                DisplayPosition(node, node.Position, isActiveBoard);
            }
        }

        /// <summary>
        /// Sets up the position on the board
        /// reflecting the passed TreeNode or BoardPosition object.
        /// </summary>
        /// <param name="pos"></param>
        public void DisplayPosition(TreeNode node, BoardPosition pos = null, bool isActiveBoard = true)
        {
            DisplayedNode = node;
            if (node != null)
            {
                _position = new BoardPosition(node.Position);
                if (isActiveBoard && Shapes != null)
                {
                    // DEVNOTE: this area needs reviewing, when going from Chapters to e.g. Exercise
                    // with arrows, we get AppState.IsTabAllowingBoardDraw == false here.
                    // Shapes.Reset(false) was resetting the node's shapes so we changed it to Shapes.Reset(false, false).
                    // It solves the problem because DisplayPosition is then called multiple times in a better context
                    // but that is a problem in itself too!
                    //
                    if (AppState.IsTabAllowingBoardDraw)
                    {
                        Shapes.Reset(node.Arrows, node.Circles, false);
                    }
                    else
                    {
                        Shapes.Reset(false, false);
                    }
                }
            }
            else
            {
                _position = new BoardPosition(pos);
                if (isActiveBoard && Shapes != null)
                {
                    Shapes.Reset(false);
                }
            }

            for (int xcoord = 0; xcoord < 8; xcoord++)
            {
                for (int ycoord = 0; ycoord < 8; ycoord++)
                {
                    DisplayPiece(xcoord, ycoord, pos.Board[xcoord, ycoord]);
                }
            }

            RemoveMoveSquareColors();

            if (_showMaterial)
            {
                MaterialImbalanceUtils.BuildMaterialImbalanceStrings(_position, out string whiteMaterial, out string blackMaterial);
                SetWhiteMaterialLabelText(whiteMaterial);
                SetBlackMaterialLabelText(blackMaterial);
            }
        }

        /// <summary>
        /// Removes all piece images from the board.
        /// </summary>
        public void ClearBoard()
        {
            _position = new BoardPosition();

            for (int xcoord = 0; xcoord < 8; xcoord++)
            {
                for (int ycoord = 0; ycoord < 8; ycoord++)
                {
                    DisplayPiece(xcoord, ycoord, 0);
                }
            }

            RemoveMoveSquareColors();
        }

        /// <summary>
        /// Sets and shows the starting position.
        /// </summary>
        public void DisplayStartingPosition()
        {
            _position = PositionUtils.SetupStartingPosition();
            DisplayPosition(null, _position);
        }

        /// <summary>
        /// Sets and shows the starting position.
        /// </summary>
        public void SetStartingPosition()
        {
            DisplayStartingPosition();
        }

        /// <summary>
        /// Returns the type of the piece occupying the passed square.
        /// </summary>
        /// <param name="sq"></param>
        /// <returns></returns>
        public PieceType GetPieceType(SquareCoords sq)
        {
            if (PositionUtils.AreValidCoordinates(sq))
            {
                return PositionUtils.GetPieceType(_position.Board[sq.Xcoord, sq.Ycoord]);
            }
            else
            {
                return PieceType.None;
            }
        }

        /// <summary>
        /// Returns the color of the piece occupying the passed square.
        /// </summary>
        /// <param name="sq"></param>
        /// <returns></returns>
        public PieceColor GetPieceColor(SquareCoords sq)
        {
            if (PositionUtils.AreValidCoordinates(sq))
            {
                return PositionUtils.GetPieceColor(_position.Board[sq.Xcoord, sq.Ycoord]);
            }
            else
            {
                return PieceColor.None;
            }
        }

        /// <summary>
        /// Indicates whether the board is displayed upside down
        /// </summary>
        public bool IsFlipped { get => _isFlipped; set => _isFlipped = value; }

        /// <summary>
        /// This is the only method that handles the IsFlipped flag.
        /// All other methods displaying pieces and positions
        /// end up calling this one so they do not concern themselves
        /// with the position of the board.
        /// </summary>
        /// <param name="xcoord"></param>
        /// <param name="ycoord"></param>
        /// <param name="img"></param>
        private void PlacePiece(int xcoord, int ycoord, BitmapImage img)
        {
            Pieces[xcoord, ycoord].Dispatcher.Invoke(() =>
            {
                if (_isFlipped)
                {
                    xcoord = 7 - xcoord;
                    ycoord = 7 - ycoord;
                }

                Pieces[xcoord, ycoord].Source = img;
            });
        }

        /// <summary>
        /// Sets Image controls for every square.
        /// The position in the Canvas depends on the
        /// rank and file on the chessboard and on
        /// whether the board is flipped.
        /// </summary>
        /// <param name="startPosition"></param>
        protected void Initialize(bool startPosition)
        {
            for (var i = 0; i < 8; i++)
            {
                for (var j = 0; j < 8; j++)
                {
                    int xPos = i;
                    int yPos = 7 - j;

                    Pieces[xPos, yPos] = new Image();
                    CanvasCtrl.Children.Add(Pieces[xPos, yPos]);
                    PositionImageControlForSquare(xPos, yPos);
                }
            }
            if (startPosition)
            {
                SetStartingPosition();
            }

            _moveFromOverlay.Source = ChessForge.Pieces.YellowOverlay;
            _moveToOverlay.Source = ChessForge.Pieces.YellowOverlay;
        }

        /// <summary>
        /// Places an image of a piece (or empty image object)
        /// on the requested square.
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        private void PositionImageControlForSquare(int xPos, int yPos)
        {
            Canvas.SetLeft(Pieces[xPos, yPos], SquareSize * xPos + BoardImgCtrl.Margin.Left);
            Canvas.SetTop(Pieces[xPos, yPos], SquareSize * (7 - yPos) + BoardImgCtrl.Margin.Top);
            Canvas.SetZIndex(Pieces[xPos, yPos], Constants.ZIndex_PieceOnBoard);
        }

        /// <summary>
        /// Puts a piece image on the board if one
        /// is encoded in "square".
        /// Otherwise, clears the square.
        /// </summary>
        /// <param name="xcoord"></param>
        /// <param name="ycoord"></param>
        /// <param name="square"></param>
        private void DisplayPiece(int xcoord, int ycoord, byte square)
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                if (PositionUtils.GetPieceColor(square) == PieceColor.White)
                {
                    PlacePiece(xcoord, ycoord, WhitePieces[PositionUtils.GetPieceType(square)]);
                }
                else if (PositionUtils.GetPieceColor(square) == PieceColor.Black)
                {
                    PlacePiece(xcoord, ycoord, BlackPieces[PositionUtils.GetPieceType(square)]);
                }
                else
                {
                    PlacePiece(xcoord, ycoord, null);
                }
            });
        }

    }
}

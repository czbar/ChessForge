using ChessPosition;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChessForge
{
    public class ChessBoardSmall : ChessBoard
    {
        // size of an individual square in pixels
        protected int _squareSize = 30;

        /// <summary>
        /// Label showing above the "regular" lable.
        /// It is shown in Bookmarks view to display the Chapter index.
        /// </summary>
        private Label _topLabel;

        /// <summary>
        /// Size of an individual square in pixels
        /// </summary>
        override public int SquareSize
        {
            get => _squareSize;
        }

        /// <summary>
        /// Images for White pieces 30x30.
        /// </summary>
        protected Dictionary<PieceType, BitmapImage> _dictWhitePiecesSmall =
            new Dictionary<PieceType, BitmapImage>()
            {
                [PieceType.Rook] = ChessForge.Pieces.WhiteRookSmall,
                [PieceType.Bishop] = ChessForge.Pieces.WhiteBishopSmall,
                [PieceType.Knight] = ChessForge.Pieces.WhiteKnightSmall,
                [PieceType.Queen] = ChessForge.Pieces.WhiteQueenSmall,
                [PieceType.King] = ChessForge.Pieces.WhiteKingSmall,
                [PieceType.Pawn] = ChessForge.Pieces.WhitePawnSmall
            };

        /// <summary>
        /// Images for Black pieces 80x80.
        /// </summary>
        protected Dictionary<PieceType, BitmapImage> _dictBlackPiecesSmall =
            new Dictionary<PieceType, BitmapImage>()
            {
                [PieceType.Rook] = ChessForge.Pieces.BlackRookSmall,
                [PieceType.Bishop] = ChessForge.Pieces.BlackBishopSmall,
                [PieceType.Knight] = ChessForge.Pieces.BlackKnightSmall,
                [PieceType.Queen] = ChessForge.Pieces.BlackQueenSmall,
                [PieceType.King] = ChessForge.Pieces.BlackKingSmall,
                [PieceType.Pawn] = ChessForge.Pieces.BlackPawnSmall
            };

        /// <summary>
        /// Accessor to White pieces images.
        /// </summary>
        override protected Dictionary<PieceType, BitmapImage> WhitePieces
        {
            get => _dictWhitePiecesSmall;
        }

        /// <summary>
        /// Accessor to Black pieces images.
        /// </summary>
        override protected Dictionary<PieceType, BitmapImage> BlackPieces
        {
            get => _dictBlackPiecesSmall;
        }

        /// <summary>
        /// Constructs a chess board with 30x30 squares.
        /// </summary>
        /// <param name="cnv"></param>
        /// <param name="BoardCtrl"></param>
        /// <param name="labelCtrl"></param>
        /// <param name="startPos"></param>
        /// <param name="includeCoords"></param>
        public ChessBoardSmall(Canvas cnv, Image BoardCtrl, Label labelCtrl, Label topLabel, bool startPos, bool includeCoords)
            : base(false, cnv, BoardCtrl, labelCtrl, startPos, includeCoords)
        {
            _topLabel = topLabel;
        }

        /// <summary>
        /// Shows or Hides the top label
        /// </summary>
        /// <param name="Show"></param>
        public void ShowTopLabel(bool show)
        {
            if (_topLabel != null)
            {
                _topLabel.Visibility = show ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Sets the text of the top label.
        /// </summary>
        /// <param name="txt"></param>
        public void SetTopLabelText(string txt)
        {
            if (_topLabel != null)
            {
                _topLabel.Content = txt;
            }
        }

        /// <summary>
        /// Sets the color of the label's text.
        /// </summary>
        /// <param name="br"></param>
        public void SetTopLabelColor(SolidColorBrush br)
        {
            _topLabel.Foreground = br;
        }

        /// <summary>
        /// Sizes and positions coordinate labels.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="lbl"></param>
        /// <param name="isVert"></param>
        override protected void SetCoordinateLabelProperties(int index, Label lbl, bool isVert)
        {
            lbl.Width = 20;
            lbl.Height = 21;
            lbl.FontSize = 8;

            if (isVert)
            {
                Canvas.SetLeft(lbl, -2);
                Canvas.SetTop(lbl, 18 + (index * 30));
            }
            else
            {
                Canvas.SetLeft(lbl, 19 + (index * 30));
                Canvas.SetBottom(lbl, -1);
            }
        }

    }
}

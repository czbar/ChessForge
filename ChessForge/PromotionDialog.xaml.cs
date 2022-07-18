using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using ChessPosition;


namespace ChessForge
{
    /// <summary>
    /// Interaction logic for PromotionDialog.xaml
    /// </summary>
    public partial class PromotionDialog : Window
    {
        /// <summary>
        /// Sets image visibility depending on which side
        /// is promoting.
        /// The window has 2 images, one wiht White pieces and one
        /// with Black ones.
        /// </summary>
        /// <param name="whitePromotion"></param>
        public PromotionDialog(bool whitePromotion)
        {
            InitializeComponent();

            if (whitePromotion)
            {
                _imgWhitePromo.Visibility = Visibility.Visible;
                _imgBlackPromo.Visibility = Visibility.Hidden;
            }
            else
            {
                _imgWhitePromo.Visibility = Visibility.Hidden;
                _imgBlackPromo.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// The piece selected by the user.
        /// </summary>
        public PieceType SelectedPiece = PieceType.None;

        /// <summary>
        /// Handles the click in the box.
        /// Invokes the GetPieceFromPoint() to determine
        /// user's choice.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _dlgPromotion_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectedPiece = GetPieceFromPoint(e.GetPosition(this));
            this.Close();
            e.Handled = true;
        }

        /// <summary>
        /// Determines the piece selected by the user
        /// based on the location of the click. 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private PieceType GetPieceFromPoint(Point p)
        {
            int imgIndex = (int)(p.Y / _pieceSize);

            PieceType pt;

            switch (imgIndex)
            {
                case 0:
                    pt = PieceType.Queen;
                    break;
                case 1:
                    pt = PieceType.Knight;
                    break;
                case 2:
                    pt = PieceType.Rook;
                    break;
                case 3:
                    pt = PieceType.Bishop;
                    break;
                default:
                    pt = PieceType.None;
                    break;
            }

            return pt;
        }

        /// <summary>
        /// Size in pixels of individual pieces in the image.
        /// Note that while at this stage it is the same as the size of the square
        /// in the main chessboard we don't want the same variables as in the future
        /// these sizes may change and not be identical.
        /// </summary>
        private double _pieceSize = 80;

    }
}

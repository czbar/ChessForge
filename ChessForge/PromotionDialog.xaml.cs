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
        /// The piece selected by the user.
        /// </summary>
        public PieceType SelectedPiece = PieceType.None;

        /// <summary>
        /// Size in pixels of individual pieces in the image.
        /// Note that while at this stage it is the same as the size of the square
        /// in the main chessboard we don't want the same variables as in the future
        /// these sizes may change and not be identical.
        /// </summary>
        private double _pieceSize = 80;

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
            AppLog.Message("PromotionDialog initialized");

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
        /// Determines the piece selected by the user
        /// based on the location of the click. 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private PieceType GetPieceFromPoint(Point p)
        {
            AppLog.Message("PromotionDialog: clicked point X=" + p.X.ToString() + " Y=" + p.Y.ToString());
            
            int imgIndex = (int)(p.Y / _pieceSize);
            AppLog.Message("PromotionDialog: image index = " + imgIndex.ToString());

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
        /// Checks what piece the MouseDown event occured over.
        /// user's choice.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _dlgPromotion_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //AppLog.Message("PromotionDialog MouseDown: timestamp = " + e.Timestamp.ToString());
            SelectedPiece = GetPieceFromPoint(e.GetPosition(this));
        }

        /// <summary>
        /// Checks what piece the MouseUp event occured over.
        /// If same as on MouseDown, we return the result.
        /// This is done for 2 reasons.
        /// Firstly, there is something strange in WPF translating the TouchFown to MouseDown. Sometimes
        /// it returns coordinates from a few toches ago (?). Yet, it does not seem to happen when we  
        /// handle MouseUp too.
        /// Secondly, this is the logic we want anyway.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _dlgPromotion_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //AppLog.Message("PromotionDialog MouseUp: timestamp = " + e.Timestamp.ToString());
            PieceType pt = GetPieceFromPoint(e.GetPosition(this));
            if (SelectedPiece != pt)
            {
                pt = PieceType.None;
            }
            this.Close();
            e.Handled = true;
        }

    }
}

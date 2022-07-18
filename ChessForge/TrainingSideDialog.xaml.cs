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
    /// Interaction logic for TrainingSideDialog.xaml
    /// </summary>
    public partial class TrainingSideDialog : Window
    {
        public TrainingSideDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Side selected in the dialog.
        /// </summary>
        public PieceColor SelectedSide;

        /// <summary>
        /// User clicked the White button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _btnWhite_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectedSide = PieceColor.White;
            this.Close();
        }

        /// <summary>
        /// User clicked the Black button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _btnBlack_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectedSide = PieceColor.Black;
            this.Close();
        }
    }
}

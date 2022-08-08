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
        /// <summary>
        /// Creates the dialog
        /// </summary>
        public TrainingSideDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The Training Side selected in the dialog.
        /// </summary>
        public PieceColor SelectedSide;

        /// <summary>
        /// The Workbook Title entered by the user
        /// </summary>
        public string WorkbookTitle;

        /// <summary>
        /// User clicked the White button.
        /// Save the selected side and move on to editing of the Title.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _btnWhite_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectedSide = PieceColor.White;
            EditWorkbookTitle();
        }

        /// <summary>
        /// User clicked the Black button.
        /// Save the selected side and move on to editing of the Title.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _btnBlack_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectedSide = PieceColor.Black;
            EditWorkbookTitle();
        }

        /// <summary>
        /// Hides the side selection controls and
        /// prompts the user to enter Workbook's title.
        /// </summary>
        private void EditWorkbookTitle()
        {
            _btnBlack.Visibility = Visibility.Collapsed;
            _btnWhite.Visibility = Visibility.Collapsed;

            _btnOK.Visibility = Visibility.Visible;
            _tbTitle.Visibility = Visibility.Visible;

            _tbTitle.Focus();

            _lblMainPrompt.Content = "Type in a name for this Workbook";
        }

        /// <summary>
        /// The user pressed the OK button.
        /// Saves the workbook's title and exits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _btnOK_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            WorkbookTitle = _tbTitle.Text;
            this.Close();
        }
    }
}

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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for DownloadedGamesActionDialog.xaml
    /// </summary>
    public partial class DownloadedGamesActionDialog : Window
    {
        /// <summary>
        /// Types of Save actions that can be selected
        /// </summary>
        public enum Action
        {
            None,
            CurrentChapter,
            NewChapter,
            NewWorkbook
        }

        /// <summary>
        /// Selected Save Action
        /// </summary>
        public Action SaveOption = Action.None;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DownloadedGamesActionDialog()
        {
            InitializeComponent();

            // set label here because we want to add a colon
            UiLblNumberOfGames.Content = Properties.Resources.NumberOfGames + ": ";
            UiRbAppendCurrentChapter.IsChecked = true;
        }

        /// <summary>
        /// Sets SaveOption end exists successfully
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (UiRbAppendCurrentChapter.IsChecked == true)
            {
                SaveOption = Action.CurrentChapter;
            }
            else if (UiRbCreateNewChapter.IsChecked == true)
            {
                SaveOption = Action.NewChapter;
            }
            else
            {
                SaveOption = Action.NewWorkbook;
            }
            DialogResult = true;
        }
    }
}

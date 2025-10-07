using ChessPosition;
using System.Collections.ObjectModel;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SelectEngineLinesDialog.xaml
    /// </summary>
    public partial class SelectEngineLinesDialog : Window
    {
        /// <summary>
        /// The list of games to process.
        /// </summary>
        private ObservableCollection<SelectableString> _lineList;

        /// <summary>
        /// Creates the dialog object. Sets ItemsSource for the ListView
        /// to the list of engine lines.
        /// </summary>
        public SelectEngineLinesDialog(ObservableCollection<SelectableString> lineList)
        {
            _lineList = lineList;

            InitializeComponent();

            UiCbSelectAll.Checked += UiCbSelectAll_Checked;
            UiCbSelectAll.Unchecked += UiCbSelectAll_Unchecked;

            UiLvLines.ItemsSource = lineList;
        }

        /// <summary>
        /// SelectAll box was checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _lineList)
            {
                item.IsSelected = true;
            }
        }

        /// <summary>
        /// SelectAll box was unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _lineList)
            {
                item.IsSelected = false;
            }
        }

        /// <summary>
        /// OK button was clicked. Exits with the result = true
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// Cancel button was clicked. Exits with the result = false
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Engine-and-Evaluation#select-engine-lines-to-paste-dialog");
        }
    }
}

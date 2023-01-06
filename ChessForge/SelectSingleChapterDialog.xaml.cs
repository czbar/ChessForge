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
    /// Interaction logic for SelectSingleChapterDialog.xaml
    /// </summary>
    public partial class SelectSingleChapterDialog : Window
    {
        // true if user exits by clicking ok or double click on the list
        public bool ExitOk = false;

        // selected chapter index
        public int SelectedIndex = -1;

        /// <summary>
        /// Constructs the dialog and binds the list of chapters.
        /// </summary>
        public SelectSingleChapterDialog()
        {
            InitializeComponent();
            UiLbChapters.ItemsSource = WorkbookManager.SessionWorkbook.Chapters;
        }

        /// <summary>
        /// The user clicked the Ok button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            SelectedIndex = UiLbChapters.SelectedIndex;
            ExitOk = true;
            DialogResult = true;
        }

        /// <summary>
        /// The user double clicked on item in the ListBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLbChapters_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            UiBtnOk_Click(sender, e);
        }

        /// <summary>
        /// The user clicked the Cancel button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; 
        }
    }
}

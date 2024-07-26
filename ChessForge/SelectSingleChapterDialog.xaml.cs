using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SelectSingleChapterDialog.xaml
    /// </summary>
    public partial class SelectSingleChapterDialog : Window
    {
        // selected chapter index
        public int SelectedIndex = -1;

        // flags whether the user request a new chapter
        public bool CreateNew = false;

        /// <summary>
        /// Constructs the dialog and binds the list of chapters.
        /// </summary>
        public SelectSingleChapterDialog(int chapterIndex)
        {
            InitializeComponent();
            UiBtnCreateNew.Content = "   " + Properties.Resources.CreateNewChapter + "    ";
            UiBtnCreateNew.Background = Brushes.LightGreen;
            UiLbChapters.ItemsSource = WorkbookManager.SessionWorkbook.Chapters;
            UiLbChapters.SelectedIndex = chapterIndex;
            UiLbChapters.Focus();
        }

        /// <summary>
        /// Exit the dialog with a flag to create a new chapter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCreateNew_Click(object sender, RoutedEventArgs e)
        {
            CreateNew = true;
            DialogResult = true;
        }

        /// <summary>
        /// The user clicked the Ok button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            SelectedIndex = UiLbChapters.SelectedIndex;
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

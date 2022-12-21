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
    /// Interaction logic for SelectChaptersDialog.xaml
    /// </summary>
    public partial class SelectChaptersDialog : Window
    {
        /// <summary>
        /// Exit result of this dialog.
        /// </summary>
        public bool ExitOK = false;

        // Theh Workbook object created to hold data to be imported
        private Workbook _workbook;

        // The list of properties to bind to the List View
        public List<SelectedChapter> ChapterList = new List<SelectedChapter>();

        /// <summary>
        /// Initializes the dialog abd builds a list of chapters
        /// for the user to choose from.
        /// </summary>
        /// <param name="workbook"></param>
        public SelectChaptersDialog(Workbook workbook)
        {
            InitializeComponent();
            _workbook = workbook;

            foreach (Chapter ch in _workbook.Chapters)
            {
                SelectedChapter sel = new SelectedChapter();
                sel.Chapter = ch;
                sel.ChapterTitle = ch.Id.ToString() + ". " + ch.GetTitle();
                sel.IsSelected = true;
                ChapterList.Add(sel);
            }
            UiLvChapters.ItemsSource = ChapterList;
        }

        /// <summary>
        /// SelectAll box was checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in ChapterList)
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
            foreach (var item in ChapterList)
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
            ExitOK = true;
            Close();
        }

        /// <summary>
        /// Cancel button was clicked. Exits with the result = false
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ExitOK = false;
            Close();
        }
    }

    /// <summary>
    /// Helper class to bind with the list view
    /// </summary>
    public class SelectedChapter
    {
        // Chapter object
        public Chapter Chapter { get; set; }

        // Chapter title
        public string ChapterTitle { get; set; }

        // Chapter selection flag
        public bool IsSelected { get; set; }
    }
}

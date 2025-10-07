using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SelectChaptersDialog.xaml
    /// </summary>
    public partial class SelectChaptersDialog : Window
    {
        /// <summary>
        /// How was the dialog invoked.
        /// Determines which Help topic to show.
        /// </summary>
        public enum Mode
        {   
            IMPORT,
            MERGE
        }

        /// <summary>
        /// Exit result of this dialog.
        /// </summary>
        public bool ExitOK = false;

        // Theh Workbook object created to hold data to be imported
        private Workbook _workbook;

        // The list of properties to bind to the List View
        public ObservableCollection<SelectedChapter> ChapterList = new ObservableCollection<SelectedChapter>();

        // How was the dialog invoked
        private Mode _mode;

        /// <summary>
        /// Initializes the dialog and builds a list of chapters
        /// for the user to choose from.
        /// </summary>
        /// <param name="workbook"></param>
        public SelectChaptersDialog(Workbook workbook, Mode mode, string title = null)
        {
            InitializeComponent();

            _workbook = workbook;
            _mode = mode;

            int index = 0;
            foreach (Chapter ch in _workbook.Chapters)
            {
                index++;
                SelectedChapter sel = new SelectedChapter();
                sel.Chapter = ch;
                sel.ChapterTitle = index.ToString() + ". " + ch.GetTitle();
                sel.IsSelected = false;
                ChapterList.Add(sel);
            }
            if (title != null)
            {
                UiLblInstruct.Content = title;
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
            if (_workbook.IsReady)
            {
                ExitOK = true;
                Close();
            }
            else
            {
                MessageBox.Show(Properties.Resources.DataProcessingInProgress, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// Cancel button was clicked. Exits with the result = false
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _workbook.GamesManager.CancelAll();
            ExitOK = false;
            Close();
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            if (_mode == Mode.IMPORT)
            {
                System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Importing-Data-into-Chess-Forge#importing-chapters");
            }
            else if (_mode == Mode.MERGE)
            {
                System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Merging-Chapters");
            }
            else
            {
                System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/User's-Manual");
            }
        }

    }

    /// <summary>
    /// Helper class to bind with the list view
    /// </summary>
    public class SelectedChapter : INotifyPropertyChanged
    {
        // Chapter object
        public Chapter Chapter { get; set; }

        // Chapter title
        public string ChapterTitle { get; set; }

        // Chapter selection flag
        private bool _isSelected;

        /// <summary>
        /// PropertChange event handler
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Accessor to _isSelected.
        /// This is the only property that can be changed
        /// from the GUI.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Notifies the framework of the change in the bound data.
        /// </summary>
        /// <param name="propertyName"></param>
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

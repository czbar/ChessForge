using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SearchPgnFilesDialog.xaml
    /// </summary>
    public partial class SearchPgnFilesDialog : Window
    {
        // root folder to search for PGN files in
        private string _rootFolder;

        // whether a search is in progress
        private bool _isSearchInProgress = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="crits"></param>
        public SearchPgnFilesDialog(SearchPositionCriteria crits)
        {
            InitializeComponent();
            _rootFolder = Configuration.LastOpenDirectory;
            UiTbDirectory.Text = _rootFolder;
        }

        /// <summary>
        /// Gets the list of PGN files in the passed folder and its subfolders.
        /// </summary>
        /// <param name="rootFolder"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetPgnFilesSafe(string rootFolder)
        {
            if (string.IsNullOrWhiteSpace(rootFolder))
                yield break;

            var pending = new Stack<string>();
            pending.Push(rootFolder);

            while (pending.Count > 0)
            {
                string currentFolder = pending.Pop();

                string[] subfolders;
                try
                {
                    subfolders = Directory.GetDirectories(currentFolder);
                }
                catch
                {
                    continue;
                }

                foreach (string subfolder in subfolders)
                    pending.Push(subfolder);

                string[] files;
                try
                {
                    files = Directory.GetFiles(currentFolder, "*.pgn");
                }
                catch
                {
                    continue;
                }

                foreach (string file in files)
                    yield return file;
            }
        }

        /// <summary>
        /// In response to the user clicking the select button,
        /// opens the dialog to select the root folder to search for PGN files in.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            _rootFolder = FolderPicker.ShowDialog(this, Properties.Resources.SelectFolder, UiTbDirectory.Text);
        }

        /// <summary>
        /// The user clicks the start/stop button, we either start the search for the position in the PGN files
        /// or stop it if it is already in progress.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (_isSearchInProgress)
            {
                // stop the search
                _isSearchInProgress = false;
                UiBtnStartStop.Content = Properties.Resources.Search;
            }
            else
            {

                UiBtnStartStop.Content = Properties.Resources.Stop;
                List<string> files = GetPgnFilesSafe(_rootFolder).ToList();
                if (files.Count > 0)
                {
                    _isSearchInProgress = true;
                    // begin background search for the position in the files
                    //TODO: implement the search and report the results in the dialog
                }
            }
        }

        /// <summary>
        /// The user clicks the close button, we just close the dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

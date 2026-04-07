using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        /// <summary>
        /// Object that manages the background search for the position in the PGN files.
        /// </summary>
        private BkgSearchPositionManager _bkgSearchManager;

        // root folder to search for PGN files in
        private string _rootFolder;

        // the criteria for searching for the position in the PGN files
        private SearchPositionCriteria _searchCrits;

        /// <summary>
        /// Whether the search for the position in the PGN files is in progress. 
        /// This is set to true when the user clicks the start button 
        /// and set to false when the user clicks the stop button or when the search finishes.
        /// </summary>
        public bool IsSearchInProgress = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="crits"></param>
        public SearchPgnFilesDialog(SearchPositionCriteria crits)
        {
            InitializeComponent();
            _rootFolder = Configuration.LastOpenDirectory;
            _searchCrits = crits;
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
            string folder = FolderPicker.ShowDialog(this, Properties.Resources.SelectFolder, UiTbDirectory.Text);
            if (folder != null)
            {
                _rootFolder = folder;
            }
        }

        /// <summary>
        /// The user clicks the start/stop button, we either start the search for the position in the PGN files
        /// or stop it if it is already in progress.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (IsSearchInProgress)
            {
                // stop the search
                IsSearchInProgress = false;
                UiBtnStartStop.Content = Properties.Resources.Search;
            }
            else
            {

                ObservableCollection<string> files = new ObservableCollection<string>(GetPgnFilesSafe(_rootFolder));
                if (files.Count > 0)
                {
                    UiBtnStartStop.Content = Properties.Resources.Stop;
                    IsSearchInProgress = true;
                    UiLbFiles.Items.Clear();
                    _bkgSearchManager = new BkgSearchPositionManager(this, _searchCrits);
                    _bkgSearchManager.Execute(files);
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

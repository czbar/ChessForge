using System.Collections.ObjectModel;

namespace ChessForge
{
    /// <summary>
    /// A class of objects that will be passed to and from the background worker
    /// </summary>
    public class BkgFindPgnFilesData
    {
        /// <summary>
        /// Root folder to search for PGN files in.
        /// </summary>
        public string RootFolder;

        /// <summary>
        /// List of found PGN files.
        /// </summary>
        public ObservableCollection<string> FoundFiles;
    }
}

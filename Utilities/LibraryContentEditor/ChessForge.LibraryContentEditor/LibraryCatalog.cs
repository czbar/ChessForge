using System.Collections.ObjectModel;

namespace ChessForge.LibraryContentEditor
{
    /// <summary>
    /// Root object for a ChessForge library-content document.
    /// </summary>
    public sealed class LibraryCatalog
    {
        public LibraryCatalog()
        {
            Bookcases = new ObservableCollection<BookcaseNode>();
        }

        public ObservableCollection<BookcaseNode> Bookcases { get; private set; }
    }
}


using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChessForge.LibraryContentEditor
{
    public abstract class LibraryNode : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _description = string.Empty;
        private bool _isExpanded;
        private bool _isSelected;

        public event PropertyChangedEventHandler PropertyChanged;

        public abstract string Kind { get; }

        public string Name
        {
            get { return _name; }
            set
            {
                if (SetField(ref _name, value ?? string.Empty))
                {
                    OnPropertyChanged("DisplayName");
                }
            }
        }

        public string Description
        {
            get { return _description; }
            set { SetField(ref _description, value ?? string.Empty); }
        }

        public string DisplayName
        {
            get
            {
                string name = string.IsNullOrWhiteSpace(Name) ? "(unnamed)" : Name;
                return Kind + ": " + name;
            }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { SetField(ref _isExpanded, value); }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetField(ref _isSelected, value); }
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public sealed class BookcaseNode : LibraryNode
    {
        public BookcaseNode()
        {
            Shelves = new ObservableCollection<ShelfNode>();
        }

        public override string Kind { get { return "Bookcase"; } }

        public ObservableCollection<ShelfNode> Shelves { get; private set; }
    }

    public sealed class ShelfNode : LibraryNode
    {
        public ShelfNode()
        {
            Books = new ObservableCollection<BookNode>();
        }

        public override string Kind { get { return "Shelf"; } }

        public ObservableCollection<BookNode> Books { get; private set; }
    }

    public sealed class BookNode : LibraryNode
    {
        private string _filePath = string.Empty;

        public override string Kind { get { return "Book"; } }

        public string FilePath
        {
            get { return _filePath; }
            set { SetField(ref _filePath, value ?? string.Empty); }
        }
    }
}


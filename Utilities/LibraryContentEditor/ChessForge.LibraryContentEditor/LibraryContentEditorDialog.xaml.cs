using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ChessForge.LibraryContentEditor
{
    /// <summary>
    /// Reusable modal editor for a ChessForge LibraryContent.txt file.
    /// Construct it with an optional file path and call ShowDialog().
    /// </summary>
    public partial class LibraryContentEditorDialog : Window
    {
        private LibraryNode _selectedNode;
        private bool _dirty;
        private bool _suppressChanges;
        private bool _allowClose;

        public LibraryContentEditorDialog()
            : this(null)
        {
        }

        public LibraryContentEditorDialog(string initialFilePath)
        {
            InitializeComponent();
            SetCatalog(new LibraryCatalog(), null, false);

            if (!string.IsNullOrWhiteSpace(initialFilePath))
            {
                Loaded += delegate { TryLoadFile(initialFilePath); };
            }
        }

        public LibraryCatalog Catalog { get; private set; }

        public string CurrentFilePath { get; private set; }

        public bool HasUnsavedChanges { get { return _dirty; } }

        private void SetCatalog(LibraryCatalog catalog, string filePath, bool dirty)
        {
            if (Catalog != null)
            {
                foreach (LibraryNode node in EnumerateNodes(Catalog))
                {
                    node.PropertyChanged -= Node_PropertyChanged;
                }
            }

            Catalog = catalog;
            CurrentFilePath = filePath;
            _selectedNode = null;

            foreach (LibraryNode node in EnumerateNodes(Catalog))
            {
                node.PropertyChanged += Node_PropertyChanged;
            }

            LibraryTree.ItemsSource = Catalog.Bookcases;
            ShowSelectedNode(null);
            SetDirty(dirty);
            UpdateStatus();

            if (Catalog.Bookcases.Count > 0)
            {
                SelectNode(Catalog.Bookcases[0]);
            }
        }

        private static IEnumerable<LibraryNode> EnumerateNodes(LibraryCatalog catalog)
        {
            foreach (BookcaseNode bookcase in catalog.Bookcases)
            {
                yield return bookcase;
                foreach (ShelfNode shelf in bookcase.Shelves)
                {
                    yield return shelf;
                    foreach (BookNode book in shelf.Books)
                    {
                        yield return book;
                    }
                }
            }
        }

        private void SubscribeNode(LibraryNode node)
        {
            node.PropertyChanged += Node_PropertyChanged;
        }

        private void Node_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_suppressChanges)
            {
                return;
            }

            if (e.PropertyName == "Name" || e.PropertyName == "Description" || e.PropertyName == "FilePath")
            {
                SetDirty(true);
            }
        }

        private void SetDirty(bool value)
        {
            _dirty = value;
            string fileName = string.IsNullOrWhiteSpace(CurrentFilePath)
                ? "Untitled"
                : Path.GetFileName(CurrentFilePath);
            Title = (value ? "*" : string.Empty) + fileName + " — Library Content Editor";
        }

        private void UpdateStatus(string message = null)
        {
            int shelfCount = Catalog.Bookcases.Sum(bookcase => bookcase.Shelves.Count);
            int bookCount = Catalog.Bookcases.Sum(bookcase => bookcase.Shelves.Sum(shelf => shelf.Books.Count));
            string counts = Catalog.Bookcases.Count + " bookcase(s), " + shelfCount + " shelf/shelves, " + bookCount + " book(s)";
            StatusTextBlock.Text = string.IsNullOrWhiteSpace(message) ? counts : message + "  •  " + counts;
        }

        private void LibraryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ShowSelectedNode(e.NewValue as LibraryNode);
        }

        private void ShowSelectedNode(LibraryNode node)
        {
            _selectedNode = node;
            bool hasSelection = node != null;
            PropertyEditor.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
            NoSelectionTextBlock.Visibility = hasSelection ? Visibility.Collapsed : Visibility.Visible;
            PropertyEditor.DataContext = node;
            NodeKindTextBlock.Text = hasSelection ? node.Kind : string.Empty;
            FilePathEditor.Visibility = node is BookNode ? Visibility.Visible : Visibility.Collapsed;
            UpdateActionStates();
        }

        private void UpdateActionStates()
        {
            BookcaseNode owningBookcase = FindBookcase(_selectedNode);
            ShelfNode owningShelf = FindShelf(_selectedNode);
            bool canAddShelf = owningBookcase != null;
            bool canAddBook = owningShelf != null;
            bool canDelete = _selectedNode != null;
            int selectedIndex;
            int siblingCount;
            GetSiblingPosition(_selectedNode, out selectedIndex, out siblingCount);

            AddShelfButton.IsEnabled = canAddShelf;
            AddShelfMenuItem.IsEnabled = canAddShelf;
            AddBookButton.IsEnabled = canAddBook;
            AddBookMenuItem.IsEnabled = canAddBook;
            DeleteButton.IsEnabled = canDelete;
            DeleteMenuItem.IsEnabled = canDelete;
            MoveUpButton.IsEnabled = selectedIndex > 0;
            MoveUpMenuItem.IsEnabled = selectedIndex > 0;
            MoveDownButton.IsEnabled = selectedIndex >= 0 && selectedIndex < siblingCount - 1;
            MoveDownMenuItem.IsEnabled = selectedIndex >= 0 && selectedIndex < siblingCount - 1;
        }

        private void AddBookcase_Click(object sender, RoutedEventArgs e)
        {
            BookcaseNode bookcase = new BookcaseNode { Name = "New bookcase", IsExpanded = true };
            Catalog.Bookcases.Add(bookcase);
            SubscribeNode(bookcase);
            SetDirty(true);
            SelectNode(bookcase);
            UpdateStatus("Bookcase added");
        }

        private void AddShelf_Click(object sender, RoutedEventArgs e)
        {
            BookcaseNode bookcase = FindBookcase(_selectedNode);
            if (bookcase == null)
            {
                return;
            }

            ShelfNode shelf = new ShelfNode { Name = "New shelf", IsExpanded = true };
            bookcase.Shelves.Add(shelf);
            SubscribeNode(shelf);
            bookcase.IsExpanded = true;
            SetDirty(true);
            SelectNode(shelf);
            UpdateStatus("Shelf added");
        }

        private void AddBook_Click(object sender, RoutedEventArgs e)
        {
            ShelfNode shelf = FindShelf(_selectedNode);
            if (shelf == null)
            {
                return;
            }

            BookNode book = new BookNode { Name = "New book" };
            shelf.Books.Add(book);
            SubscribeNode(book);
            shelf.IsExpanded = true;
            BookcaseNode bookcase = FindBookcase(shelf);
            if (bookcase != null)
            {
                bookcase.IsExpanded = true;
            }

            SetDirty(true);
            SelectNode(book);
            UpdateStatus("Book added");
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode == null)
            {
                return;
            }

            int childCount = GetDescendantCount(_selectedNode);
            if (childCount > 0)
            {
                MessageBoxResult result = MessageBox.Show(
                    this,
                    "Delete '" + _selectedNode.Name + "' and its " + childCount + " child item(s)?",
                    "Confirm deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            LibraryNode nodeToSelect = RemoveNode(_selectedNode);
            SetDirty(true);
            SelectNode(nodeToSelect);
            UpdateStatus("Item deleted");
        }

        private LibraryNode RemoveNode(LibraryNode node)
        {
            BookcaseNode bookcase = node as BookcaseNode;
            if (bookcase != null)
            {
                int index = Catalog.Bookcases.IndexOf(bookcase);
                Catalog.Bookcases.Remove(bookcase);
                return ChooseNeighbour(Catalog.Bookcases.Cast<LibraryNode>().ToList(), index);
            }

            ShelfNode shelf = node as ShelfNode;
            if (shelf != null)
            {
                BookcaseNode parentBookcase = FindBookcase(shelf);
                int index = parentBookcase.Shelves.IndexOf(shelf);
                parentBookcase.Shelves.Remove(shelf);
                LibraryNode neighbour = ChooseNeighbour(parentBookcase.Shelves.Cast<LibraryNode>().ToList(), index);
                return neighbour ?? parentBookcase;
            }

            BookNode book = node as BookNode;
            if (book != null)
            {
                ShelfNode parentShelf = FindShelf(book);
                int index = parentShelf.Books.IndexOf(book);
                parentShelf.Books.Remove(book);
                LibraryNode neighbour = ChooseNeighbour(parentShelf.Books.Cast<LibraryNode>().ToList(), index);
                return neighbour ?? parentShelf;
            }

            return null;
        }

        private static LibraryNode ChooseNeighbour(IList<LibraryNode> siblings, int removedIndex)
        {
            if (siblings.Count == 0)
            {
                return null;
            }

            return siblings[Math.Min(removedIndex, siblings.Count - 1)];
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            MoveSelected(-1);
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            MoveSelected(1);
        }

        private void MoveSelected(int offset)
        {
            if (_selectedNode == null)
            {
                return;
            }

            BookcaseNode bookcase = _selectedNode as BookcaseNode;
            if (bookcase != null)
            {
                MoveInCollection(Catalog.Bookcases, bookcase, offset);
            }
            else
            {
                ShelfNode shelf = _selectedNode as ShelfNode;
                if (shelf != null)
                {
                    MoveInCollection(FindBookcase(shelf).Shelves, shelf, offset);
                }
                else
                {
                    BookNode book = _selectedNode as BookNode;
                    if (book != null)
                    {
                        MoveInCollection(FindShelf(book).Books, book, offset);
                    }
                }
            }

            SetDirty(true);
            SelectNode(_selectedNode);
            UpdateActionStates();
        }

        private static void MoveInCollection<T>(ObservableCollection<T> collection, T item, int offset)
        {
            int oldIndex = collection.IndexOf(item);
            int newIndex = oldIndex + offset;
            if (oldIndex >= 0 && newIndex >= 0 && newIndex < collection.Count)
            {
                collection.Move(oldIndex, newIndex);
            }
        }

        private void SelectNode(LibraryNode node)
        {
            _suppressChanges = true;
            try
            {
                foreach (LibraryNode catalogNode in EnumerateNodes(Catalog))
                {
                    catalogNode.IsSelected = catalogNode == node;
                }

                if (node != null)
                {
                    ShelfNode shelf = FindShelf(node);
                    BookcaseNode bookcase = FindBookcase(node);
                    if (shelf != null)
                    {
                        shelf.IsExpanded = true;
                    }
                    if (bookcase != null)
                    {
                        bookcase.IsExpanded = true;
                    }
                }
            }
            finally
            {
                _suppressChanges = false;
            }

            ShowSelectedNode(node);
            Dispatcher.BeginInvoke(new Action(delegate { LibraryTree.Focus(); }), DispatcherPriority.Input);
        }

        private BookcaseNode FindBookcase(LibraryNode node)
        {
            if (node == null)
            {
                return null;
            }

            BookcaseNode selectedBookcase = node as BookcaseNode;
            if (selectedBookcase != null)
            {
                return selectedBookcase;
            }

            foreach (BookcaseNode bookcase in Catalog.Bookcases)
            {
                if (node is ShelfNode && bookcase.Shelves.Contains((ShelfNode)node))
                {
                    return bookcase;
                }

                if (node is BookNode && bookcase.Shelves.Any(shelf => shelf.Books.Contains((BookNode)node)))
                {
                    return bookcase;
                }
            }

            return null;
        }

        private ShelfNode FindShelf(LibraryNode node)
        {
            if (node == null)
            {
                return null;
            }

            ShelfNode selectedShelf = node as ShelfNode;
            if (selectedShelf != null)
            {
                return selectedShelf;
            }

            BookNode book = node as BookNode;
            if (book != null)
            {
                foreach (BookcaseNode bookcase in Catalog.Bookcases)
                {
                    ShelfNode shelf = bookcase.Shelves.FirstOrDefault(item => item.Books.Contains(book));
                    if (shelf != null)
                    {
                        return shelf;
                    }
                }
            }

            return null;
        }

        private void GetSiblingPosition(LibraryNode node, out int index, out int count)
        {
            index = -1;
            count = 0;
            if (node is BookcaseNode)
            {
                index = Catalog.Bookcases.IndexOf((BookcaseNode)node);
                count = Catalog.Bookcases.Count;
            }
            else if (node is ShelfNode)
            {
                BookcaseNode bookcase = FindBookcase(node);
                if (bookcase != null)
                {
                    index = bookcase.Shelves.IndexOf((ShelfNode)node);
                    count = bookcase.Shelves.Count;
                }
            }
            else if (node is BookNode)
            {
                ShelfNode shelf = FindShelf(node);
                if (shelf != null)
                {
                    index = shelf.Books.IndexOf((BookNode)node);
                    count = shelf.Books.Count;
                }
            }
        }

        private static int GetDescendantCount(LibraryNode node)
        {
            BookcaseNode bookcase = node as BookcaseNode;
            if (bookcase != null)
            {
                return bookcase.Shelves.Count + bookcase.Shelves.Sum(shelf => shelf.Books.Count);
            }

            ShelfNode shelfNode = node as ShelfNode;
            return shelfNode == null ? 0 : shelfNode.Books.Count;
        }

        private void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (ConfirmAbandonChanges())
            {
                SetCatalog(new LibraryCatalog(), null, false);
                UpdateStatus("New document");
            }
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!ConfirmAbandonChanges())
            {
                return;
            }

            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Open ChessForge library content",
                Filter = "Library content files (*.txt)|*.txt|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog(this) == true)
            {
                TryLoadFile(dialog.FileName);
            }
        }

        private bool TryLoadFile(string path)
        {
            try
            {
                string text = File.ReadAllText(path, Encoding.UTF8);
                LibraryCatalog catalog = LibraryContentSerializer.Parse(text);
                SetCatalog(catalog, Path.GetFullPath(path), false);
                UpdateStatus("Opened " + Path.GetFileName(path));
                return true;
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "Cannot open library content", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveDocument(false);
        }

        private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveDocument(true);
        }

        private bool SaveDocument(bool saveAs)
        {
            IList<string> errors = LibraryContentSerializer.Validate(Catalog);
            if (errors.Count > 0)
            {
                ShowValidationErrors(errors);
                return false;
            }

            string destination = CurrentFilePath;
            if (saveAs || string.IsNullOrWhiteSpace(destination))
            {
                SaveFileDialog dialog = new SaveFileDialog
                {
                    Title = "Save ChessForge library content",
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FileName = string.IsNullOrWhiteSpace(CurrentFilePath) ? "LibraryContent.txt" : Path.GetFileName(CurrentFilePath),
                    OverwritePrompt = true,
                    AddExtension = true,
                    DefaultExt = ".txt"
                };

                if (!string.IsNullOrWhiteSpace(CurrentFilePath))
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(CurrentFilePath);
                }

                if (dialog.ShowDialog(this) != true)
                {
                    return false;
                }

                destination = dialog.FileName;
            }

            try
            {
                string content = LibraryContentSerializer.Serialize(Catalog);
                File.WriteAllText(destination, content, new UTF8Encoding(false));
                CurrentFilePath = Path.GetFullPath(destination);
                SetDirty(false);
                UpdateStatus("Saved " + Path.GetFileName(destination));
                return true;
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "Cannot save library content", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            IList<string> errors = LibraryContentSerializer.Validate(Catalog);
            if (errors.Count == 0)
            {
                MessageBox.Show(this, "The document is valid and ready to save.", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                ShowValidationErrors(errors);
            }
        }

        private void ShowValidationErrors(IList<string> errors)
        {
            string message = "Please fix the following before saving:" + Environment.NewLine + Environment.NewLine +
                             string.Join(Environment.NewLine, errors.Select(error => "• " + error));
            MessageBox.Show(this, message, "Library content validation", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private bool ConfirmAbandonChanges()
        {
            if (!_dirty)
            {
                return true;
            }

            MessageBoxResult result = MessageBox.Show(
                this,
                "Save changes to the current library-content file?",
                "Unsaved changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
            {
                return false;
            }

            return result == MessageBoxResult.No || SaveDocument(false);
        }

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            BookNode book = _selectedNode as BookNode;
            if (book == null)
            {
                return;
            }

            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Choose the book/workbook file",
                Filter = "ChessForge workbooks (*.pgn)|*.pgn|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (!string.IsNullOrWhiteSpace(CurrentFilePath))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(CurrentFilePath);
            }

            if (dialog.ShowDialog(this) == true)
            {
                // in the Codex generated first version
                // MakeDocumentRelativePath(dialog.FileName) as called here;
                // Probably because Codex was given a sample Library.txt file with relative paths
                book.FilePath = dialog.FileName;
            }
        }

        /// <summary>
        /// Converts an absolute file path to a relative path based on the current document's location.
        /// </summary>
        /// <param name="selectedPath"></param>
        /// <returns></returns>
        private string MakeDocumentRelativePath(string selectedPath)
        {
            if (string.IsNullOrWhiteSpace(CurrentFilePath))
            {
                return selectedPath.Replace('\\', '/');
            }

            string documentDirectory = Path.GetDirectoryName(Path.GetFullPath(CurrentFilePath));
            Uri directoryUri = new Uri(AppendDirectorySeparator(documentDirectory));
            Uri fileUri = new Uri(Path.GetFullPath(selectedPath));
            Uri relativeUri = directoryUri.MakeRelativeUri(fileUri);
            if (relativeUri.IsAbsoluteUri)
            {
                return selectedPath.Replace('\\', '/');
            }

            return Uri.UnescapeDataString(relativeUri.ToString()).Replace('\\', '/');
        }

        private static string AppendDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        private void SaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            if (SaveDocument(false))
            {
                _allowClose = true;
                DialogResult = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_allowClose && !ConfirmAbandonChanges())
            {
                e.Cancel = true;
            }
        }
    }
}


using ChessPosition;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WebAccess;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for OnlineLibraryContentDialog.xaml
    /// </summary>
    public partial class OnlineLibraryContentDialog : Window
    {
        /// <summary>
        /// Enumeration for objects in the library.
        /// </summary>
        private enum LibObjectType
        {
            NONE,
            BOOKCASE,
            SHELF,
            BOOK,
            BOOKS
        }


        // the listing of the content of the library
        private LibraryContent _content;

        // dictionary mapping Runs to Workbook that they represent
        private Dictionary<Run, WebAccess.Book> _dictRunBooks = new Dictionary<Run, WebAccess.Book>();

        /// <summary>
        /// A library workbook object selected by the user.
        /// </summary>
        public WebAccess.Book SelectedBook = null;

        // prefix for a bookcase paragraph
        private string _para_bookcase_ = "ParaBookcase";

        // prefix for a bookcase run
        private string _run_bookcase_ = "RunBookcase";

        // prefix for a shelf paragraph
        private string _para_shelf_ = "ParaShelf";

        // prefix for a shelf run
        private string _run_shelf_ = "RunShelf";

        // prefix for a books paragraph
        private string _para_books_ = "ParaBooks";

        // separator to parse element names by
        private char NAME_SEPAR = '_';

        // set on exit if user clicks the Libraries button
        public bool ShowLibraries = false;

        /// <summary>
        /// Constructor. Builds the content of the Rich Text Box.
        /// </summary>
        /// <param name="content"></param>
        public OnlineLibraryContentDialog(LibraryContent content)
        {
            InitializeComponent();
            _content = content;

            UiBtnLibraries.Content = Properties.Resources.Libraries + "...";

            SetObjectIds();
            BuildTitleParagraph();
            BuildMessagesParagraph();

            BuildBookcases();
        }

        /// <summary>
        /// Sets unique IDs for bookcase and shelf objects
        /// </summary>
        private void SetObjectIds()
        {
            char bookcaseId = 'A';

            foreach (Bookcase bookcase in _content.Bookcases)
            {
                bookcase.Id = bookcaseId.ToString();

                uint shelfId = 1;
                foreach (Shelf shelf in bookcase.Shelves)
                {
                    shelf.Id = shelfId.ToString();
                    shelf.ParentBookcaseId = bookcaseId.ToString();
                    shelf.ShelfPathId = bookcase.Id + NAME_SEPAR + shelf.Id;

                    uint bookId = 1;
                    foreach (Book book in shelf.Books)
                    {
                        book.Id = bookId.ToString();
                        book.BookPathId = shelf.ShelfPathId + NAME_SEPAR + book.Id;
                        bookId++;
                    }

                    shelfId++;
                }
                bookcaseId++;
            }
        }

        /// <summary>
        /// Insert the title for the view. 
        /// </summary>
        private void BuildTitleParagraph()
        {
            Paragraph para = new Paragraph
            {
                Margin = new Thickness(10, 10, 0, 0),
            };

            Run run = new Run();
            run.Text = Properties.Resources.ChessForgeOnlineLibrary;
            run.FontWeight = FontWeights.Bold;
            run.FontSize = 18 + Configuration.FontSizeDiff;
            para.Inlines.Add(run);

            UiRtbOnlineLibrary.Document.Blocks.Add(para);
        }

        /// <summary>
        /// Inserts paragraph with "messages", if any.
        /// </summary>
        private void BuildMessagesParagraph()
        {
            Paragraph para = new Paragraph
            {
                Margin = new Thickness(10, 0, 0, 0),
            };

            Run run = new Run(Properties.Resources.OnlineLibraryInfo);
            run.FontWeight = FontWeights.Normal;
            run.FontSize = 14 + Configuration.FontSizeDiff;

            foreach (string msg in _content.Messages)
            {
                run.Text += "\n";
                run.Text += (msg);
            }
            para.Inlines.Add(run);

            UiRtbOnlineLibrary.Document.Blocks.Add(para);
        }

        //**********************************************************
        //
        // Building the view of the content of the library.
        // There is a Paragraph for each Bookcase followed by Paragraphs
        // for each Shelf in that Bookcase, followed by a single Paragraph
        // with a Run for each Book in the Shelf.
        //
        //**********************************************************

        /// <summary>
        /// Build or re-builds bookcase paragraphs.
        /// </summary>
        private void BuildBookcases()
        {
            UiRtbOnlineLibrary.Document.Blocks.Clear();

            foreach (WebAccess.Bookcase bookcase in _content.Bookcases)
            {
                BuildBookcaseParagraph(bookcase);
            }
        }

        /// <summary>
        /// Build paragraphs for a single bookcase.
        /// </summary>
        /// <param name="bookcase"></param>
        private void BuildBookcaseParagraph(WebAccess.Bookcase bookcase)
        {
            string paraName = _para_bookcase_ + NAME_SEPAR + bookcase.Id;

            Paragraph para = new Paragraph
            {
                Margin = new Thickness(20, 0, 0, 0),
                Name = paraName
            };

            BuildBookcaseTitleAndDescription(para, bookcase);
            UiRtbOnlineLibrary.Document.Blocks.Add(para);

            if (bookcase.IsExpanded)
            {
                BuildShelves(bookcase);
            }
        }


        /// <summary>
        /// Build shelves paragraphs.
        /// </summary>
        /// <param name="bookcase"></param>
        /// <param name="bookcaseId"></param>
        private void BuildShelves(WebAccess.Bookcase bookcase)
        {
            foreach (WebAccess.Shelf shelf in bookcase.Shelves)
            {
                BuildShelfParagraph(shelf);
            }
        }

        /// <summary>
        /// Build paragraphs for a single shelf.
        /// </summary>
        /// <param name="shelf"></param>
        private Paragraph BuildShelfParagraph(WebAccess.Shelf shelf)
        {
            string paraName = _para_shelf_ + NAME_SEPAR + shelf.ShelfPathId;

            Paragraph para = new Paragraph
            {
                Margin = new Thickness(30, 0, 0, 0),
                Name = paraName
            };

            BuildShelfTitleAndDescription(para, shelf);
            UiRtbOnlineLibrary.Document.Blocks.Add(para);

            if (shelf.IsExpanded)
            {
                BuildBooksParagraph(shelf);
            }

            return para;
        }

        /// <summary>
        /// Build a paragraph with book titles.
        /// </summary>
        /// <param name="books"></param>
        /// <param name="bookcaseId"></param>
        /// <param name="shelfId"></param>
        private Paragraph BuildBooksParagraph(Shelf shelf)
        {
            string paraName = _para_books_ + NAME_SEPAR + shelf.ShelfPathId;

            Paragraph para = new Paragraph
            {
                Margin = new Thickness(40, 0, 0, 0),
                Name = paraName
            };

            Paragraph lastPara = para;

            bool first = true;
            foreach (WebAccess.Book book in shelf.Books)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    para.Inlines.Add(new Run("\n"));
                }

                BuildBookTitleAndDescription(para, shelf, book);
            }
            UiRtbOnlineLibrary.Document.Blocks.Add(para);

            return lastPara;
        }

        //************************************************************************
        //
        // Title and Description Runs 
        //
        //************************************************************************

        /// <summary>
        /// Builds a Title and a Description Run for the Bookcase
        /// and inserts them into the passed Paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="bookcase"></param>
        private void BuildBookcaseTitleAndDescription(Paragraph para, Bookcase bookcase)
        {
            Run runTitle = new Run();
            runTitle.FontWeight = FontWeights.Bold;
            runTitle.FontSize = 16 + Configuration.FontSizeDiff;

            char expSymbol = bookcase.IsExpanded ? Constants.CHAR_BLACK_TRIANGLE_DOWN : Constants.CHAR_BLACK_TRIANGLE_UP;
            runTitle.Text = "\n" + expSymbol + Properties.Resources.Bookcase + " " + bookcase.Id + ": " + bookcase.Title;
            runTitle.Name = _run_bookcase_ + NAME_SEPAR + bookcase.Id;
            runTitle.MouseDown += EventTitleLineClicked;
            runTitle.Cursor = Cursors.Hand;
            para.Inlines.Add(runTitle);

            if (bookcase.IsExpanded && !string.IsNullOrEmpty(bookcase.Description))
            {
                Run descript = new Run("\n");
                descript.FontWeight = FontWeights.Normal;
                descript.FontSize = 14 + Configuration.FontSizeDiff;
                descript.Text += bookcase.Description;
                para.Inlines.Add(descript);
            }
        }

        /// <summary>
        /// Builds a Title and a Description Run for the Bookcase
        /// and inserts them into the passed Paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="bookcase"></param>
        private void BuildShelfTitleAndDescription(Paragraph para, Shelf shelf)
        {
            string screenId = shelf.ParentBookcaseId.ToString() + shelf.Id.ToString();

            Run runTitle = new Run();
            runTitle.FontWeight = FontWeights.Bold;
            runTitle.FontSize = 14 + Configuration.FontSizeDiff;

            char expSymbol = shelf.IsExpanded ? Constants.CHAR_BLACK_TRIANGLE_DOWN : Constants.CHAR_BLACK_TRIANGLE_UP;
            runTitle.Text = "\n" + expSymbol + Properties.Resources.Shelf + " " + screenId + ": " + shelf.Title;
            runTitle.Name = _run_shelf_ + NAME_SEPAR + shelf.ShelfPathId;
            runTitle.MouseDown += EventTitleLineClicked;
            runTitle.Cursor = Cursors.Hand;
            para.Inlines.Add(runTitle);

            if (shelf.IsExpanded && !string.IsNullOrEmpty(shelf.Description))
            {
                Run descript = new Run("\n" + shelf.Description);
                descript.FontWeight = FontWeights.Normal;
                descript.FontSize = 14 + Configuration.FontSizeDiff;
                para.Inlines.Add(descript);
            }
        }

        /// <summary>
        /// Builds a Title and a Description Run for the Book
        /// and inserts them into the passed Paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="bookcase"></param>
        private void BuildBookTitleAndDescription(Paragraph para, Shelf shelf, Book book)
        {
            string prefix = shelf.ParentBookcaseId.ToString() + shelf.Id.ToString() + "." + book.Id.ToString() + ": ";
            Run rPrefix = new Run(prefix);
            rPrefix.FontWeight = FontWeights.Bold;
            rPrefix.FontSize = 14 + Configuration.FontSizeDiff;
            para.Inlines.Add(rPrefix);

            Run title = new Run(book.Title);
            title.FontWeight = FontWeights.Normal;
            title.FontSize = 14 + Configuration.FontSizeDiff;
            title.TextDecorations = TextDecorations.Underline;
            title.Foreground = Brushes.Blue;
            title.Cursor = Cursors.Hand;
            title.MouseDown += EventBookTitle;

            _dictRunBooks[title] = book;
            para.Inlines.Add(title);

            if (!string.IsNullOrEmpty(book.Description))
            {
                Run descript = new Run("      " + book.Description);
                descript.FontWeight = FontWeights.Normal;
                descript.FontSize = 14 + Configuration.FontSizeDiff;
                descript.FontStyle = FontStyles.Italic;
                para.Inlines.Add(descript);
            }
        }


        //************************************************************************
        //
        // Paragraph content checks. 
        //
        //************************************************************************

        /// <summary>
        /// Checks if the passed bookcase paragraph is followed by a Shelf paragraph.
        /// </summary>
        /// <param name="bookcasePara"></param>
        /// <returns></returns>
        private bool HasShelves(Paragraph bookcasePara)
        {
            bool hasShelves = false;

            bool foundPara = false;
            foreach (Block block in UiRtbOnlineLibrary.Document.Blocks)
            {
                if (foundPara)
                {
                    if (block is Paragraph para)
                    {
                        if (para.Name != null && para.Name.StartsWith(_para_shelf_))
                        {
                            hasShelves = true;
                        }
                        break;
                    }
                }
                else
                {
                    if ((block as Paragraph) == bookcasePara)
                    {
                        foundPara = true;
                    }
                }
            }

            return hasShelves;
        }

        /// <summary>
        /// Checks if the passed Shelf paragraph is followed by a Shelf paragraph.
        /// </summary>
        /// <param name="bookcasePara"></param>
        /// <returns></returns>
        private bool HasBooks(Paragraph shelfPara)
        {
            bool hasBooks = false;

            bool foundPara = false;
            foreach (Block block in UiRtbOnlineLibrary.Document.Blocks)
            {
                if (foundPara)
                {
                    if (block is Paragraph para)
                    {
                        if (para.Name != null && para.Name.StartsWith(_para_books_))
                        {
                            hasBooks = true;
                        }
                        break;
                    }
                }
                else
                {
                    if ((block as Paragraph) == shelfPara)
                    {
                        foundPara = true;
                    }
                }
            }

            return hasBooks;
        }


        //************************************************************************
        //
        // Clear paragraphs methods. 
        //
        //************************************************************************


        /// <summary>
        /// Finds the requested bookcase paragraph and removes all shelf paras that follow.
        /// </summary>
        /// <param name="bookcasePara"></param>
        private void ClearShelves(Paragraph bookcasePara, Bookcase bookcase)
        {
            bool foundPara = false;
            List<Paragraph> parasToRemove = new List<Paragraph>();
            foreach (Block block in UiRtbOnlineLibrary.Document.Blocks)
            {
                if (foundPara)
                {
                    if (block is Paragraph para)
                    {
                        if (para.Name != null && para.Name.StartsWith(_para_shelf_))
                        {
                            parasToRemove.Add(para);
                        }
                        else if (para.Name != null && para.Name.StartsWith(_para_bookcase_))
                        {
                            break;
                        }
                    }
                }
                else
                {
                    if ((block as Paragraph) == bookcasePara)
                    {
                        foundPara = true;
                    }
                }
            }

            foreach (Paragraph para in parasToRemove)
            {
                RemoveBooksPara(para);
                UiRtbOnlineLibrary.Document.Blocks.Remove(para);
            }
        }

        /// <summary>
        /// Finds the requested shelf paragraph and removes the following Books paragraphs.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="shelf"></param>
        private void RemoveBooksPara(Paragraph shelfPara)
        {
            bool foundPara = false;
            Paragraph paraToRemove = null;
            foreach (Block block in UiRtbOnlineLibrary.Document.Blocks)
            {
                if (foundPara)
                {
                    if (block is Paragraph para)
                    {
                        if (para.Name != null && para.Name.StartsWith(_para_books_))
                        {
                            paraToRemove = para;
                        }
                        break;
                    }
                }
                else
                {
                    if ((block as Paragraph) == shelfPara)
                    {
                        foundPara = true;
                    }
                }
            }

            UiRtbOnlineLibrary.Document.Blocks.Remove(paraToRemove);
        }


        //************************************************************************
        //
        // Event Handlers. 
        //
        //************************************************************************


        /// <summary>
        /// Handles user's click on a book title.
        /// Identifies the clicked book and closes the dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventBookTitle(object sender, MouseEventArgs e)
        {
            try
            {
                Run run = e.Source as Run;
                if (run != null)
                {
                    SelectedBook = _dictRunBooks[run];
                    DialogResult = true;
                }
            }
            catch { }
        }

        /// <summary>
        /// A Run was clicked. This method will determine
        /// the type of line clicked and perform appropriate action.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventTitleLineClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Determine which object was clicked, change its IsExpanded state and rebuild.
                if (sender is Run run)
                {
                    string id = GetObjectTypeAndId(run.Name, out LibObjectType typ);
                    switch (typ)
                    {
                        case LibObjectType.BOOKCASE:
                            Bookcase bookcase = GetBookcaseById(id);
                            if (bookcase != null)
                            {
                                bookcase.IsExpanded = !bookcase.IsExpanded;
                            }
                            break;
                        case LibObjectType.SHELF:
                            Shelf shelf = GetShelfByPathId(id);
                            if (shelf != null)
                            {
                                shelf.IsExpanded = !shelf.IsExpanded;
                            }
                            break;
                    }

                    BuildBookcases();
                }
            }
            catch { }
        }

        /// <summary>
        /// Find the Bookcase by its Id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private Bookcase GetBookcaseById(string id)
        {
            Bookcase bookcase = null;

            foreach (Bookcase bc in _content.Bookcases)
            {
                if (bc.Id == id)
                {
                    bookcase = bc;
                    break;
                }
            }

            return bookcase;
        }

        /// <summary>
        /// Find the Shelf by its PathId.
        /// </summary>
        /// <param name="pathId"></param>
        /// <returns></returns>
        private Shelf GetShelfByPathId(string pathId)
        {
            Shelf shelf = null;

            foreach (Bookcase bc in _content.Bookcases)
            {
                foreach (Shelf sh in bc.Shelves)
                {
                    if (sh.ShelfPathId == pathId)
                    {
                        shelf = sh;
                        break;
                    }
                }
            }

            return shelf;
        }


        //************************************************************************
        //
        // Utilities. 
        //
        //************************************************************************

        /// <summary>
        /// Parses the passed name of the paragraph to determine what
        /// type it represents and what id it has.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="typ"></param>
        /// <returns></returns>
        private string GetObjectTypeAndId(string name, out LibObjectType typ)
        {
            typ = LibObjectType.NONE;
            string pathId = "";

            if (!string.IsNullOrEmpty(name))
            {
                int pos = name.IndexOf(NAME_SEPAR);
                if (pos > 0)
                {
                    pathId = name.Substring(pos + 1);
                    typ = GetObjectTypeFromString(name.Substring(0, pos));
                }
            }

            return pathId;
        }

        /// <summary>
        /// Based on the passed string that is a name of the Run or Paragraph,
        /// determines the type of object.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private LibObjectType GetObjectTypeFromString(string s)
        {
            LibObjectType typ = LibObjectType.NONE;

            if (!string.IsNullOrEmpty(s))
            {
                if (s.Contains("Bookcase"))
                {
                    typ = LibObjectType.BOOKCASE;
                }
                else if (s.Contains("Shelf"))
                {
                    typ = LibObjectType.SHELF;
                }
                else if (s.Contains("Books"))
                {
                    typ = LibObjectType.BOOKS;
                }
                else if (s.Contains("Book"))
                {
                    typ = LibObjectType.BOOK;
                }
            }

            return typ;
        }

        //************************************************************************
        //
        // Dialog buttons click events. 
        //
        //************************************************************************

        /// <summary>
        /// User clicked close without selecting any action.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Open browser to the Help web page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Online-Libraries");
        }

        /// <summary>
        /// The user clicked the Libraries button so we exit with false
        /// and set the ShowLibraries falg.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnLibraries_Click(object sender, RoutedEventArgs e)
        {
            ShowLibraries = true;
            DialogResult = false;
        }
        
    }
}

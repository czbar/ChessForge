using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        // the listing of the content of the library
        private LibraryContent _content;

        // dictionary mapping Runs to Workbook that they represent
        private Dictionary<Run, WebAccess.Book> _dictRunBooks = new Dictionary<Run, WebAccess.Book>();

        /// <summary>
        /// A library workbook object selected by the user.
        /// </summary>
        public WebAccess.Book SelectedBook = null;

        // prefix for a bookcase paragraph
        private string _para_bookcase_ = "para_bookcase_";

        // prefix for a bookcase run
        private string _run_bookcase_ = "run_bookcase_";

        // prefix for a shelf paragraph
        private string _para_shelf_ = "para_shelf_";

        // prefix for a shelf run
        private string _run_shelf_ = "run_shelf_";

        // prefix for a books paragraph
        private string _para_books_ = "para_books_";

        /// <summary>
        /// Constructor. Builds the content of the Rich Text Box.
        /// </summary>
        /// <param name="content"></param>
        public OnlineLibraryContentDialog(LibraryContent content)
        {
            InitializeComponent();
            _content = content;

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
                    shelf.ShelfPathId = bookcase.Id + "_" + shelf.Id;

                    uint bookId = 1;
                    foreach (Book book in shelf.Books)
                    {
                        book.Id = bookId.ToString();
                        book.BookPathId = shelf.ShelfPathId + "_" + book.Id;
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
            string paraName = _para_bookcase_ + bookcase.Id;

            // do we already have this paragraph?
            Paragraph para = RichTextBoxUtilities.FindParagraphByName(UiRtbOnlineLibrary.Document, paraName, false);

            bool existed = (para != null);
            if (para == null)
            {
                para = new Paragraph
                {
                    Margin = new Thickness(20, 0, 0, 0),
                    Name = paraName
                };
            }

            if (!existed)
            {
                BuildBookcaseTitleAndDescription(para, bookcase);
                UiRtbOnlineLibrary.Document.Blocks.Add(para);
            }

            if (bookcase.IsExpanded)
            {
                if (!HasShelves(para))
                {
                    BuildShelves(para, bookcase);
                }
            }
            else
            {
                ClearShelves(para, bookcase);
            }
        }

        /// <summary>
        /// Build shelves paragraphs.
        /// </summary>
        /// <param name="bookcase"></param>
        /// <param name="bookcaseId"></param>
        private void BuildShelves(Paragraph bookcasePara, WebAccess.Bookcase bookcase)
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
            string paraName = _para_shelf_ + shelf.ShelfPathId;

            Paragraph para = RichTextBoxUtilities.FindParagraphByName(UiRtbOnlineLibrary.Document, paraName, false);

            bool existed = (para != null);
            if (para == null)
            {
                para = new Paragraph
                {
                    Margin = new Thickness(30, 0, 0, 0),
                    Name = paraName
                };
            };

            if (!existed)
            {
                BuildShelfTitleAndDescription(para, shelf);
                UiRtbOnlineLibrary.Document.Blocks.Add(para);
            }

            if (shelf.IsExpanded)
            {
                if (!HasBooks(para))
                {
                    BuildBooksParagraph(shelf);
                }
            }
            else
            {
                ClearBooks(para, shelf);
            }

            return para;
        }

        /// <summary>
        /// Build a paragraph with book titles.
        /// </summary>
        /// <param name="books"></param>
        /// <param name="bookcaseId"></param>
        /// <param name="shelfId"></param>
        private void BuildBooksParagraph(Shelf shelf)
        {
            Paragraph para = new Paragraph
            {
                Margin = new Thickness(40, 0, 0, 0),
            };

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
            runTitle.Text = "\n" + Properties.Resources.Bookcase + " " + bookcase.Id + ": " + bookcase.Title;
            runTitle.Name = _run_bookcase_ + bookcase.Id;
            runTitle.MouseDown += EventTitleLineClicked;
            para.Inlines.Add(runTitle);

            if (!string.IsNullOrEmpty(bookcase.Description))
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
            Run runTitle = new Run("\n" + Properties.Resources.Shelf + " " + screenId + ": " + shelf.Title);
            runTitle.FontWeight = FontWeights.Bold;
            runTitle.FontSize = 14 + Configuration.FontSizeDiff;
            runTitle.Name = _run_shelf_ + shelf.ShelfPathId;
            runTitle.MouseDown += EventTitleLineClicked;
            para.Inlines.Add(runTitle);

            if (!string.IsNullOrEmpty(shelf.Description))
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
                        else
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
                UiRtbOnlineLibrary.Document.Blocks.Remove(para);
            }
        }

        /// <summary>
        /// Finds the requested bookcase paragraph and removes the following Books paragraphs.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="shelf"></param>
        private void ClearBooks(Paragraph shelfPara, Shelf shelf)
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
                        else
                        {
                            break;
                        }
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
                // TODO: determin which object was clicked, change its IsExpanded state and rebuild.
                BuildBookcases();
            }
            catch { }
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
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Online-Library");
        }
    }
}

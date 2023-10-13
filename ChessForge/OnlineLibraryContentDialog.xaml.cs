using ChessPosition;
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
using WebAccess;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for OnlineLibraryContentDialog.xaml
    /// </summary>
    public partial class OnlineLibraryContentDialog : Window
    {
        // starting letter for bookcase numbering
        private char _bookcaseId = 'A';

        // the listing of the content of the library
        private LibraryContent _content;

        // dictionary mapping Runs to Workbook that they represent
        private Dictionary<Run, WebAccess.Book> _dictRunBooks = new Dictionary<Run, WebAccess.Book>();

        /// <summary>
        /// A library workbook object selected by the user.
        /// </summary>
        public WebAccess.Book SelectedBook = null;

        /// <summary>
        /// Constructor. Builds the content of the Rich Text Box.
        /// </summary>
        /// <param name="content"></param>
        public OnlineLibraryContentDialog(LibraryContent content)
        {
            InitializeComponent();
            _content = content;

            BuildTitleParagraph();
            BuildMessagesParagraph();

            BuildSections();
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

        /// <summary>
        /// Build section paragraphs.
        /// </summary>
        private void BuildSections()
        {
            foreach (WebAccess.Section section in _content.Sections)
            {
                BuildSectionParagraph(section);
            }
        }

        /// <summary>
        /// Build paragraphs for a single section.
        /// </summary>
        /// <param name="section"></param>
        private void BuildSectionParagraph(WebAccess.Section section)
        {
            Paragraph para = new Paragraph
            {
                Margin = new Thickness(20, 0, 0, 0),
            };

            Run runTitle = new Run();
            runTitle.FontWeight = FontWeights.Bold;
            runTitle.FontSize = 16 + Configuration.FontSizeDiff;
            runTitle.Text = "\n" + Properties.Resources.Bookcase + " " + _bookcaseId.ToString() + ": " + section.Title;
            para.Inlines.Add(runTitle);

            if (!string.IsNullOrEmpty(section.Description))
            {
                Run descript = new Run("\n");
                descript.FontWeight = FontWeights.Normal;
                descript.FontSize = 14 + Configuration.FontSizeDiff;
                descript.Text += section.Description;
                para.Inlines.Add(descript);
            }

            UiRtbOnlineLibrary.Document.Blocks.Add(para);

            BuildSubSections(section, _bookcaseId);
            _bookcaseId++;
        }

        /// <summary>
        /// Build subsections paragraphs.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="bookcaseId"></param>
        private void BuildSubSections(WebAccess.Section section, char bookcaseId)
        {
            uint shelfId = 1;

            foreach (WebAccess.SubSection subSection in section.SubSections)
            {
                BuildSubSectionParagraph(subSection, bookcaseId, shelfId);
                shelfId++;
            }
        }

        /// <summary>
        /// Build paragraphs for a single subsections.
        /// </summary>
        /// <param name="subSection"></param>
        /// <param name="bookcaseId"></param>
        /// <param name="shelfId"></param>
        private void BuildSubSectionParagraph(WebAccess.SubSection subSection, char bookcaseId, uint shelfId)
        {
            Paragraph para = new Paragraph
            {
                Margin = new Thickness(30, 0, 0, 0),
            };

            Run title = new Run("\n" + Properties.Resources.Shelf + " " + bookcaseId.ToString() + shelfId.ToString() + ": " + subSection.Title);
            title.FontWeight = FontWeights.Bold;
            title.FontSize = 14 + Configuration.FontSizeDiff;
            para.Inlines.Add(title);

            if (!string.IsNullOrEmpty(subSection.Description))
            {
                Run descript = new Run("\n" + subSection.Description);
                descript.FontWeight = FontWeights.Normal;
                descript.FontSize = 14 + Configuration.FontSizeDiff;
                para.Inlines.Add(descript);
            }

            UiRtbOnlineLibrary.Document.Blocks.Add(para);

            BuildWorkbooksParagraph(subSection.Books, bookcaseId, shelfId);
        }

        /// <summary>
        /// Build a paragraph with book titles.
        /// </summary>
        /// <param name="workbooks"></param>
        /// <param name="bookcaseId"></param>
        /// <param name="shelfId"></param>
        private void BuildWorkbooksParagraph(List<WebAccess.Book> workbooks, char bookcaseId, uint shelfId)
        {
            Paragraph para = new Paragraph
            {
                Margin = new Thickness(40, 0, 0, 0),
            };

            bool first = true;
            uint bookNo = 1;
            foreach (WebAccess.Book book in workbooks)
            {
                string prefix = "";
                if (first)
                {
                    first = false;
                }
                else
                {
                    prefix = "\n";
                }

                prefix += bookcaseId.ToString() + shelfId.ToString() + "." + bookNo.ToString() + ": ";
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

                bookNo++;
            }

            UiRtbOnlineLibrary.Document.Blocks.Add(para);
        }

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

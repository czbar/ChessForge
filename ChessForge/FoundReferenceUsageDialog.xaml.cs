using ChessPosition;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for FoundReferenceUsageDialog.xaml
    /// </summary>
    public partial class FoundReferenceUsageDialog : Window
    {
        public enum Action
        {
            None,
            OpenView,
        }

        /// <summary>
        /// Action to take after exit.
        /// </summary>
        public Action Request;

        /// <summary>
        /// Index of the article to be acted upon exit.
        /// </summary>
        public int ArticleIndexId = -1;

        // the list of articles to display
        private ObservableCollection<ArticleListItem> _articleList;

        // Prefix for the OpenView button
        private const string PREFIX_BUTTON_OPEN_VIEW = "btnopenview_";

        // Prefix for the name of the paragraph with moves.
        private const string PREFIX_ITEM_INDEX = "itemindex_";

        // Indent spaces before buttons.
        private const string BUTTON_INDENT = "       ";

        public FoundReferenceUsageDialog(ObservableCollection<ArticleListItem> articleList)
        {
            InitializeComponent();

            _articleList = articleList;
            BuildAllItemParagraphs();
        }

        /// <summary>
        /// Build paragraphs for all article items in the list.
        /// </summary>
        private void BuildAllItemParagraphs()
        {
            for (int i = 0; i < _articleList.Count; i++)
            {
                Paragraph para = CreateItemParagraph(_articleList[i], i);
                UiRtbReferenceUsage.Document.Blocks.Add(para);
            }
        }

        /// <summary>
        /// Builds a paragraph for the chapter's title.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private Paragraph BuildChapterTitleParagraph(ArticleListItem item)
        {
            Paragraph para = new Paragraph
            {
                Margin = new Thickness(10, 0, 0, 20),
            };

            if (item.Chapter != null)
            {
                Run rChapter = new Run();
                rChapter.Text = Properties.Resources.Chapter + " " + (item.ChapterIndex + 1).ToString() + ". " + item.Chapter.Title;
                rChapter.FontWeight = FontWeights.Bold;
                rChapter.FontSize = Constants.BASE_FIXED_FONT_SIZE + 2 + Configuration.FontSizeDiff;
                para.Inlines.Add(rChapter);
            }

            return para;
        }

        /// <summary>
        /// Creates a paragraph for an individual article item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="itemIndex"></param>
        /// <returns></returns>
        private Paragraph CreateItemParagraph(ArticleListItem item, int itemIndex)
        {
            if (item.Chapter != null)
            {
                return BuildChapterTitleParagraph(item);
            }

            Paragraph para = new Paragraph
            {
                Margin = new Thickness(30, 0, 0, 10),
            };

            para.Name = PREFIX_ITEM_INDEX + itemIndex.ToString();
            if (item.Article != null)
            {
                InsertArticleTitleRun(para, item);
            }

            InsertOpenViewButton(para, item, itemIndex);

            return para;
        }

        /// <summary>
        /// Builds a Run with the Article's title and inserts it in the passed paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="item"></param>
        private void InsertArticleTitleRun(Paragraph para, ArticleListItem item)
        {
            if (item.Article != null)
            {
                Run rArticle = new Run();
                rArticle.Text = item.ElementTitleForDisplay + "\n";
                rArticle.FontWeight = FontWeights.Bold;
                rArticle.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
                para.Inlines.Add(rArticle);
            }
        }

        /// <summary>
        /// Inserts a Run for the Open View button.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="item"></param>
        /// <param name="itemIndex"></param>
        private void InsertOpenViewButton(Paragraph para, ArticleListItem item, int itemIndex)
        {
            InsertIndent(para);
            Run rOpenView = new Run();
            rOpenView.Name = PREFIX_BUTTON_OPEN_VIEW + itemIndex.ToString();
            rOpenView.Text = Properties.Resources.OpenView;
            rOpenView.Cursor = Cursors.Hand;
            rOpenView.MouseDown += EventOpenViewButtonClicked;

            rOpenView.TextDecorations = TextDecorations.Underline;
            rOpenView.FontWeight = FontWeights.Normal;
            rOpenView.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
            rOpenView.Foreground = Brushes.Blue;
            para.Inlines.Add(rOpenView);
            para.Inlines.Add(new Run("\n"));
        }

        /// <summary>
        /// Inserts a Run with spaces to simulate indent.
        /// </summary>
        /// <param name="para"></param>
        private void InsertIndent(Paragraph para)
        {
            para.Inlines.Add(new Run(BUTTON_INDENT));
        }

        /// <summary>
        /// Handles the user's click on Open View button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventOpenViewButtonClicked(object sender, MouseEventArgs e)
        {
            if (e.Source is Run r)
            {
                ArticleIndexId = TextUtils.GetIdFromPrefixedString(r.Name);
            }

            Request = Action.OpenView;
            DialogResult = true;
        }
    }
}

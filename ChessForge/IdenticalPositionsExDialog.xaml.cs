using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for IdenticalPositionsExDialog.xaml
    /// </summary>
    public partial class IdenticalPositionsExDialog : Window
    {
        /// <summary>
        /// Set to the article to be acted upon exit.
        /// </summary>
        public ArticleListItem SelectedArticleListItem = null;

        /// <summary>
        /// The list of games to process.
        /// </summary>
        private ObservableCollection<ArticleListItem> _articleList;

        // Node for which this dialog was invoked.
        private TreeNode _node;

        /// <summary>
        /// Creates the dialog and builds the content of the rich text box.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="articleList"></param>
        public IdenticalPositionsExDialog(TreeNode nd, ref ObservableCollection<ArticleListItem> articleList)
        {
            _node = nd;
            _articleList = articleList;

            InitializeComponent();

            BuildAllItemParagraphs();
        }

        /// <summary>
        /// Build paragraphs for all articles/positions in the list.
        /// </summary>
        private void BuildAllItemParagraphs()
        {
            for (int i = 0; i < _articleList.Count; i++)
            {
                Paragraph para = CreateItemParagraph(_articleList[i], i);
                UiRtbIdenticalPositions.Document.Blocks.Add(para);
            }
        }

        /// <summary>
        /// Create a paragraph for the passed item.
        /// It will be a chapter title or the content
        /// relating to the position.
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
                Margin = new Thickness(10, 0, 0, 10),
            };

            if (item.Article != null)
            {
                InsertArticleTitleRun(para, item);
            }

            InsertStemRuns(para, item);
            InsertTailRuns(para, item);
            InsertCopyButton(para, item);

            return para;
        }

        /// <summary>
        /// Builds a paragraph with the Chapter title.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private Paragraph BuildChapterTitleParagraph(ArticleListItem item)
        {
            Paragraph para = new Paragraph
            {
                Margin = new Thickness(10, 0, 0, 10),
            };

            if (item.Chapter != null)
            {
                Run rChapter = new Run();
                rChapter.Text = Properties.Resources.Chapter + " " + (item.ChapterIndex + 1).ToString() + ". " + item.Chapter.Title;
                rChapter.FontWeight = FontWeights.Bold;
                rChapter.FontSize = 16 + Configuration.FontSizeDiff;
                para.Inlines.Add(rChapter);
            }

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
                rArticle.FontSize = 14 + Configuration.FontSizeDiff;
                para.Inlines.Add(rArticle);
            }
        }

        /// <summary>
        /// Builds Runs for stem moves and inserts it in the passed paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="item"></param>
        private void InsertStemRuns(Paragraph para, ArticleListItem item)
        {
            foreach (TreeNode nd in item.StemLine)
            {
                Run rStem = new Run();
                rStem.Text = MoveUtils.BuildSingleMoveText(nd, nd.Parent.NodeId == 0, true) + " ";
                rStem.FontWeight = FontWeights.Bold;
                rStem.FontSize = 13 + Configuration.FontSizeDiff;
                para.Inlines.Add(rStem);
            }
        }

        /// <summary>
        /// Builds Runs for tail moves and inserts it in the passed paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="item"></param>
        private void InsertTailRuns(Paragraph para, ArticleListItem item)
        {
            int plyCount = 0;

            foreach (TreeNode nd in item.TailLine)
            {
                Run rTail = new Run();
                rTail.Text = MoveUtils.BuildSingleMoveText(nd, nd.Parent.NodeId == 0 || plyCount == 0, true) + " ";
                rTail.FontStyle = FontStyles.Italic;  
                rTail.FontWeight = FontWeights.Normal;
                rTail.FontSize = 12 + Configuration.FontSizeDiff;
                para.Inlines.Add(rTail);
                plyCount++;
            }
            para.Inlines.Add(new Run("\n"));
        }

        /// <summary>
        /// Builds a Run for the Copy button.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="item"></param>
        private void InsertCopyButton(Paragraph para, ArticleListItem item)
        {
            Run rCopyButton = new Run();
            rCopyButton.Text = "    [Copy Variation Tree]";
            rCopyButton.FontWeight = FontWeights.Bold;
            rCopyButton.FontSize = 14 + Configuration.FontSizeDiff;
            rCopyButton.Foreground = Brushes.Blue;
            para.Inlines.Add(rCopyButton);
            para.Inlines.Add(new Run("\n"));
        }
    }
}

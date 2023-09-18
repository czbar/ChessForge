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

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for IdenticalPositionsExDialog.xaml
    /// </summary>
    public partial class IdenticalPositionsExDialog : Window
    {
        public enum Action
        {
            None,
            CopyLine,
            CopyTree,
            OpenView
        }

        /// <summary>
        /// Action to take after exit.
        /// </summary>
        public Action Request;

        /// <summary>
        /// Index of the article to be acted upon exit.
        /// </summary>
        public int ArticleIndexId = -1;

        /// <summary>
        /// The list of games to process.
        /// </summary>
        private ObservableCollection<ArticleListItem> _articleList;

        // Node for which this dialog was invoked.
        private TreeNode _node;

        // Name of the summary para.
        private const string PARA_SUMMARY = "summary";

        // Prefix for the name of the paragraph with moves.
        private const string PREFIX_ITEM_INDEX = "itemindex_";

        // Prefix for the stem line moves.
        private const string PREFIX_STEM_MOVE = "stemmove_";

        // Prefix for the tail line moves.
        private const string PREFIX_TAIL_MOVE = "tailmove_";

        // Prefix for the CopyLine button
        private const string PREFIX_BUTTON_COPY_LINE = "btncopyline_";

        // Prefix for the CopyTree button
        private const string PREFIX_BUTTON_COPY_TREE = "btncopytree_";

        // Prefix for the OpenView button
        private const string PREFIX_BUTTON_OPEN_VIEW = "btnopenview_";


        // Indent spaces before buttons.
        private const string BUTTON_INDENT = "       ";


        /// <summary>
        /// Chessboard shown over moves in the Identical Position dialog.
        /// </summary>
        private ChessBoardSmall IdenticalPositionFloatingBoard;

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
            IdenticalPositionFloatingBoard = new ChessBoardSmall(_cnvFloatingBoard, UiImgFloatingBoard, null, null, true, false);

            BuildSummaryParagraph();
            BuildAllItemParagraphs();
        }

        /// <summary>
        /// Builds the top paragraph with the summary number.
        /// </summary>
        private void BuildSummaryParagraph()
        {
            int studyCount = 0;
            int gameCount = 0;
            int exerciseCount = 0;

            foreach (ArticleListItem item in _articleList)
            {
                switch (item.ContentType)
                {
                    case GameData.ContentType.STUDY_TREE:
                        studyCount++;
                        break;
                    case GameData.ContentType.MODEL_GAME:
                        gameCount++;
                        break;
                    case GameData.ContentType.EXERCISE:
                        exerciseCount++;
                        break;
                }
            }

            int count = studyCount + gameCount + exerciseCount;

            Paragraph para = new Paragraph
            {
                Margin = new Thickness(10, 0, 0, 20),
            };

            para.Name = PARA_SUMMARY;

            Run runTotal = new Run();
            runTotal.Text = Properties.Resources.NumberOfOccurrences + ": " + count.ToString();
            runTotal.FontWeight = FontWeights.Bold;
            runTotal.FontSize = 16 + Configuration.FontSizeDiff;
            para.Inlines.Add(runTotal);

            CreateItemCountRun(para, Properties.Resources.Studies, studyCount);
            CreateItemCountRun(para, Properties.Resources.Games, gameCount);
            CreateItemCountRun(para, Properties.Resources.Exercises, exerciseCount);

            if (gameCount + exerciseCount > 0)
            {
                CreateCopyMoveItemsLink(para);
            }

            UiRtbIdenticalPositions.Document.Blocks.Add(para);
        }

        /// <summary>
        /// Creates a Run reporting the count of items.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="label"></param>
        /// <param name="count"></param>
        private void CreateItemCountRun(Paragraph para, string label, int count)
        {
            if (count > 0)
            {
                Run run = new Run();
                run.Text = "\n     " + label + ": " + count.ToString();
                run.FontWeight = FontWeights.Bold;
                run.FontSize = 14 + Configuration.FontSizeDiff;
                para.Inlines.Add(run);
            }
        }

        /// <summary>
        /// Creates a Run that will invoke selection of the items when clicked on.
        /// </summary>
        /// <param name="para"></param>
        private void CreateCopyMoveItemsLink(Paragraph para)
        {
            Run rCopyLine = new Run();
            rCopyLine.Text = "\n\n" + Properties.Resources.SelectGamesStudiesToCopyMove;
            rCopyLine.Cursor = Cursors.Hand;
            //rCopyLine.MouseDown += EventCopyLineButtonClicked;

            rCopyLine.TextDecorations = TextDecorations.Underline;
            rCopyLine.FontWeight = FontWeights.Normal;
            rCopyLine.FontSize = 14 + Configuration.FontSizeDiff;
            rCopyLine.Foreground = Brushes.Blue;
            para.Inlines.Add(rCopyLine);
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
                Margin = new Thickness(30, 0, 0, 10),
            };

            para.Name = PREFIX_ITEM_INDEX + itemIndex.ToString();
            if (item.Article != null)
            {
                InsertArticleTitleRun(para, item);
            }

            InsertStemRuns(para, item);
            InsertTailRuns(para, item);
            InsertCopyMainLineButton(para, item, itemIndex);
            InsertCopySubtreeButton(para, item, itemIndex);
            InsertOpenViewButton(para, item, itemIndex);

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
                Margin = new Thickness(10, 0, 0, 20),
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
                rStem.Name = PREFIX_STEM_MOVE + nd.NodeId.ToString();

                uint moveNumberOffset = 0;
                if (item.Article != null && item.Article.Tree != null)
                {
                    moveNumberOffset = item.Article.Tree.MoveNumberOffset;
                }
                rStem.Text = MoveUtils.BuildSingleMoveText(nd, nd.Parent.NodeId == 0, true, moveNumberOffset) + " ";
                rStem.FontWeight = FontWeights.Bold;
                rStem.FontSize = 13 + Configuration.FontSizeDiff;
                rStem.MouseMove += EventRunMoveOver;
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

            if (item.TailLine.Count > 0)
            {
                para.Inlines.Add(new Run(" (" + Properties.Resources.MainLine + ": "));
            }

            foreach (TreeNode nd in item.TailLine)
            {
                Run rTail = new Run();
                rTail.Name = PREFIX_TAIL_MOVE + nd.NodeId.ToString();
                uint moveNumberOffset = 0;
                if (item.Article != null && item.Article.Tree != null)
                {
                    moveNumberOffset = item.Article.Tree.MoveNumberOffset;
                }
                rTail.Text = MoveUtils.BuildSingleMoveText(nd, nd.Parent.NodeId == 0 || plyCount == 0, true, moveNumberOffset) + " ";
                rTail.FontStyle = FontStyles.Italic;
                rTail.FontWeight = FontWeights.Normal;
                rTail.FontSize = 12 + Configuration.FontSizeDiff;
                rTail.MouseMove += EventRunMoveOver;
                para.Inlines.Add(rTail);
                plyCount++;
            }

            if (item.TailLine.Count > 0)
            {
                para.Inlines.Add(new Run(")"));
            }

            para.Inlines.Add(new Run("\n"));
        }

        /// <summary>
        /// Inserts a Run for the Copy button.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="item"></param>
        private void InsertCopyMainLineButton(Paragraph para, ArticleListItem item, int itemIndex)
        {
            TreeNode nd = item.StemLine.LastOrDefault();

            if (nd != null && item.TailLine.Count > 0)
            {
                InsertIndent(para);
                uint moveNumberOffset = 0;
                if (item.Article != null && item.Article.Tree != null)
                {
                    moveNumberOffset = item.Article.Tree.MoveNumberOffset;
                }
                string moveText = MoveUtils.BuildSingleMoveText(nd, true, false, moveNumberOffset);
                Run rCopyLine = new Run();
                rCopyLine.Name = PREFIX_BUTTON_COPY_LINE + itemIndex.ToString();
                rCopyLine.Text = Properties.Resources.CopyMainLineAfterMove + " " + moveText;
                rCopyLine.Cursor = Cursors.Hand;
                rCopyLine.MouseDown += EventCopyLineButtonClicked;

                rCopyLine.TextDecorations = TextDecorations.Underline;
                rCopyLine.FontWeight = FontWeights.Normal;
                rCopyLine.FontSize = 12 + Configuration.FontSizeDiff;
                rCopyLine.Foreground = Brushes.Blue;
                para.Inlines.Add(rCopyLine);
                para.Inlines.Add(new Run("\n"));
            }
        }

        /// <summary>
        /// Inserts a Run for the Copy button.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="item"></param>
        /// <param name="itemIndex"></param>
        private void InsertCopySubtreeButton(Paragraph para, ArticleListItem item, int itemIndex)
        {
            TreeNode nd = item.StemLine.LastOrDefault();

            if (nd != null && item.TailLine.Count > 0)
            {
                InsertIndent(para);
                uint moveNumberOffset = 0;
                if (item.Article != null && item.Article.Tree != null)
                {
                    moveNumberOffset = item.Article.Tree.MoveNumberOffset;
                }
                string moveText = MoveUtils.BuildSingleMoveText(nd, true, false, moveNumberOffset);
                Run rCopyTree = new Run();
                rCopyTree.Name = PREFIX_BUTTON_COPY_TREE + itemIndex.ToString();
                rCopyTree.Text = Properties.Resources.CopySubtreeAfterMove + " " + moveText;
                rCopyTree.Cursor = Cursors.Hand;
                rCopyTree.MouseDown += EventCopyTreeButtonClicked;

                rCopyTree.TextDecorations = TextDecorations.Underline;
                rCopyTree.FontWeight = FontWeights.Normal;
                rCopyTree.FontSize = 12 + Configuration.FontSizeDiff;
                rCopyTree.Foreground = Brushes.Blue;
                para.Inlines.Add(rCopyTree);
                para.Inlines.Add(new Run("\n"));
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
            rOpenView.FontSize = 12 + Configuration.FontSizeDiff;
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
        /// Shows the floating board in reposnse to mouse
        /// hovering over a position node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventRunMoveOver(object sender, MouseEventArgs e)
        {
            if (e.Source is Run)
            {
                try
                {
                    Run r = (Run)e.Source;
                    if (r.Parent is Paragraph)
                    {
                        Paragraph para = (Paragraph)r.Parent;
                        if (r.Parent != null)
                        {
                            int itemIndex = TextUtils.GetIdFromPrefixedString(para.Name);
                            int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
                            bool isStem = r.Name.StartsWith(PREFIX_STEM_MOVE);
                            TreeNode nd = GetNodeFromItemIndexAndId(itemIndex, nodeId, isStem);
                            if (nd != null)
                            {
                                Point pt = e.GetPosition(UiRtbIdenticalPositions);
                                PositionFloatingBoard(pt);
                                ShowFloatingBoard(nd, pt);
                            }
                        }
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Handles the user click on Copy Line button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventCopyLineButtonClicked(object sender, MouseEventArgs e)
        {
            if (e.Source is Run r)
            {
                ArticleIndexId = TextUtils.GetIdFromPrefixedString(r.Name);
            }

            Request = Action.CopyLine;
            DialogResult = true;
        }

        /// <summary>
        /// Handles the user click on Copy Tree button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventCopyTreeButtonClicked(object sender, MouseEventArgs e)
        {
            if (e.Source is Run r)
            {
                ArticleIndexId = TextUtils.GetIdFromPrefixedString(r.Name);
            }

            Request = Action.CopyTree;
            DialogResult = true;
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

        /// <summary>
        /// Adjusts the positioning of the floating board
        /// if it might go outside the dialog boundaries.
        /// </summary>
        /// <param name="pt"></param>
        private void PositionFloatingBoard(Point pt)
        {
            double xCoord = pt.X + 10;
            double yCoord = pt.Y + 20;

            // are we too far to the right?
            if (UiRtbIdenticalPositions.ActualWidth < xCoord + 170)
            {
                // show at the other side
                xCoord = pt.X - 170;
            }

            // are we too high?
            if (UiRtbIdenticalPositions.ActualHeight < yCoord + 170)
            {
                // show at the other side
                yCoord = pt.Y - 170;
            }

            UiVbFloatingBoard.Margin = new Thickness(xCoord, yCoord, 0, 0);
        }

        /// <summary>
        /// Shows floating board for the passed position
        /// at the passed point.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="pt"></param>
        private void ShowFloatingBoard(TreeNode nd, Point pt)
        {
            IdenticalPositionFloatingBoard.DisplayPosition(nd, false);
            UiImgFloatingBoard.Visibility = Visibility.Visible;
            UiVbFloatingBoard.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Returns a node identified by item index and node id.
        /// </summary>
        /// <param name="itemIndex"></param>
        /// <param name="nodeId"></param>
        /// <param name="isStem"></param>
        /// <returns></returns>
        private TreeNode GetNodeFromItemIndexAndId(int itemIndex, int nodeId, bool isStem)
        {
            TreeNode nd = null;

            try
            {
                List<TreeNode> lst = isStem ? _articleList[itemIndex].StemLine : _articleList[itemIndex].TailLine;
                nd = lst.Find(x => x.NodeId == nodeId);
            }
            catch { }

            return nd;
        }

        /// <summary>
        /// Hides the floating board on mouse move.
        /// (If this was over a position, EventRunMoveOver will show the board regardless.) 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRtbIdenticalPositions_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            UiImgFloatingBoard.Visibility = Visibility.Collapsed;
            UiVbFloatingBoard.Visibility = Visibility.Collapsed;
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
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Identical-Positions");
        }
    }

}

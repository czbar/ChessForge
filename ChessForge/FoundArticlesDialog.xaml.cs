using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for FoundArticlesDialog.xaml
    /// </summary>
    public partial class FoundArticlesDialog : Window
    {
        /// <summary>
        /// The mode in which to open the dialog.
        /// </summary>
        public enum Mode
        {
            IDENTICAL_ARTICLES,
            FILTER_GAMES
        }

        public enum Action
        {
            None,
            CopyLine,
            CopyTree,
            OpenView,
            CopyArticles,
            MoveArticles,
            CopyOrMoveArticles,
            SearchAgain
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

        // Prefix for the main line line moves.
        private const string PREFIX_MAIN_LINE_MOVE = "mainlinemove_";

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
        /// Mode in which this dialog is open.
        /// </summary>
        private Mode _mode;

        // determines whethere to show button to request editing of the search 
        private bool _editableSearch;

        /// <summary>
        /// Creates the dialog and builds the content of the rich text box.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="articleList"></param>
        public FoundArticlesDialog(TreeNode nd, Mode mode, ref ObservableCollection<ArticleListItem> articleList, bool editableSearch)
        {
            _node = nd;
            _mode = mode;
            _articleList = articleList;
            _editableSearch = editableSearch;

            InitializeComponent();

            UiBtnCopyMove.Content = "   " + Properties.Resources.SelectCopyMove + "    ";
            UiBtnSearchAgain.Content = "   " + Properties.Resources.SearchAgain + "    ";
            IdenticalPositionFloatingBoard = new ChessBoardSmall(_cnvFloatingBoard, UiImgFloatingBoard, null, null, true, false);

            UiBtnSearchAgain.Visibility = _editableSearch ? Visibility.Visible : Visibility.Collapsed;

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
            int chapterCount = 0;

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
                    default:
                        if (item.Chapter != null)
                        {
                            chapterCount++;
                        }
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
            runTotal.FontSize = Constants.BASE_FIXED_FONT_SIZE + 2 + Configuration.FontSizeDiff;
            para.Inlines.Add(runTotal);

            CreateItemCountRun(para, Properties.Resources.Chapters, chapterCount);
            CreateItemCountRun(para, Properties.Resources.Studies, studyCount);
            CreateItemCountRun(para, Properties.Resources.Games, gameCount);
            CreateItemCountRun(para, Properties.Resources.Exercises, exerciseCount);

            UiRtbIdenticalPositions.Document.Blocks.Add(para);

            if (gameCount == 0 && exerciseCount == 0)
            {
                UiBtnCopyMove.Visibility = Visibility.Collapsed;
            }
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
                run.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
                para.Inlines.Add(run);
            }
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

            if (_mode == Mode.IDENTICAL_ARTICLES)
            {
                InsertStemRuns(para, item);
                InsertTailRuns(para, item);
            }
            else if (_mode == Mode.FILTER_GAMES)
            {
                InsertMainLineRuns(para, item);
            }

            if (item.Node == _node && _mode == Mode.IDENTICAL_ARTICLES)
            {
                InsertSameArticleRun(para, item);
            }
            else
            {
                if (_mode == Mode.IDENTICAL_ARTICLES)
                {
                    InsertCopyMainLineButton(para, item, itemIndex);
                    InsertCopySubtreeButton(para, item, itemIndex);
                }
                InsertOpenViewButton(para, item, itemIndex);
            }

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
                rChapter.FontSize = Constants.BASE_FIXED_FONT_SIZE + 2 + Configuration.FontSizeDiff;
                para.Inlines.Add(rChapter);
            }

            return para;
        }

        /// <summary>
        /// Inserts a run indicating that the item is the one currently viewed.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="item"></param>
        private void InsertSameArticleRun(Paragraph para, ArticleListItem item)
        {
            Run rThisPosition = new Run();
            rThisPosition.Text = "    (" + Properties.Resources.CurrentlyViewed + ")\n";
            rThisPosition.FontWeight = FontWeights.Bold;
            rThisPosition.Foreground = Brushes.Green;
            rThisPosition.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
            para.Inlines.Add(rThisPosition);
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
                rStem.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
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

            string lineName = Properties.Resources.Line;
            switch (item.ContentType)
            {
                case GameData.ContentType.MODEL_GAME:
                    lineName = item.IsTailLineMain ? Properties.Resources.GameText : Properties.Resources.SideLine;
                    break;
                case GameData.ContentType.EXERCISE:
                    lineName = item.IsTailLineMain ? Properties.Resources.ExerciseText : Properties.Resources.SideLine;
                    break;
            }

            if (item.TailLine.Count > 0)
            {
                string trailPrefix = " (" + lineName + ": ";
                Run r = new Run(trailPrefix);
                r.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
                para.Inlines.Add(r);
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
                rTail.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
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
        /// Builds Runs for main line moves and inserts it in the passed paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="item"></param>
        private void InsertMainLineRuns(Paragraph para, ArticleListItem item)
        {
            int plyCount = 0;

            foreach (TreeNode nd in item.MainLine)
            {
                Run rMain = new Run();
                rMain.Name = PREFIX_MAIN_LINE_MOVE + nd.NodeId.ToString();
                uint moveNumberOffset = 0;
                if (item.Article != null && item.Article.Tree != null)
                {
                    moveNumberOffset = item.Article.Tree.MoveNumberOffset;
                }
                rMain.Text = MoveUtils.BuildSingleMoveText(nd, nd.Parent.NodeId == 0 || plyCount == 0, true, moveNumberOffset) + " ";
                rMain.FontStyle = FontStyles.Normal;
                rMain.FontWeight = FontWeights.Normal;
                rMain.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
                rMain.MouseMove += EventRunMoveOver;
                para.Inlines.Add(rMain);
                plyCount++;
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
                rCopyLine.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
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
                rCopyTree.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
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
                            bool isMain = r.Name.StartsWith(PREFIX_MAIN_LINE_MOVE);
                            TreeNode nd = GetNodeFromItemIndexAndId(itemIndex, nodeId, isStem, isMain);
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
        private TreeNode GetNodeFromItemIndexAndId(int itemIndex, int nodeId, bool isStem, bool isMain)
        {
            TreeNode nd = null;

            try
            {
                List<TreeNode> lst;
                if (isMain)
                {
                    lst = _articleList[itemIndex].MainLine;
                }
                else
                {
                    lst = isStem ? _articleList[itemIndex].StemLine : _articleList[itemIndex].TailLine;
                }
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
        /// The user clicked the Select Articles to Copy or Move button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCopyMove_Click(object sender, RoutedEventArgs e)
        {
            Request = Action.CopyOrMoveArticles;
            DialogResult = true;
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
        /// The user pressed the Search Again button
        /// so exit the dialod with false and set action to SearchAgain
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnSearchAgain_Click(object sender, RoutedEventArgs e)
        {
            Request = Action.SearchAgain;
            DialogResult = false;
        }


        /// <summary>
        /// Handle key down so we can scroll with the keyboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
            {
                try
                {
                    switch (e.Key)
                    {
                        case Key.Home:
                            UiRtbIdenticalPositions.ScrollToHome();
                            e.Handled = true;
                            break;
                        case Key.End:
                            UiRtbIdenticalPositions.ScrollToEnd();
                            e.Handled = true;
                            break;
                    }
                }
                catch
                {
                }
            }
            else if (e.Key == Key.PageUp)
            {
                UiRtbIdenticalPositions.PageUp();
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                UiRtbIdenticalPositions.PageDown();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Open browser to the Help web page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            if (_mode == Mode.FILTER_GAMES)
            {
                System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Finding-Games");
            }
            else if (_mode == Mode.IDENTICAL_ARTICLES)
            {
                System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Finding-Positions");
            }
            else
            {
                System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/User's-Manual");            
            }
        }
    }

}

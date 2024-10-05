using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for ArticleReferencesDialog.xaml
    /// </summary>
    public partial class ArticleReferencesDialog : Window
    {
        /// <summary>
        /// Set to the article to be acted upon exit.
        /// </summary>
        public Article SelectedArticle;

        // last clicked article
        private Article _lastClickedArticle;

        /// <summary>
        /// The list of games to process.
        /// </summary>
        private ObservableCollection<ArticleListItem> _articles = new ObservableCollection<ArticleListItem>();

        // Node for which this dialog was invoked.
        private TreeNode _node;

        /// <summary>
        /// The dialog for selecting Articles (games or exercises) from multiple chapters.
        /// </summary>
        /// <param name="nd"></param>
        public ArticleReferencesDialog(TreeNode nd)
        {
            _node = nd;

            // if there is any selection outside the active chapter show all chapters (issue #465)
            InitializeComponent();
            BuildArticleList();

            UiLvGames.ItemsSource = _articles;
        }

        /// <summary>
        /// Builds a list of Articles from the references string of the Node
        /// </summary>
        private void BuildArticleList()
        {
            if (!string.IsNullOrEmpty(_node.References))
            {
                string[] refs = _node.References.Split('|');
                foreach (string guid in refs)
                {
                    Article art = WorkbookManager.SessionWorkbook.GetArticleByGuid(guid, out _, out _);
                    if (art != null)
                    {
                        ArticleListItem item = new ArticleListItem(null, -1, art, 0);
                        _articles.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// OK button was clicked. Exits with the result = true
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// Cancel button was clicked. Exits with the result = false
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Identifies a List View item from the click coordinates. 
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private ListViewItem GetListViewItemFromPoint(ListView listView, Point point)
        {
            HitTestResult result = VisualTreeHelper.HitTest(listView, point);
            if (result == null)
            {
                return null;
            }

            DependencyObject hitObject = result.VisualHit;
            while (hitObject != null && !(hitObject is ListViewItem))
            {
                hitObject = VisualTreeHelper.GetParent(hitObject);
            }

            return hitObject as ListViewItem;
        }

        /// <summary>
        /// Handles a double-click event on an Article.
        /// Opens the Game Preview dialog for the clicked game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLvGames_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = GetListViewItemFromPoint(UiLvGames, e.GetPosition(UiLvGames));
            if (item != null && item.Content is ArticleListItem)
            {
                Article art = (item.Content as ArticleListItem).Article;
                _lastClickedArticle = art;
                InvokeGamePreviewDialog(art);
            }
        }

        private void InvokeGamePreviewDialog(Article art)
        {
            List<string> gameIdList = new List<string>();
            List<Article> games = new List<Article> { art };
            gameIdList.Add(art.Tree.Header.GetGuid(out _));

            SingleGamePreviewDialog dlg = new SingleGamePreviewDialog(gameIdList, games);
            //{
            //    Left = this.Left + 20,
            //    Top = this.Top + 20,
            //    Topmost = false,
            //    Owner = this
            //};
            GuiUtilities.PositionDialog(dlg, this, 20);
            dlg.ShowDialog();
        }

        /// <summary>
        /// Handles a right-click even on an Article.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLvGames_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = GetListViewItemFromPoint(UiLvGames, e.GetPosition(UiLvGames));
            if (item != null && item.Content is ArticleListItem)
            {
                Article art = (item.Content as ArticleListItem).Article;
                _lastClickedArticle = art;
                if (art != null)
                {
                    if (art.Tree.Header.GetContentType(out _) == GameData.ContentType.EXERCISE)
                    {
                        UiMnPreviewGame.Header = Properties.Resources.PreviewExercise;
                        UiMnOpenGame.Header = Properties.Resources.GoToExercises;
                    }
                    else
                    {
                        UiMnPreviewGame.Header = Properties.Resources.PreviewGame;
                        UiMnOpenGame.Header = Properties.Resources.GoToGames;
                    }
                    UiCmGame.IsOpen = true;
                }
            }
        }

        private void UiLvGames_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = true;
        }

        private void UiMnPreviewGame_Click(object sender, RoutedEventArgs e)
        {
            if (_lastClickedArticle != null)
            {
                InvokeGamePreviewDialog(_lastClickedArticle);
            }
        }

        private void UiMnOpenGame_Click(object sender, RoutedEventArgs e)
        {
            SelectedArticle = _lastClickedArticle;
            UiBtnOk_Click(null, null);
        }
    }
}
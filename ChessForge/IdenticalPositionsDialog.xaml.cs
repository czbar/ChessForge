using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Linq;
using System.Security.Policy;
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
    /// Interaction logic for SelectArticlesDialog.xaml
    /// </summary>
    public partial class IdenticalPositionsDialog : Window
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
        /// The dialog for selecting Articles (games or exercises) from multiple chapters.
        /// </summary>
        /// <param name="articleList"></param>
        public IdenticalPositionsDialog(TreeNode nd, ref ObservableCollection<ArticleListItem> articleList)
        {
            _node = nd;
            _articleList = articleList;

            // if there is any selection outside the active chapter show all chapters (issue #465)
            InitializeComponent();

            UiLvGames.ItemsSource = _articleList;
        }

        /// <summary>
        /// The Exit button was clicked or this method
        /// wasa called after double click.
        /// That means that we want no action and leave SelectedArticleListItem as null.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
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

                SelectedArticleListItem = item.Content as ArticleListItem;
            }

            UiBtnOk_Click(null, null);
        }

        /// <summary>
        /// Identifies the Article from the point coordinates.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private Article GetArticleFromPoint(Point pt)
        {
            ListViewItem item = GetListViewItemFromPoint(UiLvGames, pt);
            Article art = null;
            if (item != null && item.Content is ArticleListItem)
            {
                art = (item.Content as ArticleListItem).Article;
            }

            return art;
        }
    }
}
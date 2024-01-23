using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SelectDuplicates.xaml
    /// </summary>
    public partial class SelectDuplicatesDialog : Window
    {
        // Collection bound to the ListView control
        private ObservableCollection<ArticleListItem> _articleListItemsSource = new ObservableCollection<ArticleListItem>();

        /// <summary>
        /// Creates the dialog, builds the list of items to display
        /// in the list view, and binds them.
        /// </summary>
        /// <param name="article"></param>
        public SelectDuplicatesDialog(Article article)
        {
            InitializeComponent();

            List<Article> dupes = FindDuplicates.GetArticleDuplicates(article);
            BuildArticleItemList(dupes);

            Title = Properties.Resources.DuplicatesOf + ": " + BuildNamesString(article);

            UiLvArticles.ItemsSource = _articleListItemsSource;
        }

        /// <summary>
        /// Builds the collection to bind to the ListView
        /// </summary>
        /// <param name="dupes"></param>
        /// <returns></returns>
        public void BuildArticleItemList(List<Article> dupes)
        {
            _articleListItemsSource = new ObservableCollection<ArticleListItem>();

            foreach (Article article in dupes)
            {
                Article art = AppState.Workbook.GetArticleByGuid(article.Guid, out int chapterIndex, out int articleIndex);
                ArticleListItem item = new ArticleListItem(AppState.Workbook.Chapters[chapterIndex], chapterIndex, art, articleIndex);
                _articleListItemsSource.Add(item);
            }
        }

        /// <summary>
        /// Builds a string combining the White and Black players' names 
        /// to show in the dialog's title bar
        /// </summary>
        /// <param name="article"></param>
        /// <returns></returns>
        private string BuildNamesString(Article article)
        {
            return (article.Tree.Header.GetWhitePlayer(out _) ?? "NN") + " - " + (article.Tree.Header.GetBlackPlayer(out _) ?? "NN");
        }
    }
}

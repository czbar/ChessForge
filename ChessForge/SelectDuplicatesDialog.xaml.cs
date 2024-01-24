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
    /// Identifies all duplicates of the passed article
    /// and allows the user to determine which ones to keep and which ones to delete.
    /// </summary>
    public partial class SelectDuplicatesDialog : Window
    {
        // Collection bound to the ListView control
        public ObservableCollection<ArticleListItem> DuplicateArticles = new ObservableCollection<ArticleListItem>();

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

            UiLvArticles.ItemsSource = DuplicateArticles;
        }

        /// <summary>
        /// Builds the collection to bind to the ListView
        /// </summary>
        /// <param name="dupes"></param>
        /// <returns></returns>
        public void BuildArticleItemList(List<Article> dupes)
        {
            DuplicateArticles = new ObservableCollection<ArticleListItem>();

            bool isFirst = true;
            foreach (Article article in dupes)
            {
                Article art = AppState.Workbook.GetArticleByGuid(article.Guid, out int chapterIndex, out int articleIndex);
                ArticleListItem item = new ArticleListItem(AppState.Workbook.Chapters[chapterIndex], chapterIndex, art, articleIndex);
                item.IsSelected = !isFirst;
                DuplicateArticles.Add(item);
                isFirst = false;
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

        /// <summary>
        /// Close the dialog on OK
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            foreach (ArticleListItem item in DuplicateArticles)
            {
                item.Article.Data = item.IsSelected;
            }

            DialogResult = true;
        }

        /// <summary>
        /// Close the dialog on cancel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Select-Duplicates-Dialog");
        }
    }
}

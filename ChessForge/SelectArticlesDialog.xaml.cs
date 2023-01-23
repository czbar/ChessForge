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
    /// Interaction logic for SelectArticlesDialog.xaml
    /// </summary>
    public partial class SelectArticlesDialog : Window
    {
        /// <summary>
        /// Whether to show articles fromthe current chapter only
        /// </summary>
        private bool _showActiveChapterOnly = true;

        /// <summary>
        /// The list of games to process.
        /// </summary>
        private ObservableCollection<ArticleListItem> _articleList;

        /// <summary>
        /// The dialog for selecting Articles (games or exercises) from multiple chapters.
        /// </summary>
        /// <param name="articleList"></param>
        public SelectArticlesDialog(ref ObservableCollection<ArticleListItem> articleList)
        {
            _articleList = articleList;
            InitializeComponent();
            _showActiveChapterOnly = true;
            SetItemVisibility();
            UiLvGames.ItemsSource = _articleList;
        }

        /// <summary>
        /// Sets the IsShown property on all items.
        /// </summary>
        private void SetItemVisibility()
        {
            foreach (ArticleListItem item in _articleList)
            {
                if (!_showActiveChapterOnly)
                {
                    item.IsShown = true;
                }
                else
                {
                    item.IsShown = item.Chapter == WorkbookManager.SessionWorkbook.ActiveChapter;
                }
            }
        }

        /// <summary>
        /// SelectAll box was checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _articleList)
            {
                item.IsSelected = true;
            }
        }

        /// <summary>
        /// SelectAll box was unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _articleList)
            {
                item.IsSelected = false;
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
        /// The user wants to show articles from all chapters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbAllChapters_Checked(object sender, RoutedEventArgs e)
        {
            _showActiveChapterOnly = false;
            SetItemVisibility();
        }

        /// <summary>
        /// The user wants to show articles from the active chapter only
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbAllChapters_Unchecked(object sender, RoutedEventArgs e)
        {
            _showActiveChapterOnly = true;
            SetItemVisibility();
        }
    }
}

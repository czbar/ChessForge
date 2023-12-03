using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SelectArticlesDialog.xaml
    /// </summary>
    public partial class SelectArticlesDialog : Window
    {
        /// <summary>
        /// Set to the article to be acted upon exit.
        /// </summary>
        public Article SelectedArticle;

        /// <summary>
        /// Action to perform on the selected articles
        /// </summary>
        public ArticlesAction ActionOnArticles = ArticlesAction.NONE;

        // last clicked article
        private Article _lastClickedArticle;

        /// <summary>
        /// Whether to show articles fromthe current chapter only
        /// </summary>
        private bool _showActiveChapterOnly = true;

        /// <summary>
        /// The list of games to process.
        /// </summary>
        private ObservableCollection<ArticleListItem> _articleList;

        /// <summary>
        /// The temporary list to bind with the ListView in the GUI.
        /// </summary>
        private ObservableCollection<ArticleListItem> _articleListItemsSource;

        // type of articles handled
        private GameData.ContentType _articleType;

        // Node for which this dialog was invoked.
        private TreeNode _node;

        /// <summary>
        /// The dialog for selecting Articles (games or exercises) from multiple chapters.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="allChaptersCheckbox">whether to show "All Chapter" check box</param>
        /// <param name="title">the title of the dialog to display</param>
        /// <param name="articleList">list of articles to show</param>
        /// <param name="allChapters">whether to start with all chapters</param>
        /// <param name="articleType"></param>
        public SelectArticlesDialog(TreeNode nd, 
            bool allChaptersCheckbox, string title, 
            ref ObservableCollection<ArticleListItem> articleList, 
            bool allChapters, 
            ArticlesAction actionOnArticles,
            GameData.ContentType articleType = GameData.ContentType.GENERIC)
        {
            _node = nd;
            _articleList = articleList;
            _articleType = articleType;
            ActionOnArticles = actionOnArticles;

            // if there is any selection outside the active chapter show all chapters (issue #465)
            InitializeComponent();

            UiLblEvalTime.Visibility = Visibility.Collapsed;
            UiTbEngEvalTime.Visibility = Visibility.Collapsed;

            UiBtnCopy.Visibility = Visibility.Collapsed;
            UiBtnMove.Visibility = Visibility.Collapsed;

            if (title != null)
            {
                Title = title;
            }
            if (allChaptersCheckbox)
            {
                UiCbAllChapters.Visibility = Visibility.Visible;
            }
            else
            {
                UiCbAllChapters.Visibility = Visibility.Collapsed;
            }

            SelectNodeReferences();

            if (!allChapters)
            {
                allChapters = IsAnySelectionOutsideActiveChapter();
            }
            _showActiveChapterOnly = !allChapters;
            UiCbAllChapters.IsChecked = allChapters;

            SetItemVisibility();

            // if everything is selected, check the box
            bool isAllSelected = true;
            foreach (ArticleListItem item in _articleList)
            {
                if (!item.IsSelected)
                {
                    isAllSelected = false;
                    break;
                }
            }
            UiCbSelectAll.IsChecked = isAllSelected;

            _articleListItemsSource = new ObservableCollection<ArticleListItem>();
            CopyVisibleToItemsSource(false);
            UiLvGames.ItemsSource = _articleListItemsSource;
        }

        /// <summary>
        /// Hides the "all chapters" check box and makes
        /// the evaluation time label and text box visible.
        /// </summary>
        public void SetupGuiForGamesEval()
        {
            UiLblEvalTime.Visibility = Visibility.Visible;
            UiTbEngEvalTime.Visibility = Visibility.Visible;
            double dval = (double)Configuration.EngineEvaluationTime / 1000.0;
            UiTbEngEvalTime.Text = dval.ToString("F1");
        }

        /// <summary>
        /// Shows buttons relevant to the CopyOrMove mode
        /// </summary>
        public void SetupGuiForGamesCopyOrMove()
        {
            UiBtnCopy.Visibility = Visibility.Visible;
            UiBtnCopy.Content = Properties.Resources.CopyArticles + "...";
            UiBtnMove.Visibility = Visibility.Visible;
            UiBtnMove.Content = Properties.Resources.MoveArticles + "...";
        }

        /// <summary>
        /// Returns a list of selected references.
        /// </summary>
        /// <returns></returns>
        public List<string> GetSelectedReferenceStrings()
        {
            List<string> refs = new List<string>();

            foreach (ArticleListItem item in _articleListItemsSource)
            {
                if (item.Article != null && item.IsSelected)
                {
                    GameData.ContentType ctype = item.Article.Tree.Header.GetContentType(out _);
                    if (ctype == GameData.ContentType.MODEL_GAME || ctype == GameData.ContentType.EXERCISE)
                    {
                        refs.Add(item.Article.Tree.Header.GetGuid(out _));
                    }
                }
            }

            return refs;
        }

        /// <summary>
        /// Copies the items marked as Visible from the main list
        /// to the GUI bound list.
        /// </summary>
        private void CopyVisibleToItemsSource(bool updateSelected)
        {
            if (updateSelected)
            {
                UpdateSelectedInOriginal();
            }

            _articleListItemsSource.Clear();
            foreach (ArticleListItem item in _articleList)
            {
                if (item.IsShown == true)
                {
                    _articleListItemsSource.Add(item);
                }
            }
        }

        /// <summary>
        /// Updates selection status in the original list
        /// based on the content of the GUI bound list.
        /// </summary>
        private void UpdateSelectedInOriginal()
        {
            foreach (ArticleListItem item in _articleListItemsSource)
            {
                ArticleListItem orig = _articleList.FirstOrDefault(x => x == item);
                if (orig != null)
                {
                    orig.IsSelected = item.IsSelected;
                }
            }
        }

        /// <summary>
        /// Marks as selected all references currently in the node.
        /// </summary>
        private void SelectNodeReferences()
        {
            if (_node == null)
            {
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(_node.ArticleRefs))
                {
                    string[] refs = _node.ArticleRefs.Split('|');
                    foreach (string guid in refs)
                    {
                        foreach (ArticleListItem item in _articleListItemsSource)
                        {
                            if (item.Article != null)
                            {
                                if (item.Article.Tree.Header.GetGuid(out _) == guid)
                                {
                                    item.IsSelected = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Checks if any selected item is not in the active chapter. 
        /// </summary>
        /// <returns></returns>
        private bool IsAnySelectionOutsideActiveChapter()
        {
            bool res = false;

            foreach (ArticleListItem item in _articleList)
            {
                if (item.IsSelected && item.Chapter != WorkbookManager.SessionWorkbook.ActiveChapter)
                {
                    res = true;
                    break;
                }
            }

            return res;
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
        /// Handles a double-click event on an item.
        /// If this is an article, opens the Game Preview dialog for the clicked game.
        /// If this is a Chaper header expands or collapses the clicked chapter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLvGames_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = GetListViewItemFromPoint(UiLvGames, e.GetPosition(UiLvGames));
            if (item != null && item.Content is ArticleListItem)
            {
                ArticleListItem artItem = (ArticleListItem)item.Content;
                Article art = artItem.Article;
                if (art != null)
                {
                    _lastClickedArticle = art;
                    InvokeGamePreviewDialog(art);
                }
                else
                {
                    // a chapter line was clicked
                    ChapterHeaderDoubleClicked(artItem);
                }
            }
        }

        /// <summary>
        /// Handles double click on a Chapter Header line.
        /// </summary>
        /// <param name="artItem"></param>
        private void ChapterHeaderDoubleClicked(ArticleListItem artItem)
        {
            if (artItem != null && artItem.IsChapterHeader)
            {
                GetChapterItemsSelectionStatus(artItem.ChapterIndex, out bool anySelected, out bool anyUnselected);

                artItem.IsChapterAllSelected = anySelected && !anyUnselected;
                artItem.IsChapterAllUnselected = !anySelected && anyUnselected;
                artItem.IsChapterExpanded = ExpandCollapseChapter(artItem);

                artItem.ChapterCheckBoxVisible =  (artItem.IsChapterExpanded || artItem.IsChapterAllSelected || artItem.IsChapterAllUnselected) ? "Visible" : "Collapsed";
                artItem.ChapterGrayedCheckBoxVisible = (!artItem.IsChapterExpanded && !artItem.IsChapterAllSelected && !artItem.IsChapterAllUnselected) ? "Visible" : "Collapsed";
            }
        }

        /// <summary>
        /// Checks the seletcion status of a chapter.
        /// Returns values indicating whether all items in the chapter are selected or unselected.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="anySelected"></param>
        /// <param name="anyUnSelected"></param>
        private void GetChapterItemsSelectionStatus(int chapterIndex, out bool anySelected, out bool anyUnSelected)
        {
            anySelected = false;
            anyUnSelected = false;

            // TODO: this can be optimized but is it worthwhile?
            foreach (ArticleListItem item in _articleList)
            {
                if (!item.IsChapterHeader && item.ChapterIndex == chapterIndex)
                {
                    if (item.IsSelected)
                    {
                        anySelected = true;
                    }
                    else
                    {
                        anyUnSelected = true;
                    }

                    if (anySelected && anyUnSelected)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if any Articles in a chapter are shown.
        /// This determines whether the chapter is expanded or collapsed.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <returns></returns>
        private bool AnyItemsShownFromChapter(int chapterIndex)
        {
            bool shown = false;
            foreach (ArticleListItem item in _articleList)
            {
                if (item.ChapterIndex == chapterIndex && !item.IsChapterHeader && item.IsShown)
                {
                    shown = true;
                    break;
                }
            }

            return shown;
        }

        /// <summary>
        /// Expands or collapses a chapter.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool ExpandCollapseChapter(ArticleListItem item)
        {
            bool expanded = true;

            try
            {
                expanded = AnyItemsShownFromChapter(item.ChapterIndex);
                if (item.IsChapterHeader)
                {
                    for (int i = 0; i < _articleList.Count; i++)
                    {
                        ArticleListItem art = _articleList[i];
                        if (art.ChapterIndex == item.ChapterIndex && !art.IsChapterHeader)
                        {
                            art.IsShown = !expanded;
                        }
                    }
                }
            }
            catch
            {
            }

            CopyVisibleToItemsSource(true);

            return !expanded;
        }

        /// <summary>
        /// Opens a game preview dialog.
        /// </summary>
        /// <param name="art"></param>
        private void InvokeGamePreviewDialog(Article art)
        {
            if (art != null)
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
        }

        /// <summary>
        /// SelectAll box was checked
        /// Check all currently shown items.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _articleListItemsSource)
            {
                if (item.IsShown)
                {
                    item.IsSelected = true;
                }
            }
        }

        /// <summary>
        /// SelectAll box was unchecked.
        /// Uncheck all currently shown items.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _articleListItemsSource)
            {
                if (item.IsShown)
                {
                    item.IsSelected = false;
                }
            }
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
            CopyVisibleToItemsSource(true);

            ArticleListItem chapterHeader = _articleListItemsSource.FirstOrDefault(x => x.Chapter == AppState.Workbook.ActiveChapter);
            if (chapterHeader != null)
            {
                UiLvGames.ScrollIntoView(chapterHeader);
            }
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
            CopyVisibleToItemsSource(true);
        }

        /// <summary>
        /// Handles a right-click event on an Article.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLvGames_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            return;
        }

        /// <summary>
        /// Prevents context menu from opening.
        /// If we want a context menu, because we clicked on an article
        /// UiLvGames_MouseRightButtonUp() will bring it up and this method won't
        /// be invoked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLvGames_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Invokes the game preview dialog when the user
        /// chooses the preview from the context menu. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnPreviewGame_Click(object sender, RoutedEventArgs e)
        {
            if (_lastClickedArticle != null)
            {
                InvokeGamePreviewDialog(_lastClickedArticle);
            }
        }

        /// <summary>
        /// The Open Game context menu item was selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiMnOpenGame_Click(object sender, RoutedEventArgs e)
        {
            SelectedArticle = _lastClickedArticle;
            UiBtnOk_Click(null, null);
        }

        /// <summary>
        /// The selection CheckBox was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckBox cb = sender as CheckBox;
                ArticleListItem item = cb.DataContext as ArticleListItem;
                if (item.IsChapterHeader)
                {
                    for (int i = 0; i < _articleListItemsSource.Count; i++)
                    {
                        ArticleListItem art = _articleListItemsSource[i];
                        if (art.ChapterIndex == item.ChapterIndex && !art.IsChapterHeader)
                        {
                            art.IsSelected = true;
                        }
                    }
                    if (!item.IsChapterExpanded)
                    {
                        ChapterHeaderDoubleClicked(item);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// The selection CheckBox was unclicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckBox cb = sender as CheckBox;
                ArticleListItem item = cb.DataContext as ArticleListItem;
                if (item.IsChapterHeader)
                {
                    for (int i = 0; i < _articleListItemsSource.Count; i++)
                    {
                        ArticleListItem art = _articleListItemsSource[i];
                        if (art.ChapterIndex == item.ChapterIndex && !art.IsChapterHeader)
                        {
                            art.IsSelected = false;
                        }
                    }
                    if (!item.IsChapterExpanded)
                    {
                        ChapterHeaderDoubleClicked(item);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// When the "grayed" chapter box is clicked, expand the chapter and ensure 
        /// that the box remains checked for when it is made visible the next time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbGrayedChapter_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb != null)
            {
                cb.IsChecked = true;
                ArticleListItem item = cb.DataContext as ArticleListItem;
                if (!item.IsChapterExpanded)
                {
                    ChapterHeaderDoubleClicked(item);
                }
            }
        }

        /// <summary>
        /// The user selected the Copy option.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCopy_Click(object sender, RoutedEventArgs e)
        {
            ActionOnArticles = ArticlesAction.COPY;
            UpdateSelectedInOriginal();
            DialogResult = true;
        }

        /// <summary>
        /// The user selected the Move option.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnMove_Click(object sender, RoutedEventArgs e)
        {
            ActionOnArticles = ArticlesAction.MOVE;
            UpdateSelectedInOriginal();
            DialogResult = true;
        }

        /// <summary>
        /// OK button was clicked. Exits with the result = true
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            UpdateSelectedInOriginal();
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
    }
}
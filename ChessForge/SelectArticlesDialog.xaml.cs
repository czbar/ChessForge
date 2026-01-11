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
        private ObservableCollection<ArticleListItem> _articleListOriginal;

        /// <summary>
        /// The temporary list to bind with the ListView in the GUI.
        /// </summary>
        private ObservableCollection<ArticleListItem> _articleListItemsSource = new ObservableCollection<ArticleListItem>();

        // type of articles handled
        private GameData.ContentType _articleType;

        // Node for which this dialog was invoked.
        private TreeNode _node;

        // flag to block processing of CheckBox events
        private bool _doNotProcessCheckEvents = false;

        // flags that the dialog is in the games evaluation mode
        private bool _isGameEvalMode = false;

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
            _articleListOriginal = articleList;
            _articleType = articleType;
            ActionOnArticles = actionOnArticles;

            // if there is any selection outside the active chapter show all chapters (issue #465)
            InitializeComponent();

            switch (ActionOnArticles)
            {
                case ArticlesAction.COPY_OR_MOVE_FOUND_GAMES:
                case ArticlesAction.COPY_OR_MOVE_FOUND_POSITIONS:
                    UiBtnOk.Visibility = Visibility.Collapsed;
                    break;
                case ArticlesAction.DELETE:
                    UiBtnOk.Content = Properties.Resources.Delete;
                    break;
                case ArticlesAction.COPY:
                    UiBtnOk.Content = Properties.Resources.Copy;
                    break;
                case ArticlesAction.MOVE:
                    UiBtnOk.Content = Properties.Resources.MoveArticles;
                    break;
            }

            // do not process CheckBox events while in the constructor
            _doNotProcessCheckEvents = true;

            UiLblEvalTime.Visibility = Visibility.Collapsed;
            UiTbEngEvalTime.Visibility = Visibility.Collapsed;

            UiLblMoveRange.Visibility = Visibility.Collapsed;
            UiLblDash.Visibility = Visibility.Collapsed;

            UiBtnCopy.Visibility = Visibility.Collapsed;
            UiBtnMove.Visibility = Visibility.Collapsed;

            Title = title ?? Title;

            UiCbAllChapters.Visibility = allChaptersCheckbox ? Visibility.Visible : UiCbAllChapters.Visibility = Visibility.Collapsed;

            if (nd != null)
            {
                // this dialog was invoked to edit references
                SelectNodeReferences();
            }

            if (!allChapters)
            {
                allChapters = IsAnySelectionOutsideActiveChapter();
            }
            _showActiveChapterOnly = !allChapters;
            UiCbAllChapters.IsChecked = allChapters;
            SetOriginalItemsVisibility();

            // if everything is selected, check the box
            bool isAllSelected = true;
            foreach (ArticleListItem item in _articleListOriginal)
            {
                if (item.IsSelected == false)
                {
                    isAllSelected = false;
                    break;
                }
            }
            UiCbSelectAll.IsChecked = isAllSelected;
            if (isAllSelected)
            {
                SelectAllShownOriginalItems();
            }

            // populate the list bound to the GUI ListView
            CopyShownItemsToItemsSource(false);
            UiLvGames.ItemsSource = _articleListItemsSource;

            _doNotProcessCheckEvents = false;
        }

        /// <summary>
        /// Hides the "all chapters" check box and makes
        /// the evaluation time label and text box visible.
        /// </summary>
        public void SetupGuiForGamesEval()
        {
            _isGameEvalMode = true;

            UiLblEvalTime.Visibility = Visibility.Visible;
            UiTbEngEvalTime.Visibility = Visibility.Visible;
            double dval = (double)Configuration.EngineEvaluationTime / 1000.0;
            UiTbEngEvalTime.Text = dval.ToString("F1");

            UiLblMoveRange.Visibility = Visibility.Visible;
            UiTbFromMove.Visibility = Visibility.Visible;
            if (Configuration.EvalMoveRangeStart == 0)
            {
                UiTbFromMove.Text = "";
            }
            else
            {
                UiTbFromMove.Text = Configuration.EvalMoveRangeStart.ToString();
            }
            UiLblDash.Visibility = Visibility.Visible;
            UiTbToMove.Visibility = Visibility.Visible;
            if (Configuration.EvalMoveRangeEnd == 0)
            {
                UiTbToMove.Text = "";
            }
            else
            {
                UiTbToMove.Text = Configuration.EvalMoveRangeEnd.ToString();
            }

            GridLength row0Height = UiGridMain.RowDefinitions[0].Height;
            GridLength row1Height = UiGridMain.RowDefinitions[1].Height;

            UiGridMain.RowDefinitions[0].Height = new GridLength(row0Height.Value + 40);
            UiGridMain.RowDefinitions[1].Height = new GridLength(row1Height.Value - 40);
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


        //***********************************************************************************
        //
        // Functions dealing with references when the dialog is invoked to select references
        //
        //***********************************************************************************

        /// <summary>
        /// Returns a list of selected references.
        /// </summary>
        /// <returns></returns>
        public List<string> GetSelectedReferenceStrings()
        {
            List<string> refs = new List<string>();

            foreach (ArticleListItem item in _articleListItemsSource)
            {
                if (item.Article != null && item.IsSelected == true)
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
        /// Marks as selected all references currently in the node.
        /// This will only run if _node is not null i.e. when this dialog
        /// is invoked for setting up references.
        /// </summary>
        private void SelectNodeReferences()
        {
            if (_node == null)
            {
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(_node.References))
                {
                    string[] refs = _node.References.Split('|');
                    foreach (string guid in refs)
                    {
                        foreach (ArticleListItem item in _articleListOriginal)
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

        //*************** END of reference handling



        /// <summary>
        /// Copies the items marked as Shown from the main list
        /// to the GUI bound list.
        /// </summary>
        /// <param name="updateSelected">If true, update item selection flags in the original list
        /// based on the selection flags in the GUI bound list. This must be done before
        /// copying the items.
        /// </param>
        private void CopyShownItemsToItemsSource(bool updateSelected)
        {
            if (_articleListItemsSource == null)
            {
                // the GUI bound list is null so this is called too early (from the constructor)
                return;
            }

            if (updateSelected)
            {
                UpdateSelectedInOriginal();
            }

            _articleListItemsSource.Clear();
            foreach (ArticleListItem item in _articleListOriginal)
            {
                if (item.IsShown == true)
                {
                    _articleListItemsSource.Add(item);
                }
            }
        }

        /// <summary>
        /// Updates item selection flags in the original list
        /// based on the content of the GUI bound list.
        /// </summary>
        private void UpdateSelectedInOriginal()
        {
            foreach (ArticleListItem item in _articleListItemsSource)
            {
                ArticleListItem orig = _articleListOriginal.FirstOrDefault(x => x == item);
                if (orig != null)
                {
                    orig.IsSelected = item.IsSelected;
                }
            }
        }

        /// <summary>
        /// Checks if any selected item is not in the active chapter. 
        /// </summary>
        /// <returns></returns>
        private bool IsAnySelectionOutsideActiveChapter()
        {
            bool res = false;

            foreach (ArticleListItem item in _articleListOriginal)
            {
                if (item.IsSelected == true && item.Chapter != WorkbookManager.SessionWorkbook.ActiveChapter)
                {
                    res = true;
                    break;
                }
            }

            return res;
        }

        /// <summary>
        /// Sets the IsShown property on all items
        /// in the original list.
        /// </summary>
        private void SetOriginalItemsVisibility()
        {
            foreach (ArticleListItem item in _articleListOriginal)
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
            }
        }

        /// <summary>
        /// Checks the selection status of a chapter.
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
            foreach (ArticleListItem item in _articleListOriginal)
            {
                if (!item.IsChapterHeader && item.ChapterIndex == chapterIndex)
                {
                    if (item.IsSelected == true)
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
            foreach (ArticleListItem item in _articleListOriginal)
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
        /// First the Shown status is updated in the original list
        /// and then the GUI bound list is recreated.
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
                    for (int i = 0; i < _articleListOriginal.Count; i++)
                    {
                        ArticleListItem art = _articleListOriginal[i];
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

            CopyShownItemsToItemsSource(true);

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
                try
                {
                    List<string> gameIdList = new List<string>();
                    List<Article> games = new List<Article> { art };
                    gameIdList.Add(art.Tree.Header.GetGuid(out _));

                    SingleGamePreviewDialog dlg = new SingleGamePreviewDialog(gameIdList, games);
                    GuiUtilities.PositionDialog(dlg, this, 20);
                    dlg.ShowDialog();
                }
                catch (Exception ex)
                {
                    AppLog.Message("InvokeGamePreviewDialog() ", ex);
                }
            }
        }

        /// <summary>
        /// The overall selection state of the parent chapter is checked.
        /// It could be all selected, all unselected or partial.
        /// The chapter header's checkbox is updated accordingly.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool? ChapterArticlesSelectionState(ArticleListItem item)
        {
            bool selectedFound = false;
            bool unselectedFound = false;

            bool? result = null;

            if (item != null)
            {
                ArticleListItem chapterHeader = null;

                for (int i = 0; i < _articleListItemsSource.Count; i++)
                {
                    if (_articleListItemsSource[i].ChapterIndex == item.ChapterIndex)
                    {
                        if (_articleListItemsSource[i].IsChapterHeader)
                        {
                            chapterHeader = _articleListItemsSource[i];
                        }
                        else
                        {
                            if (_articleListItemsSource[i].IsSelected == true)
                            {
                                selectedFound = true;
                            }
                            else if (_articleListItemsSource[i].IsSelected == false)
                            {
                                unselectedFound = true;
                            }

                            if (selectedFound && unselectedFound)
                            {
                                break;
                            }
                        }
                    }
                }

                if (selectedFound && !unselectedFound)
                {
                    result = true;
                }
                else if (!selectedFound && unselectedFound)
                {
                    result = false;
                }
                else
                {
                    result = null;
                }

                if (chapterHeader != null)
                {
                    chapterHeader.IsChapterAllSelected = selectedFound && !unselectedFound;
                    chapterHeader.IsChapterAllUnselected = !selectedFound && unselectedFound;
                    chapterHeader.IsSelected = result;
                }
            }

            return result;
        }

        /// <summary>
        /// Sets the IsSelected flag on all shown items
        /// in the original list.
        /// </summary>
        private void SelectAllShownOriginalItems()
        {
            foreach (var item in _articleListOriginal)
            {
                if (item.IsShown)
                {
                    item.IsSelected = true;
                }
            }
        }

        /// <summary>
        /// SelectAll box was checked.
        /// Set IsSelected flag on all items in the GUI bound list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            if (_doNotProcessCheckEvents)
            {
                return;
            }

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
        /// Reset IsSelected flag on all items in the GUI bound list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_doNotProcessCheckEvents)
            {
                return;
            }

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
            if (_doNotProcessCheckEvents)
            {
                return;
            }

            _showActiveChapterOnly = false;

            SetOriginalItemsVisibility();
            CopyShownItemsToItemsSource(true);

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
            if (_doNotProcessCheckEvents)
            {
                return;
            }

            _showActiveChapterOnly = true;
            SetOriginalItemsVisibility();
            CopyShownItemsToItemsSource(true);
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


        // flag to block processing of CheckBox events when we are programmatically processing them
        private bool _blockGameClicks = false;

        /// <summary>
        /// The selection CheckBox was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SelectionCheckBoxClicked(sender, true);
        }

        /// <summary>
        /// The selection CheckBox was unclicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectionCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SelectionCheckBoxClicked(sender, false);
        }

        /// <summary>
        /// A chapter or article IsSelected CheckBox was clicked.
        /// If a chapter CheckBox was clicked, the chapter will be expanded
        /// if it is not already and all articles in the chapter will be selected or unselected
        /// depending on the new state of the chapter CheckBox.
        /// If an article CheckBox was clicked, the overall chapter selection state will be updated
        /// as it could be all selected, all unselected or partial.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="check"></param>
        private void SelectionCheckBoxClicked(object sender, bool check)
        {
            if (_blockGameClicks)
            {
                return;
            }

            try
            {
                CheckBox cb = sender as CheckBox;
                ArticleListItem item = cb.DataContext as ArticleListItem;
                if (item != null)
                {
                    if (item.IsChapterHeader)
                    {
                        // a chapter header checkbox was clicked
                        if (!item.IsChapterExpanded)
                        {
                            ChapterHeaderDoubleClicked(item);
                        }

                        _blockGameClicks = true;
                        for (int i = 0; i < _articleListItemsSource.Count; i++)
                        {
                            ArticleListItem art = _articleListItemsSource[i];
                            if (art.ChapterIndex == item.ChapterIndex && !art.IsChapterHeader)
                            {
                                art.IsSelected = check;
                            }
                        }
                    }
                    else
                    {
                        // an article checkbox was clicked
                        ChapterArticlesSelectionState(item);
                    }
                }
            }
            catch
            {
            }
            finally
            {
                _blockGameClicks = false;
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
            ExitOk();
        }

        /// <summary>
        /// The user selected the Move option.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnMove_Click(object sender, RoutedEventArgs e)
        {
            ActionOnArticles = ArticlesAction.MOVE;
            ExitOk();
        }

        /// <summary>
        /// OK button was clicked. Exits with the result = true
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (_isGameEvalMode && !IsValidMoveRange())
            {
                MessageBox.Show(Properties.Resources.MsgInvalidMoveRange, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                ExitOk();
            }
        }

        /// <summary>
        /// Checks if the entered move range is valid. 
        /// </summary>
        /// <returns></returns>
        private bool IsValidMoveRange()
        {
            int ivalFrom;
            int.TryParse(UiTbFromMove.Text, out ivalFrom);

            int ivalTo;
            int.TryParse(UiTbToMove.Text, out ivalTo);

            return ivalFrom <= ivalTo || ivalTo == 0;
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
        /// Copy all selection flags to the original.
        /// Deselect all not in the active chapter if the all chapters CheckBox is unchecked
        /// </summary>
        private void ExitOk()
        {
            UpdateSelectedInOriginal();
            if (_showActiveChapterOnly)
            {
                ClearNonActiveChapterSelectedInOriginal();
            }
            DialogResult = true;
        }

        /// <summary>
        /// Ensures that all items not from the active chapter are deselected.
        /// </summary>
        private void ClearNonActiveChapterSelectedInOriginal()
        {
            foreach (ArticleListItem item in _articleListOriginal)
            {
                if (item.ChapterIndex != AppState.Workbook.ActiveChapterIndex)
                {
                    item.IsSelected = false;
                }
            }
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            switch (ActionOnArticles)
            {
                case ArticlesAction.COPY_OR_MOVE_FOUND_GAMES:
                    System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Finding-Games");
                    break;
                case ArticlesAction.COPY_OR_MOVE_FOUND_POSITIONS:
                    System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Copying-Games-and-Exercises-between-Chapters#Copy-Games-and-Exercises-from-Identical-Positions");
                    break;
                case ArticlesAction.DELETE:
                    if (_articleType == GameData.ContentType.EXERCISE)
                    {
                        System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Deleting-Exercises#deleting-multiple-exercises");
                    }
                    else
                    {
                        System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Deleting-Games#deleting-multiple-games");
                    }
                    break;
                case ArticlesAction.COPY:
                    System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Copying-Games-and-Exercises-between-Chapters");
                    break;
                case ArticlesAction.MOVE:
                    System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Moving-Games-and-Exercises-between-Chapters");
                    break;
                case ArticlesAction.EVALUATE:
                    System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Engine-and-Evaluation#evaluate-games");
                    break;
                default:
                    System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/User's-Manual");
                    break;
            }
        }

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Manages training bookmarks.
    /// The WorkbookTree keeps the Bookmark data objects.
    /// This class mainatins a list of BOOKMARKS_PER_PAGE (9) BookmarkView objects 
    /// that contain
    /// references to the relevant GUI controls
    /// and can show position from the Bookmark objects.
    /// </summary>
    public class BookmarkManager
    {
        /// <summary>
        /// The page currently shown in the GUI.
        /// The first page has number 1 (not 0).
        /// </summary>
        private static int _currentPage = 1;

        /// <summary>
        /// The number of Bookmark pages.
        /// </summary>
        private static int _maxPage
        {
            get {
                int bm_count = _mainWin.Workbook.Bookmarks.Count;
                if (bm_count <= BOOKMARKS_PER_PAGE)
                    return 1;
                else
                    return (bm_count - 1) / BOOKMARKS_PER_PAGE + 1;
                }
        }

        /// <summary>
        /// Max number of bookmarks that can be shown in the GUI.
        /// There is no limit on how many bookmarks there can be
        /// altogether.
        /// </summary>
        public static readonly int BOOKMARKS_PER_PAGE = 9;

        /// <summary>
        /// A Bookmark context menu uses this value
        /// to apply user selection to the appropriate bookmark.
        /// </summary>
        public static int ClickedIndex = -1;

        /// <summary>
        /// The list of bookmarks.
        /// </summary>
        public static List<BookmarkView> Bookmarks = new List<BookmarkView>();

        /// <summary>
        /// Index in the list of bookmarks of the bookmark currently being
        /// active in a training session.
        /// Precisely one bookmark can be active during a session. 
        /// </summary>
        public static int ActiveBookmarkInTraining = -1;

        // main application window
        private static MainWindow _mainWin;

        /// <summary>
        /// Resets or recreates all the bookmarks.
        /// Called on app initialization..
        /// </summary>
        public static void InitBookmarksGui(MainWindow mainWin)
        {
            _mainWin = mainWin;

            Bookmarks.Clear();

            Bookmarks.Add(new BookmarkView(new ChessBoard(_mainWin.UiCnvBookmark_1, _mainWin.UiImgBookmark_1, _mainWin.UiLblBookmark_1, false)));
            Bookmarks.Add(new BookmarkView(new ChessBoard(_mainWin.UiCnvBookmark_2, _mainWin.UiImgBookmark_2, _mainWin.UiLblBookmark_2, false)));
            Bookmarks.Add(new BookmarkView(new ChessBoard(_mainWin.UiCnvBookmark_3, _mainWin.UiImgBookmark_3, _mainWin.UiLblBookmark_3, false)));
            Bookmarks.Add(new BookmarkView(new ChessBoard(_mainWin.UiCnvBookmark_4, _mainWin.UiImgBookmark_4, _mainWin.UiLblBookmark_4, false)));
            Bookmarks.Add(new BookmarkView(new ChessBoard(_mainWin.UiCnvBookmark_5, _mainWin.UiImgBookmark_5, _mainWin.UiLblBookmark_5, false)));
            Bookmarks.Add(new BookmarkView(new ChessBoard(_mainWin.UiCnvBookmark_6, _mainWin.UiImgBookmark_6, _mainWin.UiLblBookmark_6, false)));
            Bookmarks.Add(new BookmarkView(new ChessBoard(_mainWin.UiCnvBookmark_7, _mainWin.UiImgBookmark_7, _mainWin.UiLblBookmark_7, false)));
            Bookmarks.Add(new BookmarkView(new ChessBoard(_mainWin.UiCnvBookmark_8, _mainWin.UiImgBookmark_8, _mainWin.UiLblBookmark_8, false)));
            Bookmarks.Add(new BookmarkView(new ChessBoard(_mainWin.UiCnvBookmark_9, _mainWin.UiImgBookmark_9, _mainWin.UiLblBookmark_9, false)));
        }

        /// <summary>
        /// Initializes the GUI for the bookmarks.
        /// Only BOOKMARKS_PER_PAGE bookmarks can be shown at most.
        /// </summary>
        public static void ShowBookmarks()
        {
            SortBookmarks();

            for (int i = 0; i < _mainWin.Workbook.Bookmarks.Count; i++)
            {
                if (i >= Bookmarks.Count)
                    break;

                Bookmarks[i].BookmarkData = _mainWin.Workbook.Bookmarks[i];
                Bookmarks[i].Activate();
            }
        }

        /// <summary>
        /// Sorts bookmarks by round numbers and then
        /// color-to-move.
        /// This method should be called after initilaization
        /// and any subsequents addition.
        /// </summary>
        public static void SortBookmarks()
        {
            _mainWin.Workbook.Bookmarks.Sort();
            ResyncBookmarks(_currentPage);
        }

        /// <summary>
        /// resets all bookmark chessboards in the Bookmark view.
        /// </summary>
        public static void ClearBookmarksGui()
        {
            foreach (BookmarkView bv in Bookmarks)
            {
                bv.Deactivate();
                bv.SetOpacity(0.5);
            }
        }

        /// <summary>
        /// Adds a bookmark to the list of bookmarks at index 0.
        /// If there are 9 or more bookmarks, shifts them so 
        /// the lst one drops out of the GUI's first page.
        /// </summary>
        /// <returns>0 on success, 1 if already exists, -1 on failure</returns>
        public static int AddBookmark(int nodeId)
        {
            TreeNode nd = _mainWin.Workbook.GetNodeFromNodeId(nodeId);
            if (nd != null)
            {
                //add to the list in the Workbook
                if (_mainWin.Workbook.AddBookmark(nd, true) == 0)
                {
                    SortBookmarks();
                    ResyncBookmarks(_currentPage);
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                return -1;
            }

        }

        /// <summary>
        /// Deletes the bookmark at index ClickedIndex.
        /// </summary>
        public static void DeleteBookmark()
        {
            if (ClickedIndex < 0 || ClickedIndex >= Bookmarks.Count)
            {
                return;
            }

            TreeNode nd = Bookmarks[ClickedIndex].BookmarkData.Node;
            if (nd != null)
            {
                _mainWin.Workbook.DeleteBookmark(nd);
                if (_currentPage > _maxPage)
                {
                    _currentPage = _maxPage;
                }
                ResyncBookmarks(_currentPage);
            }
            LearningMode.SaveWorkbookFile();
        }

        /// <summary>
        /// Removes all bookmarks.
        /// </summary>
        public static void DeleteAllBookmarks()
        {
            if (Bookmarks.Count > 0)
            {
                if (MessageBox.Show("This will delete all Bookmarks. Proceed?"
                    , "Training Bookmarks", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            foreach (BookmarkView bm in Bookmarks)
            {
                if (bm.BookmarkData != null && bm.BookmarkData.Node != null)
                {
                    bm.BookmarkData.Node.IsBookmark = false;
                }
            }

            ClearBookmarksGui();
            Bookmarks.Clear();
            LearningMode.SaveWorkbookFile();
        }

        /// <summary>
        /// A request to go back one page has been made
        /// by the user.
        /// </summary>
        public static void PageDown()
        {
            if (_currentPage == 1)
                return;

            _currentPage--;
            ResyncBookmarks(_currentPage);
        }

        /// <summary>
        /// A request to go one page forward has been made
        /// by the user.
        /// </summary>
        public static void PageUp()
        {
            if (_currentPage == _maxPage)
                return;

            _currentPage++;
            ResyncBookmarks(_currentPage);
        }

        /// <summary>
        /// Handles a click on one of the bookmark chessboards.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cm"></param>
        /// <param name="e"></param>
        public static void ChessboardClickedEvent(string name, ContextMenu cm, MouseButtonEventArgs e)
        {
            int underscore = name.LastIndexOf('_');
            int bkmNo;
            if (underscore > 0 && underscore < name.Length - 1)
            {
                if (!int.TryParse(name.Substring(underscore + 1), out bkmNo))
                {
                    return;
                }

                // adjust bkmNo for Page number
                bkmNo = (bkmNo - 1) + (_currentPage - 1) * BOOKMARKS_PER_PAGE;

                if (e.ChangedButton == MouseButton.Left)
                {
                    _mainWin.SetAppInTrainingMode(bkmNo);
                    e.Handled = true;
                }
                // for the benefit of the context menu set the clicked index.
                ClickedIndex = bkmNo;
                EnableBookmarkMenus(cm, true);
            }
        }

        /// <summary>
        /// Generate bookmarks automatically.
        /// This option should only be available from the menus if there are currently no
        /// bookmarks.
        /// But we will handle the situtation if this condition somehow is not met.
        /// </summary>
        public static void GenerateBookmarks()
        {
            if (Bookmarks.Count > 0)
            {
                if (MessageBox.Show("Generated bookmarks will replace the ones in the Workbook. Proceed?"
                    , "Training Bookmarks", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            DeleteAllBookmarks();
            _mainWin.Workbook.GenerateBookmarks();
            _mainWin.UiTabBookmarks.Focus();
            ShowBookmarks();
        }

        /// <summary>
        /// Manages state of the Bookmark context menu.
        /// The isEnabled argument is true if the user's last click
        /// was on a bookmark rather than elsewhere in the view.
        /// Some items are enabled according to the value of isEnable
        /// while some have a different logic (e.g. Delete All Bookmarks
        /// is always enabled if there is at least one bookmark.
        /// </summary>
        /// <param name="cmn"></param>
        /// <param name="isEnabled"></param>
        public static void EnableBookmarkMenus(ContextMenu cmn, bool isEnabled)
        {
            // ClickedIndex should be in sync with isEnabled but double check just in case
            if (ClickedIndex < 0)
            {
                isEnabled = false;
            }

            foreach (var item in cmn.Items)
            {
                if (item is MenuItem)
                {
                    MenuItem menuItem = item as MenuItem;
                    switch (menuItem.Name)
                    {
                        case "_mnTrainFromBookmark":
                            menuItem.IsEnabled = isEnabled;
                            break;
                        case "_mnAddBookmark":
                            menuItem.IsEnabled = true;
                            break;
                        case "_mnDeleteBookmark":
                            menuItem.IsEnabled = isEnabled;
                            break;
                        case "_mnDeleteAllBookmarks":
                            menuItem.IsEnabled = Bookmarks.Count > 0;
                            break;
                        case "_mnGenerateBookmark":
                            menuItem.Visibility = Bookmarks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Syncs the list of BookmarkViews with the Workbook's list
        /// of bookmarks.
        /// </summary>
        private static void ResyncBookmarks(int pageNo)
        {
            int count = _mainWin.Workbook.Bookmarks.Count;

            int start = (pageNo - 1) * BOOKMARKS_PER_PAGE;
            int end = pageNo * BOOKMARKS_PER_PAGE - 1;

            for (int i = start; i <= end; i++)
            {
                if (i < count)
                {
                    Bookmarks[i - start].BookmarkData = _mainWin.Workbook.Bookmarks[i];
                    Bookmarks[i - start].Activate();
                }
                else
                {
                    Bookmarks[i - start].BookmarkData = null;
                    Bookmarks[i - start].Deactivate();
                }
            }

            ShowPageControls();
        }

        /// <summary>
        /// Shows/hides paging controls and updates
        /// the label text as per the current number of 
        /// Bookmarks and currently displayed page.
        /// </summary>
        private static void ShowPageControls()
        {
            int bm_count = _mainWin.Workbook.Bookmarks.Count;
            if (bm_count <= BOOKMARKS_PER_PAGE)
            {
                _mainWin.UiGridBookmarks.RowDefinitions[0].Height = new GridLength(0);
                _mainWin.UiCnvPaging.Visibility = Visibility.Collapsed;
            }
            else
            {
                _mainWin.UiCnvPaging.Visibility = Visibility.Visible;
                _mainWin.UiGridBookmarks.RowDefinitions[0].Height = new GridLength(20);
                _mainWin.UiLblBookmarkPage.Visibility = Visibility.Visible;
                _mainWin.UiLblBookmarkPage.Content = "Page " + _currentPage.ToString() +" of " + _maxPage.ToString();
                if (_currentPage == 1)
                {
                    _mainWin.UiImgRightArrow.Visibility = Visibility.Visible;
                    _mainWin.UiImgLeftArrow.Visibility = Visibility.Hidden;
                }
                else if (_currentPage == _maxPage)
                {
                    _mainWin.UiImgRightArrow.Visibility = Visibility.Hidden;
                    _mainWin.UiImgLeftArrow.Visibility = Visibility.Visible;
                }
                else
                {
                    _mainWin.UiImgRightArrow.Visibility = Visibility.Visible;
                    _mainWin.UiImgLeftArrow.Visibility = Visibility.Visible;
                }
            }
        }
    }
}

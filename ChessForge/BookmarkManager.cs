using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using GameTree;
using ChessForge.Properties;
using System.Windows.Media.Imaging;

namespace ChessForge
{
    /// <summary>
    /// Manages bookmarks from the Study, Games and Exercises.
    /// Each VariationTree holds its the Bookmark objects in its Bookmarks list property.
    /// 
    /// This class maintains a list of BOOKMARKS_PER_PAGE (9) BookmarkView objects 
    /// that contain references to the relevant GUI controls
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
        /// Indicates whether Bookmarks need to be rebuilt
        /// </summary>
        public static bool IsDirty = true;

        /// <summary>
        /// The list of bookmarks shown in the GUI.
        /// </summary>
        public static List<BookmarkView> BookmarkGuiList = new List<BookmarkView>();

        /// <summary>
        /// List of all bookmarks in the chapter
        /// </summary>
        public static List<BookmarkWrapper> BookmarkList = new List<BookmarkWrapper>();

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

        // main application window
        private static MainWindow _mainWin;

        // Chapter for which this view was built, if null, the bookmarks are from all chapters
        private static Chapter _parentChapter;

        /// <summary>
        /// Resets the bookmark list and all Bookmark chessboards in the Bookmark view.
        /// </summary>
        public static void ClearBookmarksGui()
        {
            BookmarkList.Clear();

            foreach (BookmarkView bv in BookmarkGuiList)
            {
                bv.Deactivate();
                bv.SetOpacity(0.5);
            }
            _mainWin.UiGridBookmarks.RowDefinitions[0].Height = GridLength.Auto;
            
//            _mainWin.UiCnvPaging.Visibility = Visibility.Collapsed;
            _mainWin.UiLblBookmarkPage.Visibility = Visibility.Collapsed;
            _mainWin.UiImgLeftArrow.Visibility = Visibility.Collapsed;
            _mainWin.UiImgRightArrow.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Rebuilds the list of bookmarks for the current chapter
        /// </summary>
        public static void BuildBookmarkList(Chapter chapter)
        {
            _parentChapter = chapter;

            BookmarkList.Clear();
            int chapterIndex = WorkbookManager.SessionWorkbook.GetChapterIndex(chapter);
            if (chapter != null)
            {
                foreach (Bookmark bkm in chapter.StudyTree.Tree.Bookmarks)
                {
                    BookmarkWrapper bkv = new BookmarkWrapper(chapterIndex, chapter.StudyTree.Tree, bkm, -1);
                    BookmarkList.Add(bkv);
                }

                for (int i = 0; i < chapter.GetModelGameCount(); i++)
                {
                    Article art = chapter.ModelGames[i];
                    foreach (Bookmark bkm in art.Tree.Bookmarks)
                    {
                        BookmarkWrapper bkv = new BookmarkWrapper(chapterIndex, art.Tree, bkm, i);
                        BookmarkList.Add(bkv);
                    }
                }

                for (int i = 0; i < chapter.GetExerciseCount(); i++)
                {
                    Article art = chapter.Exercises[i];
                    foreach (Bookmark bkm in art.Tree.Bookmarks)
                    {
                        BookmarkWrapper bkv = new BookmarkWrapper(chapterIndex, art.Tree, bkm, i);
                        BookmarkList.Add(bkv);
                    }
                }
            }

            SortBookmarks();
        }

        /// <summary>
        /// The number of Bookmark pages.
        /// </summary>
        private static int _maxPage
        {
            get
            {
                int bm_count = BookmarkList.Count;
                if (bm_count <= BOOKMARKS_PER_PAGE)
                {
                    return 1;
                }
                else
                {
                    return (bm_count - 1) / BOOKMARKS_PER_PAGE + 1;
                }
            }
        }

        /// <summary>
        /// Resets or recreates all the bookmarks.
        /// Called on app initialization..
        /// </summary>
        public static void InitBookmarksGui(MainWindow mainWin)
        {
            _mainWin = mainWin;

            BookmarkGuiList.Clear();

            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_1, _mainWin.UiImgBookmark_1, _mainWin.UiLblBookmark_1, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_2, _mainWin.UiImgBookmark_2, _mainWin.UiLblBookmark_2, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_3, _mainWin.UiImgBookmark_3, _mainWin.UiLblBookmark_3, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_4, _mainWin.UiImgBookmark_4, _mainWin.UiLblBookmark_4, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_5, _mainWin.UiImgBookmark_5, _mainWin.UiLblBookmark_5, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_6, _mainWin.UiImgBookmark_6, _mainWin.UiLblBookmark_6, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_7, _mainWin.UiImgBookmark_7, _mainWin.UiLblBookmark_7, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_8, _mainWin.UiImgBookmark_8, _mainWin.UiLblBookmark_8, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_9, _mainWin.UiImgBookmark_9, _mainWin.UiLblBookmark_9, false, false)));
        }

        /// <summary>
        /// Sorts bookmarks by move number and then color-to-move.
        /// This method should be called after initialization
        /// and any subsequents addition.
        /// </summary>
        public static void SortBookmarks()
        {
            BookmarkList.Sort();
            ResyncBookmarks(_currentPage);
        }

        /// <summary>
        /// Adds a bookmark to the list of bookmarks.
        /// Sorts the bookmarks and updates the GUI.
        /// </summary>
        /// <returns>newly created bookmark on success or null if operation failed</returns>
        public static Bookmark AddBookmark(VariationTree tree, TreeNode nd, int articleIndex)
        {
            Bookmark bm = null;

            if (tree != null && nd != null)
            {
                //add to the list in the Workbook
                bm = tree.AddBookmark(nd, true);
                if (bm != null)
                {
                    BookmarkWrapper bmv = new BookmarkWrapper(WorkbookManager.SessionWorkbook.ActiveChapterIndex, tree, bm, articleIndex);
                    BookmarkList.Add(bmv);
                    SortBookmarks();
                    ResyncBookmarks(_currentPage);
                    AppState.IsDirty = true;
                }
            }

            return bm;
        }

        /// <summary>
        /// Adds a bookmark to the list of bookmarks.
        /// </summary>
        /// <returns>newly created bookmark or null if the operation failed</returns>
        public static Bookmark AddBookmark(VariationTree tree, int nodeId, int index)
        {
            TreeNode nd = tree.GetNodeFromNodeId(nodeId);
            if (nd != null)
            {
                return AddBookmark(tree, nd, index);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Deletes the bookmark at index ClickedIndex.
        /// </summary>
        public static void DeleteBookmark()
        {
            if (ClickedIndex < 0 || ClickedIndex >= BookmarkGuiList.Count)
            {
                return;
            }

            BookmarkWrapper bmv = BookmarkGuiList[ClickedIndex].BookmarkWrapper;
            TreeNode nd = bmv.Bookmark.Node;
            VariationTree tree = bmv.Tree;
            if (nd != null)
            {
                tree.DeleteBookmark(nd);
                BookmarkList.Remove(bmv);
                if (_currentPage > _maxPage)
                {
                    _currentPage = _maxPage;
                }
                ResyncBookmarks(_currentPage);
            }
            AppState.IsDirty = true;
        }

        /// <summary>
        /// Removes all bookmarks.
        /// </summary>
        public static void DeleteAllBookmarks(bool askUser = true)
        {
            if (askUser && BookmarkList.Count > 0)
            {
                if (MessageBox.Show(Strings.GetResource("ConfirmDeleteAllBookmarks")
                    , Strings.GetResource("Bookmarks"), MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            foreach (BookmarkWrapper bm in BookmarkList)
            {
                if (bm.Bookmark != null && bm.Bookmark.Node != null)
                {
                    bm.Tree.DeleteBookmark(bm.Bookmark.Node);
                    bm.Bookmark.Node.IsBookmark = false;
                    bm.Bookmark = null;
                }
            }

            BookmarkList.Clear();

            ClearBookmarksGui();
            AppState.IsDirty = true;
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
            if (_mainWin.ActiveVariationTree == null)
            {
                return;
            }

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
                            menuItem.IsEnabled = BookmarkList.Count > 0;
                            break;
                        case "_mnGenerateBookmark":
                            menuItem.Visibility = BookmarkList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Syncs the list of BookmarkViews with the Workbook's list
        /// of bookmarks.
        /// </summary>
        public static void ResyncBookmarks(int pageNo)
        {
            int count = BookmarkList.Count;

            int start = (pageNo - 1) * BOOKMARKS_PER_PAGE;
            int end = pageNo * BOOKMARKS_PER_PAGE - 1;

            for (int i = start; i <= end; i++)
            {
                if (i < count)
                {
                    GameData.ContentType contetType = BookmarkList[i].ContentType;

                    BookmarkGuiList[i - start].BookmarkWrapper = BookmarkList[i];

                    BookmarkGuiList[i - start].Activate();

                    BitmapImage imgBoard;

                    switch (contetType)
                    {
                        case GameData.ContentType.STUDY_TREE:
                            imgBoard = ChessBoards.ChessBoardBlueSmall;
                            break;
                        case GameData.ContentType.MODEL_GAME:
                            imgBoard = ChessBoards.ChessBoardBrownShadesSmall;
                            break;
                        case GameData.ContentType.EXERCISE:
                            imgBoard = ChessBoards.ChessBoardPaleBlue;
                            break;
                        default:
                            imgBoard = ChessBoards.ChessBoardGreySmall;
                            break;
                    }

                    BookmarkGuiList[i - start].ChessBoard.BoardImgCtrl.Source = imgBoard;
                }
                else
                {
                    BookmarkGuiList[i - start].BookmarkWrapper = null;
                    BookmarkGuiList[i - start].Deactivate();
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
            int bm_count = BookmarkList.Count;
            if (bm_count <= BOOKMARKS_PER_PAGE)
            {
                //_mainWin.UiGridBookmarks.RowDefinitions[0].Height = new GridLength(0);

                //_mainWin.UiCnvPaging.Visibility = Visibility.Collapsed;
                _mainWin.UiLblBookmarkPage.Visibility = Visibility.Collapsed;
                _mainWin.UiImgLeftArrow.Visibility = Visibility.Collapsed;
                _mainWin.UiImgRightArrow.Visibility = Visibility.Collapsed;
            }
            else
            {
                //_mainWin.UiCnvPaging.Visibility = Visibility.Visible;
                //_mainWin.UiGridBookmarks.RowDefinitions[0].Height = new GridLength(20);
                _mainWin.UiLblBookmarkPage.Visibility = Visibility.Visible;
                _mainWin.UiLblBookmarkPage.Content = ResourceUtils.GetCounterBarText("Page", _currentPage - 1, _maxPage);
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

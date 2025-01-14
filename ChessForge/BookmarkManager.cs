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
        public static readonly int BOOKMARKS_PER_PAGE = 12;

        /// <summary>
        /// A Bookmark context menu uses this value
        /// to apply user selection to the appropriate bookmark.
        /// </summary>
        public static int ClickedIndex = -1;

        /// <summary>
        /// Node in the last clicked bookmark.
        /// </summary>
        public static TreeNode SelectedBookmarkNode = null;

        /// <summary>
        /// Returns the bookmark count.
        /// </summary>
        public static int BookmarkCount
        {
            get => BookmarkList.Count;
        }

        /// <summary>
        /// Workbook for which the Bookmark view was last rebuilt.
        /// </summary>
        private static Workbook _currentWorkbook;

        /// <summary>
        /// Chapter that was last selected.
        /// </summary>
        private static Chapter _selectedChapter;

        /// <summary>
        /// Recently selected content type.
        /// If null, index 0 will be assumed.
        /// </summary>
        private static ContentTypeListItem _selectedContentType;

        /// <summary>
        /// Recently selected chapter number.
        /// If null, index 0 will be assumed.
        /// </summary>
        private static int _selectedChapterIndex;

        /// <summary>
        /// The page currently shown in the GUI.
        /// The first page has number 1 (not 0).
        /// </summary>
        private static int _currentPage = 1;

        // last added bookmark
        private static Bookmark _lastAddedBookmark;

        // main application window
        private static MainWindow _mainWin;

        /// <summary>
        /// Resets the bookmark list and all Bookmark chessboards in the Bookmark view.
        /// TODO: this is probably necessary now that we check and rebuild the Bookmark view every
        /// time it gets focus.
        /// </summary>
        public static void ClearBookmarksGui()
        {
            BookmarkList.Clear();

            foreach (BookmarkView bv in BookmarkGuiList)
            {
                bv.Deactivate();
                bv.SetOpacity(0.5);
            }
            _mainWin.UiLblBookmarkPage.Visibility = Visibility.Collapsed;
            _mainWin.UiImgLeftArrow.Visibility = Visibility.Collapsed;
            _mainWin.UiImgRightArrow.Visibility = Visibility.Collapsed;
        }

        private static bool _ignoreSelectionChange = false;

        /// <summary>
        /// Rebuilds the list of bookmarks for the current chapter.
        /// </summary>
        /// <param name="rebuild" if false this method was called in response to a changed selection
        /// in one of the CombpoBoxes so no rebuild is needed.</param>
        public static void BuildBookmarkList(bool rebuild)
        {
            _ignoreSelectionChange = true;

            bool sameWorkbook = WorkbookManager.SessionWorkbook == _currentWorkbook;
            if (!sameWorkbook)
            {
                rebuild = true;
                _selectedChapter = null;
                _selectedChapterIndex = -1;
                _selectedContentType = null;
                _currentWorkbook = WorkbookManager.SessionWorkbook;
            }

            if (rebuild)
            {
                AppState.MainWin.UiComboBoxBmChapters.Items.Clear();
                AppState.MainWin.UiComboBoxBmChapters.Items.Add("*");
                for (int i = 0; i < WorkbookManager.SessionWorkbook.GetChapterCount(); i++)
                {
                    AppState.MainWin.UiComboBoxBmChapters.Items.Add((i + 1).ToString());
                }

                // see if we can select the previously selected chapter
                bool foundSelectedChapter = false;
                if (_selectedChapter != null)
                {
                    for (int i = 0; i < WorkbookManager.SessionWorkbook.GetChapterCount(); i++)
                    {
                        if (WorkbookManager.SessionWorkbook.Chapters[i] == _selectedChapter)
                        {
                            _selectedChapterIndex = i;
                            AppState.MainWin.UiComboBoxBmChapters.SelectedItem = (_selectedChapterIndex + 1).ToString();
                            foundSelectedChapter = true;
                            break;
                        }
                    }
                }

                // if we did not work out the selection above, set it to the first element (which is "*")
                if (!foundSelectedChapter || _selectedChapterIndex < 0)
                {
                    _selectedChapterIndex = -1;
                    AppState.MainWin.UiComboBoxBmChapters.SelectedIndex = 0;
                }

                //_lastAddedBookmark = null;
            }

            if (_selectedContentType == null || _selectedContentType.ContentType == GameData.ContentType.NONE)
            {
                AppState.MainWin.UiComboBoxBmContent.SelectedIndex = 0;
            }
            else
            {
                AppState.MainWin.UiComboBoxBmContent.SelectedItem = _selectedContentType;
            }

            GameData.ContentType contentType = _selectedContentType == null ? GameData.ContentType.NONE : _selectedContentType.ContentType;

            _currentPage = 1;
            BookmarkList.Clear();
            if (_selectedChapterIndex >= 0)
            {
                BuildBookmarkListForChapter(_selectedChapter, contentType);
            }
            else
            {
                foreach (Chapter ch in WorkbookManager.SessionWorkbook.Chapters)
                {
                    BuildBookmarkListForChapter(ch, contentType);
                }
            }

            SortBookmarks();

            HighlightBookmark(_lastAddedBookmark);
            _lastAddedBookmark = null;
            _ignoreSelectionChange = false;
        }

        /// <summary>
        /// Resets the chapter and content type selection.
        /// Should be called when a new Workbook is open or created.
        /// </summary>
        public static void ResetSelections()
        {
            _selectedContentType = null;
            _selectedChapterIndex = -1;

            AppState.MainWin.UiComboBoxBmChapters.SelectedIndex = 0;
            AppState.MainWin.UiComboBoxBmContent.SelectedIndex = 0;
        }

        /// <summary>
        /// The selection has changed so re-build the view.
        /// </summary>
        public static void ComboBoxChaptersSelectionChanged()
        {
            if (_ignoreSelectionChange)
            {
                return;
            }

            bool res = int.TryParse(AppState.MainWin.UiComboBoxBmChapters.SelectedItem as string, out int listBoxSelectedString);
            if (!res)
            {
                _selectedChapterIndex = -1;
                _selectedChapter = null;
                BuildBookmarkList(false);
            }
            else
            {
                // listBoxSelectedString is a string representing the 1-based chapter's position in Chapters list
                // to get the actual index we subtract 1.
                int chapterIndex = listBoxSelectedString - 1;
                // make sure that this is not due to the programmatic change in BuildBookmarkList() so that we don't call it in a loop!
                if (_selectedChapterIndex != chapterIndex)
                {
                    _selectedChapterIndex = chapterIndex;
                    _selectedChapter = WorkbookManager.SessionWorkbook.GetChapterByIndex(_selectedChapterIndex);
                    BuildBookmarkList(false);
                }
            }
        }

        /// <summary>
        /// The selection has changed so re-build the view.
        /// </summary>
        public static void ComboBoxContentSelectionChanged()
        {
            if (_ignoreSelectionChange)
            {
                return;
            }

            ContentTypeListItem li = AppState.MainWin.UiComboBoxBmContent.SelectedItem as ContentTypeListItem;
            if (_selectedContentType != li)
            {
                _selectedContentType = li;
                BuildBookmarkList(false);
            }
        }

        /// <summary>
        /// Sets the Last Added Bookmark object.
        /// It will be used to select the Bookmarks page to open.
        /// </summary>
        /// <param name="bm"></param>
        public static void SetLastAddedBookmark(Bookmark bm)
        {
            _lastAddedBookmark = bm;
        }

        /// <summary>
        /// Resets or recreates all the bookmarks.
        /// Called on app initialization.
        /// </summary>
        public static void InitBookmarksGui(MainWindow mainWin)
        {
            _mainWin = mainWin;

            BookmarkGuiList.Clear();

            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_1, _mainWin.UiImgBookmark_1, _mainWin.UiLblBookmark_1, _mainWin.UiLblChapter_1, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_2, _mainWin.UiImgBookmark_2, _mainWin.UiLblBookmark_2, _mainWin.UiLblChapter_2, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_3, _mainWin.UiImgBookmark_3, _mainWin.UiLblBookmark_3, _mainWin.UiLblChapter_3, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_4, _mainWin.UiImgBookmark_4, _mainWin.UiLblBookmark_4, _mainWin.UiLblChapter_4, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_5, _mainWin.UiImgBookmark_5, _mainWin.UiLblBookmark_5, _mainWin.UiLblChapter_5, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_6, _mainWin.UiImgBookmark_6, _mainWin.UiLblBookmark_6, _mainWin.UiLblChapter_6, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_7, _mainWin.UiImgBookmark_7, _mainWin.UiLblBookmark_7, _mainWin.UiLblChapter_7, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_8, _mainWin.UiImgBookmark_8, _mainWin.UiLblBookmark_8, _mainWin.UiLblChapter_8, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_9, _mainWin.UiImgBookmark_9, _mainWin.UiLblBookmark_9, _mainWin.UiLblChapter_9, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_10, _mainWin.UiImgBookmark_10, _mainWin.UiLblBookmark_10, _mainWin.UiLblChapter_10, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_11, _mainWin.UiImgBookmark_11, _mainWin.UiLblBookmark_11, _mainWin.UiLblChapter_11, false, false)));
            BookmarkGuiList.Add(new BookmarkView(new ChessBoardSmall(_mainWin.UiCnvBookmark_12, _mainWin.UiImgBookmark_12, _mainWin.UiLblBookmark_12, _mainWin.UiLblChapter_12, false, false)));
        }

        /// <summary>
        /// Sorts bookmarks by move number and then color-to-move.
        /// This method should be called after initialization
        /// and any subsequents addition.
        /// </summary>
        public static void SortBookmarks()
        {
            BookmarkList.Sort();

            int lastBmPage = PageForLastAddedBookmark();
            if (lastBmPage > 0)
            {
                _currentPage = lastBmPage;
            }

            ResyncBookmarks(_currentPage);
        }

        /// <summary>
        /// Adds a bookmark to the list of bookmarks.
        /// Sorts the bookmarks and updates the GUI.
        /// </summary>
        /// <returns>newly created bookmark on success or null if operation failed</returns>
        public static Bookmark AddBookmark(VariationTree tree, TreeNode nd, int articleIndex, out bool alreadyExists)
        {
            Bookmark bm = null;
            alreadyExists = false;

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
                else
                {
                    alreadyExists = true;
                }
            }

            return bm;
        }

        /// <summary>
        /// Adds a bookmark to the list of bookmarks.
        /// </summary>
        /// <returns>newly created bookmark or null if the operation failed</returns>
        public static Bookmark AddBookmark(VariationTree tree, int nodeId, int index, out bool alreadyExists)
        {
            alreadyExists = false;

            TreeNode nd = tree.GetNodeFromNodeId(nodeId);
            if (nd != null)
            {
                return AddBookmark(tree, nd, index, out alreadyExists);
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
            if (ClickedIndex < 0 || ClickedIndex >= BookmarkList.Count)
            {
                return;
            }

            try
            {
                BookmarkWrapper bmw = BookmarkGuiList[ClickedIndex % BOOKMARKS_PER_PAGE].BookmarkWrapper;
                DeleteBookmark(bmw);
            }
            catch { }
        }

        /// <summary>
        /// Deletes the bookmark identified by the passed parameters.
        /// </summary>
        /// <param name="chapterIndex"></param>
        /// <param name="articleType"></param>
        /// <param name="articleIndex"></param>
        /// <param name="nodeId"></param>
        public static BookmarkWrapper DeleteBookmark(int chapterIndex, GameData.ContentType articleType, int articleIndex, int nodeId)
        {
            BookmarkWrapper bmw = null;
            foreach (BookmarkWrapper wrapper in BookmarkList)
            {
                if (wrapper.ChapterIndex == chapterIndex && wrapper.ContentType == articleType && wrapper.ArticleIndex == articleIndex && wrapper.Node.NodeId == nodeId)
                {
                    bmw = wrapper;
                }
            }

            DeleteBookmark(bmw);
            return bmw;
        }

        /// <summary>
        /// Removes all bookmarks.
        /// </summary>
        public static void DeleteAllBookmarks(bool askUser = true)
        {
            if (askUser && BookmarkList.Count > 0)
            {
                if (MessageBox.Show(Properties.Resources.ConfirmDeleteAllBookmarks
                    , Properties.Resources.Bookmarks, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
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
                ClickedIndex = bkmNo;

                if (e.ChangedButton == MouseButton.Left)
                {
                    AppState.MainWin.UiMnBmGotoPosition_Click(null, null);
                    e.Handled = true;
                }
                else
                {
                    EnableBookmarkMenus(cm, true);
                }
            }
        }

        /// <summary>
        /// Given the index of the bookmark clicked in the GUI,
        /// set active chapter and article. 
        /// This is to be invoked from outside, once there was a request to 
        /// navigate to the bookmarked position or start training from it.
        /// </summary>
        /// <param name="ClickedIndex"></param>
        public static void SetActiveEntities(bool openTab)
        {
            if (ClickedIndex < 0 || ClickedIndex >= BookmarkList.Count)
            {
                return;
            }

            BookmarkWrapper bmw = BookmarkList[ClickedIndex];
            if (bmw.ChapterIndex < 0)
            {
                return;
            }

            WorkbookManager.SessionWorkbook.SetActiveChapterTreeByIndex(bmw.ChapterIndex, bmw.ContentType, bmw.ArticleIndex);
            SelectedBookmarkNode = bmw.Node;
            if (true)
            {
                if (AppState.ActiveVariationTree != null && SelectedBookmarkNode != null)
                {
                    switch (bmw.ContentType)
                    {
                        case GameData.ContentType.STUDY_TREE:
                            AppState.MainWin.SetupGuiForActiveStudyTree(true);
                            break;
                        case GameData.ContentType.MODEL_GAME:
                            AppState.MainWin.SelectModelGame(bmw.ArticleIndex, true);
                            break;
                        case GameData.ContentType.EXERCISE:
                            AppState.MainWin.SelectExercise(bmw.ArticleIndex, true);
                            if (SelectedBookmarkNode.NodeId != 0 && !AppState.MainWin.ActiveVariationTree.ShowTreeLines)
                            {
                                AppState.MainWin.ActiveVariationTree.ShowTreeLines = true;
                                AppState.MainWin.ActiveTreeView.BuildFlowDocumentForVariationTree(false);
                            }
                            break;
                    }
                    AppState.MainWin.SetActiveLine(SelectedBookmarkNode.LineId, SelectedBookmarkNode.NodeId);
                    AppState.MainWin.ActiveTreeView.HighlightLineAndMove(AppState.MainWin.ActiveTreeView.HostRtb.Document, SelectedBookmarkNode.LineId, SelectedBookmarkNode.NodeId);
                }
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

            // note that menu will not open if there are no bookmarks so we don't have to handle it here
            if (ClickedIndex < 0 || ClickedIndex >= BookmarkList.Count)
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
                        case "UiMnBmGotoPosition":
                            menuItem.IsEnabled = isEnabled;
                            break;
                        case "_mnTrainFromBookmark":
                            TreeNode nd = GetNodeFromClickedIndex(ClickedIndex);
                            if (nd != null && !nd.Position.IsCheckmate && !nd.Position.IsStalemate)
                            {
                                menuItem.IsEnabled = isEnabled;
                            }
                            else
                            {
                                menuItem.IsEnabled = false;
                            }
                            break;
                        case "_mnDeleteBookmark":
                            menuItem.IsEnabled = isEnabled;
                            break;
                        case "_mnDeleteAllBookmarks":
                            menuItem.IsEnabled = isEnabled;
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
                            imgBoard = Configuration.StudyBoardSet.SmallBoard;
                            break;
                        case GameData.ContentType.MODEL_GAME:
                            imgBoard = Configuration.GameBoardSet.SmallBoard;
                            break;
                        case GameData.ContentType.EXERCISE:
                            imgBoard = Configuration.ExerciseBoardSet.SmallBoard;
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
            HighlightBookmark(_lastAddedBookmark);
        }

        /// <summary>
        /// Highlights the passed bookmark,
        /// unhighlights the rest.
        /// </summary>
        /// <param name="bm"></param>
        public static void HighlightBookmark(Bookmark bm)
        {
            foreach (BookmarkView bv in BookmarkGuiList)
            {
                BookmarkWrapper bw = bv.BookmarkWrapper;

                if (bw != null && bw.Bookmark == bm)
                {
                    bv.Highlight(true);
                }
                else
                {
                    bv.Highlight(false);
                }
            }
        }

        /// <summary>
        /// Deletes a bookmark from the parent tree
        /// and the list of bookmarks.
        /// </summary>
        /// <param name="bmw"></param>
        private static void DeleteBookmark(BookmarkWrapper bmw)
        {
            if (bmw == null)
            {
                return;
            }

            try
            {
                TreeNode nd = bmw.Bookmark.Node;
                VariationTree tree = bmw.Tree;
                if (nd != null)
                {
                    tree.DeleteBookmark(nd);
                    BookmarkList.Remove(bmw);
                    if (_currentPage > _maxPage)
                    {
                        _currentPage = _maxPage;
                    }
                    ResyncBookmarks(_currentPage);
                }
                AppState.IsDirty = true;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Returns the TreeNode given its index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private static TreeNode GetNodeFromClickedIndex(int index)
        {
            TreeNode node = null;

            try
            {
                if (index >= 0 && index < BookmarkList.Count)
                {
                    BookmarkWrapper bmv = BookmarkGuiList[ClickedIndex % BOOKMARKS_PER_PAGE].BookmarkWrapper;
                    node = bmv.Bookmark.Node;
                }
            }
            catch { }

            return node;
        }

        /// <summary>
        /// Shows/hides paging controls and updates
        /// the label text as per the current number of 
        /// Bookmarks and currently displayed page.
        /// </summary>
        private static void ShowPageControls()
        {
            string barText = ResourceUtils.GetCounterBarText("Page", _currentPage - 1, _maxPage);
            _mainWin.UiLblBookmarkPage.Content = barText;

            int bm_count = BookmarkList.Count;
            if (bm_count <= BOOKMARKS_PER_PAGE)
            {
                _mainWin.UiLblBookmarkPage.Visibility = bm_count > 0 ? Visibility.Visible : Visibility.Collapsed;
                _mainWin.UiImgLeftArrow.Visibility = Visibility.Collapsed;
                _mainWin.UiImgRightArrow.Visibility = Visibility.Collapsed;
            }
            else
            {
                _mainWin.UiLblBookmarkPage.Visibility = Visibility.Visible;
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

        /// <summary>
        /// Finds the page with the _lastAddedBookmark
        /// </summary>
        /// <returns></returns>
        private static int PageForLastAddedBookmark()
        {
            int pageNo = 0;

            if (_lastAddedBookmark == null)
            {
                return 0;
            }
            else
            {
                for (int i = 0; i < BookmarkList.Count; i++)
                {
                    if (BookmarkList[i].Bookmark == _lastAddedBookmark)
                    {
                        pageNo = GetPageNoFromIndex(i);
                    }
                }
            }

            return pageNo;
        }

        /// <summary>
        /// Builds a list of bookmarks for a single chapter.
        /// </summary>
        /// <param name="chapter"></param>
        private static void BuildBookmarkListForChapter(Chapter chapter, GameData.ContentType contentType)
        {
            int chapterIndex = WorkbookManager.SessionWorkbook.GetChapterIndex(chapter);

            if (contentType == GameData.ContentType.NONE || contentType == GameData.ContentType.STUDY_TREE)
            {
                foreach (Bookmark bkm in chapter.StudyTree.Tree.Bookmarks)
                {
                    BookmarkWrapper bkv = new BookmarkWrapper(chapterIndex, chapter.StudyTree.Tree, bkm, -1);
                    BookmarkList.Add(bkv);
                }
            }

            if (contentType == GameData.ContentType.NONE || contentType == GameData.ContentType.MODEL_GAME)
            {
                for (int i = 0; i < chapter.GetModelGameCount(); i++)
                {
                    Article art = chapter.ModelGames[i];
                    foreach (Bookmark bkm in art.Tree.Bookmarks)
                    {
                        BookmarkWrapper bkv = new BookmarkWrapper(chapterIndex, art.Tree, bkm, i);
                        BookmarkList.Add(bkv);
                    }
                }
            }

            if (contentType == GameData.ContentType.NONE || contentType == GameData.ContentType.EXERCISE)
            {
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
        /// Gets page number for a given index in the Bookmark List.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private static int GetPageNoFromIndex(int index)
        {
            return (index / BOOKMARKS_PER_PAGE) + 1;
        }

    }
}

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
    /// This class mainatins a list of MAX_BOOKMARKS (currently 9) BookmarkView objects 
    /// that contain
    /// references to the relevant GUI controls
    /// and can show position from the Bookmark objects.
    /// </summary>
    internal class BookmarkManager
    {
        /// <summary>
        /// Max number of bookmarks that can be shown in the GUI.
        /// There is no limit on how many bookmarks there can be
        /// altogether.
        /// </summary>
        public static readonly int MAX_BOOKMARKS = 9;

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

        /// <summary>
        /// Resets or recreates all the bookmarks.
        /// Called on app initialization..
        /// </summary>
        public static void InitBookmarksGui()
        {
            MainWindow mw = AppState.MainWin;

            Bookmarks.Clear();

            Bookmarks.Add(new BookmarkView(new ChessBoard(mw._cnvBookmark_1, mw._imgBookmark_1, mw._lblBookmark_1, false)));
            Bookmarks.Add(new BookmarkView(new ChessBoard(mw._cnvBookmark_2, mw._imgBookmark_2, mw._lblBookmark_2, false)));
            Bookmarks.Add(new BookmarkView(new ChessBoard(mw._cnvBookmark_3, mw._imgBookmark_3, mw._lblBookmark_3, false)));
            Bookmarks.Add(new BookmarkView(new ChessBoard(mw._cnvBookmark_4, mw._imgBookmark_4, mw._lblBookmark_4, false)));
            Bookmarks.Add(new BookmarkView(new ChessBoard(mw._cnvBookmark_5, mw._imgBookmark_5, mw._lblBookmark_5, false)));
            Bookmarks.Add(new BookmarkView(new ChessBoard(mw._cnvBookmark_6, mw._imgBookmark_6, mw._lblBookmark_6, false)));
            Bookmarks.Add(new BookmarkView(new ChessBoard(mw._cnvBookmark_7, mw._imgBookmark_7, mw._lblBookmark_7, false)));
            Bookmarks.Add(new BookmarkView(new ChessBoard(mw._cnvBookmark_8, mw._imgBookmark_8, mw._lblBookmark_8, false)));
            Bookmarks.Add(new BookmarkView(new ChessBoard(mw._cnvBookmark_9, mw._imgBookmark_9, mw._lblBookmark_9, false)));
        }

        /// <summary>
        /// Initializes the GUI for the bookmarks.
        /// Only first MAX_BOOKMARKS bookmarks can be shown at most.
        /// </summary>
        internal static void ShowBookmarks()
        {
            for (int i = 0; i < AppState.MainWin.Workbook.Bookmarks.Count; i++)
            {
                if (i >= Bookmarks.Count)
                    break;

                Bookmarks[i].BookmarkData = AppState.MainWin.Workbook.Bookmarks[i];
                Bookmarks[i].Activate();
            }
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
            TreeNode nd = AppState.MainWin.Workbook.GetNodeFromNodeId(nodeId);
            if (nd != null)
            {
                //add to the list in the Workbook
                if (AppState.MainWin.Workbook.AddBookmark(nd, true) == 0)
                {
                    ResyncBookmarks();
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
                AppState.MainWin.Workbook.DeleteBookmark(nd);
                ResyncBookmarks();
            }
            AppState.SaveWorkbookFile();
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
            AppState.SaveWorkbookFile();
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
                if (e.ChangedButton == MouseButton.Left)
                {
                    AppState.MainWin.SetAppInTrainingMode(bkmNo - 1);
                    e.Handled = true;
                }
                // for the benefit of the conetx menu set the clicked index.
                ClickedIndex = bkmNo - 1;
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
            AppState.MainWin.Workbook.GenerateBookmarks();
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
        private static void ResyncBookmarks()
        {
            int count = AppState.MainWin.Workbook.Bookmarks.Count;

            for (int i = 0; i < MAX_BOOKMARKS; i++)
            {
                if (i < count)
                {
                    Bookmarks[i].BookmarkData = AppState.MainWin.Workbook.Bookmarks[i];
                    Bookmarks[i].Activate();
                }
                else
                {
                    Bookmarks[i].BookmarkData = null;
                    Bookmarks[i].Deactivate();
                }
            }
        }

    }
}

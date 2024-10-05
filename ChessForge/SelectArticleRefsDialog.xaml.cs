using GameTree;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SelectArticleRefsDialog.xaml
    /// </summary>
    public partial class SelectArticleRefsDialog : Window
    {
        /// <summary>
        /// A '|' separated list of selected reference GUID.
        /// </summary>
        public string GameExerciseRefs;

        /// <summary>
        /// The list of games to process.
        /// </summary>
        private ObservableCollection<ArticleListItem> _articleList;

        // type of articles handled
        private GameData.ContentType _articleType;

        // Node for which this dialog was invoked.
        private TreeNode _node;

        /// <summary>
        /// The dialog for selecting Articles (games or exercises) from multiple chapters.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="articleType"></param>
        public SelectArticleRefsDialog(TreeNode nd, GameData.ContentType articleType = GameData.ContentType.GENERIC)
        {
            _node = nd;
            _articleType = articleType;
            _articleList = WorkbookManager.SessionWorkbook.GenerateArticleList(null, articleType);

            // if there is any selection outside the active chapter show all chapters (issue #465)
            InitializeComponent();

            Title = Properties.Resources.SelectReferences;

            SelectNodeReferences();
            UiLvGames.ItemsSource = _articleList;

            ArticleListItem itemToBringIntoView = null;
            // ScrollIntoView does not bring the item of interest to the top so
            // apply it to 10 items down
            bool activeChapterFound = false;
            int extraLinesCount = 0;
            for (int i = 0; i < _articleList.Count; i++)
            {
                if (_articleList[i].Chapter == AppState.ActiveChapter)
                {
                    activeChapterFound = true;
                }

                if (activeChapterFound)
                {
                    extraLinesCount++;
                    itemToBringIntoView = _articleList[i];
                }

                if (extraLinesCount >= 11)
                {
                    break;
                }
            }

            if (itemToBringIntoView != null)
            {
                UiLvGames.ScrollIntoView(itemToBringIntoView);
            }
        }

        /// <summary>
        /// Returns a list of selected references.
        /// </summary>
        /// <returns></returns>
        public List<string> GetSelectedReferenceStrings()
        {
            List<string> refs = new List<string>();

            foreach (ArticleListItem item in _articleList)
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
                        foreach (ArticleListItem item in _articleList)
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
        /// OK button was clicked.
        /// Update the list of references and exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            var lstRefs = GetSelectedReferenceStrings();

            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (var item in lstRefs)
            {
                if (!first)
                {
                    sb.Append('|');
                }
                sb.Append(item);
                first = false;
            }

            GameExerciseRefs = sb.ToString();

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
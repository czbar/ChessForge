using System.Text;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SelectChapterRefsDialog.xaml
    /// </summary>
    public partial class SelectChapterRefsDialog : Window
    {
        /// <summary>
        /// A '|' separated list of selected reference GUID.
        /// </summary>
        public string ChapterRefGuids;

        /// <summary>
        /// Creates the dialog.
        /// Performs selections per the passed references string.
        /// </summary>
        /// <param name="chapterRefs"></param>
        public SelectChapterRefsDialog(string chapterRefs)
        {
            InitializeComponent();

            UiLbChapters.ItemsSource = WorkbookManager.SessionWorkbook.Chapters;

            ChapterRefGuids = chapterRefs;
            string[] tokens = chapterRefs.Split('|');

            if (AppState.Workbook != null)
            {
                foreach (string token in tokens)
                {
                    Chapter chapter = AppState.Workbook.GetChapterByGuid(token, out _);
                    UiLbChapters.SelectedItems.Add(chapter);
                }
            }

            UiLbChapters.Focus();
        }

        /// <summary>
        /// The user clicked the Ok button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            // build the reference string
            bool first = true;
            ChapterRefGuids = "";
            foreach (var item in UiLbChapters.SelectedItems)
            {
                if (item is Chapter chapter)
                {
                    if (!first)
                    {
                        sb.Append('|');
                    }
                    sb.Append(chapter.Guid);
                    first = false;

                    ChapterRefGuids = sb.ToString();
                }
            }

            DialogResult = true;
        }

        /// <summary>
        /// The user clicked the Cancel button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

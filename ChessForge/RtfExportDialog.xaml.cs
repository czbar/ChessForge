using System.Windows;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for RtfExportDialog.xaml
    /// </summary>
    public partial class RtfExportDialog : Window
    {
        /// <summary>
        /// Scope of export selected by the user.
        /// </summary>
        public static PrintScope Scope = PrintScope.ARTICLE;

        /// <summary>
        /// Chapter to print if scope is chapter.
        /// </summary>
        public Chapter Chapter;

        /// <summary>
        /// Article to print, if scope is Article.
        /// </summary>
        public Article Article;

        /// <summary>
        /// Initializes the data.
        /// </summary>
        public RtfExportDialog()
        {
            InitializeComponent();

            switch (Scope)
            {
                case PrintScope.ARTICLE:
                    UiRbCurrentItem.IsChecked = true;
                    break;
                case PrintScope.CHAPTER:
                    UiRbCurrentChapter.IsChecked = true;
                    break;
                case PrintScope.WORKBOOK:
                    UiRbWorkbook.IsChecked = true;
                    break;
            }
        }

        /// <summary>
        /// Proceed with the export as configured.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            Chapter = AppState.ActiveChapter;
            Article = null;

            if (UiRbCurrentItem.IsChecked == true)
            {
                Scope = PrintScope.ARTICLE;
                if (Chapter != null)
                {
                    Article = Chapter.ActiveArticle;
                }
            }
            else if (UiRbCurrentChapter.IsChecked == true)
            {
                Scope = PrintScope.CHAPTER;
            }
            else if (UiRbWorkbook.IsChecked == true)
            {
                Scope = PrintScope.WORKBOOK;
            }
            
            DialogResult = true; 
        }

    }
}

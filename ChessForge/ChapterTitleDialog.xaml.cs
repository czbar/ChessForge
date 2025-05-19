using System.Windows;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for ChapterTitleDialog.xaml
    /// </summary>
    public partial class ChapterTitleDialog : Window
    {
        /// <summary>
        /// Title of the chapter as edited in this dialog.
        /// </summary>
        public string ChapterTitle { get; set; }

        /// <summary>
        /// Author of the chapter.
        /// </summary>
        public string Author{ get; set; }

        /// <summary>
        /// Preamble of the Study.
        /// </summary>
        public string Preamble { get; set; }

        /// <summary>
        /// True if the Preamble was changed by the user in this dialog.
        /// </summary>
        public bool PreambleChanged { get; set; } = false;

        /// <summary>
        /// The chapter whose title is being edited.
        /// </summary>
        private Chapter _chapter;

        /// <summary>
        /// Constructor.
        /// Sets the text to the current title of the chapter.
        /// </summary>
        /// <param name="chapter"></param>
        public ChapterTitleDialog(Chapter chapter)
        {
            InitializeComponent();
            _chapter = chapter;

            UiTbChapterTitle.Text = _chapter.GetTitle();
            UiTbChapterTitle.Focus();
            UiTbChapterTitle.SelectAll();

            UiTbAuthor.Text = _chapter.GetAuthor();
            UiTbPreamble.Text = _chapter.StudyTree.Tree.Header.BuildPreambleText();
        }

        /// <summary>
        /// Check if the user pressed key combination to enter a figurine.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbPreamble_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (GuiUtilities.InsertFigurine(UiTbPreamble, sender, e))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Set the title property and Exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOK_Click(object sender, RoutedEventArgs e)
        {
            ChapterTitle = UiTbChapterTitle.Text;
            Author = UiTbAuthor.Text;

            if (Preamble != UiTbPreamble.Text)
            {
                PreambleChanged = true;
                Preamble = UiTbPreamble.Text;
            }

            _chapter.StudyTree.Tree.Header.SetPreamble(UiTbPreamble.Text);

            DialogResult = true;
        }

        /// <summary>
        /// Exit without setting the Title property.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Check if the user pressed key combination to enter a figurine.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbChapterTitle_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (GuiUtilities.InsertFigurine(UiTbChapterTitle, sender, e))
            {
                e.Handled = true;
            }
        }
    }
}

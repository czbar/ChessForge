using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for RegenerateStudyDialog.xaml
    /// </summary>
    public partial class RegenerateStudyDialog : Window
    {
        /// <summary>
        /// Whether the Sort command should be applied to all chapters.
        /// </summary>
        public bool ApplyToAllChapters = false;

        /// <summary>
        /// Constructors. Initializes the list box values.
        /// </summary>
        public RegenerateStudyDialog(Chapter chapter)
        {
            InitializeComponent();

            UiLabelChapterTitle.Content = Properties.Resources.Chapter + ": " + chapter.GetTitle();

            if (Configuration.AutogenTreeDepth == 0)
            {
                UiTbLastTreeMoveNo.Text = "";
            }
            else
            {
                UiTbLastTreeMoveNo.Text = Configuration.AutogenTreeDepth.ToString();
            }

            UiTbLastTreeMoveNo.Focus();
            UiTbLastTreeMoveNo.SelectAll();
        }

        /// <summary>
        /// Collect the states and exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            uint treeDepth;
            if (!uint.TryParse(UiTbLastTreeMoveNo.Text, out treeDepth))
            {
                treeDepth = 0;
            }
            Configuration.AutogenTreeDepth = treeDepth;

            ApplyToAllChapters = UiCbAllChapters.IsChecked == true;

            DialogResult = true;
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Regenerating-Study-from-Games");
        }
    }

}

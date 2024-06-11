using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for ExerciseSolutionsDialog.xaml
    /// </summary>
    public partial class ExerciseSolutionsDialog : Window
    {
        /// <summary>
        /// In Exercises: whether to show solutions on open
        /// </summary>
        public bool ShowSolutionOnOpen;

        /// <summary>
        /// Whether the Sort command should be applied to all chapters.
        /// </summary>
        public bool ApplyToAllChapters = false;

        /// <summary>
        /// Constructors. Initializes the list box values.
        /// </summary>
        public ExerciseSolutionsDialog(Chapter chapter)
        {
            InitializeComponent();

            ShowSolutionOnOpen = chapter.ShowSolutionsOnOpen;
            UiCbShowSolutions.IsChecked = ShowSolutionOnOpen;

            UiLabelChapterTitle.Content = Properties.Resources.Chapter + ": " + chapter.GetTitle();
        }

        /// <summary>
        /// Collect the states and exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            ShowSolutionOnOpen = UiCbShowSolutions.IsChecked == true;
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
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Managing-Chapter");
        }
    }
}

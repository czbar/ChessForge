using ChessPosition;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for SortGamesDialog.xaml
    /// </summary>
    public partial class SortGamesDialog : Window
    {
        /// <summary>
        /// Selected Sort Criterion
        /// </summary>
        public GameSortCriterion.SortItem SortGamesBy = GameSortCriterion.SortItem.NONE;

        /// <summary>
        /// Selected sort direction.
        /// </summary>
        public GameSortCriterion.SortItem SortGamesDirection = GameSortCriterion.SortItem.ASCENDING;

        /// <summary>
        /// Whether the Sort command should be applied to all chapters.
        /// </summary>
        public bool ApplyToAllChapters = false;

        /// <summary>
        /// Constructors. Initializes the list box values.
        /// </summary>
        public SortGamesDialog(Chapter chapter)
        {
            InitializeComponent();


            UiLabelChapterTitle.Content = Properties.Resources.Chapter + ": " + chapter.GetTitle();

            UiComboBoxSortBy.Items.Add(new GameSortCriterion(GameSortCriterion.SortItem.NONE, "-"));
            UiComboBoxSortBy.Items.Add(new GameSortCriterion(GameSortCriterion.SortItem.DATE, Properties.Resources.SortByDate));
            UiComboBoxSortBy.Items.Add(new GameSortCriterion(GameSortCriterion.SortItem.ROUND, Properties.Resources.SortByRound));
            UiComboBoxSortBy.Items.Add(new GameSortCriterion(GameSortCriterion.SortItem.ECO, Properties.Resources.SortByEco));
            UiComboBoxSortBy.Items.Add(new GameSortCriterion(GameSortCriterion.SortItem.WHITE_NAME, Properties.Resources.SortByWhiteName));
            UiComboBoxSortBy.Items.Add(new GameSortCriterion(GameSortCriterion.SortItem.BLACK_NAME, Properties.Resources.SortByBlackName));
            UiComboBoxSortBy.SelectedIndex = 0;

            UiComboBoxSortDirection.Items.Add(new GameSortCriterion(GameSortCriterion.SortItem.ASCENDING, Properties.Resources.SortAsc));
            UiComboBoxSortDirection.Items.Add(new GameSortCriterion(GameSortCriterion.SortItem.DESCENDING, Properties.Resources.SortDesc));
            UiComboBoxSortDirection.SelectedIndex = 0;
        }

        /// <summary>
        /// If there is no valid selection, disable the Sort Direction box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiComboBoxSortBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GameSortCriterion crit = UiComboBoxSortBy.SelectedItem as GameSortCriterion;
            if (crit == null || crit.ItemId == GameSortCriterion.SortItem.NONE)
            {
                UiComboBoxSortDirection.IsEnabled = false;
            }
            else
            {
                UiComboBoxSortDirection.IsEnabled = true;
            }
        }

        /// <summary>
        /// Collect the states and exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            GameSortCriterion crit = UiComboBoxSortBy.SelectedItem as GameSortCriterion;
            if (crit != null && crit.ItemId != GameSortCriterion.SortItem.NONE)
            {
                SortGamesBy = crit.ItemId;
                GameSortCriterion direction = UiComboBoxSortDirection.SelectedItem as GameSortCriterion;
                if (direction == null || direction.ItemId == GameSortCriterion.SortItem.ASCENDING)
                {
                    SortGamesDirection = GameSortCriterion.SortItem.ASCENDING;
                }
                else
                {
                    SortGamesDirection = GameSortCriterion.SortItem.DESCENDING;
                }
            }

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

    /// <summary>
    /// A class for a game selection criterion.
    /// </summary>
    public class GameSortCriterionEx
    {
        /// <summary>
        /// Creates an object.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public GameSortCriterionEx(SortItem id, string name)
        {
            ItemId = id;
            Name = name;
        }

        /// <summary>
        /// Sort related items for the combo boxes.
        /// Both, for sort criteria and sort direction.
        /// </summary>
        public enum SortItem
        {
            NONE,
            ECO,
            WHITE_NAME,
            BLACK_NAME,
            DATE,
            ROUND,

            ASCENDING,
            DESCENDING
        }

        /// <summary>
        /// Name of the criterion to show
        /// </summary>
        public string Name;

        /// <summary>
        /// Id of the criterion
        /// </summary>
        public SortItem ItemId;

        /// <summary>
        /// Name to show in the selection ListBox.
        /// </summary>
        /// <returns></returns>
        override public string ToString()
        {
            return Name;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for ManageChapterDialog.xaml
    /// </summary>
    public partial class ManageChapterDialog : Window
    {
        /// <summary>
        /// State of the Regenerate Study check box
        /// </summary>
        public bool RegenerateStudy = false;

        /// <summary>
        /// Selected Sort Criterion
        /// </summary>
        public GameSortCriterion.SortItem SortGamesBy = GameSortCriterion.SortItem.NONE;

        /// <summary>
        /// Selected sort direction.
        /// </summary>
        public GameSortCriterion.SortItem SortGamesDirection = GameSortCriterion.SortItem.ASCENDING;

        /// <summary>
        /// Constructors. Initializes the list box values.
        /// </summary>
        public ManageChapterDialog()
        {
            InitializeComponent();

            UiCbGenerateStudyTree.IsChecked = false;
            UiCbGenerateStudyTree_Unchecked(null, null);

            UiComboBoxSortBy.Items.Add(new GameSortCriterion(GameSortCriterion.SortItem.NONE, "-"));
            UiComboBoxSortBy.Items.Add(new GameSortCriterion(GameSortCriterion.SortItem.DATE, Properties.Resources.SortByDate));
            UiComboBoxSortBy.Items.Add(new GameSortCriterion(GameSortCriterion.SortItem.ECO, Properties.Resources.SortByEco));
            UiComboBoxSortBy.Items.Add(new GameSortCriterion(GameSortCriterion.SortItem.WHITE_NAME, Properties.Resources.SortByWhiteName));
            UiComboBoxSortBy.Items.Add(new GameSortCriterion(GameSortCriterion.SortItem.BLACK_NAME, Properties.Resources.SortByBlackName));
            UiComboBoxSortBy.SelectedIndex = 0;

            UiComboBoxSortDirection.Items.Add(new GameSortCriterion(GameSortCriterion.SortItem.ASCENDING, Properties.Resources.SortAsc));
            UiComboBoxSortDirection.Items.Add(new GameSortCriterion(GameSortCriterion.SortItem.DESCENDING, Properties.Resources.SortDesc));
            UiComboBoxSortDirection.SelectedIndex = 0;


            if (Configuration.AutogenTreeDepth == 0)
            {
                UiTbLastTreeMoveNo.Text = "";
            }
            else
            {
                UiTbLastTreeMoveNo.Text = Configuration.AutogenTreeDepth.ToString();
            }
        }

        /// <summary>
        /// Enables the tree trim level when Generate Tree box is checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbGenerateStudyTree_Checked(object sender, RoutedEventArgs e)
        {
            UiTbLastTreeMoveNo.IsEnabled = true;
        }

        /// <summary>
        /// Disables the tree trim level when Generate Tree box is unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbGenerateStudyTree_Unchecked(object sender, RoutedEventArgs e)
        {
            UiTbLastTreeMoveNo.IsEnabled = false;
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
            RegenerateStudy = UiCbGenerateStudyTree.IsChecked == true;
            if (RegenerateStudy)
            {
                uint treeDepth;
                if (!uint.TryParse(UiTbLastTreeMoveNo.Text, out treeDepth))
                {
                    treeDepth = 0;
                }
                Configuration.AutogenTreeDepth = treeDepth;
            }

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

            DialogResult = true;
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Manage-Chapter-Dialog");
        }
    }

    /// <summary>
    /// A class for a game selection criterion.
    /// </summary>
    public class GameSortCriterion
    {
        /// <summary>
        /// Creates an object.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public GameSortCriterion(SortItem id, string name)
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

using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

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
        /// In Exercises: whether to show solutions on open
        /// </summary>
        public bool ShowSolutionOnOpen;

        /// <summary>
        /// Selected Sort Criterion
        /// </summary>
        public GameSortCriterion.SortItem SortGamesBy = GameSortCriterion.SortItem.NONE;

        /// <summary>
        /// Selected sort direction.
        /// </summary>
        public GameSortCriterion.SortItem SortGamesDirection = GameSortCriterion.SortItem.ASCENDING;

        /// <summary>
        /// Number of the move at which to place a thumbnail 
        /// </summary>
        public int ThumbnailMove = -1;

        /// <summary>
        /// Whether to overwrite existing thumbnails
        /// </summary>
        public bool OverwriteThumbnails = false;

        /// <summary>
        /// Whether the thumbnail should be placed at the White's or Black's move.
        /// </summary>
        public PieceColor ThumbnailMoveColor = PieceColor.White;

        /// <summary>
        /// Whether the Sort command should be applied to all chapters.
        /// </summary>
        public bool ApplyToAllChapters = false;

        /// <summary>
        /// Constructors. Initializes the list box values.
        /// </summary>
        public ManageChapterDialog(Chapter chapter)
        {
            InitializeComponent();

            ShowSolutionOnOpen = chapter.ShowAllSolutions;
            UiCbShowSolutions.IsChecked = ShowSolutionOnOpen;

            UiLabelChapterTitle.Content = Properties.Resources.Chapter + ": " + chapter.GetTitle();

            UiCbGenerateStudyTree.IsChecked = false;
            UiCbGenerateStudyTree_Unchecked(null, null);

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

            UiTbThumbMove.Text = "";
            EnableThumbnailControls(false);

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

            ShowSolutionOnOpen = UiCbShowSolutions.IsChecked == true;   

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

            ThumbnailMove = -1;
            if (int.TryParse(UiTbThumbMove.Text, out int moveNo))
            {
                if (moveNo > 0)
                {
                    ThumbnailMove = moveNo;
                    ThumbnailMoveColor = UiRbBlack.IsChecked == true ? PieceColor.Black : PieceColor.White;
                    OverwriteThumbnails = UiCbOverwriteThumb.IsChecked == true;
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

        /// <summary>
        /// Text changed in the thumbnail move number box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiTbThumbMove_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool enable = false;

            if (int.TryParse(UiTbThumbMove.Text, out int moveNo))
            {
                if (moveNo > 0)
                {
                    enable = true;
                }
            }
            EnableThumbnailControls(enable);
        }

        /// <summary>
        /// Enable/disable thumbnail group box controls.
        /// </summary>
        /// <param name="enable"></param>
        private void EnableThumbnailControls(bool enable)
        {
            UiRbWhite.IsEnabled = enable;
            UiRbBlack.IsEnabled = enable;
            UiCbOverwriteThumb.IsEnabled = enable;
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

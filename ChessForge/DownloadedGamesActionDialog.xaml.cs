using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for DownloadedGamesActionDialog.xaml
    /// </summary>
    public partial class DownloadedGamesActionDialog : Window
    {
        /// <summary>
        /// Types of Save actions that can be selected
        /// </summary>
        public enum Action
        {
            None,
            CurrentChapter,
            NewChapter,
            NewWorkbook
        }

        /// <summary>
        /// Selected Save Action
        /// </summary>
        public Action SaveOption = Action.None;

        /// <summary>
        /// Whether the user chose to build repertoire chapters
        /// </summary>
        public bool BuildRepertoireChapters;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DownloadedGamesActionDialog(int gamesCount)
        {
            InitializeComponent();

            UiGbOptions.Header = Properties.Resources.NumberOfGames + ": " + gamesCount.ToString();
            UiRbRepertoireChapters.IsChecked = true;

            if (WorkbookManager.SessionWorkbook == null)
            {
                UiCbCreateNewWorkbook.IsChecked = true;
                UiCbCreateNewWorkbook.IsEnabled = false;

                UiRbAppendCurrentChapter.IsEnabled = false;
            }
            else
            {
                UiCbCreateNewWorkbook.IsChecked = false;
            }

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
        /// Sets SaveOption end exists successfully
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (UiCbCreateNewWorkbook.IsChecked == true)
            {
                SaveOption = Action.NewWorkbook;
            }
            else if (UiRbAppendCurrentChapter.IsChecked == true)
            {
                SaveOption = Action.CurrentChapter;
            }
            else if (UiRbCreateNewChapter.IsChecked == true || UiRbRepertoireChapters.IsChecked == true)
            {
                SaveOption = Action.NewChapter;
            }

            BuildRepertoireChapters = UiRbRepertoireChapters.IsChecked == true;
            if (BuildRepertoireChapters)
            {
                uint treeDepth;
                if (!uint.TryParse(UiTbLastTreeMoveNo.Text, out treeDepth))
                {
                    treeDepth = 0;
                }
                Configuration.AutogenTreeDepth = treeDepth;
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
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Importing-or-Downloading-Games#downloading-games-of-a-player-from-chesscom-or-lichess");
        }

        /// <summary>
        /// The Create New Workbook box was checked.
        /// Ensure the Append to Current Chapter option is not enabled as it makes no sense in this case.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbCreateNewWorkbook_Checked(object sender, RoutedEventArgs e)
        {
            UiRbAppendCurrentChapter.IsEnabled = false;

            if (UiRbAppendCurrentChapter.IsChecked == true)
            {
                UiRbCreateNewChapter.IsChecked = true;
            }
        }

        /// <summary>
        /// The Create New Workbook box was unchecked.
        /// The Append to Current Chapter option can be enabled now.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbCreateNewWorkbook_Unchecked(object sender, RoutedEventArgs e)
        {
            UiRbAppendCurrentChapter.IsEnabled = true;
        }

        /// <summary>
        /// Repertoire radio button was checked.
        /// Enable the "last move" controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbRepertoireChapters_Checked(object sender, RoutedEventArgs e)
        {
            UilblLastTreeMoveNo.IsEnabled = true;
            UiTbLastTreeMoveNo.IsEnabled = true;
        }

        /// <summary>
        /// Repertoire radio button was unchecked.
        /// Disable the "last move" controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbRepertoireChapters_Unchecked(object sender, RoutedEventArgs e)
        {
            UilblLastTreeMoveNo.IsEnabled = false;
            UiTbLastTreeMoveNo.IsEnabled = false;
        }
    }
}

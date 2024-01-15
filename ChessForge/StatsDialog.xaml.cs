using ChessPosition;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for StatsDialog.xaml
    /// </summary>
    public partial class StatsDialog : Window
    {
        // total stats supplied by the caller
        private Dictionary<string, PlayerAggregatedStats> _dictPlayersStats;

        // scope: Workbook or Active Chapter
        private OperationScope _operationScope;

        // chapter/workbook statistics
        private StatsData _chapterStats;

        // list of plyares
        private List<string> _listPlayers;

        // index of the item that contains a separator between "every game" players and the rest
        private int _invalidIndex = -1;

        // list of strings to show in the GUI
        private List<string> _listBoxForGui;

        /// <summary>
        /// Creates the dialog, computes the necessary lists and populates
        /// the controls.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="chapter"></param>
        /// <param name="chapterStats"></param>
        /// <param name="dictPlayersStats"></param>
        public StatsDialog(OperationScope scope, Chapter chapter, StatsData chapterStats, Dictionary<string, PlayerAggregatedStats> dictPlayersStats)
        {
            _dictPlayersStats = dictPlayersStats;
            _chapterStats = chapterStats;
            _operationScope = scope;

            InitializeComponent();

            UiLblChaptersCount.Visibility = Visibility.Visible;

            if (scope == OperationScope.WORKBOOK)
            {
                UiGbChapterStats.Header = Properties.Resources.Workbook;
            }
            else if (scope == OperationScope.CHAPTER)
            {
                UiGbChapterStats.Header = Properties.Resources.Chapter;
                UiLblChapters.Content = chapter != null ? (Properties.Resources.Title + ": " + chapter.Title) : "-";
                UiLblChaptersCount.Visibility = Visibility.Hidden;
            }
            else
            {
                UiGbChapterStats.Content = "";
            }

            PopulateDataFields();
            int playerCount = PopulateListOfPlayers();
            EnablePlayerStats(playerCount > 0);

            if (playerCount > 0)
            {
                UiLbPlayers.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Populates the chapter stats data fields.
        /// </summary>
        private void PopulateDataFields()
        {
            if (_operationScope == OperationScope.WORKBOOK)
            {
                UiLblChaptersCount.Content = _chapterStats.ChapterCount;
            }

            UiLblGamesCount.Content = _chapterStats.GameCount;
            UiLblWhiteWinsCount.Content = _chapterStats.WhiteWins;
            UiLblBlackWinsCount.Content = _chapterStats.BlackWins;
            UiLblDrawsCount.Content = _chapterStats.Draws;

            UiLblExercisesCount.Content = _chapterStats.ExerciseCount;
        }

        /// <summary>
        /// Builds the list of players the will be bound to the dialog's ListBox.
        /// It will show "every game players" first followed by the alphabetic list
        /// of al players.
        /// The _listPlayers and _listBoxForGui lists represent the same players, in the same order.
        /// The former contains just the names, the latter adds extra info (number of games played)
        /// </summary>
        private int PopulateListOfPlayers()
        {
            _listPlayers = new List<string>();

            foreach (string player in _dictPlayersStats.Keys)
            {
                if (!string.IsNullOrEmpty(player))
                {
                    _listPlayers.Add(player);
                }
            }
            _listPlayers.Sort();

            int playerCount = _listPlayers.Count;

            // Identify "every game players"
            List<string> everyGamePlayers = new List<string>();
            foreach (string player in _dictPlayersStats.Keys)
            {
                if (_dictPlayersStats[player].TotalStats.GameCount >= _chapterStats.GameCount / 2)
                {
                    everyGamePlayers.Insert(0, player);
                }
            }

            // if the only players are "every game players", ignore them,
            // otherwise insert them at the front followed by a dash line.
            if (everyGamePlayers.Count != _listPlayers.Count && everyGamePlayers.Count > 0)
            {
                foreach (string player in everyGamePlayers)
                {
                    _listPlayers.Insert(0, player);
                }
                _invalidIndex = everyGamePlayers.Count;
                _listPlayers.Insert(_invalidIndex, null);
            }

            _listBoxForGui = new List<string>();
            foreach (string player in _listPlayers)
            {
                _listBoxForGui.Add(PlayerNameForList(player));
            }

            UiLbPlayers.ItemsSource = _listBoxForGui;

            return playerCount;
        }

        /// <summary>
        /// Builds the player's entry in the List Box.
        /// It consists of the player's name followed by 
        /// the number of games the player played.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private string PlayerNameForList(string player)
        {
            return player == null ? "--------" : player + " (" + _dictPlayersStats[player].TotalStats.GameCount + ")";
        }

        /// <summary>
        /// A player has been selected in the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiLbPlayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = UiLbPlayers.SelectedIndex;

            if (index >= 0 && index != _invalidIndex)
            {
                EnablePlayerStats(true);
                PlayerAggregatedStats aggregStats = _dictPlayersStats[_listPlayers[index]];
                ShowPlayerStats(aggregStats);
            }
            else
            {
                EnablePlayerStats(false);
            }
        }

        /// <summary>
        /// Displays stats for a player.
        /// </summary>
        /// <param name="aggregStats"></param>
        private void ShowPlayerStats(PlayerAggregatedStats aggregStats)
        {
            UiLblOverallCount.Content = BuildPlayerStatsString(aggregStats.TotalStats);
            UiLblWhiteCount.Content = BuildPlayerStatsString(aggregStats.WhiteStats);
            UiLblBlackCount.Content = BuildPlayerStatsString(aggregStats.BlackStats);
        }

        /// <summary>
        /// Builds a results string for a player.
        /// </summary>
        /// <param name="stats"></param>
        /// <returns></returns>
        private string BuildPlayerStatsString(PlayerStats stats)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("+" + stats.Wins.ToString() + " -" + stats.Losses.ToString() + " =" + stats.Draws.ToString() + "  (" + stats.GameCount.ToString() + ")");
            return sb.ToString();
        }

        /// <summary>
        /// Shows/hides player stats controls 
        /// </summary>
        /// <param name="enable"></param>
        private void EnablePlayerStats(bool enable)
        {
            UiLblOverall.Visibility = enable ? Visibility.Visible : Visibility.Hidden;
            UiLblOverallCount.Visibility = enable ? Visibility.Visible : Visibility.Hidden;

            UiLblWhite.Visibility = enable ? Visibility.Visible : Visibility.Hidden;
            UiLblWhiteCount.Visibility = enable ? Visibility.Visible : Visibility.Hidden;

            UiLblBlack.Visibility = enable ? Visibility.Visible : Visibility.Hidden;
            UiLblBlackCount.Visibility = enable ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>
        /// Links to the relevant Wiki page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/czbar/ChessForge/wiki/Statistics");
        }
    }
}

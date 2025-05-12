using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using WebAccess;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for ImportWebGamesDialog.xaml
    /// </summary>
    public partial class DownloadWebGamesDialog : Window
    {

        /// <summary>
        /// The list of downloaded games 
        /// </summary>
        public ObservableCollection<GameData> Games = new ObservableCollection<GameData>();

        /// <summary>
        /// Nickname of the user for who the games were downloaded.
        /// </summary>
        public string UserNick;

        /// <summary>
        /// Constructor. Sets up event handler.
        /// </summary>
        public DownloadWebGamesDialog()
        {
            InitializeComponent();

            LichessUserGames.UserGamesReceived += UserGamesReceived;
            ChesscomUserGames.UserGamesReceived += UserGamesReceived;

            EnableControls(false);

            SetControlValues();
            AdjustDates(true);

            UiCmbSite.Items.Add(Constants.LichessNameId);
            UiCmbSite.Items.Add(Constants.ChesscomNameId);

            UiCmbSite.SelectedItem = Configuration.WebGamesSite;
            if (UiCmbSite.SelectedItem == null)
            {
                UiCmbSite.SelectedItem = Constants.LichessNameId;
            }

            SetUserName();
        }

        /// <summary>
        /// Invoked when the games download has finished.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UserGamesReceived(object sender, WebAccessEventArgs e)
        {
            bool exit = false;

            try
            {
                if (e.Success)
                {
                    if (string.IsNullOrEmpty(e.TextData))
                    {
                        MessageBox.Show(Properties.Resources.ErrGamesNotFound, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        if (e.TextData.IndexOf("DOCTYPE") > 0 && e.TextData.IndexOf("DOCTYPE") < 10)
                        {
                            throw new Exception(Properties.Resources.ErrGameNotFound);
                        }

                        var lstGames = e.GameData;

                        // sort games from earliest to latest
                        lstGames = GameUtils.SortGamesByDateTime(lstGames);
                        lstGames = GameUtils.RemoveGamesOutOfDateRange(lstGames, e.GamesFilter.StartDateEpochTicks, e.GamesFilter.EndDateEpochTicks);

                        int gamesCount = lstGames.Count;
                        if (gamesCount >= e.GamesFilter.MaxGames && e.GamesFilter.MaxGames != 0)
                        {
                            if (e.GamesFilter.StartDate.HasValue)
                            {
                                lstGames.RemoveRange(e.GamesFilter.MaxGames, lstGames.Count - e.GamesFilter.MaxGames);
                            }
                            else
                            {
                                lstGames.RemoveRange(0, gamesCount - e.GamesFilter.MaxGames);
                            }
                        }

                        UiLblLoading.Visibility = Visibility.Collapsed;
                        // Set game's web site id
                        for (int i = 0; i < lstGames.Count; i++)
                        {
                            SetGameWebId(lstGames[i]);
                        }

                        Games = new ObservableCollection<GameData>(lstGames);
                        if (Games.Count > 0)
                        {
                            if (SelectGames(ref Games))
                            {
                                UserNick = UiTbUserName.Text;
                                DialogResult = true;
                                exit = true;
                            }
                        }
                        else
                        {
                            MessageBox.Show(Properties.Resources.NoGamesFound, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                else
                {
                    MessageBox.Show(Properties.Resources.GameDownloadError + ": " + e.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Properties.Resources.GameDownloadError + ": " + ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (!exit)
            {
                EnableControls(false);
            }
        }

        /// <summary>
        /// Checks for the GameId from Lichess or chess.com.
        /// If from lichess, then the id will be found in the [Site] header.
        /// If from chess.com, the id will be found in the [Link] header. 
        /// However, chess.com also has a [Site] header so we need to check it first.
        /// </summary>
        private void SetGameWebId(GameData game)
        {
            string val = game.Header.GetValueForKey(PgnHeaders.KEY_LINK);
            if (!string.IsNullOrEmpty(val))
            {
                // double check that this from chess.com
                if (val.Contains("chess.com"))
                {
                    string[] tokens = val.Split('/');
                    game.Header.SetHeaderValue(PgnHeaders.KEY_CHESSCOM_ID, tokens[tokens.Length - 1]);
                }
            }
            else
            {
                val = game.Header.GetValueForKey(PgnHeaders.KEY_SITE);
                if (!string.IsNullOrEmpty(val))
                {
                    // double check that this from lichess
                    if (val.Contains("lichess"))
                    {
                        string[] tokens = val.Split('/');
                        game.Header.SetHeaderValue(PgnHeaders.KEY_LICHESS_ID, tokens[tokens.Length - 1]);
                    }
                }
            }
        }

        /// <summary>
        /// Set user name per currently selected web site.
        /// </summary>
        private void SetUserName()
        {
            if ((string)UiCmbSite.SelectedItem == Constants.ChesscomNameId)
            {
                UiTbUserName.Text = Configuration.WebGamesChesscomUser;
            }
            else
            {
                UiTbUserName.Text = Configuration.WebGamesLichessUser;
            }
        }

        /// <summary>
        /// Set values in the controls per configuration items.
        /// </summary>
        private void SetControlValues()
        {
            if (Configuration.WebGamesMaxCount <= 0)
            {
                UiTbMaxGames.Text = "";
            }
            else
            {
                UiTbMaxGames.Text = Configuration.WebGamesMaxCount.ToString();
            }

            EnableDateControls(!Configuration.WebGamesMostRecent);
            UiCbOnlyNew.IsChecked = Configuration.WebGamesMostRecent;

            SetDatesValues();
        }

        /// <summary>
        /// Set the values for the dates controls per the configuration.
        /// </summary>
        private void SetDatesValues()
        {
            UiDtStartDate.SelectedDate = Configuration.WebGamesStartDate;
            UiDtEndDate.SelectedDate = Configuration.WebGamesEndDate;
            UiCbUtc.IsChecked = Configuration.WebGamesDatesUtc == 1;
        }

        /// <summary>
        /// Resets the dates so that the start date is the previous last date
        /// and the last date is today.
        /// </summary>
        private void ResetDates()
        {
            UiDtStartDate.SelectedDate = UiDtEndDate.SelectedDate;
            UiDtEndDate.SelectedDate = DateTime.Now;
            AdjustDates(true);
        }

        /// <summary>
        /// Invokes the dialog to select games for import.
        /// </summary>
        /// <param name="games"></param>
        /// <returns></returns>
        private bool SelectGames(ref ObservableCollection<GameData> games)
        {
            for (int i = 0; i < games.Count; i++)
            {
                games[i].OrderNo = (i + 1).ToString();
            }

            SelectGamesDialog dlg = new SelectGamesDialog(ref games, SelectGamesDialog.Mode.DOWNLOAD_WEB_GAMES);
            GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);
            return dlg.ShowDialog() == true;
        }

        /// <summary>
        /// Enables/disables controls depending on whether there is a download in progress.
        /// </summary>
        /// <param name="isDownloading"></param>
        private void EnableControls(bool isDownloading)
        {
            UiLblLoading.Visibility = isDownloading ? Visibility.Visible : Visibility.Collapsed;
            if (isDownloading)
            {
                if (IsChesscomDownload())
                {
                    UiLblLoading.Content = Properties.Resources.DownloadingFromChesscom;
                }
                else
                {
                    UiLblLoading.Content = Properties.Resources.DownloadingFromLichess;
                }
            }

            UiBtnDownload.IsEnabled = !isDownloading;
            UiCbOnlyNew.IsEnabled = !isDownloading;
            UiCmbSite.IsEnabled = !isDownloading;
            UiTbMaxGames.IsEnabled = !isDownloading;
            UiTbUserName.IsEnabled = !isDownloading;

            UiDtStartDate.IsEnabled = !isDownloading && !UiCbOnlyNew.IsChecked == true;
            UiDtEndDate.IsEnabled = !isDownloading && !UiCbOnlyNew.IsChecked == true;
            UiBtnResetDates.IsEnabled = !isDownloading && !UiCbOnlyNew.IsChecked == true;
            UiCbUtc.IsEnabled = !isDownloading && !UiCbOnlyNew.IsChecked == true;
        }

        /// <summary>
        /// The user clicked the button requesting the download.
        /// This method kicks off the process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(UiTbUserName.Text))
            {
                MessageBox.Show(Properties.Resources.ErrEmptyUserName, Properties.Resources.PromptCorrectData, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                UpdateConfiguration();

                EnableControls(true);
                GamesFilter filter = new GamesFilter();
                filter.User = UiTbUserName.Text;

                int gameCount;
                int.TryParse(UiTbMaxGames.Text, out gameCount);
                if (gameCount <= 0 || gameCount > DownloadWebGamesManager.MAX_DOWNLOAD_GAME_COUNT)
                {
                    gameCount = DownloadWebGamesManager.MAX_DOWNLOAD_GAME_COUNT;
                }
                filter.MaxGames = gameCount;

                if (UiCbOnlyNew.IsChecked == true)
                {
                    filter.StartDate = null;
                    filter.EndDate = null;
                }
                else
                {
                    filter.StartDate = UiDtStartDate.SelectedDate;
                    filter.EndDate = UiDtEndDate.SelectedDate;

                    VerifyDatesInFilter(filter);
                }
                filter.IsUtcTimes = UiCbUtc.IsChecked == true;

                if (IsChesscomDownload())
                {
                    _ = WebAccess.ChesscomUserGames.GetChesscomUserGames(filter);
                }
                else
                {
                    _ = WebAccess.LichessUserGames.GetLichessUserGames(filter);
                }
            }
        }

        /// <summary>
        /// Replaces null dates with the current date and yesterday if both are null.
        /// If one is null, then the other one is used for both.
        /// </summary>
        /// <param name="filter"></param>
        private void VerifyDatesInFilter(GamesFilter filter)
        {
            if (filter.StartDate == null && filter.EndDate == null)
            {
                filter.StartDate = DateTime.Now;
                filter.EndDate = DateTime.Now;
            }
            else if (!filter.StartDate.HasValue && filter.EndDate.HasValue)
            {
                filter.StartDate = filter.EndDate;
            }
            else if (filter.StartDate.HasValue && !filter.EndDate.HasValue)
            {
                filter.EndDate = filter.StartDate;
            }
        }

        /// <summary>
        /// Checks the Site combobox selection to determine which site we are downloading from.
        /// </summary>
        /// <returns></returns>
        private bool IsChesscomDownload()
        {
            return (string)UiCmbSite.SelectedItem == Constants.ChesscomNameId;
        }

        /// <summary>
        /// Update WebGames configuaration items.
        /// </summary>
        private void UpdateConfiguration()
        {
            string site = (string)UiCmbSite.SelectedItem;
            Configuration.WebGamesSite = site;
            if (site == Constants.LichessNameId)
            {
                Configuration.WebGamesLichessUser = UiTbUserName.Text;
            }
            else if (site == Constants.ChesscomNameId)
            {
                Configuration.WebGamesChesscomUser = UiTbUserName.Text;
            }

            int.TryParse(UiTbMaxGames.Text, out Configuration.WebGamesMaxCount);

            Configuration.WebGamesMostRecent = UiCbOnlyNew.IsChecked == true;
            Configuration.WebGamesStartDate = UiDtStartDate.SelectedDate;
            Configuration.WebGamesEndDate = UiDtEndDate.SelectedDate;
            Configuration.WebGamesDatesUtc = UiCbUtc.IsChecked == true ? 1 : 0;
        }

        /// <summary>
        /// Enable/disable date controls.
        /// </summary>
        /// <param name="enable"></param>
        private void EnableDateControls(bool enable)
        {
            UiDtStartDate.IsEnabled = enable;
            UiDtEndDate.IsEnabled = enable;
            UiBtnResetDates.IsEnabled = enable;
            UiCbUtc.IsEnabled = enable;
        }

        /// <summary>
        /// Makes sure that start date is not later that end date.
        /// </summary>
        private void AdjustDates(bool startDatePriority)
        {
            if (UiDtStartDate.SelectedDate.HasValue && UiDtEndDate.SelectedDate.HasValue)
            {
                if (UiDtStartDate.SelectedDate.Value > UiDtEndDate.SelectedDate.Value)
                {
                    if (startDatePriority)
                    {
                        UiDtEndDate.SelectedDate = UiDtStartDate.SelectedDate;
                    }
                    else
                    {
                        UiDtStartDate.SelectedDate = UiDtEndDate.SelectedDate;
                    }
                }
            }
        }

        /// <summary>
        /// Checkbox for "Recent Games only" chnaged.
        /// Disable date controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbOnlyNew_Checked(object sender, RoutedEventArgs e)
        {
            EnableDateControls(false);
        }

        /// <summary>
        /// Checkbox for "Recent Games only" chnaged.
        /// Enable date controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCbOnlyNew_Unchecked(object sender, RoutedEventArgs e)
        {
            EnableDateControls(true);
        }

        /// <summary>
        /// Web site selection changed so change the user name accordingly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiCmbSite_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetUserName();
        }

        /// <summary>
        /// StartDate control value changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiDtStartDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            AdjustDates(true);
        }

        /// <summary>
        /// EndDate control value changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiDtEndDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            AdjustDates(false);
        }

        /// <summary>
        /// Remove handler subscription.
        /// Otherwise, the it will be called twice when the dialog is called again.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LichessUserGames.UserGamesReceived -= UserGamesReceived;
            ChesscomUserGames.UserGamesReceived -= UserGamesReceived;
        }

        /// <summary>
        /// Reset dates buttom was clicked.
        /// Set the end date to today and start date tpo the last end date
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnResetDates_Click(object sender, RoutedEventArgs e)
        {
            ResetDates();
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
    }
}

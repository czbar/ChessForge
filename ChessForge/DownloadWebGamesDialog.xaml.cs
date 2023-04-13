using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WebAccess;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for ImportWebGamesDialog.xaml
    /// </summary>
    public partial class DownloadWebGamesDialog : Window
    {
        /// <summary>
        /// Constructor. Sets up event handler.
        /// </summary>
        public DownloadWebGamesDialog()
        {
            InitializeComponent();
            GameDownload.UserGamesReceived += UserGamesReceived;
            EnableControls(false);
        }

        /// <summary>
        /// Invoked when the games download has finished.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UserGamesReceived(object sender, WebAccessEventArgs e)
        {
            try
            {
                if (e.Success)
                {
                    if (string.IsNullOrEmpty(e.TextData))
                    {
                        throw new Exception(Properties.Resources.ErrNoGamesDownloaded);
                    }
                    if (e.TextData.IndexOf("DOCTYPE") > 0 && e.TextData.IndexOf("DOCTYPE") < 10)
                    {
                        throw new Exception(Properties.Resources.ErrGameNotFound);
                    }
                    ObservableCollection<GameData> games = new ObservableCollection<GameData>();
                    int gamesCount = PgnMultiGameParser.ParsePgnMultiGameText(e.TextData, ref games);
                    SelectGames(ref games);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Properties.Resources.GameDownloadError + ": " + ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            EnableControls(false);
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

            SelectGamesDialog dlg = new SelectGamesDialog(ref games, SelectGamesDialog.Mode.DOWNLOAD_WEB_GAMES )
            {
                Left = AppState.MainWin.ChessForgeMain.Left + 100,
                Top = AppState.MainWin.ChessForgeMain.Top + 100,
                Topmost = false,
                Owner = AppState.MainWin
            };
            return dlg.ShowDialog() == true;
        }

        /// <summary>
        /// Enables/disables controls depending on whether there is a download in progress.
        /// </summary>
        /// <param name="isDownloading"></param>
        private void EnableControls(bool isDownloading)
        {
            UiLblLoading.Visibility = isDownloading ? Visibility.Visible : Visibility.Collapsed;

            UiBtnDownload.IsEnabled = !isDownloading;
            UiCbOnlyNew.IsEnabled = !isDownloading;
            UiCbUseStartDate.IsEnabled = !isDownloading;
            UiCmbSite.IsEnabled = !isDownloading;
            UiTbMaxGames.IsEnabled = !isDownloading;
            UiTbUserName.IsEnabled = !isDownloading;
            UiDtStartDate.IsEnabled = !isDownloading;
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
                EnableControls(true);
                GamesFilter filter = new GamesFilter();
                filter.User = UiTbUserName.Text;

                int gameCount;
                int.TryParse(UiTbMaxGames.Text, out gameCount);
                if (gameCount <= 0)
                {
                    gameCount = WebAccess.GameDownload.DEFAULT_DOWNLOAD_GAME_COUNT;
                }
                filter.MaxGames = gameCount;

                _ = WebAccess.GameDownload.GetLichessUserGames(filter);
            }
        }

    }
}

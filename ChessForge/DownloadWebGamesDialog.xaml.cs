using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
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
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Properties.Resources.GameDownloadError + ": " + ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// The user clicked the button requesting the download.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(UiTbUserName.Text))
            {
                MessageBox.Show("", "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
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

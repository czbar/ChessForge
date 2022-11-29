using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using WebAccess;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for QuickReplayDialog.xaml
    /// </summary>
    public partial class QuickReplayDialog : Window
    {
        private ChessBoardSmall _chessBoard;
        private VariationTree _tree;
        private ObservableCollection<TreeNode> _mainLine;
        private bool _isAutoReplay = false;
        private bool _isExiting = false;

        /// <summary>
        /// Creates the dialog and requests game's text from lichess.
        /// </summary>
        /// <param name="lichessGameId"></param>
        public QuickReplayDialog(string lichessGameId)
        {
            InitializeComponent();

            ShowControls(false);
            _chessBoard = new ChessBoardSmall(UiCnvBoard, UiImgChessBoard, null, false, false);
            GameDownload.GameReceived += GameReceived;

            GameDownload.GetGame(lichessGameId);
        }

        /// <summary>
        /// Handles the Game Received event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GameReceived(object sender, WebAccessEventArgs e)
        {
            if (_isExiting)
            {
                return;
            }
            try
            {
                if (e.Success)
                {
                    if (string.IsNullOrEmpty(GameDownload.GameText))
                    {
                        throw new Exception("Game text is empty.");
                    }
                    if (GameDownload.GameText.IndexOf("DOCTYPE") > 0 && GameDownload.GameText.IndexOf("DOCTYPE") < 10)
                    {
                        throw new Exception("Game not found.");
                    }
                    _tree = new VariationTree(GameData.ContentType.MODEL_GAME);
                    PgnGameParser pgnGame = new PgnGameParser(GameDownload.GameText, _tree);
                    _chessBoard.DisplayStartingPosition();
                    _mainLine = _tree.SelectLine("1");
                    _isAutoReplay = true;
                    ShowControls(true);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                UiLblLoading.Visibility= Visibility.Collapsed;
                UiImgChessBoard.Visibility= Visibility.Collapsed;
                UiBtnImport.Visibility= Visibility.Collapsed;

                UiLblDownloadError.Visibility= Visibility.Visible;
                UiTbError.Visibility= Visibility.Visible;
                UiTbError.Text= ex.Message;
            }
        }

        /// <summary>
        /// Shows/Hides controls according to the value of hasGames.
        /// </summary>
        /// <param name="hasGame"></param>
        private void ShowControls(bool hasGame)
        {
            UiImgChessBoard.Opacity = hasGame ? 1 : 0.6;
            UiLblLoading.Visibility = hasGame ?  Visibility.Collapsed : Visibility.Visible;

            UiImgFirstMove.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;
            UiImgPreviousMove.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;
            UiImgPlay.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;
            UiImgPause.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;
            UiImgNextMove.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;
            UiImgLastMove.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;

            UiBtnImport.IsEnabled = hasGame;

            UiCnvOver.Background = hasGame ? Brushes.Black : Brushes.White;
            if (hasGame)
            {
                UiImgPlay.Visibility = _isAutoReplay ? Visibility.Collapsed : Visibility.Visible;
                UiImgPause.Visibility = _isAutoReplay ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// User clicked Exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnExit_Click(object sender, RoutedEventArgs e)
        {
            _isExiting= true;
            Close();
        }

        /// <summary>
        /// The dialog is closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _isExiting = true;
        }
    }
}

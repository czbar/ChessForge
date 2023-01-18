using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WebAccess;

namespace ChessForge
{
    /// <summary>
    /// Allows the user to browse and preview Top Games from lichess.
    /// </summary>
    public class LichessGamesPreviewDialog : GamePreviewDialog
    {
        // hosted TopGamesView
        private TopGamesView _topGamesView;

        /// <summary>
        /// Initializes the dialog.
        /// </summary>
        /// <param name="lichessGameId"></param>
        /// <param name="gameIdList"></param>
        public LichessGamesPreviewDialog(string lichessGameId, List<string> gameIdList)
            : base(lichessGameId, gameIdList)
        {
            GameDownload.GameReceived += GameReceived;
            ConfigureTopGamesView();
            DownloadGame(_currentGameId);
        }

        /// <summary>
        /// Shows/Hides game operations related controls. 
        /// </summary>
        /// <param name="hasGame"></param>
        /// <param name="isError"></param>
        override protected void ShowGameControls(bool hasGame, bool isError)
        {
            UiLblLoading.Visibility = (hasGame || isError) ? Visibility.Collapsed : Visibility.Visible;
            UiBtnImport.IsEnabled = hasGame;
            UiLblViewOnLichess.IsEnabled = hasGame;
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
                    _tree.ContentType = GameData.ContentType.MODEL_GAME;

                    PopulateHeaderLine();

                    _chessBoard.DisplayStartingPosition();
                    _mainLine = _tree.SelectLine("1");

                    _currentNodeMoveIndex = 1;
                    RequestMoveAnimation(_currentNodeMoveIndex);

                    ShowControls(true, false);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                ShowControls(false, true);
                MessageBox.Show("Game download error: " + ex.Message, "Chess Forge Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Configures the hosted TopGamesView
        /// </summary>
        private void ConfigureTopGamesView()
        {
            _topGamesView = new TopGamesView(UiRtbGames.Document, false);
            UiRtbGames.IsDocumentEnabled = true;
            _topGamesView.BuildFlowDocument();
            _topGamesView.TopGameClicked += EventSelectGame;
        }

        /// <summary>
        /// Imports the current game into the active chapter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        override protected void UiBtnImport_Click(object sender, RoutedEventArgs e)
        {
            Chapter chapter = AppStateManager.ActiveChapter;
            if (chapter != null)
            {
                AppStateManager.MainWin.UiTabChapters.Focus();
                chapter.AddModelGame(_tree);
                string guid = _tree.Header.GetGuid(out _);
                // if the current active tree is Study Tree, add reference
                if (chapter.ActiveVariationTree != null && chapter.ActiveVariationTree.ContentType == GameData.ContentType.STUDY_TREE)
                {
                    TreeNode nd = chapter.ActiveVariationTree.SelectedNode;
                    if (nd != null)
                    {
                        nd.AddArticleReference(guid);
                    }
                }
                AppStateManager.MainWin.RefreshChaptersViewAfterImport(GameData.ContentType.MODEL_GAME, chapter, chapter.GetModelGameCount() - 1);
                AppStateManager.IsDirty = true;
            }
        }

        /// <summary>
        /// Opens the browser and navigates to the game on lichess. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        override protected void UiLblViewOnLichess_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AppStateManager.ViewGameOnLichess(_currentGameId);
        }

        /// <summary>
        /// Opens the browser and navigates to the game on lichess. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        override protected void UiImgLichess_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AppStateManager.ViewGameOnLichess(_currentGameId);
        }

        /// <summary>
        /// Downloads and displayes the next game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        override protected void UiNextGame_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating)
            {
                _pauseRequested = true;
                _cachedOperation = CachedOperation.NEXT_GAME;
            }
            else
            {
                if (_gameIdList.Count > 0)
                {
                    int index = FindGameIndex(_currentGameId);
                    if (index == -1)
                    {
                        index = 0;
                    }
                    if (index < _gameIdList.Count - 1)
                    {
                        index++;
                        _currentGameId = _gameIdList[index];
                        _isAnimating = false;
                        DownloadGame(_currentGameId);
                    }
                }
            }
        }

        /// <summary>
        /// Downloads and displayes the previous game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        override protected void UiPreviousGame_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating)
            {
                _pauseRequested = true;
                _cachedOperation = CachedOperation.PREV_GAME;
            }
            else
            {
                if (_gameIdList.Count > 0)
                {
                    int index = FindGameIndex(_currentGameId);
                    if (index == -1)
                    {
                        index = 0;
                    }
                    if (index > 0)
                    {
                        index--;
                        _currentGameId = _gameIdList[index];
                        _isAnimating = false;
                        DownloadGame(_currentGameId);
                    }
                }
            }
        }

        /// <summary>
        /// Download a game from lichess.org.
        /// </summary>
        /// <param name="gameId"></param>
        override protected void DownloadGame(string gameId)
        {
            _ = GameDownload.GetGame(gameId);
            _topGamesView.SetRowBackgorunds(gameId);
        }

        /// <summary>
        /// Handler for the click event on a game row
        /// in the hosted TopGamesView
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventSelectGame(object sender, WebAccessEventArgs e)
        {
            try
            {
                _currentGameId = e.GameId;
                PlaySelectGame();
            }
            catch { }
        }

    }
}

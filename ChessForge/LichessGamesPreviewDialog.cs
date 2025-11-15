using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
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
        // the latest Stats data received from Lichess.
        private static LichessOpeningsStats _openingsData;

        // lock object for accessing _openingsData as it theoretically (highly unlikely, though) may be written into while we processing 
        private static object _lockOpeningData = new object();

        // list of games already downloaded in this dialog session
        private List<string> _importedGameIds = new List<string>();

        // downloaded game trees cache
        protected Dictionary<string, VariationTree> _downloadedGameTrees = new Dictionary<string, VariationTree>();

        /// <summary>
        /// Sets the value of the _openingsData property. 
        /// </summary>
        /// <param name="openingsData"></param>
        public static void SetOpeningsData(LichessOpeningsStats openingsData)
        {
            lock (_lockOpeningData)
            {
                _openingsData = openingsData;
            }
        }

        /// <summary>
        /// The tab that should be made active by the caller.
        /// </summary>
        public TabViewType ActiveTabOnExit
        {
            get { return _activeTabOnExit; }
        }

        // hosted TopGamesView
        private TopGamesView _topGamesView;

        // active tab when entering the dialog
        private TabViewType _activeTabOnEntry;

        // active tab when exiting the dialog
        private TabViewType _activeTabOnExit;

        /// <summary>
        /// Initializes the dialog.
        /// </summary>
        /// <param name="lichessGameId"></param>
        /// <param name="gameIdList"></param>
        public LichessGamesPreviewDialog(string lichessGameId, List<string> gameIdList, TabViewType activeTab,
                                         VariationTreeView activeTreeView, int activeArticleIndex)
            : base(lichessGameId, gameIdList, activeTreeView, activeArticleIndex, true)
        {
            GameDownload.GameReceived += GameReceived;
            _activeTabOnEntry = activeTab;
            _activeTabOnExit = activeTab;
            ConfigureTopGamesView();
            DownloadGame(_currentGameId);
            _activeArticleIndexOnEntry = activeArticleIndex;
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
                        UiLblLoading.Visibility = Visibility.Collapsed;
                        MessageBox.Show(Properties.Resources.EmptyGameTextFromLichess, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                    else
                    {
                        if (GameDownload.GameText.IndexOf("DOCTYPE") > 0 && GameDownload.GameText.IndexOf("DOCTYPE") < 10)
                        {
                            throw new Exception(Properties.Resources.ErrGamesNotFound);
                        }

                        VariationTree tree = new VariationTree(GameData.ContentType.MODEL_GAME);
                        PgnGameParser pgnGame = new PgnGameParser(GameDownload.GameText, tree, null);
                        tree.ContentType = GameData.ContentType.MODEL_GAME;

                        string gameId = tree.Header.GetGameId(out _);

                        if (!string.IsNullOrEmpty(gameId))
                        {
                            // set Lichess ID header to GAME_ID from the downloaded game
                            tree.Header.SetHeaderValue(PgnHeaders.KEY_LICHESS_ID, gameId);

                            PopulateHeaderLine(tree);
                            tree.BuildLines();

                            _downloadedGameTrees[tree.Header.GetLichessId(out _)] = tree;

                            _chessBoard.DisplayStartingPosition();
                            _mainLine = tree.GetNodesForLine("1");

                            _currentNodeMoveIndex = 1;
                            RequestMoveAnimation(_currentNodeMoveIndex);

                            ShowControls(true, false);
                        }
                    }
                }
                else
                {
                    //game download failed on the lichess side, remove the blue banner and stop previous replay if happening
                    UiLblLoading.Visibility = Visibility.Collapsed;
                    _mainLine = null;
                    UiImgPause_MouseDown(null, null);
                    MessageBox.Show(Properties.Resources.GameDownloadError, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                ShowControls(false, true);
                MessageBox.Show(Properties.Resources.GameDownloadError + ": " + ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                GameDownload.RemoveFromCache(_currentGameId);
            }
        }

        /// <summary>
        /// Configures the hosted TopGamesView
        /// </summary>
        private void ConfigureTopGamesView()
        {
            _topGamesView = new TopGamesView(UiRtbGames, false);
            UiRtbGames.IsDocumentEnabled = true;
            lock (_lockOpeningData)
            {
                _topGamesView.BuildFlowDocument(_openingsData);
            }
            _topGamesView.TopGameClicked += EventSelectGame;
        }

        /// <summary>
        /// Imports the current game into the active chapter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        override protected void UiBtnImport_Click(object sender, RoutedEventArgs e)
        {
            Chapter chapter = AppState.ActiveChapter;
            if (chapter != null)
            {
                try
                {
                    bool import = true;
                    if (_importedGameIds.Find(x => x == _currentGameId) != null
                        ||
                        !string.IsNullOrEmpty(_currentGameId) && chapter.ModelGames.Find(x => x.Tree.Header.GetHeaderValue(PgnHeaders.KEY_LICHESS_ID) == _currentGameId) != null)
                    {
                        if (MessageBox.Show(Properties.Resources.MsgDuplicateLichessImport
                            , Properties.Resources.ImportIntoChapter
                            , MessageBoxButton.YesNo, MessageBoxImage.Question
                            , MessageBoxResult.No) != MessageBoxResult.Yes)
                        {
                            import = false;
                        }
                    }

                    if (import)
                    {
                        if (_downloadedGameTrees.TryGetValue(_currentGameId, out VariationTree treeToProcess))
                        {
                            AppState.MainWin.UiTabChapters.Focus();
                            _importedGameIds.Add(_currentGameId);

                            AppState.FinalizeLichessDownload(chapter, treeToProcess, _currentGameId, _activeTabOnEntry, _activeViewOnEntry);
                        }
                        else
                        {
                            MessageBox.Show(Properties.Resources.ErrInternalGameNotFound, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Properties.Resources.Error + ": " + ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Opens the browser and navigates to the game on lichess. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        override protected void UiLblViewOnLichess_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AppState.ViewGameOnLichess(_currentGameId);
        }

        /// <summary>
        /// Opens the browser and navigates to the game on lichess. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        override protected void UiImgLichess_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AppState.ViewGameOnLichess(_currentGameId);
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
                _queuedOperation = QueuedOperation.NEXT_GAME;
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
                _queuedOperation = QueuedOperation.PREV_GAME;
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
            _topGamesView.SetRowBackgrounds(gameId);
        }

        /// <summary>
        /// Prepares replay of the selected game.
        /// </summary>
        override protected void PlaySelectedGame()
        {
            if (_isAnimating)
            {
                _pauseRequested = true;
                _queuedOperation = QueuedOperation.SELECT_GAME;
            }
            else
            {
                _isAnimating = false;
                DownloadGame(_currentGameId);
            }
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
                PlaySelectedGame();
            }
            catch { }
        }

    }
}

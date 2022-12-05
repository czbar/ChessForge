using GameTree;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using WebAccess;
using System.Text;
using System.Linq;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Interaction logic for QuickReplayDialog.xaml
    /// </summary>
    public partial class QuickReplayDialog : Window
    {
        // chessboard object for game replay
        private ChessBoardSmall _chessBoard;

        // VariationTree into which the current game is loaded
        private VariationTree _tree;

        // the list of Nodes to replay
        private ObservableCollection<TreeNode> _mainLine;

        // true if we are exiting the dialog
        private bool _isExiting = false;

        // index in the list of Nodes of the position with the move being animated
        private int _currentNodeMoveIndex = 0;

        // move animation speed in millieconds
        private int _animationSpeed = 200;

        // the MoveAnimator object running the animation
        private MoveAnimator _animator;

        // whether the Animation was started and there was no completion event
        private bool _isAnimating = false;

        // whether pause was requested by the user
        private bool _pauseRequested = false;

        // animation speeds
        private const int _fastAnimation = 200;
        private const int _mediumAnimation = 400;
        private const int _slowAnimation = 800;

        /// <summary>
        /// List of operations that can be put on hold
        /// if requested while animation is running.
        /// </summary>
        private enum CachedOperation
        {
            NONE,
            FIRST,
            LAST,
            NEXT,
            PREV,
            PAUSE,
            PLAY
        }

        // currently cached operation
        private CachedOperation _cachedOperation;

        private List<string> _gameIdList = new List<string>();
        private string _currentGameId;

        /// <summary>
        /// Creates the dialog and requests game's text from lichess.
        /// </summary>
        /// <param name="lichessGameId"></param>
        public QuickReplayDialog(string lichessGameId, List<string> gameIdList)
        {
            InitializeComponent();
            _gameIdList = gameIdList;
            _currentGameId = lichessGameId;

            ShowControls(false, false);
            _chessBoard = new ChessBoardSmall(UiCnvBoard, UiImgChessBoard, null, false, false);
            _animator = new MoveAnimator(_chessBoard);

            _animationSpeed = _fastAnimation;
            UiRbFastReplay.IsChecked = true;
            _animator.SetAnimationSpeed(_animationSpeed);

            GameDownload.GameReceived += GameReceived;
            DownloadGame(_currentGameId);

            _animator.AnimationCompleted += AnimationFinished;
        }

        /// <summary>
        /// Download a game.
        /// </summary>
        /// <param name="gameId"></param>
        private void DownloadGame(string gameId)
        {
            _ = GameDownload.GetGame(gameId);
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
        /// Sets up the label with players' names.
        /// </summary>
        private void PopulateHeaderLine()
        {
            UiLblWhiteSquare.Content = Constants.CharWhiteSquare;

            string white = _tree.Header.GetWhitePlayer(out _) ?? "";
            UiLblWhite.Content = white;
            UiLblWhite.FontWeight = FontWeights.Bold;
            UiLblWhite.ToolTip = white;

            UiLblBlackSquare.Content = Constants.CharBlackSquare;

            string black = _tree.Header.GetBlackPlayer(out _) ?? "";
            UiLblBlack.Content = black;
            UiLblBlack.FontWeight = FontWeights.Bold;
            UiLblBlack.ToolTip = black;

            string result = (_tree.Header.GetResult(out _) ?? "");
            UiLblResult.Content = result;
            UiLblResult.FontWeight = FontWeights.Bold;
        }

        /// <summary>
        /// Request animation of the move at a given index in the
        /// Node list.
        /// </summary>
        /// <param name="moveIndex"></param>
        private void RequestMoveAnimation(int moveIndex)
        {
            if (moveIndex > 0 && moveIndex < _mainLine.Count)
            {
                _animator.AnimateMove(_mainLine[moveIndex]);
                _isAnimating = true;
            }
            else
            {
                _isAnimating = false;
            }
            ShowControls(true, false);
        }

        /// <summary>
        /// Shows/Hides controls according to the value of hasGames.
        /// </summary>
        /// <param name="hasGame"></param>
        private void ShowControls(bool hasGame, bool isError)
        {
            UiLblLoading.Visibility = (hasGame || isError) ? Visibility.Collapsed : Visibility.Visible;

            UiImgFirstMove.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;
            UiImgPreviousMove.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;
            UiImgPlay.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;
            UiImgPause.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;
            UiImgNextMove.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;
            UiImgLastMove.Visibility = hasGame ? Visibility.Visible : Visibility.Collapsed;

            UiBtnImport.IsEnabled = hasGame;
            UiBtnLichess.IsEnabled = hasGame;

            UiImgFirstMove.IsEnabled = _currentNodeMoveIndex > 1;
            UiImgPreviousMove.IsEnabled = _currentNodeMoveIndex > 1;
            UiImgNextMove.IsEnabled = _mainLine != null &&  (_currentNodeMoveIndex < _mainLine.Count - 1);
            UiImgLastMove.IsEnabled = _mainLine != null && (_currentNodeMoveIndex < _mainLine.Count - 1);

            UiBtnNextGame.IsEnabled = !IsCurrentGameLast();
            UiBtnPreviousGame.IsEnabled = !IsCurrentGameFirst();

            if (hasGame)
            {
                ShowPlayPauseButtons();
            }
        }

        /// <summary>
        /// Shows/hides Play/Pause buttons depending on the state
        /// of animation.
        /// </summary>
        private void ShowPlayPauseButtons()
        {
            UiImgPause.Visibility = _isAnimating ? Visibility.Visible : Visibility.Collapsed;
            UiImgPlay.Visibility = _isAnimating ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Invoke when move animation finishes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimationFinished(object sender, EventArgs e)
        {
            _chessBoard.DisplayPosition(_mainLine[_currentNodeMoveIndex]);

            if (_pauseRequested)
            {
                _isAnimating = false;
                _pauseRequested = false;

                switch (_cachedOperation)
                {
                    case CachedOperation.FIRST:
                        UiImgFirstMove_MouseDown(null, null);
                        break;
                    case CachedOperation.LAST:
                        UiImgLastMove_MouseDown(null, null);
                        break;
                    case CachedOperation.PREV:
                        UiImgPreviousMove_MouseDown(null, null);
                        break;
                    case CachedOperation.NEXT:
                        UiImgNextMove_MouseDown(null, null);
                        break;
                    case CachedOperation.PAUSE:
                        UiImgPause_MouseDown(null, null);
                        break;
                    case CachedOperation.PLAY:
                        UiImgPlay_MouseDown(null, null);
                        break;
                }
                _cachedOperation = CachedOperation.NONE;
            }
            else if (_currentNodeMoveIndex < _mainLine.Count - 1)
            {
                _currentNodeMoveIndex++;
                RequestMoveAnimation(_currentNodeMoveIndex);
            }
            else
            {
                _isAnimating = false;
                ShowControls(true, false);
            }
        }

        /// <summary>
        /// Returns true if the currently viewed game is last on the list.
        /// </summary>
        /// <returns></returns>
        private bool IsCurrentGameLast()
        {
            return _gameIdList.Count == 0 || _gameIdList[_gameIdList.Count - 1] == _currentGameId;
        }

        /// <summary>
        /// Returns true if the currently viewed game is first on the list.
        /// </summary>
        /// <returns></returns>
        private bool IsCurrentGameFirst()
        {
            return _gameIdList.Count == 0 || _gameIdList[0] == _currentGameId;
        }

        /// <summary>
        /// User clicked Exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnExit_Click(object sender, RoutedEventArgs e)
        {
            _isExiting = true;
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


        //********************************************************
        //
        // USER MOVE CONTROLS
        //
        //********************************************************

        /// <summary>
        /// Pause button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgPause_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAnimating)
            {
                _pauseRequested = true;
                _cachedOperation = CachedOperation.PAUSE;
            }
            else
            {
                ShowPlayPauseButtons();
                _chessBoard.DisplayPosition(_mainLine[_currentNodeMoveIndex]);
            }
        }

        /// <summary>
        /// Play button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgPlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAnimating)
            {
                _pauseRequested = true;
                _cachedOperation = CachedOperation.PLAY;
            }
            else
            {
                UiImgPause.Visibility = Visibility.Visible;
                UiImgPlay.Visibility = Visibility.Collapsed;

                if (_currentNodeMoveIndex < _mainLine.Count - 1)
                {
                    _currentNodeMoveIndex++;
                    RequestMoveAnimation(_currentNodeMoveIndex);
                }
            }
        }

        /// <summary>
        /// Got to first move button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgFirstMove_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAnimating)
            {
                _pauseRequested = true;
                _cachedOperation = CachedOperation.FIRST;
            }
            else
            {
                _cachedOperation = CachedOperation.NONE;
                _currentNodeMoveIndex = 1;
                _chessBoard.DisplayPosition(_mainLine[_currentNodeMoveIndex - 1]);
            }
        }

        /// <summary>
        /// Go to one move back button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgPreviousMove_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //BreakOutOfAnimation();

            if (_isAnimating)
            {
                _pauseRequested = true;
                _cachedOperation = CachedOperation.PREV;
            }
            else
            {
                _cachedOperation = CachedOperation.NONE;

                if (_currentNodeMoveIndex > 1)
                {
                    _currentNodeMoveIndex--;
                    _chessBoard.DisplayPosition(_mainLine[_currentNodeMoveIndex - 1]);
                }
            }
        }

        /// <summary>
        /// Make the next move button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgNextMove_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAnimating)
            {
                _pauseRequested = true;
                _cachedOperation = CachedOperation.NEXT;
            }
            else
            {
                _cachedOperation = CachedOperation.NONE;
                if (_currentNodeMoveIndex < _mainLine.Count - 1)
                {
                    _currentNodeMoveIndex++;
                    _chessBoard.DisplayPosition(_mainLine[_currentNodeMoveIndex - 1]);
                }
            }
        }

        /// <summary>
        /// Got to last move button clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiImgLastMove_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAnimating)
            {
                _pauseRequested = true;
                _cachedOperation = CachedOperation.NEXT;
            }
            else
            {
                _cachedOperation = CachedOperation.NONE;
                _currentNodeMoveIndex = _mainLine.Count - 1;
                _chessBoard.DisplayPosition(_mainLine[_currentNodeMoveIndex]);
            }
        }

        //********************************************************
        //
        // REPLAY SPEED RADIO BUTTONS
        //
        //********************************************************

        /// <summary>
        /// Request fast speed replay
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbFastReplay_Checked(object sender, RoutedEventArgs e)
        {
            _animationSpeed = _fastAnimation;
            _animator.SetAnimationSpeed(_animationSpeed);
        }

        /// <summary>
        /// Request medium speed replay
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbMediumReplay_Checked(object sender, RoutedEventArgs e)
        {
            _animationSpeed = _mediumAnimation;
            _animator.SetAnimationSpeed(_animationSpeed);
        }

        /// <summary>
        /// Request slow speed replay
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiRbSlowReplay_Checked(object sender, RoutedEventArgs e)
        {
            _animationSpeed = _slowAnimation;
            _animator.SetAnimationSpeed(_animationSpeed);
        }

        /// <summary>
        /// Finds the requested game in the list.
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns></returns>
        private int FindGameIndex(string gameId)
        {
            int index = -1;

            for (int i = 0; i < _gameIdList.Count; i++)
            {
                if (_gameIdList[i] == gameId)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        /// <summary>
        /// Downloads and displayes the next game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnNextGame_Click(object sender, RoutedEventArgs e)
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

        /// <summary>
        /// Downloads and displayes the previous game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnPreviousGame_Click(object sender, RoutedEventArgs e)
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

        /// <summary>
        /// Opens the browser and navigates to the game on lichess. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnLichess_Click(object sender, RoutedEventArgs e)
        {
            AppStateManager.ViewGameOnLichess(_currentGameId);
        }

        /// <summary>
        /// Imports the current game into the active chapter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UiBtnImport_Click(object sender, RoutedEventArgs e)
        {
            Chapter chapter = AppStateManager.ActiveChapter;
            if (chapter != null)
            {
                chapter.AddModelGame(_tree);
                AppStateManager.MainWin.RefreshChaptersViewAfterImport(GameData.ContentType.MODEL_GAME, chapter, chapter.GetModelGameCount() - 1);
                AppStateManager.IsDirty = true;
            }
        }
    }
}

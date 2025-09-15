using GameTree;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Handles selection and replay of Referenced games.
    /// </summary>
    public class SingleGamePreviewDialog : GamePreviewDialog
    {
        /// <summary>
        /// Chapter index of the replayed item.
        /// </summary>
        public int SelectedChapterIndex = -1;

        /// <summary>
        /// Article Index of the replayed item.
        /// </summary>
        public int SelectedArticleIndex = -1;

        /// <summary>
        /// Type of the replayed item (Game or Exercise)
        /// </summary>
        public GameData.ContentType SelectedContentType = GameData.ContentType.NONE;

        // a list of one article
        private List<Article> _games;

        /// <summary>
        /// Creates the dialog.
        /// Serves only a single article but uses lists for "compatibility" with the base class.
        /// </summary>
        /// <param name="gameIdList"></param>
        /// <param name="games"></param>
        public SingleGamePreviewDialog(List<string> gameIdList, List<Article> games)
            : base(null, gameIdList, null, -1)
        {
            if (games.Count > 0)
            {
                GameData.ContentType ctype = games[0].Tree.Header.GetContentType(out _);
                if (ctype == GameData.ContentType.EXERCISE)
                {
                    this.Title = Properties.Resources.ExercisePreview;
                }
                else
                {
                    this.Title = Properties.Resources.GamePreview;
                }
            }

            Thickness currExitBtnRThick = UiBtnExit.Margin;
            currExitBtnRThick.Right = 25;
            UiBtnExit.Margin = currExitBtnRThick;

            this.Width = 520;
            UiGReplaySpeed.Margin = new Thickness(UiGReplaySpeed.Margin.Left, UiGReplaySpeed.Margin.Top - 40, UiGReplaySpeed.Margin.Right, UiGReplaySpeed.Margin.Bottom);
            UiBtnExit.Margin = new Thickness(UiBtnExit.Margin.Left, UiBtnExit.Margin.Top, UiBtnExit.Margin.Right, UiBtnExit.Margin.Bottom + 20);

            _games = games;
            PlaySelectedGame();
        }

        /// <summary>
        /// Updates GUI for the selected game and starts replay.
        /// </summary>
        /// <param name="index"></param>
        private void SelectGame()
        {
            _tree = _games[0].Tree;
            _tree.BuildLines();
            PopulateHeaderLine(_tree);
            _chessBoard.DisplayStartingPosition();
            _mainLine = _tree.GetNodesForLine("1");

            _currentNodeMoveIndex = 1;
            RequestMoveAnimation(_currentNodeMoveIndex);

            ShowControls(true, false);
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
                SelectGame();
            }
        }

        /// <summary>
        /// Not handled in this sub-class as there is no ability to select.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        override protected void UiLbGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { }

        /// <summary>
        /// Shows/Hides game operations related controls. 
        /// </summary>
        /// <param name="hasGame"></param>
        /// <param name="isError"></param>
        protected override void ShowGameControls(bool hasGame, bool isError)
        {
            UiLblLoading.Visibility = Visibility.Collapsed;
            UiBtnImport.Visibility = Visibility.Collapsed;
            UiLblViewOnLichess.Visibility = Visibility.Collapsed;

            UiImgGameUp.Visibility = Visibility.Collapsed;
            UiImgGameDown.Visibility = Visibility.Collapsed;

            UiLblNextGame.Visibility = Visibility.Collapsed;
            UiLblPrevGame.Visibility = Visibility.Collapsed;

            UiImgLichess.Visibility = Visibility.Collapsed;

            UiRtbGames.Visibility = Visibility.Collapsed;

            UiLbGames.Visibility = Visibility.Collapsed;
            UiBtnViewGame.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// The user clicked the Full View button so we close the dialog
        /// and open the Game/Exercise view for the selected item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        override protected void UiBtnViewGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int index = UiLbGames.SelectedIndex;
                SelectedContentType = _games[index].Tree.ContentType;
                WorkbookManager.SessionWorkbook.GetArticleByGuid(_games[index].Tree.Header.GetGuid(out _), out SelectedChapterIndex, out SelectedArticleIndex);
                DialogResult = true;
            }
            catch
            {
            }
        }
    }
}

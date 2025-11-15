using System;
using System.Collections.Generic;
using System.Windows;
using System.Text;
using GameTree;
using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Handles selection and replay of Referenced games.
    /// </summary>
    public class ReferenceGamePreviewDialog : GamePreviewDialog
    {
        public int SelectedChapterIndex = -1;
        public int SelectedArticleIndex = -1;
        public GameData.ContentType SelectedContentType = GameData.ContentType.NONE;

        // games to show
        private List<Article> _games;

        /// <summary>
        /// Creates the dialog.
        /// Populates the ListBox.
        /// </summary>
        /// <param name="gameIdList"></param>
        /// <param name="games"></param>
        public ReferenceGamePreviewDialog(List<string> gameIdList, List<Article> games)
            : base(null, gameIdList, null, -1)
        {
            this.Width = 695;
            UiBtnExit.Margin = new Thickness(UiBtnExit.Margin.Left, UiGReplaySpeed.Margin.Top - 35, 0, 0);

            _games = games;
            foreach (var game in games)
            {
                UiLbGames.Items.Add(game.Tree.Header.BuildGameHeaderLine(game.Tree.Header.GetContentType(out _) == GameData.ContentType.MODEL_GAME));
            }
            UiLbGames.SelectedIndex = 0;
            PlaySelectedGame();
        }

        /// <summary>
        /// Updates GUI for the selected game and starts replay.
        /// </summary>
        /// <param name="index"></param>
        private void SelectGame(int index)
        {
            if (index >= 0)
            {
                // process a variation tree for the selected game
                // to get the _mainLine nodes an populate GUI fields.
                VariationTree tree = _games[index].Tree;
                if (string.IsNullOrEmpty(tree.RootNode.LineId))
                {
                    tree.BuildLines();
                }
                PopulateHeaderLine(tree);

                _chessBoard.DisplayStartingPosition();
                _mainLine = tree.GetNodesForLine("1");
                _currentNodeMoveIndex = 1;
                RequestMoveAnimation(_currentNodeMoveIndex);

                ShowControls(true, false);
            }
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
                SelectGame(UiLbGames.SelectedIndex);
            }
        }

        /// <summary>
        /// The user changed selecttion in the Games ListBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        override protected void UiLbGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectGame(UiLbGames.SelectedIndex);
        }

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

            UiLbGames.Visibility = Visibility.Visible;
            UiBtnViewGame.Visibility = Visibility.Visible;
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

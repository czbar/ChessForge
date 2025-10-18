using GameTree;
using ChessPosition;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using WebAccess;

namespace ChessForge
{
    /// <summary>
    /// Mnaages the process of evaluating multiple games one after another
    /// </summary>
    public class GamesEvaluationManager
    {
        // whether the dialog's variables have been initialized
        private static bool _initialized = false;

        // dialog shown while evaluation is in progress
        private static GamesEvalDialog _dlgProgress;

        // whether evaluation process has been initilized
        private static bool _isEvaluationInProgress = false;

        // whether evaluation process has been initilized and the first evaluation requested
        private static bool _isEvaluationStarted = false;

        // index in the list of games of the game being evaluated
        private static int _evalGameIndex = -1;

        // list of games to evaluate
        private static ObservableCollection<ArticleListItem> _games;

        // total plies to evaluate
        private static int _plyCountToEvaluate;

        // plies evaluated running count
        private static int _pliesEvaluated;

        // games evaluated running count
        private static int _gamesEvaluated = 0;

        // total games to evaluate
        private static int _gamesToEvaluate = 0;

        // estimated execution time
        private static long _estExecutionTime;

        /// <summary>
        /// Flags if the evaluation is currently in progress.
        /// </summary>
        public static bool IsEvaluationInProgress
        {
            get => _isEvaluationInProgress;
            set => _isEvaluationInProgress = value;
        }

        /// <summary>
        /// Sets references for the games to be evaluated.
        /// Starts the timer.
        /// </summary>
        public static void InitializeProcess(ObservableCollection<ArticleListItem> games)
        {
            _isEvaluationStarted = false;

            if (!_initialized)
            {
                EngineMessageProcessor.MoveEvalFinished += MoveEvalFinished;
                _initialized = true;
            }
            _games = games;

            _plyCountToEvaluate = 0;
            _pliesEvaluated = 0;
            _gamesEvaluated = 0;
            _gamesToEvaluate = 0;
            _evalGameIndex = -1;

            foreach (ArticleListItem game in _games)
            {
                if (game.ContentType == GameData.ContentType.MODEL_GAME && game.Article != null)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(game.Article.Tree.RootNode.LineId))
                        {
                            game.Article.Tree.BuildLines();
                        }
                        if (game.IsSelected == true)
                        {
                            int plyCount = CalculatePlyCount(game);
                            _plyCountToEvaluate += plyCount;
                            _gamesToEvaluate++;
                        }
                    }
                    catch { }
                }
            }

            _estExecutionTime = _plyCountToEvaluate * Configuration.EngineEvaluationTime;

            AppState.MainWin.Timers.Start(AppTimers.TimerId.GAMES_EVALUATION);
            _isEvaluationInProgress = true;
        }

        /// <summary>
        /// Handles timer events. If this is the first event, kicks off the process.
        /// Otherwise checks on progress.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void StartGamesEvaluation(object source, ElapsedEventArgs e)
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                if (!_isEvaluationStarted)
                {
                    try
                    {
                        AppState.MainWin.UiTabModelGames.Focus();
                        _isEvaluationStarted = true;
                        _evalGameIndex = FindNextGameIndex(_evalGameIndex);
                        if (_evalGameIndex >= 0 && _plyCountToEvaluate > 0)
                        {
                            _dlgProgress = new GamesEvalDialog();
                            GuiUtilities.PositionDialog(_dlgProgress, AppState.MainWin, 100);
                            _dlgProgress.UiPbProgress.Minimum = 0;
                            _dlgProgress.UiPbProgress.Maximum = 100;
                            SetGameNoLabel();
                            KickoffSingleGameEval(_evalGameIndex);
                            _dlgProgress.ShowDialog();
                        }
                        else
                        {
                            MessageBox.Show(Properties.Resources.MsgNothingSelectedForEvaluation, Properties.Resources.Information, MessageBoxButton.OK, MessageBoxImage.Information);
                        }

                        _dlgProgress = null;

                    }
                    catch
                    {
                    }

                    _isEvaluationInProgress = false;
                    AppState.MainWin.Timers.Stop(AppTimers.TimerId.GAMES_EVALUATION);
                    AppState.MainWin.StopEvaluation(true);
                }
            });
        }

        /// <summary>
        /// This will be called from the App when a problem occured
        /// while evaluating multiple games.
        /// </summary>
        public static void CloseDialog()
        {
            if (_dlgProgress != null)
            {
                try
                {
                    AppState.MainWin.Dispatcher.Invoke(() =>
                    {
                        _dlgProgress.AbandonEvaluation();
                    } );
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Checks if the passed move index (ply number) is outside the configured range.
        /// </summary>
        /// <param name="moveIndex"></param>
        /// <returns></returns>
        public static bool IsAboveMoveRangeEnd(int moveIndex)
        {
            bool result = false;

            if (Configuration.EvalMoveRangeEnd > 0 && Configuration.EvalMoveRangeEnd * 2 <= moveIndex)
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Invoked when move evaluation finishes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MoveEvalFinished(object sender, MoveEvalEventArgs e)
        {
            if (_isEvaluationInProgress)
            {
                _pliesEvaluated++;
                AppState.MainWin.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        double fract = (double)_pliesEvaluated / (double)_plyCountToEvaluate;
                        long timeRemaining = _estExecutionTime - (long)(fract * (double)_estExecutionTime);
                        _dlgProgress.UiLblTimeRemaining.Content = Properties.Resources.TimeRemainig + ": " + GuiUtilities.TimeStringInTwoParts(timeRemaining);
                        int pct = (int)(fract * 100.0);
                        _dlgProgress.UiLblProgressPct.Content = pct.ToString() + "%";
                        _dlgProgress.UiPbProgress.Value = pct;

                        if (e.IsLastMove || IsAboveMoveRangeEnd(e.MoveIndex))
                        {
                            _gamesEvaluated++;
                            _evalGameIndex = FindNextGameIndex(_evalGameIndex);
                            if (_evalGameIndex >= 0)
                            {
                                SetGameNoLabel();
                                KickoffSingleGameEval(_evalGameIndex);
                            }
                            else
                            {
                                _dlgProgress.Close();
                                _dlgProgress = null;
                            }
                        }
                    }
                    catch
                    {
                    }
                });
            }
        }

        /// <summary>
        /// Sets text of the "Game N of M" label. 
        /// </summary>
        private static void SetGameNoLabel()
        {
            string gameNo = (Properties.Resources.Game0of0).Replace("$0", (_gamesEvaluated + 1).ToString()).Replace("$1", _gamesToEvaluate.ToString());
            _dlgProgress.UiLblCurrentGame.Content = gameNo;
        }

        /// <summary>
        /// Returns the index of the next game to evaluate.
        /// Returns -1 when none found.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        private static int FindNextGameIndex(int startIndex)
        {
            for (int i = startIndex + 1; i < _games.Count; i++)
            {
                if (_games[i].IsSelected == true)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Starts evaluation of a particular game.
        /// </summary>
        /// <param name="gameIndex"></param>
        private static void KickoffSingleGameEval(int gameIndex)
        {
            ArticleListItem game = _games[gameIndex];
            AppState.MainWin.SelectArticle(game.ChapterIndex, GameData.ContentType.MODEL_GAME, game.ArticleIndex);
            ObservableCollection<TreeNode> lineToSelect = game.Article.Tree.GetNodesForLine("1");
            if (HasMovesToEvaluate(game))
            {
                int firstNodeId = FirstNodeToEvaluate(lineToSelect);
                AppState.MainWin.SetActiveLine(lineToSelect, firstNodeId);
                AppState.MainWin.UiMnEvaluateLine_Click(null, null);
            }
            else
            {
                // send a fake MoveEvalFinished message
                MoveEvalEventArgs args = new MoveEvalEventArgs();
                args.IsLastMove = true;
                MoveEvalFinished(null, args);
            }
        }

        /// <summary>
        /// Calculate the number of plies to calculate, taking into
        /// account configured range.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        private static int CalculatePlyCount(ArticleListItem game)
        {
            int plyCount = game.Article.Tree.GetNodesForLine("1").Count - 1;
            if (Configuration.EvalMoveRangeEnd > 0)
            {
                plyCount = Math.Min(plyCount, Configuration.EvalMoveRangeEnd * 2);
            }

            if (Configuration.EvalMoveRangeStart > 0)
            {
                plyCount = plyCount - (Configuration.EvalMoveRangeStart - 1) * 2;
                if (plyCount < 0)
                {
                    plyCount = 0;
                }
            }

            return plyCount;
        }

        /// <summary>
        /// Checks if the game is long enough so that at least one move
        /// falls within the configured move range.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        private static bool HasMovesToEvaluate(ArticleListItem game)
        {
            bool result = false;

            int plyCount = game.Article.Tree.Nodes.Count - 1;
            if (plyCount > 0)
            {
                uint lastMoveNumber = game.Article.Tree.Nodes[plyCount].MoveNumber;
                if (lastMoveNumber >= Configuration.EvalMoveRangeStart)
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Determines NodeId of the node to start the evaluation from.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        private static int FirstNodeToEvaluate(ObservableCollection<TreeNode> lineToSelect)
        {
            int startNodeIndex = 0;
            if (Configuration.EvalMoveRangeStart > 0)
            {
                startNodeIndex = (Configuration.EvalMoveRangeStart - 1) * 2;
            }

            int firstNodeId = 0;

            if (startNodeIndex < lineToSelect.Count && lineToSelect[startNodeIndex].Children.Count > 0)
            {
                firstNodeId = lineToSelect[startNodeIndex].Children[0].NodeId;
            }
            
            return firstNodeId;
        }
    }
}

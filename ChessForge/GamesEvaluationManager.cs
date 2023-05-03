﻿using GameTree;
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

        /// <summary>
        /// Flags if the evaluation is currently in progress.
        /// </summary>
        public static bool IsEvaluationInProgress
        {
            get => _isEvaluationInProgress;
            private set => _isEvaluationInProgress = value;
        }

        /// <summary>
        /// Sets references for the games to be evaluated.
        /// Starts the timer.
        /// </summary>
        public static void InitializeProcess(ObservableCollection<ArticleListItem> games)
        {
            if (!_initialized)
            {
                EngineMessageProcessor.MoveEvalFinished += MoveEvalFinished;
                _initialized = true;
            }
            _games = games;
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
                    AppState.MainWin.UiTabModelGames.Focus();
                    _isEvaluationStarted = true;
                    _evalGameIndex = FindNextGameIndex(_evalGameIndex);
                    if (_evalGameIndex >= 0)
                    {
                        KickoffSingleGameEval(_evalGameIndex);
                    }

                    // display dialog monitoring progress
                    _dlgProgress = new GamesEvalDialog();
                    _dlgProgress.ShowDialog();
                }

                //foreach (ArticleListItem game in _games)
                //{
                //    if (game.IsSelected)
                //    {
                //        AppState.MainWin.SelectArticle(game.ChapterIndex, GameData.ContentType.MODEL_GAME, game.ArticleIndex);

                //        ObservableCollection<TreeNode> lineToSelect = game.Article.Tree.SelectLine("1");
                //        if (game.Article.Tree.Nodes.Count > 1)
                //        {
                //            int firstNodeId = game.Article.Tree.Nodes[0].Children[0].NodeId;
                //            AppState.MainWin.SetActiveLine(lineToSelect, firstNodeId);
                //            AppState.MainWin.UiMnEvaluateLine_Click(null, null);
                //        }
                //    }
                //}
            });
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
                AppState.MainWin.Dispatcher.Invoke(() =>
                {
                    // update dialog
                });
            }
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
                if (_games[i].IsSelected)
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
            ObservableCollection<TreeNode> lineToSelect = game.Article.Tree.SelectLine("1");
            if (game.Article.Tree.Nodes.Count > 1)
            {
                int firstNodeId = game.Article.Tree.Nodes[0].Children[0].NodeId;
                AppState.MainWin.SetActiveLine(lineToSelect, firstNodeId);
                AppState.MainWin.UiMnEvaluateLine_Click(null, null);
            }
        }

        /// <summary>
        /// Stops the evaluation process.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void StopGamesEvaluation(object source, ElapsedEventArgs e)
        {
            AppState.MainWin.Timers.Stop(AppTimers.TimerId.GAMES_EVALUATION);
        }
    }
}
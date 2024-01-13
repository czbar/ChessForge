using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChessPosition;
using Newtonsoft.Json.Linq;

namespace ChessForge
{
    /// <summary>
    /// Utilities for calculating result statistics.
    /// </summary>
    public class StatsUtils
    {
        /// <summary>
        /// Calculates and reports stats for the requested scope.
        /// </summary>
        /// <param name="scope"></param>
        public static void ReportStats(OperationScope scope)
        {
            Dictionary<string, PlayerAggregatedStats> _dictPlayersStats = new Dictionary<string, PlayerAggregatedStats>();
            StatsData stats = new StatsData();
            GetResultsStats(scope, stats, _dictPlayersStats);

            // report in the dialog
            // identify the common player(s)
        }

        /// <summary>
        /// Calculates statistics for the requested scope.
        /// Returns the numbers of white, black wins and draws.
        /// Returns non-null values for commonPlayer_1  and commonPlayer_2 
        /// if there is player/players who played every game.
        /// </summary>
        /// <param name="scope">this must be either CHAPTER or WORKBOOK</param>
        /// <param name="whiteWins"></param>
        /// <param name="blackWins"></param>
        /// <param name="draws"></param>
        /// <param name="commonPlayer_1"></param>
        /// <param name="commonPlayer_2"></param>
        public static void GetResultsStats(OperationScope scope, StatsData stats, Dictionary<string, PlayerAggregatedStats> dictPlayersStats)
        {
            if (scope == OperationScope.CHAPTER)
            {
                if (AppState.ActiveChapter != null)
                {
                    GetChapterResultsStats(AppState.ActiveChapter, stats, dictPlayersStats);

                    stats.ChapterCount = 1;
                    stats.GameCount = AppState.ActiveChapter.GetModelGameCount();
                    stats.ExerciseCount = AppState.ActiveChapter.GetExerciseCount();
                }
            }
            else if (scope == OperationScope.WORKBOOK && AppState.Workbook != null)
            {
                foreach (Chapter chapter in AppState.Workbook.Chapters)
                {
                    stats.ChapterCount++;
                    stats.GameCount += chapter.GetModelGameCount();
                    stats.ExerciseCount += chapter.GetExerciseCount();

                    StatsData chapterStats = new StatsData();
                    if (GetChapterResultsStats(chapter, chapterStats, dictPlayersStats))
                    {
                        stats.WhiteWins += chapterStats.WhiteWins;
                        stats.BlackWins += chapterStats.BlackWins;
                        stats.Draws += chapterStats.Draws;
                    }
                }
            }
        }

        /// <summary>
        /// Calculates statistics for a single chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="whiteWins"></param>
        /// <param name="blackWins"></param>
        /// <param name="draws"></param>
        /// <param name="commonPlayer_1"></param>
        /// <param name="commonPlayer_2"></param>
        /// <returns>true if any games were processed</returns>
        private static bool GetChapterResultsStats(Chapter chapter, StatsData stats, Dictionary<string, PlayerAggregatedStats> dictPlayersStats)
        {
            int gamesCount = chapter.ModelGames.Count;

            try
            {
                foreach (Article article in chapter.ModelGames)
                {
                    GetResultFromString(article.Tree.Header.GetResult(out _), out bool whiteWin, out bool blackWin, out bool draw);
                    stats.WhiteWins += whiteWin ? 1 : 0;
                    stats.BlackWins += blackWin ? 1 : 0;
                    stats.Draws += draw ? 1 : 0;

                    string whitePlayer = article.Tree.Header.GetWhitePlayer(out _).Trim();
                    string blackPlayer = article.Tree.Header.GetBlackPlayer(out _).Trim();

                    UpdatePlayerScoresStats(whitePlayer, true, whiteWin, blackWin, draw, dictPlayersStats);
                    UpdatePlayerScoresStats(blackPlayer, false, whiteWin, blackWin, draw, dictPlayersStats);

                    // adjust total games if white and black have the same name, to avoid incorrect games total for the player
                    if (whitePlayer == blackPlayer && !string.IsNullOrEmpty(whitePlayer))
                    {
                        ReduceTotalGamesCount(whitePlayer, dictPlayersStats);
                    }
                }
            }
            catch
            {
            }
            return gamesCount > 0;
        }

        /// <summary>
        /// Decrements the total number of games for a player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="dictPlayersStats"></param>
        private static void ReduceTotalGamesCount(string player, Dictionary<string, PlayerAggregatedStats> dictPlayersStats)
        {
            if (dictPlayersStats.ContainsKey(player))
            {
                dictPlayersStats[player].TotalStats.GameCount--;
            }
        }

        /// <summary>
        /// Updates individual player stats.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="playedWhite"></param>
        /// <param name="whiteWin"></param>
        /// <param name="blackWin"></param>
        /// <param name="draw"></param>
        /// <param name="dictPlayersStats"></param>
        private static void UpdatePlayerScoresStats(string player,
                                                    bool playedWhite,
                                                    bool whiteWin,
                                                    bool blackWin,
                                                    bool draw,
                                                    Dictionary<string, PlayerAggregatedStats> dictPlayersStats)
        {
            if (!string.IsNullOrEmpty(player))
            {
                if (!dictPlayersStats.ContainsKey(player))
                {
                    dictPlayersStats.Add(player, new PlayerAggregatedStats());
                }

                PlayerAggregatedStats aggreg = dictPlayersStats[player];
                aggreg.TotalStats.GameCount++;
                if (playedWhite)
                {
                    UpdateColorStats(aggreg.WhiteStats, whiteWin, blackWin, draw);
                }
                else
                {
                    UpdateColorStats(aggreg.BlackStats, blackWin, whiteWin, draw);
                }

                aggreg.UpdateTotalScores();
            }
        }

        /// <summary>
        /// Updates player stats in the passed stats object.
        /// This will be called to update player stats 
        /// for one color. 
        /// </summary>
        /// <param name="stats"></param>
        /// <param name="win"></param>
        /// <param name="loss"></param>
        /// <param name="draw"></param>
        private static void UpdateColorStats(PlayerStats stats,
                                             bool win,
                                             bool loss,
                                             bool draw)
        {
            stats.GameCount++;
            stats.Wins += win ? 1 : 0;
            stats.Losses += loss ? 1 : 0;
            stats.Draws += draw ? 1 : 0;
        }

        /// <summary>
        /// Checks the type of the result as encoded in the passed string.
        /// Returns true if this is white win, black win or draw.
        /// Otherwise returns false.
        /// </summary>
        /// <param name="sResult"></param>
        /// <param name="whiteWin"></param>
        /// <param name="blackWin"></param>
        /// <param name="draw"></param>
        /// <returns></returns>
        private static bool GetResultFromString(string sResult, out bool whiteWin, out bool blackWin, out bool draw)
        {
            bool valid = false;

            whiteWin = false; blackWin = false; draw = false;

            switch (sResult)
            {
                case Constants.PGN_WHITE_WIN_RESULT:
                case Constants.PGN_WHITE_WIN_RESULT_EX:
                    whiteWin = true;
                    break;
                case Constants.PGN_BLACK_WIN_RESULT:
                case Constants.PGN_BLACK_WIN_RESULT_EX:
                    blackWin = true;
                    break;
                case Constants.PGN_DRAW_RESULT:
                case Constants.PGN_DRAW_SHORT_RESULT:
                    draw = true;
                    break;
            }

            return valid;
        }
    }
}

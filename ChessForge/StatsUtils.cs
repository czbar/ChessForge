using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChessPosition;

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
            StatsData stats = new StatsData();
            GetResultsStats(scope, stats);
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
        public static void GetResultsStats(OperationScope scope, StatsData stats)
        {
            if (scope == OperationScope.CHAPTER)
            {
                if (AppState.ActiveChapter != null)
                {
                    GetChapterResultsStats(AppState.ActiveChapter, stats);

                    stats.ChapterCount = 1;
                    stats.GameCount = AppState.ActiveChapter.GetModelGameCount();
                    stats.ExerciseCount = AppState.ActiveChapter.GetExerciseCount();
                }
            }
            else if (scope == OperationScope.WORKBOOK && AppState.Workbook != null)
            {
                bool hasCommonPlayer_1 = true;
                bool hasCommonPlayer_2 = true;

                bool firstChapter = true;

                foreach (Chapter chapter in AppState.Workbook.Chapters)
                {
                    stats.ChapterCount++;
                    stats.GameCount += chapter.GetModelGameCount();
                    stats.ExerciseCount += chapter.GetExerciseCount();

                    StatsData chapterStats = new StatsData();
                    if (GetChapterResultsStats(chapter, chapterStats))
                    {
                        stats.WhiteWins += chapterStats.WhiteWins;
                        stats.BlackWins += chapterStats.BlackWins;
                        stats.Draws += chapterStats.Draws;

                        if (firstChapter)
                        {
                            stats.CommonPlayer_1 = chapterStats.CommonPlayer_1;
                            stats.CommonPlayer_2 = chapterStats.CommonPlayer_2;
                            firstChapter = false;
                        }
                        else
                        {
                            // check if we keep the current "common players"
                            if (hasCommonPlayer_1 && stats.CommonPlayer_1 != chapterStats.CommonPlayer_1 && stats.CommonPlayer_1 != chapterStats.CommonPlayer_2)
                            {
                                stats.CommonPlayer_1 = null;
                                hasCommonPlayer_1 = false;
                            }
                            if (hasCommonPlayer_2 && stats.CommonPlayer_2 != chapterStats.CommonPlayer_1 && stats.CommonPlayer_2 != chapterStats.CommonPlayer_2)
                            {
                                stats.CommonPlayer_2 = null;
                                hasCommonPlayer_2 = false;
                            }
                        }
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
        private static bool GetChapterResultsStats(Chapter chapter, StatsData stats)
        {
            Dictionary<string, int> playersGameCounts = new Dictionary<string, int>();

            int gamesCount = chapter.ModelGames.Count;

            try
            {
                foreach (Article article in chapter.ModelGames)
                {
                    UpdatePlayerGamesDictionary(playersGameCounts, article);
                    GetResultFromString(article.Tree.Header.GetResult(out _), out bool whiteWin, out bool blackWin, out bool draw);
                    if (whiteWin)
                    {
                        stats.WhiteWins++;
                    }
                    else if (blackWin)
                    {
                        stats.BlackWins++;
                    }
                    else if (draw)
                    {
                        stats.Draws++;
                    }
                }
            }
            catch
            {
            }

            stats.CommonPlayer_1 = null;
            stats.CommonPlayer_2 = null;

            // find any players that were found in all games, they can only be 0, 1 or 2 such players
            // they appearance number must be exactly gamesCount 'coz we avoided dupes in the logic above
            foreach (KeyValuePair<string, int> keyValue in playersGameCounts)
            {
                if (keyValue.Value == gamesCount)
                {
                    if (string.IsNullOrEmpty(stats.CommonPlayer_1))
                    {
                        stats.CommonPlayer_1 = keyValue.Key;
                    }
                    else
                    {
                        stats.CommonPlayer_2 = keyValue.Key;
                    }
                }
            }

            if (!string.IsNullOrEmpty(stats.CommonPlayer_1))
            {
                GetPlayerResultsStats(chapter, stats.CommonPlayer_1, stats.Player_1_Stats);
            }

            if (!string.IsNullOrEmpty(stats.CommonPlayer_2))
            {
                GetPlayerResultsStats(chapter, stats.CommonPlayer_2, stats.Player_2_Stats);
            }

            return gamesCount > 0;
        }

        /// <summary>
        /// Updates the dictionary holding th enumber of games played by each player.
        /// </summary>
        /// <param name="playersGameCounts"></param>
        /// <param name="article"></param>
        private static void UpdatePlayerGamesDictionary(Dictionary<string, int> playersGameCounts, Article article)
        {
            string whitePlayer = article.Tree.Header.GetWhitePlayer(out _).Trim();
            string blackPlayer = article.Tree.Header.GetBlackPlayer(out _).Trim();

            // update player stats
            if (!playersGameCounts.ContainsKey(whitePlayer))
            {
                playersGameCounts.Add(whitePlayer, 1);
            }
            else
            {
                playersGameCounts[whitePlayer]++;
            }

            // handle the second player only if different than the previous one,
            // otherwise we may get into some unwanted cases when a player plays vs themselves
            if (!playersGameCounts.ContainsKey(blackPlayer))
            {
                playersGameCounts.Add(blackPlayer, 1);
            }
            else
            {
                playersGameCounts[blackPlayer]++;
            }

        }

        /// <summary>
        /// Collect results for the specified player in the passed chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="player"></param>
        /// <param name="stats"></param>
        private static void GetPlayerResultsStats(Chapter chapter, string player, PlayerScoresStats stats)
        {
            foreach (Article article in chapter.ModelGames)
            {
                string whitePlayer = article.Tree.Header.GetWhitePlayer(out _).Trim();
                string blackPlayer = article.Tree.Header.GetBlackPlayer(out _).Trim();

                GetResultFromString(article.Tree.Header.GetResult(out _), out bool whiteWin, out bool blackWin, out bool draw);

                if (whitePlayer == player)
                {
                    UpdatePlayerStats(stats, true, whiteWin, blackWin, draw);
                }

                if (blackPlayer == player)
                {
                    UpdatePlayerStats(stats, false, whiteWin, blackWin, draw);
                    if (whitePlayer == blackPlayer)
                    {
                        // this is an anomaly when we have the same name as white and black
                        // we will adjust down the total for this case
                        stats.GameCount--;
                    }
                }
            }
        }

        /// <summary>
        /// Updates player stats with a single result
        /// </summary>
        /// <param name="stats"></param>
        /// <param name="isWhite"></param>
        /// <param name="whiteWin"></param>
        /// <param name="blackWin"></param>
        /// <param name="draw"></param>
        private static void UpdatePlayerStats(PlayerScoresStats stats, bool isWhite, bool whiteWin, bool blackWin, bool draw)
        {
            if (isWhite)
            {
                stats.WhiteScoreStats.GameCount++;
                if (whiteWin)
                {
                    stats.WhiteScoreStats.Wins++;
                }
                else if (blackWin)
                {
                    stats.WhiteScoreStats.Losses++;
                }
                else if (draw)
                {
                    stats.WhiteScoreStats.Draws++;
                }
            }
            else
            {
                stats.BlackScoreStats.GameCount++;
                if (blackWin)
                {
                    stats.BlackScoreStats.Wins++;
                }
                else if (blackWin)
                {
                    stats.BlackScoreStats.Losses++;
                }
                else if (draw)
                {
                    stats.BlackScoreStats.Draws++;
                }
            }

            stats.SumupColorTotals();
        }

        /// <summary>
        /// Checks the type of the result as encoded in the passed string.
        /// Returns true if this white win, black win or draw.
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

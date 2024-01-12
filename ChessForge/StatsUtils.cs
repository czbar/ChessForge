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
        public static void GetResultsStats(OperationScope scope, 
                                    out int whiteWins, 
                                    out int blackWins, 
                                    out int draws,
                                    out string commonPlayer_1, 
                                    out string commonPlayer_2)
        {
            whiteWins = 0;
            blackWins = 0;
            draws = 0;

            commonPlayer_1 = null;
            commonPlayer_2 = null;

            if (scope == OperationScope.CHAPTER)
            {
                if (AppState.ActiveChapter != null)
                {
                    GetChapterResultsStats(AppState.ActiveChapter, out whiteWins, out blackWins, out draws, out commonPlayer_1, out commonPlayer_2);
                }
            }
            else if (scope == OperationScope.WORKBOOK && AppState.Workbook != null)
            {
                bool hasCommonPlayer_1 = true;
                bool hasCommonPlayer_2 = true;

                bool firstChapter = true;

                foreach (Chapter chapter in AppState.Workbook.Chapters)
                {
                    if (GetChapterResultsStats(chapter, out int whiteChapterWins, out int blackChapterWins, out int chapterDraws,
                                            out string chapterPlayer_1, out string chapterPlayer_2))
                    {
                        whiteWins += whiteChapterWins;
                        blackWins += blackChapterWins;
                        draws += chapterDraws;

                        if (firstChapter)
                        {
                            commonPlayer_1 = chapterPlayer_1;
                            commonPlayer_2 = chapterPlayer_2;
                            firstChapter = false;
                        }
                        else
                        {
                            // check if we keep the current "common players"
                            if (hasCommonPlayer_1 && commonPlayer_1 != chapterPlayer_1 && commonPlayer_1 != chapterPlayer_2)
                            {
                                commonPlayer_1 = null;
                                hasCommonPlayer_1 = false;
                            }
                            if (hasCommonPlayer_2 && commonPlayer_2 != chapterPlayer_1 && commonPlayer_2 != chapterPlayer_2)
                            {
                                commonPlayer_2 = null;
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
        private static bool GetChapterResultsStats(Chapter chapter,
                                            out int whiteWins,
                                            out int blackWins,
                                            out int draws,
                                            out string commonPlayer_1, 
                                            out string commonPlayer_2)
        {
            whiteWins = 0;
            blackWins = 0;
            draws = 0;

            Dictionary<string, int> _playersGameCounts = new Dictionary<string, int>();

            int gamesCount = chapter.ModelGames.Count;

            try
            {
                foreach (Article article in chapter.ModelGames)
                {
                    switch (article.Tree.Header.GetResult(out _))
                    {
                        case Constants.PGN_WHITE_WIN_RESULT:
                        case Constants.PGN_WHITE_WIN_RESULT_EX:
                            whiteWins++;
                            break;
                        case Constants.PGN_BLACK_WIN_RESULT:
                        case Constants.PGN_BLACK_WIN_RESULT_EX:
                            blackWins++;
                            break;
                        case Constants.PGN_DRAW_RESULT:
                        case Constants.PGN_DRAW_SHORT_RESULT:
                            draws++;
                            break;
                    }

                    string whitePlayer = article.Tree.Header.GetWhitePlayer(out _).Trim();
                    string blackPlayer = article.Tree.Header.GetBlackPlayer(out _).Trim();

                    // update player stats
                    if (!_playersGameCounts.ContainsKey(whitePlayer))
                    {
                        _playersGameCounts.Add(whitePlayer, 1);
                    }
                    else
                    {
                        _playersGameCounts[whitePlayer]++;
                    }

                    // handle the second player only if different than the previous one,
                    // otherwise we may get into some unwanted cases when a player plays vs themselves
                    if (!_playersGameCounts.ContainsKey(blackPlayer))
                    {
                        _playersGameCounts.Add(blackPlayer, 1);
                    }
                    else
                    {
                        _playersGameCounts[blackPlayer]++;
                    }
                }
            }
            catch
            {
            }

            commonPlayer_1 = null;
            commonPlayer_2 = null;

            // find any players that were found in all games, they can only be 0, 1 or 2 such players
            // they appearance number must be exactly gamesCount 'coz we avoided dupes in the logic above
            foreach (KeyValuePair<string, int> keyValue in _playersGameCounts)
            {
                if (keyValue.Value == gamesCount)
                {
                    if (string.IsNullOrEmpty(commonPlayer_1))
                    {
                        commonPlayer_1 = keyValue.Key;
                    }
                    else
                    {
                        commonPlayer_2 = keyValue.Key;
                    }
                }
            }

            return gamesCount > 0;
        }

    }
}

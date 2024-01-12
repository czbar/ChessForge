using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessForge
{
    /// <summary>
    /// Holds statistics of a Chapter or a Workbook.
    /// </summary>
    public class StatsData
    {
        /// <summary>
        /// Number of chapters included 
        /// </summary>
        public int ChapterCount;

        /// <summary>
        /// Total number of Games
        /// </summary>
        public int GameCount;

        /// <summary>
        /// Total number of Exercises
        /// </summary>
        public int ExerciseCount;

        /// <summary>
        /// Games won by White
        /// </summary>
        public int WhiteWins;

        /// <summary>
        /// Games won by Black
        /// </summary>
        public int BlackWins;

        /// <summary>
        /// Number of games drawn.
        /// </summary>
        public int Draws;

        /// <summary>
        /// Name of the player if there was one found in every game
        /// </summary>
        public string CommonPlayer_1;

        /// <summary>
        /// Name of another player if there was one found in every game
        /// </summary>
        public string CommonPlayer_2;

        /// <summary>
        /// Statistics for player 1.
        /// </summary>
        PlayerScoresStats Player_1_Stats = new PlayerScoresStats();

        /// <summary>
        /// Statistics for player 2.
        /// </summary>
        PlayerScoresStats Player_2_Stats = new PlayerScoresStats();

        /// <summary>
        /// Statistics for player 1 with White
        /// </summary>
        PlayerScoresStats Player_1_WhiteStats = new PlayerScoresStats();

        /// <summary>
        /// Statistics for player 2 with White
        /// </summary>
        PlayerScoresStats Player_2_WhiteStats = new PlayerScoresStats();


        /// <summary>
        /// Statistics for player 1 with Black
        /// </summary>
        PlayerScoresStats Player_1_BlackStats = new PlayerScoresStats();

        /// <summary>
        /// Statistics for player 2 with Black
        /// </summary>
        PlayerScoresStats Player_2_BlackStats = new PlayerScoresStats();
    }

    /// <summary>
    /// Holds statistics of an individual player
    /// </summary>
    public class PlayerScoresStats
    {
        /// <summary>
        /// Number of games played
        /// </summary>
        public int GameCount;

        /// <summary>
        /// Number of wins
        /// </summary>
        public int Wins;

        /// <summary>
        /// Number of losses
        /// </summary>
        public int Losses;

        /// <summary>
        /// Number of draws
        /// </summary>
        public int Draws;
    }
}

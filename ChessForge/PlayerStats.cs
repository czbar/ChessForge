using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Holds statistics of an individual player
    /// </summary>
    public class PlayerStats
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

    /// <summary>
    /// Holds the PlayerStats objects for total stats
    /// as well as white and black stats.
    /// </summary>
    public class PlayerAggregatedStats
    {
        public PlayerStats TotalStats = new PlayerStats();
        public PlayerStats WhiteStats = new PlayerStats();
        public PlayerStats BlackStats = new PlayerStats();

        /// <summary>
        /// Works out total result stats by adding results for white and black.
        /// Note, we count total games elsewhere due to a possibility of
        /// getting the wrong number if there are games where the same player is 
        /// listed as both white and black.
        /// </summary>
        public void UpdateTotalScores()
        {
            TotalStats.Wins = WhiteStats.Wins + BlackStats.Wins;
            TotalStats.Losses = WhiteStats.Losses + BlackStats.Losses;
            TotalStats.Draws = WhiteStats.Draws + BlackStats.Draws;
        }
    }
}

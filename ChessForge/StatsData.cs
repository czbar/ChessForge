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
    }
}

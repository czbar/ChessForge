using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAccess
{
    /// <summary>
    /// Sets filtering parameters for games download
    /// </summary>
    public class GamesFilter
    {
        /// <summary>
        /// Name of the user/account
        /// </summary>
        public string User;

        /// <summary>
        /// Whether to only download games newer than the last downloaded game.
        /// </summary>
        public bool NewGamesOnly;

        /// <summary>
        /// Maximum number of games to download.
        /// </summary>
        public int MaxGames;

        /// <summary>
        /// Whether to use the specified Start Date or to ignore it
        /// </summary>
        public bool UseStartDate;

        /// <summary>
        /// The earliest date for which to look for the games.
        /// </summary>
        public DateTime? StartDate;
    }
}

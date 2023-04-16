using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        /// Maximum number of games to download.
        /// </summary>
        public int MaxGames;

        /// <summary>
        /// The earliest date for which to look for the games.
        /// </summary>
        public DateTime? StartDate;

        /// <summary>
        /// The end date at which to look for the games.
        /// </summary>
        public DateTime? EndDate;
    }
}

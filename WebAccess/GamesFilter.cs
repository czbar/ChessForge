using ChessPosition;
using System;

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
        /// Whether StartDate and EndDate are UTC.
        /// True if UTC, false if local.
        /// </summary>
        public bool IsUtcTimes = false;

        /// <summary>
        /// The earliest date for which to look for the games.
        /// </summary>
        public DateTime? StartDate;

        /// <summary>
        /// The end date at which to look for the games.
        /// </summary>
        public DateTime? EndDate;

        /// <summary>
        /// Returns the StartDate as Epoch milliseconds.
        /// </summary>
        public long? StartDateEpochTicks
        {
            get
            {
                return DateUtils.ConvertDateTimeToEpochTicks(StartDate, IsUtcTimes);
            }
        }

        /// <summary>
        /// Returns the EndDate as Epoch milliseconds.
        /// </summary>
        public long? EndDateEpochTicks
        {
            get
            {
                if (EndDate.HasValue)
                {
                    return DateUtils.ConvertDateTimeToEpochTicks(EndDate.Value.AddDays(1).AddMilliseconds(-1), IsUtcTimes);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}

using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
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
                return ConvertDateTimeToEpochTicks(StartDate);
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
                    return ConvertDateTimeToEpochTicks(EndDate.Value.AddDays(1).AddMilliseconds(-1));
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Converts the passed datetime into Unix epoch milliseconds.
        /// Depending on the value of IsUtcTimes considers the passed value
        /// as UTC or Local.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private long? ConvertDateTimeToEpochTicks(DateTime? dt)
        {
            if (dt == null)
            {
                return null;
            }
            else
            {
                DateTime dtToConvert = new DateTime(dt.Value.Year, dt.Value.Month, dt.Value.Day, 
                                                    dt.Value.Hour, dt.Value.Minute, dt.Value.Second, dt.Value.Millisecond, 
                                                    IsUtcTimes ? DateTimeKind.Utc : DateTimeKind.Local);

                return EncodingUtils.ConvertDateToEpoch(dtToConvert);
            }
        }
    }
}

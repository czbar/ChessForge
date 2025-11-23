using System;

namespace ChessPosition
{
    /// <summary>
    /// Utilities for date and time operations.
    /// </summary>
    public class DateUtils
    {
        /// <summary>
        /// Removes the time component from the DateTime object.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static DateTime ClearTime(DateTime? dt)
        {
            if (dt.HasValue)
            {
                return new DateTime(dt.Value.Year, dt.Value.Month, dt.Value.Day, 0, 0, 0, dt.Value.Kind);
            }
            else
            {
                throw new ArgumentNullException("dt");
            }
        }


        /// <summary>
        /// Converts the passed datetime into Unix epoch milliseconds.
        /// Depending on the value of isUtc considers the passed value
        /// as UTC or Local.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static long? ConvertDateTimeToEpochTicks(DateTime? dt, bool isUtc)
        {
            if (dt == null)
            {
                return null;
            }
            else
            {
                DateTime dtToConvert = new DateTime(dt.Value.Year, dt.Value.Month, dt.Value.Day,
                                                    dt.Value.Hour, dt.Value.Minute, dt.Value.Second, dt.Value.Millisecond,
                                                    isUtc ? DateTimeKind.Utc : DateTimeKind.Local);

                return EncodingUtils.ConvertDateToEpoch(dtToConvert);
            }
        }
    }
}

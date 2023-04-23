using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessPosition
{
    /// <summary>
    /// Encoding/Decoding utilities
    /// </summary>
    public class EncodingUtils
    {
        /// <summary>
        /// Base64 encoding of text.
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static string Base64Encode(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return "";
            }

            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>
        /// Decoding Base64 encoded text.
        /// </summary>
        /// <param name="base64EncodedData"></param>
        /// <returns></returns>
        public static string Base64Decode(string base64EncodedData)
        {
            if (string.IsNullOrEmpty(base64EncodedData))
            {
                return "";
            }

            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        /// <summary>
        /// Converts the data to epoch Unix time
        /// If this is for the end of the day, then takes the start of the next day and subtracts a millisecond.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static long? ConvertDateToEpoch(DateTime? date, bool dayStart)
        {
            long? millisec = null;

            if (date != null)
            {
                DateTime dt;
                if (dayStart)
                {
                    dt = date.Value;
                }
                else
                {
                    dt = date.Value.AddDays(1).AddMilliseconds(-1);
                }
                DateTimeOffset dateTimeOffset = dt.ToUniversalTime();
                millisec = dateTimeOffset.ToUnixTimeMilliseconds();
            }

            return millisec;
        }
    }
}

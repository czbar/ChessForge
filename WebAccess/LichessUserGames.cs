using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAccess
{
    public class LichessUserGames
    {
        // default number of games to download if not specified.
        public static int DEFAULT_DOWNLOAD_GAME_COUNT = 20;

        // urls for downloading user games from lichess
        private static string _urlLichessUserGames = "https://lichess.org/api/games/user/{0}";

        /// <summary>
        /// Handler for the UserGamesReceived event
        /// </summary>
        public static event EventHandler<WebAccessEventArgs> UserGamesReceived;

        /// <summary>
        /// Downloads user games from lichess.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static async Task<string> GetLichessUserGames(GamesFilter filter)
        {
            WebAccessEventArgs eventArgs = new WebAccessEventArgs();
            try
            {
                string url = BuildLichessUserGamesUrl(filter);
                var response = await RestApiRequest.GameImportClient.GetAsync(url);
                using (var fs = new MemoryStream())
                {
                    await response.Content.CopyToAsync(fs);
                    fs.Position = 0;
                    StreamReader sr = new StreamReader(fs);
                    eventArgs.TextData = sr.ReadToEnd();
                }
                eventArgs.Success = true;
                UserGamesReceived?.Invoke(null, eventArgs);
                return "";
            }
            catch (Exception ex)
            {
                eventArgs.Success = true;
                eventArgs.Message = ex.Message;
                UserGamesReceived?.Invoke(null, eventArgs);
                return "";
            }
        }

        /// <summary>
        /// Builds URL for downloading games from Lichess.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private static string BuildLichessUserGamesUrl(GamesFilter filter)
        {
            bool hasParam = false;

            StringBuilder url = new StringBuilder();
            url.Append(String.Format(_urlLichessUserGames, filter.User));
            int gamesCount = Math.Max(DEFAULT_DOWNLOAD_GAME_COUNT, filter.MaxGames);
            if (gamesCount > 0)
            {
                url.Append("?" + "max=" + gamesCount.ToString());
                hasParam = true;
            }

            long? startTime = ConvertDateToEpoch(filter.StartDate, true);
            long? endTime = ConvertDateToEpoch(filter.EndDate, false);

            if (startTime.HasValue)
            {
                url.Append(hasParam ? "&" : "?");
                hasParam = true;
                url.Append("since=" + startTime.Value.ToString());
            }

            if (endTime.HasValue)
            {
                url.Append(hasParam ? "&" : "?");
                hasParam = true;
                url.Append("until=" + endTime.Value.ToString());
            }

            return url.ToString();
        }

        /// <summary>
        /// Converts the data to epoch Unix time
        /// If this is for the end of the day, then takes the start of the next day and subtracts a millisecond.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static long? ConvertDateToEpoch(DateTime? date, bool dayStart)
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

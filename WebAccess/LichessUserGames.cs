using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebAccess
{
    public class LichessUserGames
    {
        // urls for downloading user games from lichess
        private static string _urlLichessUserGames = "https://lichess.org/api/games/user/{0}";

        // REST parameter specifying the type of games to include in the download
        private static string _perfTypeParameter = "perfType=ultraBullet,bullet,blitz,rapid,classical,correspondence";

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
            eventArgs.GamesFilter = filter;
            try
            {
                string url = BuildLichessUserGamesUrl(filter);
                HttpClient httpClient = RestApiRequest.GameImportClient;
                httpClient.DefaultRequestHeaders.Add("User-Agent", RestApiRequest.UserAgent);
                var response = await httpClient.GetAsync(url);
                int statusCode = RestApiRequest.GetResponseCode(response.ToString());
                if (statusCode != 200)
                {
                    throw new Exception(RestApiRequest.STATUS_CODE + statusCode.ToString());
                }
                using (var fs = new MemoryStream())
                {
                    await response.Content.CopyToAsync(fs);
                    fs.Position = 0;
                    StreamReader sr = new StreamReader(fs);
                    eventArgs.TextData = sr.ReadToEnd();
                }
                eventArgs.GameData = PgnMultiGameParser.ParsePgnMultiGameText(eventArgs.TextData);
                eventArgs.Success = true;
                UserGamesReceived?.Invoke(null, eventArgs);
                return "";
            }
            catch (Exception ex)
            {
                eventArgs.Success = false;
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

            url.Append("?" + _perfTypeParameter);
            hasParam = true;

            int gamesCount = filter.MaxGames;
            if (gamesCount > 0)
            {
                url.Append(hasParam ? "&" : "?");
                url.Append("max=" + gamesCount.ToString());
                hasParam = true;
            }

            long? startTime = EncodingUtils.ConvertDateToEpoch(filter.StartDate, true);
            long? endTime = EncodingUtils.ConvertDateToEpoch(filter.EndDate, false);

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

    }
}

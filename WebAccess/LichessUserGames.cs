using GameTree;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebAccess
{
    public class LichessUserGames
    {
        // urls for downloading user games from lichess
        private static string _urlLichessUserGames = UrlTarget.LichessUserGames;

        // REST parameter specifying the type of games to include in the download
        //private static string _perfTypeParameter = "perfType=ultraBullet,bullet,blitz,rapid,classical,correspondence";

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
                RestApiRequest.SetHttpClientLichessDefaultHeaders(httpClient);

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
                eventArgs.GameData = PgnMultiGameParser.ParsePgnMultiGameText(eventArgs.TextData, out int variantGamesCount);
                eventArgs.Success = true;

                // if there are variant games and the filter specifies MaxGames 
                // include the count of variant games in the event args so that we can report it to the user.
                // Otherwise the user may be confused when they see that the number of downloaded games is smaller than the MaxGames specified in the filter,
                // without understanding that some games were filtered out because they are variant games.
                if (variantGamesCount > 0 && filter.MaxGames > 0)
                {
                    eventArgs.VariantGamesCount = variantGamesCount;
                }

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

            // TODO: for now we are downloading all games and then filtering out variant games on client side, but if lichess ever adds a REST parameter to exclude variant games, we should use it to avoid downloading unnecessary data. 
            // Setting _perfTypeParameter seems to block the download when lichess experiencing problems with their database.
            
            //url.Append("?" + _perfTypeParameter);
            //hasParam = true;

            int gamesCount = filter.MaxGames;
            if (gamesCount > 0)
            {
                url.Append(hasParam ? "&" : "?");
                url.Append("max=" + gamesCount.ToString());
                hasParam = true;
            }

            if (filter.StartDate.HasValue)
            {
                url.Append(hasParam ? "&" : "?");
                hasParam = true;
                url.Append("since=" + filter.StartDateEpochTicks.Value.ToString());
            }

            if (filter.EndDate.HasValue)
            {
                url.Append(hasParam ? "&" : "?");
                hasParam = true;
                url.Append("until=" + filter.EndDateEpochTicks.Value.ToString());
            }

            return url.ToString();
        }

    }
}

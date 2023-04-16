using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Text;

namespace WebAccess
{
    /// <summary>
    /// Manages Game download from lichess.
    /// </summary>
    public class GameDownload
    {
        // default number of games to download if not specified.
        public static int DEFAULT_DOWNLOAD_GAME_COUNT = 20;

        // urls for downloading user games from lichess
        private static string _urlLichessUserGames = "https://lichess.org/api/games/user/{0}";

        // urls for downloading user games from chesscom
        private static string _urlChesscomUserGames = "https://api.chess.com/pub/player/{0}/games/{$1}/{$2}/pgn";

        /// <summary>
        /// Max number of games to keep in cache.
        /// </summary>
        private static int GAME_CACHE_SIZE = 100;

        /// <summary>
        /// Ids of the currently cached games
        /// </summary>
        private static Queue<string> _queueGameIds = new Queue<string>();

        /// <summary>
        /// Cached games.
        /// </summary>
        private static Dictionary<string, string> _dictCachedGames = new Dictionary<string, string>();

        /// <summary>
        /// Handler for the GameReceived event
        /// </summary>
        public static event EventHandler<WebAccessEventArgs> GameReceived;

        /// <summary>
        /// Handler for the UserGamesReceived event
        /// </summary>
        public static event EventHandler<WebAccessEventArgs> UserGamesReceived;

        /// <summary>
        /// Received text of the game.
        /// </summary>
        public static string GameText;

        /// <summary>
        /// Clears the cache queue and dictionary
        /// </summary>
        public void CleareGameCache()
        {
            _queueGameIds.Clear();
            _dictCachedGames.Clear();
        }

        /// <summary>
        /// Gets a game from lichess.org.
        /// </summary>
        /// <param name="gameId"></param>
        public static async Task<string> GetGame(string gameId)
        {
            WebAccessEventArgs eventArgs = new WebAccessEventArgs();
            try
            {
                if (_dictCachedGames.ContainsKey(gameId))
                {
                    GameText = _dictCachedGames[gameId];
                }
                else
                {
                    var response = await RestApiRequest.GameImportClient.GetAsync("https://lichess.org/game/export/" + gameId);
                    using (var fs = new MemoryStream())
                    {
                        await response.Content.CopyToAsync(fs);
                        fs.Position = 0;
                        StreamReader sr = new StreamReader(fs);
                        GameText = sr.ReadToEnd();
                        AddGameToCache(gameId, GameText);
                    }
                }
                eventArgs.Success = true;
                GameReceived?.Invoke(null, eventArgs);
                return GameText;
            }
            catch (Exception ex)
            {
                eventArgs.Success = true;
                eventArgs.Message = ex.Message;
                GameReceived?.Invoke(null, eventArgs);
                return "";
            }
        }

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

        /// <summary>
        /// Add game to cache.
        /// Delete the oldest game if the cache is full.
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="gameText"></param>
        private static void AddGameToCache(string gameId, string gameText)
        {
            try
            {
                if (_queueGameIds.Count >= GAME_CACHE_SIZE)
                {
                    string dequeuedGameId = _queueGameIds.Dequeue();
                    _dictCachedGames.Remove(dequeuedGameId);
                }
                _queueGameIds.Enqueue(gameId);
                _dictCachedGames[gameId] = gameText;
            }
            catch
            {
            }
        }

    }
}

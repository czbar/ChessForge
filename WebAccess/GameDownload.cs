using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WebAccess
{
    /// <summary>
    /// Manages Game download from lichess.
    /// </summary>
    public class GameDownload
    {
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
        /// Remove game from cache.
        /// This is will be called when the downloaded was not successfully parsed
        /// (which occasionally happens when corrupted data comes back from lichess).
        /// We don't want such game in the cache so that next time, the download is attempted again.
        /// </summary>
        /// <param name="gameId"></param>
        public static void RemoveFromCache(string gameId)
        {
            if (_dictCachedGames.ContainsKey(gameId))
            {
                _dictCachedGames.Remove(gameId);

                List<string> dequeued = new List<string>();
                try
                {
                    while (_queueGameIds.Count > 0)
                    {
                        string id = _queueGameIds.Dequeue();
                        if (id == gameId || _queueGameIds.Count == 0)
                        {
                            foreach (string d in dequeued)
                            {
                                _queueGameIds.Enqueue(d);
                            }
                            break;
                        }
                        else
                        {
                            dequeued.Insert(0, id);
                        }
                    }
                }
                catch
                {
                }
            }
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

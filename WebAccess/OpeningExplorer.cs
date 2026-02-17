using ChessForge;
using GameTree;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace WebAccess
{
    /// <summary>
    /// Manages querying lichess API for Opening stats.
    /// </summary>
    public class OpeningExplorer
    {
        /// <summary>
        /// Handler for the error in the Web response
        /// </summary>
        public static event EventHandler<WebAccessEventArgs> OpeningStatsErrorReceived;

        // max number of results to store in cache
        private static int STATS_CACHE_SIZE = 1000;

        // number of entries to remove when cache is full
        private static int COUNT_TO_FREE_ON_FULL = STATS_CACHE_SIZE / 5;

        /// Cached stats. The key is FEN.
        private static Dictionary<string, LichessOpeningsStats> _dictCachedStats = new Dictionary<string, LichessOpeningsStats>();

        /// Last touch times for the cached items. The key is FEN.
        private static Dictionary<string, long> _dictLastTouch = new Dictionary<string, long>();

        // the last requested fen
        private static string _lastRequestedFen;

        /// <summary>
        /// Lock for cache access
        /// </summary>
        private static object _lockCacheAccess = new object();

        /// <summary>
        /// Returns opening stats for the passed position
        /// or null if stats not found.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public static LichessOpeningsStats GetOpeningStats(TreeNode nd)
        {
            lock (_lockCacheAccess)
            {
                if (nd != null)
                {
                    string fen = FenParser.GenerateFenFromPosition(nd.Position);
                    if (_dictCachedStats.ContainsKey(fen))
                    {
                        _dictLastTouch[fen] = DateTime.Now.Ticks;
                        return _dictCachedStats[fen];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Requests Opening Stats from lichess
        /// </summary>
        /// <returns></returns>
        public static async void RequestOpeningStats(int treeId, TreeNode nd)
        {
            string fen = FenParser.GenerateFenFromPosition(nd.Position);
            _lastRequestedFen = fen;

            // event args will only be used on error/exception
            WebAccessEventArgs eventArgs = new WebAccessEventArgs();
            eventArgs.TreeId = treeId;
            eventArgs.NodeId = nd.NodeId;

            try
            {
                AppLog.Message(2, "HttpClient sending OpeningStats request for FEN: " + fen);

                HttpClient httpClient = RestApiRequest.OpeningStatsClient;
                httpClient.DefaultRequestHeaders.Add("User-Agent", RestApiRequest.UserAgentLichess);
                
                string request = "https://explorer.lichess.ovh/masters?" + "fen=" + fen;
                HttpResponseMessage response = await httpClient.GetAsync(request);
                string json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    LichessOpeningsStats stats = JsonConvert.DeserializeObject<LichessOpeningsStats>(json);

                    lock (_lockCacheAccess)
                    {
                        if (_dictCachedStats.Count >= STATS_CACHE_SIZE)
                        {
                            MakeRoomInCache();
                        }
                        _dictCachedStats[fen] = stats;
                        _dictLastTouch[fen] = DateTime.Now.Ticks;
                    }
                }
                else
                {
                    eventArgs.Success = false;
                    eventArgs.Message = json;
                    OpeningStatsErrorReceived?.Invoke(null, eventArgs);
                }
                AppLog.Message(2, "HttpClient received OpeningStats response for FEN: " + fen);
            }
            catch (Exception ex)
            {
                eventArgs.Success = false;
                eventArgs.Message = ex.Message;
                OpeningStatsErrorReceived?.Invoke(null, eventArgs);
                AppLog.Message("RequestOpeningStats()", ex);
            }
        }

        /// <summary>
        /// Resets last requested fen e.g. when closing a workbook or switching to tablebase
        /// </summary>
        public static void ResetLastRequestedFen()
        {
            _lastRequestedFen = "";
        }

        /// <summary>
        /// Remove some elements from the cache to make room for new ones.
        /// Caller must lock access with _lockCacheAccess object
        /// </summary>
        private static void MakeRoomInCache()
        {
            var keysToRemove = _dictLastTouch.OrderBy(x => x.Value).Select(x => x.Key).Take(COUNT_TO_FREE_ON_FULL);
            foreach (string key in keysToRemove)
            {
                _dictLastTouch.Remove(key);
                _dictCachedStats.Remove(key);
            }
        }
    }

    /// <summary>
    /// The class to deserialize the Lichess Opening Stats into.
    /// </summary>
    public class LichessOpeningsStats
    {
        public string White;
        public string Black;
        public LichessMoveStats[] Moves;
        public LichessTopGame[] TopGames;
        public LichessOpening Opening;
    }

    /// <summary>
    /// Move Stats class
    /// </summary>
    public class LichessMoveStats
    {
        public string Uci;
        public string San;
        public string AverageRating;
        public string White;
        public string Draws;
        public string Black;
        public object Game;
    }

    /// <summary>
    /// Game metadata class
    /// </summary>
    public class LichessTopGame
    {
        public string Uci;
        public string Id;
        public string Winner;
        public LichessPlayer Black;
        public LichessPlayer White;
        public string Year;
        public string Month;
    }

    /// <summary>
    /// Player info.
    /// </summary>
    public class LichessPlayer
    {
        public string Name;
        public string Rating;
    }

    /// <summary>
    /// Opening code and name
    /// </summary>
    public class LichessOpening
    {
        public string Eco;
        public string Name;
    }
}

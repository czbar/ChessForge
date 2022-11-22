using ChessPosition;
using ChessPosition.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAccess;

namespace WebAccess
{
    /// <summary>
    /// Manages querying lichess API for Opening stats.
    /// </summary>
    public class OpeningExplorer
    {
        /// <summary>
        /// Gets the version number of Chess Forge currently available from Source Forge.
        /// </summary>
        /// <returns></returns>
        public static async Task<string> OpeningStats(string fen)
        {
            var json = await RestApiRequest.Client.GetStringAsync("https://explorer.lichess.ovh/masters?" + "fen=" + fen);
            LichessOpeningsStats stats = JsonConvert.DeserializeObject<LichessOpeningsStats>(json);
            return json;
        }
    }

    /// <summary>
    /// The class to deserialize the Lichess Opening Stats into.
    /// </summary>
    public class LichessOpeningsStats
    {
        public string white;
        public string black;
        public LichessMoveStats[] moves;
        public LichessTopGame[] topgames;
        public LichessOpening opening;
    }

    /// <summary>
    /// Move Stats class
    /// </summary>
    public class LichessMoveStats
    {
        public string uci;
        public string san;
        public string averageRating;
        public string white;
        public string draws;
        public string black;
        public string game;
    }

    /// <summary>
    /// Game metadata class
    /// </summary>
    public class LichessTopGame
    {
        public string uci;
        public string id;
        public string winner;
        public LichessPlayer black;
        public LichessPlayer white;
        public string year;
        public string month;
    }

    /// <summary>
    /// Player info.
    /// </summary>
    public class LichessPlayer
    {
        public string name;
        public string rating;
    }

    /// <summary>
    /// Opening code and name
    /// </summary>
    public class LichessOpening
    {
        public string eco;
        public string name;
    }
}

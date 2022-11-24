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
        /// Handler for the DataReceived event
        /// </summary>
        public static event EventHandler DataReceived;

        /// <summary>
        /// Statistics and data received from Lichess
        /// </summary>
        public static LichessOpeningsStats Stats;

        /// <summary>
        /// Gets the version number of Chess Forge currently available from Source Forge.
        /// </summary>
        /// <returns></returns>
        public static async void OpeningStats(string fen)
        {
            WebAccessEventArgs eventArgs = new WebAccessEventArgs();
            try
            {
                var json = await RestApiRequest.Client.GetStringAsync("https://explorer.lichess.ovh/masters?" + "fen=" + fen);
                Stats = JsonConvert.DeserializeObject<LichessOpeningsStats>(json);
                eventArgs.Sucess = true;
                DataReceived?.Invoke(null, eventArgs);
            }
            catch
            {
                eventArgs.Sucess = false;
                DataReceived?.Invoke(null, eventArgs);
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
        public string Game;
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

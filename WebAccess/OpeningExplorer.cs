using ChessForge;
using ChessPosition;
using ChessPosition.Utils;
using GameTree;
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
        public static event EventHandler<WebAccessEventArgs> OpeningStatsReceived;

        /// <summary>
        /// Handler for the OpeningNameReceived event
        /// </summary>
        public static event EventHandler<WebAccessEventArgs> OpeningNameReceived;

        /// <summary>
        /// Requests Opening Stats from lichess
        /// </summary>
        /// <returns></returns>
        public static async void RequestOpeningStats(int treeId, TreeNode nd)
        {
            string fen = FenParser.GenerateFenFromPosition(nd.Position);
            WebAccessEventArgs eventArgs = new WebAccessEventArgs();
            eventArgs.TreeId= treeId;
            eventArgs.NodeId = nd.NodeId;
            try
            {
                AppLog.Message(2, "HttpClient sending OpeningStats request for FEN: " + fen);
                var json = await RestApiRequest.OpeningStatsClient.GetStringAsync("https://explorer.lichess.ovh/masters?" + "fen=" + fen);
                eventArgs.OpeningStats = JsonConvert.DeserializeObject<LichessOpeningsStats>(json);
                eventArgs.Success = true;
                OpeningStatsReceived?.Invoke(null, eventArgs);
                AppLog.Message(2, "HttpClient received OpeningStats response for FEN: " + fen);
            }
            catch (Exception ex) 
            {
                eventArgs.Success = false;
                eventArgs.Message = ex.Message;
                OpeningStatsReceived?.Invoke(null, eventArgs);
                AppLog.Message("RequestOpeningStats()", ex);
            }
        }

        /// <summary>
        /// Calls opening stats for the purpose of obtaining
        /// the Opening Name.
        /// </summary>
        /// <param name="treeId"></param>
        /// <param name="nd"></param>
        public static async void RequestOpeningName(TreeNode nd)
        {
            string fen = FenParser.GenerateFenFromPosition(nd.Position);
            WebAccessEventArgs eventArgs = new WebAccessEventArgs();
            eventArgs.NodeId = nd.NodeId;
            try
            {
                AppLog.Message(2, "HttpClient sending OpeningName request for FEN: " + fen);
                var json = await RestApiRequest.OpeningNameClient.GetStringAsync("https://explorer.lichess.ovh/masters?" + "fen=" + fen);
                LichessOpeningsStats stats = JsonConvert.DeserializeObject<LichessOpeningsStats>(json);
                eventArgs.Success = true;
                eventArgs.OpeningStats = stats;
                OpeningNameReceived?.Invoke(null, eventArgs);
                AppLog.Message(2, "HttpClient received OpeningName response for FEN: " + fen);
            }
            catch (Exception ex)
            {
                eventArgs.Success = false;
                eventArgs.Message = ex.Message;
                OpeningNameReceived?.Invoke(null, eventArgs);
                AppLog.Message("RequestOpeningName()", ex);
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

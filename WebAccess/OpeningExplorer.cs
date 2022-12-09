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
        public static event EventHandler<WebAccessEventArgs> DataReceived;

        /// <summary>
        /// Handler for the OpeningNameReceived event
        /// </summary>
        public static event EventHandler<WebAccessEventArgs> OpeningNameReceived;

        /// <summary>
        /// Statistics and data received from Lichess
        /// </summary>
        public static LichessOpeningsStats Stats;

        /// <summary>
        /// Requests Opening Stats from lichess
        /// </summary>
        /// <returns></returns>
        public static async void OpeningStats(int treeId, TreeNode nd)
        {
            string fen = FenParser.GenerateFenFromPosition(nd.Position);
            WebAccessEventArgs eventArgs = new WebAccessEventArgs();
            eventArgs.TreeId= treeId;
            eventArgs.NodeId = nd.NodeId;
            try
            {
                var json = await RestApiRequest.Client.GetStringAsync("https://explorer.lichess.ovh/masters?" + "fen=" + fen);
                Stats = JsonConvert.DeserializeObject<LichessOpeningsStats>(json);
                eventArgs.Success = true;
                DataReceived?.Invoke(null, eventArgs);
            }
            catch(Exception ex) 
            {
                eventArgs.Success = false;
                eventArgs.Message = ex.Message;
                DataReceived?.Invoke(null, eventArgs);
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
                var json = await RestApiRequest.Client.GetStringAsync("https://explorer.lichess.ovh/masters?" + "fen=" + fen);
                LichessOpeningsStats stats = JsonConvert.DeserializeObject<LichessOpeningsStats>(json);
                eventArgs.Success = true;
                if (stats.Opening != null)
                {
                    eventArgs.Eco = stats.Opening.Eco;
                    eventArgs.OpeningName = stats.Opening.Name;
                }
                else
                {
                    eventArgs.Eco = null;
                    eventArgs.OpeningName = null;
                }
                OpeningNameReceived?.Invoke(null, eventArgs);
            }
            catch (Exception ex)
            {
                eventArgs.Success = false;
                eventArgs.Message = ex.Message;
                OpeningNameReceived?.Invoke(null, eventArgs);
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

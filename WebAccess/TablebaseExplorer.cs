using ChessForge;
using GameTree;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebAccess
{
    /// <summary>
    /// Manages querying lichess Tablebases.
    /// </summary>
    public class TablebaseExplorer
    {
        /// <summary>
        /// Handler for the DataReceived event
        /// </summary>
        public static event EventHandler<WebAccessEventArgs> TablebaseReceived;

        /// <summary>
        /// Statistics and data received from Lichess
        /// </summary>
        public static LichessTablebaseResponse Response;

        /// <summary>
        /// Requests Opening Stats from lichess
        /// </summary>
        /// <returns></returns>
        public static async void RequestTablebaseData(int treeId, TreeNode nd, bool force = false)
        {
            string fen = FenParser.GenerateFenFromPosition(nd.Position);
            WebAccessEventArgs eventArgs = new WebAccessEventArgs();
            eventArgs.TreeId = treeId;
            eventArgs.NodeId = nd.NodeId;
            try
            {
                AppLog.Message(2, "HttpClient sending Tablebase request for FEN: " + fen);

                HttpClient httpClient = RestApiRequest.TablebaseClient;
                httpClient.DefaultRequestHeaders.Add("User-Agent", RestApiRequest.UserAgentLichess);

                var json = await httpClient.GetStringAsync("http://tablebase.lichess.ovh/standard?" + "fen=" + fen);
                Response = JsonConvert.DeserializeObject<LichessTablebaseResponse>(json);
                eventArgs.Success = true;
                TablebaseReceived?.Invoke(null, eventArgs);
                AppLog.Message(2, "HttpClient received Tablebase response for FEN: " + fen);
            }
            catch (Exception ex)
            {
                eventArgs.Success = false;
                eventArgs.Message = ex.Message;
                TablebaseReceived?.Invoke(null, eventArgs);
                AppLog.Message("TablebaseRequest()", ex);
            }
        }
    }

    /// <summary>
    /// The lichess json response structure
    /// </summary>
    public class LichessTablebaseResponse
    {
        public bool Checkmate;
        public bool Stalemate;
        public bool Variant_win;
        public bool Variant_loss;
        public bool Insufficient_material;
        public int? dtz;
        public int? precise_dtz;
        public int? dtm;
        public string category;
        public LichessTablebaseMove[] Moves;
    }

    /// <summary>
    /// A move structure for the lichess response.
    /// </summary>
    public class LichessTablebaseMove
    {
        public string Uci;
        public string San;
        public bool Zeroing;
        public bool Checkmate;
        public bool Stalemate;
        public bool Variant_win;
        public bool Variant_loss;
        public bool Insufficient_material;
        public int? dtz;
        public int? precise_dtz;
        public int? dtm;
        public string category;
    }

    //
    // Documentation at https://github.com/lichess-org/lila-tablebase
    //
    //  {
    //    "dtz": 1, // dtz50'' with rounding or null if unknown
    //    "precise_dtz": 1, // dtz50'' (only if guaranteed to be not rounded) or null if unknown
    //    "dtm": 17, // depth to mate or null if unknown
    //    "checkmate": false,
    //    "stalemate": false,
    //    "variant_win": false, // only in chess variants (atomic, antichess)
    //    "variant_loss": false, // only in chess variants
    //    "insufficient_material": false,
    //    "category": "win", // win, unknown, maybe-win, cursed-win, draw, blessed-loss, maybe-loss, loss
    //    "moves": [ // information about legal moves, best first
    //      {
    //        "uci": "h7h8q",
    //        "san": "h8=Q+",
    //        "dtz": -2,
    //        "precise_dtz": -2,
    //        "dtm": -16,
    //        "zeroing": true,
    //        "checkmate": false,
    //        "stalemate": false,
    //        "variant_win": false,
    //        "variant_loss": false,
    //        "insufficient_material": false,
    //        "category": "loss" // loss, unknown, maybe-loss, blessed-loss, draw, cursed-win, maybe-win, win
    //      },
    //      // ...
    //    ]
    //  }

}

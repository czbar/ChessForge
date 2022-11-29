using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ChessPosition;
using System.Security.Policy;
using System.IO;
using GameTree;
using System.Diagnostics.Tracing;

namespace WebAccess
{
    /// <summary>
    /// Manages Game download from lichess.
    /// </summary>
    public class GameDownload
    {
        /// <summary>
        /// Handler for the DataReceived event
        /// </summary>
        public static event EventHandler<WebAccessEventArgs> GameReceived;

        /// <summary>
        /// Received text of the game.
        /// </summary>
        public static string GameText;

        /// <summary>
        /// Gets a game from lichess.org.
        /// </summary>
        /// <param name="gameId"></param>
        public static async void GetGame(string gameId)
        {
            WebAccessEventArgs eventArgs = new WebAccessEventArgs();
            try
            {
                HttpClient client = new HttpClient();
                var response = await RestApiRequest.Client.GetAsync("https://lichess.org/game/export/" + gameId);
                using (var fs = new MemoryStream())
                {
                    await response.Content.CopyToAsync(fs);
                    fs.Position = 0;
                    StreamReader sr = new StreamReader(fs);
                    GameText = sr.ReadToEnd();
                }
                eventArgs.Success = true;
                GameReceived?.Invoke(null, eventArgs);
            }
            catch (Exception ex)
            {
                eventArgs.Success = true;
                eventArgs.Message= ex.Message;
                GameReceived?.Invoke(null, eventArgs);
            }
        }

    }
}

using ChessPosition;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAccess;

namespace WebAccess
{
    public class OpeningExplorer
    {
        /// <summary>
        /// Gets the version number of Chess Forge currently available from Source Forge.
        /// </summary>
        /// <returns></returns>
        public static async Task<string> OpeningStats(string fen)
        {
            var json = await RestApiRequest.Client.GetStringAsync("https://explorer.lichess.ovh/masters?" + "fen=" + fen );
            dynamic obj = JsonConvert.DeserializeObject<dynamic>(json);
            return json;
        }
    }
}

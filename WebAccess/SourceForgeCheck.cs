using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ChessPosition;

namespace WebAccess
{
    public class SourceForgeCheck
    {
        /// <summary>
        /// Version of ChessForge's download package on SourceForge.
        /// </summary>
        public static Version ChessForgeVersion = null;
        
        /// <summary>
        /// Gets the version number of Chess Forge currently available from Source Forge.
        /// </summary>
        /// <returns></returns>
        public static async Task<string> GetVersion()
        {
            var json = await RestApiRequest.Client.GetStringAsync("https://sourceforge.net/projects/ChessForge/best_release.json");

            dynamic obj = JsonConvert.DeserializeObject<dynamic>(json);
            string sVer = obj.release.filename;

            bool result = TextUtils.GetVersionNumbers(sVer, out int major, out int minor, out int patch);
            ChessForgeVersion = new Version(major, minor, patch); 

            return json;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ChessPosition;
using System.Linq.Expressions;

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
            try
            {
                // used only once so create a transient client
                using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
                {
                    var json = await client.GetStringAsync("https://sourceforge.net/projects/ChessForge/best_release.json");

                    dynamic obj = JsonConvert.DeserializeObject<dynamic>(json);
                    string sVer = obj.platform_releases.windows.filename;

                    bool result = TextUtils.GetVersionNumbers(sVer, out int major, out int minor, out int patch);
                    ChessForgeVersion = new Version(major, minor, patch);

                    return json;
                }
            }
            catch
            {
                return "";
            }
        }
    }
}

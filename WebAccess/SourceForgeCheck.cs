using ChessPosition;
using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebAccess
{
    public class SourceForgeCheck
    {
        /// <summary>
        /// Version of ChessForge's download package on SourceForge.
        /// </summary>
        public static Version VersionAtSourceForge = null;

        /// <summary>
        /// Version of ChessForge's download package at Microsoft App Store.
        /// </summary>
        public static Version VersionAtMicrosoftAppStore = null;

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
                    client.DefaultRequestHeaders.Add("User-Agent", RestApiRequest.UserAgentLichess);
                    var json = await client.GetStringAsync("https://chessforge.sourceforge.io/Releases/Releases.json");

                    dynamic obj = JsonConvert.DeserializeObject<dynamic>(json);
                    
                    string valueSourceForge = obj.SourceForge;
                    string valueMicrosoftAppStore = obj.MicrosoftAppStore;

                    string verAtSourceForge = ExtractVersionString(valueSourceForge);
                    string verAtMicrosoftAppStore = ExtractVersionString(valueMicrosoftAppStore);

                    VersionAtSourceForge = GetVersionFromString(verAtSourceForge);
                    VersionAtMicrosoftAppStore = GetVersionFromString(verAtMicrosoftAppStore);
                    
                    return json;
                }
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Create a Version object representing the passed string.
        /// </summary>
        /// <param name="ver"></param>
        /// <returns></returns>
        private static Version GetVersionFromString(string ver)
        {
            Version cfVer = null;

            if (!string.IsNullOrEmpty(ver))
            {
                bool result = TextUtils.GetVersionNumbers(ver, out int major, out int minor, out int patch);
                if (result)
                {
                    cfVer = new Version(major, minor, patch);
                }
            }

            return cfVer;
        }

        /// <summary>
        /// Extracts the version number in the form of n.n.n from the passed string.
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        private static string ExtractVersionString(string inputString)
        {
            string pattern = @"\b\d+\.\d+\.\d+\b";

            Match match = Regex.Match(inputString, pattern);

            string substring = "";
            if (match.Success)
            {
                substring = match.Value;
            }

            return substring;
        }
    }
}

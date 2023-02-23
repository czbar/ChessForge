using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ChessPosition;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

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
                    string latestFileName = obj.platform_releases.windows.filename;

                    string versionString = ExtractVersionString(latestFileName);
                    if (!string.IsNullOrEmpty(versionString))
                    {
                        bool result = TextUtils.GetVersionNumbers(versionString, out int major, out int minor, out int patch);
                        if (result)
                        {
                            ChessForgeVersion = new Version(major, minor, patch);
                        }
                    }
                    return json;
                }
            }
            catch
            {
                return "";
            }
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

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ChessPosition;

namespace WebClient
{
    public class SourceForgeCheck
    {
        public static async Task<string> GetVersion()
        {
            var json = await RestApiRequest.Client.GetStringAsync("https://sourceforge.net/projects/ChessForge/best_release.json");

            dynamic obj = JsonConvert.DeserializeObject<dynamic>(json);
            string sVer = obj.release.filename;

            bool result = TextUtils.GetVersionNumbers(sVer, out int major, out int minor, out int patch);

            return json;
        }

        /// <summary>
        /// Extracts the version in the form of 1.1.1 from the passed string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static bool GetVersionNumbers(string sVer, out int major, out int minor, out int patch)
        {
            bool result = false;
            major = 0;
            minor = 0;
            patch = 0;

            try
            {
                string[] tokens = sVer.Split('.');

                // find the first number and get the 2 numbers that follow
                int lastPart = 0;
                for (int i = 0; i < tokens.Length; i++)
                {
                    int val;
                    if (int.TryParse(tokens[i], out val))
                    {
                        lastPart++;
                        switch (lastPart)
                        {
                            case 1:
                                major = val;
                                break;
                            case 2:
                                minor = val;
                                break;
                            case 3:
                                patch = val;
                                break;
                        }
                    }
                    else
                    {
                        if (lastPart != 0)
                        {
                            break;
                        }
                    }

                    if (lastPart == 3)
                    {
                        result= true;
                        break;
                    }
                }
            }
            catch
            {
                result = false;
            }

            return result;
        }
    }
}

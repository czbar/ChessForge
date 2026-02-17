using System.Net.Http;

namespace WebAccess
{
    /// <summary>
    /// Http Clients to use for making REST calls.
    /// </summary>
    public class RestApiRequest
    {
        public const string UserAgentChesscom = "ChessForge/1.14 (contact robert.rozycki@gmail.com; czbar on chess.com)";
        public const string UserAgentLichess = "ChessForge/1.14 (robert.rozycki@gmail.com)";
        public const string STATUS_CODE = "StatusCode:";

        static public HttpClient OpeningStatsClient = new HttpClient();
        static public HttpClient TablebaseClient = new HttpClient();
        static public HttpClient GameImportClient = new HttpClient();

        /// <summary>
        /// Parses the response from the server to extract the reponse code.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static int GetResponseCode(string response)
        {
            int res = -1;

            if (!string.IsNullOrEmpty(response))
            {
                int idx = response.IndexOf(STATUS_CODE);
                if (idx >= 0)
                {
                    string str = response.Substring(idx + STATUS_CODE.Length).TrimStart();
                    string errorCode = "";
                    foreach (char c in str)
                    {
                        if (char.IsDigit(c))
                        {
                            errorCode += c;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (!int.TryParse(errorCode, out res))
                    {
                        res = -1;
                    }
                }
            }

            return res;
        }
    }
}

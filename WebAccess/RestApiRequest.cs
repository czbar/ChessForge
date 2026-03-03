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

        // Lichess API token, to be set by the user in the settings dialog. Required for making requests to lichess API.
        public static string LichessAuthToken = "";

        /// <summary>
        /// Number of retries to do if we get 429 Too Many Requests response from lichess API.
        /// </summary>
        public static int LichessApiRetries = 3;

        /// <summary>
        /// Sets the default headers for the HttpClient to be used for making requests to lichess API. This includes the User-Agent and Authorization headers.
        /// </summary>
        /// <param name="httpClient"></param>
        public static void SetHttpClientLichessDefaultHeaders(HttpClient httpClient)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("User-Agent", RestApiRequest.UserAgentLichess);
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + LichessAuthToken);
        }

        /// <summary>
        /// Sets the default headers for the HttpClient to be used for making requests to chess.com API. This includes the User-Agent header.
        /// </summary>
        /// <param name="httpClient"></param>
        public static void SetHttpClientChesscomDefaultHeaders(HttpClient httpClient)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("User-Agent", RestApiRequest.UserAgentChesscom);
        }

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

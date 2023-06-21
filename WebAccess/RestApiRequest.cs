using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebAccess
{
    /// <summary>
    /// Http Clients to use for making REST calls.
    /// </summary>
    public class RestApiRequest
    {
        public const string UserAgent = "ChessForge/1 (contact robert.rozycki@gmail.com; czbar on chess.com)";
        static public HttpClient OpeningStatsClient = new HttpClient();
        static public HttpClient TablebaseClient = new HttpClient();
        static public HttpClient GameImportClient = new HttpClient();
    }
}

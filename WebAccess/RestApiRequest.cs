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
        static public HttpClient OpeningStatsClient = new HttpClient();
        static public HttpClient OpeningNameClient = new HttpClient();
        static public HttpClient TablebaseClient = new HttpClient();
        static public HttpClient GameImportClient = new HttpClient();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebAccess
{
    public class RestApiRequest
    {
        /// <summary>
        /// Http Client to use for making REST calls.
        /// </summary>
        static public HttpClient Client = new HttpClient();
    }
}

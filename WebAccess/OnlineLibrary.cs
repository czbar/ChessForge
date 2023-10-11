using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebAccess
{
    /// <summary>
    /// Access to the online library.
    /// </summary>
    public class OnlineLibrary
    {
        /// <summary>
        /// Gets and deserializes the library content from ChessForge's web site.
        /// </summary>
        /// <returns></returns>
        public static LibraryContent GetLibraryContent()
        {
            LibraryContent library = null;

            string urlQuery = "https://chessforge.sourceforge.io/LibraryContent.json";
            var request = WebRequest.CreateHttp(urlQuery);

            request.Method = "GET";

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    using (var myStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        var json = myStreamReader.ReadToEnd();
                        library = JsonConvert.DeserializeObject<LibraryContent>(json);
                    }
                }
            }

            return library;
        }

    }
}

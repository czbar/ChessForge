using ChessForge;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
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
        public static LibraryContent GetLibraryContent(out string error)
        {
            error = string.Empty;

            LibraryContent library = null;
            try
            {
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
            }
            catch (Exception ex)
            {
                error = ex.Message;
                AppLog.Message("GetLibraryContent()", ex);
            }

            return library;
        }

    }
}

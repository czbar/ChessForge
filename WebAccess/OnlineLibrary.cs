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
        // root URL for the library
        private static string LIBRARY_URL = "https://chessforge.sourceforge.io/Library/";

        // name of the json content file at the root of the library
        private static string LIBRARY_CONTENT_FILE = "LibraryContent.json";

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
                string urlQuery = LIBRARY_URL + LIBRARY_CONTENT_FILE;
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

        /// <summary>
        /// Reads the content of the workbook file at the specified URL path.
        /// </summary>
        /// <param name="bookPath"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static string GetWorkbookText(string bookPath, out string error)
        {
            error = string.Empty;

            string bookText = null;

            try
            {
                string urlQuery = LIBRARY_URL + bookPath;
                var request = WebRequest.CreateHttp(urlQuery);
                request.Method = "GET";

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        using (var myStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                        {
                            bookText = myStreamReader.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                AppLog.Message("GetWorkbookText()", ex);
            }

            return bookText;
        }
    }
}

using ChessForge;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace WebAccess
{
    /// <summary>
    /// Access to the online library.
    /// </summary>
    public class OnlineLibrary
    {
        // root URL for the library
        private static string LIBRARY_URL = "https://chessforge.sourceforge.io/Library/";

        // name of the text content file at the root of the library
        private static string LIBRARY_CONTENT_FILE = "LibraryContent.txt";

        // name of the text content file at the root of the library
        private static string LIBRARY_CONTENT_FILE_PL = "LibraryContent.pl.txt";

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
                string urlQuery = LIBRARY_URL + GetLibraryContentFileName();
                var request = WebRequest.CreateHttp(urlQuery);
                request.Method = "GET";

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        using (var myStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                        {
                            var txt = myStreamReader.ReadToEnd();
                            library = ParseLibraryContent(txt);
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

        /// <summary>
        /// Returns the name of the file with the list of the library content.
        /// </summary>
        /// <returns></returns>
        private static string GetLibraryContentFileName()
        {
            if (Thread.CurrentThread.CurrentUICulture.Name.Contains("pl"))
            {
                return LIBRARY_CONTENT_FILE_PL;
            }
            else
            {
                return LIBRARY_CONTENT_FILE;
            }
        }

        /// <summary>
        /// Parses the text file with library content.
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        private static LibraryContent ParseLibraryContent(string txt)
        {
            LibraryContent library = new LibraryContent();

            using (StringReader sr = new StringReader(txt))
            {
                Bookcase currBookcase = null;
                Shelf currShelf = null;
                Book currBook = null;

                while (sr.Peek() >= 0)
                {
                    string line = sr.ReadLine();
                    string typ = ParseLine(line, out string value);
                    switch (typ)
                    {
                        case "redirect":
                            library.Redirect = value;
                            break;
                        case "bookcase":
                            currBookcase = new Bookcase(value);
                            library.Bookcases.Add(currBookcase);
                            currShelf = null;
                            currBook = null;
                            break;
                        case "shelf":
                            currShelf = new Shelf(value);
                            currBookcase?.Shelves.Add(currShelf);
                            currBook = null;
                            break;
                        case "book":
                            currBook = new Book(value);
                            currShelf?.Books.Add(currBook);
                            break;
                        case "file":
                            currBook.File = value.Trim();
                            break;
                        case "description":
                            if (currBook != null)
                            {
                                currBook.Description = value;
                            }
                            else if (currShelf != null)
                            {
                                currShelf.Description = value;
                            }
                            else if (currBookcase != null)
                            {
                                currBookcase.Description = value;
                            }
                            break;
                    }
                }
            }

            return library;
        }

        /// <summary>
        /// Parses a single line in the content file.
        /// Returns the key (type) and the value.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string ParseLine(string line, out string value)
        {
            string typ = "";
            value = "";

            if (!string.IsNullOrEmpty(line))
            {
                int index = line.IndexOf(':');
                if (index > 0)
                {
                    typ = line.Substring(0, index).Trim().ToLower();
                    value = line.Substring(index + 1);    
                }
            }

            return typ;
        }
    }
}

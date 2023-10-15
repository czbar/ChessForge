using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Dynamic;
using System.Runtime.InteropServices.ComTypes;

namespace DownloadStats
{
    /// <summary>
    /// Gets the Chess Forge download stats from the Source Forge web site. 
    /// </summary>
    public class Program
    {
        // the first day, Chess Forge was available for download.
        private static DateTime ChessForgeRegDate = new DateTime(2022, 08, 19);

        // url to download the data from
        private static string urlDownloadStats = "https://sourceforge.net/projects/chessforge/files/stats/json?start_date={0}&end_date={0}&os_by_country=false&period=daily";
        
        // name of the output text file with the stats in the csv format.
        private static string outFileName = "ChessForgeDownloadStats.csv";

        private static string[] _lines;

        static void Main(string[] args)
        {
            DateTime startDate = ChessForgeRegDate;
            DateTime endDate =  DateTime.Now;

            StringBuilder sb = new StringBuilder();

            // if the file already exists, get the last date that it has the data for
            bool outFileExists = File.Exists(outFileName);
            if (outFileExists)
            {
                _lines = File.ReadAllLines(outFileName);
                startDate = GetStartDate(_lines, out int lastValidLine).Value;
                for (int i = 0; i <= lastValidLine; i++)
                {
                    sb.AppendLine(_lines[i]);
                }
            }
            else
            {
                // if the file does not exist, make sure we will write out the headers
                sb.AppendLine("Date,Country,Count,YearMonth");
            }

            // limit the number of days to query, as Source Forge will return an error if too many
            if (endDate > startDate.AddDays(150))
            {
                endDate = startDate.AddDays(150);
            }

            // call GetStats() for each day in the range.
            DateTime dt = startDate;
            while (dt <= endDate)
            {
                sb.Append(GetStats(dt));
                dt = dt.AddDays(1);
            }

            // either write the lot out or append if output file already exists.
            if (outFileExists)
            {
                File.WriteAllText(outFileName, sb.ToString());
            }
            else
            {
                File.WriteAllText(outFileName, sb.ToString());
            }
        }

        /// <summary>
        /// Determines the last date found in the file.
        /// Returns the Start Date from which to start gathering data
        /// (3 days before the last date found, so we can update those last dates)
        /// and the last line index in the file to keep.
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="lastValidLineIndex"></param>
        /// <returns></returns>
        static DateTime? GetStartDate(string[] lines, out int lastValidLineIndex)
        {
            lastValidLineIndex = lines.Length - 1;
            DateTime? dt = null;
            DateTime? lastDate = null;

            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string line = lines[i];
                string[] tokens = line.Split(',');
                if (tokens.Length >= 2)
                {
                    if (lastDate == null)
                    {
                        try
                        {
                            lastDate = DateTime.Parse(tokens[0]);
                        }
                        catch
                        {
                            dt = null;
                        }
                    }
                    else
                    {
                        try
                        {
                            DateTime currDt = DateTime.Parse(tokens[0]);
                            if (currDt < lastDate.Value.AddDays(-2))
                            {
                                lastValidLineIndex = i;
                                dt = currDt;
                                break;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            return dt;
        }

        /// <summary>
        /// Queries Source Forge for stats of the passed day and returns a string to insert
        /// in the output file.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        static string GetStats(DateTime dt)
        {
            StringBuilder sb = new StringBuilder();
            string urlQuery = string.Format(urlDownloadStats, dt.ToString("yyyy-MM-dd"));
            var request = WebRequest.CreateHttp(urlQuery);

            request.Method = "GET";

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    using (var myStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        var responseJSON = myStreamReader.ReadToEnd();
                        ExpandoObject data = JsonConvert.DeserializeObject<ExpandoObject>(responseJSON);

                        foreach (KeyValuePair<string, object> kvp in data)
                        {
                            if (kvp.Key == "countries")
                            {
                                List<object> value = kvp.Value as List<object>;
                                foreach (object item in value)
                                {
                                    Console.Write(dt.ToString("yyyy/MM/dd") + ",");
                                    sb.Append(dt.ToString("yyyy/MM/dd") + ",");

                                    Console.Write((item as List<object>)[0] as string + ",");
                                    sb.Append((item as List<object>)[0] as string + ",");

                                    Console.WriteLine((item as List<object>)[1].ToString() + ",");
                                    sb.AppendLine((item as List<object>)[1].ToString() + ",");
                                }
                            }
                        }
                    }
                }
            }

            return sb.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Dynamic;

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

        static void Main(string[] args)
        {
            DateTime startDate = ChessForgeRegDate;
            DateTime endDate =  DateTime.Now;

            StringBuilder sb = new StringBuilder();

            // if the file already exists, get the last date that it has the data for
            bool outFileExists = File.Exists(outFileName);
            if (outFileExists)
            {
                string[] lines = File.ReadAllLines(outFileName);
                string lastLine = lines[lines.Length - 1];
                string[] tokens = lastLine.Split(',');
                startDate = DateTime.Parse(tokens[0]).AddDays(1);
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
                File.AppendAllText(outFileName, sb.ToString());
            }
            else
            {
                File.WriteAllText(outFileName, sb.ToString());
            }
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

                                    Console.Write((item as List<object>)[1].ToString() + ",");
                                    sb.Append((item as List<object>)[1].ToString() + ",");

                                    Console.WriteLine(dt.ToString("yy MM"));
                                    sb.AppendLine(dt.ToString("yy MM"));
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

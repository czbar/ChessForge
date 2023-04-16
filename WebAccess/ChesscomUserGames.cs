using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChessPosition;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace WebAccess
{
    public class ChesscomUserGames
    {
        /// <summary>
        /// Handler for the UserGamesReceived event
        /// </summary>
        public static event EventHandler<WebAccessEventArgs> UserGamesReceived;

        /// <summary>
        /// Downloads games from chess.com.
        /// This is a multistage process.
        /// First we need to get the list of archives with games of the requested. Then
        /// we have to select the right ones to match the filter criteria
        /// and issue as many requestes as required.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static async Task<string> GetChesscomUserGames(GamesFilter filter)
        {
            WebAccessEventArgs eventArgs = new WebAccessEventArgs();
            try
            {
                // Stage 1 get list of possible archives
                List<string> urlArchivesList = null;
                string urlArchivesCommand = GetArchiveListUrl(filter.User);
                string text = await ExecuteHttpCall(urlArchivesCommand);
                eventArgs.TextData = text.Replace(',', ' ');
                urlArchivesList = TextUtils.MatchUrls(eventArgs.TextData);

                if (filter.StartDate != null || filter.EndDate != null)
                {
                    urlArchivesList = TrimUrlList(filter, urlArchivesList);
                }

                if (urlArchivesList.Count == 0)
                {
                    eventArgs.TextData = "";
                }
                else
                {
                    // start loading archive after archive and retrieving games until we get the required number
                    // if start date is not null we start from there, otherwise, we go back from the latest archive.
                    if (filter.StartDate.HasValue)
                    {
                    }
                    else
                    {
                    }
                }
                eventArgs.Success = true;
                UserGamesReceived?.Invoke(null, eventArgs);
                return "";
            }
            catch (Exception ex)
            {
                eventArgs.Success = true;
                eventArgs.Message = ex.Message;
                UserGamesReceived?.Invoke(null, eventArgs);
                return "";
            }
        }

        /// <summary>
        /// Executes an http REST call.
        /// Returns the text received in response.
        /// </summary>
        /// <param name="rest"></param>
        /// <returns></returns>
        private static async Task<string> ExecuteHttpCall(string rest)
        {
            string text = "";

            var response = await RestApiRequest.GameImportClient.GetAsync(rest);
            using (var fs = new MemoryStream())
            {
                await response.Content.CopyToAsync(fs);
                fs.Position = 0;
                StreamReader sr = new StreamReader(fs);
                text = sr.ReadToEnd().Replace(',', ' ');
            }

            return text;
        }

        /// <summary>
        /// Returns a list of urls after urls for archives from outside
        /// the filter's range were removed.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="urlList"></param>
        /// <returns></returns>
        private static List<string> TrimUrlList(GamesFilter filter, List<string> urlList)
        {
            List<string> trimmedList = new List<string>();

            Regex urlRegex = new Regex(@"\/\d{4}\/\d{2}$", RegexOptions.Compiled);
            foreach (string url in urlList)
            {
                Match match = urlRegex.Match(url);
                // first 4 digits is year followed by slash and 2 digits for month
                try
                {
                    int year = int.Parse(match.Value.Substring(1, 4));
                    int month = int.Parse(match.Value.Substring(6, 2));
                    if (IsDateToYearMonthGood(true, filter.StartDate, year, month) && IsDateToYearMonthGood(false, filter.EndDate, year, month))
                    {
                        trimmedList.Add(url);
                    }
                }
                catch
                {
                }
            }

            return trimmedList;
        }

        /// <summary>
        /// Checks if the passed year & month represents an earlier/later date than the passed date
        /// depending on wheter startDate is true/false.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="date"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        private static bool IsDateToYearMonthGood(bool startDate, DateTime? date, int year, int month)
        {
            if (!date.HasValue)
            {
                return true;
            }

            if (startDate)
            {
                if (date.Value.Year < year || date.Value.Year == year && date.Value.Month <= month)
                {
                    return true;
                }
            }
            else
            {
                if (date.Value.Year > year || date.Value.Year == year && date.Value.Month >= month)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Builds a REST string to query archives with player's games.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private static string GetArchiveListUrl(string user)
        {
            string url = string.Format("https://api.chess.com/pub/player/{0}/games/archives", user);
            return url;
        }
    }
}

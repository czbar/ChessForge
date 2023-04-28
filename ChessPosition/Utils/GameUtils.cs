using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessPosition
{
    public class GameUtils
    {
        /// <summary>
        /// Sorts games by date/time found in the headers
        /// </summary>
        /// <param name="games"></param>
        /// <returns></returns>
        public static List<GameData> SortGamesByDateTime(List<GameData> games)
        {
            List<GameData> lstGames = new List<GameData>(games);
            lstGames.Sort(CompareGamesByDateTime);
            return lstGames;
        }

        /// <summary>
        /// Remove games that are not in the passed date range.
        /// </summary>
        /// <param name="games"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public static List<GameData> RemoveGamesOutOfDateRange(List<GameData> games, DateTime? startDate, DateTime? endDate)
        {
            List<GameData> lstGames = new List<GameData>();
            foreach (GameData game in games)
            {
                if ((!startDate.HasValue || CompareGameDateToDate(game, startDate.Value) >= 0)
                    && (!endDate.HasValue || CompareGameDateToDate(game, endDate.Value) <= 0))
                {
                    lstGames.Add(game);
                }
            }
            return lstGames;
        }

        /// <summary>
        /// Compares the date of the game against the passed reference date
        /// </summary>
        /// <param name="game"></param>
        /// <param name="dtRef"></param>
        /// <returns></returns>
        public static int CompareGameDateToDate(GameData game, DateTime dtRef)
        {
            DateTime? dtGame = GetDateTimeFromGameData(game);

            if (dtGame.HasValue)
            {
                return (DateTime.Compare(dtGame.Value, dtRef));
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Makes best effort to find date/time for the passed game.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public static DateTime? GetDateTimeFromGameData(GameData game)
        {
            string date = game.Header.GetHeaderValue(PgnHeaders.KEY_UTC_DATE);
            string time = game.Header.GetHeaderValue(PgnHeaders.KEY_UTC_TIME);
            if (string.IsNullOrEmpty(date))
            {
                date = game.Header.GetDate(out _);
            }

            if (!string.IsNullOrEmpty(time))
            {
                date += " " + time;
            }

            DateTime dt;
            bool res = DateTime.TryParse(date, out dt);

            if (res)
            {
                return dt;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Compares games by date/time found in the headers
        /// </summary>
        /// <param name="game1"></param>
        /// <param name="game2"></param>
        /// <returns></returns>
        private static int CompareGamesByDateTime(GameData game1, GameData game2)
        {
            DateTime? dt1 = GetDateTimeFromGameData(game1);
            DateTime? dt2 = GetDateTimeFromGameData(game2);

            if (dt1.HasValue && dt2.HasValue)
            {
                return (DateTime.Compare(dt1.Value, dt2.Value));
            }
            else
            {
                if (!dt1.HasValue && !dt2.HasValue)
                {
                    return 0;
                }
                else
                {
                    return dt1.HasValue ? 1 : -1;
                }
            }
        }

    }
}

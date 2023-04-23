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

using ChessForge.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    public class ResourceUtils
    {
        /// <summary>
        /// Builds the string for the previous/next bar in Chapter/Game/Exercise views
        /// </summary>
        /// <param name="itemType"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static string GetCounterBarText(string itemType, int index, int count)
        {
            string counter = Resources.ResourceManager.GetString(itemType + "0of0");
            if (!string.IsNullOrEmpty(counter))
            {
                counter = counter.Replace("$0", (index + 1).ToString());
                counter = counter.Replace("$1", count.ToString());
            }
            else
            {
                counter = "Game " + (index + 1).ToString() + " of " + count.ToString();
            }

            return counter;
        }
    }
}

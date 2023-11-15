using ChessForge;
using ChessPosition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    public class NagUtils
    {
        /// <summary>
        /// Identifies position NagId in the string, of found.
        /// If not found, returns 0;
        /// </summary>
        /// <param name="nags"></param>
        /// <returns></returns>
        public static int GetPositionEvalNagId(string nags)
        {
            return GetNagId(Constants.MinPositionNagId, Constants.MaxPositionNagId, nags);
        }

        /// <summary>
        /// Builds a NAG string from NAG ids.
        /// </summary>
        /// <param name="moveNagId"></param>
        /// <param name="positionNagId"></param>
        /// <returns></returns>
        public static string BuildNagsString(int moveNagId, int positionNagId)
        {
            StringBuilder sb = new StringBuilder();
            if (moveNagId > 0)
            {
                sb.Append("$" +  moveNagId + " ");
            }
            if (positionNagId > 0)
            {
                sb.Append(" $" + positionNagId);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Identifies move NagId in the string, of found.
        /// If not found, returns 0;
        /// </summary>
        /// <param name="nags"></param>
        /// <returns></returns>
        public static int GetMoveEvalNagId(string nags)
        {
            return GetNagId(Constants.MinMoveNagId, Constants.MaxMoveNagId, nags);
        }

        /// <summary>
        /// Extracts a NAG within a requested range from the nags string.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="nags"></param>
        /// <returns></returns>
        public static int GetNagId(int from, int to, string nags)
        {
            int ret = 0;
            if (!string.IsNullOrEmpty(nags))
            {
                string[] tokens = nags.Split(' ');
                foreach (string token in tokens)
                {
                    // skip the leading $ sign
                    if (token.Length > 1 && int.TryParse(token.Substring(1), out int id))
                    {
                        if (id >= from && id <= to)
                        {
                            ret = id;
                            break;
                        }
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// Returns true if the id is within the position NAGs
        /// </summary>
        /// <param name="nagId"></param>
        /// <returns></returns>
        public static bool IsPositionNag(int nagId)
        {
            return nagId >= Constants.MinPositionNagId && nagId <= Constants.MaxPositionNagId;
        }

        /// <summary>
        /// Returns true if the id is within the move NAGs
        /// </summary>
        /// <param name="nagId"></param>
        /// <returns></returns>
        public static bool IsMoveNag(int nagId)
        {
            return nagId >= Constants.MinMoveNagId && nagId < Constants.MaxMoveNagId;
        }
    }
}

using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    public class EcoUtils
    {
        /// <summary>
        /// Gets the opening name from the built-in dictionary, if available.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="eco"></param>
        /// <returns></returns>
        public static string GetOpeningNameFromDictionary(TreeNode nd, out string eco)
        {
            string shFen = TextUtils.AdjustResourceStringForXml(FenParser.GenerateShortFen(nd.Position));
            string opening = Properties.OpeningNames.ResourceManager.GetString(shFen);
            if (opening != null && opening.Length > 4)
            {
                eco = opening.Substring(0, 3);
                return opening.Substring(4);
            }
            else
            {
                eco = "";
                return null;
            }
        }

    }
}

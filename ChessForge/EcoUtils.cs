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

        /// <summary>
        /// Attempts to find the opening's Eco and Opening Name for the passed article.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="eco"></param>
        /// <returns></returns>
        public static string GetArticleEcoFromDictionary(Article article, out string eco)
        {
            string name = "";
            eco = "";

            try
            {
                VariationTree tree = article.Tree;
                // collect the list of Nodes on the main line until move 14
                // (after which opening ECOs do not change in our dictionary, even though the name may somewhat change, we will ignore it)
                List<TreeNode> line = new List<TreeNode>();
                TreeNode node = tree.RootNode;
                while (node != null && node.Children.Count > 0 && node.MoveNumber < 15)
                {
                    node = node.Children[0];
                    line.Add(node);
                }

                // walk back the list until you find eco
                for (int i = line.Count - 1; i >= 0; i--)
                {
                    name = GetOpeningNameFromDictionary(line[i], out eco);
                    if (!string.IsNullOrEmpty(eco))
                    {
                        break;
                    }
                }
            }
            catch
            {
            }

            return name;
        }
    }
}

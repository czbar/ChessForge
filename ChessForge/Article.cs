using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Encapsulates Study or Model Game or Exercise tree
    /// together with related objects.
    /// </summary>
    public class Article
    {
        /// <summary>
        /// The Variation Tree of this Article.
        /// </summary>
        public VariationTree Tree;

        /// <summary>
        /// The Solving Manager of this Article.
        /// </summary>
        public SolvingManager Solver;

        public Article(GameData.ContentType contentType)
        {
            Tree = new VariationTree(contentType);
            Solver = new SolvingManager();
        }

        public Article(VariationTree tree)
        {
            Tree = tree;
            Solver = new SolvingManager();
        }
    }
}

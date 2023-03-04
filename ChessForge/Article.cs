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

        /// <summary>
        /// Encoded content when applicable (Intro XAML)
        /// </summary>
        public string CodedContent
        {
            get => Tree.RootNode.Data;
        }

        /// <summary>
        /// Constructs a new object with a Tree of the requested ContentType.
        /// </summary>
        /// <param name="contentType"></param>
        public Article(GameData.ContentType contentType)
        {
            Tree = new VariationTree(contentType);
            Solver = new SolvingManager();
        }

        /// <summary>
        /// Constructs a new object from the passed Tree.
        /// </summary>
        /// <param name="tree"></param>
        public Article(VariationTree tree)
        {
            Tree = tree;
            Solver = new SolvingManager();
        }
    }
}

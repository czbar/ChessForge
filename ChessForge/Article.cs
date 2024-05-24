using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessPosition;
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
            get => (Tree.RootNode == null ? null : Tree.RootNode.Data);
        }

        /// <summary>
        /// Applies to Exercises only.
        /// Determines whether to show or hide the solution by default.
        /// </summary>
        public bool ShowSolutionByDefault;

        /// <summary>
        /// True if the article has been fully created from it PGN
        /// </summary>
        public bool IsReady;

        /// <summary>
        /// Index on the list of the PGN background processor.
        /// </summary>
        public int PgnProcessorListIndex
        {
            get => _pgnProcessorListIndex;
        }

        // position on the PGN's background processor list
        private int _pgnProcessorListIndex = -1;


        /// <summary>
        /// Guid of the article
        /// </summary>
        public string Guid
        {
            get => Tree.Header.GetGuid(out _);
        }

        /// <summary>
        /// Stores arbitary data element for temporary use
        /// by custom scenarios.
        /// Not to be persisted!
        /// This can be used e.g. to mark articles as duplicates
        /// while figuring out the duplicates.
        /// </summary>
        public object Data;

        /// <summary>
        /// Article content type
        /// </summary>
        public GameData.ContentType ContentType => Tree.ContentType;

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

        /// <summary>
        /// Constructs a new object setting the value for its processor list's object.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="processorListIndex"></param>
        public Article(GameData.ContentType contentType, int processorListIndex) : this(contentType)
        {
            _pgnProcessorListIndex = processorListIndex;
        }

        /// <summary>
        /// Creates a clone of this article.
        /// </summary>
        /// <returns></returns>
        public Article CloneMe()
        {
            Article copy = this.MemberwiseClone() as Article;
            copy.Tree = TreeUtils.CopyVariationTree(this.Tree);
            copy.Tree.Header = this.Tree.Header.CloneMe(true); 

            return copy;
        }
    }
}

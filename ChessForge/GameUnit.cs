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
    public class GameUnit
    {
        /// <summary>
        /// The Variation Tree of this Unit
        /// </summary>
        public VariationTree Tree;

        /// <summary>
        /// The Solving Manager of this Unit.
        /// </summary>
        public SolvingManager Solver;

        public GameUnit(GameData.ContentType contentType)
        {
            Tree = new VariationTree(contentType);
            Solver = new SolvingManager();
        }

        public GameUnit(VariationTree tree)
        {
            Tree = tree;
            Solver = new SolvingManager();
        }
    }
}

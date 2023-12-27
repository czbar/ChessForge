using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Holds a tree structure with LineSectorNodes as nodes.
    /// </summary>
    public class LineSectorsTree
    {
        /// <summary>
        /// The list of LineSector nodes in the Tree.
        /// </summary>
        public List<LineSector> LineSectors;

        /// <summary>
        /// Creates a LineSectorsTree with a root node
        /// </summary>
        public LineSectorsTree() 
        {
            LineSectors = new List<LineSector>();

            LineSector sector = new LineSector();
            LineSectors.Add(sector);
        }

        /// <summary>
        /// Returns the root LineSector
        /// </summary>
        public LineSector Root
        {
            get => LineSectors[0];
        }
    }
}

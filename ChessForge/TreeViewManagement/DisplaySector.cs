using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Visibility status of the line.
    /// It can be fully visible (EXPANDED), or hidden (COLLAPSED)
    /// </summary>
    public enum DisplaySectorVisibility
    {
        EXPANDED,
        COLLAPSED
    }

    /// <summary>
    /// A variation to be displayed in a single paragraph.
    /// This is either a single line sector without any sub-sectors, or with one or more
    /// sub-sectors e.g. "15.e4 e5 (15...Nf6 16.Nf3; 15...f6 16.Bc2) 16.Qe4",
    /// where the sub-sectors are parts within the parenthesis.
    /// Display Sectors do not exactly align with sub-sectors as, for example, they
    /// may "borrow" the first node from the first child's node list to show themselves.
    /// </summary>
    public class DisplaySector
    {
        // the (main) line shown in this sector
        private LineSector _line;

        // subsector lines if any
        public List<LineSector> SubSectors;

        // how the line is currently shown in the view
        private DisplaySectorVisibility _visibility;

        /// <summary>
        /// The level at which this sector is to be displayed.
        /// Note that even when it represents a LineSector at Tree branching level 3 it
        /// may be at level 1 for display (because it is the first line of the view's section)
        /// </summary>
        public int DisplayLevel;

        /// <summary>
        /// List of TreeNodes from the main sector.
        /// If there are sub-sectors they will be accessed via the _subSectors list.
        /// </summary>
        public List<TreeNode> Nodes;
    }
}

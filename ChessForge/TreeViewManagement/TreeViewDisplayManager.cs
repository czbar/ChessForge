using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Manages no-branch lines and fragments for a VariationTree
    /// </summary>
    public class TreeViewDisplayManager
    {
        // managed tree 
        private VariationTree _tree;

        // the tree holding all LineSectors.
        public LineSectorsTree SectorsTree;

        public List<DisplaySector> DisplaySectors;

        /// <summary>
        /// The VariationTree whose lines are managed by this objects
        /// </summary>
        public VariationTree Tree
        {
            get => _tree;
            set => _tree = value;
        }

        // sector id to assign
        private int _runningSectorId = 0;

        /// <summary>
        /// Constructs the object.
        /// </summary>
        public TreeViewDisplayManager()
        {
        }

        /// <summary>
        /// Separates lines out of the tree, sets line Ids on the nodes
        /// and places the lines in the list.
        /// </summary>
        public void BuildLineSectors(VariationTree tree)
        {
            _tree = tree;
            TreeNode root = _tree.RootNode;
            root.LineId = "1";

            // create a SectorsTrre and add the root TreeNode to its root LineSector
            SectorsTree = new LineSectorsTree();
            SectorsTree.Root.Nodes.Add(root);

            // for each child of the root TreeNode, build LineSectors adding them to the tree. 
            foreach (TreeNode child in SectorsTree.Root.Nodes[0].Children)
            {
                BuildLineSector(SectorsTree.Root, child, 1);
            }
        }

        /// <summary>
        /// Each invocation of this method builds a line sector for the flattened view of the Workbook.
        /// The method calls itself recursively to build the complete set of clean lines.
        /// </summary>
        /// <param name="nd"></param>
        private void BuildLineSector(LineSector parent, TreeNode nd, int level)
        {
            _runningSectorId++;
            LineSector sector = new LineSector();
            sector.LineSectorId = _runningSectorId;
            SectorsTree.LineSectors.Add(sector);

            sector.BranchLevel = level;
            sector.Nodes.Add(nd);

            // add all leaf nodes that follow
            while (nd.Children.Count == 1)
            {
                nd.Children[0].LineId = nd.LineId;
                nd = nd.Children[0];
                sector.Nodes.Add(nd);
            }

            // now the nd node has either 0 children or more than 1
            if (nd.Children.Count > 1)
            {
                // mark the sector as FORKING and build a subtree from here
                sector.SectorType = LineSectorType.FORKING;
                parent.AddChild(sector);
                level++;
                for (int i = 0; i < nd.Children.Count; i++)
                {
                    nd.Children[i].LineId = nd.LineId + "." + (i + 1).ToString();
                    BuildLineSector(sector, nd.Children[i], level);
                }
            }

            if (nd.Children.Count == 0)
            {
                // we reached the end of the branch so return
                sector.SectorType = LineSectorType.LEAF;
                parent.AddChild(sector);
            }
        }

        /// <summary>
        /// Recursively builds all DisplaySectors
        /// </summary>
        public void BuildDisplaySectors()
        {
            DisplaySectors = new List<DisplaySector>();
            BuildDisplaySector(SectorsTree.Root, 1, false);
        }

        /// <summary>
        /// Builds a DisplaySector that will be shown in a single paragraph.
        /// Calls itself recursively to build a sub-tree of DisplaySectors.
        /// </summary>
        /// <param name="lineSector"></param>
        /// <param name="displayLevel"></param>
        /// <param name="isSectionTitle"></param>
        private LineSector BuildDisplaySector(LineSector lineSector, int displayLevel, bool isSectionTitle)
        {
            LineSector lastProcessedSector = lineSector;

            DisplaySector displaySector = new DisplaySector();
            displaySector.DisplayLevel = displayLevel;
            DisplaySectors.Add(displaySector);

            // copy TreeNodes from the lineSector
            foreach (TreeNode node in lineSector.Nodes)
            {
                displaySector.Nodes.Add(node);
            }

            if (isSectionTitle)
            {
                // finish here and self-invoke to continue down the tree
                foreach (LineSector sector in lineSector.Children)
                {
                    BuildDisplaySector(sector, displayLevel++, isSectionTitle);
                }
            }
            else
            {
                if (lineSector.SectorType == LineSectorType.LEAF)
                {
                    // all done so just return
                }
                else if (lineSector.SectorType == LineSectorType.FORKING)
                {
                    // not a section title line so the main line takes the first move of the first child
                    displaySector.Nodes.Add(lineSector.Children[0].Nodes[0]);
                    // if we have just 2 children and the second is of type LEAF, add it as a sub-sector
                    // and keep going with the first child as if it was the same sector
                    lastProcessedSector = HandleSubSectors(displaySector, lineSector);

                    foreach (LineSector sector in lastProcessedSector.Children)
                    {
                        BuildDisplaySector(sector, displayLevel++, isSectionTitle);
                    }
                }
            }

            return lastProcessedSector;
        }

        private LineSector HandleSubSectors(DisplaySector displaySector, LineSector lineSector)
        {
            LineSector subSector = GetSubSector(lineSector);
            {
                while (subSector != null)
                {
                    displaySector.SubSectors.Add(subSector);
                    foreach (TreeNode node in lineSector.Children[0].Nodes)
                    {
                        displaySector.Nodes.Add(node);
                    }
                    subSector = HandleSubSectors(displaySector, subSector);
                }
            }

            return subSector;
        }

        /// <summary>
        /// Checks if there is a child LineSector meeting the sub-sector criteria.
        /// </summary>
        /// <param name="lineSector"></param>
        /// <returns></returns>
        private LineSector GetSubSector(LineSector lineSector)
        {
            if (lineSector.Children.Count == 2 && lineSector.Children[1].SectorType == LineSectorType.LEAF)
            {
                return lineSector.Children[1];
            }
            else
            {
                return null;
            }
        }
    }
}


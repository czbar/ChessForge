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
        public void BuildLineSector(LineSector parent, TreeNode nd, int level)
        {
            _runningSectorId++;
            LineSector sector = new LineSector();
            sector.LineSectorId = _runningSectorId;
            SectorsTree.LineSectors.Add(sector);

            sector.DisplayLevel = level;
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
    }
}


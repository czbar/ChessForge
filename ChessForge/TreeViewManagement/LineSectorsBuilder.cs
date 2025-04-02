using ChessPosition;
using GameTree;
using System.Collections.Generic;

namespace ChessForge.TreeViewManagement
{
    public class LineSectorsBuilder
    {
        // sector id being progressively assigned
        private int _runningSectorId = 0;

        // helper list for deletions in processing
        private List<LineSector> _lineSectorsToDelete;

        // highest branch level in the view
        private int _maxBranchLevel = -1;

        // index depth
        private int _variationIndexDepth = -1;

        /// <summary>
        /// The list of LineSectors
        /// </summary>
        public List<LineSector> LineSectors;

        /// <summary>
        /// Separates lines out of the tree, sets line Ids on the nodes
        /// and places the lines in the list.
        /// </summary>
        public void BuildLineSectors(TreeNode root, bool isIndexed)
        {
            _lineSectorsToDelete = new List<LineSector>();
            _maxBranchLevel = -1;
            
            _variationIndexDepth = isIndexed ? VariationTreeViewUtils.VariationIndexDepth : -1;

            LineSectors = new List<LineSector>();
            LineSector rootSector = CreateRootLineSector(root);
            LineSectors.Add(rootSector);

            ProcessChildSectors(rootSector, root);

            CombineSiblingLineSectors();
            CombineTopLineSectors();
        }

        /// <summary>
        /// Each invocation of this method builds a line sector for the flattened view of the Workbook.
        /// The method calls itself recursively to build the complete set of clean lines.
        /// </summary>
        /// <param name="nd"></param>
        private LineSector BuildLineSector(LineSector parent, TreeNode nd, int displayLevel)
        {
            _runningSectorId++;

            LineSector sector = new LineSector();
            sector.LineSectorId = _runningSectorId;
            sector.DisplayLevel = displayLevel;
            sector.BranchLevel = TreeUtils.GetBranchLevel(nd.LineId);
            LineSectors.Add(sector);

            if (sector.BranchLevel > _maxBranchLevel)
            {
                _maxBranchLevel = sector.BranchLevel;
            }
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
                ProcessChildSectors(sector, nd);
            }

            if (nd.Children.Count == 0)
            {
                // we reached the end of the branch so return
                sector.SectorType = LineSectorType.LEAF;
                parent.AddChild(sector);
            }

            return sector;
        }

        /// <summary>
        /// Kicks off recursive build of LineSectors 
        /// for child modes.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="nd"></param>
        private void ProcessChildSectors(LineSector parent, TreeNode nd)
        {
            // if child level is within index levels, proceeed in normal order, otherwise apply section layout
            if (IsIndexLevel(parent.BranchLevel + 1) || parent.Parent == null)
            {
                foreach (TreeNode child in nd.Children)
                {
                    BuildLineSector(parent, child, parent.DisplayLevel + 1);
                }
            }
            else
            {
                int displayLevel = parent.DisplayLevel;

                if (nd.Children.Count > 0)
                {
                    // this is not a root's child so we will have at least 2 children
                    // process the first one last as we are in "Game" layout now rather than "Sections" one.
                    for (int i = 1; i < nd.Children.Count; i++)
                    {
                        BuildLineSector(parent, nd.Children[i], displayLevel + 1);
                    }
                    LineSector built = BuildLineSector(parent, nd.Children[0], parent.DisplayLevel);
                    if (nd.Children.Count > 0)
                    {
                        parent.Nodes.Add(nd.Children[0]);
                        built.Nodes.RemoveAt(0);
                    }
                }
            }
        }

        /// <summary>
        /// Combines siblings sectors when one is a top line sector and the other
        /// is a leaf sector.
        /// Note that the target top line sector is at position 1 in the parent as we have swapped the positions
        /// when generating the list of Sectors.
        /// </summary>
        private void CombineSiblingLineSectors()
        {
            foreach (LineSector lineSector in LineSectors)
            {
                if (!IsIndexLevel(lineSector.BranchLevel - 1) && lineSector.Parent != null)
                {
                    // do not proceed if Parent.Parent == null 'coz we then get a parenthesis first (after the 0 move!)
                    if (lineSector.Parent.Children.Count == 2 
                        && !lineSector.Nodes[0].IsMainLine() 
                        && lineSector.Parent.Parent != null 
                        && lineSector.Parent.Children[1] == lineSector 
                        && lineSector.Parent.Children[0].SectorType == LineSectorType.LEAF)
                    {
                        int index = 0;
                        lineSector.Parent.Children[1].InsertOpenBracketNode(index);
                        index++;
                        foreach (TreeNode nd in lineSector.Parent.Children[0].Nodes)
                        {
                            lineSector.Parent.Children[1].Nodes.Insert(index, nd);
                            index++;
                        }
                        lineSector.Parent.Children[1].InsertCloseBracketNode(index);
                        _lineSectorsToDelete.Add(lineSector.Parent.Children[0]);
                        lineSector.Parent.Children.RemoveAt(0);
                    }
                }
            }
            foreach (LineSector s in _lineSectorsToDelete)
            {
                LineSectors.Remove(s);
            }
        }

        /// <summary>
        /// Merges top level LineSectors that may have appeared
        /// after combining leaf sectors with siblings.
        /// </summary>
        private void CombineTopLineSectors()
        {
            _lineSectorsToDelete.Clear();
            foreach (LineSector lineSector in LineSectors)
            {
                while (true)
                {
                    if (!MergeTopLines(lineSector))
                    {
                        break;
                    }
                }
            }
            foreach (LineSector s in _lineSectorsToDelete)
            {
                LineSectors.Remove(s);
            }
        }

        /// <summary>
        /// Merges 2 top line sectors.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private bool MergeTopLines(LineSector target)
        {
            bool merged = false;

            if (target.Children.Count == 1)
            {
                // do not process sectors already marked for deletion
                if (_lineSectorsToDelete.Find(x => x == target.Children[0]) == null && _lineSectorsToDelete.Find(x => x == target) == null)
                {
                    foreach (TreeNode nd in target.Children[0].Nodes)
                    {
                        target.Nodes.Add(nd);
                    }
                    foreach (LineSector child in target.Children[0].Children)
                    {
                        target.Children.Add(child);
                        child.Parent = target;
                    }
                    _lineSectorsToDelete.Add(target.Children[0]);
                    target.Children.Remove(target.Children[0]);

                    merged = true;
                }
            }

            return merged;
        }

        /// <summary>
        /// Returns true if the passed branch level is within index section levels.
        /// Since the first "true" (i.e. not the stem line) index level is 2, 
        /// the last one is 1 + VariationIndexDepth
        /// e.g if VariationIndexDepth = 3 then the first "true" index is 2 and the last one is 4.
        /// </summary>
        /// <param name="branchLevel"></param>
        /// <returns></returns>
        private bool IsIndexLevel(int branchLevel)
        {
            return branchLevel <= (_variationIndexDepth + 1);
        }

        /// <summary>
        /// Creates the root LineSector
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        private LineSector CreateRootLineSector(TreeNode root)
        {
            _runningSectorId = 0;

            LineSector rootSector = new LineSector();
            rootSector.Nodes.Add(root);
            rootSector.LineSectorId = _runningSectorId;
            rootSector.BranchLevel = 1;
            rootSector.DisplayLevel = -1;

            return rootSector;
        }
    }
}

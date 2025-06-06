﻿using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;

namespace ChessForge
{
    /// <summary>
    /// INDEX LEVELS: 
    /// Index levels begin at First Index Branch Level and include the "stem line" plus the number of 
    /// SECTION_TITLE_LEVELS Branch Levels.
    /// 
    /// The root level LineSector only contains the root node of the view and is never displayed.
    /// If it only has one child, then the First Index Branch Level is set to 1, otherwise it is set to 2.
    /// 
    /// In the case of one child we will have a stem line e.g. "1.e4 e5 2.Nf3" before the first fork.
    /// The Branch Level of the stem's Line Sector equals 1.
    /// 
    /// In the case of multiple children we have no stem line and start with index entries like "A) 1.e4", "B) 1.d4" etc
    /// The Branch Level of these entries' Line Sectors equals 2. There is no Branch Level 1.
    /// 
    /// Therefore, the last Branch Level that is still an Index Level is always SECTION_TITLE_LEVELS while
    /// the first one can be 1 or 2.
    /// 
    /// Note that the Branch Level can be determined from the LineId of the first Node 
    /// by counting the number of dots and adding 1.
    /// For example: 1.1 is branch level 2 while 1.2.3.1 is level 4 
    /// 
    /// The Display Level represents the format of the paragraph in which a given Line Sector
    /// is displayed. We start with 0 for the stem line for the child/children of the root node
    /// and then increment at each fork for all children while in the index section.
    /// 
    /// Display Level is reset back to 0, when leaving the index section and then incremented
    /// at each fork other than for the first child.  First children remain at their parent's 
    /// display level which defines the "game" layout.
    /// </summary>
    public class LineSectorManager
    {
        // sector id being progressively assigned
        private int _runningSectorId = 0;

        // helper list for deletions in processing
        private List<LineSector> _lineSectorsToDelete;

        // highest branch level in the view
        private int _maxBranchLevel = -1;

        // indent between levels in the index paragraph.
        private readonly string _indent = "    ";

        /// <summary>
        /// manages colors for the last node of a sector at a given level.
        /// </summary>
        private LineSectorRunColors _runColors = new LineSectorRunColors();

        /// <summary>
        /// Resets the last move color selection.
        /// </summary>
        public void ResetLastMoveBrush()
        {
            _runColors.ResetLastMoveBrush();
        }

        /// <summary>
        // Accessor tp the highest branch level value in the view
        /// </summary>
        public int MaxBranchLevel
        {
            get { return _maxBranchLevel; }
            set { _maxBranchLevel = value; }
        }

        /// <summary>
        /// The list of LineSectors
        /// </summary>
        public List<LineSector> LineSectors;

        /// <summary>
        /// The paragraph that contains the index header.
        /// </summary>
        public Paragraph IndexHeaderPara;

        /// <summary>
        /// The paragraph that contains the index content.
        /// </summary>
        public Paragraph IndexContentPara;

        /// <summary>
        /// Creates a paragraph for the "Variation Index" title and the depth arrows.
        /// </summary>
        public void CreateIndexHeaderPara()
        {
            IndexHeaderPara = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 6),
            };
        }

        /// <summary>
        /// Creates a paragraph for the "Variation Index" content.
        /// </summary>
        public void CreateIndexContentPara()
        {
            IndexContentPara = _studyView.CreateParagraph("0", true);
            IndexContentPara.Name = RichTextBoxUtilities.StudyIndexParagraphName;
            IndexContentPara.Foreground = ChessForgeColors.CurrentTheme.IndexPrefixForeground;
            IndexContentPara.FontWeight = FontWeights.Normal;
            IndexContentPara.FontSize = IndexContentPara.FontSize - 1;
            IndexContentPara.Margin = new Thickness(0, 0, 0, 0);
        }

        /// <summary>
        /// Populates the passed paragraph with the Variation Index header. 
        /// </summary>
        /// <param name="para"></param>
        public void PopulateIndexHeaderPara()
        {
            if (IndexHeaderPara != null)
            {
                IndexHeaderPara.Inlines.Clear();

                Run run = new Run(Properties.Resources.VariationIndex + "  ");
                IndexHeaderPara.Inlines.Add(run);
                IndexHeaderPara.FontWeight = FontWeights.DemiBold;

                _studyView.InsertArrowRuns(IndexHeaderPara);
            }
        }

        /// <summary>
        /// Populates the passed paragraph with the Variation Index content. 
        /// </summary>
        public void PopulateIndexContentPara()
        {
            if (IndexContentPara != null)
            {
                Paragraph para = IndexContentPara;
                para.Inlines.Clear();

                foreach (LineSector sector in LineSectors)
                {
                    if (sector.Nodes.Count == 0)
                    {
                        continue;
                    }

                    int level = sector.BranchLevel;
                    if (IsEffectiveIndexLevel(level))
                    {
                        if (sector.Nodes[0].LineId == "1")
                        {
                            // Build the stem line
                            // Note we don't want an indent here 'coz if the stem line is long it will wrap ugly
                            bool validMove = false;
                            
                            int stemRunCount = sector.Nodes.Count;
                            // if there is nothing but the main line then we only want to show the first move
                            // in the stem.
                            // That means 2 nodes because the first one is the 0 move with emtpy text.
                            if (sector.Nodes[stemRunCount -1].Children.Count == 0)
                            {
                                stemRunCount = 2;
                            }

                            for (int i = 0; i < stemRunCount; i++)
                            {
                                TreeNode nd = sector.Nodes[i];
                                Run rMove = _studyView.BuildIndexNodeAndAddToPara(nd, false, para);
                                rMove.TextDecorations = TextDecorations.Underline;
                                rMove.FontWeight = FontWeights.DemiBold;
                                if (nd.NodeId != 0)
                                {
                                    validMove = true;
                                }
                            }

                            if (validMove)
                            {
                                para.Inlines.Add(new Run("\n"));
                            }
                        }
                        else
                        {
                            for (int i = 0; i < level; i++)
                            {
                                Run r = new Run(_indent);
                                para.Inlines.Add(r);
                            }

                            Run rIdTitle = _studyView.BuildSectionIdTitle(sector.Nodes[0].LineId);
                            para.Inlines.Add(rIdTitle);
                            if (IsLastEffectiveIndexLine(level) || sector.Nodes[sector.Nodes.Count - 1].Children.Count == 0)
                            {
                                _studyView.BuildIndexNodeAndAddToPara(sector.Nodes[0], true, para);
                            }
                            else
                            {
                                bool firstMove = true;

                                int nodeCount = sector.Nodes.Count;
                                int firstSkip = nodeCount;
                                int lastSkip = -1;

                                if (nodeCount >= 5)
                                {
                                    firstSkip = 2;
                                    lastSkip = nodeCount - 3;
                                }

                                for (int i = 0; i < sector.Nodes.Count; i++)
                                {
                                    TreeNode nd = sector.Nodes[i];
                                    _studyView.BuildIndexNodeAndAddToPara(nd, firstMove, para);
                                    firstMove = false;
                                }
                            }
                            rIdTitle.FontWeight = FontWeights.DemiBold;

                            para.Inlines.Add(new Run("\n"));
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Gets the list of all sectors in the subtree
        /// of LineSectors starting at sector.
        /// The passed sector is not included.
        /// </summary>
        /// <param name="sector"></param>
        /// <param name="lstSectors"></param>
        public void GetSubTree(LineSector sector, List<LineSector> lstSectors)
        {
            foreach (LineSector ls in sector.Children)
            {
                lstSectors.Add(ls);
                GetSubTree(ls, lstSectors);
            }
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
            return branchLevel <= (VariationTreeViewUtils.VariationIndexDepth + 1);
        }

        // hosting Study View
        private StudyTreeView _studyView;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tree"></param>
        public LineSectorManager(StudyTreeView tree)
        {
            _studyView = tree;
        }

        /// <summary>
        /// Checks if the tree has a non-empty index at level 0.
        /// If the first child of node 0 has branch level 2, then we 
        /// do not have index level 0
        /// </summary>
        /// <returns></returns>
        public bool HasIndexLevelZero()
        {
            TreeNode root = LineSectors[0].Nodes[0];
            if (root.Children.Count > 0)
            {
                int startLevel = TreeUtils.GetBranchLevel(root.Children[0].LineId);
                return startLevel == 1;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the passed branch level corresponds
        /// to the last index section level.
        /// </summary>
        /// <param name="branchLevel"></param>
        /// <returns></returns>
        private bool IsLastIndexLine(int branchLevel)
        {
            return branchLevel == (VariationTreeViewUtils.VariationIndexDepth + 1);
        }

        /// <summary>
        /// Creates a paragraph for the main Study View area.
        /// </summary>
        /// <param name="attrs"></param>
        /// <returns></returns>
        public static Paragraph CreateStudyParagraph(SectorParaAttrs attrs, int displayLevel)
        {
            // create the paragraph.
            Paragraph para = new Paragraph
            {
                Margin = new Thickness(20 * displayLevel, attrs.TopMarginExtra, 0, 5 + attrs.BottomMarginExtra),
                FontSize = attrs.FontSize,
                FontWeight = displayLevel == 0 ? FontWeights.DemiBold : FontWeights.Normal,
                TextAlignment = TextAlignment.Left,
                Foreground = ChessForgeColors.CurrentTheme.RtbForeground
            };

            return para;
        }

        /// <summary>
        /// Updates the paragraph attributes for a Study View paragraph.
        /// It will be invoked to update the attributes of a paragraph 
        /// in the currently active LineSectorManager with those of the
        /// "updated" one.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="attrs"></param>
        /// <param name="displayLevel"></param>
        public static void UpdateStudyParagraphAttrs(Paragraph para, SectorParaAttrs attrs, int displayLevel)
        {
            if (para == null)
            {
                return;
            }

            try
            {
                Thickness margin = new Thickness(20 * displayLevel, attrs.TopMarginExtra, 0, 5 + attrs.BottomMarginExtra);

                if (para.Margin != margin)
                {
                    para.Margin = margin;
                }
                if (para.FontSize != attrs.FontSize)
                {
                    para.FontSize = attrs.FontSize;
                }
                if (para.FontWeight != attrs.FontWeight)
                {
                    para.FontWeight = attrs.FontWeight;
                }
                if (para.TextAlignment != attrs.TextAlignment)
                {
                    para.TextAlignment = attrs.TextAlignment;
                }
                if (para.Foreground != attrs.Foreground)
                {
                    para.Foreground = attrs.Foreground;
                }
                if (para.Background != attrs.Background)
                {
                    para.Background = attrs.Background;
                }
            }
            catch(Exception ex)
            {
                AppLog.Message("UpdateStudyParagraphAttrs()", ex);
            }
        }

        /// <summary>
        /// Separates lines out of the tree, sets line Ids on the nodes
        /// and places the lines in the list.
        /// </summary>
        public void BuildLineSectors(TreeNode root)
        {
            _lineSectorsToDelete = new List<LineSector>();
            _maxBranchLevel = -1;

            LineSectors = new List<LineSector>();
            LineSector rootSector = CreateRootLineSector(root);
            LineSectors.Add(rootSector);

            ProcessChildSectors(rootSector, root);

            CombineSiblingLineSectors();
            CombineTopLineSectors();

            // Calculate the associated paragraph attributes for each sector
            BuildParaAttrs();
        }

        /// <summary>
        /// This is called after the list of LineSectors has been created.
        /// Builds the paragraph attributes for each sector.
        /// They will then be used when creating the paragraphs an when
        /// comparing different layouts.
        /// </summary>
        private void BuildParaAttrs()
        {
            List<LineSector> doNotShow = new List<LineSector>();
            int levelGroup = 0;
            bool firstAtIndexLevel2 = true;

            for (int idx = 0; idx < LineSectors.Count; idx++)
            {
                LineSector sector = LineSectors[idx];
                sector.ParaAttrs.LevelGroup = levelGroup;

                // do not show the sector if it is marked as hidden (because an ancestor is collapsed)
                if (doNotShow.Find(x => x == sector) != null)
                {
                    sector.IsShown = false;
                    continue;
                }

                if (sector.IsCollapsed)
                {
                    // put all children in the doNotShow list
                    GetSubTree(sector, doNotShow);
                }

                // skip the sector for the 0 move unless it has a comment
                if (sector.Nodes.Count == 0 || sector.Nodes.Count == 1 && sector.Nodes[0].NodeId == 0 && string.IsNullOrEmpty(sector.Nodes[0].Comment))
                {
                    continue;
                }

                // if the sector is at a different level than the previous one, increment the level group
                if (idx > 0 && sector.DisplayLevel != LineSectors[idx - 1].BranchLevel)
                {
                    levelGroup++;
                    sector.ParaAttrs.LevelGroup = levelGroup;
                }

                try
                {
                    if (sector.DisplayLevel < 0)
                    {
                        sector.DisplayLevel = 0;
                    }

                    int topMarginExtra = 0;
                    int bottomMarginExtra = 0;

                    if (!IsEffectiveIndexLevel(sector.BranchLevel))
                    {
                        // add extra room above/below first/last line in a block at the same level 
                        if (idx > 0 && LineSectors[idx - 1].DisplayLevel < sector.DisplayLevel)
                        {
                            topMarginExtra = SectorParaAttrs.EXTRA_MARGIN;
                        }
                        if (idx < LineSectors.Count - 1 && LineSectors[idx + 1].DisplayLevel < sector.DisplayLevel)
                        {
                            bottomMarginExtra = SectorParaAttrs.EXTRA_MARGIN;
                        }
                        // also make sure that the sector outside the indexed range is not expanded!
                        foreach (TreeNode node in sector.Nodes)
                        {
                            node.IsCollapsed = false; // TODO: do we need this here
                        }

                    }
                    else
                    {
                        // add extra room above an index line, unless this is the first line at level 2
                        if (sector.BranchLevel == 2 && firstAtIndexLevel2)
                        {
                            firstAtIndexLevel2 = false;
                        }
                        else
                        {
                            topMarginExtra = SectorParaAttrs.EXTRA_MARGIN;
                        }
                    }

                    CreateParagraphAttrs(sector.ParaAttrs, sector.DisplayLevel, topMarginExtra, bottomMarginExtra);
                    if (sector.ParaAttrs.FontWeight == FontWeights.Bold)
                    {
                        sector.ParaAttrs.FontWeight = FontWeights.Normal;
                    }

                    if (IsEffectiveIndexLevel(sector.BranchLevel))
                    {
                        sector.ParaAttrs.FontWeight = FontWeights.DemiBold;
                    }
                    else
                    {
                        if (IsLastEffectiveIndexLine(sector.DisplayLevel + 1))
                        {
                            sector.ParaAttrs.FontWeight = FontWeights.DemiBold;
                        }
                    }

                    SetFirstLastNodeColors(idx, levelGroup);

                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Creates a SectorParagraphAttributes object for a Sector.
        /// </summary>
        /// <param name="displayLevel"></param>
        /// <param name="topMarginExtra"></param>
        /// <param name="bottomMarginExtra"></param>
        /// <returns></returns>
        private void CreateParagraphAttrs(SectorParaAttrs attrs, int displayLevel, int topMarginExtra = 0, int bottomMarginExtra = 0)
        {
            int fontSize = Constants.BASE_FIXED_FONT_SIZE;
            if (!Configuration.UseFixedFont)
            {
                fontSize = SectorParaAttrs.GetVariableFontSizeForLevel(displayLevel);
            }
            fontSize = GuiUtilities.AdjustFontSize(fontSize);

            attrs.FontSize = fontSize;
            attrs.FontWeight = displayLevel == 0 ? FontWeights.DemiBold : FontWeights.Normal;
            attrs.TextAlignment = TextAlignment.Left;
            attrs.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
            attrs.Margin = new Thickness(20 * displayLevel, topMarginExtra, 0, 5 + bottomMarginExtra);
            attrs.TopMarginExtra = topMarginExtra;
            attrs.BottomMarginExtra = bottomMarginExtra;
        }

        /// <summary>
        /// Sets the colors for the last node and the first nodes of child sectors.
        /// </summary>
        /// <param name="sectorIndex"></param>
        /// <param name="levelGroup"></param>
        private void SetFirstLastNodeColors(int sectorIndex, int levelGroup)
        {
            LineSector sector = LineSectors[sectorIndex];

            TreeNode nd = null;
            int i = sector.Nodes.Count - 1;

            if (i >= 0)
            {
                nd = sector.Nodes[i];
            }

            // do not color if this is an index level unless this is the last index level and is not the first node.
            if (nd != null && !IsEffectiveIndexLevel(sector.BranchLevel) || IsLastEffectiveIndexLine(sector.BranchLevel) && nd != sector.Nodes[0])
            {
                if (sector.Nodes.Count > 0 && nd.Parent != null && nd.Parent.Children.Count > 1)
                {
                    sector.ParaAttrs.LastNodeColor = _runColors.GetBrushForLastMove(sector.DisplayLevel, levelGroup);
                }

                // except the first child as it will be the continuation of the top line
                foreach (LineSector ls in sector.Children)
                {
                    if (nd.Children.Count == 0 || ls.Nodes[0] != nd.Children[0])
                    {
                        ls.ParaAttrs.FirstNodeColor = _runColors.GetBrushForLastMove(sector.DisplayLevel, levelGroup);
                    }
                }
            }

        }

        /// <summary>
        /// Checks if the passed branch level is within
        /// the effective levels.
        /// </summary>
        /// <param name="branchLevel"></param>
        /// <returns></returns>
        public bool IsEffectiveIndexLevel(int branchLevel)
        {
            return branchLevel <= EffectiveIndexDepth + 1;
        }

        /// <summary>
        /// Checks if the passed branch level is the
        /// last within the effective levels.
        /// </summary>
        /// <param name="branchLevel"></param>
        /// <returns></returns>
        public bool IsLastEffectiveIndexLine(int branchLevel)
        {
            return branchLevel == EffectiveIndexDepth + 1;
        }


        /// <summary>
        /// Returns the value VariationIndexDepth that will be adjusted
        /// if we currently do not have enough branch levels
        /// </summary>
        public int EffectiveIndexDepth
        {
            get
            {
                int depth = VariationTreeViewUtils.VariationIndexDepth;
                if (depth > MaxBranchLevel - 1)
                {
                    depth = MaxBranchLevel - 1;
                }
                else if (VariationTreeViewUtils.VariationIndexDepth == 0 && !HasIndexLevelZero())
                {
                    // we configured level 0 but there is no stem line to show
                    depth = -1;
                }
                return depth;
            }
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
                    if (lineSector.Parent.Children.Count == 2 && lineSector.Parent.Parent != null && lineSector.Parent.Children[1] == lineSector && lineSector.Parent.Children[0].SectorType == LineSectorType.LEAF)
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

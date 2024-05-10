using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ChessForge
{
    /// <summary>
    /// Derived from VariationTreeView manages the special layout (with variation index)
    /// for the Study View.
    /// </summary>
    public class StudyTreeView : VariationTreeView
    {
        // prefix for Runs in the index paragraph.
        private readonly string _indexrun_ = "indexrun_";

        // prefix for index level lines in the main body
        private readonly string _idxprefix_ = "idxprefix_";

        // prefix for expand elipsis runs
        private readonly string _expelipsis_ = "expelipsis_";

        // indent between levels in the index paragraph.
        private readonly string _indent = "    ";

        // id of the first node in the sector for which index prefix was last clicked 
        private int _lastClickedIndexPrefix = -1;

        /// <summary>
        /// Object managing the layout for this view
        /// </summary>
        public LineSectorManager LineManager;

        /// <summary>
        /// Safe accessor to the chapter's variation index depth.
        /// </summary>
        /// <returns></returns>
        public int VariationIndexDepth
        {
            get { return AppState.ActiveChapter == null ? Configuration.DefaultIndexDepth : AppState.ActiveChapter.VariationIndexDepth.Value; }
        }

        /// <summary>
        /// Instantiates the view.
        /// </summary>
        /// <param name="rtb"></param>
        /// <param name="contentType"></param>
        /// <param name="entityIndex"></param>
        public StudyTreeView(RichTextBox rtb, GameData.ContentType contentType, int entityIndex) : base(rtb, contentType, entityIndex)
        {
            LineManager = new LineSectorManager(this);
        }

        /// <summary>
        /// Overrides the parent's method building the view with the layout specific
        /// to the Study view.
        /// Unlike in the parent's view this is method does not call itself recursively.
        /// It builds the list of lines first (there is a recursion there) and only then 
        /// populates the view with the lines.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="para"></param>
        /// <param name="includeNumber"></param>
        override protected void BuildTreeLineText(TreeNode root, Paragraph para, bool includeNumber)
        {
            DisplayLevelAttrs.ResetLastMoveBrush();
            LineManager.BuildLineSectors(root);

            // it could be that a new move was made and it is "hidden" under a collapsed root
            UncollapseMove(_mainWin.ActiveLine.GetSelectedTreeNode());

            CreateVariationIndexPara();
            CreateParagraphs(para);
        }

        /// <summary>
        /// Finds out if the move has a collapsed ancestor
        /// and if so, invokes UncollapseSector on that ancestor.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns>true if the uncollapse was required</returns>
        public bool UncollapseMove(TreeNode nd)
        {
            bool result = false;

            if (nd != null)
            {
                TreeNode collapsedAncestor = null;
                while (nd.Parent != null)
                {
                    if (nd.Parent.IsCollapsed)
                    {
                        collapsedAncestor = nd.Parent;
                        result = true;
                        break;
                    }
                    nd = nd.Parent;
                }

                UncollapseSector(collapsedAncestor);
            }

            return result;
        }

        /// <summary>
        /// Expanding the clicked sector was requested from the menu.
        /// </summary>
        /// <param name="sender"></param>
        public void ExpandSectorFromMenu(object sender)
        {
            TreeNode node = _mainVariationTree.GetNodeFromNodeId(_lastClickedIndexPrefix);
            ExpandSector(node);
        }

        /// <summary>
        /// Collapsing of the clicked sector was requested from the menu.
        /// </summary>
        /// <param name="sender"></param>
        public void CollapseSectorFromMenu(object sender)
        {
            TreeNode node = _mainVariationTree.GetNodeFromNodeId(_lastClickedIndexPrefix);
            CollapseSector(node);
        }

        /// <summary>
        /// Expanding of all sectors was requested from the menu.
        /// </summary>
        /// <param name="sender"></param>
        public void ExpandAllSectorsFromMenu(object sender)
        {
            foreach (TreeNode nd in _mainVariationTree.Nodes)
            {
                nd.IsCollapsed = false;
            }
            BuildFlowDocumentForVariationTree();

            TreeNode selNode = GetSelectedNode();
            if (selNode != null && _dictNodeToRun.ContainsKey(selNode.NodeId))
            {
                SelectRun(_dictNodeToRun[selNode.NodeId], 1, MouseButton.Left);
            }
            BringSelectedRunIntoView();
        }

        /// <summary>
        /// Collapsing of all sectors was requested from the menu.
        /// </summary>
        /// <param name="sender"></param>
        public void CollapseAllSectorsFromMenu(object sender)
        {
            MarkAllSectorsAsCollapsed();

            try
            {
                // get the last node in the stem line
                LineSector stemLine = LineManager.LineSectors[0];
                int nodeCount = stemLine.Nodes.Count;

                TreeNode selNode = stemLine.Nodes[nodeCount - 1];
                AppState.MainWin.SetActiveLine("1", selNode.NodeId, false);
                BuildFlowDocumentForVariationTree();

                if (selNode != null && _dictNodeToRun.ContainsKey(selNode.NodeId))
                {
                    SelectRun(_dictNodeToRun[selNode.NodeId], 1, MouseButton.Left);
                }
                BringSelectedRunIntoView();
            }
            catch { }
        }

        /// <summary>
        /// Expanding of the clicked sector and collapsing of all others 
        /// was requested from the menu.
        /// </summary>
        /// <param name="sender"></param>
        public void ExpandThisSectorOnlyFromMenu(object sender)
        {
            MarkAllSectorsAsCollapsed();
            TreeNode nd = _mainVariationTree.GetNodeFromNodeId(_lastClickedIndexPrefix);
            ExpandSector(nd);
        }

        /// <summary>
        /// Mark all sectors as collapsed.
        /// </summary>
        private void MarkAllSectorsAsCollapsed()
        {
            foreach (LineSector sector in LineManager.LineSectors)
            {
                if (sector.Nodes.Count > 0)
                {
                    if (IsEffectiveIndexLevel(sector.BranchLevel) && sector.Nodes[0].LineId != "1")
                    {
                        sector.Nodes[0].IsCollapsed = true;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the value VariationIndexDepth that will be adjusted
        /// if we currently do not have enough branch levels
        /// </summary>
        private int EffectiveIndexDepth
        {
            get
            {
                int depth = VariationIndexDepth;
                if (depth > LineManager.MaxBranchLevel - 1)
                {
                    depth = LineManager.MaxBranchLevel - 1;
                }
                else if (VariationIndexDepth == 0 && !LineManager.HasIndexLevelZero())
                {
                    // we configured level 0 but there is no stem line to show
                    depth = -1;
                }
                return depth;
            }
        }

        /// <summary>
        /// Checks if the passed branch level is within
        /// the effective levels.
        /// </summary>
        /// <param name="branchLevel"></param>
        /// <returns></returns>
        private bool IsEffectiveIndexLevel(int branchLevel)
        {
            return branchLevel <= EffectiveIndexDepth + 1;
        }

        /// <summary>
        /// Checks if the passed branch level is the
        /// last within the effective levels.
        /// </summary>
        /// <param name="branchLevel"></param>
        /// <returns></returns>
        private bool IsLastEffectiveIndexLine(int branchLevel)
        {
            return branchLevel == EffectiveIndexDepth + 1;
        }

        /// <summary>
        /// Increment the index depth paying attention to limits
        /// and empty level 0 (in case of e.g. 1.e4 and 1.d4)
        /// </summary>
        public void IncrementVariationIndexDepth()
        {
            Chapter chapter = AppState.ActiveChapter;
            if (chapter != null)
            {
                int depth = EffectiveIndexDepth;
                if (depth == -1 && !LineManager.HasIndexLevelZero())
                {
                    depth = 1;
                }
                else
                {
                    if (depth + 1 < LineManager.MaxBranchLevel)
                    {
                        depth++;
                    }
                }
                // do not set the chapter value if this is still lower than previously configured.
                if (depth > chapter.VariationIndexDepth)
                {
                    chapter.VariationIndexDepth = depth;
                    AppState.IsIndexDepthDirty = true;   
                }
            }
        }

        /// <summary>
        /// Decrement the index depth paying attention to limits
        /// and empty level 0 (in case of e.g. 1.e4 and 1.d4).
        /// Update the Chapter's VariationIndexDepth.
        /// </summary>
        public void DecrementVariationIndexDepth()
        {
            Chapter chapter = AppState.ActiveChapter;
            if (chapter != null)
            {
                int depth = EffectiveIndexDepth;
                if (depth == 1 && !LineManager.HasIndexLevelZero())
                {
                    depth = -1;
                }
                else
                {
                    if (depth > -1)
                    {
                        depth--;
                    }
                }
                chapter.VariationIndexDepth = depth;
                AppState.IsIndexDepthDirty = true;
            }
        }

        /// <summary>
        /// Finds sector with the passed node and ensures it is not collapsed
        /// </summary>
        /// <param name="nd"></param>
        private void UncollapseSector(TreeNode nd)
        {
            if (nd != null)
            {
                foreach (LineSector sector in LineManager.LineSectors)
                {
                    if (sector.Nodes.Count > 0 && sector.Nodes.Find(x => x == nd) != null)
                    {
                        sector.Nodes[0].IsCollapsed = false;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a paragraph for the "Variation Index" title and the depth arrows.
        /// </summary>
        private void CreateVariationIndexHeader()
        {
            Paragraph para = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 6),
            };

            Run r = new Run(Properties.Resources.VariationIndex + "  ");
            para.Inlines.Add(r);
            para.FontWeight = FontWeights.DemiBold;

            InsertArrowRuns(para);
            Document.Blocks.Add(para);
        }

        /// <summary>
        /// Creates the Index paragraph.
        /// </summary>
        private void CreateVariationIndexPara()
        {
            if (EffectiveIndexDepth >= 0)
            {
                Paragraph para = CreateParagraph("0", true);
                para.Foreground = ChessForgeColors.CurrentTheme.IndexPrefixForeground;
                para.FontWeight = FontWeights.Normal;
                para.FontSize = para.FontSize - 1;
                para.Margin = new Thickness(0, 0, 0, 0);


                bool first = true;
                foreach (LineSector sector in LineManager.LineSectors)
                {
                    if (sector.Nodes.Count == 0)
                    {
                        continue;
                    }

                    int level = sector.BranchLevel;
                    if (IsEffectiveIndexLevel(level))
                    {
                        if (first)
                        {
                            CreateVariationIndexHeader();
                            first = false;
                        }

                        if (sector.Nodes[0].LineId == "1")
                        {
                            // Build the stem line
                            // Note we don't want an indent here 'coz if the stem line is long it will wrap ugly
                            bool validMove = false;
                            foreach (TreeNode nd in sector.Nodes)
                            {
                                Run rMove = BuildIndexNodeAndAddToPara(nd, false, para);
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

                            Run rIdTitle = BuildSectionIdTitle(sector.Nodes[0].LineId);
                            para.Inlines.Add(rIdTitle);
                            if (IsLastEffectiveIndexLine(level) || sector.Nodes[sector.Nodes.Count - 1].Children.Count == 0)
                            {
                                BuildIndexNodeAndAddToPara(sector.Nodes[0], true, para);
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
                                    if (i < firstSkip || i > lastSkip)
                                    {
                                        TreeNode nd = sector.Nodes[i];
                                        BuildIndexNodeAndAddToPara(nd, firstMove, para);
                                        firstMove = false;
                                    }
                                    else if (i == firstSkip)
                                    {
                                        Run rElipsis = new Run(" [...] ");
                                        rElipsis.FontWeight = FontWeights.Normal;
                                        para.Inlines.Add(rElipsis);
                                        firstMove = true;
                                    }
                                }
                            }
                            rIdTitle.FontWeight = FontWeights.DemiBold;

                            para.Inlines.Add(new Run("\n"));
                        }
                    }
                }
                Document.Blocks.Add(para);
            }
            else if (_pageHeaderParagraph != null)
            {
                _pageHeaderParagraph.ToolTip = Properties.Resources.ShowIndex;
            }
        }

        /// <summary>
        /// Creates paragraphs from the LineSectors.
        /// </summary>
        /// <param name="firstPara"></param>
        private void CreateParagraphs(Paragraph firstPara)
        {
            // sectors that are under a collapsed sector
            List<LineSector> doNotShow = new List<LineSector>();

            // TODO: redo so that we used the "firstPara" for VariationIndex.
            // Be aware it already contains a Run for the root node!
            Document.Blocks.Remove(firstPara);

            int levelGroup = 0;
            bool firstAtIndexLevel2 = true;

            for (int n = 0; n < LineManager.LineSectors.Count; n++)
            {
                LineSector sector = LineManager.LineSectors[n];
                if (doNotShow.Find(x => x == sector) != null)
                {
                    continue;
                }

                if (sector.Nodes.Count == 0 || sector.Nodes.Count == 1 && sector.Nodes[0].NodeId == 0 && string.IsNullOrEmpty(sector.Nodes[0].Comment))
                {
                    continue;
                }
                if (n > 0 && sector.DisplayLevel != LineManager.LineSectors[n - 1].BranchLevel)
                {
                    levelGroup++;
                }

                try
                {
                    Paragraph para;
                    if (sector.DisplayLevel < 0)
                    {
                        sector.DisplayLevel = 0;
                    }

                    int topMarginExtra = 0;
                    int bottomMarginExtra = 0;

                    if (!IsEffectiveIndexLevel(sector.BranchLevel))
                    {
                        // add extra room above/below first/last line in a block at the same level 
                        if (n > 0 && LineManager.LineSectors[n - 1].DisplayLevel < sector.DisplayLevel)
                        {
                            topMarginExtra = DisplayLevelAttrs.EXTRA_MARGIN;
                        }
                        if (n < LineManager.LineSectors.Count - 1 && LineManager.LineSectors[n + 1].DisplayLevel < sector.DisplayLevel)
                        {
                            bottomMarginExtra = DisplayLevelAttrs.EXTRA_MARGIN;
                        }
                        // also make sure that the sector outside the indexed range is not expanded!
                        foreach (TreeNode node in sector.Nodes)
                        {
                            node.IsCollapsed = false;
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
                            topMarginExtra = DisplayLevelAttrs.EXTRA_MARGIN;
                        }
                    }

                    para = DisplayLevelAttrs.CreateStudyParagraph(sector.DisplayLevel, topMarginExtra, bottomMarginExtra);
                    if (para.FontWeight == FontWeights.Bold)
                    {
                        para.FontWeight = FontWeights.Normal;
                    }

                    if (IsEffectiveIndexLevel(sector.BranchLevel))
                    {
                        InsertIndexPrefixRun(sector, para);
                    }
                    else
                    {
                        if (IsLastEffectiveIndexLine(sector.DisplayLevel + 1))
                        {
                            para.FontWeight = FontWeights.DemiBold;
                        }
                    }

                    BuildSectorRuns(sector, para, levelGroup, doNotShow);
                    sector.HostPara = para;
                    Document.Blocks.Add(para);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Inserts the Runs with arrow for incrementing/decrementing the Variation Index depth.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="downOnly"></param>
        private void InsertArrowRuns(Paragraph para, bool downOnly = false)
        {
            Run rPlus = new Run(Constants.CHAR_DOWN_ARROW.ToString());
            rPlus.FontWeight = FontWeights.Normal;
            rPlus.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
            rPlus.ToolTip = Properties.Resources.ToolTipIncreaseIndexDepth;
            rPlus.PreviewMouseDown += EventDownArrowClicked;
            para.Inlines.Add(rPlus);

            if (!downOnly)
            {
                Run rMinus = new Run(Constants.CHAR_UP_ARROW.ToString());
                rMinus.FontWeight = FontWeights.Normal;
                rMinus.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
                rMinus.ToolTip = Properties.Resources.ToolTipDecreaseIndexDepth;
                rMinus.PreviewMouseDown += EventUpArrowClicked;
                para.Inlines.Add(rMinus);
            }
        }

        /// <summary>
        /// Builds Runs for TreeNodes in the sector.
        /// </summary>
        /// <param name="sector"></param>
        /// <param name="para"></param>
        /// <param name="levelGroup"></param>
        private void BuildSectorRuns(LineSector sector, Paragraph para, int levelGroup, List<LineSector> doNotShow)
        {
            bool includeNumber = true;
            bool parenthesis = false;

            bool collapsed = false;
            for (int i = 0; i < sector.Nodes.Count; i++)
            {
                if (sector.IsCollapsed)
                {
                    // mark as collapsed and let this iteration complete to insert the first Run
                    collapsed = true;
                }

                TreeNode nd = sector.Nodes[i];
                if (nd.NodeId == LineSector.OPEN_BRACKET)
                {
                    para.Inlines.Add(new Run("( "));
                    parenthesis = true;
                }
                else if (nd.NodeId == LineSector.CLOSE_BRACKET)
                {
                    para.Inlines.Add(new Run(") "));
                    parenthesis = true;
                }
                else
                {
                    if (parenthesis)
                    {
                        includeNumber = true;
                    }
                    Run r = BuildNodeTextAndAddToPara(nd, includeNumber, para, sector.DisplayLevel, !collapsed);
                    if (r.FontWeight == FontWeights.Bold)
                    {
                        r.FontWeight = FontWeights.DemiBold;
                    }
                    if (para.FontWeight == FontWeights.Normal)
                    {
                        r.FontWeight = FontWeights.Normal;
                    }
                    r.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
                    parenthesis = false;

                    if (i == 0 && sector.FirstNodeColor != null && !IsEffectiveIndexLevel(sector.BranchLevel))
                    {
                        r.Foreground = sector.FirstNodeColor;
                    }
                    if (sector.Nodes.Count > 1 && i == sector.Nodes.Count - 1 && sector.BranchLevel >= EffectiveIndexDepth)
                    {
                        ColorLastNode(sector, r, nd, levelGroup);
                    }
                }
                includeNumber = false;

                if (collapsed)
                {
                    // insert the elipsis Run and exit
                    InsertCollapseElipsisRun(para, nd, sector, doNotShow);
                    break;
                }
            }
        }

        /// <summary>
        /// Inserts a Run with the index level Id
        /// </summary>
        /// <param name="sector"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        private Run InsertIndexPrefixRun(LineSector sector, Paragraph para)
        {
            TreeNode startNode = sector.Nodes[0];

            Run rIndexTitle = BuildSectionIdTitle(startNode.LineId);
            rIndexTitle.Name = _idxprefix_ + startNode.NodeId.ToString();
            rIndexTitle.PreviewMouseDown += EventIdxPrefixRunClicked;
            rIndexTitle.Foreground = ChessForgeColors.CurrentTheme.IndexPrefixForeground;
            rIndexTitle.ToolTip = Properties.Resources.TtClickToExpandCollapse;
            para.Inlines.Add(rIndexTitle);
            para.FontWeight = FontWeights.DemiBold;

            return rIndexTitle;
        }

        /// <summary>
        /// Inserts a Run symbolizing the collapsed state of the Sector.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="nd"></param>
        /// <param name="sector"></param>
        /// <param name="doNotShow"></param>
        /// <returns></returns>
        private Run InsertCollapseElipsisRun(Paragraph para, TreeNode nd, LineSector sector, List<LineSector> doNotShow)
        {
            Run elipsis = new Run(" [...]");

            elipsis.Name = _expelipsis_ + nd.NodeId.ToString();
            elipsis.PreviewMouseDown += EventIdxPrefixRunClicked;
            elipsis.ToolTip = Properties.Resources.TtClickToExpand;
            para.Inlines.Add(elipsis);
            LineManager.GetSubTree(sector, doNotShow);

            return elipsis;
        }

        /// <summary>
        /// Sets the foreground color for the passed Run.
        /// If this is within the index levels do not set the color
        /// but set FirstMoveColor on the child sectors that are
        /// at the lower level
        /// </summary>
        /// <param name="sector"></param>
        /// <param name="r"></param>
        /// <param name="nd"></param>
        private void ColorLastNode(LineSector sector, Run r, TreeNode nd, int levelGroup)
        {
            // do not color if this is an index level unless this is the last index level and is not the first node.
            //if (!LineManager.IsIndexLevel(sector.BranchLevel) || LineManager.IsLastIndexLine(sector.BranchLevel) && nd != sector.Nodes[0])
            if (!IsEffectiveIndexLevel(sector.BranchLevel) || IsLastEffectiveIndexLine(sector.BranchLevel) && nd != sector.Nodes[0])
            {
                if (sector.Nodes.Count > 0 && nd.Parent != null && nd.Parent.Children.Count > 1)
                {
                    r.Foreground = DisplayLevelAttrs.GetBrushForLastMove(sector.DisplayLevel, levelGroup);
                }

                // except the first child as it will be the continuation of the top line
                foreach (LineSector ls in sector.Children)
                {
                    if (nd.Children.Count == 0 || ls.Nodes[0] != nd.Children[0])
                    {
                        ls.FirstNodeColor = DisplayLevelAttrs.GetBrushForLastMove(sector.DisplayLevel, levelGroup);
                    }
                }
            }
        }

        /// <summary>
        /// Builds a Run for the move in the Index paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeNumber"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        protected Run BuildIndexNodeAndAddToPara(TreeNode nd, bool includeNumber, Paragraph para)
        {
            string nodeText = BuildNodeText(nd, includeNumber);

            SolidColorBrush fontColor = null;
            Run rMove = AddIndexRunToParagraph(nd, para, nodeText, fontColor);
            rMove.FontWeight = FontWeights.Normal;
            return rMove;
        }

        // guid for checking single click versus double click.
        private Guid _clickPageHeaderGuid;

        /// <summary>
        /// Adds the single-click behaviour to the base function.
        /// When the header is clicked while there is no Variation Index,
        /// it will be created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        override protected void EventPageHeaderClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ClickCount == 1)
                {
                    // generate fresh guid to hold in Delay
                    Guid guid = Guid.NewGuid();
                    _clickPageHeaderGuid = guid;

                    Task.Run(async () =>
                    {
                        // Delay in case the double-click is coming.
                        await Task.Delay(300);

                        // if new guid was generated, it means that there was a second click
                        if (guid == _clickPageHeaderGuid)
                        {
                            if (VariationIndexDepth == -1)
                            {
                                IncrementVariationIndexDepth();
                            }

                            _mainWin.Dispatcher.Invoke(() =>
                            {
                                BuildFlowDocumentForVariationTree();
                            });

                            e.Handled = true;
                            return;
                        }
                    });
                }
                else if (e.ClickCount == 2)
                {
                    _clickPageHeaderGuid = Guid.NewGuid();
                    base.EventPageHeaderClicked(sender, e);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("EventPageHeaderClicked()", ex);
            }
        }

        /// <summary>
        /// Handles a mouse click on a move in the index paragraph.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventIndexRunClicked(object sender, MouseButtonEventArgs e)
        {
            Run r = e.Source as Run;
            if (r != null)
            {
                ClearCopySelect();
                int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
                // if sectors are collapsed the Run may not be present
                if (_dictNodeToRun.ContainsKey(nodeId))
                {
                    Run target = _dictNodeToRun[nodeId];
                    SelectRun(target, 1, e.ChangedButton);
                    BringSelectedRunIntoView();
                }
                else
                {
                    TreeNode nd = _mainVariationTree.GetNodeFromNodeId(nodeId);
                    if (UncollapseMove(nd))
                    {
                        BuildFlowDocumentForVariationTree();
                        Run target = _dictNodeToRun[nodeId];
                        SelectRun(target, 1, e.ChangedButton);
                        BringSelectedRunIntoView();
                    }
                }
            }
        }

        /// <summary>
        /// An index prefix Run was clicked.
        /// Toggle its collapse/expand status.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventIdxPrefixRunClicked(object sender, MouseButtonEventArgs e)
        {
            Run r = e.Source as Run;
            if (r != null)
            {
                ClearCopySelect();
                int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);

                if (nodeId >= 0 && e.ChangedButton == MouseButton.Left)
                {
                    try
                    {
                        if (nodeId > 0)
                        {
                            TreeNode nd = _mainVariationTree.GetNodeFromNodeId(nodeId);
                            nd.IsCollapsed = !nd.IsCollapsed;

                            if (nd.IsCollapsed)
                            {
                                CollapseSector(nd);
                            }
                            else
                            {
                                ExpandSector(nd);
                            }
                        }
                    }
                    catch { }
                }
                else if (nodeId >= 0 && e.ChangedButton == MouseButton.Right)
                {
                    try
                    {
                        _lastClickedIndexPrefix = nodeId;
                        ContextMenu contextMenu = _mainWin.Resources["CmIndexExpandCollapse"] as ContextMenu;
                        if (contextMenu != null)
                        {
                            EnableExpandCollapseMenuItems(contextMenu, nodeId);
                            contextMenu.IsOpen = true;
                        }
                    }
                    catch { }
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// Enables disable menu items according to the state of the clicked node. 
        /// </summary>
        /// <param name="nodeId"></param>
        private void EnableExpandCollapseMenuItems(ContextMenu contextMenu, int nodeId)
        {
            try
            {
                TreeNode nd = _mainVariationTree.GetNodeFromNodeId(nodeId);
                if (nd != null)
                {
                    foreach (var item in contextMenu.Items)
                    {
                        if (item is MenuItem)
                        {
                            MenuItem menuItem = item as MenuItem;
                            switch (menuItem.Name)
                            {
                                case "UiMnciExpand":
                                    menuItem.IsEnabled = !IsNodeExpanded(nd);
                                    break;
                                case "UiMnciCollapse":
                                    menuItem.IsEnabled = !nd.IsCollapsed;
                                    break;
                                case "UiMnciExpandAll":
                                    menuItem.IsEnabled = !IsAllExpanded();
                                    break;
                                case "UiMnciCollapseAll":
                                    menuItem.IsEnabled = !IsAllCollapsed(null);
                                    break;
                                case "UiMnciExpandThisOne":
                                    menuItem.IsEnabled = !IsAllElseCollapsed(nd);
                                    break;

                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Checks if all sectors are expanded
        /// </summary>
        /// <returns></returns>
        private bool IsAllExpanded()
        {
            bool res = true;

            foreach (TreeNode nd in _mainVariationTree.Nodes)
            {
                if (nd.IsCollapsed)
                {
                    res = false;
                    break;
                }
            }

            return res;
        }

        /// <summary>
        /// Determines of the passed node and all descendants are expanded.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns>True if the node and all descendants are expanded. Otherwise false.</returns>
        private bool IsNodeExpanded(TreeNode nd)
        {
            bool isExpanded = true;

            if (nd != null)
            {
                if (nd.IsCollapsed)
                {
                    isExpanded = false;
                }
                else
                {
                    foreach (TreeNode child in nd.Children)
                    {
                        if (!IsNodeExpanded(child))
                        {
                            isExpanded = false;
                            break;
                        }
                    }
                }
            }

            return isExpanded;
        }

        /// <summary>
        /// Checks if all sectors are collapsed except the one passed (if not null).
        /// </summary>
        /// <returns></returns>
        private bool IsAllCollapsed(TreeNode leftExpanded)
        {
            bool res = true;

            foreach (LineSector sector in LineManager.LineSectors)
            {
                if (!IsEffectiveIndexLevel(sector.BranchLevel))
                {
                    break;
                }
                if (sector.Nodes.Count > 0 && sector.Nodes[0].LineId != "1" && !sector.Nodes[0].IsCollapsed && sector.Nodes[0] != leftExpanded)
                {
                    res = false;
                    break;
                }
            }

            return res;
        }

        /// <summary>
        /// Checks if all sectors are collapsed except the one passed 
        /// which should be expanded..
        /// </summary>
        /// <param name="leftExpanded"></param>
        /// <returns></returns>
        private bool IsAllElseCollapsed(TreeNode leftExpanded)
        {
            bool res = false;

            if (leftExpanded != null)
            {
                if (leftExpanded.IsCollapsed)
                {
                    res = false;
                }
                else if (IsAllCollapsed(leftExpanded))
                {
                    res = true;
                }
            }

            return res;
        }

        /// <summary>
        /// Expands the sector starting with the passed NodeId.
        /// </summary>
        /// <param name="nodeId"></param>
        private void ExpandSector(TreeNode nd)
        {
            if (nd != null && nd.NodeId >= 0)
            {
                // uncollapse the node and all descendants
                ExpandNode(nd);

                // make sure all above are not collapsed, as in some cases they may be
                while (nd.Parent != null)
                {
                    nd.IsCollapsed = false;
                    nd = nd.Parent;
                }


                BuildFlowDocumentForVariationTree();
                TreeNode selNode = GetSelectedNode();
                if (selNode != null && _dictNodeToRun.ContainsKey(selNode.NodeId))
                {
                    SelectRun(_dictNodeToRun[selNode.NodeId], 1, MouseButton.Left);
                }
                BringSelectedRunIntoView();
            }
        }

        /// <summary>
        /// Marks the passed node and all its descendants as expanded.
        /// </summary>
        /// <param name="nd"></param>
        private void ExpandNode(TreeNode nd)
        {
            if (nd != null)
            {
                nd.IsCollapsed = false;
                foreach (TreeNode child in nd.Children)
                {
                    ExpandNode(child);
                }
            }
        }

        /// <summary>
        /// Collapses the sector beginning at the passed NodeId
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private TreeNode CollapseSector(TreeNode nd)
        {
            TreeNode adjustedSelection = null;

            if (nd != null && nd.NodeId >= 0)
            {
                nd.IsCollapsed = true;

                //if collapsed, check if we need to update the selection
                adjustedSelection = UpdateSelectedNodeAfterCollapse(nd);
                if (adjustedSelection != null)
                {
                    AppState.MainWin.SetActiveLine(adjustedSelection.LineId, adjustedSelection.NodeId, false);
                }
                BuildFlowDocumentForVariationTree();

                if (adjustedSelection == null)
                {
                    adjustedSelection = GetSelectedNode();
                }
                if (adjustedSelection != null && _dictNodeToRun.ContainsKey(adjustedSelection.NodeId))
                {
                    SelectRun(_dictNodeToRun[adjustedSelection.NodeId], 1, MouseButton.Left);
                }
            }

            return adjustedSelection;
        }

        /// <summary>
        /// The passed node was where the user requested a sector collapse.
        /// If the currenly selected node is the node being collapsed or the
        /// passed node is its ancestor then we need to update selection to the 
        /// first expanded ancestor of the node that was clicked.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private TreeNode UpdateSelectedNodeAfterCollapse(TreeNode nd)
        {
            TreeNode adjustedNode = null;

            TreeNode selectedNode = GetSelectedNode();
            if (selectedNode != null && selectedNode.NodeId != 0)
            {
                if (selectedNode == nd || TreeUtils.IsAncestor(selectedNode, nd))
                {
                    TreeNode ancestor = TreeUtils.GetFirstExpandedAncestor(nd);
                    if (ancestor != null)
                    {
                        adjustedNode = ancestor;
                    }
                }
            }
            else
            {
                adjustedNode = selectedNode;
            }

            return adjustedNode;
        }

        /// <summary>
        /// The Up Arrow to decrement the depth of the Variation Index was clicked. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventUpArrowClicked(object sender, MouseButtonEventArgs e)
        {
            Run r = e.Source as Run;
            if (r != null)
            {
                if (EffectiveIndexDepth > -1)
                {
                    Chapter chapter = AppState.ActiveChapter;
                    if (chapter != null)
                    {
                        DecrementVariationIndexDepth();
                    }
                }
                BuildFlowDocumentForVariationTree();
                SelectLineAndMove(_mainVariationTree.SelectedLineId, _mainVariationTree.SelectedNodeId);
            }
            e.Handled = true;
        }

        /// <summary>
        /// The Down Arrow to increment the depth of the Variation Index was clicked. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventDownArrowClicked(object sender, MouseButtonEventArgs e)
        {
            Run r = e.Source as Run;
            if (r != null)
            {
                if (EffectiveIndexDepth < Configuration.MAX_INDEX_DEPTH && EffectiveIndexDepth < LineManager.MaxBranchLevel - 1)
                {
                    Chapter chapter = AppState.ActiveChapter;
                    if (chapter != null)
                    {
                        IncrementVariationIndexDepth();
                    }
                }
                BuildFlowDocumentForVariationTree();
                SelectLineAndMove(_mainVariationTree.SelectedLineId, _mainVariationTree.SelectedNodeId);
            }
            e.Handled = true;
        }

        /// <summary>
        /// Adds a Run to the Index paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="para"></param>
        /// <param name="text"></param>
        /// <param name="fontColor"></param>
        /// <returns></returns>
        private Run AddIndexRunToParagraph(TreeNode nd, Paragraph para, string text, SolidColorBrush fontColor)
        {
            Run r = null;

            try
            {
                r = new Run(text.ToString());
                r.Name = _indexrun_ + nd.NodeId.ToString();
                r.PreviewMouseDown += EventIndexRunClicked;

                // only used the passed fontColor on the first move in the paragraph
                if (fontColor != null && para.Inlines.Count == 0)
                {
                    r.Foreground = fontColor;
                    r.FontWeight = FontWeights.DemiBold;
                }

                if (para.Margin.Left == 0 && nd.IsMainLine())
                {
                    r.FontWeight = FontWeights.DemiBold;
                    para.Inlines.Add(r);
                }
                else
                {
                    para.Inlines.Add(r);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("AddIndexRunToParagraph()", ex);
            }

            return r;
        }

        /// <summary>
        /// Builds the section level title in the form of "A.1.2".
        /// </summary>
        /// <param name="lineId"></param>
        /// <returns></returns>
        private Run BuildSectionIdTitle(string lineId)
        {
            Run r = new Run();

            if (lineId != null && lineId != "1")
            {
                StringBuilder sb = new StringBuilder();
                string[] tokens = lineId.Split('.');
                for (int i = 1; i < tokens.Length; i++)
                {
                    if (i == 1)
                    {
                        sb.Append((char)(tokens[1][0] - '1' + 'A'));
                    }
                    else
                    {
                        //if (i > 2)
                        {
                            sb.Append('.');
                        }
                        sb.Append(tokens[i]);
                    }
                }

                sb.Append(") ");
                r.Text = sb.ToString();
            }

            return r;
        }
    }
}

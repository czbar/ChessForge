using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

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

        // id of the first node in the sector for which index prefix was last clicked 
        private int _lastClickedIndexPrefix = -1;

        /// <summary>
        /// Object managing the layout for this view
        /// </summary>
        public LineSectorManager LineManager;

        /// <summary>
        /// Instantiates the view.
        /// </summary>
        /// <param name="rtb"></param>
        /// <param name="contentType"></param>
        /// <param name="entityIndex"></param>
        public StudyTreeView(RichTextBox rtb, GameData.ContentType contentType, bool isPrinting = false) : base(rtb, contentType)
        {
            _isPrinting = isPrinting;
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
        override protected void BuildTreeLineText(FlowDocument doc, TreeNode root, Paragraph para, bool includeNumber)
        {
            LineManager.ResetLastMoveBrush();
            LineManager.BuildLineSectors(root);

            // it could be that a new move was made and it is "hidden" under a collapsed root
            UncollapseMove(_mainWin.ActiveLine.GetSelectedTreeNode());

            if (LineManager.EffectiveIndexDepth >= 0)
            {
                LineManager.CreateIndexHeaderPara();
                LineManager.PopulateIndexHeaderPara();
                doc.Blocks.Add(LineManager.IndexHeaderPara);

                LineManager.CreateIndexContentPara();
                LineManager.PopulateIndexContentPara();
                doc.Blocks.Add(LineManager.IndexContentPara);
            }
            else
            {
                // set index paras to null as otherwise it will confuse UpdateLayout (exception when attempting to InsertAfter IndexPara)
                LineManager.IndexHeaderPara = null;
                LineManager.IndexContentPara = null;

                if (PageHeaderParagraph != null)
                {
                    PageHeaderParagraph.ToolTip = Properties.Resources.ShowIndex;
                }
            }

            CreateParagraphs(doc, para);
        }

        /// <summary>
        /// Whether diagram inserted here should be large or small.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        override protected bool IsLargeDiagram(TreeNode nd)
        {
            return true;
        }

        /// <summary>
        /// Updates the layout after a move was added.
        /// Creates a new LineSectorManager instance,
        /// calculates the new set LineSectors, and the index paragraphs,
        /// then updates the paragraphs where there are differences.
        /// </summary>
        public void UpdateLayoutOnAddedMove()
        {
            LineSectorManager updateLineManger = new LineSectorManager(null);
            updateLineManger.BuildLineSectors(ShownVariationTree.Nodes[0]);

            if (updateLineManger.EffectiveIndexDepth >= 0 && LineManager.IndexHeaderPara == null)
            {
                LineManager.CreateIndexHeaderPara();
                HostRtb.Document.Blocks.InsertAfter(PageHeaderParagraph, LineManager.IndexHeaderPara);

                LineManager.CreateIndexContentPara();
                HostRtb.Document.Blocks.InsertAfter(LineManager.IndexHeaderPara, LineManager.IndexContentPara);
            }

            // NOTE: on added move we don't need to worry to worry about removing index paragraphs
            // in case updateLineManger.EffectiveIndexDepth <= 0 as they are not present in the first place. 

            LineManager.MaxBranchLevel = updateLineManger.MaxBranchLevel;
            LineManager.PopulateIndexHeaderPara();

            TreeViewUpdater updater = new TreeViewUpdater(LineManager, updateLineManger, this);
            updater.MoveAdded();

            LineManager.PopulateIndexContentPara();
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
            BuildFlowDocumentForVariationTree(false);

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
                BuildFlowDocumentForVariationTree(false);

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
                    if (LineManager.IsEffectiveIndexLevel(sector.BranchLevel) && sector.Nodes[0].LineId != "1")
                    {
                        sector.Nodes[0].IsCollapsed = true;
                    }
                }
            }
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
                int depth = LineManager.EffectiveIndexDepth;
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
                int depth = LineManager.EffectiveIndexDepth;
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
        /// Creates paragraphs from the LineSectors.
        /// </summary>
        /// <param name="firstPara"></param>
        private void CreateParagraphs(FlowDocument doc, Paragraph firstPara)
        {
            // sectors that are under a collapsed sector
            List<LineSector> doNotShow = new List<LineSector>();

            // TODO: redo so that we used the "firstPara" for VariationIndex.
            // Be aware it already contains a Run for the root node!
            doc.Blocks.Remove(firstPara);

            bool firstAtIndexLevel2 = true;

            for (int n = 0; n < LineManager.LineSectors.Count; n++)
            {
                LineSector sector = LineManager.LineSectors[n];
                if (!sector.IsShown)
                {
                    continue;
                }

                if (sector.Nodes.Count == 0 || sector.Nodes.Count == 1 && sector.Nodes[0].NodeId == 0 && string.IsNullOrEmpty(sector.Nodes[0].Comment))
                {
                    continue;
                }

                try
                {
                    Paragraph para;
                    if (sector.DisplayLevel < 0)
                    {
                        sector.DisplayLevel = 0;
                    }

                    if (!LineManager.IsEffectiveIndexLevel(sector.BranchLevel))
                    {
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
                    }

                    para = LineSectorManager.CreateStudyParagraph(sector.ParaAttrs, sector.DisplayLevel);
                    if (para.FontWeight == FontWeights.Bold)
                    {
                        para.FontWeight = FontWeights.Normal;
                    }

                    sector.HostPara = para;
                    BuildSectorRuns(sector);
                    RemoveTrailingNewLinesInPara(para);
                    doc.Blocks.Add(para);
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
        public void InsertArrowRuns(Paragraph para, bool downOnly = false)
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
        public void BuildSectorRuns(LineSector sector)
        {
            bool includeNumber = true;
            bool parenthesis = false;

            bool collapsed = false;
            Paragraph para = sector.HostPara;

            if (para != null)
            {
                para.Inlines.Clear();

                if (LineManager.IsEffectiveIndexLevel(sector.BranchLevel))
                {
                    InsertIndexPrefixRun(sector, para);
                }

                for (int i = 0; i < sector.Nodes.Count; i++)
                {
                    bool diagram = false;

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
                        Run r = BuildNodeTextAndAddToPara(nd, includeNumber, para, out diagram, sector.DisplayLevel, !collapsed);
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

                        if (i == 0 && sector.ParaAttrs.FirstNodeColor != null && !LineManager.IsEffectiveIndexLevel(sector.BranchLevel))
                        {
                            r.Foreground = sector.ParaAttrs.FirstNodeColor;
                        }
                        if (sector.Nodes.Count > 1 && i == sector.Nodes.Count - 1 && sector.BranchLevel >= LineManager.EffectiveIndexDepth)
                        {
                            if (sector.ParaAttrs.LastNodeColor != null)
                            {
                                r.Foreground = sector.ParaAttrs.LastNodeColor;
                            }
                        }
                    }
                    includeNumber = diagram;

                    if (collapsed)
                    {
                        // insert the elipsis Run and exit
                        InsertCollapseElipsisRun(para, nd, sector);
                        break;
                    }
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
        private Run InsertCollapseElipsisRun(Paragraph para, TreeNode nd, LineSector sector)
        {
            Run elipsis = new Run(" [...]");

            elipsis.Name = _expelipsis_ + nd.NodeId.ToString();
            elipsis.PreviewMouseDown += EventIdxPrefixRunClicked;
            elipsis.ToolTip = Properties.Resources.TtClickToExpand;
            para.Inlines.Add(elipsis);

            return elipsis;
        }

        /// <summary>
        /// Builds a Run for the move in the Index paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeNumber"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public Run BuildIndexNodeAndAddToPara(TreeNode nd, bool includeNumber, Paragraph para)
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
                            if (VariationTreeViewUtils.VariationIndexDepth == -1)
                            {
                                IncrementVariationIndexDepth();
                            }

                            _mainWin.Dispatcher.Invoke(() =>
                            {
                                BuildFlowDocumentForVariationTree(false);
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
                        BuildFlowDocumentForVariationTree(false);
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
                if (!LineManager.IsEffectiveIndexLevel(sector.BranchLevel))
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


                BuildFlowDocumentForVariationTree(false);
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
                BuildFlowDocumentForVariationTree(false);

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
                if (LineManager.EffectiveIndexDepth > -1)
                {
                    Chapter chapter = AppState.ActiveChapter;
                    if (chapter != null)
                    {
                        DecrementVariationIndexDepth();
                    }
                }
                BuildFlowDocumentForVariationTree(false);
                HighlightLineAndMove(HostRtb.Document, _mainVariationTree.SelectedLineId, _mainVariationTree.SelectedNodeId);
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
                if (LineManager.EffectiveIndexDepth < Configuration.MAX_INDEX_DEPTH && LineManager.EffectiveIndexDepth < LineManager.MaxBranchLevel - 1)
                {
                    Chapter chapter = AppState.ActiveChapter;
                    if (chapter != null)
                    {
                        IncrementVariationIndexDepth();
                    }
                }
                BuildFlowDocumentForVariationTree(false);
                HighlightLineAndMove(HostRtb.Document, _mainVariationTree.SelectedLineId, _mainVariationTree.SelectedNodeId);
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
        public Run BuildSectionIdTitle(string lineId)
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
                        if (int.TryParse(tokens[1], out int branchNo))
                        {
                            if (branchNo <= 26) // 'A' - 'Z'
                            {
                                sb.Append((char)((branchNo - 1) + 'A'));
                            }
                            else
                            {
                                sb.Append(BuildSectionIdTitlePart(tokens[i]));
                            }
                        }
                        else
                        {
                            sb.Append('@');
                        }
                    }
                    else
                    {
                        sb.Append('.');
                        sb.Append(tokens[i]);
                    }
                }

                sb.Append(") ");
                r.Text = sb.ToString();
            }

            return r;
        }

        /// <summary>
        /// Invoked when we have to name a branch that is greater than 26th on a given fork.
        /// We have no more single letters so we will name it as A1, A2, ... , A356, A357 etc.
        /// It is highly unlikely that it will ever needed but we have to cover all possibilities.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private string BuildSectionIdTitlePart(string token)
        {
            string partName = "@";

            if (int.TryParse(token, out int branchNo))
            {
                // if branchNo is between 1 and 90 we map a capital letter to it
                if (branchNo <= 26) // 'A' - 'Z' (65 - 90)
                {
                    partName = ((char)((branchNo - 1) + 'A')).ToString();
                }
                else
                {
                    int secondNo = branchNo - 26;
                    partName = "A" + secondNo.ToString();
                }
            }

            return partName;
        }
    }
}

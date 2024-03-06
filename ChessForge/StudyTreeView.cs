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

        // indent between levels in the index paragraph.
        private readonly string _indent = "    ";

        // whether BuildTreeLineText is being invoked for the first time
        private bool _firstInvocation = true;

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
        public StudyTreeView(RichTextBox rtb, GameData.ContentType contentType, int entityIndex) : base(rtb, contentType, entityIndex)
        {
            LineManager = new LineSectorManager(this);
        }

        /// <summary>
        /// Safe accessor to the chapter's variation index depth.
        /// </summary>
        /// <returns></returns>
        public int VariationIndexDepth
        {
            get
            {
                return AppState.ActiveChapter == null ? Configuration.DefaultIndexDepth : AppState.ActiveChapter.VariationIndexDepth.Value;
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
            }
        }

        /// <summary>
        /// Overrides the parent's method building the view with the layout specific
        /// to the Study view.
        /// Unlike, in the parent's view this is method does not call itself recursively.
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

            CreateVariationIndexPara();
            CreateParagraphs(para);

            // if first invocation then this was already called by the client in the SetUpGUI method. 
            if (!_firstInvocation)
            {
                SelectLineAndMove(_mainVariationTree.SelectedLineId, _mainVariationTree.SelectedNodeId);
            }
            _firstInvocation = false;
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
                para.Foreground = ChessForgeColors.VARIATION_INDEX_FORE;
                para.FontWeight = FontWeights.Normal;
                para.FontSize = para.FontSize - 1;
                para.Margin = new Thickness(0, 0, 0, 0);


                bool first = true;
                foreach (LineSector sector in LineManager.LineSectors)
                {
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
        /// Inserts the Runs with arrow for incrementing/decrementing the Variation Index depth.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="downOnly"></param>
        private void InsertArrowRuns(Paragraph para, bool downOnly = false)
        {
            Run rPlus = new Run(Constants.CHAR_DOWN_ARROW.ToString());
            rPlus.FontWeight = FontWeights.Normal;
            rPlus.Foreground = Brushes.Black;
            rPlus.ToolTip = Properties.Resources.ToolTipIncreaseIndexDepth;
            rPlus.PreviewMouseDown += EventDownArrowClicked;
            para.Inlines.Add(rPlus);

            if (!downOnly)
            {
                Run rMinus = new Run(Constants.CHAR_UP_ARROW.ToString());
                rMinus.FontWeight = FontWeights.Normal;
                rMinus.Foreground = Brushes.Black;
                rMinus.ToolTip = Properties.Resources.ToolTipDecreaseIndexDepth;
                rMinus.PreviewMouseDown += EventUpArrowClicked;
                para.Inlines.Add(rMinus);
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

                if (sector.Nodes.Count == 0 || sector.Nodes.Count == 1 && sector.Nodes[0].NodeId == 0)
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
                    Document.Blocks.Add(para);
                }
                catch
                {
                }
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
                if (nd.NodeId == -100)
                {
                    para.Inlines.Add(new Run("("));
                    parenthesis = true;
                }
                else if (nd.NodeId == -101)
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
                    Run r = BuildNodeTextAndAddToPara(nd, includeNumber, para, sector.DisplayLevel);
                    if (r.FontWeight == FontWeights.Bold)
                    {
                        r.FontWeight = FontWeights.DemiBold;
                    }
                    if (para.FontWeight == FontWeights.Normal)
                    {
                        r.FontWeight = FontWeights.Normal;
                    }
                    r.Foreground = Brushes.Black;
                    parenthesis = false;

                    if (i == 0 && sector.FirstNodeColor != null && !IsEffectiveIndexLevel(sector.BranchLevel))
                    {
                        r.Foreground = sector.FirstNodeColor;
                    }
                    if (i == sector.Nodes.Count - 1 && sector.BranchLevel >= EffectiveIndexDepth)
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
            rIndexTitle.Foreground = ChessForgeColors.VARIATION_INDEX_FORE;
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
        /// Handles a mouse click on over a move in the index paragraph.
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
                // if sectors are collpase the Run may not be present
                if (_dictNodeToRun.ContainsKey(nodeId))
                {
                    Run target = _dictNodeToRun[nodeId];
                    // we don't want any handling of letf/right button so fake it to Middle
                    SelectRun(target, 1, MouseButton.Middle);
                    BringSelectedRunIntoView();
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
            if (r != null && e.ChangedButton == MouseButton.Left)
            {
                try
                {
                    ClearCopySelect();
                    int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);

                    if (nodeId > 0)
                    {
                        TreeNode nd = _mainVariationTree.GetNodeFromNodeId(nodeId);
                        nd.IsCollapsed = !nd.IsCollapsed;

                        //if collapsed, check if we need to update the selection
                        if (nd.IsCollapsed)
                        {
                            TreeNode selectedNode = GetSelectedNode();
                            if (selectedNode != null && selectedNode.NodeId != 0)
                            {
                                if (TreeUtils.IsAncestor(selectedNode, nd))
                                {
                                    TreeNode ancestor = TreeUtils.GetFirstExpandedAncestor(nd);
                                    if (ancestor != null)
                                    {
                                        SelectLineAndMove(ancestor.LineId, ancestor.NodeId);
                                        BringSelectedRunIntoView();
                                    }
                                }
                            }
                        }
                        BuildFlowDocumentForVariationTree();
                    }
                }
                catch { }

                e.Handled = true;
            }
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

using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using static System.Net.Mime.MediaTypeNames;

namespace ChessForge
{
    /// <summary>
    /// Manages text and events in the main Workbook view.
    /// The view is built in a RichTextBox.
    /// </summary>
    public class WorkbookView : RichTextBuilder
    {
        // Application's Main Window
        private MainWindow _mainWin;

        // tracks the last added run as we may need to change its color
        private Run _lastAddedRun;

        /// <summary>
        /// Constructor. Sets a reference to the 
        /// FlowDocument for the RichTextBox control, via
        /// a call to the base class's constructor.
        /// </summary>
        /// <param name="doc"></param>
        public WorkbookView(FlowDocument doc, MainWindow mainWin) : base(doc)
        {
            _mainWin = mainWin;
        }

        /// <summary>
        /// RichTextPara dictionary accessor
        /// </summary>
        override internal Dictionary<string, RichTextPara> RichTextParas { get { return _richTextParas; } }

        /// <summary>
        /// Most recent clicked node
        /// </summary>
        public int LastClickedNodeId { get => _lastClickedNodeId; set => _lastClickedNodeId = value; }

        /// <summary>
        /// Layout definitions for paragrahs at different levels.
        /// </summary>
        private Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            ["0"] = new RichTextPara(0, 10, 18, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(0, 0, 0)), TextAlignment.Left),
            ["1"] = new RichTextPara(40, 10, 16, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Left),
            ["2"] = new RichTextPara(70, 5, 14, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(51, 159, 141)), TextAlignment.Left),
            ["3"] = new RichTextPara(90, 5, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Left),
            ["4"] = new RichTextPara(105, 5, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(128, 98, 63)), TextAlignment.Left),
            ["5"] = new RichTextPara(110, 5, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(100, 90, 63)), TextAlignment.Left),
            ["6"] = new RichTextPara(115, 5, 11, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(90, 60, 60)), TextAlignment.Left),
            ["7"] = new RichTextPara(120, 5, 11, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(60, 60, 60)), TextAlignment.Left),
            ["8"] = new RichTextPara(125, 5, 11, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(40, 50, 60)), TextAlignment.Left),
            ["9"] = new RichTextPara(130, 5, 11, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(30, 50, 40)), TextAlignment.Left),
            ["10"] = new RichTextPara(135, 5, 11, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(20, 20, 10)), TextAlignment.Left),
            ["default"] = new RichTextPara(140, 5, 11, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(20, 0, 0)), TextAlignment.Left),
        };

        /// <summary>
        /// Most recent clicked node.
        /// This allows the context menu to reference the correct move.
        /// </summary>
        private int _lastClickedNodeId = -1;

        /// <summary>
        /// Determines whether we are inside an intra fork.
        /// The "intra fork" means that we have a "leaf only" fork and are presenting
        /// the line(s) within parenthesis rather than opening a new paragraph.
        /// </summary>
        private bool _isIntraFork = false;

        /// <summary>
        /// Font style to use when inside an intra fork.
        /// </summary>
        private FontStyle _intraForkFontStyle = FontStyles.Italic;

        /// <summary>
        /// The main Workbook tree.
        /// </summary>
        private WorkbookTree _workbook;

        /// <summary>
        /// Maps Node Ids to Runs for quick access.
        /// </summary>
        private Dictionary<int, Run> _dictNodeToRun = new Dictionary<int, Run>();

        /// <summary>
        /// Maps Runs to Paragraphs for quick access.
        /// </summary>
        private Dictionary<Run, Paragraph> _dictRunToParagraph = new Dictionary<Run, Paragraph>();

        /// <summary>
        /// Currently selected line.
        /// </summary>
        private ObservableCollection<TreeNode> _lstSelectedLine = new ObservableCollection<TreeNode>();

        /// <summary>
        /// Current Paragraph level.
        /// </summary>
        private int _currParagraphLevel;

        /// <summary>
        /// Color to use for the background of the highlighted line.
        /// </summary>
        private SolidColorBrush _brushSelectedBkg = new SolidColorBrush(Color.FromRgb(255, 255, 206));

        /// <summary>
        /// Color to use for the background of the selected move.
        /// </summary>
        private SolidColorBrush _brushSelectedMoveBkg = new SolidColorBrush(Color.FromRgb(0, 0, 0));

        /// <summary>
        /// Color to use for the foreground of the selected move.
        /// </summary>
        private SolidColorBrush _brushSelectedMoveFore = new SolidColorBrush(Color.FromRgb(255, 255, 255));

        /// <summary>
        /// Color to use for the overall background.
        /// </summary>
        private SolidColorBrush _brushRegularBkg = new SolidColorBrush(Color.FromRgb(255, 255, 255));

        /// <summary>
        /// Color to use for the overall foreground.
        /// </summary>
        private SolidColorBrush _brushRegularFore = new SolidColorBrush(Color.FromRgb(0, 0, 0));

        /// <summary>
        /// Selected (clicked) run.
        /// </summary>
        private Run _selectedRun;
        private SolidColorBrush _selectedRunFore;
        private SolidColorBrush _selectedRunBkg;

        /// <summary>
        /// Prefix that will be followed by NodeId in the name of each Run
        /// that represents a TreeNode.
        /// </summary>
        private readonly string RUN_NAME_PREFIX = "run_";

        /// <summary>
        /// Type of nodes that can be encountered in the Workbook tree.
        /// The layout in the box is determinaed by these types
        /// </summary>
        private enum NodeType
        {
            ISOLATED, // a node with exactly one child
            LEAF, // a node with no children
            FORK_WITH_LEAF_LINES_ONLY, // a fork with no further forks down any of the branches starting from it
            FORK_WITH_FORK_LINES // a fork with at least one other fork down any of the branches starting from it.
        }

        /// <summary>
        /// Promotes the line with the last clicked node
        /// one level up.
        /// </summary>
        public void PromoteCurrentLine()
        {
            TreeNode nd = _workbook.GetNodeFromNodeId(_lastClickedNodeId);
            _workbook.PromoteLine(nd);
            BuildFlowDocumentForWorkbook();
            AppStateManager.IsDirty = true;
        }

        /// <summary>
        /// Deletes the current move and all moves that follow it.
        /// </summary>
        public void DeleteRemainingMoves()
        {
            TreeNode nd = _workbook.GetNodeFromNodeId(_lastClickedNodeId);
            _workbook.DeleteRemainingMoves(nd);
            BuildFlowDocumentForWorkbook();
            AppStateManager.IsDirty = true;
        }

        /// <summary>
        /// Sets up Workbook view's context menu.
        /// </summary>
        /// <param name="cmn"></param>
        /// <param name="isEnabled"></param>
        public void EnableWorkbookMenus(ContextMenu cmn, bool isEnabled)
        {
            // ClickedIndex should be in sync with isEnabled but double check just in case
            if (LastClickedNodeId < 0)
            {
                isEnabled = false;
            }

            foreach (var item in cmn.Items)
            {
                if (item is MenuItem)
                {
                    MenuItem menuItem = item as MenuItem;
                    switch (menuItem.Name)
                    {
                        case "_mnWorkbookSelectAsBookmark":
                            menuItem.IsEnabled = isEnabled;
                            break;
                        case "_mnWorkbookBookmarkAlternatives":
                            if (_mainWin.Workbook.NodeHasSiblings(LastClickedNodeId))
                            {
                                menuItem.Visibility = Visibility.Visible;
                                menuItem.IsEnabled = isEnabled;
                            }
                            else
                            {
                                menuItem.Visibility = Visibility.Collapsed;
                                menuItem.IsEnabled = false;
                            }
                            break;
                        case "_mnWorkbookEvalMove":
                            menuItem.IsEnabled = isEnabled;
                            break;
                        case "_mnWorkbookEvalLine":
                            menuItem.IsEnabled = true;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Selects the move and the line in this view on a request from another view (as opposed
        /// to a user request).
        /// Therefore it does not request other views to follow the selection.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="lineId"></param>
        public void SelectLineAndMove(string lineId, int nodeId)
        {
            if (_selectedRun != null)
            {
                _selectedRun.Background = _selectedRunBkg;
                _selectedRun.Foreground = _selectedRunFore;
            }

            foreach (TreeNode nd in _lstSelectedLine)
            {
                if (nd.NodeId != 0)
                {
                    _dictNodeToRun[nd.NodeId].Background = _brushRegularBkg;
                }
            }

            _selectedRun = null;
            _dictNodeToRun.TryGetValue(nodeId, out _selectedRun);

            if (!string.IsNullOrEmpty(lineId))
            {
                _lstSelectedLine = _workbook.SelectLine(lineId);
                foreach (TreeNode nd in _lstSelectedLine)
                {
                    if (nd.NodeId != 0)
                    {
                        // TODO: we crash here when run is not added
                        //       keep for now to help finding bugs
                        _dictNodeToRun[nd.NodeId].Background = _brushSelectedBkg;
                    }
                }
            }

            if (_selectedRun != null)
            {
                _selectedRunBkg = (SolidColorBrush)_selectedRun.Background;
                _selectedRunFore = (SolidColorBrush)_selectedRun.Foreground;

                _selectedRun.Background = _brushSelectedMoveBkg;
                _selectedRun.Foreground = _brushSelectedMoveFore;

                _selectedRun.BringIntoView();
            }
        }

        /// <summary>
        /// Builds the FlowDocument from the entire Workbook tree for the RichTextBox to display.
        /// </summary>
        public void BuildFlowDocumentForWorkbook(int rootNodeId = 0, bool includeStem = true)
        {
            Document.Blocks.Clear();
            _workbook = _mainWin.Workbook;

            // resets
            _dictNodeToRun.Clear();
            _dictRunToParagraph.Clear();
            _lstSelectedLine.Clear();
            _currParagraphLevel = 0;

            // we will traverse back from each leaf to the nearest parent fork (or root of we run out)
            // and note the distances in the Nodes so that we can use them when creating the document
            // in the forward traversing
            SetNodeDistances();

            TreeNode root;
            if (rootNodeId == 0)
            {
                root = _workbook.Nodes[0];
            }
            else
            {
                root = _workbook.GetNodeFromNodeId(rootNodeId);
                if (includeStem)
                {
                    Paragraph paraStem = BuildWorkbookStemLine(root);
                    Document.Blocks.Add(paraStem);
                }
            }

            // start be creating a level 1 paragraph.
            Paragraph para = CreateParagraph("0");
            Document.Blocks.Add(para);

            // if we have a stem (e.g. this is Browse view in training, we need to request a number printed too
            BuildTreeLineText(root, para, includeStem);

            RemoveEmptyParagraphs();
        }

        /// <summary>
        /// Traverses the tree back from each leaf populating the DistanceToFork
        /// and DistanceToLeaf fields
        /// </summary>
        private void SetNodeDistances()
        {
            foreach (TreeNode nd in _workbook.Nodes)
            {
                // if the node is a leaf start traversing
                if (IsLeaf(nd))
                {
                    int distanceFromLeaf = 0;
                    int distanceFromFork = -1;

                    nd.DistanceToLeaf = 0;
                    nd.DistanceToNextFork = -1;

                    TreeNode currNode = nd;
                    while (currNode.Parent != null)
                    {
                        if (distanceFromLeaf >= 0)
                        {
                            distanceFromLeaf++;
                        }

                        if (distanceFromFork >= 0)
                        {
                            distanceFromFork++;
                        }

                        currNode = currNode.Parent;

                        if (IsFork(currNode))
                        {
                            // we may have reached a fork that has been reached before
                            // in which case let's take the greater value
                            currNode.DistanceToLeaf = Math.Min(distanceFromLeaf, currNode.DistanceToLeaf);
                            currNode.DistanceToNextFork = Math.Min(distanceFromFork, currNode.DistanceToNextFork);

                            // reset the "from fork" counters
                            // disable the from Leaf counter.
                            distanceFromFork = 0;
                            distanceFromLeaf = -1;
                        }
                        else
                        {
                            // if we are past the first fork and the distance
                            // values have been set, no point continuing
                            if (distanceFromFork >= 0 && currNode.DistanceToNextFork != 0)
                            {
                                break;
                            }

                            currNode.DistanceToLeaf = distanceFromLeaf;
                            currNode.DistanceToNextFork = distanceFromFork;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the node has more than one child
        /// and is therefore a fork (a.k.a. branch or inode)
        /// </summary>
        /// <param name="nd">Node to check.</param>
        /// <returns></returns>
        private bool IsFork(TreeNode nd)
        {
            if (nd == null)
                return false;

            return nd.Children.Count > 1;
        }

        /// <summary>
        /// Checks if the node has no children and
        /// is therefore a leaf.
        /// </summary>
        /// <param name="nd">Node to check</param>
        /// <returns></returns>
        private bool IsLeaf(TreeNode nd)
        {
            return nd.Children.Count == 0;
        }

        /// <summary>
        /// Returns NodeType of a node.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private NodeType GetNodeType(TreeNode nd)
        {
            if (IsLeaf(nd))
            {
                return NodeType.LEAF;
            }

            if (IsFork(nd))
            {
                // check if all lines except the top one ("[0]") lead directly
                // (i.e. no forks along the way) to Leaf nodes.
                for (int i = 1; i < nd.Children.Count; i++)
                {
                    if (nd.Children[i].DistanceToLeaf == -1)
                    {
                        return NodeType.FORK_WITH_FORK_LINES;
                    }
                }
                return NodeType.FORK_WITH_LEAF_LINES_ONLY;
            }

            return NodeType.ISOLATED;
        }

        /// <summary>
        /// Finds the parent node of the passed node
        /// and insert this node after it.
        /// The caller needs to ensure that this is logically correct
        /// e.g. that this is a new leaf in a line
        /// </summary>
        /// <param name="nd"></param>
        public void AddNewNode(TreeNode nd)
        {
            TreeNode parent = nd.Parent;
            Run rParent = _dictNodeToRun[parent.NodeId];
            Paragraph para = _dictRunToParagraph[rParent];

            Run r = new Run(" " + MoveUtils.BuildSingleMoveText(nd, false));
            r.Name = "run_" + nd.NodeId.ToString();
            r.MouseDown += EventRunClicked;

            r.FontStyle = rParent.FontStyle;
            r.FontSize = rParent.FontSize;
            r.Foreground = Brushes.Black;
            r.FontWeight = FontWeights.Normal;

            _dictNodeToRun[nd.NodeId] = r;
            _dictRunToParagraph[r] = para;

            para.Inlines.InsertAfter(rParent, r);
            _lastAddedRun = r;
        }

        /// <summary>
        /// Each invoked instance builds text of a single Line in the Workbook. 
        /// Calls itself recursively and returns the text of the complete
        /// Workbook.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeNumber"></param>
        private void BuildTreeLineText(TreeNode nd, Paragraph para, bool includeNumber)
        {
            while (true)
            {
                // if the node has no children,
                if (nd.Children.Count == 0)
                {
                    return;
                }

                // if the node has 1 child, print it,
                // keep the same level and sublevel as the parent
                // call this method on the child
                if (GetNodeType(nd) == NodeType.ISOLATED || GetNodeType(nd) == NodeType.LEAF)
                {
                    TreeNode child = nd.Children[0];
                    BuildNodeText(child, includeNumber, para);
                    BuildTreeLineText(child, para, false);
                    return;
                }

                // if the node has more than 1 child
                // call this method on each sibling except
                // the first one, before calling it on the 
                // first one.
                if (nd.Children.Count > 1)
                {
                    NodeType nodeType = GetNodeType(nd);

                    //bool multi = nd.Children.Count > 2;
                    bool multi = (nodeType == NodeType.FORK_WITH_FORK_LINES) || nd.Children.Count > 2;

                    // the first child remains at the same level as the parent
                    BuildNodeText(nd.Children[0], includeNumber, para);

                    bool specialTopLineCase = false;

                    for (int i = 1; i < nd.Children.Count; i++)
                    {
                        // if there is more than 2 children, create a new para, otherwise just use parenthesis.
                        // The exception is when we are at the top level (_currParagraphLevel == 0) 
                        // when we don't want any "intra forks" and always create a new, lower level
                        // paragraph even if there are just 2 children.
                        Paragraph para2;
                        if (multi || _currParagraphLevel == 0)
                        {
                            if (!multi)
                            {
                                specialTopLineCase = true;
                            }
                            if (i == 1)
                            {
                                _currParagraphLevel++;
                                ColorLastRun();
                                para2 = CreateParagraph(_currParagraphLevel.ToString());
                            }
                            else
                            {
                                para2 = CreateParagraph(_currParagraphLevel.ToString());
                                para2.Margin = new Thickness(para.Margin.Left, 0, 0, 5);
                            }
                            Document.Blocks.Add(para2);
                        }
                        else
                        {
                            para2 = para;
                            if (i == 1)
                            {
                                para2.Inlines.Add(new Run(" ( "));
                                _isIntraFork = true;
                            }
                        }

                        BuildNodeText(nd.Children[i], true, para2);
                        BuildTreeLineText(nd.Children[i], para2, false);

                        if (multi && i == nd.Children.Count - 1)
                        {
                            _currParagraphLevel--;
                            para = CreateParagraph(_currParagraphLevel.ToString());
                            Document.Blocks.Add(para);
                        }
                        else
                        {
                            para = para2;
                            if (i == nd.Children.Count - 1)
                            {
                                if (_isIntraFork)
                                {
                                    para.Inlines.Add(new Run(" ) "));
                                }
                                _isIntraFork = false;
                            }
                            else
                            {
                                if (!multi)
                                {
                                    para.Inlines.Add(new Run("; "));
                                }
                            }
                        }
                    }

                    if (specialTopLineCase)
                    {
                        _currParagraphLevel--;
                        Paragraph spec = CreateParagraph(_currParagraphLevel.ToString());
                        Document.Blocks.Add(spec);
                        BuildTreeLineText(nd.Children[0], spec, true);
                    }
                    else
                    {
                        BuildTreeLineText(nd.Children[0], para, true);
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Builds text of an individual node (ply).
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeNumber"></param>
        private void BuildNodeText(TreeNode nd, bool includeNumber, Paragraph para)
        {
            StringBuilder sb = new StringBuilder();
            if (nd.Position.ColorToMove == PieceColor.Black)
            {
                if (!includeNumber && nd.Position.MoveNumber != 1)
                {
                    sb.Append(" ");
                }
                sb.Append(nd.Position.MoveNumber.ToString() + ".");
            }

            if (nd.Position.ColorToMove == PieceColor.White && includeNumber)
            {
                sb.Append(nd.Position.MoveNumber.ToString() + "...");
            }

            if (nd.Position.ColorToMove == PieceColor.White)
            {
                sb.Append(" ");
            }

            sb.Append(nd.LastMoveAlgebraicNotationWithNag);

            SolidColorBrush fontColor = null;
            if (IsFork(nd.Parent) && !nd.IsMainLine())
            {
                if (!nd.IsFirstChild())
                {
                    fontColor = GetParaAttrs(_currParagraphLevel.ToString()).FirstCharColor;
                }
            }

            AddRunToParagraph(nd, para, sb.ToString(), fontColor);
        }

        /// <summary>
        /// Creates a run, formats it and adds to the passed paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="para"></param>
        /// <param name="text"></param>
        /// <param name="fontColor"></param>
        private void AddRunToParagraph(TreeNode nd, Paragraph para, string text, SolidColorBrush fontColor)
        {
            Run r = new Run(text.ToString());
            r.Name = "run_" + nd.NodeId.ToString();
            r.MouseDown += EventRunClicked;

            if (_isIntraFork)
            {
                r.FontStyle = _intraForkFontStyle;
                r.FontSize = GetParaAttrs((_currParagraphLevel + 1).ToString()).FontSize;
            }

            if (fontColor != null && para.Inlines.Count == 0)
            {
                r.Foreground = fontColor;
                r.FontWeight = FontWeights.Bold;
            }

            if (para.Margin.Left == 0 && nd.IsMainLine())
                para.Inlines.Add(new Bold(r));
            else
                para.Inlines.Add(r);

            _dictNodeToRun.Add(nd.NodeId, r);
            _dictRunToParagraph.Add(r, para);

            _lastAddedRun = r;
        }

        /// <summary>
        /// Event handler invoked when a Run was clicked.
        /// In response, we highlight the line to which this Run belongs
        /// (selecting the top branch for the part of the line beyond
        /// the clicked Run),
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventRunClicked(object sender, MouseButtonEventArgs e)
        {
            if (_selectedRun != null)
            {
                _selectedRun.Background = _selectedRunBkg;
                _selectedRun.Foreground = _selectedRunFore;
            }

            foreach (TreeNode nd in _lstSelectedLine)
            {
                if (nd.NodeId != 0)
                {
                    Run run;
                    // if we are dealing with a subtree, we may not have 
                    // all nodes from the line.
                    if (_dictNodeToRun.TryGetValue(nd.NodeId, out run))
                    {
                        _dictNodeToRun[nd.NodeId].Background = _brushRegularBkg;
                    }
                }
            }

            Run r = (Run)e.Source;
            _selectedRun = r;

            string lineId = "";
            int nodeId = -1;
            if (r.Name != null && r.Name.StartsWith(RUN_NAME_PREFIX))
            {
                nodeId = int.Parse(r.Name.Substring(RUN_NAME_PREFIX.Length));
                TreeNode foundNode = _workbook.GetNodeFromNodeId(nodeId);
                lineId = foundNode.LineId;
                lineId = _workbook.GetDefaultLineIdForNode(nodeId);
                _lstSelectedLine = _workbook.SelectLine(lineId);
                foreach (TreeNode nd in _lstSelectedLine)
                {
                    if (nd.NodeId != 0)
                    {
                        Run run;
                        if (_dictNodeToRun.TryGetValue(nd.NodeId, out run))
                        {
                            _dictNodeToRun[nd.NodeId].Background = _brushSelectedBkg;
                        }
                    }
                }

                _mainWin.SetActiveLine(_lstSelectedLine, nodeId);
                LearningMode.ActiveLineId = lineId;
            }

            _selectedRunBkg = (SolidColorBrush)r.Background;
            _selectedRunFore = (SolidColorBrush)r.Foreground;

            r.Background = _brushSelectedMoveBkg;
            r.Foreground = _brushSelectedMoveFore;

            _mainWin._lvWorkbookTable_SelectLineAndMove(lineId, nodeId);

            // this is a right click offer the context menu
            if (e.ChangedButton == MouseButton.Right)
            {
                _lastClickedNodeId = nodeId;
                EnableWorkbookMenus(_mainWin.UiCmnWorkbookRightClick, true);
            }
            else
            {
                _lastClickedNodeId = -1;
            }
        }

        /// <summary>
        /// Colors the last run in the paragraph witht the color of the next (lower level)
        /// paragraph's first char.
        /// The idea is to provide a more obvious visual hint as to where the fork is.
        /// </summary>
        private void ColorLastRun()
        {
            if (_lastAddedRun != null)
            {
                string style = _currParagraphLevel.ToString();
                RichTextPara attrs = GetParaAttrs(style);
                _lastAddedRun.Foreground = attrs.FirstCharColor;
                _lastAddedRun.FontWeight = FontWeights.Bold;
            }
        }
    }
}

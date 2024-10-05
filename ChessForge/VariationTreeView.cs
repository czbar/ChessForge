using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static ChessForge.CommentBox;

namespace ChessForge
{
    /// <summary>
    /// Manages text and events in the main Workbook view.
    /// The view is built in a RichTextBox.
    /// </summary>
    public partial class VariationTreeView : RichTextBuilder
    {
        /// <summary>
        /// Indicates whether the view is "fresh"
        /// or if it requires a rebuild.
        /// The Fresh flag only applies if the view is invoked for the
        /// same enitity, same index and same chapter. 
        /// </summary>
        public bool IsFresh
        {
            get => _isFresh;
            set => _isFresh = value;
        }

        /// <summary>
        /// Content Type in this view
        /// </summary>
        public GameData.ContentType ContentType
        {
            get => _contentType;
        }

        /// <summary>
        /// Returns reference to the main variation tree
        /// </summary>
        public VariationTree MainVariationTree
        {
            get => _mainVariationTree;
        }

        /// <summary>
        /// Whether to show blunder assessments.
        /// </summary>
        public bool HandleBlunders
        {
            get => ContentType == GameData.ContentType.MODEL_GAME;
        }

        /// <summary>
        /// Checks whether there are any moves selected for copying
        /// </summary>
        public bool HasMovesSelectedForCopy
        {
            get
            {
                if (_selectedForCopy.Count > 0)
                {
                    return true;
                }
                else
                {
                    TreeNode nd = GetSelectedNode();
                    if (nd != null && nd.NodeId != 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        // Page Header paragraph
        protected Paragraph _pageHeaderParagraph = null;

        // flags freshness of the view
        private bool _isFresh = false;

        // content type in this view
        private GameData.ContentType _contentType;

        // the node for whcih a thumbnail was last created.
        private TreeNode _lastThumbnailNode = null;

        // The small chessboard shown in Exercises.
        protected ChessBoardSmall _exercisePassiveChessBoard;

        /// <summary>
        /// For unknown reason the first right click in the app (some views anyway) does not
        /// bring up the context menu unless we force it with IsOpen
        /// </summary>
        private static bool _contextMenuPrimed = false;

        // Application's Main Window
        protected MainWindow _mainWin;

        // tracks the last added run as we may need to change its color
        private Run _lastAddedRun;

        // currently showing fork table
        private Table _forkTable;

        // the list of nodes currently selected for copying into clipboard
        private List<TreeNode> _selectedForCopy = new List<TreeNode>();

        // whether the view is built for display or print/export
        protected bool _isPrinting = false;

        // the RichTextBox control underlying this view.
        public RichTextBox RichTextBoxControl;

        /// <summary>
        /// Constructor. Sets a reference to the 
        /// FlowDocument for the RichTextBox control, via
        /// a call to the base class's constructor.
        /// </summary>
        /// <param name="doc"></param>
        public VariationTreeView(RichTextBox rtb, GameData.ContentType contentType) : base(rtb.Document)
        {
            _mainWin = AppState.MainWin;
            _contentType = contentType;
            RichTextBoxControl = rtb;
        }

        /// <summary>
        /// Contructors assuming the null value for the RichTextBox.
        /// It is used when building "invisible" views for printing.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="entityIndex"></param>
        public VariationTreeView(FlowDocument doc, GameData.ContentType contentType) : base(doc)
        {
            _mainWin = AppState.MainWin;
            _contentType = contentType;
            RichTextBoxControl = null;

            // this constructors is only called when printing (RTF exporting)
            _isPrinting = true;
        }

        /// <summary>
        /// Returns true if the view has any entitites of the appropriate type to show.
        /// </summary>
        public bool HasEntities
        {
            get
            {
                switch (ContentType)
                {
                    case GameData.ContentType.STUDY_TREE:
                        return true;
                    case GameData.ContentType.MODEL_GAME:
                        return AppState.ActiveChapter != null && AppState.ActiveChapter.HasAnyModelGame;
                    case GameData.ContentType.EXERCISE:
                        return AppState.ActiveChapter != null && AppState.ActiveChapter.HasAnyExercise;
                    default:
                        return false;
                }
            }
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
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        private Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            ["0"] = new RichTextPara(0, 10, 16, FontWeights.Bold, TextAlignment.Left),
            ["1"] = new RichTextPara(40, 10, 16, FontWeights.Normal, TextAlignment.Left),
            ["2"] = new RichTextPara(70, 5, 14, FontWeights.Normal, TextAlignment.Left),
            ["3"] = new RichTextPara(90, 5, 12, FontWeights.Normal, TextAlignment.Left),
            ["4"] = new RichTextPara(105, 5, 12, FontWeights.Normal, TextAlignment.Left),
            ["5"] = new RichTextPara(110, 5, 12, FontWeights.Normal, TextAlignment.Left),
            ["6"] = new RichTextPara(115, 5, 11, FontWeights.Normal, TextAlignment.Left),
            ["7"] = new RichTextPara(120, 5, 11, FontWeights.Normal, TextAlignment.Left),
            ["8"] = new RichTextPara(125, 5, 11, FontWeights.Normal, TextAlignment.Left),
            ["9"] = new RichTextPara(130, 5, 11, FontWeights.Normal, TextAlignment.Left),
            ["10"] = new RichTextPara(135, 5, 11, FontWeights.Normal, TextAlignment.Left),
            ["default"] = new RichTextPara(140, 5, 11, FontWeights.Normal, TextAlignment.Left),
            ["preamble"] = new RichTextPara(40, 10, 16, FontWeights.Normal, TextAlignment.Left),
        };

        // prefixes for run names
        private readonly string _run_fork_move_ = "_run_fork_move_";
        private readonly string _run_ = "run_";

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
        /// The main variation tree in this view.
        /// </summary>
        protected VariationTree _mainVariationTree;

        /// <summary>
        /// The shown variation tree is either the _mainVariationTree or the AssociatedSecondary tree
        /// </summary>
        public VariationTree ShownVariationTree
        {
            get => _mainVariationTree.IsAssociatedTreeActive ? _mainVariationTree.AssociatedSecondary : _mainVariationTree;
        }

        /// <summary>
        /// Maps Node Ids to Runs for quick access.
        /// </summary>
        protected Dictionary<int, Run> _dictNodeToRun = new Dictionary<int, Run>();

        /// <summary>
        /// Maps Node Ids to Comment Runs for quick access.
        /// </summary>
        private Dictionary<int, Inline> _dictNodeToCommentRun = new Dictionary<int, Inline>();

        /// <summary>
        /// Maps Node Ids to CommentBeforeMove Runs for quick access.
        /// </summary>
        private Dictionary<int, Inline> _dictNodeToCommentBeforeMoveRun = new Dictionary<int, Inline>();

        /// <summary>
        /// Maps Runs to Paragraphs for quick access.
        /// </summary>
        private Dictionary<Run, Paragraph> _dictRunToParagraph = new Dictionary<Run, Paragraph>();

        /// <summary>
        /// Maps Comment Runs to Paragraphs for quick access.
        /// </summary>
        private Dictionary<Inline, Paragraph> _dictCommentRunToParagraph = new Dictionary<Inline, Paragraph>();

        /// <summary>
        /// Maps CommentBeforeMove Runs to Paragraphs for quick access.
        /// </summary>
        private Dictionary<Inline, Paragraph> _dictCommentBeforeMoveRunToParagraph = new Dictionary<Inline, Paragraph>();

        /// <summary>
        /// Current Paragraph level.
        /// </summary>
        private int _currParagraphLevel;

        /// <summary>
        /// Selected (clicked) run.
        /// </summary>
        private Run _selectedRun;
        private SolidColorBrush _selectedRunFore;
        private SolidColorBrush _selectedRunBkg;

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
        /// Builds the FlowDocument from the entire Variation Tree for the RichTextBox to display.
        /// Inserts dummy (no text) run for the starting position (NodeId == 0)
        /// </summary>
        public void BuildFlowDocumentForVariationTree(VariationTree treeForPrint = null, int rootNodeId = 0, bool includeStem = true)
        {
            try
            {
                GameData.ContentType contentType = GameData.ContentType.NONE;

                if (treeForPrint == null)
                {
                    if (_contentType == GameData.ContentType.STUDY_TREE)
                    {
                        _mainVariationTree = WorkbookManager.SessionWorkbook.ActiveChapter.StudyTree.Tree;
                    }
                    else
                    {
                        _mainVariationTree = _mainWin.ActiveVariationTree;
                        if (_mainVariationTree != null && _mainVariationTree.AssociatedPrimary != null)
                        {
                            // ActiveVariationTree may return a secondary tree which we don't want so check for it
                            _mainVariationTree = _mainVariationTree.AssociatedPrimary;
                        }
                    }

                    if (_mainVariationTree == null || _mainVariationTree.ContentType != this.ContentType)
                    {
                        return;
                    }
                }
                else
                {
                    _mainVariationTree = treeForPrint;
                }

                if (string.IsNullOrEmpty(_mainVariationTree.RootNode.LineId) || _mainVariationTree.Nodes.Count > 1 && string.IsNullOrEmpty(_mainVariationTree.Nodes[1].LineId))
                {
                    _mainVariationTree.BuildLines();
                }

                contentType = _mainVariationTree.Header.GetContentType(out _);

                Clear(GameData.ContentType.GENERIC);

                if (treeForPrint == null)
                {
                    PreviousNextViewBars.BuildPreviousNextBar(contentType);
                }

                Document.Blocks.Add(BuildDummyPararaph());

                // do not print page header if this is RTF export (print) and the view is Study
                // NOTE: first, it is redundant; second, it will print the title of the active chapter
                if (treeForPrint == null || contentType != GameData.ContentType.STUDY_TREE)
                {
                    _pageHeaderParagraph = BuildPageHeader(_mainVariationTree, contentType);
                    if (_pageHeaderParagraph != null)
                    {
                        Document.Blocks.Add(_pageHeaderParagraph);
                    }
                }

                BuildExerciseParagraphs();

                Paragraph preamblePara = BuildPreamble();
                if (preamblePara != null)
                {
                    Document.Blocks.Add(preamblePara);
                }

                if (treeForPrint == null)
                {
                    Paragraph quizInfoPara = BuildQuizInfoPara();
                    if (quizInfoPara != null)
                    {
                        Document.Blocks.Add(quizInfoPara);
                    }

                    Paragraph movePromptPara = BuildYourMovePrompt();
                    if (movePromptPara != null)
                    {
                        Document.Blocks.Add(movePromptPara);
                    }
                }

                if (contentType != GameData.ContentType.EXERCISE || ShownVariationTree.ShowTreeLines || treeForPrint != null)
                {
                    // we will traverse back from each leaf to the nearest parent fork (or root of we run out)
                    // and note the distances in the Nodes so that we can use them when creating the document
                    // in the forward traversing
                    SetNodeDistances();

                    TreeNode root;
                    if (rootNodeId == 0)
                    {
                        root = ShownVariationTree.Nodes[0];
                    }
                    else
                    {
                        root = ShownVariationTree.GetNodeFromNodeId(rootNodeId);
                        if (includeStem)
                        {
                            Paragraph paraStem = BuildWorkbookStemLine(root, true);
                            Document.Blocks.Add(paraStem);
                        }
                    }

                    // start by creating a level 1 paragraph.
                    Paragraph para = CreateParagraph("0", true);
                    Document.Blocks.Add(para);

                    CreateRunForStartingNode(para, root);

                    // if we have a stem (e.g. this is Browse view in training, we need to request a number printed too
                    BuildTreeLineText(root, para, includeStem);

                    if (contentType == GameData.ContentType.MODEL_GAME || contentType == GameData.ContentType.EXERCISE)
                    {
                        Paragraph resultPara = BuildResultPara();
                        if (resultPara != null)
                        {
                            Document.Blocks.Add(resultPara);
                        }
                    }
                }

                Paragraph guessFinished = BuildGuessingFinishedParagraph();
                {
                    if (guessFinished != null)
                    {
                        Document.Blocks.Add(guessFinished);
                    }
                }

                // add dummy para so that the last row can be comfortable viewed
                Document.Blocks.Add(BuildDummyPararaph());
            }
            catch (Exception ex)
            {
                AppLog.Message("BuildFlowDocumentForVariationTree()", ex);
            }
        }

        /// <summary>
        /// Update the chapter title that is displayed above the Study Tree.
        /// </summary>
        public Paragraph UpdateChapterTitle()
        {
            Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
            if (chapter != null)
            {
                if (_pageHeaderParagraph == null)
                {
                    _pageHeaderParagraph = CreateParagraph("0", true);
                    _pageHeaderParagraph.MouseLeftButtonDown += EventPageHeaderClicked;
                }

                _pageHeaderParagraph.Inlines.Clear();

                Run rTitle = new Run(chapter.GetTitle());
                rTitle.TextDecorations = TextDecorations.Underline;
                _pageHeaderParagraph.Inlines.Add(rTitle);

                if (!string.IsNullOrWhiteSpace(chapter.GetAuthor()))
                {
                    Run rAuthor = new Run("\n    " + Properties.Resources.Author + ": " + chapter.GetAuthor());
                    rAuthor.FontWeight = FontWeights.Normal;
                    rAuthor.FontSize = GuiUtilities.AdjustFontSize(Constants.BASE_FIXED_FONT_SIZE) - 2;
                    _pageHeaderParagraph.Inlines.Add(rAuthor);
                }
            }

            return _pageHeaderParagraph;
        }


        /// <summary>
        /// A dummy method to be overridden in the Exercise view.
        /// </summary>
        /// <returns></returns>
        virtual public Paragraph BuildGuessingFinishedParagraph()
        {
            return null;
        }

        /// <summary>
        /// If this is an Exercise view in the solving mode and there are no moves yet,
        /// prompt the user to start entering them.
        /// </summary>
        /// <returns></returns>
        virtual public Paragraph BuildYourMovePrompt()
        {
            return null;
        }

        /// <summary>
        /// If this is an Exercise view in the ANALYSIS solving mode 
        /// and there are quiz points to be awarded,
        /// advise the user.
        /// </summary>
        /// <returns></returns>
        virtual public Paragraph BuildQuizInfoPara()
        {
            return null;
        }

        /// <summary>
        /// Returns the currently selected Node.
        /// </summary>
        /// <returns></returns>
        public TreeNode GetSelectedNode()
        {
            TreeNode node = null;
            try
            {
                if (_selectedRun != null)
                {
                    int nodeId = TextUtils.GetIdFromPrefixedString(_selectedRun.Name);
                    node = ShownVariationTree.GetNodeFromNodeId(nodeId);
                }
            }
            catch (Exception ex)
            {
                node = null;
                AppLog.Message("GetSelectedNode()", ex);
            }

            return node;
        }

        /// <summary>
        /// Promotes the line with the last clicked node one level up.
        /// </summary>
        public void PromoteCurrentLine()
        {
            try
            {
                TreeNode nd = ShownVariationTree.GetNodeFromNodeId(_lastClickedNodeId);
                // TODO: it would be more precise to get the last move of the line being promoted and set it as line id
                // otherwise we end up selecting a different line that the one we are promoting.
                // However, with the current GUI logic, the selected line changes when the user right-clicks on the
                // move to promote the line, so the end result wouldn't change. But it may if we change that other logic.
                ShownVariationTree.PromoteLine(nd);
                _mainWin.SetActiveLine(nd.LineId, nd.NodeId);
                BuildFlowDocumentForVariationTree();
                _mainWin.SelectLineAndMoveInWorkbookViews(_mainWin.ActiveTreeView, nd.LineId, _mainWin.ActiveLine.GetSelectedPlyNodeIndex(false), false);
                AppState.IsDirty = true;
            }
            catch (Exception ex)
            {
                AppLog.Message("PromoteCurrentLine()", ex);
            }
        }

        /// <summary>
        /// Copies FEN of the selected position to the clipboard.
        /// </summary>
        public void CopyFenToClipboard()
        {
            try
            {
                TreeNode nd = null;

                // special case if we are solving in guess mode, as the "selected node" is not what we want then
                if (AppState.CurrentSolvingMode == VariationTree.SolvingMode.GUESS_MOVE)
                {
                    nd = ShownVariationTree.Nodes[ShownVariationTree.Nodes.Count - 1];
                }
                else
                {
                    nd = GetSelectedNode();
                }

                if (nd == null && _mainVariationTree != null)
                {
                    nd = _mainVariationTree.Nodes[0];
                }

                if (nd != null)
                {
                    SystemClipboard.SetText(FenParser.GenerateFenFromPosition(nd.Position, ShownVariationTree.MoveNumberOffset));
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Marks the current node as the thumbnail and clears the previous selection.
        /// If this is the previously selected thumbnail, it clears it.
        /// </summary>
        public void MarkSelectedNodeAsThumbnail(bool defaultToRootNode = false)
        {
            try
            {
                TreeNode nd = GetSelectedNode();
                if (nd != null || defaultToRootNode)
                {
                    TreeNode prevThumbnail = _mainVariationTree.GetThumbnail();

                    if (prevThumbnail == nd)
                    {
                        _mainVariationTree.ClearThumbnail(nd);
                    }
                    else
                    {
                        _mainVariationTree.SetThumbnail(nd ?? _mainVariationTree.RootNode);
                        InsertOrUpdateCommentRun(prevThumbnail);
                    }

                    InsertOrUpdateCommentRun(nd);

                    EditOperation.EditType typ = EditOperation.EditType.MARK_THUMBNAIL;
                    EditOperation op = new EditOperation(typ, nd, prevThumbnail);
                    AppState.ActiveVariationTree?.OpsManager.PushOperation(op);

                    AppState.IsDirty = true;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Toggles the diagram flag on the currently selected node.
        /// </summary>
        public void ToggleDiagramFlag()
        {
            try
            {
                TreeNode nd = GetSelectedNode();
                if (nd != null)
                {
                    nd.IsDiagram = !nd.IsDiagram;

                    EditOperation.EditType typ = nd.IsDiagram ? EditOperation.EditType.INSERT_DIAGRAM : EditOperation.EditType.DELETE_DIAGRAM;
                    EditOperation op = new EditOperation(typ, nd);
                    AppState.ActiveVariationTree?.OpsManager.PushOperation(op);
                    AppState.IsDirty = true;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// The thumbnail Node has changed. Remove any thumbnail icon
        /// that may be set and place it on the current thumbnail.
        /// </summary>
        public void UpdateThumbnail()
        {
            if (_lastThumbnailNode != null)
            {
                _mainVariationTree.ClearThumbnail(_lastThumbnailNode);
                InsertOrUpdateCommentRun(_lastThumbnailNode);
            }

            _lastThumbnailNode = _mainVariationTree.GetThumbnail();
            InsertOrUpdateCommentRun(_lastThumbnailNode);
        }

        /// <summary>
        /// Makes a copy of the currently selected line
        /// and creates a new chapter for it.
        /// </summary>
        public void CreateChapterFromLine()
        {
            try
            {
                GameData.ContentType contentType = _mainVariationTree.ContentType;
                if (_mainVariationTree.ContentType == GameData.ContentType.STUDY_TREE || _mainVariationTree.ContentType == GameData.ContentType.MODEL_GAME)
                {
                    TreeNode nd = _mainVariationTree.GetNodeFromNodeId(_lastClickedNodeId);
                    List<TreeNode> lstNodes = _mainVariationTree.BuildSubTreeNodeList(nd, true);
                    if (lstNodes.Count > 0)
                    {
                        VariationTree newTree = TreeUtils.CreateNewTreeFromNode(lstNodes[0], GameData.ContentType.STUDY_TREE);
                        Chapter chapter = WorkbookManager.SessionWorkbook.CreateNewChapter(newTree, false);
                        chapter.SetTitle(Properties.Resources.Chapter + " " + (chapter.Index + 1).ToString() + ": " + MoveUtils.BuildSingleMoveText(nd, true, true, newTree.MoveNumberOffset));

                        ChapterFromLineDialog dlg = new ChapterFromLineDialog(chapter);
                        //{
                        //    Left = _mainWin.ChessForgeMain.Left + 100,
                        //    Top = _mainWin.ChessForgeMain.Top + 100,
                        //    Topmost = false,
                        //    Owner = _mainWin
                        //};
                        GuiUtilities.PositionDialog(dlg, _mainWin, 100);
                        dlg.ShowDialog();
                        if (dlg.ExitOK)
                        {
                            bool viewRebuilt = false;
                            chapter.SetTitle(dlg.ChapterTitle);
                            if (dlg.DeleteOriginal)
                            {
                                DeleteRemainingMoves();
                                viewRebuilt = true;
                            }
                            _mainWin.RebuildChaptersView();
                            if (dlg.GoToNewChapter)
                            {
                                _mainWin.SelectChapterByIndex(chapter.Index, true);
                                viewRebuilt = true;
                            }
                            if (!viewRebuilt)
                            {
                                BuildFlowDocumentForVariationTree();
                            }

                            AppState.IsDirty = true;
                        }
                        else
                        {
                            WorkbookManager.SessionWorkbook.Chapters.Remove(chapter);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("CreateChapterFromLine()", ex);
            }
        }

        /// <summary>
        /// Makes a copy of a subtree of the shown tree, starting at the passed node.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public List<TreeNode> CopySelectedSubtree(TreeNode nd)
        {
            return ShownVariationTree.CopySubtree(nd);
        }

        /// <summary>
        /// Deletes the current move and all moves that follow it.
        /// </summary>
        public void DeleteRemainingMoves()
        {
            try
            {
                GameData.ContentType contentType = ShownVariationTree.ContentType;

                ClearCopySelect();

                TreeNode nd = GetSelectedNode(); // _shownVariationTree.GetNodeFromNodeId(_lastClickedNodeId);
                TreeNode parent = nd.Parent;
                ShownVariationTree.DeleteRemainingMoves(nd);
                ShownVariationTree.BuildLines();
                _mainWin.SetActiveLine(parent.LineId, parent.NodeId);
                BuildFlowDocumentForVariationTree();
                _mainWin.SelectLineAndMoveInWorkbookViews(_mainWin.ActiveTreeView, parent.LineId, _mainWin.ActiveLine.GetSelectedPlyNodeIndex(true), false);
                AppState.IsDirty = true;

                BookmarkManager.ResyncBookmarks(1);
            }
            catch (Exception ex)
            {
                AppLog.Message("DeleteRemainingMoves()", ex);
            }
        }

        /// <summary>
        /// Inserts the passed subtree at the currently selected node, or the node before it (parent)
        /// depending on the match/mismatch of side-to-move.
        /// </summary>
        /// <param name="nodesToInsert"></param>
        public TreeNode InsertSubtree(List<TreeNode> nodesToInsert, ref List<TreeNode> insertedNodes, ref List<TreeNode> failedInsertions)
        {
            if (nodesToInsert == null || nodesToInsert.Count == 0)
            {
                return null;
            }

            TreeNode node = null;

            // if the first node of nodes to insert has id = 0, we will insert it at the root of the tree, regardless of which node is currently selected
            TreeNode nodeToInsertAt;
            if (nodesToInsert[0].NodeId == 0 || ShownVariationTree.Nodes.Count == 1)
            {
                nodeToInsertAt = ShownVariationTree.RootNode;
            }
            else
            {
                nodeToInsertAt = GetSelectedNode();
                if (nodeToInsertAt == null || nodeToInsertAt.NodeId == 0)
                {
                    node = null;
                    MessageBox.Show(Properties.Resources.MsgSelectNodeToInserAt, Properties.Resources.MbtTitleCopyPasteError, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    nodeToInsertAt = null;
                }
            }

            if (nodeToInsertAt != null)
            {
                node = TreeUtils.InsertSubtreeMovesIntoTree(ShownVariationTree, nodeToInsertAt, nodesToInsert, ref insertedNodes, ref failedInsertions);

                if (insertedNodes.Count > 0 && failedInsertions.Count == 0)
                {
                    // Prepare info for potential Undo
                    List<int> nodeIds = new List<int>();
                    foreach (TreeNode nd in insertedNodes)
                    {
                        nodeIds.Add(nd.NodeId);
                    }
                    EditOperation op = new EditOperation(EditOperation.EditType.PASTE_MOVES, nodeToInsertAt.NodeId, nodeIds, null);
                    ShownVariationTree.OpsManager.PushOperation(op);
                }
            }

            return node;
        }

        /// <summary>
        /// Sets up ActiveTreeView's context menu.
        /// </summary>
        /// <param name="cmn"></param>
        /// <param name="isEnabled"></param>
        public void EnableActiveTreeViewMenus(MouseButton button, bool isEnabled)
        {
            if (button == MouseButton.Left)
            {
                _contextMenuPrimed = true;
            }

            // ClickedIndex should be in sync with isEnabled but double check just in case
            if (LastClickedNodeId < 0)
            {
                isEnabled = false;
            }

            switch (WorkbookManager.ActiveTab)
            {
                case TabViewType.STUDY:
                    AppState.EnableTabViewMenuItems(TabViewType.STUDY, LastClickedNodeId, isEnabled);
                    break;
                case TabViewType.MODEL_GAME:
                    AppState.EnableTabViewMenuItems(TabViewType.MODEL_GAME, LastClickedNodeId, true);
                    if (!_contextMenuPrimed)
                    {
                        _mainWin.UiMncModelGames.IsOpen = true;
                        _contextMenuPrimed = true;
                    }
                    break;
                case TabViewType.EXERCISE:
                    AppState.EnableTabViewMenuItems(TabViewType.EXERCISE, LastClickedNodeId, true);
                    if (!_contextMenuPrimed)
                    {
                        _mainWin.UiMncExercises.IsOpen = true;
                        _contextMenuPrimed = true;
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Brings the select run into view.
        /// </summary>
        public void BringSelectedRunIntoView()
        {
            if (_selectedRun != null && (TextUtils.GetIdFromPrefixedString(_selectedRun.Name) != 0))
            {
                _selectedRun?.BringIntoView();
            }
        }

        /// <summary>
        /// Sets the "passive" exercise board to the same
        /// orientation as the main board.
        /// </summary>
        public void AlignExerciseAndMainBoards()
        {
            _exercisePassiveChessBoard?.FlipBoard(_mainWin.MainChessBoard.SideAtBottom);
        }

        // counter to prevent too many debug messages in debug mode
        private static int _debugRegularBkgMsgCount = 0;

        // counter to prevent too many debug messages in debug mode
        private static int _debugSelectedBkgMsgCount = 0;

        /// <summary>
        /// Selects the passed node along with its line id.
        /// TODO: this should not be necessary, replace with a call to SelectNode(TreeNode);
        /// </summary>
        /// <param name="nodeId"></param>
        public void SelectNode(int nodeId)
        {
            TreeNode node = ShownVariationTree.GetNodeFromNodeId(nodeId);
            if (node != null)
            {
                SelectLineAndMove(node.LineId, nodeId);
            }
        }

        /// <summary>
        /// Selects a line for the next/prev sibling if we are at fork.
        /// </summary>
        /// <param name="prevNext"></param>
        /// <returns></returns>
        public TreeNode SelectParallelLine(bool prevNext)
        {
            TreeNode node = null;

            try
            {
                TreeNode currNode = GetSelectedNode();
                node = TreeUtils.GetNextSibling(currNode, prevNext, true);
            }
            catch { }

            if (node != null)
            {
                SelectNode(node);
            }

            return node;
        }

        /// <summary>
        /// Selects the passed node.
        /// Selects the move, its line and the line in ActiveLine.
        /// </summary>
        /// <param name="node"></param>
        public void SelectNode(TreeNode node)
        {
            try
            {
                SelectRun(_dictNodeToRun[node.NodeId], 1, MouseButton.Left);
            }
            catch { }
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
            if (!IsSelectionEnabled())
            {
                return;
            }

            if (ShownVariationTree.ShowTreeLines)
            {
                if (nodeId == 0)
                {
                    RichTextBoxControl.ScrollToHome();
                }

                try
                {
                    BuildForkTable(nodeId);

                    if (_selectedRun != null)
                    {
                        _selectedRun.Background = _selectedRunBkg;
                        _selectedRun.Foreground = _selectedRunFore;
                    }

                    ObservableCollection<TreeNode> lineToSelect = ShownVariationTree.SelectLine(lineId);
                    foreach (TreeNode nd in lineToSelect)
                    {
                        if (nd.NodeId != 0)
                        {
                            if (_dictNodeToRun.ContainsKey(nd.NodeId))
                            {
                                _dictNodeToRun[nd.NodeId].Background = ChessForgeColors.CurrentTheme.RtbBackground;
                            }
                            else if (Configuration.DebugLevel != 0)
                            {
                                //we should always have this key, so show debug message if not
                                if (_debugRegularBkgMsgCount < 2)
                                {
                                    DebugUtils.ShowDebugMessage("WorkbookView:SelectLineAndMove()-brushRegularBkg nodeId=" + nd.NodeId.ToString() + " not in _dictNodeToRun");
                                    _debugRegularBkgMsgCount++;
                                }
                                AppLog.Message("WorkbookView:SelectLineAndMove()-brushRegularBkg nodeId=" + nd.NodeId.ToString() + " not in _dictNodeToRun");
                            }
                        }
                    }

                    _selectedRun = null;
                    _dictNodeToRun.TryGetValue(nodeId, out _selectedRun);

                    if (!string.IsNullOrEmpty(lineId))
                    {
                        foreach (TreeNode nd in lineToSelect)
                        {
                            if (nd.NodeId != 0)
                            {
                                //we should always have this key, so allow crash in the debug mode
                                if (_dictNodeToRun.ContainsKey(nd.NodeId))
                                {
                                    _dictNodeToRun[nd.NodeId].Background = ChessForgeColors.CurrentTheme.RtbSelectLineBackground;
                                }
                                else if (Configuration.DebugLevel != 0)
                                {
                                    //we should always have this key, so show deubug message if not
                                    if (_debugSelectedBkgMsgCount < 2)
                                    {
                                        DebugUtils.ShowDebugMessage("WorkbookView:SelectLineAndMove()-BrushSelectedBkg nodeId=" + nd.NodeId.ToString() + " not in _dictNodeToRun");
                                        _debugSelectedBkgMsgCount++;
                                    }
                                    AppLog.Message("WorkbookView:SelectLineAndMove()-BrushSelectedBkg nodeId=" + nd.NodeId.ToString() + " not in _dictNodeToRun");
                                }
                            }
                        }
                    }

                    if (_selectedRun != null)
                    {
                        _selectedRunBkg = (SolidColorBrush)_selectedRun.Background;
                        _selectedRunFore = (SolidColorBrush)_selectedRun.Foreground;

                        _selectedRun.Background = ChessForgeColors.CurrentTheme.RtbSelectRunBackground;
                        _selectedRun.Foreground = ChessForgeColors.CurrentTheme.RtbSelectRunForeground;

                        if (nodeId != 0)
                        {
                            _selectedRun.BringIntoView();
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Message("SelectLineAndMove()", ex);
                }
            }
        }

        /// <summary>
        /// Clears the document and relevant structrue.
        /// </summary>
        public void Clear(GameData.ContentType contentType)
        {
            try
            {
                Document.Blocks.Clear();

                PreviousNextViewBars.BuildPreviousNextBar(contentType);

                // resets
                _dictNodeToRun.Clear();
                _dictRunToParagraph.Clear();

                _dictNodeToCommentRun.Clear();
                _dictNodeToCommentBeforeMoveRun.Clear();
                _dictCommentRunToParagraph.Clear();
                _dictCommentBeforeMoveRunToParagraph.Clear();

                _currParagraphLevel = 0;
            }
            catch (Exception ex)
            {
                AppLog.Message("Clear Blocks", ex);
            }
        }

        /// <summary>
        /// Clears the document and displays the "quick skip messgae".
        /// </summary>
        public void ClearForQuickSkip()
        {
            Clear(GameData.ContentType.GENERIC);
        }

        /// <summary>
        /// Creates a dummy paragraph to use for spacing before
        /// the first "real" paragraph. 
        /// </summary>
        /// <returns></returns>
        private Paragraph BuildDummyPararaph()
        {
            Paragraph dummy = new Paragraph();
            dummy.Margin = new Thickness(0, 0, 0, 0);
            dummy.Inlines.Add(new Run(""));
            return dummy;
        }

        /// <summary>
        /// Builds the player line for the header.
        /// </summary>
        /// <param name="playerName"></param>
        /// <param name="playerElo"></param>
        /// <returns></returns>
        private string BuildPlayerLine(string playerName, string playerElo)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return "NN";
            }

            if (string.IsNullOrWhiteSpace(playerElo))
            {
                return playerName;
            }
            else
            {
                return playerName + " (" + playerElo + ")";
            }
        }

        /// <summary>
        /// Builds the line with the ECO code and the result.
        /// </summary>
        /// <param name="eco"></param>
        /// <param name="result"></param>
        /// <param name="rEco"></param>
        /// <param name="rResult"></param>
        private void BuildResultAndEcoLine(string eco, string result, out Run rEco, out Run rResult)
        {
            rEco = null;
            rResult = null;
            if (!string.IsNullOrWhiteSpace(eco))
            {
                rEco = new Run(eco + "  ");
            }

            if (!string.IsNullOrWhiteSpace(result) && result != "*")
            {
                rResult = new Run("(" + result + ")");
            }
        }

        /// <summary>
        /// Builds a Table with moves available at the passed node.
        /// There must be at least 2 children in the passed node for the
        /// table to be shown.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="style"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private Table BuildForkTable(int nodeId)
        {
            if (!Configuration.ShowMovesAtFork)
            {
                return null;
            }

            Document.Blocks.Remove(_forkTable);
            _forkTable = null;

            TreeNode node = ShownVariationTree.GetNodeFromNodeId(nodeId);

            if (node == null || node.Children.Count <= 2)
            {
                return null;
            }

            try
            {
                Run r = _dictNodeToRun[node.NodeId];
                Paragraph para = _dictRunToParagraph[r];
                _forkTable = null;

                if (ShownVariationTree != null)
                {
                    _forkTable = CreateTable(para.Margin.Left);

                    // constant settings
                    _forkTable.FontSize = 14 + Configuration.FontSizeDiff;
                    _forkTable.CellSpacing = 2;
                    int columnsPerRow = 4;

                    // number of move to put in the table
                    int moveCount = node.Children.Count;

                    // required number of rows
                    int rowCount = (int)((moveCount - 1) / columnsPerRow) + 1;
                    _forkTable.RowGroups.Add(new TableRowGroup());
                    for (int i = 0; i < rowCount; i++)
                    {
                        _forkTable.RowGroups[0].Rows.Add(new TableRow());
                    }

                    // required number of columns 
                    int columnCount = moveCount <= columnsPerRow ? moveCount : columnsPerRow;
                    // total cells in the table, with moves or without
                    int cellCount = columnCount * rowCount;

                    // create columns
                    for (int i = 0; i < columnCount; i++)
                    {
                        _forkTable.Columns.Add(new TableColumn());
                    }

                    // populate cells
                    for (int i = 0; i < cellCount; i++)
                    {
                        int rowIndex = (int)(i / columnsPerRow);
                        int columnIndex = i - (rowIndex * columnsPerRow);
                        TableRow row = _forkTable.RowGroups[0].Rows[rowIndex];

                        TableCell cell;
                        if (i < moveCount)
                        {
                            Run rCell = new Run(MoveUtils.BuildSingleMoveText(node.Children[i], true, true, ShownVariationTree.MoveNumberOffset));
                            rCell.Name = _run_fork_move_ + node.Children[i].NodeId.ToString();
                            rCell.MouseDown += EventForkChildClicked;

                            cell = new TableCell(new Paragraph(rCell));
                            // click on the Cell to have the same effect as click on the Run
                            cell.MouseDown += EventForkChildClicked;
                            cell.Cursor = Cursors.Hand;
                            cell.Blocks.First().Name = _run_fork_move_ + node.Children[i].NodeId.ToString();
                        }
                        else
                        {
                            cell = new TableCell(new Paragraph(new Run("")));
                        }
                        cell.TextAlignment = TextAlignment.Center;

                        if ((i % 2 + rowIndex % 2) % 2 == 0)
                        {
                            cell.Background = _forkTable.Columns[columnIndex].Background = Brushes.LightBlue;
                        }
                        else
                        {
                            cell.Background = _forkTable.Columns[columnIndex].Background = Brushes.LightSteelBlue;
                        }
                        cell.BorderThickness = new Thickness(2, 2, 2, 2);
                        cell.BorderBrush = Brushes.White;
                        row.Cells.Add(cell);
                    }

                    Document.Blocks.InsertAfter(para, _forkTable);

                }
            }
            catch (Exception ex)
            {
                AppLog.Message("EventForkChildClicked()", ex);
            }

            return _forkTable;
        }

        /// <summary>
        /// Builds a paragraph with Game/Exercise result
        /// </summary>
        /// <returns></returns>
        private Paragraph BuildResultPara()
        {
            if (_mainVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.NONE)
            {
                string result = ShownVariationTree.Header.GetResult(out _);
                if (!string.IsNullOrWhiteSpace(result))
                {
                    Paragraph para = CreateParagraph("0", true);
                    para.Margin = new Thickness(0, 0, 0, 0);
                    Run rResult = new Run("     " + result);
                    para.Inlines.Add(rResult);
                    return para;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Builds a preamble paragarph
        /// </summary>
        /// <returns></returns>
        private Paragraph BuildPreamble()
        {
            string preamble = _mainVariationTree.Header.BuildPreambleText();
            if (!string.IsNullOrWhiteSpace(preamble))
            {
                Paragraph para = CreateParagraph("preamble", true);
                para.Margin = new Thickness(20, 20, 20, 20);
                Run rPreamble = new Run(preamble);
                para.Inlines.Add(rPreamble);
                para.BorderThickness = new Thickness(1, 1, 1, 1);
                para.BorderBrush = ChessForgeColors.CurrentTheme.RtbForeground;
                para.Padding = new Thickness(10, 10, 10, 10);

                para.MouseLeftButtonDown += EventPageHeaderClicked;
                return para;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// To be overridden in the Exercise view
        /// </summary>
        /// <returns></returns>
        virtual protected void BuildExerciseParagraphs()
        {
        }

        /// <summary>
        /// Whether diagram inerted here should be large or small.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        protected virtual bool IsLargeDiagram(TreeNode nd)
        {
            return nd != null && nd.IsMainLine();
        }

        /// <summary>
        /// On Mouse up on the button brings the first node to view.
        /// Doing it from the click handler would be premature (ineffective).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void EventShowHideButtonMouseUp(object sender, RoutedEventArgs e)
        {
            try
            {
                _mainWin.SetActiveLine("1", 0);
                SelectLineAndMove("1", 0);
                _mainWin.DisplayPosition(_mainVariationTree.Nodes[0]);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Responds to the Show/Hide button being clicked by
        /// flipping the ShowTreeLines flag and requesting the rebuild of the document.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EventShowHideButtonClicked(object sender, RoutedEventArgs e)
        {
            _contextMenuPrimed = true;
            _mainVariationTree.ShowTreeLines = !_mainVariationTree.ShowTreeLines;

            ShowHideSolution(_mainVariationTree.ShowTreeLines);

            if (e != null)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Shows or hides the solution lines.
        /// </summary>
        /// <param name="showHide"></param>
        public void ShowHideSolution(bool showHide)
        {
            if (showHide)
            {
                _mainVariationTree.CurrentSolvingMode = VariationTree.SolvingMode.EDITING;
                AppState.MainWin.ResizeTabControl(AppState.MainWin.UiTabCtrlManualReview, TabControlSizeMode.SHOW_ACTIVE_LINE);
            }
            else
            {
                _mainVariationTree.CurrentSolvingMode = VariationTree.SolvingMode.NONE;
                AppState.MainWin.ResizeTabControl(AppState.MainWin.UiTabCtrlManualReview, TabControlSizeMode.HIDE_ACTIVE_LINE);
            }

            AppState.ShowExplorers(AppState.AreExplorersOn, true);
            BuildFlowDocumentForVariationTree();
            _mainWin.BoardCommentBox.ShowTabHints();
        }

        /// <summary>
        /// Traverses the tree back from each leaf populating the DistanceToFork
        /// and DistanceToLeaf fields
        /// </summary>
        private void SetNodeDistances()
        {
            foreach (TreeNode nd in ShownVariationTree.Nodes)
            {
                if (nd != null)
                {
                    nd.DistanceToLeaf = -1;
                    nd.DistanceToNextFork = 0;
                }
            }

            foreach (TreeNode nd in ShownVariationTree.Nodes)
            {
                // if the node is a leaf start traversing
                if (nd != null && IsLeaf(nd))
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
        public void AddNewNodeToDocument(TreeNode nd)
        {
            TreeNode parent = nd.Parent;

            Inline rParent;
            Paragraph para;

            try
            {
                if (_dictNodeToCommentRun.ContainsKey(parent.NodeId))
                {
                    rParent = _dictNodeToCommentRun[parent.NodeId];
                    para = _dictCommentRunToParagraph[rParent];
                }
                else
                {
                    rParent = _dictNodeToRun[parent.NodeId];
                    para = _dictRunToParagraph[rParent as Run];
                }


                Run r = new Run(" " + MoveUtils.BuildSingleMoveText(nd, false, false, ShownVariationTree.MoveNumberOffset));
                r.Name = _run_ + nd.NodeId.ToString();
                r.PreviewMouseDown += EventRunClicked;

                r.FontStyle = rParent.FontStyle;
                r.FontSize = rParent.FontSize;
                if (nd.IsMainLine())
                {
                    r.FontWeight = FontWeights.Bold;
                }
                else
                {
                    r.FontWeight = FontWeights.Normal;
                }
                r.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;

                _dictNodeToRun[nd.NodeId] = r;
                _dictRunToParagraph[r] = para;

                para.Inlines.InsertAfter(rParent, r);
                _lastAddedRun = r;
            }
            catch { }
        }

        /// <summary>
        /// Each invoked instance builds text of a single Line in the Workbook. 
        /// Calls itself recursively and returns the text of the complete
        /// Workbook.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeNumber"></param>
        virtual protected void BuildTreeLineText(TreeNode nd, Paragraph para, bool includeNumber)
        {
            while (true)
            {
                // if the node has no children,
                if (nd.Children.Count == 0)
                {
                    return;
                }

                bool diagram = false;
                // if the node has 1 child, print it,
                // keep the same level and sublevel as the parent
                // call this method on the child
                if (GetNodeType(nd) == NodeType.ISOLATED || GetNodeType(nd) == NodeType.LEAF)
                {
                    TreeNode child = nd.Children[0];
                    BuildNodeTextAndAddToPara(child, includeNumber, para, out diagram);
                    BuildTreeLineText(child, para, diagram);
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
                    BuildNodeTextAndAddToPara(nd.Children[0], includeNumber, para, out diagram);

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
                                para2 = CreateParagraph(_currParagraphLevel.ToString(), true);
                            }
                            else
                            {
                                para2 = CreateParagraph(_currParagraphLevel.ToString(), true);
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

                        BuildNodeTextAndAddToPara(nd.Children[i], true, para2, out diagram);
                        BuildTreeLineText(nd.Children[i], para2, diagram);

                        if (multi && i == nd.Children.Count - 1)
                        {
                            _currParagraphLevel--;
                            para = CreateParagraph(_currParagraphLevel.ToString(), true);
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
                        Paragraph spec = CreateParagraph(_currParagraphLevel.ToString(), true);
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
        /// Builds text of an individual node (ply),
        /// creates a new Run and adds it to the paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeNumber"></param>
        protected Run BuildNodeTextAndAddToPara(TreeNode nd, bool includeNumber, Paragraph para, out bool diagram, int displayLevel = -1, bool inclComment = true)
        {
            diagram = false;

            // check if we must set includeNumber to true
            if (!includeNumber && (inclComment && IsLastRunComment(para, nd) || !string.IsNullOrEmpty(nd.CommentBeforeMove)))
            {
                includeNumber = true;
            }
            string nodeText = BuildNodeText(nd, includeNumber);
            SolidColorBrush fontColor = null;
            if (IsFork(nd.Parent) && !nd.IsMainLine())
            {
                if (!nd.IsFirstChild())
                {
                    if (displayLevel < 0)
                    {
                        displayLevel = _currParagraphLevel;
                    }

                    fontColor = ChessForgeColors.GetForegroundForLevel(displayLevel - 1);
                }
            }

            Run rMove = AddRunToParagraph(nd, para, nodeText, fontColor);

            if (inclComment)
            {
                // must use Insert... because cannot Add... before rMove is created.
                InsertOrUpdateCommentBeforeMoveRun(nd, includeNumber);
                AddCommentRunsToParagraph(nd, para, out bool isBlunder);
                if (AddDiagramToParagraph(nd, para))
                {
                    diagram = true;
                }
                if (isBlunder)
                {
                    TextUtils.RemoveBlunderNagFromText(rMove);
                }
            }

            return rMove;
        }

        /// <summary>
        /// Builds text for the passed Node.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeNumber"></param>
        /// <returns></returns>
        protected string BuildNodeText(TreeNode nd, bool includeNumber)
        {
            if (nd.NodeId == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();

            if (nd.Position.ColorToMove == PieceColor.Black)
            {
                if (!includeNumber && nd.Position.MoveNumber != 1)
                {
                    //sb.Append(" ");
                }
                sb.Append((nd.Position.MoveNumber + MainVariationTree.MoveNumberOffset).ToString() + ".");
            }

            if (nd.Position.ColorToMove == PieceColor.White && includeNumber)
            {
                sb.Append((nd.Position.MoveNumber + MainVariationTree.MoveNumberOffset).ToString() + "...");
            }

            if (nd.Position.ColorToMove == PieceColor.White)
            {
                //sb.Append(" ");
            }

            sb.Append(nd.GetGuiPlyText(true));
            sb.Append(" ");
            return sb.ToString();
        }

        /// <summary>
        /// Creates a run with no move for the starting position.
        /// This is necessary so that we have a parent for the first move
        /// when starting a new Workbook.
        /// Also, there may be a comment that we want to show before the first move.
        /// </summary>
        /// <param name="para"></param>
        private void CreateRunForStartingNode(Paragraph para, TreeNode nd)
        {
            if (nd != null)
            {
                Run r = AddRunToParagraph(nd, para, "", Brushes.White);
                AddCommentRunsToParagraph(nd, para, out bool isBlunder);
                if (isBlunder)
                {
                    TextUtils.RemoveBlunderNagFromText(r);
                }
            }
        }

        /// <summary>
        /// Creates a run, formats it and adds to the passed paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="para"></param>
        /// <param name="text"></param>
        /// <param name="fontColor"></param>
        private Run AddRunToParagraph(TreeNode nd, Paragraph para, string text, SolidColorBrush fontColor)
        {
            Run r = null;

            try
            {
                r = new Run(text.ToString());
                r.Name = _run_ + nd.NodeId.ToString();
                r.PreviewMouseDown += EventRunClicked;

                if (_isIntraFork)
                {
                    r.FontStyle = _intraForkFontStyle;
                    r.FontSize = GetParaAttrs((_currParagraphLevel + 1).ToString(), true).FontSize;
                }

                // only used the passed fontColor on the first move in the paragraph
                if (fontColor != null && para.Inlines.Count == 0)
                {
                    r.Foreground = fontColor;
                    r.FontWeight = FontWeights.Bold;
                }

                // use bold on the mainline and ... if the paragraph is left aligned
                // TODO: what does the second condition do?
                if (para.Margin.Left == 0 && nd.IsMainLine())
                {
                    r.FontWeight = FontWeights.Bold;
                    para.Inlines.Add(r);
                }
                else
                {
                    para.Inlines.Add(r);
                }

                _dictNodeToRun[nd.NodeId] = r;
                _dictRunToParagraph[r] = para;

                _lastAddedRun = r;
            }
            catch (Exception ex)
            {
                AppLog.Message("AddRunToParagraph()", ex);
            }

            return r;
        }

        /// <summary>
        /// Whether move/node selection is allowed in the current mode.
        /// </summary>
        /// <returns></returns>
        virtual protected bool IsSelectionEnabled()
        {
            return true;
        }

        /// <summary>
        /// Places a deep copy of the "selected for copy" nodes in the clipboard
        /// </summary>
        public void PlaceSelectedForCopyInClipboard()
        {
            if (_selectedForCopy.Count == 0)
            {
                TreeNode nd = GetSelectedNode();
                if (nd != null && nd.NodeId != 0)
                {
                    _selectedForCopy.Add(nd);
                }
            }

            if (_selectedForCopy.Count > 0)
            {
                List<TreeNode> lstNodes = TreeUtils.CopyNodeList(_selectedForCopy);
                SystemClipboard.CopyMoveList(lstNodes, ShownVariationTree.MoveNumberOffset);
                _mainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgCopiedMoves, HintType.INFO);
            }
        }

        /// <summary>
        /// Selects the entire subtree under the currently selected node.
        /// </summary>
        public void SelectSubtreeForCopy()
        {
            ClearCopySelect();

            TreeNode selectedNode = GetSelectedNode();
            if (selectedNode != null)
            {
                List<TreeNode> lstNodes = ShownVariationTree.BuildSubTreeNodeList(selectedNode, false);
                _selectedForCopy.AddRange(lstNodes);
                HighlightSelectedForCopy();
            }
        }

        /// <summary>
        /// Selects for copy the currently highlighted line.
        /// </summary>
        public void SelectActiveLineForCopy()
        {
            ClearCopySelect();

            ObservableCollection<TreeNode> lstNodes = _mainWin.GetActiveLine();
            _selectedForCopy.AddRange(lstNodes);
            HighlightSelectedForCopy();
        }

        /// <summary>
        /// Change background to the "Copy Select" color
        /// for all nodes between the selected node to the passed one. 
        /// </summary>
        /// <param name="r"></param>
        private void SetCopySelect(Run r)
        {
            try
            {
                if (_selectedForCopy.Count > 0)
                {
                    HighlightActiveLine();
                }

                _selectedForCopy.Clear();

                TreeNode currSelected = GetSelectedNode();
                TreeNode shiftClicked = null;
                if (r.Name != null && r.Name.StartsWith(_run_))
                {
                    int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
                    shiftClicked = ShownVariationTree.GetNodeFromNodeId(nodeId);
                }

                if (currSelected != null && shiftClicked != null)
                {
                    // check if there is a branch between the 2
                    TreeNode node_1 = shiftClicked;
                    TreeNode node_2 = currSelected;

                    if (currSelected.MoveNumber < shiftClicked.MoveNumber || currSelected.MoveNumber == shiftClicked.MoveNumber && currSelected.ColorToMove == PieceColor.Black)
                    {
                        node_1 = currSelected;
                        node_2 = shiftClicked;
                    }

                    bool found = false;
                    while (node_2.Parent != null && node_1.MoveNumber <= node_2.MoveNumber)
                    {
                        _selectedForCopy.Insert(0, node_2);
                        if (node_2.NodeId == node_1.NodeId)
                        {
                            found = true;
                            break;
                        }
                        node_2 = node_2.Parent;
                    }

                    if (found)
                    {
                        HighlightSelectedForCopy();
                        PlaceSelectedForCopyInClipboard();
                    }
                    else
                    {
                        _selectedForCopy.Clear();
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Highlights the nodes selected for copy.
        /// </summary>
        private void HighlightSelectedForCopy()
        {
            try
            {
                TreeNode selectedNode = GetSelectedNode();
                foreach (TreeNode nd in _selectedForCopy)
                {
                    if (nd == selectedNode)
                    {
                        _dictNodeToRun[nd.NodeId].Foreground = ChessForgeColors.CurrentTheme.RtbSelectMoveWhileCopyForeground;
                        _dictNodeToRun[nd.NodeId].Background = ChessForgeColors.CurrentTheme.RtbSelectMoveWhileCopyBackground;
                    }
                    else
                    {
                        _dictNodeToRun[nd.NodeId].Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
                        _dictNodeToRun[nd.NodeId].Background = ChessForgeColors.CurrentTheme.RtbSelectMovesForCopyBackground;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("HighlightSelectedForCopy()", ex);
            }
        }

        /// <summary>
        /// Clears the "for Copy" selection.
        /// </summary>
        protected void ClearCopySelect()
        {
            try
            {
                // reset copy selection if any
                if (_selectedForCopy.Count > 0)
                {
                    HighlightActiveLine();

                    TreeNode selectedNode = GetSelectedNode();
                    foreach (TreeNode nd in _selectedForCopy)
                    {
                        _dictNodeToRun[nd.NodeId].Background = ChessForgeColors.CurrentTheme.RtbBackground; ;
                    }
                    HighlightActiveLine();
                    if (selectedNode != null)
                    {
                        _dictNodeToRun[selectedNode.NodeId].Foreground = ChessForgeColors.CurrentTheme.RtbSelectRunForeground;
                        _dictNodeToRun[selectedNode.NodeId].Background = ChessForgeColors.CurrentTheme.RtbSelectRunBackground;
                    }
                    _selectedForCopy.Clear();
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Select a Run.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="clickCount"></param>
        /// <param name="changedButton"></param>
        protected void SelectRun(Run r, int clickCount, MouseButton changedButton)
        {
            if (!IsSelectionEnabled() || r == null)
            {
                return;
            }

            try
            {
                if (changedButton == MouseButton.Left)
                {
                    ClearCopySelect();
                }

                if (clickCount == 2)
                {
                    if (r != null)
                    {
                        int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
                        TreeNode nd = ShownVariationTree.GetNodeFromNodeId(nodeId);
                        if (_mainWin.InvokeAnnotationsDialog(nd))
                        {
                            InsertOrUpdateCommentRun(nd);
                        }
                    }
                }
                else
                {
                    if (changedButton == MouseButton.Left && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
                    {
                        SetCopySelect(r);
                    }
                    else
                    {
                        if (EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE)
                        {
                            _mainWin.StopEvaluation(true);
                            AppState.SwapCommentBoxForEngineLines(false);
                        }

                        if (_selectedRun != null)
                        {
                            _selectedRun.Background = _selectedRunBkg;
                            _selectedRun.Foreground = _selectedRunFore;
                        }

                        foreach (TreeNode nd in _mainWin.ActiveLine.Line.NodeList)
                        {
                            if (nd.NodeId != 0)
                            {
                                Run run;
                                // if we are dealing with a subtree, we may not have all nodes from the line.
                                if (_dictNodeToRun.TryGetValue(nd.NodeId, out run))
                                {
                                    run.Background = ChessForgeColors.CurrentTheme.RtbBackground;
                                }
                            }
                        }

                        _selectedRun = r;

                        int idd = TextUtils.GetIdFromPrefixedString(r.Name);
                        BuildForkTable(idd);

                        int nodeId = -1;
                        if (r.Name != null && r.Name.StartsWith(_run_))
                        {
                            nodeId = TextUtils.GetIdFromPrefixedString(r.Name);

                            // This should never be needed but protect against unexpected timoing issue with sync/async processing
                            if (!ShownVariationTree.HasLinesCalculated())
                            {
                                ShownVariationTree.BuildLines();
                            }

                            string lineId = ShownVariationTree.GetDefaultLineIdForNode(nodeId);

                            SelectAndHighlightLine(lineId, nodeId);
                            LearningMode.ActiveLineId = lineId;
                        }

                        _selectedRunBkg = (SolidColorBrush)r.Background;
                        _selectedRunFore = (SolidColorBrush)r.Foreground;

                        r.Background = ChessForgeColors.CurrentTheme.RtbSelectRunBackground;
                        r.Foreground = ChessForgeColors.CurrentTheme.RtbSelectRunForeground;

                        // this is a right click offer the context menu
                        if (changedButton == MouseButton.Right)
                        {
                            _lastClickedNodeId = nodeId;
                            EnableActiveTreeViewMenus(changedButton, true);
                        }
                        else
                        {
                            _lastClickedNodeId = nodeId;
                        }

                        if (changedButton != MouseButton.Left)
                        {
                            // restore selection for copy
                            HighlightSelectedForCopy();
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Sets background for all moves in the currently selected line.
        /// </summary>
        /// <param name="lineId"></param>
        /// <param name="nodeId"></param>
        public void SelectAndHighlightLine(string lineId, int nodeId)
        {
            // TODO: do not select line and therefore repaint everything if the clicked line is already selected
            // UNLESS there is "copy select" active
            ObservableCollection<TreeNode> lineToSelect = ShownVariationTree.SelectLine(lineId);
            WorkbookManager.SessionWorkbook.ActiveVariationTree.SetSelectedLineAndMove(lineId, nodeId);
            foreach (TreeNode nd in lineToSelect)
            {
                if (nd.NodeId != 0)
                {
                    if (_dictNodeToRun.TryGetValue(nd.NodeId, out Run run))
                    {
                        run.Background = ChessForgeColors.CurrentTheme.RtbSelectLineBackground;
                    }
                }
            }
            _mainWin.SetActiveLine(lineToSelect, nodeId);
        }

        /// <summary>
        /// Highlights the active line.
        /// </summary>
        private void HighlightActiveLine()
        {
            try
            {
                ObservableCollection<TreeNode> line = _mainWin.GetActiveLine();
                foreach (TreeNode nd in line)
                {
                    if (nd.NodeId != 0)
                    {
                        if (_dictNodeToRun.TryGetValue(nd.NodeId, out Run run))
                        {
                            run.Background = ChessForgeColors.CurrentTheme.RtbSelectLineBackground;
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Event handler invoked when a Run was clicked.
        /// In response, we highlight the line to which this Run belongs
        /// (selecting the top branch for the part of the line beyond
        /// the clicked Run).
        /// 
        /// This event will also be invoked if an inline diagram was clicked.
        /// In that case the diagram's associated Run will be identified.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventRunClicked(object sender, MouseButtonEventArgs e)
        {
            Run r = null;

            if (e.Source is Run)
            {
                r = e.Source as Run;
            }
            else if (sender is InlineUIContainer iuc)
            {
                int nodeId = TextUtils.GetIdFromPrefixedString(iuc.Name);
                if (_dictNodeToRun.ContainsKey(nodeId))
                {
                    r = _dictNodeToRun[nodeId];
                }
            }

            _mainWin.StopReplayIfActive();
            SelectRun(r, e.ClickCount, e.ChangedButton);
        }

        /// <summary>
        /// A hyperlink part of the comment was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventHyperlinkMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try
                {
                    var hyperlink = (Hyperlink)sender;
                    Process.Start(hyperlink.NavigateUri.ToString());
                }
                catch { }
            }
        }

        /// <summary>
        /// Highlight the link when hovered over. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventHyperlinkMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Hyperlink hl)
            {
                hl.Foreground = ChessForgeColors.CurrentTheme.HyperlinkHoveredForeground;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Back to normal hyperlink color when mouse left.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventHyperlinkMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Hyperlink hl)
            {
                hl.Foreground = ChessForgeColors.CurrentTheme.HyperlinkForeground;
                e.Handled = true;
            }
        }

        /// <summary>
        /// An article reference in the comment was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventReferenceMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Inline inl)
                {
                    int nodeId = TextUtils.GetNodeIdAndArticleRefFromPrefixedString(inl.Name, out string articleRef);

                    if (_dictNodeToRun.ContainsKey(nodeId))
                    {
                        SelectRun(_dictNodeToRun[nodeId], 1, MouseButton.Left);
                    }

                    if (e.ChangedButton == MouseButton.Left)
                    {
                        Article art = AppState.Workbook.GetArticleByGuid(articleRef, out int chapterIndex, out int articleIndex, true);
                        if (art != null)
                        {
                            if (art.ContentType == GameData.ContentType.MODEL_GAME || art.ContentType == GameData.ContentType.EXERCISE)
                            {
                                _mainWin.SelectArticle(chapterIndex, art.ContentType, articleIndex);
                            }
                            else
                            {
                                _mainWin.SelectChapterByIndex(chapterIndex, true);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Highlight the article reference when hovered over. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventReferenceMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Inline inl)
            {
                inl.Foreground = ChessForgeColors.CurrentTheme.ReferenceHoveredForeground;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Back to normal article reference color when mouse left.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventReferenceMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Inline inl)
            {
                inl.Foreground = ChessForgeColors.CurrentTheme.ReferenceForeground;
                e.Handled = true;
            }
        }

        /// <summary>
        /// A "comment run" was clicked.
        /// Invoke the dialog and update the run as needed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventCommentRunClicked(object sender, MouseButtonEventArgs e)
        {
            if (!IsSelectionEnabled())
            {
                return;
            }

            Run r = (Run)e.Source;

            int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
            TreeNode nd = _mainWin.ActiveVariationTree.GetNodeFromNodeId(nodeId);

            if (_dictNodeToRun.ContainsKey(nd.NodeId))
            {
                SelectRun(_dictNodeToRun[nd.NodeId], 1, MouseButton.Left);
                if (e.ClickCount == 2 && _mainWin.InvokeAnnotationsDialog(nd))
                {
                    InsertOrUpdateCommentRun(nd);
                }
            }
        }

        /// <summary>
        /// A "comment before move run" was clicked.
        /// Invoke the dialog and update the run as needed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventCommentBeforeMoveRunClicked(object sender, MouseButtonEventArgs e)
        {
            if (!IsSelectionEnabled())
            {
                return;
            }

            if (e.ClickCount == 2)
            {
                Run r = (Run)e.Source;

                int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
                TreeNode nd = _mainWin.ActiveVariationTree.GetNodeFromNodeId(nodeId);

                if (_dictNodeToRun.ContainsKey(nd.NodeId))
                {
                    SelectRun(_dictNodeToRun[nd.NodeId], 1, MouseButton.Left);
                    if (_mainWin.InvokeCommentBeforeMoveDialog(nd))
                    {
                        InsertOrUpdateCommentBeforeMoveRun(nd);
                    }
                }
            }
        }

        /// <summary>
        /// The Page Header paragraph was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        virtual protected void EventPageHeaderClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ClickCount == 2)
                {
                    GameData.ContentType contentType = _mainVariationTree.Header.GetContentType(out _);
                    switch (contentType)
                    {
                        case GameData.ContentType.EXERCISE:
                            _mainWin.EditExerciseHeader();
                            break;
                        case GameData.ContentType.MODEL_GAME:
                            _mainWin.EditGameHeader();
                            break;
                        case GameData.ContentType.STUDY_TREE:
                            _mainWin.RenameChapter(AppState.ActiveChapter);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("EventPageHeaderClicked()", ex);
            }
        }

        /// <summary>
        /// A run in the fork table was clicked.
        /// Select the move with the nodeid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventForkChildClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                TextElement r = e.Source as TextElement;
                int id = TextUtils.GetIdFromPrefixedString(r.Name);
                Run rPly = _dictNodeToRun[id];
                SelectRun(rPly, 1, MouseButton.Left);
                rPly.BringIntoView();
            }
            catch (Exception ex)
            {
                AppLog.Message("EventForkChildClicked()", ex);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Checks if the move's text is prefixed by move number.
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        private bool IsMoveTextWithNumber(string txt)
        {
            if (string.IsNullOrWhiteSpace(txt))
            {
                return false;
            }

            return Char.IsDigit(txt.Trim()[0]);
        }

        /// <summary>
        /// Builds text for the Reference Run
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private string BuildReferenceRunText(TreeNode nd)
        {
            if (string.IsNullOrEmpty(nd.ArticleRefs))
            {
                return "";
            }
            else
            {
                return Constants.CHAR_REFERENCE_MARK.ToString();
            }
        }

        /// <summary>
        /// Colors the last run in the paragraph with the color of the next (lower level)
        /// paragraph's first char.
        /// The idea is to provide a more obvious visual hint as to where the fork is.
        /// </summary>
        private void ColorLastRun()
        {
            if (_lastAddedRun != null)
            {
                string style = _currParagraphLevel.ToString();
                RichTextPara attrs = GetParaAttrs(style, true);
                _lastAddedRun.Foreground = ChessForgeColors.GetForegroundForLevel(_currParagraphLevel - 1);
                _lastAddedRun.FontWeight = FontWeights.Bold;
            }
        }

        /// <summary>
        /// Returns the clicked node from event arguments.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private TreeNode GetClickedNode(MouseButtonEventArgs e)
        {
            try
            {
                Run r = (Run)e.Source;

                int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
                TreeNode nd = _mainWin.ActiveVariationTree.GetNodeFromNodeId(nodeId);
                return nd;
            }
            catch
            {
                return null;
            }
        }

    }
}

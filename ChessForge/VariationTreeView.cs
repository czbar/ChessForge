﻿using ChessPosition;
using ChessPosition.Utils;
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
        /// Index of the entity (game or exercise) in the Active Chapter.
        /// </summary>
        public int EntityIndex
        {
            get => _entityIndex;
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

        // game/exercise index in this view
        private int _entityIndex = -1;

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

        // the RichTextBox control underlying this view.
        public RichTextBox RichTextBoxControl;

        /// <summary>
        /// Constructor. Sets a reference to the 
        /// FlowDocument for the RichTextBox control, via
        /// a call to the base class's constructor.
        /// </summary>
        /// <param name="doc"></param>
        public VariationTreeView(RichTextBox rtb, GameData.ContentType contentType, int entityIndex) : base(rtb.Document)
        {
            _mainWin = AppState.MainWin;
            _contentType = contentType;
            RichTextBoxControl = rtb;
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
        /// Layout definitions for paragrahs at different levels.
        /// </summary>
        private Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            ["0"] = new RichTextPara(0, 10, 16, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(0, 0, 0)), TextAlignment.Left),
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
            ["preamble"] = new RichTextPara(40, 10, 16, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Left),
        };

        // prefixes for run names
        private readonly string _run_fork_move_ = "_run_fork_move_";
        private readonly string _run_ = "run_";
        private readonly string _run_comment_ = "run_comment_";
        private readonly string _run_comment_before_move_ = "run_comment_before_move_";
        private readonly string _run_reference_ = "run_reference_";

        // name of the header paragraph
        private readonly string _para_header_ = "para_header_";

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
        /// Maps Node Ids to Reference Runs for quick access.
        /// </summary>
        private Dictionary<int, Run> _dictNodeToReferenceRun = new Dictionary<int, Run>();

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
        /// Maps Reference Runs to Paragraphs for quick access.
        /// </summary>
        private Dictionary<Run, Paragraph> _dictReferenceRunToParagraph = new Dictionary<Run, Paragraph>();

        /// <summary>
        /// Current Paragraph level.
        /// </summary>
        private int _currParagraphLevel;

        /// <summary>
        /// Color to use for the background of the highlighted line.
        /// </summary>
        private SolidColorBrush _brushSelectedBkg = new SolidColorBrush(Color.FromRgb(255, 255, 206));

        /// <summary>
        /// Color to use for the background of the moves selected for copy.
        /// </summary>
        private SolidColorBrush _brushSelectedForCopyBkg = Brushes.LightBlue;

        /// <summary>
        /// Color to use for the background of the selected move.
        /// </summary>
        private SolidColorBrush _brushSelectedMoveBkg = new SolidColorBrush(Color.FromRgb(0, 0, 0));

        /// <summary>
        /// Color to use for the foreground of the selected move.
        /// </summary>
        private SolidColorBrush _brushSelectedMoveFore = new SolidColorBrush(Color.FromRgb(255, 255, 255));

        /// <summary>
        /// Color to use for the background of the selected move when showing copy selection.
        /// </summary>
        private SolidColorBrush _brushCopySelectedMoveBkg = Brushes.Blue;

        /// <summary>
        /// Color to use for the foreground of the selected move when showing copy selection.
        /// </summary>
        private SolidColorBrush _brushCopySelectedMoveFore = new SolidColorBrush(Color.FromRgb(255, 255, 255));

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
        public void BuildFlowDocumentForVariationTree(int rootNodeId = 0, bool includeStem = true)
        {
            GameData.ContentType contentType = GameData.ContentType.NONE;
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

            if (string.IsNullOrEmpty(_mainVariationTree.RootNode.LineId))
            {
                _mainVariationTree.BuildLines();
            }

            contentType = _mainVariationTree.Header.GetContentType(out _);

            Clear(GameData.ContentType.GENERIC);

            PreviousNextViewBars.BuildPreviousNextBar(contentType);

            Document.Blocks.Add(BuildDummyPararaph());

            Paragraph titlePara = BuildPageHeader(contentType);
            if (titlePara != null)
            {
                Document.Blocks.Add(titlePara);
            }

            BuildExerciseParagraphs();

            Paragraph preamblePara = BuildPreamble();
            if (preamblePara != null)
            {
                Document.Blocks.Add(preamblePara);
            }

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

            if (contentType != GameData.ContentType.EXERCISE || ShownVariationTree.ShowTreeLines)
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
            _selectedRun?.BringIntoView();
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
            SelectRun(_dictNodeToRun[node.NodeId], 1, MouseButton.Left);
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
                                _dictNodeToRun[nd.NodeId].Background = _brushRegularBkg;
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
                                    _dictNodeToRun[nd.NodeId].Background = _brushSelectedBkg;
                                }
                                else if (Configuration.DebugLevel != 0)
                                {
                                    //we should always have this key, so show deubug message if not
                                    if (_debugSelectedBkgMsgCount < 2)
                                    {
                                        DebugUtils.ShowDebugMessage("WorkbookView:SelectLineAndMove()-_brushSelectedBkg nodeId=" + nd.NodeId.ToString() + " not in _dictNodeToRun");
                                        _debugSelectedBkgMsgCount++;
                                    }
                                    AppLog.Message("WorkbookView:SelectLineAndMove()-_brushSelectedBkg nodeId=" + nd.NodeId.ToString() + " not in _dictNodeToRun");
                                }
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

                _dictNodeToReferenceRun.Clear();
                _dictReferenceRunToParagraph.Clear();

                _currParagraphLevel = 0;
            }
            catch(Exception ex)
            {
                AppLog.Message("Clear Blocks", ex);
            }
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
        /// Builds the top paragraph for the page if applicable.
        /// It will be Game/Exercise header or the chapter's title
        /// if we are in the Chapters View
        /// </summary>
        /// <returns></returns>
        private Paragraph BuildPageHeader(GameData.ContentType contentType)
        {
            _pageHeaderParagraph = null;

            if (_mainVariationTree != null)
            {
                switch (contentType)
                {
                    case GameData.ContentType.MODEL_GAME:
                    case GameData.ContentType.EXERCISE:
                        string whitePlayer = _mainVariationTree.Header.GetWhitePlayer(out _);
                        string blackPlayer = _mainVariationTree.Header.GetBlackPlayer(out _);

                        string whitePlayerElo = _mainVariationTree.Header.GetWhitePlayerElo(out _);
                        string blackPlayerElo = _mainVariationTree.Header.GetBlackPlayerElo(out _);

                        _pageHeaderParagraph = CreateParagraph("0", true);
                        _pageHeaderParagraph.Margin = new Thickness(0, 0, 0, 0);
                        _pageHeaderParagraph.Name = _para_header_;
                        _pageHeaderParagraph.MouseLeftButtonDown += EventPageHeaderClicked;

                        bool hasPlayerNames = !(string.IsNullOrWhiteSpace(whitePlayer) && string.IsNullOrWhiteSpace(blackPlayer));

                        if (hasPlayerNames)
                        {
                            Run rWhiteSquare = CreateRun("0", (Constants.CharWhiteSquare.ToString() + " "), true);
                            rWhiteSquare.FontWeight = FontWeights.Normal;
                            _pageHeaderParagraph.Inlines.Add(rWhiteSquare);

                            Run rWhite = CreateRun("0", (BuildPlayerLine(whitePlayer, whitePlayerElo) + "\n"), true);
                            _pageHeaderParagraph.Inlines.Add(rWhite);

                            Run rBlackSquare = CreateRun("0", (Constants.CharBlackSquare.ToString() + " "), true);
                            rBlackSquare.FontWeight = FontWeights.Normal;
                            _pageHeaderParagraph.Inlines.Add(rBlackSquare);

                            Run rBlack = CreateRun("0", (BuildPlayerLine(blackPlayer, blackPlayerElo)) + "\n", true);
                            _pageHeaderParagraph.Inlines.Add(rBlack);
                        }

                        if (!string.IsNullOrEmpty(_mainVariationTree.Header.GetEventName(out _)))
                        {
                            if (hasPlayerNames)
                            {
                                string round = _mainVariationTree.Header.GetRound(out _);
                                if (!string.IsNullOrWhiteSpace(round))
                                {
                                    round = " (" + round + ")";
                                }
                                else
                                {
                                    round = "";
                                }
                                string eventName = _mainVariationTree.Header.GetEventName(out _) + round;
                                Run rEvent = CreateRun("1", "      " + eventName + "\n", true);
                                _pageHeaderParagraph.Inlines.Add(rEvent);
                            }
                            else
                            {
                                Run rEvent = CreateRun("0", _mainVariationTree.Header.GetEventName(out _) + "\n", true);
                                _pageHeaderParagraph.Inlines.Add(rEvent);
                            }
                        }

                        string dateForDisplay = TextUtils.BuildDateFromDisplayFromPgnString(_mainVariationTree.Header.GetDate(out _));
                        if (!string.IsNullOrEmpty(dateForDisplay))
                        {
                            Run rDate = CreateRun("1", "      " + Properties.Resources.Date + ": " + dateForDisplay + "\n", true);
                            _pageHeaderParagraph.Inlines.Add(rDate);
                        }

                        string eco = _mainVariationTree.Header.GetECO(out _);
                        string result = _mainVariationTree.Header.GetResult(out _);
                        BuildResultAndEcoLine(eco, result, out Run rEco, out Run rResult);
                        if (rEco != null || rResult != null)
                        {
                            Run rIndent = new Run("      ");
                            rIndent.FontWeight = FontWeights.Normal;
                            _pageHeaderParagraph.Inlines.Add(rIndent);

                            if (rEco != null)
                            {
                                rEco.FontWeight = FontWeights.Bold;
                                _pageHeaderParagraph.Inlines.Add(rEco);
                            }
                            if (rResult != null)
                            {
                                rResult.FontWeight = FontWeights.Normal;
                                _pageHeaderParagraph.Inlines.Add(rResult);
                            }

                            Run rNewLine = new Run("\n");
                            _pageHeaderParagraph.Inlines.Add(rNewLine);
                        }
                        break;
                    case GameData.ContentType.STUDY_TREE:
                        if (WorkbookManager.SessionWorkbook.ActiveChapter != null)
                        {
                            _pageHeaderParagraph = CreateParagraph("0", true);
                            _pageHeaderParagraph.MouseLeftButtonDown += EventPageHeaderClicked;

                            Run rPrefix = new Run();
                            rPrefix.TextDecorations = TextDecorations.Underline;
                            _pageHeaderParagraph.Inlines.Add(rPrefix);

                            Run r = new Run(WorkbookManager.SessionWorkbook.ActiveChapter.GetTitle());
                            r.TextDecorations = TextDecorations.Underline;
                            _pageHeaderParagraph.Inlines.Add(r);
                        }
                        break;
                }
            }

            return _pageHeaderParagraph;
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

            Run r = _dictNodeToRun[node.NodeId];
            Paragraph para = _dictRunToParagraph[r];
            _forkTable = null;

            if (ShownVariationTree != null)
            {
                try
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
                catch (Exception ex)
                {
                    AppLog.Message("EventForkChildClicked()", ex);
                }
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
                para.BorderBrush = Brushes.Black;
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
            if (_mainVariationTree.ShowTreeLines)
            {
                _mainVariationTree.CurrentSolvingMode = VariationTree.SolvingMode.EDITING;
            }
            else
            {
                _mainVariationTree.CurrentSolvingMode = VariationTree.SolvingMode.NONE;
            }
            AppState.ShowExplorers(AppState.AreExplorersOn, true);
            BuildFlowDocumentForVariationTree();
            _mainWin.BoardCommentBox.ShowTabHints();
            if (e != null)
            {
                e.Handled = true;
            }
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

            if (_dictNodeToCommentRun.ContainsKey(parent.NodeId))
            {
                rParent = _dictNodeToCommentRun[parent.NodeId];
                para = _dictCommentRunToParagraph[rParent];
            }
            else if (_dictNodeToReferenceRun.ContainsKey(parent.NodeId))
            {
                rParent = _dictNodeToReferenceRun[parent.NodeId];
                para = _dictReferenceRunToParagraph[rParent as Run];
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
            r.Foreground = Brushes.Black;

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
        virtual protected void BuildTreeLineText(TreeNode nd, Paragraph para, bool includeNumber)
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
                    BuildNodeTextAndAddToPara(child, includeNumber, para);
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
                    BuildNodeTextAndAddToPara(nd.Children[0], includeNumber, para);

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

                        BuildNodeTextAndAddToPara(nd.Children[i], true, para2);
                        BuildTreeLineText(nd.Children[i], para2, false);

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
        protected Run BuildNodeTextAndAddToPara(TreeNode nd, bool includeNumber, Paragraph para, int displayLevel = -1)
        {
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
                    fontColor = GetParaAttrs(displayLevel.ToString(), true).FirstCharColor;
                }
            }

            Run rMove = AddRunToParagraph(nd, para, nodeText, fontColor);
            // must use Insert... because cannot Add... before rMove is created.
            InsertOrUpdateCommentBeforeMoveRun(nd);
            AddReferenceRunToParagraph(nd, para);
            AddCommentRunsToParagraph(nd, para, out bool isBlunder);
            if (isBlunder)
            {
                TextUtils.RemoveBlunderNagFromText(rMove);
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
                AddReferenceRunToParagraph(nd, para);
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

                _dictNodeToRun.Add(nd.NodeId, r);
                _dictRunToParagraph.Add(r, para);

                _lastAddedRun = r;
            }
            catch (Exception ex)
            {
                AppLog.Message("AddRunToParagraph()", ex);
            }

            return r;
        }

        /// <summary>
        /// Creates the comment Run or Runs if there is a comment with the move.
        /// Adds the runs to the paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="para"></param>
        private void AddCommentRunsToParagraph(TreeNode nd, Paragraph para, out bool isAssessmentBlunderShown)
        {
            isAssessmentBlunderShown = false;

            if (!IsCommentRunToShow(nd))
            {
                return;
            }

            try
            {
                List<CommentPart> parts = CommentProcessor.SplitCommentTextAtUrls(nd.Comment);
                if (nd.QuizPoints != 0)
                {
                    parts.Add(new CommentPart(CommentPartType.QUIZ_POINTS, " *" + Properties.Resources.QuizPoints + ": " + nd.QuizPoints.ToString() + "* "));
                }

                if (nd.IsThumbnail)
                {
                    CommentPart thumb = new CommentPart(CommentPartType.THUMBNAIL_SYMBOL, "");
                    parts.Insert(0, thumb);
                }

                // check only here as we may have quiz points
                if (parts == null)
                {
                    return;
                }

                //CommentPart startPart = new CommentPart(CommentPartType.TEXT, nd.NodeId == 0 ? "[ " : "[ ");
                CommentPart startPart = new CommentPart(CommentPartType.TEXT, " ");
                parts.Insert(0, startPart);

                //CommentPart endPart = new CommentPart(CommentPartType.TEXT, nd.NodeId == 0 ? " ] " : " ] ");
                CommentPart endPart = new CommentPart(CommentPartType.TEXT, " ");
                parts.Add(endPart);

                // in front of that start bracket!
                if (HandleBlunders && nd.Assessment == (uint)ChfCommands.Assessment.BLUNDER && nd.IsMainLine())
                {
                    // if we only have start and end parts so far, delete them.
                    // We don't need brackets if all we have ASSESSMENT
                    if (parts.Count == 2)
                    {
                        parts.Clear();
                        // but we need a space after assessment
                        parts.Add(new CommentPart(CommentPartType.TEXT, " "));
                    }

                    CommentPart ass = new CommentPart(CommentPartType.ASSESSMENT, "");
                    parts.Insert(0, ass);
                }


                Inline inlPrevious = null;
                for (int i = 0; i < parts.Count; i++)
                {
                    CommentPart part = parts[i];
                    Inline inl;

                    switch (part.Type)
                    {
                        case CommentPartType.ASSESSMENT:
                            string assSymbol = " ?? ";
                            inl = new Run(assSymbol);
                            inl.ToolTip = Properties.Resources.TooltipEngineBlunderDetect;
                            inl.FontStyle = FontStyles.Normal;
                            inl.Foreground = Brushes.White;
                            inl.Background = Brushes.Red;
                            inl.FontWeight = FontWeights.Bold;
                            isAssessmentBlunderShown = true;
                            break;
                        case CommentPartType.THUMBNAIL_SYMBOL:
                            // if this is not the second last part, insert extra space
                            string thmb;
                            if (i < parts.Count - 2)
                            {
                                thmb = Constants.CHAR_SQUARED_SQUARE.ToString() + " ";
                            }
                            else
                            {
                                thmb = Constants.CHAR_SQUARED_SQUARE.ToString();
                            }
                            _lastThumbnailNode = nd;
                            inl = new Run(thmb);
                            inl.ToolTip = nd.IsThumbnail ? Properties.Resources.ChapterThumbnail : null;
                            inl.FontStyle = FontStyles.Normal;
                            inl.Foreground = Brushes.Black;
                            inl.FontWeight = FontWeights.Normal;
                            inl.PreviewMouseDown += EventCommentRunClicked;
                            break;
                        case CommentPartType.URL:
                            inl = new Hyperlink(new Run(part.Text));
                            (inl as Hyperlink).NavigateUri = new Uri(part.Text);
                            inl.FontWeight = FontWeights.Normal;
                            inl.PreviewMouseDown += Hyperlink_MouseLeftButtonDown;
                            inl.Foreground = Brushes.Blue;
                            inl.Cursor = Cursors.Hand;
                            break;
                        default:
                            inl = new Run(part.Text);
                            inl.FontStyle = FontStyles.Normal;
                            inl.Foreground = Brushes.Black;
                            inl.FontWeight = FontWeights.Normal;
                            inl.PreviewMouseDown += EventCommentRunClicked;
                            break;
                    }

                    inl.Name = _run_comment_ + nd.NodeId.ToString();

                    if (inlPrevious == null)
                    {
                        // insert after the reference run or immediately after the move run if no reference run
                        Run rNode;
                        if (_dictNodeToReferenceRun.ContainsKey(nd.NodeId))
                        {
                            rNode = _dictNodeToReferenceRun[nd.NodeId];
                        }
                        else
                        {
                            rNode = _dictNodeToRun[nd.NodeId];
                        }
                        para.Inlines.InsertAfter(rNode, inl);

                        _dictNodeToCommentRun.Add(nd.NodeId, inl);
                        _dictCommentRunToParagraph.Add(inl, para);
                    }
                    else
                    {
                        para.Inlines.InsertAfter(inlPrevious, inl);
                    }

                    inlPrevious = inl;
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("AddCommentRunsToParagraph()", ex);
            }
        }


        /// <summary>
        /// Creates the comment-before-move Run if there is a CommentBeforeRun with the move.
        /// Adds the run to the paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="para"></param>
        private void AddCommentBeforeMoveRunToParagraph(TreeNode nd, Paragraph para)
        {
            if (string.IsNullOrEmpty(nd.CommentBeforeMove))
            {
                return;
            }

            try
            {

                //string commentText = "[ " + nd.CommentBeforeMove + " ] ";
                string commentText = " " + nd.CommentBeforeMove + " ";
                Run run = new Run(commentText);
                run.FontStyle = FontStyles.Normal;
                run.Foreground = Brushes.Black;
                run.FontWeight = FontWeights.Normal;
                run.PreviewMouseDown += EventCommentBeforeMoveRunClicked;
                run.Name = _run_comment_ + nd.NodeId.ToString();

                run.Name = _run_comment_before_move_ + nd.NodeId.ToString();

                _dictNodeToCommentBeforeMoveRun.Add(nd.NodeId, run);
                _dictCommentBeforeMoveRunToParagraph.Add(run, para);

                Run rNode = _dictNodeToRun[nd.NodeId];
                para.Inlines.InsertBefore(rNode, run);
            }
            catch (Exception ex)
            {
                AppLog.Message("AddCommentBeforeMoveRunToParagraph()", ex);
            }
        }

        /// <summary>
        /// Creates a new Reference Run and adds it to Paragraph.
        /// A Reference Run contains just a single symbol indicating that there are game
        /// references for the preceding Node.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="para"></param>
        private void AddReferenceRunToParagraph(TreeNode nd, Paragraph para)
        {
            try
            {
                if (string.IsNullOrEmpty(nd.ArticleRefs))
                {
                    return;
                }

                Run r = new Run(BuildReferenceRunText(nd));

                r.Name = _run_reference_ + nd.NodeId.ToString();
                r.ToolTip = Properties.Resources.OpenReferencesDialog;

                r.PreviewMouseDown += EventReferenceRunClicked;

                r.FontStyle = FontStyles.Normal;

                r.Foreground = Brushes.Black;
                r.FontWeight = FontWeights.Normal;

                Run rNode = _dictNodeToRun[nd.NodeId];
                para.Inlines.InsertAfter(rNode, r);

                _dictNodeToReferenceRun.Add(nd.NodeId, r);
                _dictReferenceRunToParagraph.Add(r, para);
            }
            catch (Exception ex)
            {
                AppLog.Message("AddReferenceRunToParagraph()", ex);
            }
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
                _mainWin.BoardCommentBox.ShowFlashAnnouncement(Properties.Resources.FlMsgCopiedMoves, Brushes.Green);
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
                        _dictNodeToRun[nd.NodeId].Foreground = _brushCopySelectedMoveFore;
                        _dictNodeToRun[nd.NodeId].Background = _brushCopySelectedMoveBkg;
                    }
                    else
                    {
                        _dictNodeToRun[nd.NodeId].Foreground = _brushRegularFore;
                        _dictNodeToRun[nd.NodeId].Background = _brushSelectedForCopyBkg;
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
                        _dictNodeToRun[nd.NodeId].Background = _brushRegularBkg;
                    }
                    HighlightActiveLine();
                    if (selectedNode != null)
                    {
                        _dictNodeToRun[selectedNode.NodeId].Foreground = _brushSelectedMoveFore;
                        _dictNodeToRun[selectedNode.NodeId].Background = _brushSelectedMoveBkg;
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
                                run.Background = _brushRegularBkg;
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

                    r.Background = _brushSelectedMoveBkg;
                    r.Foreground = _brushSelectedMoveFore;

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
                        run.Background = _brushSelectedBkg;
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
                            run.Background = _brushSelectedBkg;
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
        /// the clicked Run),
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventRunClicked(object sender, MouseButtonEventArgs e)
        {
            Run r = e.Source as Run;
            _mainWin.StopReplayIfActive();
            SelectRun(r, e.ClickCount, e.ChangedButton);
        }

        /// <summary>
        /// A hyperlink part of the comment was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

            if (e.ClickCount == 2)
            {
                Run r = (Run)e.Source;

                int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
                TreeNode nd = _mainWin.ActiveVariationTree.GetNodeFromNodeId(nodeId);

                if (_dictNodeToRun.ContainsKey(nd.NodeId))
                {
                    SelectRun(_dictNodeToRun[nd.NodeId], 1, MouseButton.Left);
                    if (_mainWin.InvokeAnnotationsDialog(nd))
                    {
                        InsertOrUpdateCommentRun(nd);
                    }
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
        /// Event handler for Article selection.
        /// MainWindow subscribes to it with EventSelectArticle().
        /// </summary>
        public event EventHandler<ChessForgeEventArgs> ArticleSelected;

        /// <summary>
        /// A "reference" run was clicked.
        /// Open the Game Preview dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventReferenceRunClicked(object sender, MouseButtonEventArgs e)
        {
            if (!IsSelectionEnabled())
            {
                return;
            }

            try
            {
                TreeNode nd = GetClickedNode(e);
                SelectRun(_dictNodeToRun[nd.NodeId], e.ClickCount, e.ChangedButton);

                ArticleReferencesDialog dlg = new ArticleReferencesDialog(nd);
                //{
                //    Left = AppState.MainWin.Left + 100,
                //    Top = AppState.MainWin.Top + 100,
                //    Topmost = false,
                //    Owner = AppState.MainWin
                //};
                GuiUtilities.PositionDialog(dlg, AppState.MainWin, 100);

                dlg.ShowDialog();

                if (dlg.SelectedArticle != null)
                {
                    // we exited after user selected Open Full View from the context menu of an item
                    WorkbookManager.SessionWorkbook.GetArticleByGuid(dlg.SelectedArticle.Tree.Header.GetGuid(out _), out int chapterIndex, out int articleIndex);

                    ChessForgeEventArgs args = new ChessForgeEventArgs();
                    args.ChapterIndex = chapterIndex;
                    args.ArticleIndex = articleIndex;
                    args.ContentType = dlg.SelectedArticle.Tree.Header.GetContentType(out _);

                    ArticleSelected?.Invoke(this, args);
                }
                else
                {
                    if (dlg.DialogResult == true)
                    {
                        // user requested the dialog for editing references
                        AppState.MainWin.UiMnReferenceArticles_Click(null, null);
                    }
                }
            }
            catch
            {
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
        /// If the Comment run for the passed node already exists, it will be updated.
        /// If it does not exist, it will be created.
        /// This also updates the run's text itself and move NAGs may have changed
        /// before this method was called.
        /// </summary>
        /// <param name="nd"></param>
        public Inline InsertOrUpdateCommentRun(TreeNode nd)
        {
            Inline inlComment;

            if (nd == null)
            {
                return null;
            }

            try
            {
                Run r;
                _dictNodeToRun.TryGetValue(nd.NodeId, out r);

                if (r == null)
                {
                    // something seriously wrong
                    AppLog.Message("ERROR: InsertOrUpdateCommentRun()- Run " + nd.NodeId.ToString() + " not found in _dictNodeToRun");
                    return null;
                }

                // we are refreshing the move's text in case we have a change in NAG,
                // be sure to keep any leading spaces
                string spaces = TextUtils.GetLeadingSpaces(r.Text);
                r.Text = BuildNodeText(nd, IsMoveTextWithNumber(r.Text));
                r.Text = spaces + r.Text.TrimStart();

                _dictNodeToCommentRun.TryGetValue(nd.NodeId, out inlComment);

                if (!IsCommentRunToShow(nd))
                {
                    // if the comment run existed, remove it
                    if (inlComment != null)
                    {
                        _dictNodeToCommentRun.Remove(nd.NodeId);
                        RemoveCommentRunsFromHostingParagraph(inlComment);
                    }
                }
                else
                {
                    _dictNodeToCommentRun.Remove(nd.NodeId);
                    RemoveCommentRunsFromHostingParagraph(inlComment);

                    Paragraph para = r.Parent as Paragraph;
                    AddCommentRunsToParagraph(nd, para, out bool isBlunder);
                    if (isBlunder)
                    {
                        TextUtils.RemoveBlunderNagFromText(r);
                    }
                }
            }
            catch
            {
                inlComment = null;
            }

            return inlComment;
        }

        /// <summary>
        /// If the Comment Before Run for the passed node already exists, it will be updated.
        /// If it does not exist, it will be created.
        /// </summary>
        /// <param name="nd"></param>
        public Inline InsertOrUpdateCommentBeforeMoveRun(TreeNode nd)
        {
            Inline inlCommentBeforeMove;

            if (nd == null)
            {
                return null;
            }

            try
            {
                Run rMove;
                _dictNodeToRun.TryGetValue(nd.NodeId, out rMove);

                if (rMove == null)
                {
                    // something seriously wrong
                    AppLog.Message("ERROR: InsertOrUpdateCommentBeforeRun()- Run " + nd.NodeId.ToString() + " not found in _dictNodeToRun");
                    return null;
                }

                _dictNodeToCommentBeforeMoveRun.TryGetValue(nd.NodeId, out inlCommentBeforeMove);

                if (string.IsNullOrEmpty(nd.CommentBeforeMove))
                {
                    // if the comment run existed, remove it
                    if (inlCommentBeforeMove != null)
                    {
                        _dictNodeToCommentBeforeMoveRun.Remove(nd.NodeId);
                        RemoveRunFromHostingParagraph(inlCommentBeforeMove);
                    }
                }
                else
                {
                    _dictNodeToCommentBeforeMoveRun.Remove(nd.NodeId);
                    RemoveRunFromHostingParagraph(inlCommentBeforeMove);

                    Paragraph para = rMove.Parent as Paragraph;
                    AddCommentBeforeMoveRunToParagraph(nd, para);
                }
            }
            catch
            {
                inlCommentBeforeMove = null;
            }

            return inlCommentBeforeMove;
        }

        /// <summary>
        /// Updates reference runs for the passed list of nodes.
        /// This is called when the list has changed e.g. after deletion
        /// of a referenced article.
        /// </summary>
        /// <param name="nodes"></param>
        public void UpdateReferenceRuns(List<FullNodeId> nodes)
        {
            if (AppState.Workbook != null)
            {
                foreach (FullNodeId fullNode in nodes)
                {
                    VariationTree tree = AppState.Workbook.GetTreeByTreeId(fullNode.TreeId);
                    if (tree != null)
                    {
                        TreeNode nd = tree.GetNodeFromNodeId(fullNode.NodeId);
                        InsertOrDeleteReferenceRun(nd);
                    }
                }
            }
        }

        /// <summary>
        /// Inserts or deleted a reference run depending
        /// on whether we have any reference for the node
        /// </summary>
        /// <param name="nd"></param>
        public void InsertOrDeleteReferenceRun(TreeNode nd)
        {
            if (nd == null)
            {
                return;
            }

            try
            {
                Run r;
                _dictNodeToRun.TryGetValue(nd.NodeId, out r);

                Run r_reference;
                _dictNodeToReferenceRun.TryGetValue(nd.NodeId, out r_reference);

                if (string.IsNullOrEmpty(nd.ArticleRefs))
                {
                    // if the reference run existed, remove it
                    if (r_reference != null)
                    {
                        _dictNodeToReferenceRun.Remove(nd.NodeId);
                        RemoveRunFromHostingParagraph(r_reference);
                    }
                }
                else
                {
                    // if the reference run existed just leave it, otherwise create it
                    if (r_reference == null)
                    {
                        Paragraph para = r.Parent as Paragraph;
                        AddReferenceRunToParagraph(nd, para);
                    }
                }
            }
            catch
            {
            }
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
                _lastAddedRun.Foreground = attrs.FirstCharColor;
                _lastAddedRun.FontWeight = FontWeights.Bold;
            }
        }

        /// <summary>
        /// Checks if there is anything to show in the comment run i.e.
        /// non-empty comment text, a thumbnail indicator or quiz points if the tree is in exercise editing mode.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private bool IsCommentRunToShow(TreeNode nd)
        {
            return !string.IsNullOrEmpty(nd.Comment)
                   || nd.IsThumbnail
                   || HandleBlunders && nd.Assessment == (uint)ChfCommands.Assessment.BLUNDER && nd.IsMainLine()
                   || (_mainVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.EDITING && nd.QuizPoints != 0);
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

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
//using static System.Net.Mime.MediaTypeNames;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Media3D;
using System.Xml.Linq;
using ChessPosition.GameTree;
using System.Windows.Resources;

namespace ChessForge
{
    /// <summary>
    /// Manages text and events in the main Workbook view.
    /// The view is built in a RichTextBox.
    /// </summary>
    public class VariationTreeView : RichTextBuilder
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
        /// Index of the entity (game or exercise) in the Active Chapter.
        /// </summary>
        public int EntityIndex
        {
            get => _entityIndex;
        }

        // flags freshness of the view
        private bool _isFresh = false;

        // content type in this view
        private GameData.ContentType _contentType;

        // game/exercise index in this view
        private int _entityIndex = -1;

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

        /// <summary>
        /// Constructor. Sets a reference to the 
        /// FlowDocument for the RichTextBox control, via
        /// a call to the base class's constructor.
        /// </summary>
        /// <param name="doc"></param>
        public VariationTreeView(FlowDocument doc, MainWindow mainWin, GameData.ContentType contentType, int entityIndex) : base(doc)
        {
            _mainWin = mainWin;
            _contentType = contentType;
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
                        return AppStateManager.ActiveChapter != null && AppStateManager.ActiveChapter.HasAnyModelGame;
                    case GameData.ContentType.EXERCISE:
                        return AppStateManager.ActiveChapter != null && AppStateManager.ActiveChapter.HasAnyExercise;
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
            ["preamble"] = new RichTextPara(40, 10, 16, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Left),
        };

        // prefixes for run names
        private readonly string _run_fork_move_ = "_run_fork_move_";
        private readonly string _run_ = "run_";
        private readonly string _run_comment_ = "run_comment_";

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
        /// The shown variation tree is either the main _variationTreeOld or the associated tree
        /// </summary>
        protected VariationTree _shownVariationTree
        {
            get => _mainVariationTree.IsAssociatedTreeActive ? _mainVariationTree.AssociatedSecondary : _mainVariationTree;
        }

        /// <summary>
        /// Maps Node Ids to Runs for quick access.
        /// </summary>
        private Dictionary<int, Run> _dictNodeToRun = new Dictionary<int, Run>();

        /// <summary>
        /// Maps Node Ids to Comment Runs for quick access.
        /// </summary>
        private Dictionary<int, Run> _dictNodeToCommentRun = new Dictionary<int, Run>();

        /// <summary>
        /// Maps Runs to Paragraphs for quick access.
        /// </summary>
        private Dictionary<Run, Paragraph> _dictRunToParagraph = new Dictionary<Run, Paragraph>();

        /// <summary>
        /// Maps Comment Runs to Paragraphs for quick access.
        /// </summary>
        private Dictionary<Run, Paragraph> _dictCommentRunToParagraph = new Dictionary<Run, Paragraph>();

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

            contentType = _mainVariationTree.Header.GetContentType(out _);

            Clear(GameData.ContentType.GENERIC);

            BuildPreviousNextBar(contentType);

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

            if (contentType != GameData.ContentType.EXERCISE || _shownVariationTree.ShowTreeLines)
            {
                // we will traverse back from each leaf to the nearest parent fork (or root of we run out)
                // and note the distances in the Nodes so that we can use them when creating the document
                // in the forward traversing
                SetNodeDistances();

                TreeNode root;
                if (rootNodeId == 0)
                {
                    root = _shownVariationTree.Nodes[0];
                }
                else
                {
                    root = _shownVariationTree.GetNodeFromNodeId(rootNodeId);
                    if (includeStem)
                    {
                        Paragraph paraStem = BuildWorkbookStemLine(root, true);
                        Document.Blocks.Add(paraStem);
                    }
                }

                // start by creating a level 1 paragraph.
                Paragraph para = CreateParagraph("0", true);
                Document.Blocks.Add(para);

                CreateStartingNode(para);

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

            RemoveEmptyParagraphs();
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
                    node = _shownVariationTree.GetNodeFromNodeId(nodeId);
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
                TreeNode nd = _shownVariationTree.GetNodeFromNodeId(_lastClickedNodeId);
                // TODO: it would be more precise to get the last move of the line being promoted and set it as line id
                // otherwise we end up selecting a different line that the one we are promoting.
                // However, with the current GUI logic, the selected line changes when the user right-clicks on the
                // move to promote the line, so the end result wouldn't change. But it may if we change that other logic.
                _shownVariationTree.PromoteLine(nd);
                _mainWin.SetActiveLine(nd.LineId, nd.NodeId);
                BuildFlowDocumentForVariationTree();
                _mainWin.SelectLineAndMoveInWorkbookViews(_mainWin.ActiveTreeView, nd.LineId, _mainWin.ActiveLine.GetSelectedPlyNodeIndex(false));
                AppStateManager.IsDirty = true;
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
                TreeNode nd = GetSelectedNode();
                if (nd == null && _mainVariationTree != null)
                {
                    nd = _mainVariationTree.Nodes[0];
                }

                if (nd != null)
                {
                    Clipboard.SetText(FenParser.GenerateFenFromPosition(nd.Position));
                }
            }
            catch
            {
            }
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
                        chapter.SetTitle("Chapter " + chapter.Id.ToString() + ": " + MoveUtils.BuildSingleMoveText(nd, true, true));

                        ChapterFromLineDialog dlg = new ChapterFromLineDialog(chapter)
                        {
                            Left = _mainWin.ChessForgeMain.Left + 100,
                            Top = _mainWin.ChessForgeMain.Top + 100,
                            Topmost = false,
                            Owner = _mainWin
                        };
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
                                _mainWin.SelectChapterById(chapter.Id, true);
                                viewRebuilt = true;
                            }
                            if (!viewRebuilt)
                            {
                                BuildFlowDocumentForVariationTree();
                            }

                            AppStateManager.IsDirty = true;
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
        /// Deletes the current move and all moves that follow it.
        /// </summary>
        public void DeleteRemainingMoves()
        {
            try
            {
                GameData.ContentType contentType = _shownVariationTree.ContentType;

                TreeNode nd = _shownVariationTree.GetNodeFromNodeId(_lastClickedNodeId);
                TreeNode parent = nd.Parent;
                _shownVariationTree.DeleteRemainingMoves(nd);
                _shownVariationTree.BuildLines();
                _mainWin.SetActiveLine(parent.LineId, parent.NodeId);
                BuildFlowDocumentForVariationTree();
                _mainWin.SelectLineAndMoveInWorkbookViews(_mainWin.ActiveTreeView, parent.LineId, _mainWin.ActiveLine.GetSelectedPlyNodeIndex(true));
                AppStateManager.IsDirty = true;

                if (contentType == GameData.ContentType.STUDY_TREE)
                {
                    BookmarkManager.ResyncBookmarks(1);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("DeleteRemainingMoves()", ex);
            }
        }

        /// <summary>
        /// Merges the selected line in the game into the Study tree
        /// </summary>
        public void MergeIntoStudy()
        {
            try
            {
                GameData.ContentType contentType = _mainVariationTree.ContentType;
                if (_mainVariationTree.ContentType != GameData.ContentType.MODEL_GAME)
                {
                    return;
                }

                TreeNode nd = _mainVariationTree.GetNodeFromNodeId(_lastClickedNodeId);
                List<TreeNode> lstNodes = _mainVariationTree.BuildSubTreeNodeList(nd, true);

                VariationTree treeFromGame = new VariationTree(GameData.ContentType.GENERIC);
                treeFromGame.CreateNew(lstNodes);
                VariationTree targetTree = WorkbookManager.SessionWorkbook.ActiveChapter.StudyTree.Tree;
                
                // Prepare info for potential Undo
                EditOperation op = new EditOperation(EditOperation.EditType.MERGE_TREE, targetTree.GetListOfNodeIds(true), null);

                VariationTree merged = WorkbookTreeMerge.MergeWorkbooks(targetTree, treeFromGame);
                WorkbookManager.SessionWorkbook.ActiveChapter.StudyTree.Tree = merged;
                merged.BuildLines();

                // Save info for undo in the new tree
                WorkbookManager.SessionWorkbook.ActiveChapter.StudyTree.Tree.OpsManager.PushOperation(op);
                
                AppStateManager.IsDirty = true;
            }
            catch (Exception ex)
            {
                AppLog.Message("MergeIntoStudy()", ex);
            }
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
                case WorkbookManager.TabViewType.STUDY:
                    AppStateManager.EnableTabViewMenuItems(WorkbookManager.TabViewType.STUDY, LastClickedNodeId, isEnabled);
                    break;
                case WorkbookManager.TabViewType.MODEL_GAME:
                    AppStateManager.EnableTabViewMenuItems(WorkbookManager.TabViewType.MODEL_GAME, LastClickedNodeId, true);
                    if (!_contextMenuPrimed)
                    {
                        _mainWin.UiCmModelGames.IsOpen = true;
                        _contextMenuPrimed = true;
                    }
                    break;
                case WorkbookManager.TabViewType.EXERCISE:
                    AppStateManager.EnableTabViewMenuItems(WorkbookManager.TabViewType.EXERCISE, LastClickedNodeId, true);
                    if (!_contextMenuPrimed)
                    {
                        _mainWin.UiCmExercises.IsOpen = true;
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
            if (_selectedRun != null)
            {
                _selectedRun.BringIntoView();
            }
        }

        /// <summary>
        /// Sets the "passive" exercise board to the same
        /// orientation as the main board.
        /// </summary>
        public void AlignExerciseAndMainBoards()
        {
            if (_exercisePassiveChessBoard != null)
            {
                _exercisePassiveChessBoard.FlipBoard(_mainWin.MainChessBoard.SideAtBottom);
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
            if (!IsSelectionEnabled())
            {
                return;
            }

            if (_shownVariationTree.ShowTreeLines)
            {
                try
                {
                    BuildForkTable(nodeId);

                    if (_selectedRun != null)
                    {
                        _selectedRun.Background = _selectedRunBkg;
                        _selectedRun.Foreground = _selectedRunFore;
                    }

                    ObservableCollection<TreeNode> lineToSelect = _shownVariationTree.SelectLine(lineId);
                    foreach (TreeNode nd in lineToSelect)
                    {
                        if (nd.NodeId != 0)
                        {
                            if (_dictNodeToRun.ContainsKey(nd.NodeId))
                            {
                                _dictNodeToRun[nd.NodeId].Background = _brushRegularBkg;
                            }
                            else if (Configuration.DebugLevel != 0)
                            {  //we should always have this key, so show debug message if not
                                DebugUtils.ShowDebugMessage("WorkbookView:SelectLineAndMove()-brushRegularBkg nodeId=" + nd.NodeId.ToString() + " not in _dictNodeToRun");
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
                                {  //we should always have this key, so show deubug message if not
                                    DebugUtils.ShowDebugMessage("WorkbookView:SelectLineAndMove()-_brushSelectedBkg nodeId=" + nd.NodeId.ToString() + " not in _dictNodeToRun");
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
            Document.Blocks.Clear();

            BuildPreviousNextBar(contentType);

            // resets
            _dictNodeToRun.Clear();
            _dictRunToParagraph.Clear();

            _dictNodeToCommentRun.Clear();
            _dictCommentRunToParagraph.Clear();

            _currParagraphLevel = 0;
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
        /// Populates or hides the Previous/Next game/exercise bar above the tree view
        /// as appropriate.
        /// </summary>
        /// <param name="contentType"></param>
        private void BuildPreviousNextBar(GameData.ContentType contentType)
        {
            try
            {
                switch (contentType)
                {
                    case GameData.ContentType.STUDY_TREE:
                        BuildPreviousNextChapterBar();
                        break;
                    case GameData.ContentType.MODEL_GAME:
                        BuildPreviousNextModelGameBar();
                        break;
                    case GameData.ContentType.EXERCISE:
                        BuildPreviousNextExerciseBar();
                        break;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Builds the Previous/Next bar for Chapter/Study Tree view.
        /// </summary>
        private void BuildPreviousNextChapterBar()
        {
            int chapterCount = 0;
            int chapterIndex = -1;

            if (WorkbookManager.SessionWorkbook != null)
            {
                chapterCount = WorkbookManager.SessionWorkbook.GetChapterCount();
                chapterIndex = WorkbookManager.SessionWorkbook.ActiveChapterIndex;
            }

            if (chapterCount > 1)
            {
                _mainWin.UiCnvStudyTreePrevNext.Visibility = Visibility.Visible;
                _mainWin.UiGridStudyTree.RowDefinitions[0].Height = new GridLength(20);
                _mainWin.UiRtbStudyTreeView.Height = 620;

                _mainWin.UiLblChapterPrevNextHint.Visibility = Visibility.Visible;
                _mainWin.UiLblChapterCounter.Content = "Chapter " + (chapterIndex + 1).ToString() + " of " + chapterCount.ToString();
                if (chapterIndex == 0)
                {
                    _mainWin.UiImgChapterRightArrow.Visibility = Visibility.Visible;
                    _mainWin.UiImgChapterLeftArrow.Visibility = Visibility.Hidden;
                    _mainWin.UiLblChapterPrevNextHint.Content = "Next";
                }
                else if (chapterIndex == chapterCount - 1)
                {
                    _mainWin.UiImgChapterRightArrow.Visibility = Visibility.Hidden;
                    _mainWin.UiImgChapterLeftArrow.Visibility = Visibility.Visible;
                    _mainWin.UiLblChapterPrevNextHint.Content = "Previous";
                }
                else
                {
                    _mainWin.UiImgChapterRightArrow.Visibility = Visibility.Visible;
                    _mainWin.UiImgChapterLeftArrow.Visibility = Visibility.Visible;
                    _mainWin.UiLblChapterPrevNextHint.Content = "Previous | Next";
                }
            }
            else
            {
                _mainWin.UiCnvStudyTreePrevNext.Visibility = Visibility.Collapsed;
                _mainWin.UiGridStudyTree.RowDefinitions[0].Height = new GridLength(0);
                _mainWin.UiRtbStudyTreeView.Height = 640;
            }
        }


        /// <summary>
        /// Builds the Previous/Next bar for Model Games view.
        /// </summary>
        private void BuildPreviousNextModelGameBar()
        {
            int gameCount = 0;
            int gameIndex = -1;

            if (WorkbookManager.SessionWorkbook != null && WorkbookManager.SessionWorkbook.ActiveChapter != null)
            {
                gameCount = WorkbookManager.SessionWorkbook.ActiveChapter.GetModelGameCount();
                gameIndex = WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex;
            }


            if (gameCount > 1)
            {
                _mainWin.UiCnvModelGamePrevNext.Visibility = Visibility.Visible;
                _mainWin.UiGridModelGames.RowDefinitions[0].Height = new GridLength(20);
                _mainWin.UiRtbModelGamesView.Height = 620;

                _mainWin.UiLblModelGamePrevNextHint.Visibility = Visibility.Visible;
                _mainWin.UiLblGameCounter.Content = "Game " + (gameIndex + 1).ToString() + " of " + gameCount.ToString();
                if (gameIndex == 0)
                {
                    _mainWin.UiImgModelGameRightArrow.Visibility = Visibility.Visible;
                    _mainWin.UiImgModelGameLeftArrow.Visibility = Visibility.Hidden;
                    _mainWin.UiLblModelGamePrevNextHint.Content = "Next";
                }
                else if (gameIndex == gameCount - 1)
                {
                    _mainWin.UiImgModelGameRightArrow.Visibility = Visibility.Hidden;
                    _mainWin.UiImgModelGameLeftArrow.Visibility = Visibility.Visible;
                    _mainWin.UiLblModelGamePrevNextHint.Content = "Previous";
                }
                else
                {
                    _mainWin.UiImgModelGameRightArrow.Visibility = Visibility.Visible;
                    _mainWin.UiImgModelGameLeftArrow.Visibility = Visibility.Visible;
                    _mainWin.UiLblModelGamePrevNextHint.Content = "Previous | Next";
                }
            }
            else
            {
                _mainWin.UiCnvModelGamePrevNext.Visibility = Visibility.Collapsed;
                _mainWin.UiGridModelGames.RowDefinitions[0].Height = new GridLength(0);
                _mainWin.UiRtbModelGamesView.Height = 640;
            }
        }

        /// <summary>
        /// Builds the Previous/Next bar for the Exercises view.
        /// </summary>
        private void BuildPreviousNextExerciseBar()
        {
            int exerciseCount = 0;
            int exerciseIndex = -1;

            if (WorkbookManager.SessionWorkbook != null && WorkbookManager.SessionWorkbook.ActiveChapter != null)
            {
                exerciseCount = WorkbookManager.SessionWorkbook.ActiveChapter.GetExerciseCount();
                exerciseIndex = WorkbookManager.SessionWorkbook.ActiveChapter.ActiveExerciseIndex;
            }

            if (exerciseCount > 1)
            {
                _mainWin.UiCnvExercisePrevNext.Visibility = Visibility.Visible;
                _mainWin.UiGridExercises.RowDefinitions[0].Height = new GridLength(20);
                _mainWin.UiRtbExercisesView.Height = 620;

                _mainWin.UiLblExcercisePrevNextHint.Visibility = Visibility.Visible;
                _mainWin.UiLblExerciseCounter.Content = "Exercise " + (exerciseIndex + 1).ToString() + " of " + exerciseCount.ToString();
                if (exerciseIndex == 0)
                {
                    _mainWin.UiImgExerciseRightArrow.Visibility = Visibility.Visible;
                    _mainWin.UiImgExerciseLeftArrow.Visibility = Visibility.Hidden;
                    _mainWin.UiLblExcercisePrevNextHint.Content = "Next";
                }
                else if (exerciseIndex == exerciseCount - 1)
                {
                    _mainWin.UiImgExerciseRightArrow.Visibility = Visibility.Hidden;
                    _mainWin.UiImgExerciseLeftArrow.Visibility = Visibility.Visible;
                    _mainWin.UiLblExcercisePrevNextHint.Content = "Previous";
                }
                else
                {
                    _mainWin.UiImgExerciseRightArrow.Visibility = Visibility.Visible;
                    _mainWin.UiImgExerciseLeftArrow.Visibility = Visibility.Visible;
                    _mainWin.UiLblExcercisePrevNextHint.Content = "Previous | Next";
                }
            }
            else
            {
                _mainWin.UiCnvExercisePrevNext.Visibility = Visibility.Collapsed;
                _mainWin.UiGridExercises.RowDefinitions[0].Height = new GridLength(0);
                _mainWin.UiRtbModelGamesView.Height = 640;
            }
        }

        /// <summary>
        /// Builds the top paragraph for the page if applicable.
        /// It will be Game/Exercise header or the chapter's title
        /// if we are in the Chapters View
        /// </summary>
        /// <returns></returns>
        private Paragraph BuildPageHeader(GameData.ContentType contentType)
        {
            Paragraph para = null;

            if (_mainVariationTree != null)
            {
                switch (contentType)
                {
                    case GameData.ContentType.MODEL_GAME:
                    case GameData.ContentType.EXERCISE:
                        string whitePlayer = _mainVariationTree.Header.GetWhitePlayer(out _);
                        string blackPlayer = _mainVariationTree.Header.GetBlackPlayer(out _);

                        para = CreateParagraph("0", true);
                        para.Margin = new Thickness(0, 0, 0, 0);
                        para.Name = _para_header_;
                        para.MouseLeftButtonDown += EventPageHeaderClicked;

                        bool hasPlayerNames = !(string.IsNullOrWhiteSpace(whitePlayer) && string.IsNullOrWhiteSpace(blackPlayer));

                        if (hasPlayerNames)
                        {
                            Run rWhiteSquare = CreateRun("0", (Constants.CharWhiteSquare.ToString() + " "), true);
                            rWhiteSquare.FontWeight = FontWeights.Normal;
                            para.Inlines.Add(rWhiteSquare);

                            Run rWhite = CreateRun("0", (whitePlayer ?? "NN") + "\n", true);
                            para.Inlines.Add(rWhite);

                            Run rBlackSquare = CreateRun("0", (Constants.CharBlackSquare.ToString() + " "), true);
                            rBlackSquare.FontWeight = FontWeights.Normal;
                            para.Inlines.Add(rBlackSquare);

                            Run rBlack = CreateRun("0", (blackPlayer ?? "NN") + "\n", true);
                            para.Inlines.Add(rBlack);
                        }

                        if (!string.IsNullOrEmpty(_mainVariationTree.Header.GetEventName(out _)))
                        {
                            if (hasPlayerNames)
                            {
                                Run rEvent = CreateRun("1", "      " + _mainVariationTree.Header.GetEventName(out _) + "\n", true);
                                para.Inlines.Add(rEvent);
                            }
                            else
                            {
                                Run rEvent = CreateRun("0", _mainVariationTree.Header.GetEventName(out _) + "\n", true);
                                para.Inlines.Add(rEvent);
                            }
                        }

                        string date = _mainVariationTree.Header.GetDate(out _);
                        if (!string.IsNullOrEmpty(date))
                        {
                            if (TextUtils.GetDateFromPgnString(date) != null)
                            {
                                Run rDate = CreateRun("1", "      Date: " + date + "\n", true);
                                para.Inlines.Add(rDate);
                            }
                        }

                        string result = _mainVariationTree.Header.GetResult(out _);
                        if (!string.IsNullOrWhiteSpace(result) && result != "*")
                        {
                            Run rResult = new Run("      (" + result + ")\n");
                            rResult.FontWeight = FontWeights.Normal;
                            para.Inlines.Add(rResult);
                        }

                        break;
                    case GameData.ContentType.STUDY_TREE:
                        if (WorkbookManager.SessionWorkbook.ActiveChapter != null)
                        {
                            para = CreateParagraph("0", true);
                            para.MouseLeftButtonDown += EventPageHeaderClicked;

                            Run rPrefix = new Run();
                            rPrefix.TextDecorations = TextDecorations.Underline;
                            para.Inlines.Add(rPrefix);

                            Run r = new Run(WorkbookManager.SessionWorkbook.ActiveChapter.GetTitle());
                            r.TextDecorations = TextDecorations.Underline;
                            para.Inlines.Add(r);
                        }
                        break;
                }
            }

            return para;
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

            TreeNode node = _shownVariationTree.GetNodeFromNodeId(nodeId);

            if (node == null || node.Children.Count <= 2)
            {
                return null;
            }

            Run r = _dictNodeToRun[node.NodeId];
            Paragraph para = _dictRunToParagraph[r];
            _forkTable = null;

            if (_shownVariationTree != null)
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
                            Run rCell = new Run(MoveUtils.BuildSingleMoveText(node.Children[i], true, true));
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
                string result = _shownVariationTree.Header.GetResult(out _);
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
                if (_mainVariationTree.ShowTreeLines)
                {
                    //SelectLineAndMove("1", _variationTree.Nodes[0].Children[0].NodeId);
                    _mainWin.SetActiveLine("1", 0);
                    SelectLineAndMove("1", 0);
                }
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
            foreach (TreeNode nd in _shownVariationTree.Nodes)
            {
                nd.DistanceToLeaf = -1;
                nd.DistanceToNextFork = 0;
            }

            foreach (TreeNode nd in _shownVariationTree.Nodes)
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
        public void AddNewNodeToDocument(TreeNode nd)
        {
            TreeNode parent = nd.Parent;

            Run rParent;
            Paragraph para;

            if (_dictNodeToCommentRun.ContainsKey(parent.NodeId))
            {
                rParent = _dictNodeToCommentRun[parent.NodeId];
                para = _dictCommentRunToParagraph[rParent];
            }
            else
            {
                rParent = _dictNodeToRun[parent.NodeId];
                para = _dictRunToParagraph[rParent];
            }


            Run r = new Run(" " + MoveUtils.BuildSingleMoveText(nd, false));
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
        private void BuildNodeTextAndAddToPara(TreeNode nd, bool includeNumber, Paragraph para)
        {
            string nodeText = BuildNodeText(nd, includeNumber);

            SolidColorBrush fontColor = null;
            if (IsFork(nd.Parent) && !nd.IsMainLine())
            {
                if (!nd.IsFirstChild())
                {
                    fontColor = GetParaAttrs(_currParagraphLevel.ToString(), true).FirstCharColor;
                }
            }

            AddRunToParagraph(nd, para, nodeText, fontColor);
            AddCommentRunToParagraph(nd, para);
        }

        /// <summary>
        /// Builds text for the passed Node.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeNumber"></param>
        /// <returns></returns>
        private string BuildNodeText(TreeNode nd, bool includeNumber)
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

            sb.Append(nd.GetPlyText(true));
            return sb.ToString();
        }

        /// <summary>
        /// Creates a run with no move for the starting position.
        /// This is necessary so that we have parent for the first move
        /// when starting a new Workbook.
        /// Also, there may be a comment that we want to show before the first move.
        /// </summary>
        /// <param name="para"></param>
        private void CreateStartingNode(Paragraph para)
        {
            if (_mainWin.ActiveVariationTree != null)
            {
                TreeNode nd = _mainWin.ActiveVariationTree.Nodes[0];
                AddRunToParagraph(nd, para, "", Brushes.White);
                AddCommentRunToParagraph(nd, para);
            }
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
            try
            {
                Run r = new Run(text.ToString());
                r.Name = _run_ + nd.NodeId.ToString();
                r.PreviewMouseDown += EventRunClicked;

                if (_isIntraFork)
                {
                    r.FontStyle = _intraForkFontStyle;
                    r.FontSize = GetParaAttrs((_currParagraphLevel + 1).ToString(), true).FontSize;
                }

                if (fontColor != null && para.Inlines.Count == 0)
                {
                    r.Foreground = fontColor;
                    r.FontWeight = FontWeights.Bold;
                }

                if (para.Margin.Left == 0 && nd.IsMainLine())
                {
                    r.FontWeight = FontWeights.Bold;
                    para.Inlines.Add(r);
                }
                else
                    para.Inlines.Add(r);

                _dictNodeToRun.Add(nd.NodeId, r);
                _dictRunToParagraph.Add(r, para);

                _lastAddedRun = r;
            }
            catch (Exception ex)
            {
                AppLog.Message("AddRunToParagraph()", ex);
            }
        }

        /// <summary>
        /// Creates a "Comment Run" if there is a comment with the move.
        /// Adds the run to the paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="para"></param>
        private void AddCommentRunToParagraph(TreeNode nd, Paragraph para)
        {
            try
            {
                // check if there is anything to show
                if (string.IsNullOrEmpty(nd.Comment)
                    &&
                    (_mainVariationTree.CurrentSolvingMode != VariationTree.SolvingMode.EDITING || nd.QuizPoints == 0)
                    )
                {
                    return;
                }

                Run rNode = _dictNodeToRun[nd.NodeId];

                Run r = new Run(BuildCommentRunText(nd));
                r.Name = _run_comment_ + nd.NodeId.ToString();
                r.PreviewMouseDown += EventCommentRunClicked;

                r.FontStyle = FontStyles.Normal;

                r.Foreground = Brushes.Black;
                r.FontWeight = FontWeights.Normal;

                para.Inlines.InsertAfter(rNode, r);

                _dictNodeToCommentRun.Add(nd.NodeId, r);
                _dictCommentRunToParagraph.Add(r, para);
            }
            catch (Exception ex)
            {
                AppLog.Message("AddCommentRunToParagraph()", ex);
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
        /// Select a Run.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="clickCount"></param>
        /// <param name="changedButton"></param>
        private void SelectRun(Run r, int clickCount, MouseButton changedButton)
        {
            if (!IsSelectionEnabled())
            {
                return;
            }

            if (clickCount == 2)
            {
                if (r != null)
                {
                    int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
                    TreeNode nd = _shownVariationTree.GetNodeFromNodeId(nodeId);
                    if (_mainWin.InvokeAnnotationsDialog(nd))
                    {
                        InsertOrUpdateCommentRun(nd);
                    }
                }
            }
            else
            {
                if (EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE)
                {
                    _mainWin.StopEvaluation(true);
                    AppStateManager.SwapCommentBoxForEngineLines(false);
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
                        // if we are dealing with a subtree, we may not have 
                        // all nodes from the line.
                        if (_dictNodeToRun.TryGetValue(nd.NodeId, out run))
                        {
                            _dictNodeToRun[nd.NodeId].Background = _brushRegularBkg;
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
                    TreeNode foundNode = _shownVariationTree.GetNodeFromNodeId(nodeId);
                    string lineId = _shownVariationTree.GetDefaultLineIdForNode(nodeId);

                    // TODO: do not select line and therefore repaint everything if the clicked line is already selected
                    ObservableCollection<TreeNode> lineToSelect = _shownVariationTree.SelectLine(lineId);
                    WorkbookManager.SessionWorkbook.ActiveVariationTree.SetSelectedLineAndMove(lineId, nodeId);
                    foreach (TreeNode nd in lineToSelect)
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

                    _mainWin.SetActiveLine(lineToSelect, nodeId);
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
            }
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
            Run r = (Run)e.Source;
            SelectRun(r, e.ClickCount, e.ChangedButton);
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
                if (_mainWin.InvokeAnnotationsDialog(nd))
                {
                    InsertOrUpdateCommentRun(nd);
                }
            }
        }

        /// <summary>
        /// The Page Header paragraph was clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventPageHeaderClicked(object sender, MouseButtonEventArgs e)
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
                            _mainWin.RenameChapter(AppStateManager.ActiveChapter);
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
        }

        /// <summary>
        /// If the Comment run for the passed node already exists, it will be updated.
        /// If it does not exist, it will be created.
        /// This also updates the run's text itself and move NAGs may have changed
        /// before this method was called.
        /// </summary>
        /// <param name="nd"></param>
        public void InsertOrUpdateCommentRun(TreeNode nd)
        {
            if (nd == null)
            {
                return;
            }

            try
            {
                Run r;
                _dictNodeToRun.TryGetValue(nd.NodeId, out r);

                if (r == null)
                {
                    // something seriously wrong
                    AppLog.Message("ERROR: InsertOrUpdateCommentRun()- Run " + nd.NodeId.ToString() + " not found in _dictNodeToRun");
                    return;
                }

                // we are refreshing the move's text in case we have a change in NAG,
                // be sure to keep any leading spaces
                string spaces = TextUtils.GetLeadingSpaces(r.Text);
                r.Text = BuildNodeText(nd, IsMoveTextWithNumber(r.Text));
                r.Text = spaces + r.Text.TrimStart();

                Run r_comment;
                _dictNodeToCommentRun.TryGetValue(nd.NodeId, out r_comment);

                if (string.IsNullOrEmpty(nd.Comment)
                    &&
                    (_mainVariationTree.CurrentSolvingMode != VariationTree.SolvingMode.EDITING || nd.QuizPoints == 0)
                    )
                {
                    // if the comment run existed, remove it
                    if (r_comment != null)
                    {
                        _dictNodeToCommentRun.Remove(nd.NodeId);
                        RemoveRunFromHostingParagraph(r_comment);
                    }
                }
                else
                {
                    // if the comment run existed update it
                    if (r_comment != null)
                    {
                        r_comment.Text = BuildCommentRunText(nd);
                    }
                    // if did not exists, create it
                    else
                    {
                        Paragraph para = r.Parent as Paragraph;
                        AddCommentRunToParagraph(nd, para);
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
        /// Builds text for the Comment Run
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private string BuildCommentRunText(TreeNode nd)
        {
            if (string.IsNullOrEmpty(nd.Comment)
                &&
                (_mainVariationTree.CurrentSolvingMode != VariationTree.SolvingMode.EDITING || nd.QuizPoints == 0)
                )
            {
                return "";
            }

            StringBuilder sb = new StringBuilder(" [");
            if (!string.IsNullOrEmpty(nd.Comment))
            {
                sb.Append(nd.Comment);
            }

            if (_mainVariationTree.CurrentSolvingMode == VariationTree.SolvingMode.EDITING && nd.QuizPoints != 0)
            {
                if (!string.IsNullOrEmpty(nd.Comment))
                {
                    sb.Append(" /");
                }
                sb.Append(" " + nd.QuizPoints.ToString() + " quiz ");
                if (nd.QuizPoints == 1)
                {
                    sb.Append("point ");
                }
                else
                {
                    sb.Append("points ");
                }
            }

            sb.Append("]");

            // if this is a root node add a space because the first move does not have it in front.
            if (nd.NodeId == 0)
            {
                sb.Append(" ");
            }
            return sb.ToString();
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

    }
}

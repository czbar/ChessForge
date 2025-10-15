using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Manages the RichTextBox showing the progress of the training session.
    /// The document has the following structure:
    /// - The "stem" paragraph is displayed at the top of the box.
    /// - The "instructions" paragraph follows
    /// - The initial prompt paragraph is shown before user's first move and is then deleted.
    /// - The paragraphs that follow show user moves and "coach's" responses with comment
    ///   paragraphs, optionally, in between.
    ///   The comment paragraph will in particular include workbook moves other than the move
    ///   made by the user.
    /// - If the user starts a game with the engine, an engine game paragraph will be created.
    ///   It will be removed when the game ends or is abandoned.
    /// - The history paragraph will record the list of session branches (when the user goes back and restarts training 
    ///   at a different move, the new session branch is created.)
    ///      
    /// Paragraphs and Runs are assigned names that are used to upadate / delete or use them 
    /// as position references.
    /// Runs with moves are named using a prefix specific to the type of paragraph they're in
    /// followed by a NodeId for easy identification. 
    /// </summary>
    public partial class TrainingView : RichTextBuilder
    {
        /// <summary>
        /// Types of paragraphs used in this document.
        /// There can only be one, or none, paragraph of a given type in the document
        /// </summary>
        private enum ParaType
        {
            INTRO,
            STEM,
            SESSION_HEADER,
            INSTRUCTIONS,
            PROMPT_TO_MOVE,
            USER_MOVE,
            WORKBOOK_MOVES,
            TAKEBACK
        }

        /// <summary>
        /// Maps paragraph types to Paragraph objects
        /// </summary>
        private Dictionary<ParaType, Paragraph> _dictParas = new Dictionary<ParaType, Paragraph>()
        {
            [ParaType.INTRO] = null,
            [ParaType.STEM] = null,
            [ParaType.SESSION_HEADER] = null,
            [ParaType.INSTRUCTIONS] = null,
            [ParaType.PROMPT_TO_MOVE] = null,
            [ParaType.USER_MOVE] = null,
            [ParaType.WORKBOOK_MOVES] = null,
            [ParaType.TAKEBACK] = null,
        };

        /// <summary>
        /// Keeps the order of paragraphs that is needed 
        /// e.g. when we search for the first non-null paragraph
        /// before a given one.
        /// </summary>
        private List<ParaType> _lstParaOrder = new List<ParaType>()
        {
            ParaType.INTRO,
            ParaType.STEM,
            ParaType.SESSION_HEADER,
            ParaType.INSTRUCTIONS,
            ParaType.PROMPT_TO_MOVE,
            ParaType.USER_MOVE,
            ParaType.WORKBOOK_MOVES,
            ParaType.TAKEBACK
        };

        /// <summary>
        /// Types of moves that can be shown in this view
        /// </summary>
        private enum MoveContext
        {
            // undefined
            NONE,
            // main training line
            LINE,
            // workbook move in the Coach's comment
            WORKBOOK_COMMENT,
            // move in the game against engine
            GAME
        }

        /// <summary>
        /// The paragraph holding the text of the current game
        /// against the engine if one is in progress.
        /// </summary>
        private Paragraph _paraCurrentEngineGame;

        /// <summary>
        /// Number of moves made in the game agains the engine.
        /// This is used to decide when to start a new line in the
        /// Engine Game paragraph.
        /// </summary>
        private int _currentEngineGameMoveCount;

        /// <summary>
        /// The Node at which the training game started
        /// </summary>
        private TreeNode _engineGameRootNode;

        /// <summary>
        /// Whether the floating board is allowed to be displayed.
        /// </summary>
        private bool _blockFloatingBoard;

        /// <summary>
        /// The list of moves found in the Workbook in the current position,
        /// except the move made by the user (if it was a Workbook move).
        /// </summary>
        private List<TreeNode> _otherMovesInWorkbook = new List<TreeNode>();

        /// <summary>
        /// The user's move being currently examined.
        /// </summary>
        private TreeNode _userMove;

        /// <summary>
        /// Node id of the last move made by the user
        /// </summary>
        private int _lastUserMoveNodeId = -1;

        /// <summary>
        /// The last clicked move in the view
        /// </summary>
        private TreeNode _lastClickedNode;

        /// <summary>
        /// The last clicked Run in the view
        /// </summary>
        private Run _lastClickedRun;

        /// <summary>
        /// Position of the mouse when a Node was last clicked.
        /// </summary>
        private Point _lastClickedPoint;

        /// <summary>
        /// Type of move being made.
        /// Used to display the appropriate context menu.
        /// Should be set whenever _lastClickedNode is set.
        /// </summary>
        private MoveContext _moveContext;

        /// <summary>
        /// Offset to apply to move numbers shown in the GUI
        /// </summary>
        private uint _moveNumberOffset;

        /// <summary>
        /// Id of the node over which to temporarily suspend floating board.
        /// </summary>
        private int _nodeIdSuppressFloatingBoard = -1;

        // Brush color for user moves
        private SolidColorBrush _userBrush = ChessForgeColors.GetHintForeground(CommentBox.HintType.INFO);

        // Brush color for workbook moves
        private SolidColorBrush _workbookBrush = ChessForgeColors.GetHintForeground(CommentBox.HintType.PROGRESS);

        /// <summary>
        /// Names and prefixes for Runs.
        /// NOTE: prefixes that are to be followed by NodeId 
        /// must end with the undesrscore character.
        /// This faciliates easy parsing of the NodeId when the run's 
        /// prefix is not that important e.g. when showing the floating
        /// chessboard.
        /// </summary>
        private readonly string _run_engine_game_move_ = "eng_game_";
        private readonly string _run_wb_move_ = "wb_move_";
        private readonly string _run_line_move_ = "line_move_";
        private readonly string _run_move_eval_ = "move_eval_";
        private readonly string _run_stem_move_ = "stem_move_";
        private readonly string _run_user_wb_alignment_ = "user_wb_alignment_";
        private readonly string _run_wb_response_alignment_ = "wb_reponse_alignment_";
        private readonly string _run_wb_alternatives_ = "wb_alternatives_";
        private readonly string _run_wb_comment_ = "wb_comment_";
        private readonly string _run_wb_ended_ = "wb_ended_";

        private readonly string _par_line_moves_ = "par_line_moves_";
        private readonly string _par_game_moves_ = "par_game_moves_";

        private readonly string _par_checkmate_ = "par_checkmate_";
        private readonly string _par_stalemate_ = "par_stalemate_";
        private readonly string _par_insufficientmaterial_ = "par_insufficientmaterial_";

        // Application's Main Window
        private MainWindow _mainWin;

        /// <summary>
        /// Creates an instance of this class and sets reference 
        /// to the FlowDocument managed by the object.
        /// </summary>
        /// <param name="rtb"></param>
        public TrainingView(RichTextBox rtb, MainWindow mainWin) : base(rtb)
        {
            _mainWin = mainWin;
        }

        /// <summary>
        /// Property referencing definitions of Paragraphs 
        /// </summary>
        override internal Dictionary<string, RichTextPara> RichTextParas { get { return _richTextParas; } }

        private static readonly string STYLE_INTRO = "intro";
        private static readonly string STYLE_FIRST_PROMPT = "first_prompt";
        private static readonly string STYLE_SECOND_PROMPT = "second_prompt";
        private static readonly string STYLE_STEM_LINE = "stem_line";
        private static readonly string STYLE_TAKEBACK = "takeback";
        private static readonly string STYLE_MOVES_MAIN = "moves_main";
        private static readonly string STYLE_COACH_NOTES = "coach_notes";
        private static readonly string STYLE_ENGINE_EVAL = "engine_eval";
        private static readonly string STYLE_ENGINE_GAME = "engine_game";

        private static readonly string STYLE_CHECKMATE = "mate";

        private static readonly string STYLE_DEFAULT = "default";

        // type of the training source
        private static GameData.ContentType _sourceType = GameData.ContentType.NONE;

        /// <summary>
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        internal Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            [STYLE_INTRO] = new RichTextPara(0, 0, 12, FontWeights.Normal, TextAlignment.Left),
            [STYLE_FIRST_PROMPT] = new RichTextPara(10, 0, 16, FontWeights.Bold, TextAlignment.Left, Brushes.Green),
            [STYLE_SECOND_PROMPT] = new RichTextPara(10, 0, 14, FontWeights.Bold, TextAlignment.Left, Brushes.Green),
            [STYLE_STEM_LINE] = new RichTextPara(0, 10, 14, FontWeights.Bold, TextAlignment.Left),
            [STYLE_TAKEBACK] = new RichTextPara(20, 0, 16, FontWeights.Bold, TextAlignment.Left, Brushes.DarkOrange),
            [STYLE_DEFAULT] = new RichTextPara(10, 5, 12, FontWeights.Normal, TextAlignment.Left),

            [STYLE_MOVES_MAIN] = new RichTextPara(10, 5, 16, FontWeights.Bold, TextAlignment.Left),
            [STYLE_COACH_NOTES] = new RichTextPara(50, 5, 12, FontWeights.Normal, TextAlignment.Left),
            [STYLE_ENGINE_EVAL] = new RichTextPara(80, 0, 12, FontWeights.Normal, TextAlignment.Left),
            [STYLE_ENGINE_GAME] = new RichTextPara(50, 0, 16, FontWeights.Normal, TextAlignment.Left),

            [STYLE_CHECKMATE] = new RichTextPara(50, 0, 16, FontWeights.Bold, TextAlignment.Left, Brushes.Navy),
        };

        /// <summary>
        /// Initializes the Dictionary that holds references
        /// to Paragraphs.
        /// </summary>
        private void InitParaDictionary()
        {
            _dictParas[ParaType.INTRO] = null;
            _dictParas[ParaType.STEM] = null;
            _dictParas[ParaType.INSTRUCTIONS] = null;
            _dictParas[ParaType.PROMPT_TO_MOVE] = null;
            _dictParas[ParaType.USER_MOVE] = null;
            _dictParas[ParaType.WORKBOOK_MOVES] = null;
            _dictParas[ParaType.TAKEBACK] = null;
        }

        /// <summary>
        /// Initializes the state of the view after the user opened a new Training Session..
        /// Sets the starting node of the training session
        /// and shows the intro text.
        /// </summary>
        /// <param name="node"></param>
        public void Initialize(TreeNode node, GameData.ContentType contentType)
        {
            TrainingSession.StartPosition = node;
            TrainingSession.BuildFirstTrainingLine();

            _sourceType = contentType;
            InitParaDictionary();
            _moveNumberOffset = _mainWin.ActiveVariationTree.MoveNumberOffset;

            Reset(node);
        }

        /// <summary>
        /// Resets GUI elements of the view.
        /// This method is called when the user re-starts training in an existing session.
        /// </summary>
        /// <param name="startNode"></param>
        public void Reset(TreeNode startNode)
        {
            _currentEngineGameMoveCount = 0;
            HostRtb.Document.Blocks.Clear();
            BuildIntroText(startNode);
        }

        /// <summary>
        /// Sets the last clicked node.
        /// </summary>
        /// <param name="nd"></param>
        public void SetLastClickedNode(TreeNode nd)
        {
            _lastClickedNode = nd;
        }

        /// <summary>
        /// Increase/decrease font size in the Training View
        /// </summary>
        /// <param name="increase"></param>
        public void IncrementFontSize(bool? increase)
        {
            if (increase != null)
            {
                foreach (var block in HostRtb.Document.Blocks)
                {
                    if (block is Paragraph)
                    {
                        foreach (Inline inl in (block as Paragraph).Inlines)
                        {
                            inl.FontSize = increase == true ? inl.FontSize + 1 : inl.FontSize - 1;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A color theme was changed in the application and the colors
        /// must be updated.
        /// </summary>
        public void UpdateColors()
        {
            SolidColorBrush previousUserBrush = _userBrush;
            SolidColorBrush previousWorkbookBrush = _workbookBrush;

            _userBrush = ChessForgeColors.GetHintForeground(CommentBox.HintType.INFO);
            _workbookBrush = ChessForgeColors.GetHintForeground(CommentBox.HintType.PROGRESS);

            // first create a dictionary of colors
            Dictionary<string, SolidColorBrush> _dictColors = new Dictionary<string, SolidColorBrush>();

            _dictColors[_run_engine_game_move_] = ChessForgeColors.CurrentTheme.TrainingEngineGameForeground;
            _dictColors[_run_wb_move_] = ChessForgeColors.CurrentTheme.RtbForeground; //TODO: fix
            _dictColors[_run_line_move_] = ChessForgeColors.CurrentTheme.RtbForeground;
            _dictColors[_run_move_eval_] = ChessForgeColors.CurrentTheme.RtbForeground;
            _dictColors[_run_stem_move_] = ChessForgeColors.CurrentTheme.RtbForeground;
            _dictColors[_run_user_wb_alignment_] = ChessForgeColors.CurrentTheme.RtbForeground;
            _dictColors[_run_wb_response_alignment_] = ChessForgeColors.CurrentTheme.RtbForeground;
            _dictColors[_run_wb_alternatives_] = _workbookBrush;
            _dictColors[_run_wb_comment_] = _workbookBrush;
            _dictColors[_run_wb_ended_] = ChessForgeColors.CurrentTheme.RtbForeground;

            foreach (var block in HostRtb.Document.Blocks)
            {
                if (block is Paragraph b)
                {
                    if (b.Name == _par_checkmate_ || b.Name == _par_stalemate_ || b.Name == _par_insufficientmaterial_)
                    {
                        foreach (Inline inl in b.Inlines)
                        {
                            if (inl is Run run)
                            {
                                run.Foreground = ChessForgeColors.CurrentTheme.TrainingCheckmateForeground;
                            }
                        }
                    }
                    else
                    {
                        foreach (Inline inl in b.Inlines)
                        {
                            if (inl is Run run)
                            {
                                string prefix = TextUtils.GetPrefixFromPrefixedString(run.Name);

                                // special case for _run_wb_move_
                                if (prefix == _run_wb_move_)
                                {
                                    if (run.Foreground == previousUserBrush)
                                    {
                                        run.Foreground = _userBrush;
                                    }
                                    else
                                    {
                                        run.Foreground = _workbookBrush;
                                    }
                                }
                                else if (_dictColors.TryGetValue(prefix, out SolidColorBrush brush))
                                {
                                    run.Foreground = brush;
                                }
                                else
                                {
                                    run.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;
                                }
                            }
                        }
                    }
                }
            }

            if (_dictParas[ParaType.TAKEBACK] != null)
            {
                BuildTakebackParagraph();
            }
        }

        /// <summary>
        /// Rolls back the training to the ply
        /// that we want to replace with the last clicked node.
        /// </summary>
        public void RollbackToWorkbookMove(TreeNode rollbackNode)
        {
            try
            {
                AppLog.Message("RollbackToWorkbookMove()");
                TrainingSession.IsTakebackAvailable = false;
                RemoveTakebackParagraph();

                _currentEngineGameMoveCount = 0;

                TrainingSession.RemoveTrainingMoves(rollbackNode);
                EngineGame.RollbackGame(rollbackNode);
                TrainingSession.AdjustTrainingLine(rollbackNode);

                SoundPlayer.PlayMoveSound(rollbackNode.LastMoveAlgebraicNotation);

                TrainingSession.ChangeCurrentState(TrainingSession.State.USER_MOVE_COMPLETED);

                LearningMode.ChangeCurrentMode(LearningMode.Mode.TRAINING);

                AppState.SetupGuiForCurrentStates();

                _mainWin.BoardCommentBox.GameMoveMade(rollbackNode, true);

                RemoveParagraphsFromMove(rollbackNode);
                ReportLastMoveVsWorkbook();

                EnableChangeLineRuns();
            }
            catch (Exception ex)
            {
                AppLog.Message("RollbackToWorkbookMove()", ex);
            }
        }

        /// <summary>
        /// The user requested rollback to one of their moves.
        /// </summary>
        public void RollbackToUserMove(TreeNode ndToRollbackTo = null)
        {
            TrainingSession.IsTakebackAvailable = false;
            RemoveTakebackParagraph();

            if (ndToRollbackTo == null)
            {
                ndToRollbackTo = _lastClickedNode;
            }

            _currentEngineGameMoveCount = 0;

            try
            {
                EngineGame.RollbackGame(ndToRollbackTo);
                TrainingSession.RemoveTrainingMoves(ndToRollbackTo);
                TrainingSession.AdjustTrainingLine(ndToRollbackTo);

                SoundPlayer.PlayMoveSound(ndToRollbackTo.LastMoveAlgebraicNotation);
                TrainingSession.ChangeCurrentState(TrainingSession.State.AWAITING_USER_TRAINING_MOVE);

                LearningMode.ChangeCurrentMode(LearningMode.Mode.TRAINING);
                AppState.SetupGuiForCurrentStates();

                _mainWin.BoardCommentBox.GameMoveMade(ndToRollbackTo, false);

                RemoveParagraphsFromMove(ndToRollbackTo);
                BuildSecondPromptParagraph();
                _mainWin.DisplayPosition(ndToRollbackTo);
                if (TrainingSession.IsContinuousEvaluation)
                {
                    RequestMoveEvaluation(_mainWin.ActiveVariationTreeId, true);
                }

                EnableChangeLineRuns();
            }
            catch (Exception ex)
            {
                AppLog.Message("RollbackToUserMove()", ex);
            }
        }

        /// <summary>
        /// Find the last move in training that is in ActiveTree.
        /// Note that if the user chose not to add the last training line ot the tree,
        /// this won't be the last move in training but the last one that aligned with the Workbook source.
        /// </summary>
        /// <returns></returns>
        public TreeNode LastTrainingNodePresentInActiveTree()
        {
            TreeNode lastNode = null;

            if (_mainWin.ActiveVariationTree != null)
            {
                for (int i = EngineGame.Line.NodeList.Count - 1; i >= 0; i--)
                {
                    TreeNode nd = EngineGame.Line.NodeList[i];
                    if (_mainWin.ActiveVariationTree.Nodes.FirstOrDefault(x => x.NodeId == nd.NodeId) != null)
                    {
                        lastNode = nd;
                        break;
                    }
                }
            }

            return lastNode;
        }

        /// <summary>
        /// Removes all nodes marked "IsNewTrainingNode" unless they exist in the EngineGame.Line.
        /// In other words removes all game moves made previously that are not part of the current game
        /// which is the only one we would consider saving.
        /// </summary>
        public void CleanupVariationTree()
        {
            // for each child 
            while (true)
            {
                bool allClear = true;
                for (int i = 0; i < _mainWin.ActiveVariationTree.Nodes.Count; i++)
                {
                    TreeNode nd = _mainWin.ActiveVariationTree.Nodes[i];
                    if (nd.IsNewTrainingMove && EngineGame.Line.NodeList.FirstOrDefault(x => x.NodeId == nd.NodeId) == null)
                    {
                        _mainWin.ActiveVariationTree.DeleteRemainingMoves(nd);
                        allClear = false;
                        break;
                    }
                }

                if (allClear)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Builds text for intorductory paragraphs.
        /// </summary>
        /// <param name="node"></param>
        private void BuildIntroText(TreeNode node)
        {
            CreateStemParagraph(node);
            BuildInstructionsText();
        }

        /// <summary>
        /// Gets the Run with the text about Workbook line
        /// having ended from the Engine Game paragraph.
        /// </summary>
        /// <returns></returns>
        private Run GetWorkbookEndedRun()
        {
            Run r = null;

            foreach (Inline inl in _paraCurrentEngineGame.Inlines)
            {
                if (inl is Run && inl.Name == _run_wb_ended_)
                {
                    r = inl as Run;
                    break;
                }
            }

            return r;
        }

        /// <summary>
        /// This method will be invoked when we requested evaluation and got the results back.
        /// The EngineMessageProcessor has the results.
        /// We can be in a CONTINUOUS or LINE evaluation mode.
        /// </summary>
        public void ShowEvaluationResult(TreeNode nd, bool delayed)
        {
            if (nd == null)
            {
                AppLog.Message("TrainingView:ShowEvaluationResult(): null node received ");
                return;
            }

            AppLog.Message("TrainingView:ShowEvaluationResult() Evaluation Mode:" + EvaluationManager.CurrentMode.ToString()
                + ", node=" + nd.LastMoveAlgebraicNotation);

            if (EvaluationManager.CurrentMode == EvaluationManager.Mode.ENGINE_GAME && !TrainingSession.IsContinuousEvaluation)
            {
                return;
            }

            ShowEvaluationRun(nd, true);

            if (EvaluationManager.CurrentMode == EvaluationManager.Mode.LINE)
            {
                if (!delayed)
                {
                    AppLog.Message("Request next node in LINE EVAL");
                    RequestMoveEvaluation(_mainWin.ActiveVariationTreeId);
                }
            }
            else if (!TrainingSession.IsContinuousEvaluation && EvaluationManager.CurrentMode != EvaluationManager.Mode.CONTINUOUS)
            {
                EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);
            }

            if (EvaluationManager.CurrentMode == EvaluationManager.Mode.CONTINUOUS)
            {
                if (_lastClickedNode == null)
                {
                    EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);
                }
                else
                {
                    EvaluationManager.SetSingleNodeToEvaluate(_lastClickedNode);
                }
            }
            AppState.SetupGuiForCurrentStates();
        }

        /// <summary>
        /// Creates or updates the evaluation run.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="force"></param>
        public void ShowEvaluationRun(TreeNode nd, bool force = false)
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                Run runEvaluated = GetRunForNodeId(HostRtb.Document, nd.NodeId);
                if (runEvaluated == null)
                {
                    return;
                }

                Paragraph para = runEvaluated.Parent as Paragraph;
                if (para == null)
                {
                    return;
                }

                string runEvalName = _run_move_eval_ + nd.NodeId.ToString();

                // Remove previous evaluation if exists
                var r_prev = para.Inlines.FirstOrDefault(x => x.Name == runEvalName);
                para.Inlines.Remove(r_prev);

                if (!string.IsNullOrEmpty(nd.EngineEvaluation))
                {
                    Run r_eval = CreateEvaluationRun(nd.EngineEvaluation, runEvalName, nd);
                    para.Inlines.InsertAfter(runEvaluated, r_eval);
                }

            });
        }

        /// <summary>
        /// Creates a Run object with the evaluation text
        /// </summary>
        /// <param name="eval"></param>
        /// <param name="runName"></param>
        /// <returns></returns>
        private Run CreateEvaluationRun(string strEval, string runName, TreeNode nd)
        {
            Run r_eval = new Run("(" + strEval + ") ");
            r_eval.Name = runName;
            r_eval.FontWeight = FontWeights.Normal;
            r_eval.Foreground = ChessForgeColors.CurrentTheme.RtbForeground;

            return r_eval;
        }
        /// <summary>
        /// Builds text for the stem of the line and inserts in the 
        /// passed paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="lastNode"></param>
        private void InsertPrefixRuns(Paragraph para, TreeNode lastNode)
        {
            TreeNode nd = lastNode;
            Run prevRun = null;
            // NOTE without nd.Parent != null we'd be getting "starting position" text in front
            while (nd != null && nd.Parent != null)
            {
                Run r = CreateButtonRun(MoveUtils.BuildSingleMoveText(nd, nd.Parent.NodeId == 0, false,
                    _moveNumberOffset) + " ",
                    _run_stem_move_ + nd.NodeId.ToString(),
                    ChessForgeColors.CurrentTheme.RtbForeground, true);

                nd = nd.Parent;

                if (prevRun == null)
                {
                    para.Inlines.Add(r);
                }
                else
                {
                    para.Inlines.InsertBefore(prevRun, r);
                }
                prevRun = r;
            }
        }

        /// <summary>
        /// Returns a list of non-new-training siblings of the passed node.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private List<TreeNode> GetWorkbookNonNullLeafSiblings(TreeNode nd)
        {
            List<TreeNode> lstNodes = new List<TreeNode>();
            if (nd != null && nd.Parent != null)
            {
                foreach (TreeNode child in nd.Parent.Children)
                {
                    // we cannot use ArePositionsIdentical() because nd only has static position
                    if (child.LastMoveEngineNotation != nd.LastMoveEngineNotation
                        && !child.IsNewTrainingMove
                        && !MoveUtils.IsNullLeafMove(child))
                    {
                        lstNodes.Add(child);
                    }
                }
            }

            return lstNodes;
        }

        /// <summary>
        /// Finds paragraph with a given user move.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private Paragraph FindUserMoveParagraph(TreeNode nd)
        {
            string paraName = _par_line_moves_ + nd.NodeId.ToString();
            return FindParagraphByName(HostRtb.Document, paraName, false);
        }

        /// <summary>
        /// Creates a highlighted, clickable text that here we refer to
        /// as a "button".
        /// </summary>
        /// <param name="text"></param>
        /// <param name="runName"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        private Run CreateButtonRun(string text, string runName, Brush color, bool isStem = false)
        {
            Run r = new Run(text);
            r.Name = runName;

            r.Foreground = color;

            r.FontWeight = FontWeights.Bold;
            r.MouseMove += EventRunMoveOver;

            if (!isStem)
            {
                r.MouseDown += EventRunClicked;
                r.Cursor = Cursors.Hand;
            }

            return r;
        }

        /// <summary>
        /// Builds a move text for use in the context menu 
        /// </summary>
        /// <param name="midTxt"></param>
        /// <returns></returns>
        public string BuildMoveTextForMenu(TreeNode nd)
        {
            if (nd == null)
            {
                return "";
            }
            else
            {
                return MoveUtils.BuildSingleMoveText(nd, true, true, _moveNumberOffset);
            }
        }

        /// <summary>
        /// Shows the Training View's context menu.
        /// Visibility of the items is configured based on the
        /// value of _moveContext.
        /// </summary>
        public void ShowPopupMenu()
        {
            if (_lastClickedNode == null)
                return;

            _blockFloatingBoard = true;
            _mainWin.ShowTrainingFloatingBoard(false);

            _mainWin.Dispatcher.Invoke(() =>
            {
                string moveTxt = BuildMoveTextForMenu(_lastClickedNode);

                ContextMenu cm = _mainWin.UiMncTrainingView;
                foreach (object o in cm.Items)
                {
                    if (o is MenuItem)
                    {
                        MenuItem mi = o as MenuItem;
                        switch (mi.Name)
                        {
                            case "_mnTrainEvalMove":
                                mi.Header = Properties.Resources.EvaluateMove + " " + moveTxt;
                                mi.Visibility = Visibility.Visible;
                                break;
                            case "_mnTrainEvalLine":
                                mi.Header = Properties.Resources.EvaluateLine;
                                mi.Visibility = _moveContext == MoveContext.WORKBOOK_COMMENT ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "_mnTrainRestartGame":
                                mi.Header = Properties.Resources.RestartGameFrom + " " + moveTxt;
                                mi.Visibility = _moveContext == MoveContext.GAME ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "_mnRollBackTraining":
                                mi.Header = Properties.Resources.RestartFrom + " " + moveTxt;
                                mi.Visibility = (_moveContext == MoveContext.LINE || _moveContext == MoveContext.WORKBOOK_COMMENT) ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "UiMncTrainReplaceEngineMove":
                                mi.Header = Properties.Resources.ReplaceEngineMove + " " + moveTxt;
                                mi.Visibility = (_moveContext == MoveContext.GAME
                                                 && EngineGame.CurrentState == EngineGame.GameState.USER_THINKING
                                                 && _lastClickedNode.ColorToMove == TrainingSession.ActualTrainingSide)
                                                 ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "_mnTrainSwitchToWorkbook":
                                string altMove = Properties.Resources.TrnPlayMoveInstead;
                                altMove = altMove.Replace("$0", moveTxt);
                                mi.Header = altMove;
                                mi.Visibility = _moveContext == MoveContext.WORKBOOK_COMMENT ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "UiMnCiTrainFromBeginningLine":
                                mi.Visibility = Visibility.Visible;
                                break;
                            case "UiMnCiTrainExitTraining":
                                mi.Visibility = Visibility.Visible;
                                break;
                            default:
                                break;
                        }
                    }

                    if (o is Separator)
                    {
                        ((Separator)o).Visibility = Visibility.Visible;
                    }
                }
                cm.PlacementTarget = _mainWin.UiRtbTrainingProgress;
                cm.IsOpen = true;
                _mainWin.Timers.Stop(AppTimers.TimerId.SHOW_TRAINING_PROGRESS_POPUP_MENU);
            });
            _blockFloatingBoard = false;
        }

        /// <summary>
        /// Collects all Runs found in the Paragraph
        /// identified by the firstRun and all Paragraphs 
        /// that follow.
        /// </summary>
        /// <param name="firstRun"></param>
        private void SetMainLineRunsToEvaluate(string firstParaName, Run firstRun)
        {
            bool started = false;

            foreach (var block in HostRtb.Document.Blocks)
            {
                if (block is Paragraph)
                {
                    Paragraph para = (Paragraph)block;

                    if (!started && para.Name == firstParaName)
                    {
                        started = true;
                    }

                    if (started && (para.Name.StartsWith(_par_line_moves_)))
                    {
                        foreach (var inl in para.Inlines)
                        {
                            if (inl is Run)
                            {
                                if (inl.Name.StartsWith(_run_line_move_) || inl.Name.StartsWith(_run_wb_move_))
                                {
                                    EvaluationManager.AddLineRunToEvaluate(inl as Run);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Collects all Runs found in the passed
        /// game Paragraph, starting from the firstRun.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="firstRun"></param>
        private void SetGameRunsToEvaluate(Paragraph para, Run firstRun)
        {
            bool started = false;
            foreach (var inl in para.Inlines)
            {
                if (inl is Run)
                {
                    if (!started && (firstRun == null || inl.Name == firstRun.Name))
                    {
                        started = true;
                    }

                    if (started && inl.Name.StartsWith(_run_engine_game_move_))
                    {
                        EvaluationManager.AddLineRunToEvaluate(inl as Run);
                    }
                }
            }
        }

        /// <summary>
        /// Restarts the game against the engine after the last clicked move.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RestartGameAfter(object sender, RoutedEventArgs e)
        {
            TreeNode nd = _lastClickedNode;
            if (nd != null)
            {
                try
                {
                    if (_lastClickedNode.ColorToMove != TrainingSession.ActualTrainingSide)
                    {
                        RestartGameAtUserMove(nd);
                    }
                    else
                    {
                        RestartGameAtEngineMove(nd);
                    }

                    UpdateViewForGameRestart(nd);
                }
                catch (Exception ex)
                {
                    AppLog.Message("RestartGameAfter()", ex);
                }
            }
        }

        /// <summary>
        /// Switches to special mode where this is the engine's turn but we are allowing the user 
        /// to manually enter the move that will replace the current engine's move.
        /// </summary>
        public void ReplaceEngineMove()
        {
            // first check that we are indeed replacing an engine move
            TreeNode nd = _lastClickedNode;
            if (nd != null && _lastClickedNode.ColorToMove == TrainingSession.ActualTrainingSide)
            {
                try
                {
                    // pretend that the parent of this move was engine's move so we can enter the wait-for-user mode.
                    // in effect, we are changing sides for a single ply
                    RestartGameAtEngineMove(nd.Parent, true);
                    UpdateViewForGameRestart(nd.Parent);
                }
                catch (Exception ex)
                {
                    AppLog.Message("ReplaceEngineMove()", ex);
                }
            }
        }

        /// <summary>
        /// Roll back training to the last selected node
        /// or the move before.
        /// </summary>
        public void RollbackTraining()
        {
            if (!TrainingSession.IsContinuousEvaluation)
            {
                _mainWin.StopEvaluation(true);
            }

            if (_lastClickedNode != null)
            {
                if (_lastClickedNode.ColorToMove == TrainingSession.ActualTrainingSide)
                {
                    RollbackToUserMove();
                }
                else
                {
                    // A user move was clicked so rollback to the previous Workbook move
                    RollbackToWorkbookMove(_lastClickedNode);
                }
            }
            else
            {
                EnableChangeLineRuns();
            }
        }

        /// <summary>
        /// Handles key presses.
        /// </summary>
        /// <param name="e"></param>
        public void ProcessKeyDown(KeyEventArgs e)
        {
            try
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
                {
                    switch (e.Key)
                    {
                        case Key.B:
                            AppState.MainWin.UiMnTrainFromBeginning_Click(null, e);
                            e.Handled = true;
                            break;
                        case Key.N:
                            AppState.MainWin.UiMnTrainNextLine_Click(null, e);
                            e.Handled = true;
                            break;
                        case Key.P:
                            AppState.MainWin.UiMnTrainPreviousLine_Click(null, e);
                            e.Handled = true;
                            break;
                        case Key.R:
                            AppState.MainWin.UiMnTrainRandomLine(null, e);
                            e.Handled = true;
                            break;
                    }
                }
                else
                {
                    switch (e.Key)
                    {
                        case Key.Space:
                            if (TrainingSession.IsTakebackAvailable)
                            {
                                RestartFromLastUserWorkbookMove();
                                e.Handled = true;
                            }
                            break;
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Restarts the game at the passed user move.
        /// </summary>
        /// <param name="nd"></param>
        private void RestartGameAtUserMove(TreeNode nd)
        {
            SoundPlayer.PlayMoveSound(nd.LastMoveAlgebraicNotation);
            EngineGame.RestartAtUserMove(nd);
            _mainWin.BoardCommentBox.GameMoveMade(nd, true);
        }

        /// <summary>
        /// Restarts the game at the specified engine move.
        /// </summary>
        /// <param name="nd"></param>
        private void RestartGameAtEngineMove(TreeNode nd, bool isEngineMoveReplacement = false)
        {
            SoundPlayer.PlayMoveSound(nd.LastMoveAlgebraicNotation);
            EngineGame.RestartAtEngineMove(nd);
            if (TrainingSession.IsContinuousEvaluation)
            {
                RequestMoveEvaluation(_mainWin.ActiveVariationTreeId, true);
            }

            if (isEngineMoveReplacement)
            {
                _mainWin.BoardCommentBox.GameEngineReplacementToMake(nd);

                // takeback paragraph may exist and could be confusing to the user
                HostRtb.Document.Blocks.Remove(_dictParas[ParaType.TAKEBACK]);
                _dictParas[ParaType.TAKEBACK] = null;

                HostRtb.Document.Blocks.Remove(_dictParas[ParaType.PROMPT_TO_MOVE]);
                _dictParas[ParaType.PROMPT_TO_MOVE] = AddNewParagraphToDoc(HostRtb.Document, STYLE_SECOND_PROMPT, "\n   " + Properties.Resources.ReplacingEngineMove
                    + "\n   " + Properties.Resources.MakeMoveForEngine);
                _dictParas[ParaType.PROMPT_TO_MOVE].Foreground = ChessForgeColors.GetHintForeground(CommentBox.HintType.ERROR);
            }
            else
            {
                _mainWin.BoardCommentBox.GameMoveMade(nd, false);
            }
        }

        /// <summary>
        /// Updates the chessboard with the current position
        /// and refreshes the game paragraphs.
        /// </summary>
        /// <param name="nd"></param>
        private void UpdateViewForGameRestart(TreeNode nd)
        {
            _mainWin.DisplayPosition(nd);
            RebuildEngineGamePara(nd);

            RemoveCheckmatePara();
            RemoveStalematePara();
            RemoveInsufficientMaterialPara();
        }

        /// <summary>
        /// Find the last user move that was in the workbook
        /// and restart the training from there.
        /// </summary>
        private void RestartFromLastUserWorkbookMove()
        {
            // make sure there is no game in progress
            if (_lastUserMoveNodeId <= 0)
            {
                return;
            }

            try
            {
                TreeNode nd = _mainWin.ActiveVariationTree.GetNodeFromNodeId(_lastUserMoveNodeId);
                if (nd != null)
                {
                    // we expect this to be marked as "Training Move"
                    if (nd.IsNewTrainingMove)
                    {
                        // get previous move
                        if (nd.Parent != null)
                        {
                            RollbackToUserMove(nd.Parent);
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Restarts training from the clicked mode.
        /// </summary>
        /// <param name="context"></param>
        private void RestartFromClickedMove(MoveContext context)
        {
            if (context == MoveContext.LINE || _moveContext == MoveContext.WORKBOOK_COMMENT)
            {
                RollbackTraining();
            }
            else if (context == MoveContext.GAME)
            {
                RestartGameAfter(null, null);
            }
        }

        /// <summary>
        /// Based on the name of the run, determines the
        /// last clicked node.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="prefix"></param>
        /// <param name="e"></param>
        private void DetectLastClickedNode(Run r, string prefix, MouseButtonEventArgs e)
        {
            int nodeId = GetNodeIdFromObjectName(r.Name, prefix);
            TreeNode nd = _mainWin.ActiveVariationTree.GetNodeFromNodeId(nodeId);
            SetLastClicked(nd, r, e);
        }

        /// <summary>
        /// Places the floating board over the move.
        /// If the move is close to the boundary, place the board such that it is fully visible.
        /// Also, it must not covert the point where the mouse is positioned so that the user can click it.
        /// </summary>
        /// <param name="nodeId"></param>
        private void ShowFloatingBoard(int nodeId, Point pt, bool isStemMove)
        {
            _mainWin.TrainingFloatingBoard.FlipBoard(_mainWin.MainChessBoard.IsFlipped);
            _mainWin.TrainingFloatingBoard.DisplayPosition(_mainWin.ActiveVariationTree.GetNodeFromNodeId(nodeId), false);

            double xCoord = pt.X + 10;
            if (_mainWin.UiRtbTrainingProgress.ActualWidth < xCoord + 170)
            {
                xCoord = _mainWin.UiRtbTrainingProgress.ActualWidth - 170;
            }

            int yOffset = isStemMove ? 25 : -165;
            double yCoord = pt.Y + yOffset;
            if (yCoord < 0)
            {
                // show under the move
                yCoord = pt.Y + 10;
            }

            _mainWin.UiVbTrainingFloatingBoard.Margin = new Thickness(xCoord, yCoord, 0, 0);
            if (_nodeIdSuppressFloatingBoard != nodeId)
            {
                _mainWin.ShowTrainingFloatingBoard(true);
                _nodeIdSuppressFloatingBoard = -1;
            }
        }

        /// <summary>
        /// Sets the values of "_lastClicked" properties.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="r"></param>
        /// <param name="e"></param>
        private void SetLastClicked(TreeNode nd, Run r, MouseButtonEventArgs e)
        {
            _lastClickedNode = nd;
            _lastClickedRun = r;
            _lastClickedPoint = e.GetPosition(_mainWin.UiRtbTrainingProgress);
        }

        /// <summary>
        /// This method will be called when the program made a move 
        /// and we want to update the Training view.
        /// </summary>
        public void WorkbookMoveMade()
        {
            _mainWin.UiRtbTrainingProgress.Dispatcher.Invoke(() =>
            {
                BuildSecondPromptParagraph();
            });
        }

        /// <summary>
        /// Given a run name and a prefix, returns the NodeId
        /// associated with the run.
        /// The run name is expected to be in the form prefix + NodeId.
        /// </summary>
        /// <param name="runName"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        int GetNodeIdFromObjectName(string runName, string prefix)
        {
            if (string.IsNullOrEmpty(runName) || runName.Length <= prefix.Length)
                return -1;

            string sId = runName.Substring(prefix.Length);
            int iId = int.Parse(sId);
            return iId;
        }

        /// <summary>
        /// It the Paragraph of the passed type is not null,
        /// the reference to it is returned back.
        /// If it is null, then the first non-null paragraph
        /// above it in the document's hierarchy is returned.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        private Paragraph NonNullParaAtOrBefore(ParaType pt)
        {
            bool ptFound = false;
            Paragraph para = null;
            for (int i = _lstParaOrder.Count - 1; i >= 0; i--)
            {
                if (_lstParaOrder[i] == pt)
                {
                    ptFound = true;
                }

                if (ptFound)
                {
                    para = _dictParas[_lstParaOrder[i]];
                    if (para != null)
                        break;
                }
            }

            return para;
        }

        /// <summary>
        /// Removes the introductory paragraphs
        /// that are shown only at the start of the session.
        /// </summary>
        private void RemoveIntroParas()
        {
            HostRtb.Document.Blocks.Remove(_dictParas[ParaType.PROMPT_TO_MOVE]);
            _dictParas[ParaType.PROMPT_TO_MOVE] = null;
        }
    }
}

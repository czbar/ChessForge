using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ChessPosition;
using GameTree;
using System.Windows.Markup.Localizer;

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
    ///   The comment paragraph will in particular include wrokbook moves other than the move
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
            WORKBOOK_MOVES
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
            [ParaType.WORKBOOK_MOVES] = null
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
            ParaType.WORKBOOK_MOVES
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
        /// Id of the node over which to temporarily suspend floating board.
        /// </summary>
        private int _nodeIdSuppressFloatingBoard = -1;

        // Brush color for user moves
        private Brush _userBrush = Brushes.Green;

        // Brush color for workbook moves
        private Brush _workbookBrush = Brushes.Blue;

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
        private readonly string _run_wb_alternatives_ = "wb_alternatives_";
        private readonly string _run_wb_comment_ = "wb_comment_";

        private readonly string _par_line_moves_ = "par_line_moves_";
        private readonly string _par_game_moves_ = "par_game_moves_";

        private readonly string _par_checkmate_ = "par_checkmate_";
        private readonly string _par_stalemate_ = "par_stalemate_";

        // Application's Main Window
        private MainWindow _mainWin;

        /// <summary>
        /// Creates an instance of this class and sets reference 
        /// to the FlowDocument managed by the object.
        /// </summary>
        /// <param name="doc"></param>
        public TrainingView(FlowDocument doc, MainWindow mainWin) : base(doc)
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
        private static readonly string STYLE_MOVES_MAIN = "moves_main";
        private static readonly string STYLE_COACH_NOTES = "coach_notes";
        private static readonly string STYLE_ENGINE_EVAL = "engine_eval";
        private static readonly string STYLE_ENGINE_GAME = "engine_game";

        private static readonly string STYLE_CHECKMATE = "mate";
        private static readonly string STYLE_STALEMATE = "stalemate";

        private static readonly string STYLE_DEFAULT = "default";

        /// <summary>
        /// Training Side for this session.
        /// Note it does not have to be the LearningMode.TrainingSide
        /// </summary>
        private PieceColor _trainingSide;

        /// <summary>
        /// The word to use in messaging the user; Workbook or Game, depending on
        /// when the training started from.
        /// </summary>
        private static string TRAINING_SOURCE = "Workbook";

        /// <summary>
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        internal Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            [STYLE_INTRO] = new RichTextPara(0, 0, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(0, 0, 0)), TextAlignment.Left),
            [STYLE_FIRST_PROMPT] = new RichTextPara(10, 0, 16, FontWeights.Bold, Brushes.Green, TextAlignment.Left, Brushes.Green),
            [STYLE_SECOND_PROMPT] = new RichTextPara(10, 0, 14, FontWeights.Bold, Brushes.Green, TextAlignment.Left, Brushes.Green),
            [STYLE_STEM_LINE] = new RichTextPara(0, 10, 14, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Left),
            [STYLE_DEFAULT] = new RichTextPara(10, 5, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(128, 98, 63)), TextAlignment.Left),

            [STYLE_MOVES_MAIN] = new RichTextPara(10, 5, 16, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Left),
            [STYLE_COACH_NOTES] = new RichTextPara(50, 5, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Left),
            [STYLE_ENGINE_EVAL] = new RichTextPara(80, 0, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Left),
            [STYLE_ENGINE_GAME] = new RichTextPara(50, 0, 16, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Left),

            [STYLE_CHECKMATE] = new RichTextPara(50, 0, 16, FontWeights.Bold, Brushes.Navy, TextAlignment.Left, Brushes.Navy),
            [STYLE_STALEMATE] = new RichTextPara(50, 0, 16, FontWeights.Bold, Brushes.Navy, TextAlignment.Left, Brushes.Navy),
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
        }

        /// <summary>
        /// Initializes the state of the view.
        /// Sets the starting node of the training session
        /// and shows the intro text.
        /// </summary>
        /// <param name="node"></param>
        public void Initialize(TreeNode node, GameData.ContentType contentType)
        {
            if (contentType == GameData.ContentType.MODEL_GAME)
            {
                TRAINING_SOURCE = "Game";
            }
            else
            {
                TRAINING_SOURCE = "Workbook";
            }

            _currentEngineGameMoveCount = 0;
            _trainingSide = node.ColorToMove;

            TrainingSession.ResetTrainingLine(node);
            Document.Blocks.Clear();
            InitParaDictionary();

            BuildIntroText(node);
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
        /// Rolls back the training to the ply
        /// that we want to replace with the last clicked node.
        /// </summary>
        public void RollbackToWorkbookMove()
        {
            _currentEngineGameMoveCount = 0;

            TrainingSession.RollbackTrainingLine(_lastClickedNode);
            EngineGame.RollbackGame(_lastClickedNode);

            TrainingSession.ChangeCurrentState(TrainingSession.State.USER_MOVE_COMPLETED);

            LearningMode.ChangeCurrentMode(LearningMode.Mode.TRAINING);
            AppState.SetupGuiForCurrentStates();

            _mainWin.BoardCommentBox.GameMoveMade(_lastClickedNode, true);

            RemoveParagraphsFromMove(_lastClickedNode);
            ReportLastMoveVsWorkbook();
            if (TrainingSession.IsContinuousEvaluation)
            {
                RequestMoveEvaluation(_mainWin.ActiveVariationTreeId, true);
            }
        }

        /// <summary>
        /// The user requested rollback to one of their moves.
        /// </summary>
        public void RollbackToUserMove()
        {
            _currentEngineGameMoveCount = 0;

            TrainingSession.RollbackTrainingLine(_lastClickedNode);
            EngineGame.RollbackGame(_lastClickedNode);

            TrainingSession.ChangeCurrentState(TrainingSession.State.AWAITING_USER_TRAINING_MOVE);

            LearningMode.ChangeCurrentMode(LearningMode.Mode.TRAINING);
            AppState.SetupGuiForCurrentStates();

            _mainWin.BoardCommentBox.GameMoveMade(_lastClickedNode, false);

            RemoveParagraphsFromMove(_lastClickedNode);
            BuildSecondPromptParagraph();
            _mainWin.DisplayPosition(_lastClickedNode);
            if (TrainingSession.IsContinuousEvaluation)
            {
                RequestMoveEvaluation(_mainWin.ActiveVariationTreeId, true);
            }
        }

        /// <summary>
        /// Removes all nodes marked "IsNewTrainingNode" unless they exist in the EngineGame.Line
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
        /// Removes the paragraph for the ply
        /// with the move number and color-to-move same
        /// as in the passed Node.
        /// </summary>
        /// <param name="move"></param>
        private void RemoveParagraphsFromMove(TreeNode move)
        {
            List<Block> parasToRemove = new List<Block>();

            bool found = false;
            foreach (var block in Document.Blocks)
            {
                if (found)
                {
                    parasToRemove.Add(block);
                }
                else if (block is Paragraph)
                {
                    int nodeId = TextUtils.GetIdFromPrefixedString(((Paragraph)block).Name);
                    TreeNode nd = _mainWin.ActiveVariationTree.GetNodeFromNodeId(nodeId);
                    if (nd != null && nd.MoveNumber == move.MoveNumber && nd.ColorToMove == move.ColorToMove)
                    {
                        found = true;
                        parasToRemove.Add(block);
                    }
                }
            }

            foreach (var block in parasToRemove)
            {
                Document.Blocks.Remove(block);
            }

            // if anything was removed then the position was indeed rolled back
            if (parasToRemove.Count > 0)
            {
                // remove a possible checkmate/stalemate paragraph if exits
                RemoveCheckmatePara();
                RemoveStalematePara();
            }
        }

        /// <summary>
        /// Removes a checkmate para if exists.
        /// </summary>
        private void RemoveCheckmatePara()
        {
            Paragraph paraToRemove = null;

            foreach (var block in Document.Blocks)
            {
                if (block is Paragraph)
                {
                    if (((Paragraph)block).Name == _par_checkmate_)
                    {
                        paraToRemove = block as Paragraph;
                        break;
                    }
                }
            }

            if (paraToRemove != null)
            {
                Document.Blocks.Remove(paraToRemove);
            }
        }

        /// <summary>
        /// Removes a stalemate para if exists.
        /// </summary>
        private void RemoveStalematePara()
        {
            Paragraph paraToRemove = null;

            foreach (var block in Document.Blocks)
            {
                if (block is Paragraph)
                {
                    if (((Paragraph)block).Name == _par_stalemate_)
                    {
                        paraToRemove = block as Paragraph;
                        break;
                    }
                }
            }

            if (paraToRemove != null)
            {
                Document.Blocks.Remove(paraToRemove);
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
        /// Adds a node/ply to the Engine Game paragraph.
        /// </summary>
        /// <param name="nd"></param>
        private void AddMoveToEngineGamePara(TreeNode nd, bool isUserMove)
        {
            if (_paraCurrentEngineGame == null)
            {
                // should never be null here so this is just defensive
                _paraCurrentEngineGame = AddNewParagraphToDoc(STYLE_ENGINE_GAME, "");
            }

            string text = "";
            if (_currentEngineGameMoveCount == 0)
            {
                _paraCurrentEngineGame.Inlines.Clear();
                _paraCurrentEngineGame.Inlines.Add(new Run("\nA training game against the engine is in progress...\n"));
                text = "          " + MoveUtils.BuildSingleMoveText(nd, true) + " ";
            }
            else
            {
                text = MoveUtils.BuildSingleMoveText(nd, false) + " ";
            }

            Run gm = CreateButtonRun(text, _run_engine_game_move_ + nd.NodeId.ToString(), Brushes.Brown);
            _paraCurrentEngineGame.Inlines.Add(gm);

            if (nd.Position.IsCheckmate)
            {
                BuildCheckmateParagraph(nd, isUserMove);
            }
            else if (nd.Position.IsStalemate)
            {
                BuildStalemateParagraph(nd);
            }

            _currentEngineGameMoveCount++;

        }

        /// <summary>
        /// Rebuilds the Engine Game paragraph up to 
        /// a specified Node.
        /// </summary>
        /// <param name="toNode"></param>
        private void RebuildEngineGamePara(TreeNode toNode)
        {
            if (_paraCurrentEngineGame == null)
            {
                _paraCurrentEngineGame = AddNewParagraphToDoc(STYLE_ENGINE_GAME, "");
            }
            TreeNode nd = _engineGameRootNode.Children[0];
            _currentEngineGameMoveCount = 0;

            string text = "";
            _paraCurrentEngineGame.Inlines.Clear();
            _paraCurrentEngineGame.Inlines.Add(new Run("\nA training game against the engine is in progress...\n"));
            text = "          " + MoveUtils.BuildSingleMoveText(nd, true) + " ";
            Run r_root = CreateButtonRun(text, _run_engine_game_move_ + nd.NodeId.ToString(), Brushes.Brown);
            _paraCurrentEngineGame.Inlines.Add(r_root);

            _currentEngineGameMoveCount++;

            while (nd.Children.Count > 0)
            {
                nd = nd.Children[0];
                _currentEngineGameMoveCount++;
                text = MoveUtils.BuildSingleMoveText(nd, false) + " ";
                Run gm = CreateButtonRun(text, _run_engine_game_move_ + nd.NodeId.ToString(), Brushes.Brown);
                _paraCurrentEngineGame.Inlines.Add(gm);
            };
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
                DebugUtils.ShowDebugMessage("Null node in Training ShowEvaluationResult");
                return;
            }

            AppLog.Message("TrainingView:ShowEvaluationResult() Evaluation Mode:" + EvaluationManager.CurrentMode.ToString()
                + ", node=" + nd.LastMoveAlgebraicNotation);

            if (!TrainingSession.IsContinuousEvaluation)
            {
                return;
            }

            _mainWin.Dispatcher.Invoke(() =>
            {
                Run runEvaluated = GetRunForNodeId(nd.NodeId);
                if (runEvaluated != null)
                {
                    Paragraph para = runEvaluated.Parent as Paragraph;
                    if (para != null)
                    {
                        string runEvalName = _run_move_eval_ + nd.NodeId.ToString();

                        // Remove previous evaluation if exists
                        var r_prev = para.Inlines.FirstOrDefault(x => x.Name == runEvalName);
                        para.Inlines.Remove(r_prev);

                        if (!string.IsNullOrEmpty(nd.EngineEvaluation))
                        {
                            Run r_eval = CreateEvaluationRun(nd.EngineEvaluation, runEvalName, nd);
                            para.Inlines.InsertAfter(runEvaluated, r_eval);
                        }

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
                    }
                }
            });

            AppState.SetupGuiForCurrentStates();
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
            r_eval.Foreground = Brushes.Black;

            return r_eval;
        }

        /// <summary>
        /// Builds a paragraph containing just one move with NAGs and Comments/Commands if any. 
        /// </summary>
        private void BuildMoveParagraph(TreeNode nd, bool userMove)
        {
            string paraName = _par_line_moves_ + nd.NodeId.ToString();
            string runName = _run_line_move_ + nd.NodeId.ToString();

            // check if already exists. Due to timing issues it may be called multiple times
            if (FindParagraphByName(paraName, false) == null)
            {
                Paragraph para = AddNewParagraphToDoc(STYLE_MOVES_MAIN, "");
                para.Name = paraName;

                Run r_prefix = new Run();
                if (userMove)
                {
                    //r_prefix.Text = Constants.CharRightArrow + " ";
                    r_prefix.FontWeight = FontWeights.Normal;
                    para.Inlines.Add(r_prefix);
                }
                else
                {
                    r_prefix.Text = TRAINING_SOURCE + " response" + ": ";
                    r_prefix.FontWeight = FontWeights.Normal;
                    para.Inlines.Add(r_prefix);
                }

                Run r = CreateButtonRun(MoveUtils.BuildSingleMoveText(nd, true) + " ", runName, Brushes.Black);
                para.Inlines.Add(r);

                if (!userMove)
                {
                    InsertCommentIntoWorkbookMovePara(para, nd);
                }

                _mainWin.UiRtbTrainingProgress.ScrollToEnd();
            }
        }

        /// <summary>
        /// Builds a paragraph reporting checkmate
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="userMove"></param>
        private void BuildCheckmateParagraph(TreeNode nd, bool userMove)
        {
            string paraName = _par_checkmate_;

            Paragraph para = AddNewParagraphToDoc(STYLE_CHECKMATE, "");
            para.Name = paraName;

            Run r_prefix = new Run();
            if (userMove)
            {
                r_prefix.Text = "\nThe training game has ended. You have delivered a checkmate!";
            }
            else
            {
                r_prefix.Text = "\nThe training game has ended. You have been checkmated by the engine.";
            }

            para.Inlines.Add(r_prefix);
            _mainWin.UiRtbTrainingProgress.ScrollToEnd();
        }

        /// <summary>
        /// Builds a paragraph reporting stalemate
        /// </summary>
        /// <param name="nd"></param>
        private void BuildStalemateParagraph(TreeNode nd)
        {
            string paraName = _par_stalemate_;

            Paragraph para = AddNewParagraphToDoc(STYLE_CHECKMATE, "");
            para.Name = paraName;

            Run r_prefix = new Run();
            r_prefix.Text = "\nThis is a stalemate. The game has been drawn.";

            para.Inlines.Add(r_prefix);
            _mainWin.UiRtbTrainingProgress.ScrollToEnd();
        }

        /// <summary>
        /// Builds the "stem line" paragraphs that is always visible at the top
        /// of the view.
        /// </summary>
        /// <param name="node"></param>
        private void CreateStemParagraph(TreeNode node)
        {
            _dictParas[ParaType.STEM] = AddNewParagraphToDoc(STYLE_STEM_LINE, null);

            string sPrefix;
            if (node.NodeId != 0)
            {
                sPrefix = "\nThis training session with your virtual Coach begins in the starting position: \n";
            }
            else
            {
                sPrefix = "\nStarting a training session with your virtual Coach. \n";
            }
            Run r_prefix = new Run(sPrefix);

            r_prefix.FontWeight = FontWeights.Normal;
            _dictParas[ParaType.STEM].Inlines.Add(r_prefix);

            if (node.NodeId != 0)
            {
                InsertPrefixRuns(_dictParas[ParaType.STEM], node);
            }
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
                Run r = CreateButtonRun(MoveUtils.BuildSingleMoveText(nd, nd.Parent.NodeId == 0) + " ", _run_stem_move_ + nd.NodeId.ToString(), Brushes.Black);
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
        /// Initial prompt to advise the user make their move.
        /// This paragraph is removed later on to reduce clutter.
        /// </summary>
        private void BuildInstructionsText()
        {
            StringBuilder sbInstruction = new StringBuilder();
            sbInstruction.Append("You will be making moves for");
            sbInstruction.Append(TrainingSession.StartPosition.ColorToMove == PieceColor.White ? " White " : " Black ");
            sbInstruction.Append("and the program will respond for");
            sbInstruction.Append(TrainingSession.StartPosition.ColorToMove == PieceColor.White ? " Black." : " White.");
            sbInstruction.AppendLine();
            //            sbInstruction.Append("The Coach will comment on your every move based on the content of the " + TRAINING_SOURCE + ".\n");
            sbInstruction.Append("\nRemember that you can:\n");
            sbInstruction.Append("- double click on an alternative move highlighted in the comment to play it instead of your original choice,\n");
            sbInstruction.Append("- right click on any move to invoke a context menu where, among other options, you can request engine evaluation of the move.\n");

            _dictParas[ParaType.INSTRUCTIONS] = AddNewParagraphToDoc(STYLE_INTRO, sbInstruction.ToString());

            _dictParas[ParaType.PROMPT_TO_MOVE] = AddNewParagraphToDoc(STYLE_FIRST_PROMPT, "To begin, make your first move on the chessboard.");
        }

        /// <summary>
        /// Builds the paragraph prompting the user to make a move
        /// after the program responded.
        /// </summary>
        private void BuildSecondPromptParagraph()
        {
            TreeNode nd = EngineGame.GetLastGameNode();

            bool isMateCf = PositionUtils.IsCheckmate(nd.Position);

            bool isStalemate = false;
            if (!isMateCf)
            {
                isStalemate = PositionUtils.IsStalemate(nd.Position);
            }

            if (isMateCf)
            {
                BuildCheckmateParagraph(nd, false);
                Document.Blocks.Remove(_dictParas[ParaType.PROMPT_TO_MOVE]);
                _mainWin.BoardCommentBox.ReportCheckmate(false);
            }
            else if (isStalemate)
            {
                BuildStalemateParagraph(nd);
                Document.Blocks.Remove(_dictParas[ParaType.PROMPT_TO_MOVE]);
                _mainWin.BoardCommentBox.ReportStalemate();
            }
            else
            {
                BuildMoveParagraph(nd, false);

                Document.Blocks.Remove(_dictParas[ParaType.PROMPT_TO_MOVE]);
                _dictParas[ParaType.PROMPT_TO_MOVE] = AddNewParagraphToDoc(STYLE_SECOND_PROMPT, "\n   Your turn...");

                _mainWin.BoardCommentBox.GameMoveMade(nd, false);
            }
            _mainWin.UiRtbTrainingProgress.ScrollToEnd();
            if (TrainingSession.IsContinuousEvaluation)
            {
                RequestMoveEvaluation(_mainWin.ActiveVariationTreeId, true);
            }
        }

        /// <summary>
        /// Inserts a comment run into the user move paragraph.
        /// </summary>
        /// <param name="isWorkbookMove"></param>
        private void InsertCommentIntoUserMovePara(bool isWorkbookMove, TreeNode userMove)
        {
            Paragraph para = FindUserMoveParagraph(userMove);
            if (para != null)
            {
                string wbAlignmentRunName = _run_user_wb_alignment_ + userMove.NodeId.ToString();

                // do not build if already built
                if (FindRunByName(wbAlignmentRunName, para) == null)
                {
                    para.FontWeight = FontWeights.Normal;

                    InsertCheckmarkRun(para, isWorkbookMove);
                    InsertWorkbookCommentRun(para, userMove);

                    Run wbAlignmentNoteRun = new Run();
                    wbAlignmentNoteRun.Name = wbAlignmentRunName;
                    wbAlignmentNoteRun.FontSize = para.FontSize - 1;

                    StringBuilder sbAlignmentNote = new StringBuilder();
                    if (_otherMovesInWorkbook.Count == 0)
                    {
                        if (!isWorkbookMove)
                        {
                            sbAlignmentNote.Append("The Workbook line has ended. ");
                        }
                        wbAlignmentNoteRun.Text = sbAlignmentNote.ToString();
                        para.Inlines.Add(wbAlignmentNoteRun);
                    }
                    else
                    {
                        if (!isWorkbookMove)
                        {
                            sbAlignmentNote.Append("This is not in the " + TRAINING_SOURCE + ". ");
                        }

                        Run rAlternativeNote = null;
                        if (!isWorkbookMove)
                        {
                            if (_otherMovesInWorkbook.Count == 1)
                            {
                                sbAlignmentNote.Append("The only " + TRAINING_SOURCE + " move is ");
                            }
                            else
                            {
                                sbAlignmentNote.Append("The " + TRAINING_SOURCE + " moves are ");
                            }
                        }
                        else
                        {
                            rAlternativeNote = new Run();
                            if (_otherMovesInWorkbook.Count == 1)
                            {
                                rAlternativeNote.Text = "  Alternative is ";
                            }
                            else
                            {
                                rAlternativeNote.Text = "  Alternatives are ";
                            }
                            rAlternativeNote.FontSize = para.FontSize - 1;
                            rAlternativeNote.Foreground = _userBrush;
                        }

                        wbAlignmentNoteRun.Text = sbAlignmentNote.ToString();
                        para.Inlines.Add(wbAlignmentNoteRun);

                        if (rAlternativeNote != null)
                        {
                            para.Inlines.Add(rAlternativeNote);
                        }

                        BuildOtherWorkbookMovesRun(para, _otherMovesInWorkbook, true);
                    }
                    _mainWin.UiRtbTrainingProgress.ScrollToEnd();
                }
            }
        }

        /// <summary>
        /// Inserts a comment run into the Workbook move paragraph.
        /// It will include any comment found in the workbook and clickable
        /// other workbook moves
        /// </summary>
        /// <param name="moveNode"></param>
        private void InsertCommentIntoWorkbookMovePara(Paragraph para, TreeNode moveNode)
        {
            string wbAlternativesRunName = _run_wb_alternatives_ + moveNode.NodeId.ToString();
            string wbCommentRunName = _run_wb_comment_ + moveNode.NodeId.ToString();

            // do not build if already built
            if (FindRunByName(wbAlternativesRunName, para) != null)
            {
                return;
            }

            InsertWorkbookCommentRun(para, moveNode);

            para.FontWeight = FontWeights.Normal;

            Run wbAlternativesRun = new Run();
            wbAlternativesRun.FontSize = para.FontSize - 1;
            wbAlternativesRun.Name = wbAlternativesRunName;
            wbAlternativesRun.Foreground = _workbookBrush;

            StringBuilder sbWbAlternatives = new StringBuilder();

            List<TreeNode> lstAlternatives = GetWorkbookSiblings(moveNode);

            if (lstAlternatives.Count == 0)
            {
                //wbAlternativesRun.Text = sbWbAlternatives.ToString();
                //para.Inlines.Add(wbAlternativesRun);
            }
            else
            {
                if (lstAlternatives.Count == 1)
                {
                    sbWbAlternatives.Append("  Alternative: ");
                }
                else
                {
                    sbWbAlternatives.Append("  Alternatives: ");
                }

                wbAlternativesRun.Text += sbWbAlternatives;
                para.Inlines.Add(wbAlternativesRun);

                BuildOtherWorkbookMovesRun(para, lstAlternatives, false);
            }
            _mainWin.UiRtbTrainingProgress.ScrollToEnd();
        }

        /// <summary>
        /// Builds and inserts a run with a comment from the Workbook if present.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="moveNode"></param>
        private void InsertWorkbookCommentRun(Paragraph para, TreeNode moveNode)
        {
            if (string.IsNullOrWhiteSpace(moveNode.Comment))
            {
                return;
            }

            Run r = new Run("[" + moveNode.Comment + "] ");
            r.FontSize = para.FontSize - 1;
            r.FontStyle = FontStyles.Italic;
            para.Inlines.Add(r);
        }

        /// <summary>
        /// Inserts check mark indicating whether the move was in the Workbook or not.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="isWorkbookMove"></param>
        private void InsertCheckmarkRun(Paragraph para, bool isWorkbookMove)
        {
            Run r = new Run((isWorkbookMove ? Constants.CharCheckMark : Constants.CharCrossMark) + " ");
            para.Inlines.Add(r);
        }

        /// <summary>
        /// Returns a list of non-new-training siblings of the passed node.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private List<TreeNode> GetWorkbookSiblings(TreeNode nd)
        {
            List<TreeNode> lstNodes = new List<TreeNode>();
            if (nd != null && nd.Parent != null)
            {
                foreach (TreeNode child in nd.Parent.Children)
                {
                    // we cannot use ArePositionsIdentical() because nd only has static position
                    if (child.LastMoveEngineNotation != nd.LastMoveEngineNotation && !child.IsNewTrainingMove)
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
            return FindParagraphByName(paraName, false);
        }


        /// <summary>
        /// Adds plies from _otherMovesInWorkbook to the
        /// passed paragraph.
        /// </summary>
        /// <param name="para"></param>
        private void BuildOtherWorkbookMovesRun(Paragraph para, List<TreeNode> moves, bool isUserMove)
        {
            foreach (TreeNode nd in moves)
            {
                Brush brush = isUserMove ? _userBrush : _workbookBrush;
                para.Inlines.Add(CreateButtonRun(MoveUtils.BuildSingleMoveText(nd, true), _run_wb_move_ + nd.NodeId.ToString(), brush));
                Run r_semi = new Run("; ");
                para.Inlines.Add(r_semi);
            }
        }

        /// <summary>
        /// Creates a highlighted, clickable text that here we refer to
        /// as a "button".
        /// </summary>
        /// <param name="text"></param>
        /// <param name="runName"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        private Run CreateButtonRun(string text, string runName, Brush color)
        {
            Run r = new Run(text);
            r.Name = runName;

            r.Foreground = color;

            r.FontWeight = FontWeights.Bold;
            r.MouseDown += EventRunClicked;
            r.MouseMove += EventRunMoveOver;
            r.Cursor = Cursors.Hand;

            return r;
        }

        /// <summary>
        /// Builds a move text for use in the context menu 
        /// </summary>
        /// <param name="midTxt"></param>
        /// <returns></returns>
        private string BuildMoveTextForMenu(TreeNode nd, out string midTxt)
        {
            midTxt = " ";
            if (_moveContext == MoveContext.GAME || _moveContext == MoveContext.LINE)
            {
                if (nd.ColorToMove != _trainingSide)
                {
                    midTxt = " Your ";
                }
                else
                {
                    midTxt = " Engine\'s ";
                }
            }

            return MoveUtils.BuildSingleMoveText(nd, true);
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
                string midTxt;
                string moveTxt = BuildMoveTextForMenu(_lastClickedNode, out midTxt);

                ContextMenu cm = _mainWin._cmTrainingView;
                foreach (object o in cm.Items)
                {
                    if (o is MenuItem)
                    {
                        MenuItem mi = o as MenuItem;
                        switch (mi.Name)
                        {
                            case "_mnTrainEvalMove":
                                mi.Header = "Evaluate" + midTxt + "Move " + moveTxt;
                                mi.Visibility = Visibility.Visible;
                                break;
                            case "_mnTrainEvalLine":
                                mi.Header = "Evaluate Line";
                                mi.Visibility = _moveContext == MoveContext.WORKBOOK_COMMENT ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "_mnTrainRestartGame":
                                mi.Header = "Restart game from " + midTxt + "Move " + moveTxt;
                                mi.Visibility = _moveContext == MoveContext.GAME ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "_mnRollBackTraining":
                                mi.Header = "Restart Training from " + moveTxt;
                                mi.Visibility = (_moveContext == MoveContext.LINE || _moveContext == MoveContext.WORKBOOK_COMMENT) ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "_mnTrainSwitchToWorkbook":
                                mi.Header = "Play " + moveTxt + " instead of Your Move";
                                mi.Visibility = _moveContext == MoveContext.WORKBOOK_COMMENT ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "_mnTrainRestartTraining":
                                mi.Visibility = Visibility.Visible;
                                break;
                            case "_mnTrainExitTraining":
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

            foreach (var block in Document.Blocks)
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
                if (_lastClickedNode.ColorToMove != _trainingSide)
                {
                    EngineGame.RestartAtUserMove(nd);
                    _mainWin.BoardCommentBox.GameMoveMade(nd, true);
                }
                else
                {
                    EngineGame.RestartAtEngineMove(nd);
                    _mainWin.BoardCommentBox.GameMoveMade(nd, false);
                }
                _mainWin.DisplayPosition(nd);
                RebuildEngineGamePara(nd);

                RemoveCheckmatePara();
                RemoveStalematePara();
            }
        }

        /// <summary>
        /// Based on the name of the clicked run, performs an action.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventRunClicked(object sender, MouseButtonEventArgs e)
        {
            Run r = (Run)e.Source;
            if (string.IsNullOrEmpty(r.Name))
            {
                return;
            }

            //on Right Button we invoke the the Context Menu, on Left we don't but will continue with CONTINUOUS evaluation 
            if (e.ChangedButton == MouseButton.Right || e.ChangedButton == MouseButton.Left)
            {
                bool found = false;
                if (r.Name.StartsWith(_run_line_move_))
                {
                    // a move in the main training line was clicked 
                    DetectLastClickedNode(r, _run_line_move_, e);
                    _moveContext = MoveContext.LINE;
                    found = true;
                }
                else if (r.Name.StartsWith(_run_wb_move_))
                {
                    // a workbook move in the comment was clicked 
                    DetectLastClickedNode(r, _run_wb_move_, e);
                    _moveContext = MoveContext.WORKBOOK_COMMENT;
                    found = true;
                }
                else if (r.Name.StartsWith(_run_engine_game_move_))
                {
                    // a move from a game against the engine was clicked,
                    // we take the game back to that move.
                    // If it is an engine move, the user will be required to respond,
                    // otherwise it will be engine's turn.
                    DetectLastClickedNode(r, _run_engine_game_move_, e);
                    _moveContext = MoveContext.GAME;
                    found = true;
                }

                if (found)
                {
                    if (e.ChangedButton == MouseButton.Right)
                    {
                        if (EvaluationManager.CurrentMode != EvaluationManager.Mode.IDLE)
                        {
                            EvaluationManager.ChangeCurrentMode(EvaluationManager.Mode.IDLE);
                        }
                        _mainWin.Timers.Start(AppTimers.TimerId.SHOW_TRAINING_PROGRESS_POPUP_MENU);
                    }
                    else if (e.ChangedButton == MouseButton.Left)
                    {
                        _mainWin.ShowTrainingFloatingBoard(false);
                        if (_lastClickedNode != null)
                        {
                            // flip the visibility for the floating board
                            if (_nodeIdSuppressFloatingBoard == _lastClickedNode.NodeId)
                            {
                                _nodeIdSuppressFloatingBoard = -1;
                            }
                            else
                            {
                                _nodeIdSuppressFloatingBoard = _lastClickedNode.NodeId;
                            }

                            // if not the last move, ask if to restart
                            if (EngineGame.GetLastGameNode() != _lastClickedNode)
                            {
                                string moveTxt = BuildMoveTextForMenu(_lastClickedNode, out _);
                                if (MessageBox.Show("Restart from " + moveTxt + "?", "Chess Forge Training", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                                {
                                    RestartFromClickedMove(_moveContext);
                                }
                            }
                        }
                    }
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
                if (_lastClickedNode.ColorToMove == _trainingSide)
                {
                    RollbackToUserMove();
                }
                else
                {
                    // A user move was clicked so rollback to the previous Workbook move
                    RollbackToWorkbookMove();
                }
            }

            if (TrainingSession.IsContinuousEvaluation)
            {
                RequestMoveEvaluation(_mainWin.ActiveVariationTreeId, true);
            }
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
        /// Handles a mouse move over a Run.
        /// Shows the floating chessboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventRunMoveOver(object sender, MouseEventArgs e)
        {
            if (_blockFloatingBoard)
                return;

            // check if we are over a move run
            Run r = (Run)e.Source;
            if (string.IsNullOrEmpty(r.Name))
            {
                return;
            }

            int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
            if (nodeId >= 0)
            {
                Point pt = e.GetPosition(_mainWin.UiRtbTrainingProgress);
                _mainWin.TrainingFloatingBoard.FlipBoard(_mainWin.MainChessBoard.IsFlipped);
                _mainWin.TrainingFloatingBoard.DisplayPosition(_mainWin.ActiveVariationTree.GetNodeFromNodeId(nodeId), false);
                int yOffset = r.Name.StartsWith(_run_stem_move_) ? 25 : -165;
                _mainWin.UiVbTrainingFloatingBoard.Margin = new Thickness(pt.X, pt.Y + yOffset, 0, 0);
                if (_nodeIdSuppressFloatingBoard != nodeId)
                {
                    _mainWin.ShowTrainingFloatingBoard(true);
                    _nodeIdSuppressFloatingBoard = -1;
                }
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
            Document.Blocks.Remove(_dictParas[ParaType.PROMPT_TO_MOVE]);
            _dictParas[ParaType.PROMPT_TO_MOVE] = null;
        }
    }
}

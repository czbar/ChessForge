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
    public class TrainingView : RichTextBuilder
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

        private readonly string _par_line_moves_ = "par_line_moves_";
        private readonly string _par_coach_moves_ = "par_coach_moves_";
        private readonly string _par_game_moves_ = "par_game_moves_";

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
        public void Initialize(TreeNode node)
        {
            _currentEngineGameMoveCount = 0;
            TrainingState.ResetTrainingLine(node);
            Document.Blocks.Clear();
            InitParaDictionary();

            BuildIntroText(node);
        }

        /// <summary>
        /// This method is invoked when user makes their move.
        /// 
        /// It gets the last ply from the EngineGame.Line (which is the
        /// move made by the user) and finds its parent in the Workbook.
        /// 
        /// NOTE: If the parent is not in the Workbook, this method should
        /// not have been invoked as the TrainingMode should know that
        /// we are "out of the book".
        /// 
        /// Having found the parent, checks if the user's move corresponds to any move
        /// in the Workbook (i.e. children of that parent) and report accordingly)
        /// </summary>
        public void ReportLastMoveVsWorkbook()
        {
            lock (TrainingState.UserVsWorkbookMoveLock)
            {
                if (TrainingState.CurrentMode != TrainingState.Mode.USER_MOVE_COMPLETED)
                    return;

                RemoveIntroParas();

                _otherMovesInWorkbook.Clear();

                _userMove = EngineGame.GetCurrentNode();
                TreeNode parent = _userMove.Parent;

                if (PositionUtils.IsCheckmate(_userMove.Position))
                {
                    BuildMoveParagraph(_userMove, true);
                    BuildCheckmateParagraph(_userMove, true);
                }
                else if (PositionUtils.IsStalemate(_userMove.Position))
                {
                    BuildMoveParagraph(_userMove, true);
                    BuildStalemateParagraph(_userMove);
                }
                else
                {
                    // double check that we have the parent in our Workbook
                    if (_mainWin.Workbook.GetNodeFromNodeId(parent.NodeId) == null)
                    {
                        // we are "out of the book" in our training so there is nothing to report
                        return;
                    }

                    StringBuilder wbMoves = new StringBuilder();
                    TreeNode foundMove = null;
                    foreach (TreeNode child in parent.Children)
                    {
                        // we cannot use ArePositionsIdentical() because _userMove only has static position
                        if (child.LastMoveEngineNotation == _userMove.LastMoveEngineNotation && !_userMove.IsNewTrainingMove)
                        {
                            // replace the TreeNode with the one from the Workbook so that
                            // we stay with the workbook as long as the user does.
                            EngineGame.ReplaceCurrentWithWorkbookMove(child);
                            foundMove = child;
                            _userMove = child;
                        }
                        else
                        {
                            if (!child.IsNewTrainingMove)
                            {
                                wbMoves.Append(child.GetPlyText(true));
                                wbMoves.Append("; ");
                                _otherMovesInWorkbook.Add(child);
                            }
                        }
                    }

                    BuildMoveParagraph(_userMove, true);
                    BuildCommentParagraph(foundMove != null);

                    // if we found a move and this is not the last move in the Workbbook, request response.
                    if (foundMove != null && foundMove.Children.Count > 0)
                    {
                        // start the timer that will trigger a workbook response by RequestWorkbookResponse()
                        TrainingState.CurrentMode = TrainingState.Mode.AWAITING_WORKBOOK_RESPONSE;
                        _mainWin.Timers.Start(AppTimers.TimerId.REQUEST_WORKBOOK_MOVE);
                    }
                    else
                    {
                        _paraCurrentEngineGame = AddNewParagraphToDoc(STYLE_ENGINE_GAME, "");
                        _paraCurrentEngineGame.Name = _par_game_moves_;
                        if (foundMove != null)
                        {
                            _paraCurrentEngineGame.Inlines.Add(new Run("\nThe Worbook line has ended."));
                        }
                        _paraCurrentEngineGame.Inlines.Add(new Run("\nA training game against the engine has started. Wait for the engine\'s move...\n"));
                        _engineGameRootNode = _userMove;
                        // call RequestEngineResponse() directly so it invokes PlayEngine
                        AppStateManager.CurrentLearningMode = LearningMode.Mode.ENGINE_GAME;
                        AppStateManager.SetupGuiForCurrentStates();
                        RequestEngineResponse();
                    }
                }
            }
            _mainWin.UiRtbTrainingProgress.ScrollToEnd();
        }

        /// <summary>
        /// Rolls back the training to the ply
        /// that we want to replace with the last clicked node.
        /// </summary>
        public void RollbackToWorkbookMove()
        {
            _currentEngineGameMoveCount = 0;

            TrainingState.RollbackTrainingLine(_lastClickedNode);
            EngineGame.RollbackGame(_lastClickedNode);

            TrainingState.CurrentMode = TrainingState.Mode.USER_MOVE_COMPLETED;

            AppStateManager.CurrentLearningMode = LearningMode.Mode.TRAINING;
            AppStateManager.SetupGuiForCurrentStates();

            _mainWin.BoardCommentBox.GameMoveMade(_lastClickedNode, true);

            RemoveParagraphsFromMove(_lastClickedNode);
            ReportLastMoveVsWorkbook();
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
                    int nodeId = GuiUtilities.GetNodeIdFromPrefixedString(((Paragraph)block).Name);
                    TreeNode nd = _mainWin.Workbook.GetNodeFromNodeId(nodeId);
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
        }

        /// <summary>
        /// When the user made their move, and the training is in
        /// manual mode (as opposed to a game vs engine)
        /// a timer was started to invoke
        /// this method (via InvokeRequestWorkbookResponse).
        /// This method performs the move and starts the timer
        /// so that is gets picked up by EngineGame.CheckForTrainingWorkbookMoveMade.
        /// </summary>
        public void RequestWorkbookResponse()
        {
            int nodeId = _userMove.NodeId;
            _mainWin.Timers.Stop(AppTimers.TimerId.REQUEST_WORKBOOK_MOVE);

            // user may have chosen a different move to what we originally had
            // TODO: after the re-think of the GUI that probably cannot happen (?)
            EngineGame.ReplaceCurrentWithWorkbookMove(nodeId);

            TreeNode userChoiceNode = _mainWin.Workbook.GetNodeFromNodeId(nodeId);

            _mainWin.DisplayPosition(userChoiceNode.Position);
            _mainWin.ColorMoveSquares(_userMove.LastMoveEngineNotation);

            TreeNode nd = _mainWin.Workbook.SelectRandomChild(nodeId);

            // Selecting a random response to the user's choice from the Workbook
            EngineGame.AddPlyToGame(nd);

            // The move will be visualized in response to CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE timer's elapsed event
            EngineGame.TrainingWorkbookMoveMade = true;
            _mainWin.Timers.Start(AppTimers.TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE);
            AppStateManager.SwapCommentBoxForEngineLines(false);
        }

        /// <summary>
        /// This method is called directly when the user
        /// made their move and the training is in engine game mode
        /// (as opposed to the manual mode).
        /// This method requests the engine to make a move.
        /// </summary>
        public void RequestEngineResponse()
        {
            int nodeId = _userMove.NodeId;
            _mainWin.StartEngineGame(_userMove, true);
        }

        /// <summary>
        /// This is called from MainWindow.MoveEvaluationFinished()
        /// when engine produced a move while playing with the user.
        /// </summary>
        public void EngineMoveMade()
        {
            AddMoveToEngineGamePara(EngineGame.GetCurrentNode());
            _mainWin.UiRtbTrainingProgress.ScrollToEnd();
        }

        /// <summary>
        /// This is called from EngineGame.ProcessUserGameMove()
        /// when engine produced a move while playing with the user.
        /// </summary>
        public void UserMoveMade()
        {
            AddMoveToEngineGamePara(EngineGame.GetCurrentNode());
            _mainWin.UiRtbTrainingProgress.ScrollToEnd();
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
        private void AddMoveToEngineGamePara(TreeNode nd)
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
                if (_currentEngineGameMoveCount % 8 == 0)
                {
                    text = "\n          " + text;
                }
            }

            Run gm = CreateButtonRun(text, _run_engine_game_move_ + nd.NodeId.ToString(), Brushes.Brown);
            _paraCurrentEngineGame.Inlines.Add(gm);

            if (nd.Position.IsCheckmate)
            {
                BuildCheckmateParagraph(nd, false);
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
                if (_currentEngineGameMoveCount % 8 == 0)
                {
                    text = "\n          " + text;
                }
                Run gm = CreateButtonRun(text, _run_engine_game_move_ + nd.NodeId.ToString(), Brushes.Brown);
                _paraCurrentEngineGame.Inlines.Add(gm);
            };
        }

        /// <summary>
        /// This method will be invoked when we requested evaluation and got the results back.
        /// The EngineMessageProcessor has the results.
        /// We can be in a MOVE or LINE evaluation mode.
        /// </summary>
        public void ShowEvaluationResult()
        {
            Run runEvaluated;
            TreeNode nodeEvaluated;
            if (_mainWin.Evaluation.CurrentMode == EvaluationState.EvaluationMode.LINE)
            {
                runEvaluated = _mainWin.Evaluation.GetCurrentEvaluatedRun();
                nodeEvaluated = _mainWin.Evaluation.GetCurrentEvaluatedNode();
            }
            else
            {
                runEvaluated = _lastClickedRun;
                nodeEvaluated = _lastClickedNode;
            }

            if (runEvaluated == null)
            {
                // this should never happen but...
                AppStateManager.SetCurrentEvaluationMode(EvaluationState.EvaluationMode.IDLE);
                return;
            }

            // insert the evaluation result after the move.
            List<MoveEvaluation> moveCandidates = _mainWin.EngineLinesGUI.Lines;
            if (moveCandidates.Count == 0)
                return;

            _mainWin.Dispatcher.Invoke(() =>
            {
                MoveEvaluation eval = moveCandidates[0];

                Paragraph parent = runEvaluated.Parent as Paragraph;
                string runEvalName = _run_move_eval_ + nodeEvaluated.NodeId.ToString();

                // Remove previous evaluation if exists
                var r_prev = parent.Inlines.FirstOrDefault(x => x.Name == runEvalName);
                parent.Inlines.Remove(r_prev);

                Run r_eval = CreateEvaluationRun(eval, runEvalName);

                parent.Inlines.InsertAfter(runEvaluated, r_eval);

                _mainWin.FloatingChessBoard.DisplayPosition(nodeEvaluated.Position);
                _mainWin.UiVbFloatingChessboard.Margin = new Thickness(_lastClickedPoint.X, _lastClickedPoint.Y - 165, 0, 0);
                _mainWin.ShowFloatingChessboard(true);

                if (_mainWin.Evaluation.CurrentMode == EvaluationState.EvaluationMode.LINE)
                {
                    RequestMoveEvaluation();
                }
                else
                {
                    AppStateManager.SetCurrentEvaluationMode(EvaluationState.EvaluationMode.IDLE);
                }
            });

            AppStateManager.ShowEvaluationProgressControlsForCurrentStates();
        }

        /// <summary>
        /// Creates a Run object with the evaluation text
        /// </summary>
        /// <param name="eval"></param>
        /// <param name="runName"></param>
        /// <returns></returns>
        private Run CreateEvaluationRun(MoveEvaluation eval, string runName)
        {
            Run r_eval = new Run("(" + GuiUtilities.BuildEvaluationText(eval, _lastClickedNode.Position.ColorToMove) + ") ");
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

            Paragraph para = AddNewParagraphToDoc(STYLE_MOVES_MAIN, "");
            para.Name = paraName;

            Run r_prefix = new Run();
            if (userMove)
            {
                r_prefix.Text = "You played:   ";
            }
            else
            {
                r_prefix.Text = "Coach's response:   ";
            }
            r_prefix.FontWeight = FontWeights.Normal;

            para.Inlines.Add(r_prefix);

            Run r = CreateButtonRun(MoveUtils.BuildSingleMoveText(nd, true) + " ", runName, Brushes.Black);
            para.Inlines.Add(r);

            _mainWin.UiRtbTrainingProgress.ScrollToEnd();
        }

        /// <summary>
        /// Builds a paragraph reporting checkmate
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="userMove"></param>
        private void BuildCheckmateParagraph(TreeNode nd, bool userMove)
        {
            string paraName = _par_line_moves_ + nd.NodeId.ToString();
            string runName = _run_line_move_ + nd.NodeId.ToString();

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
            string paraName = _par_line_moves_ + nd.NodeId.ToString();
            string runName = _run_line_move_ + nd.NodeId.ToString();

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

            Run r_prefix = new Run("\nThis training session with your virtual Coach starts from the position arising after: \n");
            r_prefix.FontWeight = FontWeights.Normal;
            _dictParas[ParaType.STEM].Inlines.Add(r_prefix);

            InsertPrefixRuns(_dictParas[ParaType.STEM], node);
        }

        private void InsertPrefixRuns(Paragraph para, TreeNode lastNode)
        {
            TreeNode nd = lastNode;
            Run prevRun = null;
            while (nd != null)
            {
                Run r = CreateButtonRun(MoveUtils.BuildSingleMoveText(nd, false) + " ", _run_stem_move_ + nd.NodeId.ToString(), Brushes.Black);
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
            sbInstruction.Append(TrainingState.StartPosition.ColorToMove == PieceColor.White ? " White " : " Black ");
            sbInstruction.Append("and the program (a.k.a. the \"Coach\") will respond for");
            sbInstruction.Append(TrainingState.StartPosition.ColorToMove == PieceColor.White ? " Black." : " White.");
            sbInstruction.AppendLine();
            sbInstruction.Append("The Coach will comment on your every move based on the content of the Workbook.\n");
            sbInstruction.Append("\nRemember that you can:\n");
            sbInstruction.Append("- click on an alternative move in the Coach's comment to play it instead of your original choice,\n");
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
            TreeNode nd = EngineGame.GetCurrentNode();

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
        }

        /// <summary>
        /// Builds the paragraph with "coach's" comments.
        /// </summary>
        /// <param name="isWorkbookMove"></param>
        private void BuildCommentParagraph(bool isWorkbookMove)
        {
            string paraName = _par_coach_moves_ + _userMove.NodeId.ToString();

            Paragraph para = AddNewParagraphToDoc(STYLE_COACH_NOTES, "");
            para.Name = paraName;

            Run coach = new Run("Coach says:");
            coach.TextDecorations = TextDecorations.Underline;
            para.Inlines.Add(coach);

            string txt = "  ";
            if (_otherMovesInWorkbook.Count == 0)
            {
                if (isWorkbookMove)
                {
                    txt += "The move you made is the only move in the Workbook.";
                    Run r_only = new Run(txt);
                    para.Inlines.Add(r_only);
                }
                else
                {
                    txt += "The Workbook line has ended. ";
                    Run r_notWb = new Run(txt);
                    para.Inlines.Add(r_notWb);
                }
            }
            else
            {

                if (!isWorkbookMove)
                {
                    txt += "This is not in the Workbook. ";
                    Run r_notWb = new Run(txt);
                    para.Inlines.Add(r_notWb);

                    txt = "";
                }

                if (!isWorkbookMove)
                {
                    if (_otherMovesInWorkbook.Count == 1)
                    {
                        txt += "The only Workbook move is ";
                    }
                    else
                    {
                        txt += "The Workbook moves are ";
                    }
                    Run r_wb = new Run(txt);
                    para.Inlines.Add(r_wb);
                }
                else
                {
                    if (_otherMovesInWorkbook.Count == 1)
                    {
                        txt += "The only other Workbook move is ";
                    }
                    else
                    {
                        txt += "Other Workbook moves are ";
                    }
                    Run r_wb = new Run(txt);
                    para.Inlines.Add(r_wb);
                }

                BuildOtherWorkbookMovesRun(para);
            }
            _mainWin.UiRtbTrainingProgress.ScrollToEnd();
        }

        /// <summary>
        /// Adds plies from _otherMovesInWorkbook to the
        /// passed paragraph.
        /// </summary>
        /// <param name="para"></param>
        private void BuildOtherWorkbookMovesRun(Paragraph para)
        {
            foreach (TreeNode nd in _otherMovesInWorkbook)
            {
                para.Inlines.Add(CreateButtonRun(MoveUtils.BuildSingleMoveText(nd, true) + " ", _run_wb_move_ + nd.NodeId.ToString(), Brushes.Green));
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

        private string BuildMoveTextForMenu(out string midTxt)
        {
            midTxt = " ";
            if (_moveContext == MoveContext.GAME || _moveContext == MoveContext.LINE)
            {
                if (_lastClickedNode.ColorToMove != LearningMode.TrainingSide)
                {
                    midTxt = " Your ";
                }
                else
                {
                    midTxt = " Engine\'s ";
                }
            }

            return MoveUtils.BuildSingleMoveText(_lastClickedNode, true);
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
            _mainWin.ShowFloatingChessboard(false);

            _mainWin.Dispatcher.Invoke(() =>
            {
                string midTxt;
                string moveTxt = BuildMoveTextForMenu(out midTxt);

                ContextMenu cm = _mainWin.FindResource("_cmTrainingView") as ContextMenu;
                foreach (object o in cm.Items)
                {
                    if (o is MenuItem)
                    {
                        MenuItem mi = o as MenuItem;
                        switch (mi.Name)
                        {
                            case "_mnTrainEvalMove":
                                mi.Header = "Evaluate" + midTxt + "Move " + moveTxt;
                                break;
                            case "_mnTrainEvalLine":
                                mi.Header = "Evaluate Line";
                                mi.Visibility = _moveContext == MoveContext.WORKBOOK_COMMENT ? Visibility.Collapsed : Visibility.Visible;
                                break;
                            case "_mnTrainRestartGame":
                                mi.Header = "Restart Game After" + midTxt + "Move " + moveTxt;
                                mi.Visibility = _moveContext == MoveContext.GAME ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "_mnTrainSwitchToWorkbook":
                                mi.Header = "Play " + moveTxt + " instead of Your Move";
                                mi.Visibility = _moveContext == MoveContext.WORKBOOK_COMMENT ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            case "_mnTrainRestartTraining":
                                break;
                            case "_mnTrainExitTraining":
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        if (o is Separator && (o as Separator).Name == "_mnTrainSepar_1")
                        {
                            (o as Separator).Visibility = _moveContext == MoveContext.LINE ? Visibility.Collapsed : Visibility.Visible;
                        }
                    }
                }
                cm.PlacementTarget = _mainWin.UiRtbTrainingProgress;
                cm.IsOpen = true;
                _mainWin.Timers.Stop(AppTimers.TimerId.SHOW_TRAINING_PROGRESS_POPUP_MENU);
            });
            _blockFloatingBoard = false;
        }

        /// <summary>
        /// Invoked from the Training context menu.
        /// Starts evaluation of the clicked move.
        /// Alternatively, can be called as part of line 
        /// evaluation.
        /// </summary>
        public void RequestMoveEvaluation()
        {
            if (_mainWin.Evaluation.CurrentMode == EvaluationState.EvaluationMode.LINE)
            {
                TreeNode nd = _mainWin.Evaluation.GetNextNodeToEvaluate();
                if (nd == null)
                {
                    _mainWin.Evaluation.ClearRunsToEvaluate();
                    AppStateManager.SetCurrentEvaluationMode(EvaluationState.EvaluationMode.IDLE);
                }
                else
                {
                    EngineMessageProcessor.RequestMoveEvaluationInTraining(nd);
                }
            }
            else
            {
                AppStateManager.SetCurrentEvaluationMode(EvaluationState.EvaluationMode.SINGLE_MOVE);
                EngineMessageProcessor.RequestMoveEvaluationInTraining(_lastClickedNode);
            }
        }

        /// <summary>
        /// Requests evaluation of a  line.
        /// Checks if this is for the Main Line or an Engine Game,
        /// sets up a list of Nodes and Runs to evaluate 
        /// and calls RequestMpoveEvaluation().
        /// Sets Evaluation.CurrentMode to TRAINING_LINE to ensure
        /// that evaluation does not stop after the first move.
        /// </summary>
        public void RequestLineEvaluation()
        {
            _mainWin.Evaluation.ClearRunsToEvaluate();

            // figure out whether this is for the Main Line or Engine Game
            Run firstRun = _lastClickedRun;
            Paragraph parentPara = firstRun.Parent as Paragraph;
            if (firstRun != null)
            {
                string paraName = parentPara.Name;
                if (paraName.StartsWith(_par_line_moves_))
                {
                    // collect the Main Line's Runs to evaluate the moves
                    SetMainLineRunsToEvaluate(paraName, _lastClickedRun);
                    AppStateManager.SetCurrentEvaluationMode(EvaluationState.EvaluationMode.LINE);
                    RequestMoveEvaluation();
                }
                else if (paraName.StartsWith(_par_game_moves_))
                {
                    SetGameRunsToEvaluate(parentPara, _lastClickedRun);
                    AppStateManager.SetCurrentEvaluationMode(EvaluationState.EvaluationMode.LINE);
                    RequestMoveEvaluation();
                }
            }
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
                                    _mainWin.Evaluation.AddRunToEvaluate(inl as Run);
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
                    if (!started && inl.Name == firstRun.Name)
                    {
                        started = true;
                    }

                    if (started && inl.Name.StartsWith(_run_engine_game_move_))
                    {
                        _mainWin.Evaluation.AddRunToEvaluate(inl as Run);
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
                if (_lastClickedNode.ColorToMove != LearningMode.TrainingSide)
                {
                    EngineGame.RestartAtUserMove(nd);
                    _mainWin.BoardCommentBox.GameMoveMade(nd, true);
                }
                else
                {
                    EngineGame.RestartAtEngineMove(nd);
                    _mainWin.BoardCommentBox.GameMoveMade(nd, false);
                }
                _mainWin.DisplayPosition(nd.Position);
                RebuildEngineGamePara(nd);
            }
        }

        /// <summary>
        /// Based on the name of the clicked run, performs an action.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventRunClicked(object sender, MouseButtonEventArgs e)
        {
            // don't accept any clicks if evaluation is in progress
            if (_mainWin.Evaluation.CurrentMode == EvaluationState.EvaluationMode.SINGLE_MOVE
                || _mainWin.Evaluation.CurrentMode == EvaluationState.EvaluationMode.LINE)
            {
                return;
            }

            Run r = (Run)e.Source;
            if (string.IsNullOrEmpty(r.Name))
            {
                return;
            }

            if (e.ChangedButton == MouseButton.Right || e.ChangedButton == MouseButton.Left)
            {
                if (r.Name.StartsWith(_run_line_move_))
                {
                    // a move in the main training line was clicked 
                    DetectLastClickedNode(r, _run_line_move_, e);
                    _moveContext = MoveContext.LINE;
                    _mainWin.Timers.Start(AppTimers.TimerId.SHOW_TRAINING_PROGRESS_POPUP_MENU);
                }
                else if (r.Name.StartsWith(_run_wb_move_))
                {
                    // a workbook move in the comment was clicked 
                    DetectLastClickedNode(r, _run_wb_move_, e);
                    _moveContext = MoveContext.WORKBOOK_COMMENT;
                    _mainWin.Timers.Start(AppTimers.TimerId.SHOW_TRAINING_PROGRESS_POPUP_MENU);
                }
                else if (r.Name.StartsWith(_run_engine_game_move_))
                {
                    // a move from a game against the engine was clicked,
                    // we take the game back to that move.
                    // If it is an engine move, the user will be required to respond,
                    // otherwise it will be engine's turn.
                    DetectLastClickedNode(r, _run_engine_game_move_, e);
                    _moveContext = MoveContext.GAME;
                    _mainWin.Timers.Start(AppTimers.TimerId.SHOW_TRAINING_PROGRESS_POPUP_MENU);
                }
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
            TreeNode nd = _mainWin.Workbook.GetNodeFromNodeId(nodeId);
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

            int nodeId = GuiUtilities.GetNodeIdFromPrefixedString(r.Name);
            if (nodeId >= 0)
            {
                Point pt = e.GetPosition(_mainWin.UiRtbTrainingProgress);
                _mainWin.FloatingChessBoard.DisplayPosition(_mainWin.Workbook.GetNodeFromNodeId(nodeId).Position);
                int yOffset = r.Name.StartsWith(_run_stem_move_) ? 25 : -165;
                _mainWin.UiVbFloatingChessboard.Margin = new Thickness(pt.X, pt.Y + yOffset, 0, 0);
                _mainWin.ShowFloatingChessboard(true);
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

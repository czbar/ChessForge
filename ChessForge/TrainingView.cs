using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using ChessPosition;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Manages the RichTextBox showing the progress of the training session.
    /// The document has the follwoing structure:
    /// - The header and the "stem" paragraph are permanently displayed at the top of the box.
    /// - The "intro" and "instruction" paragraphs are shown only at the start and are removed
    ///   as soon as any training content is to be shown.
    /// - The training "history" paragraph shows the most important info about the moves made
    ///   so far and is updated after every move
    /// - The last paragraphs deal with the latest move made by the user:
    ///    o if the user's move was in the Workbook a paragraph offering to evaluate it or
    ///      start a game with the engine will be shown.
    ///    o a paragraph with all workbook move options will be displayed with options to evaluate
    ///      or make the selected move. If there is only one move in the workbook, and it is the move
    ///      made by the user, only an "evaluate" option will be offered.
    ///    o if the user chooses to evaluate any move, a Run with evaluation results will be inserted
    ///      after the line with the move.  It will be deleted/replaced if the user asks to evaluate
    ///      again.
    ///      
    /// When needed, the Paragraphs and Runs are assigned names that are used to upadate / delete or use them 
    /// as position references.
    /// Runs with Workbook moves (play and eval "buttons" as well as evaluation results) are named using a prefix 
    /// followed by a NodeId for easy identification. User moves and evaluations for moves not in the Workbook do
    /// not require Node identification as there can only be one such move at a time.
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
            CONTINUATION,
            INSTRUCTIONS,
            PLAY_ENGINE_NOTE,
            PROMPT_TO_MOVE,
            USER_MOVE,
            WORKBOOK_MOVES,
            HISTORY
        }

        /// <summary>
        /// Maps paragraph types to Paragraph objects
        /// </summary>
        private Dictionary<ParaType, Paragraph> _dictParas = new Dictionary<ParaType, Paragraph>()
        {
            [ParaType.INTRO] = null,
            [ParaType.STEM] = null,
            [ParaType.CONTINUATION] = null,
            [ParaType.INSTRUCTIONS] = null,
            [ParaType.PLAY_ENGINE_NOTE] = null,
            [ParaType.PROMPT_TO_MOVE] = null,
            [ParaType.USER_MOVE] = null,
            [ParaType.WORKBOOK_MOVES] = null,
            [ParaType.HISTORY] = null
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
            ParaType.CONTINUATION,
            ParaType.INSTRUCTIONS,
            ParaType.PLAY_ENGINE_NOTE,
            ParaType.PROMPT_TO_MOVE,
            ParaType.USER_MOVE,
            ParaType.WORKBOOK_MOVES,
            ParaType.HISTORY
        };

        /// <summary>
        /// IDs of button styles (here, buttons are highlighted clickable Runs
        /// </summary>
        private enum ButtonStyle
        {
            BLUE,
            GREEN,
            RED
        }

        /// <summary>
        /// The Run with the move for which engine evaluation
        /// is running.
        /// </summary>
        private Run _underEvaluationRun;

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
        /// The move with which the user wishes to proceed after selecting
        /// from the Workbook options.
        /// </summary>
        private int _userChoiceNodeId;

        /// <summary>
        /// The node from which we start the training session.
        /// </summary>
        private TreeNode _sessionStartNode;

        private int _sessionStartNodeIndex;

        private int _nodeIdUnderEvaluation = -1;

        /// <summary>
        /// Names and prefixes for Runs.
        /// </summary>
        private readonly string _run_eval_user_move = "user_eval";
        private readonly string _run_play_engine = "play_engine";
        private readonly string _run_eval_wb_move_ = "wb_eval_";
        private readonly string _run_eval_results_ = "eval_results_";
        private readonly string _run_user_eval_results = "user_eval_results";
        private readonly string _run_play_wb_move_ = "wb_play_";

        /// <summary>
        /// Creates an instance of this class and sets reference 
        /// to the FlowDocument managed by the object.
        /// </summary>
        /// <param name="doc"></param>
        public TrainingView(FlowDocument doc) : base(doc)
        {
        }

        /// <summary>
        /// Property referencing definitions of Paragraphs 
        /// </summary>
        override internal Dictionary<string, RichTextPara> RichTextParas { get { return _richTextParas; } }

        /// <summary>
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        internal Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            ["intro"] = new RichTextPara(0, 0, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(0, 0, 0)), TextAlignment.Left),
            ["first_prompt"] = new RichTextPara(10, 0, 16, FontWeights.Bold, Brushes.Green, TextAlignment.Left, Brushes.Green),
            ["second_prompt"] = new RichTextPara(10, 0, 14, FontWeights.Bold, Brushes.Green, TextAlignment.Left, Brushes.Green),
            ["play_engine_note"] = new RichTextPara(10, 10, 16, FontWeights.Bold, Brushes.Black, TextAlignment.Left, Brushes.Black),
            ["stem_line"] = new RichTextPara(0, 10, 14, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Left),
            ["continuation"] = new RichTextPara(0, 20, 12, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Left, Brushes.Gray),
            ["eval_results"] = new RichTextPara(30, 5, 14, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(51, 159, 141)), TextAlignment.Left),
            ["normal"] = new RichTextPara(10, 0, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Left),
            ["default"] = new RichTextPara(10, 5, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(128, 98, 63)), TextAlignment.Left),
        };

        /// <summary>
        /// Initializes the state of the view.
        /// Sets the starting node of the training session
        /// and shows the intro text.
        /// </summary>
        /// <param name="node"></param>
        public void Initialize(TreeNode node)
        {
            _sessionStartNode = node;
            _sessionStartNodeIndex = (int)(node.MoveNumber * 2 - (node.ColorToMove == PieceColor.Black ? -1 : 0));
            Document.Blocks.Clear();

            InitParaDictionary();

            BuildIntroText(node);
        }

        private void InitParaDictionary()
        {
            _dictParas[ParaType.INTRO] = null;
            _dictParas[ParaType.STEM] = null;
            _dictParas[ParaType.CONTINUATION] = null;
            _dictParas[ParaType.PLAY_ENGINE_NOTE] = null;
            _dictParas[ParaType.INSTRUCTIONS] = null;
            _dictParas[ParaType.PROMPT_TO_MOVE] = null;
            _dictParas[ParaType.USER_MOVE] = null;
            _dictParas[ParaType.WORKBOOK_MOVES] = null;
            _dictParas[ParaType.HISTORY] = null;
        }

        /// <summary>
        /// Builds text for intorductory paragraphs.
        /// </summary>
        /// <param name="node"></param>
        private void BuildIntroText(TreeNode node)
        {
            BuildStemText(node);
            BuildInstructionsText();
        }

        /// <summary>
        /// This method is invoked when user makes their move.
        /// 
        /// It dets the last play from the EngineGame.Line (which is the
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
            RemoveIntroParas();

            _otherMovesInWorkbook.Clear();

            _userMove = EngineGame.GetCurrentNode();
            EngineGame.AddLastNodeToPlies();
            TreeNode parent = _userMove.Parent;

            // double check that we have the parent in our Workbook
            if (AppState.MainWin.Workbook.GetNodeFromNodeId(parent.NodeId) == null)
            {
                // we are "out of the book" in our training so there is nothing to report
                return;
            }

            int childCount = parent.Children.Count;
            StringBuilder wbMoves = new StringBuilder();
            TreeNode foundMove = null;
            foreach (TreeNode child in parent.Children)
            {
                if (child.LastMoveEngineNotation == _userMove.LastMoveEngineNotation)
                {
                    // replace the TreeNode with the one from the Workbook so that
                    // we stay with the workbook as long as the user does.
                    EngineGame.ReplaceCurrentWithWorkbookMove(child);
                    foundMove = child;
                    _userMove = child;
                }
                else
                {
                    wbMoves.Append(child.GetPlyText(true));
                    wbMoves.Append("; ");
                    _otherMovesInWorkbook.Add(child);
                }
            }

            if (foundMove != null)
            {
                BuildUserMoveParagraph(foundMove, true);
                BuildWorkbookMovesParagraph(wbMoves.ToString(), true);
            }
            else
            {
                BuildUserMoveParagraph(_userMove, false);
                BuildWorkbookMovesParagraph(wbMoves.ToString(), false);
            }
        }

        /// <summary>
        /// This method will be invoked when we requested evaluation and got the results back.
        /// The EngineMessageProcessor has the results.
        /// We will create a new Run and insert it after the Run that reported
        /// the move with a "play" option. Reference to that run was preserved
        /// when calling the engine evaluation method.
        /// </summary>
        public void ShowEvaluationResult()
        {
            // restore the position we are stopped at
            AppState.MainWin.DisplayPosition(_userMove.Position);

            if (_underEvaluationRun == null)
                return;

            List<MoveEvaluation> moveCandidates = AppState.MainWin.EngineLinesGUI.Lines;
            int maxCandidates = Math.Min(5, moveCandidates.Count);

            StringBuilder sb = new StringBuilder();
            sb.Append("\n          Evaluation summary:\n");
            for (int i = 0; i < maxCandidates; i++)
            {
                TreeNode nd;

                if (_nodeIdUnderEvaluation >= 0)
                {
                    nd = AppState.MainWin.Workbook.GetNodeFromNodeId(_nodeIdUnderEvaluation);
                }
                else
                {
                    nd = _userMove;
                }

                bool dummy;
                if (i > 0)
                {
                    sb.Append("\n");
                }

                int intEval = nd.Position.ColorToMove == PieceColor.White ? moveCandidates[i].ScoreCp : -1 * moveCandidates[i].ScoreCp;
                sb.Append("               " + (i + 1).ToString() + ". (" + (((double)intEval) / 100.0).ToString("F2") + ") ");
                BoardPosition workingPosition = new BoardPosition(nd.Position);
                if (workingPosition.ColorToMove == PieceColor.White)
                {
                    sb.Append(workingPosition.MoveNumber.ToString() + ".");
                }
                else
                {
                    sb.Append(workingPosition.MoveNumber.ToString() + "...");
                }
                string move = MoveUtils.EngineNotationToAlgebraic(moveCandidates[i].GetCandidateMove(), ref workingPosition, out dummy);
                sb.Append(move);
            }

            AppState.MainWin._rtbTrainingBrowse.Dispatcher.Invoke(() =>
            {
                InsertEvaluationResultsRun(sb.ToString());
            });
        }

        /// <summary>
        /// Builds the paragraph showing moves made in this training session
        /// from the session's starting position.
        /// </summary>
        private void BuildContinuationPara()
        {
            if (_dictParas[ParaType.CONTINUATION] == null)
            {
                _dictParas[ParaType.CONTINUATION] = AddNewParagraphToDoc("continuation", "\n", NonNullParaAtOrBefore(ParaType.STEM));
            }

            Paragraph para = _dictParas[ParaType.CONTINUATION];
            para.Inlines.Clear();
            
            Run intro = new Run("     moves made so far: ");
            intro.FontWeight = FontWeights.Normal;

            para.Inlines.Add(intro);

            string line = TextUtils.BuildTextForLine(new List<TreeNode>(EngineGame.Line.NodeList), false, _sessionStartNodeIndex + 1);
            Run cont = new Run(line);
            para.Inlines.Add(cont);
        }

        /// <summary>
        /// Creates a paragraph for holding a ummary record of the
        /// training session.
        /// </summary>
        private void BuildHistoryPara()
        {
            if (_dictParas[ParaType.HISTORY] != null)
            {
                return;
            }

            _dictParas[ParaType.HISTORY] = AddNewParagraphToDoc("normal", "\n", NonNullParaAtOrBefore(ParaType.WORKBOOK_MOVES));

            Run title = new Run("\nYour Training Session summary:");
            title.FontWeight = FontWeights.Bold;
            title.FontSize = 14;

            _dictParas[ParaType.HISTORY].Inlines.Add(title);
        }

        /// <summary>
        /// Inserts new evaluation results for the requested move.
        /// Clears previous results if exist.
        /// </summary>
        /// <param name="text"></param>
        private void InsertEvaluationResultsRun(string text)
        {
            string runName;
            Run runToDelete = null;

            bool runInUserPara = false;

            if (_nodeIdUnderEvaluation >= 0)
            {
                runName = _run_eval_results_ + _nodeIdUnderEvaluation.ToString();

                if (_dictParas[ParaType.WORKBOOK_MOVES] != null)
                {
                    runToDelete = _dictParas[ParaType.WORKBOOK_MOVES].Inlines.FirstOrDefault(x => x.Name == runName) as Run;
                    if (runToDelete != null)
                    {
                        _dictParas[ParaType.WORKBOOK_MOVES].Inlines.Remove(runToDelete);
                    }
                }

                if (runToDelete == null)
                {
                    runToDelete = _dictParas[ParaType.USER_MOVE].Inlines.FirstOrDefault(x => x.Name == runName) as Run;
                    _dictParas[ParaType.USER_MOVE].Inlines.Remove(runToDelete);
                    runInUserPara = true;
                }

            }
            else
            {
                runName = _run_user_eval_results;
                runToDelete = _dictParas[ParaType.USER_MOVE].Inlines.FirstOrDefault(x => x.Name == runName) as Run;
                _dictParas[ParaType.USER_MOVE].Inlines.Remove(runToDelete);
            }

            Run evalRun = new Run(text);
            evalRun.Name = runName;
            if (_nodeIdUnderEvaluation >= 0 && !runInUserPara)
            {
                _dictParas[ParaType.WORKBOOK_MOVES].Inlines.InsertAfter(_underEvaluationRun, evalRun);
            }
            else
            {
                _dictParas[ParaType.USER_MOVE].Inlines.InsertAfter(_underEvaluationRun, evalRun);
            }
        }


        /// <summary>
        /// Builds "intro" and "stem line" paragraphs that are always visible at the top
        /// of the view.
        /// </summary>
        /// <param name="node"></param>
        private void BuildStemText(TreeNode node)
        {
            _dictParas[ParaType.INTRO] = AddNewParagraphToDoc("intro", "This training session started from the position arising after:");
            _dictParas[ParaType.STEM] = AddNewParagraphToDoc("stem_line", GetStemLineText(node));
        }

        /// <summary>
        /// Initial prompt to advise the user make their move.
        /// This paragraph is removed later on to reduce clutter.
        /// </summary>
        private void BuildInstructionsText()
        {
            StringBuilder sbInstruction = new StringBuilder();
            sbInstruction.Append("You will be making moves for");
            sbInstruction.Append(_sessionStartNode.ColorToMove == PieceColor.White ? " White " : " Black ");
            sbInstruction.Append("and the program will respond for ");
            sbInstruction.Append(_sessionStartNode.ColorToMove == PieceColor.White ? " Black " : " White " + "\n");
            sbInstruction.Append("The program will assess your moves against the moves found in the Workbook and allow you to:\n");
            sbInstruction.Append("     1. proceed with the move you made,\n");
            sbInstruction.Append("     2. switch to a move from the Workbook, if there are any options,\n");
            sbInstruction.Append("     3. request engine evaluation of your or Workbook's moves.\n");

            _dictParas[ParaType.INSTRUCTIONS] = AddNewParagraphToDoc("intro", sbInstruction.ToString());

            _dictParas[ParaType.PROMPT_TO_MOVE] = AddNewParagraphToDoc("first_prompt", "To begin, make your first move on the chessboard.");
        }

        private void BuildSecondPromptParagraph()
        {
            Document.Blocks.Remove(_dictParas[ParaType.PROMPT_TO_MOVE]);
            _dictParas[ParaType.PROMPT_TO_MOVE] = AddNewParagraphToDoc("second_prompt", " Your turn. Make your move on the chessboard.", NonNullParaAtOrBefore(ParaType.PROMPT_TO_MOVE));
        }

        /// <summary>
        /// A paragraph with a notification that user 's move was not found
        /// in the Workbook.
        /// </summary>
        /// <param name="nd"></param>
        private void BuildMoveFromWorkbookText(TreeNode nd)
        {
            AddNewParagraphToDoc("normal", "  (" + nd.GetPlyText(true) + " is in the Workbook.)");
        }

        /// <summary>
        /// This paragraphs shows the move made by the user.
        /// The commentary and "button" setup depends on whether the move
        /// was found in the Workbook or not.
        /// </summary>
        /// <param name="nd"></param>
        private void BuildUserMoveParagraph(TreeNode nd, bool moveInWorkbook)
        {
            Paragraph para = AddNewParagraphToDoc("normal", "", NonNullParaAtOrBefore(ParaType.CONTINUATION));
            _dictParas[ParaType.USER_MOVE] = para;
            para.Inlines.Clear();

            Run pre = new Run("Your move:\n");
            pre.FontWeight = FontWeights.Bold;
            pre.FontSize = 16;
            pre.Foreground = Brushes.Black;
            para.Inlines.Add(pre);

            Run r = new Run("   " + nd.GetPlyText(true));
            r.FontWeight = FontWeights.Bold;
            r.FontSize = 16;
            r.Foreground = moveInWorkbook ? Brushes.Black : Brushes.Red;
            para.Inlines.Add(r);

            if (moveInWorkbook)
            {
                if (_otherMovesInWorkbook.Count == 0)
                {
                    para.Inlines.Add(new Run(" is the only move in the Workbook in this position. \n\n"));
                    para.Inlines.Add(new Run("You can play it now (green highlight) or check the engin evaluation first (blue highlights). \n\n"));
                }
                else
                {
                    para.Inlines.Add(new Run(" is in the Workbook. \n\nYou can confirm it or choose another move from the Workbook. \n"));
                    para.Inlines.Add(new Run("You can run evaluations (blue highlights) before selecting the move to go ahead with (green highlights). \n\n"));
                }
            }
            else
            {
                para.Inlines.Add(new Run(" was not found in the Workbook. \n\nChoose how to proceed by clicking on one of the options highlighted below. \n"));
                para.Inlines.Add(new Run("You can run evaluations (blue highlights) before selecting the move to go ahead with (green highlights). \n\n"));
            }


            RemoveIntroParas();

            para.Inlines.Add(new Run("The move you made:\n"));
            para.Inlines.Add(new Run("      "));

            Run mv = new Run(_userMove.GetPlyText(true) + "  :  ");
            mv.FontWeight = FontWeights.Bold;
            para.Inlines.Add(mv);

            string evalButtonText = " evaluate only ";
            if (moveInWorkbook)
            {
                para.Inlines.Add(CreateButtonRun(evalButtonText, _run_eval_wb_move_ + nd.NodeId.ToString(), ButtonStyle.BLUE));
            }
            else
            {
                para.Inlines.Add(CreateButtonRun(evalButtonText, _run_eval_user_move, ButtonStyle.BLUE));
            }

            para.Inlines.Add(new Run("   or   "));
            if (moveInWorkbook)
            {
                para.Inlines.Add(CreateButtonRun(" play it", _run_play_wb_move_ + nd.NodeId.ToString(), ButtonStyle.GREEN));
            }
            else
            {
                para.Inlines.Add(CreateButtonRun("play it, starting a game against the engine", _run_play_engine, ButtonStyle.GREEN));
            }
        }

        /// <summary>
        /// This paragraph is built when the user made a move not
        /// found in the Workbook and we are showing them what's in
        /// the Workbook.
        /// </summary>
        /// <param name="moves"></param>
        private void BuildWorkbookMovesParagraph(string moves, bool moveInWorkbook)
        {
            RemoveIntroParas();

            if (!string.IsNullOrEmpty(moves))
            {
                Paragraph para;

                if (moveInWorkbook)
                {
                    if (_otherMovesInWorkbook.Count == 1)
                    {
                        para = AddNewParagraphToDoc("normal", "\nThe only other move in the Workbook:\n", NonNullParaAtOrBefore(ParaType.USER_MOVE));
                    }
                    else
                    {
                        para = AddNewParagraphToDoc("normal", "\nOther moves in the Workbook:\n", NonNullParaAtOrBefore(ParaType.USER_MOVE));
                    }
                }
                else
                {
                    para = AddNewParagraphToDoc("normal", "\nMoves in the Workbook:\n", NonNullParaAtOrBefore(ParaType.USER_MOVE));
                }

                _dictParas[ParaType.WORKBOOK_MOVES] = para;

                foreach (TreeNode nd in _otherMovesInWorkbook)
                {
                    para.Inlines.Add(new Run("      "));

                    Run r = new Run(nd.GetPlyText(true) + "  :  ");
                    r.FontWeight = FontWeights.Bold;
                    para.Inlines.Add(r);

                    string evalButtonText = " evaluate only ";
                    para.Inlines.Add(CreateButtonRun(evalButtonText, _run_eval_wb_move_ + nd.NodeId.ToString(), ButtonStyle.BLUE));
                    para.Inlines.Add(new Run("   or   "));
                    para.Inlines.Add(CreateButtonRun("go ahead and play it", _run_play_wb_move_ + nd.NodeId.ToString(), ButtonStyle.GREEN));
                    para.Inlines.Add(new Run("\n"));
                }
            }
            else
            {
                _dictParas[ParaType.WORKBOOK_MOVES] = null;
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
        private Run CreateButtonRun(string text, string runName, ButtonStyle style)
        {
            Run r = new Run(text);
            r.Name = runName;

            switch (style)
            {
                case ButtonStyle.BLUE:
                    r.Foreground = Brushes.Blue;
                    break;
                case ButtonStyle.GREEN:
                    r.Foreground = Brushes.Green;
                    break;
            }
            r.FontWeight = FontWeights.Bold;
            r.MouseDown += EventRunClicked;
            r.Cursor = Cursors.Hand;

            return r;
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

            if (r.Name.StartsWith(_run_eval_wb_move_))
            {
                // get id of the node
                int nodeId = int.Parse(r.Name.Substring(_run_eval_wb_move_.Length));
                TreeNode userChoiceNode = AppState.MainWin.Workbook.GetNodeFromNodeId(nodeId);

                _nodeIdUnderEvaluation = nodeId;
                // find Run for the same NodeId with "play" option,
                // we want to remember the reference so that we can append
                // the evaluation results run after it
                _underEvaluationRun = GetPlayRunForNodeId(nodeId);
                AppState.MainWin.DisplayPosition(userChoiceNode.Position);

                AppState.MainWin.RequestMoveEvaluationInTraining(nodeId);
            }
            else if (r.Name == _run_eval_user_move)
            {
                _underEvaluationRun = GetPlayRunForNodeId(-1);
                _nodeIdUnderEvaluation = -1;

                AppState.MainWin.RequestMoveEvaluationInTraining(_userMove);
            }
            else if (r.Name.StartsWith(_run_play_wb_move_))
            {
                int nodeId = int.Parse(r.Name.Substring(_run_eval_wb_move_.Length));
                EngineGame.ReplaceCurrentWithWorkbookMove(nodeId);
                TreeNode userChoiceNode = AppState.MainWin.Workbook.GetNodeFromNodeId(nodeId);
                SoundPlayer.PlayMoveSound(userChoiceNode.LastMoveAlgebraicNotation);
                _userChoiceNodeId = nodeId;

                BuildHistoryPara();
                AddUserMoveDecisionToHistory(_userMove, userChoiceNode, false);
                ClearDecisionParas();

                AppState.MainWin.DisplayPosition(userChoiceNode.Position);

                TreeNode nd = AppState.MainWin.Workbook.SelectRandomChild(nodeId);

                // Selecting a random response to the user's choice from the Workbook
                EngineGame.AddPlyToGame(nd);

                // The move will be visualized in response to CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE timer's elapsed event
                EngineGame.TrainingWorkbookMoveMade = true;
                AppState.MainWin.Timers.Start(AppTimers.TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE);
                AppState.MainWin.SwapCommentBoxForEngineLines(false);
            }
            else if (r.Name == _run_play_engine)
            {
                BuildContinuationPara();

                BuildHistoryPara();
                AddUserMoveDecisionToHistory(_userMove, null, true);
                AppState.MainWin.SwapCommentBoxForEngineLines(false);
                Document.Blocks.Remove(_dictParas[ParaType.USER_MOVE]);
                Document.Blocks.Remove(_dictParas[ParaType.WORKBOOK_MOVES]);
                _dictParas[ParaType.PLAY_ENGINE_NOTE] = AddNewParagraphToDoc("play_engine_note", "\nYou are now playing against the engine.", NonNullParaAtOrBefore(ParaType.INSTRUCTIONS));
                AppState.MainWin.PlayComputer(_userMove, true);
            }
        }

        /// <summary>
        /// This method will be called when the program made a move 
        /// and we want to update the Training view.
        /// </summary>
        public void WorkbookMoveMade()
        {
            AppState.MainWin._rtbTrainingProgress.Dispatcher.Invoke(() =>
            {
                BuildContinuationPara();
                BuildSecondPromptParagraph();
            });
        }

        /// <summary>
        /// Returns the Run with the "button" to play the move
        /// from a given NodeId.
        /// We will use it as refrence after which 
        /// to insert the evaluation results.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private Run GetPlayRunForNodeId(int nodeId)
        {
            if (nodeId >= 0)
            {
                string runName = _run_play_wb_move_ + nodeId.ToString();
                Run r = null;
                if (_dictParas[ParaType.WORKBOOK_MOVES] != null)
                {
                    r = _dictParas[ParaType.WORKBOOK_MOVES].Inlines.FirstOrDefault(x => x.Name == runName) as Run;
                }

                if (r == null)
                {
                    r = _dictParas[ParaType.USER_MOVE].Inlines.FirstOrDefault(x => x.Name == runName) as Run;
                }
                return r;

            }
            else
            {
                return _dictParas[ParaType.USER_MOVE].Inlines.FirstOrDefault(x => x.Name == _run_play_engine) as Run;
            }
        }

        /// <summary>
        /// User made their move and we have the following options to handle:
        /// - the original move was not in the Workbook and the user chose a Workbook move
        /// - the original move was not in the Workbook and the user chose play it anyway thus starting a game against the engine
        /// - the original move was in the Workbook and the user stuck with it
        /// - the original move was in the Workbook but the user chose a different Workbook move
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="changed"></param>
        private void AddUserMoveDecisionToHistory(TreeNode orig, TreeNode changed, bool playEngine)
        {
            Run pre = new Run();
            if (orig.NodeId > 0)
            {
                pre.Text = "\nYou played a Workbook move ";
            }
            else
            {
                pre.Text = "\nYou played a non-Workbook move ";
            }
            _dictParas[ParaType.HISTORY].Inlines.Add(pre);


            Run rOrig = new Run(MoveUtils.BuildSingleMoveText(orig));
            rOrig.FontWeight = FontWeights.Bold;
            if (orig.NodeId > 0)
            {
                rOrig.Foreground = Brushes.Green;
            }
            else
            {
                rOrig.Foreground = Brushes.Red;
            }
            _dictParas[ParaType.HISTORY].Inlines.Add(rOrig);

            bool showChangedMove = false;
            Run rDecision = new Run();
            if (playEngine)
            {
                rDecision.Text = " and chose to challenge the engine to a game.";
            }
            else
            {
                if (orig.NodeId == changed.NodeId)
                {
                    rDecision.Text = ".";
                }
                else
                {
                    if (orig.NodeId > 0)
                    {
                        rDecision.Text = " but changed to a different Workbook move ";
                        showChangedMove = true;
                    }
                    else
                    {
                        rDecision.Text = " and changed to a Workbook move ";
                        showChangedMove = true;
                    }
                }
            }
            _dictParas[ParaType.HISTORY].Inlines.Add(rDecision);

            if (showChangedMove)
            {
                Run rChanged = new Run(MoveUtils.BuildSingleMoveText(changed));
                rChanged.FontWeight = FontWeights.Bold;
                _dictParas[ParaType.HISTORY].Inlines.Add(rChanged);
            }
        }

        /// <summary>
        /// Removes all paragraphs related to the move decisions.
        /// This method is called once the user has chosen the move.
        /// </summary>
        private void ClearDecisionParas()
        {
            Document.Blocks.Remove(_dictParas[ParaType.WORKBOOK_MOVES]);
            Document.Blocks.Remove(_dictParas[ParaType.USER_MOVE]);
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

            Document.Blocks.Remove(_dictParas[ParaType.INSTRUCTIONS]);
            _dictParas[ParaType.INSTRUCTIONS] = null;
        }
    }
}

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
    public class TrainingProgressRichTextBuilder : RichTextBuilder
    {
        /// <summary>
        /// A reference to the paragraph with the initial prompt 
        /// to make a move on the chessboard. This reference will be used
        /// to removed this paragraph when no longer relevant.
        /// </summary>
        private Paragraph _promptPara;

        /// <summary>
        /// A reference to the paragraph with the initial instructions 
        /// regarding the training process.
        /// </summary>
        private Paragraph _instructionPara;

        /// <summary>
        /// The main paragraph with reporting moves
        /// found in the Workbook.
        /// </summary>
        private Paragraph _workbookMovesPara;

        /// <summary>
        /// The Run with the move for which engine evaluation
        /// is running.
        /// </summary>
        private Run _underEvaluationRun;

        /// <summary>
        /// The list of moves found in the Workbook in the current position,
        /// </summary>
        private List<TreeNode> _movesFromWorkbook = new List<TreeNode>();

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

        private int _nodeIdUnderEvaluation = -1;

        /// <summary>
        /// Names and prefixes for Runs.
        /// </summary>
        private readonly string _run_eval_user_move = "user_eval";
        private readonly string _run_play_engine = "play_engine";
        private readonly string _run_eval_wb_move_ = "wb_eval_";
        private readonly string _run_eval_results_ = "eval_results_";
        private readonly string _run_play_wb_move_ = "wb_play_";

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
        /// Creates an instance of this class and sets reference 
        /// to the FlowDocument managed by the object.
        /// </summary>
        /// <param name="doc"></param>
        public TrainingProgressRichTextBuilder(FlowDocument doc) : base(doc)
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
            ["stem_line"] = new RichTextPara(0, 10, 14, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Left),
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
            Document.Blocks.Clear();
            BuildIntroText(node);
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

            _movesFromWorkbook.Clear();

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
                if (child.LastMoveAlgebraicNotation == _userMove.LastMoveAlgebraicNotation)
                {
                    // replace the TreeNode with the one from the Workbook so that
                    // we stay with the workbook as long as the user does.
                    EngineGame.ReplaceCurrentWithWorkbookMove(child);
                    foundMove = child;
                }
                else
                {
                    wbMoves.Append(child.GetPlyText(true));
                    wbMoves.Append("; ");
                    _movesFromWorkbook.Add(child);
                }
            }

            if (foundMove != null)
            {
                BuildMoveFromWorkbookText(foundMove);
                BuildOtherWorkbookMovesText(wbMoves.ToString());
            }
            else
            {
                BuildMoveNotInWorkbookText(_userMove);
                BuildWorkbookMovesText(wbMoves.ToString());
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
            if (_underEvaluationRun == null)
                return;

            List<MoveEvaluation> moveCandidates = AppState.MainWin.EngineLinesGUI.Lines;
            int maxCandidates = Math.Min(5, moveCandidates.Count);

            StringBuilder sb = new StringBuilder();
            sb.Append("\n          Evaluation summary:\n");
            for (int i = 0; i < maxCandidates; i++)
            {
                TreeNode nd = AppState.MainWin.Workbook.GetNodeFromNodeId(_nodeIdUnderEvaluation);
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
        /// Inserts new evaluation results for the requested move.
        /// Clears previous results if exist.
        /// </summary>
        /// <param name="text"></param>
        private void InsertEvaluationResultsRun(string text)
        {
            string runName = _run_eval_results_ + _nodeIdUnderEvaluation.ToString();
            
            Run runToDelete = _workbookMovesPara.Inlines.FirstOrDefault(x => x.Name == runName) as Run;
            if (runToDelete != null)
            {
                _workbookMovesPara.Inlines.Remove(runToDelete);
            }

            Run evalRun = new Run(text);
            evalRun.Name = runName;
            _workbookMovesPara.Inlines.InsertAfter(_underEvaluationRun, evalRun);
        }


        /// <summary>
        /// Builds "intro" and "stem line" paragraphs that are always visible at the top
        /// of the view.
        /// </summary>
        /// <param name="node"></param>
        private void BuildStemText(TreeNode node)
        {
            AddNewParagraphToDoc("intro", "This training sessions starts from the position arising after:");
            AddNewParagraphToDoc("stem_line", GetStemLineText(node));
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

            _instructionPara = AddNewParagraphToDoc("intro", sbInstruction.ToString());

            _promptPara = AddNewParagraphToDoc("intro", "To begin, make your first move on the chessboard.");
            _promptPara.Foreground = Brushes.Green;
            _promptPara.FontWeight = FontWeights.Bold;
            _promptPara.FontSize = 16;
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
        /// This paragraphs shows the move made by the user if it was not found
        /// in the Workbook.
        /// </summary>
        /// <param name="nd"></param>
        private void BuildMoveNotInWorkbookText(TreeNode nd)
        {
            Paragraph para = AddNewParagraphToDoc("normal", "Your move ");

            Run r = new Run(nd.GetPlyText(true));
            r.FontWeight = FontWeights.Bold;
            r.FontSize = 16;
            para.Inlines.Add(r);

            para.Inlines.Add(new Run(" was not found in the Workbook. \n\nChoose how to proceed by clicking on one of the options highlighted below. \n"));
            para.Inlines.Add(new Run("You can run evaluations (blue highlights) before selecting the move to go ahead with (green highlights). \n\n"));

            RemoveIntroParas();

            para.Inlines.Add(new Run("The move you made:\n"));
            para.Inlines.Add(new Run("      "));

            Run mv = new Run(_userMove.GetPlyText(true) + "  :  ");
            mv.FontWeight = FontWeights.Bold;
            para.Inlines.Add(mv);

            string evalButtonText = " evaluate only ";
            para.Inlines.Add(CreateButtonRun(evalButtonText, _run_eval_user_move, ButtonStyle.BLUE));
            para.Inlines.Add(new Run("   or   "));
            para.Inlines.Add(CreateButtonRun("play it, starting a game against the engine", _run_play_engine, ButtonStyle.GREEN));
        }

        /// <summary>
        /// This paragraph is built when the user made a move not
        /// found in the Workbook and we are showing them what's in
        /// the Workbook.
        /// </summary>
        /// <param name="moves"></param>
        private void BuildWorkbookMovesText(string moves)
        {
            RemoveIntroParas();

            if (!string.IsNullOrEmpty(moves))
            {
                if (_workbookMovesPara != null)
                {
                    _workbookMovesPara.Inlines.Clear();
                }

                _workbookMovesPara = AddNewParagraphToDoc("normal", "Moves from the Workbook:\n");

                foreach (TreeNode nd in _movesFromWorkbook)
                {
                    _workbookMovesPara.Inlines.Add(new Run("      "));

                    Run r = new Run(nd.GetPlyText(true) + "  :  ");
                    r.FontWeight = FontWeights.Bold;
                    _workbookMovesPara.Inlines.Add(r);

                    string evalButtonText = " evaluate only ";
                    _workbookMovesPara.Inlines.Add(CreateButtonRun(evalButtonText, _run_eval_wb_move_ + nd.NodeId.ToString(), ButtonStyle.BLUE));
                    _workbookMovesPara.Inlines.Add(new Run("   or   "));
                    _workbookMovesPara.Inlines.Add(CreateButtonRun("go ahead and play it", _run_play_wb_move_ + nd.NodeId.ToString(), ButtonStyle.GREEN));
                    _workbookMovesPara.Inlines.Add(new Run("\n"));
                }
            }
        }

        /// <summary>
        /// Builds a paragraph with Workbook moves
        /// other than the move made by the user.
        /// It is used when the user makes a move
        /// found in the Workbook.
        /// </summary>
        /// <param name="moves"></param>
        private void BuildOtherWorkbookMovesText(string moves)
        {
            if (!string.IsNullOrEmpty(moves))
            {
                AddNewParagraphToDoc("normal", "Other Workbook moves: " + moves);
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
                
                _nodeIdUnderEvaluation = nodeId;
                // find Run for the same NodeId with "play" option,
                // we want to remember the reference so that we can append
                // the evaluation results run after it
                _underEvaluationRun = GetPlayRunForNodeId(nodeId);

                AppState.MainWin.RequestMoveEvaluationInTraining(nodeId);
            }
            else if (r.Name == _run_eval_user_move)
            {
                AppState.MainWin.RequestMoveEvaluationInTraining(_userMove);
            }
            else if (r.Name.StartsWith(_run_play_wb_move_))
            {
                int nodeId = int.Parse(r.Name.Substring(_run_eval_wb_move_.Length));
                EngineGame.ReplaceCurrentWithWorkbookMove(nodeId);
                TreeNode userChoiceNode = AppState.MainWin.Workbook.GetNodeFromNodeId(nodeId);
                SoundPlayer.PlayMoveSound(userChoiceNode.LastMoveAlgebraicNotation);
                _userChoiceNodeId = nodeId;

                AppState.MainWin.DisplayPosition(userChoiceNode.Position);

                TreeNode nd = AppState.MainWin.Workbook.SelectRandomChild(nodeId);

                // Selecting a random response to the user's choice from the Workbook
                EngineGame.AddPlyToGame(nd);

                // The move will be visualized in response to CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE timer's elapsed event
                EngineGame.TrainingWorkbookMoveMade = true;
                AppState.MainWin.Timers.Start(AppTimers.TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE);
            }
            else if (r.Name == _run_play_engine)
            {
                AppState.MainWin.PlayComputer(_userMove, true);
            }
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
            string runName = _run_play_wb_move_ + nodeId.ToString();
            return _workbookMovesPara.Inlines.FirstOrDefault(x => x.Name == runName) as Run;
        }

        /// <summary>
        /// Removes the introductory paragraphs
        /// that are shown only at the start of the session.
        /// </summary>
        private void RemoveIntroParas()
        {
            if (_promptPara != null)
            {
                Document.Blocks.Remove(_promptPara);
                _promptPara = null;
            }
            if (_instructionPara != null)
            {
                Document.Blocks.Remove(_instructionPara);
                _instructionPara = null;
            }
        }
    }
}

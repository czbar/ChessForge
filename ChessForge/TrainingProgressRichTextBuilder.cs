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
    public class TrainingProgressRichTextBuilder : RichTextBuilder
    {
        /// <summary>
        /// A reference to the paragraph with the initial instruction 
        /// to make a move on the chessboard. This reference will be used
        /// to removed this paragraph when no longer relevant.
        /// </summary>
        private Paragraph _prompt;

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
        /// Names and prefixes for the clickable Runs.
        /// </summary>
        private readonly string _run_eval_user_move = "user_eval";
        private readonly string _run_play_engine = "play_engine";
        private readonly string _run_eval_wb_move_ = "wb_eval_";
        private readonly string _run_play_wb_move_ = "wb_play_";
        private readonly string _run_continue = "wb_continue";

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
            ["intro"] = new RichTextPara(0, 0, 14, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(0, 0, 0)), TextAlignment.Left),
            ["prefix_line"] = new RichTextPara(0, 10, 14, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Left),
            ["eval_results"] = new RichTextPara(30, 5, 14, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(51, 159, 141)), TextAlignment.Left),
            ["normal"] = new RichTextPara(10, 5, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Left),
            ["default"] = new RichTextPara(10, 5, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(128, 98, 63)), TextAlignment.Left),
        };

        /// <summary>
        /// Builds text for intorductory paragraphs.
        /// </summary>
        /// <param name="node"></param>
        public void BuildIntroText(TreeNode node)
        {
            Document.Blocks.Clear();
            BuildHeaderText();
            BuildPrefixText(node);
            BuildInitialPromptText();
        }

        /// <summary>
        /// Gets the last move from the EngineGame.Line and finds
        /// its parent in the Workbook.
        /// 
        /// NOTE: If the parent is not in the Workbook, this method should
        /// not have been invoked as the TrainingMode should know that
        /// we are "out of the book".
        /// 
        /// Having found the parent check if the user's move corresponds to any move
        /// in the Workbook (children of that parent and report accordingly)
        /// </summary>
        public void ReportLastMoveVsWorkbook()
        {
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

            BuildContinueButton();
        }

        private void BuildContinueButton()
        {
            Paragraph para = AddNewParagraphToDoc("intro", "");
            para.Inlines.Add("        ");
            Run r = CreateButtonRun("Continue", _run_continue, ButtonStyle.GREEN);
            r.FontSize = 16;
            para.Inlines.Add(r);
        }

        private void BuildHeaderText()
        {
            AddNewParagraphToDoc("intro", "You have started training from the position arising after:");
        }

        private void BuildPrefixText(TreeNode node)
        {
            AddNewParagraphToDoc("prefix_line", GetStemLineText(node));
        }

        /// <summary>
        /// Initial prompt to advise the user make their move.
        /// This paragraph is removed later on to reduce clutter.
        /// </summary>
        private void BuildInitialPromptText()
        {
            _prompt = AddNewParagraphToDoc("intro", "Make your move on the chessboard");
        }

        private void BuildMoveFromWorkbookText(TreeNode nd)
        {
            AddNewParagraphToDoc("normal", "  (" + nd.GetPlyText(true) + " is in the Workbook.)");
        }

        private void BuildMoveNotInWorkbookText(TreeNode nd)
        {
            Paragraph para = AddNewParagraphToDoc("normal", "Your move ");

            Run r = new Run(nd.GetPlyText(true));
            r.FontWeight = FontWeights.Bold;
            r.FontSize = 16;
            para.Inlines.Add(r);

            para.Inlines.Add(new Run(" was not found in the Workbook. \n"));
            para.Inlines.Add(new Run("You can click below to either evaluate it or start a game against the engine from this move.\n"));

            if (_prompt != null)
            {
                Document.Blocks.Remove(_prompt);
                _prompt = null;
            }

            para.Inlines.Add(new Run("      "));
            para.Inlines.Add(CreateButtonRun("Evaluate", _run_eval_user_move, ButtonStyle.BLUE));
            para.Inlines.Add(new Run("   or   "));
            para.Inlines.Add(CreateButtonRun("try against the engine", _run_play_engine, ButtonStyle.GREEN));
        }

        private void BuildWorkbookMovesText(string moves)
        {
            if (!string.IsNullOrEmpty(moves))
            {
                Paragraph para = AddNewParagraphToDoc("normal", "Alternatively, change your move to one from the Workbook by clicking on it below.\n");

                foreach (TreeNode nd in _movesFromWorkbook)
                {
                    para.Inlines.Add(new Run("      "));
                    para.Inlines.Add(CreateButtonRun("Evaluate", _run_eval_wb_move_ + nd.NodeId.ToString(), ButtonStyle.BLUE));
                    para.Inlines.Add(new Run("   or   "));
                    para.Inlines.Add(CreateButtonRun("play " + MoveUtils.BuildSingleMoveText(nd), _run_play_wb_move_ + nd.NodeId.ToString(), ButtonStyle.GREEN));
                    para.Inlines.Add(new Run(";\n"));
                }
            }
        }

        private void BuildOtherWorkbookMovesText(string moves)
        {
            if (!string.IsNullOrEmpty(moves))
            {
                AddNewParagraphToDoc("normal", "Other Workbook moves: " + moves);
            }
        }

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
                AppState.MainWin.RequestMoveEvaluationInTraining(nodeId);
            }
            else if (r.Name.StartsWith(_run_eval_user_move))
            {
                AppState.MainWin.RequestMoveEvaluationInTraining(_userMove);
            }
            else if (r.Name.StartsWith(_run_play_wb_move_))
            {
                int nodeId = int.Parse(r.Name.Substring(_run_eval_wb_move_.Length));
                EngineGame.ReplaceCurrentWithWorkbookMove(nodeId);
                _userChoiceNodeId = nodeId;
                // Selecting a random response to the user's choice from the Workbook
                // TODO: set the flag so the main window can pick the move up and proceed.
                TreeNode nd = AppState.MainWin.Workbook.SelectRandomChild(nodeId);
            }
            else if (r.Name.StartsWith(_run_play_engine))
            {
                AppState.MainWin.PlayComputer(_userMove, true);
            }
            else if (r.Name.StartsWith(_run_continue))
            {
            }
        }
    }
}

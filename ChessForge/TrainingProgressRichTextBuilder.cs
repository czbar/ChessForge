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
        public TrainingProgressRichTextBuilder(FlowDocument doc) : base(doc)
        {
        }

        override internal Dictionary<string, RichTextPara> RichTextParas { get { return _richTextParas; } }

        /// <summary>
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        internal Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            ["intro"] = new RichTextPara(0,  0, 14, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(0, 0, 0) ), TextAlignment.Left),
            ["prefix_line"] = new RichTextPara(0, 10, 14, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Left),
            ["eval_results"] = new RichTextPara(30, 5, 14, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(51, 159, 141)), TextAlignment.Left),
            ["normal"] = new RichTextPara(10, 5, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Left),
            ["default"] = new RichTextPara(10, 5, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(128, 98, 63)), TextAlignment.Left),
        };

        public void BuildIntroText(TreeNode node)            
        {
            Document.Blocks.Clear();
            BuildHeaderText();
            BuildPrefixText(node);
            BuildInitialPromptText();
        }

        private void BuildHeaderText()
        {
            AddNewParagraphToDoc("intro", "You have started training from the position arising after:");
        }

        private void BuildPrefixText(TreeNode node)
        {
            AddNewParagraphToDoc("prefix_line", GetStemLineText(node));
        }

        private void BuildInitialPromptText()
        {
            AddNewParagraphToDoc("intro", "Make your move on the chessboard");

            // insert dummy para to create extra spacing
            AddNewParagraphToDoc("intro", "");
        }

        public void BuildMoveFromWorkbookText(TreeNode nd)
        {
            AddNewParagraphToDoc("normal", "  (" + nd.GetPlyText(true) + " is in the Workbook.)");
        }

        public void BuildMoveNotInWorkbookText(TreeNode nd)
        {
            AddNewParagraphToDoc("normal", "Your move " + nd.GetPlyText(true) + " has not been found in the Workbook.");
        }

        public void BuildWorkbookMovesText(string moves)
        {
            if (!string.IsNullOrEmpty(moves))
            {
                AddNewParagraphToDoc("normal", "From the Workbook: " + moves);
            }
        }

        public void BuildOtherWorkbookMovesText(string moves)
        {
            if (!string.IsNullOrEmpty(moves))
            {
                AddNewParagraphToDoc("normal", "Other Workbook moves: " + moves);
            }
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
            TreeNode userMove = EngineGame.GetCurrentNode();
            TreeNode parent = userMove.Parent;

            // double check that we have the parent in our Workbook
            if (AppState.MainWin.Workbook.Nodes.First(x => x.NodeId == parent.NodeId) == null)
            {
                return;
            }

            int childCount = parent.Children.Count;
            StringBuilder wbMoves = new StringBuilder();
            TreeNode foundMove = null;
            foreach (TreeNode child in parent.Children)
            {
                if (child.LastMoveAlgebraicNotation == userMove.LastMoveAlgebraicNotation)
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
                }
            }

            if (foundMove != null)
            {
                BuildMoveFromWorkbookText(foundMove);
                BuildOtherWorkbookMovesText(wbMoves.ToString());
            }
            else
            {
                BuildMoveNotInWorkbookText(userMove);
                BuildWorkbookMovesText(wbMoves.ToString());
            }
        }

    }
}

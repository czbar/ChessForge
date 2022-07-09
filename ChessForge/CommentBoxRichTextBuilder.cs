using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Timers;
using System.Text;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ChessForge;
using ChessPosition;
using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Manages the RichTextBox's FlowDocument that displays hints to the user next to (below) the 
    /// main chess board.
    /// This includes e.g. prompts to make a move or wait for the engine's move when
    /// playing against the computer.
    /// </summary>
    public class CommentBoxRichTextBuilder : RichTextBuilder
    {
        public CommentBoxRichTextBuilder(FlowDocument doc) : base(doc)
        {
        }

        override internal Dictionary<string, RichTextPara> RichTextParas { get { return _richTextParas; } }

        /// <summary>
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        internal Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            ["title"]         = new RichTextPara( 0, 10, 24, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(0, 0, 0)), TextAlignment.Center),
            ["bold_prompt"]   = new RichTextPara( 0, 5, 14, FontWeights.Bold,   new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Center),
            ["eval_results"]  = new RichTextPara( 30, 5, 14, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(51, 159, 141)), TextAlignment.Center),
            ["normal"]        = new RichTextPara(  0, 0, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Center),
            ["normal_14"]     = new RichTextPara(0, 0, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(120, 61, 172)), TextAlignment.Center),
            ["default"]       = new RichTextPara( 10, 5, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(128, 98, 63)), TextAlignment.Center),
            ["dummy"]         = new RichTextPara(0, 16, 10, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(0, 0, 0)), TextAlignment.Center),
            ["bold_16"]       = new RichTextPara(0, 0,  16, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Center),
            ["bold_18"]       = new RichTextPara(0, 5, 18, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(69, 89, 191)), TextAlignment.Center),
        };

        /// <summary>
        /// Displays the last move made by the user.
        /// </summary>
        /// <param name="nd"></param>
        public void GameMoveMade(TreeNode nd, bool userMove)
        {
            Document.Blocks.Clear();
            AddNewParagraphToDoc("dummy", "");

            if (userMove)
            {
                AddNewParagraphToDoc("normal", "Your move was:");
                AddNewParagraphToDoc("bold_prompt", MoveUtils.BuildSingleMoveText(nd));
                AddNewParagraphToDoc("normal", "Wait for engine's response...");
            }
            else // engine moved
            {
                AddNewParagraphToDoc("normal", "The engine played:");
                AddNewParagraphToDoc("bold_16", MoveUtils.BuildSingleMoveText(nd));
                AddNewParagraphToDoc("normal", "It is your turn now.");
            }
        }

        /// <summary>
        /// The main message when a new workbook was loaded or when nothing
        /// more relevant is to be shown.
        /// </summary>
        /// <param name="title"></param>
        public void ShowWorkbookTitle(string title)
        {
            Document.Blocks.Clear();

            AddNewParagraphToDoc("title", string.IsNullOrWhiteSpace(title) ? "Untitled Workbook" : title);
            AddNewParagraphToDoc("bold_prompt", "Some available actions are:");
            AddNewParagraphToDoc("normal", Strings.QUICK_INSTRUCTION);
        }

        /// <summary>
        /// Invoked when the game replay stops to revert to showing
        /// the workbook title message.
        /// </summary>
        public void RestoreTitleMessage()
        {
            ShowWorkbookTitle(AppState.MainWin.Workbook.Title);
        }

        public void GameReplayStart()
        {
            Document.Blocks.Clear();

            AddNewParagraphToDoc("dummy", "");
            AddNewParagraphToDoc("bold_18", "Auto-replay in progress ...");
            AddNewParagraphToDoc("normal", "Click any move to stop.");
        }

        public void TrainingSessionStart()
        {
            Document.Blocks.Clear();
            AddNewParagraphToDoc("dummy", "");
            AddNewParagraphToDoc("bold_16", "The training session has started.");
            AddNewParagraphToDoc("normal_14", "Make your move and watch the comments in the Workbook view to the right of this chessboard.");
        }

    }
}

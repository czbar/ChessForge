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
    public class MainboardCommentBox : RichTextBuilder
    {
        public MainboardCommentBox(FlowDocument doc) : base(doc)
        {
        }

        override internal Dictionary<string, RichTextPara> RichTextParas { get { return _richTextParas; } }

        /// <summary>
        /// Layout definitions for paragraphs at different levels.
        /// </summary>
        internal Dictionary<string, RichTextPara> _richTextParas = new Dictionary<string, RichTextPara>()
        {
            ["intro"] = new RichTextPara(0, 0, 14, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(0, 0, 0))),
            ["prefix_line"] = new RichTextPara(0, 10, 14, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(69, 89, 191))),
            ["eval_results"] = new RichTextPara(30, 5, 14, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(51, 159, 141))),
            ["normal"] = new RichTextPara(10, 5, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(120, 61, 172))),
            ["default"] = new RichTextPara(10, 5, 12, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(128, 98, 63))),
        };

        /// <summary>
        /// The main message when a new workbook was loaded or when nothing
        /// more relevant is to be shown.
        /// </summary>
        /// <param name="title"></param>
        public void ShowWorkbookTitle(string title)
        {
            Document.Blocks.Clear();

            Paragraph para = new Paragraph();
            para.FontSize = 24;
            para.FontWeight = FontWeights.Bold;
            para.TextAlignment = TextAlignment.Center;
            para.Margin = new Thickness(0, 0, 0, 10);

            string titleToShow = string.IsNullOrWhiteSpace(title) ? "Untitled Workbook" : title;
            para.Inlines.Add(new Run(titleToShow));
            Document.Blocks.Add(para);

            Document.Blocks.Add(CreateTextParagraph("Some available actions are:", FontWeights.Bold, 14, 5));

            string strInstruction = Strings.QUICK_INSTRUCTION;
            Document.Blocks.Add(CreateTextParagraph(strInstruction, FontWeights.Normal));
        }

        /// <summary>
        /// Invoked when the game replay stops to revert to showing
        /// the workbook title message.
        /// </summary>
        public void GameReplayStop()
        {
            ShowWorkbookTitle(AppState.MainWin.Workbook.Title);
        }

        public void GameReplayStart()
        {
            Document.Blocks.Clear();

            Paragraph dummyPara = new Paragraph();
            dummyPara.Margin = new Thickness(0, 0, 0, 16);
            Document.Blocks.Add(dummyPara);

            Paragraph line_1 = new Paragraph();
            line_1.TextAlignment = TextAlignment.Center;
            line_1.Margin = new Thickness(0, 0, 0, 0);

            Run r = new Run("Auto-replay in progress ...");
            r.FontStyle = FontStyles.Normal;
            r.FontSize = 18;
            r.FontWeight = FontWeights.Bold;

            line_1.Inlines.Add(r);
            Document.Blocks.Add(line_1);

            Paragraph line_2 = new Paragraph();
            line_2.TextAlignment = TextAlignment.Center;
            line_2.Margin = new Thickness(0, 0, 0, 0);

            r = new Run("Click any move to stop.");
            r.FontStyle = FontStyles.Normal;
            r.FontSize = 12;
            r.FontWeight = FontWeights.Regular;

            line_2.Inlines.Add(r);
            Document.Blocks.Add(line_2);
        }

        public void TrainingSessionStart()
        {
            Document.Blocks.Clear();

            Paragraph dummyPara = new Paragraph();
            dummyPara.Margin = new Thickness(0, 0, 0, 10);
            Document.Blocks.Add(dummyPara);

            Paragraph line_1 = new Paragraph();
            line_1.TextAlignment = TextAlignment.Center;
            line_1.Margin = new Thickness(0, 0, 0, 0);

            Run r = new Run("The training session has started.");
            r.FontStyle = FontStyles.Normal;
            r.FontSize = 18;
            r.FontWeight = FontWeights.Regular;

            line_1.Inlines.Add(r);
            Document.Blocks.Add(line_1);

            Paragraph line_2 = new Paragraph();
            line_2.TextAlignment = TextAlignment.Center;
            line_2.Margin = new Thickness(0, 0, 0, 0);

            r = new Run("Make your move and watch the comments in the Workbook view to the right of this chessboard.");
            r.FontStyle = FontStyles.Normal;
            r.FontSize = 20;
            r.FontWeight = FontWeights.Bold;

            line_2.Inlines.Add(r);
            Document.Blocks.Add(line_2);
        }

        /// <summary>
        /// Creates a paragraph with specified text and attributes.
        /// Adds it to the Box's FlowDocument.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="weight"></param>
        /// <param name="fontSize"></param>
        /// <param name="bottomMargin"></param>
        /// <returns></returns>
        private Paragraph CreateTextParagraph(string text, FontWeight weight, int fontSize = 12, int bottomMargin = 0)
        {
            Paragraph para = new Paragraph();
            para.FontSize = fontSize;
            para.FontWeight = weight;
            para.TextAlignment = TextAlignment.Center;
            para.Margin = new Thickness(0, 0, 0, bottomMargin);

            para.Inlines.Add(new Run(text));
            Document.Blocks.Add(para);

            return para;
        }
    }
}

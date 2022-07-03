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
    /// Manages the RichTextBox that displays hints to the user next to (below) the 
    /// main chess board.
    /// This includes e.g. prompts to make a move or wait for the engine's move when
    /// playing against the computer.
    /// </summary>
    public class MainboardCommentBox
    {
        /// <summary>
        /// RichTextBox control managed by this object
        /// </summary>
        private RichTextBox _rtbCommentBox;

        /// <summary>
        /// Initializez the object and sets the reference to the UI element.
        /// </summary>
        /// <param name="rtb"></param>
        public MainboardCommentBox(RichTextBox rtb)
        {
            _rtbCommentBox = rtb;
        }

        /// <summary>
        /// The main message when a new workbook was loaded or when nothing
        /// more relevant is to be shown.
        /// </summary>
        /// <param name="title"></param>
        public void ShowWorkbookTitle(string title)
        {
            _rtbCommentBox.Document.Blocks.Clear();

            Paragraph para = new Paragraph();
            para.FontSize = 24;
            para.FontWeight = FontWeights.Bold;
            para.TextAlignment = TextAlignment.Center;
            para.Margin = new Thickness(0, 0, 0, 10);

            string titleToShow = string.IsNullOrWhiteSpace(title) ? "Untitled Workbook" : title;
            para.Inlines.Add(new Run(titleToShow));
            _rtbCommentBox.Document.Blocks.Add(para);

            _rtbCommentBox.Document.Blocks.Add(CreateTextParagraph("Some available actions are:", FontWeights.Bold, 14, 5));

            string strInstruction = Strings.QUICK_INSTRUCTION;
            _rtbCommentBox.Document.Blocks.Add(CreateTextParagraph(strInstruction, FontWeights.Normal));
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
            _rtbCommentBox.Document.Blocks.Clear();

            Paragraph dummyPara = new Paragraph();
            dummyPara.Margin = new Thickness(0, 0, 0, 16);
            _rtbCommentBox.Document.Blocks.Add(dummyPara);

            Paragraph line_1 = new Paragraph();
            line_1.TextAlignment = TextAlignment.Center;
            line_1.Margin = new Thickness(0, 0, 0, 0);

            Run r = new Run("Auto-replay in progress ...");
            r.FontStyle = FontStyles.Normal;
            r.FontSize = 18;
            r.FontWeight = FontWeights.Bold;

            line_1.Inlines.Add(r);
            _rtbCommentBox.Document.Blocks.Add(line_1);

            Paragraph line_2 = new Paragraph();
            line_2.TextAlignment = TextAlignment.Center;
            line_2.Margin = new Thickness(0, 0, 0, 0);

            r = new Run("Click any move to stop.");
            r.FontStyle = FontStyles.Normal;
            r.FontSize = 12;
            r.FontWeight = FontWeights.Regular;

            line_2.Inlines.Add(r);
            _rtbCommentBox.Document.Blocks.Add(line_2);
        }

        public void TrainingSessionStart()
        {
            _rtbCommentBox.Document.Blocks.Clear();

            Paragraph dummyPara = new Paragraph();
            dummyPara.Margin = new Thickness(0, 0, 0, 10);
            _rtbCommentBox.Document.Blocks.Add(dummyPara);

            Paragraph line_1 = new Paragraph();
            line_1.TextAlignment = TextAlignment.Center;
            line_1.Margin = new Thickness(0, 0, 0, 0);

            Run r = new Run("The training session has started.");
            r.FontStyle = FontStyles.Normal;
            r.FontSize = 18;
            r.FontWeight = FontWeights.Regular;

            line_1.Inlines.Add(r);
            _rtbCommentBox.Document.Blocks.Add(line_1);

            Paragraph line_2 = new Paragraph();
            line_2.TextAlignment = TextAlignment.Center;
            line_2.Margin = new Thickness(0, 0, 0, 0);

            r = new Run("Your move!");
            r.FontStyle = FontStyles.Normal;
            r.FontSize = 20;
            r.FontWeight = FontWeights.Bold;

            line_2.Inlines.Add(r);
            _rtbCommentBox.Document.Blocks.Add(line_2);
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
            _rtbCommentBox.Document.Blocks.Add(para);

            return para;
        }

    }
}

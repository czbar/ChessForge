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
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The main message when a new workbook was loaded or when nothing
        /// more relevant is to be shown.
        /// </summary>
        /// <param name="title"></param>
        public void rtbBoardComment_ShowWorkbookTitle(string title)
        {
            rtbBoardComment.Document.Blocks.Clear();

            Paragraph para = new Paragraph();
            para.FontSize = 24;
            para.FontWeight = FontWeights.Bold;
            para.TextAlignment = TextAlignment.Center;
            para.Margin = new Thickness(0, 0, 0, 10);

            string titleToShow = string.IsNullOrWhiteSpace(title) ? "Untitled Workbook" : title;
            para.Inlines.Add(new Run(titleToShow));
            rtbBoardComment.Document.Blocks.Add(para);

            rtbBoardComment.Document.Blocks.Add(CreateTextParagraph("Some available actions are:", FontWeights.Bold, 14, 5));

            string strInstruction = Strings.QUICK_INSTRUCTION;
            rtbBoardComment.Document.Blocks.Add(CreateTextParagraph(strInstruction, FontWeights.Normal));
        }

        /// <summary>
        /// Invoked when the game replay stops to revert to showing
        /// the workbook title message.
        /// </summary>
        public void rtbBoardComment_GameReplayStop()
        {
            rtbBoardComment_ShowWorkbookTitle(Workbook.Title);
        }

        public void rtbBoardComment_GameReplayStart()
        {
            rtbBoardComment.Document.Blocks.Clear();

            Paragraph dummyPara = new Paragraph();
            dummyPara.Margin = new Thickness(0, 0, 0, 16);
            rtbBoardComment.Document.Blocks.Add(dummyPara);

            Paragraph line_1 = new Paragraph();
            line_1.TextAlignment = TextAlignment.Center;
            line_1.Margin = new Thickness(0, 0, 0, 0);

            Run r = new Run("Auto-replay in progress ...");
            r.FontStyle = FontStyles.Normal;
            r.FontSize = 18;
            r.FontWeight = FontWeights.Bold;

            line_1.Inlines.Add(r);
            rtbBoardComment.Document.Blocks.Add(line_1);

            Paragraph line_2 = new Paragraph();
            line_2.TextAlignment = TextAlignment.Center;
            line_2.Margin = new Thickness(0, 0, 0, 0);

            r = new Run("Click any move to stop.");
            r.FontStyle = FontStyles.Normal;
            r.FontSize = 12;
            r.FontWeight = FontWeights.Regular;

            line_2.Inlines.Add(r);
            rtbBoardComment.Document.Blocks.Add(line_2);
        }

        public void rtbBoardComment_TrainingSessionStart()
        {
            rtbBoardComment.Document.Blocks.Clear();

            Paragraph dummyPara = new Paragraph();
            dummyPara.Margin = new Thickness(0, 0, 0, 10);
            rtbBoardComment.Document.Blocks.Add(dummyPara);

            Paragraph line_1 = new Paragraph();
            line_1.TextAlignment = TextAlignment.Center;
            line_1.Margin = new Thickness(0, 0, 0, 0);

            Run r = new Run("The training session has started.");
            r.FontStyle = FontStyles.Normal;
            r.FontSize = 18;
            r.FontWeight = FontWeights.Regular;

            line_1.Inlines.Add(r);
            rtbBoardComment.Document.Blocks.Add(line_1);

            Paragraph line_2 = new Paragraph();
            line_2.TextAlignment = TextAlignment.Center;
            line_2.Margin = new Thickness(0, 0, 0, 0);

            r = new Run("Your move!");
            r.FontStyle = FontStyles.Normal;
            r.FontSize = 20;
            r.FontWeight = FontWeights.Bold;

            line_2.Inlines.Add(r);
            rtbBoardComment.Document.Blocks.Add(line_2);
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
            rtbBoardComment.Document.Blocks.Add(para);

            return para;
        }

    }
}

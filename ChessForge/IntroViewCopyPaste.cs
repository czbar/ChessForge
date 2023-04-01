using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ChessForge
{
    public partial class IntroView : RichTextBuilder
    {
        /// <summary>
        /// Pastes stored selection into the view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Paste(object sender, RoutedEventArgs e)
        {
            _rtb.Paste();
        }

        /// <summary>
        /// Calls the built-in Undo and the calls 
        /// SetEventHandlers so that event associations are restored
        /// if any Diagram or Move was part of Undo.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Undo(object sender, RoutedEventArgs e)
        {
            _rtb.Undo();
            SetEventHandlers();
        }

        /// <summary>
        /// Stores the current selection before calling
        /// the built-in cut operation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Cut(object sender, RoutedEventArgs e)
        {
            _rtb.Cut();
        }

        /// <summary>
        /// Stores the current selection in custom format
        /// so that it can get properly restored.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Copy(object sender, RoutedEventArgs e)
        {
            try
            {
                TextSelection selection = _rtb.Selection;
                TextRange textRange = new TextRange(selection.Start, selection.End);

                TextPointer start = selection.Start;
                TextPointer end = selection.End;

                //TextPointer position = start.GetNextInsertionPosition(LogicalDirection.Forward);
                //position = position.GetNextInsertionPosition(LogicalDirection.Forward);
                //UiRtbIntroView.Selection.Select(start, position);

                _rtb.Copy();
                string s = Clipboard.GetText();
                Object o = Clipboard.GetData(DataFormats.Xaml);
                //List<InlineUIContainer> lst = GetInlineUIElements(textRange);

                Run r1 = new Run("a111");
                r1.FontWeight = FontWeights.Bold;

                Paragraph paragraph = new Paragraph();
                paragraph.Inlines.Add(r1);

                _rtb.Document.Blocks.Add(paragraph);
            }
            catch { }
        }

        /// <summary>
        /// Gets selected inlines. TBD
        /// </summary>
        private void StoreSelection()
        {
            RichTextBox richTextBox = _rtb;

            // Get the selected paragraphs in the RichTextBox
            List<Paragraph> selectedParagraphs = new List<Paragraph>();
            TextPointer start = richTextBox.Selection.Start;
            TextPointer end = richTextBox.Selection.End;
            TextPointer position = start.GetNextInsertionPosition(LogicalDirection.Forward);

            while (position != null && position.CompareTo(end) < 0)
            {
                if (position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart && position.Parent is Paragraph)
                {
                    Paragraph paragraph = position.Parent as Paragraph;

                    if (start.CompareTo(paragraph.ContentStart) <= 0 && end.CompareTo(paragraph.ContentEnd) >= 0)
                    {
                        selectedParagraphs.Add(paragraph);
                    }
                }

                position = position.GetNextContextPosition(LogicalDirection.Forward);
                //                position = position.GetNextInsertionPosition(LogicalDirection.Forward);
            }
        }

        /// <summary>
        /// Builds a list of inlines from the passed TextRange. TBD
        /// This will be called when the user performs a copy of the current selection
        /// in th eIntro tab.
        /// We expect the following types of Inlines:
        /// - Paragraphs
        /// - Runs
        /// - Text Blocks (moves)
        /// - InlineUIContainers (diagrams)
        /// 
        /// Text Blocks and InlineUiContainers will no be split.
        /// If the start or and of the range falls inside a Run a new Run will be created with the part of text
        /// that is within the section.
        /// A paragraph with an InlineUiContainers will not be split, but if it has Runs, it will be recreated with 
        /// only the elements that are in the curent selection.
        /// </summary>
        /// <param name="textRange"></param>
        /// <returns></returns>
        private List<Inline> GetInlinesFromTextRange(TextRange textRange)
        {
            List<Inline> lstInlines = new List<Inline>();

            // Iterate over the Inline elements in the TextRange
            for (Inline inline = textRange.Start.Parent as Inline; inline != null; inline = inline.NextInline)
            {
                // if this is a paragraph with a diagram, save it whole, otherwise create a new one and copy all inlines to it
                if (inline.Parent is Paragraph)
                {
                    //GetInlinesFormParagraph(ref lstInlines);
                }
                lstInlines.Add(inline);
            }

            return lstInlines;
        }

    }
}

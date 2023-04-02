using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                IntroViewClipboard.Clear();
                CopySelectionToClipboard();
                _rtb.Copy();
            }
            catch { }
        }

        /// <summary>
        /// Builds a list of elements in the current selection. 
        /// This will be called when the user requests a copy of the current selection
        /// in the Intro tab.
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
        private void CopySelectionToClipboard()
        {
            TextPointer position = _rtb.Selection.Start;
            TextPointer end = _rtb.Selection.End;

            while (position.CompareTo(end) < 0)
            {
                switch (position.GetPointerContext(LogicalDirection.Forward))
                {
                    case TextPointerContext.ElementStart:
                        if (position.Parent is Paragraph)
                        {
                            IntroViewClipboard.AddParagraph(position.Parent as Paragraph);
                        }
                        break;
                    case TextPointerContext.ElementEnd:
                        break;
                    case TextPointerContext.EmbeddedElement:
                        if (position.Parent is TextElement)
                        {
                            string name = ((TextElement)position.Parent).Name;
                            int nodeId = TextUtils.GetIdFromPrefixedString(name);
                            TreeNode node = GetNodeById(nodeId);
                            if (name.StartsWith(_uic_move_))
                            {
                                IntroViewClipboard.AddMove(node);
                            }
                        }
                        break;
                    case TextPointerContext.Text:
                        Run r = (Run)position.Parent;
                        if (r.ElementEnd.CompareTo(end) > 0)
                        {
                            TextRange tr = new TextRange(position, end);
                            Run runCopy = IntroViewClipboard.CopyRun(r);
                            runCopy.Text = tr.Text;
                            IntroViewClipboard.AddRun(runCopy);
                        }
                        else
                        {
                            IntroViewClipboard.AddRun(r);
                        }
                        break;
                }

                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }
        }
    }

}

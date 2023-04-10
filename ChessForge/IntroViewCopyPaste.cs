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
    /// <summary>
    /// Functions handling Copy/Cut/Paste/Undo operations.
    /// </summary>
    public partial class IntroView : RichTextBuilder
    {
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
            IntroViewClipboard.Clear();
            CopySelectionToClipboard();
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
            }
            catch { }
        }

        /// <summary>
        /// Pastes stored selection into the view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Paste(object sender, RoutedEventArgs e)
        {
            try
            {
                // We will paste objects from the clipboard one by one

                // first delete the current selection if any
                if (!_rtb.Selection.IsEmpty)
                {
                    TextRange selection = new TextRange(_rtb.Selection.Start, _rtb.Selection.End);
                    selection.Text = "";
                }

                foreach (IntroViewClipboardElement element in IntroViewClipboard.Elements)
                {
                    switch (element.Type)
                    {
                        case IntroViewClipboard.ElementType.Paragraph:
                            Paragraph paragraph = element.DataObject as Paragraph;
                            TextPointer tp = InsertParagraphFromClipboard(paragraph);
                            _rtb.CaretPosition = tp;
                            break;
                        case IntroViewClipboard.ElementType.Diagram:
                            InsertDiagramFromClipboard(element.DataObject as TreeNode, element.BoolState == true);
                            break;
                        case IntroViewClipboard.ElementType.Run:
                            InsertRunFromClipboard(element.DataObject as Run);
                            break;
                        case IntroViewClipboard.ElementType.Move:
                            InsertMoveFromClipboard(element.DataObject as TreeNode);
                            break;
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Makes a copy of a paragraph from the clipboard
        /// and insert it at the current position.
        /// </summary>
        /// <param name="paraToInsert"></param>
        public TextPointer InsertParagraphFromClipboard(Paragraph paraToInsert)
        {
            Paragraph para = RichTextBoxUtilities.CopyParagraph(paraToInsert);
            return _rtb.CaretPosition.InsertParagraphBreak();
        }

        /// <summary>
        /// Creats a new diagram paragraph and inserts it.
        /// </summary>
        /// <param name="diagPara"></param>
        public void InsertDiagramFromClipboard(TreeNode nodeForDiag, bool isFlipped)
        {
            TreeNode nd = nodeForDiag.CloneMe(true);
            if (nd != null)
            {
                Paragraph para = InsertDiagram(nd, isFlipped);
                _rtb.CaretPosition = para.ElementEnd;
                _rtb.CaretPosition = _rtb.CaretPosition.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

        /// <summary>
        /// If parent is a run, split the run and insert this one in between.
        /// If parent is a Paragraph and it is not a diagram paragraph, insert it at the caret point.
        /// </summary>
        /// <param name="run"></param>
        private void InsertRunFromClipboard(Run runToInsert)
        {
            RichTextBoxUtilities.GetMoveInsertionPlace(_rtb, out Paragraph para, out Inline insertBefore);
            if (para != null)
            {
                Run run = RichTextBoxUtilities.CopyRun(runToInsert);
                if (insertBefore == null)
                {
                    para.Inlines.Add(run);
                }
                else
                {
                    para.Inlines.InsertBefore(insertBefore, run);
                }

                _rtb.CaretPosition = run.ElementEnd;
            }
        }

        /// <summary>
        /// Makes a copy of the passed node, inserts it into the Tree
        /// creates an InlineUiContainer for the Move and inserts it.
        /// </summary>
        /// <param name="nodeForMove"></param>
        private void InsertMoveFromClipboard(TreeNode nodeForMove)
        {
            TreeNode node = nodeForMove.CloneMe(true);
            InsertMove(node, true);
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

            Paragraph currParagraph = null;
            currParagraph = position.Paragraph;

            while (position.CompareTo(end) < 0)
            {
                switch (position.GetPointerContext(LogicalDirection.Forward))
                {
                    case TextPointerContext.ElementStart:
                        if (position.Parent is Paragraph)
                        {
                            Paragraph positionParent = position.Parent as Paragraph;
                            if (positionParent != currParagraph || RichTextBoxUtilities.IsDiagramPara(positionParent))
                            {
                                bool? flipped = null;
                                if (RichTextBoxUtilities.IsDiagramPara(positionParent))
                                {
                                    int nodeId = TextUtils.GetIdFromPrefixedString(positionParent.Name);
                                    TreeNode node = GetNodeById(nodeId);
                                    flipped = GetDiagramFlipState(positionParent);
                                    IntroViewClipboard.AddDiagram(node, flipped);
                                }
                                else
                                {
                                    IntroViewClipboard.AddParagraph(position.Parent as Paragraph);
                                }
                                currParagraph = position.Parent as Paragraph;
                            }
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
                            Run runCopy = RichTextBoxUtilities.CopyRun(r);
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

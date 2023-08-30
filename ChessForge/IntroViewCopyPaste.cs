using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
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
            
            // the call to _rtb.Cut() is convenient but we need to preserve
            // the content of the system clipboard
            string txt = SystemClipboard.GetText();
            _rtb.Cut();
            SystemClipboard.SetText(txt);
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
        /// If System Clipboard is not empty, takes text from it and clears Intro clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Paste(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!SystemClipboard.IsEmpty() && SystemClipboard.IsUpdated())
                {
                    IntroViewClipboard.Clear();
                    Run r = new Run(SystemClipboard.GetText());

                    Run currentRun = _rtb.CaretPosition.Parent as Run;
                    if (currentRun != null)
                    {
                        r.FontFamily = currentRun.FontFamily;
                        r.FontSize = currentRun.FontSize;
                        r.FontStyle = currentRun.FontStyle;
                        r.FontWeight = currentRun.FontWeight;
                    }

                    InsertRunFromClipboard(r, null);
                    return;
                }

                if (IntroViewClipboard.Elements.Count == 0)
                {
                    return;
                }

                AppState.IsDirty = true;

                // We will paste objects from the clipboard one by one

                // first delete the current selection if any
                if (!_rtb.Selection.IsEmpty)
                {
                    TextRange selection = new TextRange(_rtb.Selection.Start, _rtb.Selection.End);
                    selection.Text = "";
                }

                bool isFirstElem = true;

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
                            InsertRunFromClipboard(element.DataObject as Run, isFirstElem ? element.Margins : null);
                            break;
                        case IntroViewClipboard.ElementType.Move:
                            InsertMoveFromClipboard(element.DataObject as TreeNode);
                            break;
                    }

                    isFirstElem = false;
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
            Paragraph paraToInsertAfter = null;
            if (_rtb.CaretPosition.Paragraph != null)
            {
                paraToInsertAfter = _rtb.CaretPosition.Paragraph;
            }

            Paragraph para = RichTextBoxUtilities.CopyParagraph(paraToInsert);

            if (paraToInsertAfter != null)
            {
                _rtb.Document.Blocks.InsertAfter(paraToInsertAfter, para);
            }
            else
            {
                _rtb.Document.Blocks.Add(para);
            }
            return para.ContentEnd;
            //return _rtb.CaretPosition.InsertParagraphBreak();
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
        private void InsertRunFromClipboard(Run runToInsert, Thickness? margins)
        {
            RichTextBoxUtilities.GetMoveInsertionPlace(_rtb, out Paragraph para, out Inline insertBefore);
            if (para != null)
            {
                if (margins != null)
                {
                    para.Margin = margins.Value;
                }

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
        /// Builds a list of elements in the current selection and puts them in the clipboard
        /// If plainTextOnly is set to true, builds only the plain text representation of the
        /// entire document. 
        /// This will be called when the user requests a copy of the current selection in the Intro tab
        /// or upon exit to store the textual representation of the view in the comment of the root node.
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
        /// <param name="plainTextOnly"></param>
        private string CopySelectionToClipboard(bool plainTextOnly = false)
        {
            TextPointer position = _rtb.Selection.Start;
            TextPointer end = _rtb.Selection.End;

            if (plainTextOnly)
            {
                position = _rtb.Document.ContentStart;
                end = _rtb.Document.ContentEnd;
            }

            Paragraph currParagraph = position.Paragraph;

            StringBuilder plainText = new StringBuilder("");

            while (position.CompareTo(end) < 0)
            {
                switch (position.GetPointerContext(LogicalDirection.Forward))
                {
                    case TextPointerContext.ElementStart:
                    case TextPointerContext.ElementEnd:
                        if (position.Parent is Paragraph)
                        {
                            Paragraph positionParent = position.Parent as Paragraph;
                            if (positionParent != currParagraph) // || RichTextBoxUtilities.IsDiagramPara(positionParent))
                            {
                                bool? flipped = null;
                                if (RichTextBoxUtilities.IsDiagramPara(positionParent))
                                {
                                    int nodeId = TextUtils.GetIdFromPrefixedString(positionParent.Name);
                                    TreeNode node = GetNodeById(nodeId);
                                    flipped = GetDiagramFlipState(positionParent);
                                    if (!plainTextOnly)
                                    {
                                        IntroViewClipboard.AddDiagram(node, flipped);
                                    }
                                    plainText.AppendLine("");
                                    plainText.Append(RichTextBoxUtilities.GetDiagramPlainText(node));
                                }
                                else
                                {
                                    if (!plainTextOnly)
                                    {
                                        IntroViewClipboard.AddParagraph(position.Parent as Paragraph);
                                    }
                                    if (plainText.Length > 0)
                                    {
                                        plainText.AppendLine("");
                                        plainText.AppendLine("");
                                    }
                                }
                                currParagraph = position.Parent as Paragraph;
                            }
                        }
                        break;
                    case TextPointerContext.EmbeddedElement:
                        if (position.Parent is TextElement)
                        {
                            string name = ((TextElement)position.Parent).Name;
                            int nodeId = TextUtils.GetIdFromPrefixedString(name);
                            TreeNode node = GetNodeById(nodeId);
                            if (name.StartsWith(_uic_move_))
                            {
                                if (!plainTextOnly)
                                {
                                    IntroViewClipboard.AddMove(node);
                                }
                                plainText.Append(RichTextBoxUtilities.GetEmbeddedElementPlainText(node));
                            }
                        }
                        break;
                    case TextPointerContext.Text:
                        Run r = (Run)position.Parent;
                        Thickness? margins = null;
                        if (r.Parent is Paragraph para)
                        {
                            margins = para.Margin;
                        }

                        if (r.ElementEnd.CompareTo(end) > 0)
                        {
                            TextRange tr = new TextRange(position, end);
                            Run runCopy = RichTextBoxUtilities.CopyRun(r);
                            runCopy.Text = tr.Text;
                            if (!plainTextOnly)
                            {
                                IntroViewClipboard.AddRun(runCopy, margins);
                            }
                            plainText.Append(RichTextBoxUtilities.GetRunPlainText(runCopy));
                        }
                        else
                        {
                            if (!plainTextOnly)
                            {
                                IntroViewClipboard.AddRun(r, margins);
                            }
                            plainText.Append(RichTextBoxUtilities.GetRunPlainText(r));
                        }
                        break;
                }

                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }
            
            if (plainText.Length > 0)
            {
                SystemClipboard.Clear();
                SystemClipboard.SetText(plainText.ToString());
            }

            return plainText.ToString();
        }

    }

}

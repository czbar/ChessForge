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
        /// Calls the built-in Undo and then calls 
        /// SetEventHandlers so that event associations are restored
        /// if any Diagram or Move was part of Undo.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Undo(object sender, RoutedEventArgs e)
        {
            try
            {
                _rtb.Undo();
                SetEventHandlers();
            }
            catch { }
        }

        /// <summary>
        /// Stores the current selection in a DataObject,
        /// then uses the built-in cut operation and finally
        /// puts the cut selection in the system clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Cut(object sender, RoutedEventArgs e)
        {
            try
            {
                IntroViewClipboard.Clear();
                string plainText = CopySelectionToClipboard();

                IDataObject dataObject = new DataObject();
                dataObject.SetData(DataFormats.UnicodeText, plainText);
                dataObject.SetData(DataFormats.Serializable, IntroViewClipboard.Elements);

                // call _rtb.Cut() to conveniently remove the current selection
                _rtb.Cut();

                // Cut put some stuff into the clipboard which we will overwrite now
                SystemClipboard.SetDataObject(dataObject);
            }
            catch { }
        }

        /// <summary>
        /// Places the current selection in the system clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Copy(object sender, RoutedEventArgs e)
        {
            try
            {
                IntroViewClipboard.Clear();
                string plainText = CopySelectionToClipboard();

                IDataObject dataObject = new DataObject();
                dataObject.SetData(DataFormats.UnicodeText, plainText);
                dataObject.SetData(DataFormats.Serializable, IntroViewClipboard.Elements);
                SystemClipboard.SetDataObject(dataObject);
            }
            catch { }
        }

        /// <summary>
        /// Pastes the content of the clipboard into the view.
        /// First checks if the clipboard has a serializable content.
        /// If so, checks if it is of recognizable format i.e. a list of nodes
        /// or IntroViewElements.
        /// If none of the above, inserts whatever text is in the clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Paste(object sender, RoutedEventArgs e)
        {
            try
            {
                IDataObject dataObject = SystemClipboard.GetDataObject();
                if (dataObject != null)
                {
                    if (dataObject.GetDataPresent(DataFormats.Serializable))
                    {
                        PasteSerializable(dataObject.GetData(DataFormats.Serializable));

                    }
                    else if (dataObject.GetDataPresent(DataFormats.UnicodeText))
                    {
                        PasteUnicodeText(dataObject.GetData(DataFormats.UnicodeText) as string);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("IntroViewPaste()", ex);
            }
        }

        //***********************************************************************
        //
        // IMPLEMENTATION
        //
        //***********************************************************************


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
        /// Text Blocks and InlineUiContainers will not be split.
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

            bool isFirstParaDiagram = false;
            if (currParagraph != null && RichTextBoxUtilities.IsDiagramPara(currParagraph))
            {
                isFirstParaDiagram = true;
            }

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
                            if (isFirstParaDiagram || positionParent != currParagraph)
                            {
                                // NOTE: if something gets "caught" in the diagram paragraph, we may copy it even if not selected (?)
                                isFirstParaDiagram = false;

                                bool? flipped = null;
                                if (RichTextBoxUtilities.IsDiagramPara(positionParent))
                                {
                                    // check if proper diagram
                                    if (HasInlineUIContainer(positionParent))
                                    {
                                        int nodeId = TextUtils.GetIdFromPrefixedString(positionParent.Name);
                                        TreeNode node = GetNodeById(nodeId);
                                        flipped = RichTextBoxUtilities.GetDiagramFlipState(positionParent);
                                        if (!plainTextOnly)
                                        {
                                            IntroViewClipboard.AddDiagram(node, flipped);
                                        }
                                        plainText.AppendLine("");
                                        plainText.Append(RichTextBoxUtilities.GetDiagramPlainText(node));
                                    }
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
                        Run run = (Run)position.Parent;
                        Thickness? margins = null;
                        if (run.Parent is Paragraph para)
                        {
                            margins = para.Margin;
                        }

                        TextPointer textEnd = run.ElementEnd;
                        if (run.ElementEnd.CompareTo(end) > 0)
                        {
                            textEnd = end;
                        }

                        TextRange tr = new TextRange(position, textEnd);
                        Run runCopy = RichTextBoxUtilities.CopyRun(run);
                        runCopy.Text = tr.Text;
                        if (!plainTextOnly)
                        {
                            IntroViewClipboard.AddRun(runCopy, margins);
                        }
                        plainText.Append(RichTextBoxUtilities.GetRunPlainText(runCopy));

                        break;
                }

                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }

            return plainText.ToString();
        }

        /// <summary>
        /// Makes a copy of a paragraph from the clipboard
        /// and insert it at the current position.
        /// </summary>
        /// <param name="paraToInsert"></param>
        private TextPointer InsertParagraphFromClipboard(Paragraph paraToInsert)
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
        }

        /// <summary>
        /// Creats a new diagram paragraph and inserts it.
        /// </summary>
        /// <param name="diagPara"></param>
        private void InsertDiagramFromClipboard(TreeNode nodeForDiag, bool isFlipped)
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
        /// Inserts a serializable object found in the clipboard.
        /// This could be a List of IntroViewElements or Nodes.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>true, if objects of the recognizable type found; false otherwise.</returns>
        private bool PasteSerializable(object obj)
        {
            bool result = false;
            Type type = obj.GetType();

            if (type == typeof(List<IntroViewElement>))
            {
                PasteElements(obj as List<IntroViewElement>);
                result = true;
            }
            else if (type == typeof(List<TreeNode>))
            {
                PasteMoves(obj as List<TreeNode>);
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Pastes the passed Unicode text.
        /// </summary>
        /// <param name="text"></param>
        private void PasteUnicodeText(string text)
        {
            Run run = new Run(text);

            Run currentRun = _rtb.CaretPosition.Parent as Run;
            if (currentRun != null)
            {
                run.FontFamily = currentRun.FontFamily;
                run.FontSize = currentRun.FontSize;
                run.FontStyle = currentRun.FontStyle;
                run.FontWeight = currentRun.FontWeight;
            }

            AppState.IsDirty = true;
            InsertRunFromClipboard(run, null, out _);
        }

        /// <summary>
        /// Pastes a list of IntroView Elements.
        /// </summary>
        /// <param name="elements"></param>
        private void PasteElements(List<IntroViewElement> elements)
        {
            ClearSelection();
            AppState.IsDirty = true;

            bool isFirstElem = true;

            foreach (IntroViewElement element in elements)
            {
                switch (element.Type)
                {
                    case IntroViewClipboard.ElementType.Paragraph:
                        Paragraph paragraph = element.CreateParagraph();
                        TextPointer tp = InsertParagraphFromClipboard(paragraph);
                        _rtb.CaretPosition = tp;
                        break;
                    case IntroViewClipboard.ElementType.Diagram:
                        InsertDiagramFromClipboard(element.Node, element.BoolState == true);
                        break;
                    case IntroViewClipboard.ElementType.Run:
                        Thickness? thick = element.GetThickness();
                        InsertRunFromClipboard(element.CreateRun(), isFirstElem ? thick : null, out _);
                        break;
                    case IntroViewClipboard.ElementType.Move:
                        InsertMoveFromClipboard(element.Node);
                        break;
                }

                isFirstElem = false;
            }
        }

        /// <summary>
        /// Pastes a list of Moves
        /// </summary>
        /// <param name="nodes"></param>
        private void PasteMoves(List<TreeNode> nodes)
        {
            ClearSelection();
            AppState.IsDirty = true;

            foreach (TreeNode node in nodes)
            {
                TreeNode ndToInsert = node.CloneMe(true);
                InsertMove(ndToInsert);
            }
        }

        /// <summary>
        /// If parent is a run, split the run and insert this one in between.
        /// If parent is a Paragraph and it is not a diagram paragraph, insert it at the caret point.
        /// </summary>
        /// <param name="run"></param>
        private void InsertRunFromClipboard(Run runToInsert, Thickness? margins, out double fontSize)
        {
            RichTextBoxUtilities.GetMoveInsertionPlace(_rtb, out Paragraph para, out Inline insertBefore, out fontSize);
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
        /// Removes the current selection.
        /// </summary>
        private void ClearSelection()
        {
            if (!_rtb.Selection.IsEmpty)
            {
                TextRange selection = new TextRange(_rtb.Selection.Start, _rtb.Selection.End);
                selection.Text = "";
            }
        }

    }

}

using ChessPosition;
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
    /// Utilities for dealing with RichTextBox.
    /// </summary>
    public class RichTextBoxUtilities
    {
        public static readonly string _para_diagram_ = "para_diag_";

        /// <summary>
        /// Makes a copy of a Run with selected properties.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Run CopyRun(Run src)
        {
            Run runToAdd = new Run();

            runToAdd.Name = "";
            runToAdd.Text = src.Text;
            runToAdd.FontWeight = src.FontWeight;
            runToAdd.FontSize = src.FontSize;
            //runToAdd.TextDecorations = run.TextDecorations;

            return runToAdd;
        }

        /// <summary>
        /// Makes a copy of a Paragraph with selected properties.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Paragraph CopyParagraph(Paragraph src)
        {
            Paragraph paraToAdd = new Paragraph();

            paraToAdd.Name = "";
            paraToAdd.FontWeight = src.FontWeight;
            paraToAdd.FontSize = src.FontSize;

            return paraToAdd;
        }

        /// <summary>
        /// If the caret is inside a Run, we split the Run in two
        /// and return the newly created second Run.
        /// Otherwise returns null.
        /// </summary>
        /// <param name="rtb"></param>
        /// <returns></returns>
        public static Run SplitRun(RichTextBox rtb)
        {
            try
            {
                TextPointer caretPosition = rtb.CaretPosition;

                // if the current position is a Run, split it otherwise return
                if (caretPosition.Parent.GetType() != typeof(Run))
                {
                    return null;
                }

                Run newRun;

                // Split the Run at the current caret position
                Run currentRun = caretPosition.Parent as Run;
                Paragraph currentParagraph = caretPosition.Paragraph;

                if (caretPosition.GetOffsetToPosition(currentRun.ContentEnd) > 0)
                {
                    // Get a TextPointer to the start of the second half of the Run
                    TextPointer splitPosition = caretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);

                    // Create a new Run containing the second half of the original Run
                    newRun = new Run(currentRun.Text.Substring(-1 * splitPosition.GetOffsetToPosition(currentRun.ContentStart)));

                    // Remove the second half of the original Run
                    currentRun.Text = currentRun.Text.Substring(0, -1 * splitPosition.GetOffsetToPosition(currentRun.ContentStart));
                }
                else
                {
                    // create a new Run with empty text
                    newRun = new Run("");
                }

                // Insert the new Run after the original Run
                currentParagraph.Inlines.InsertAfter(currentRun, newRun);
                rtb.CaretPosition = newRun.ContentStart;
                return newRun;
            }
            catch (Exception ex)
            {
                AppLog.Message("SplitRun()", ex);
                return null;
            }
        }

        /// <summary>
        /// Based on the current caret position and selection (if any)
        /// determine the paragraph in which to insert the new move
        /// and an Inline before which to insert it.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="insertBefore"></param>
        public static void GetMoveInsertionPlace(RichTextBox rtb, out Paragraph para, out Inline insertBefore)
        {
            TextSelection selection = rtb.Selection;
            if (!selection.IsEmpty)
            {
                // if there is a selection we want to insert after it.
                // e.g. we just highlighted the move by clicking on it and, intuitively,
                // want the next move to come after it.
                rtb.CaretPosition = selection.End;
            }

            TextPointer tpCaret = rtb.CaretPosition;
            para = tpCaret.Paragraph;

            // if we are inside a diagram paragraph, create a new one
            if (IsDiagramPara(para, out _))
            {
                para = rtb.CaretPosition.InsertParagraphBreak().Paragraph;
                para.Name = TextUtils.GenerateRandomElementName();
                insertBefore = null;
            }
            else
            {
                // if caret is inside a Run, split it and return the second part
                insertBefore = RichTextBoxUtilities.SplitRun(rtb);
                if (insertBefore != null && insertBefore.Parent is Paragraph)
                {
                    para = insertBefore.Parent as Paragraph;
                }
                else
                {
                    DependencyObject inl = tpCaret.GetAdjacentElement(LogicalDirection.Forward);
                    if (inl != null && inl is Inline && para != null)
                    {
                        insertBefore = inl as Inline;
                    }
                    else
                    {
                        // there is no Inline ahead so just append to the current paragraph
                        // or create a new one if null
                        insertBefore = null;
                        if (para == null)
                        {
                            para = rtb.CaretPosition.InsertParagraphBreak().Paragraph;
                        }

                        if (tpCaret.Paragraph != null)
                        {
                            para = tpCaret.Paragraph;
                            insertBefore = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The diagram will be deeemed a "diagram para" if its
        /// name starts with _para_diag and it has a diagram content
        /// (the name is not enough because of how RTB can duplicate the name
        /// of a paragraph).
        /// </summary>
        /// <returns></returns>
        public static bool IsDiagramPara(Paragraph para, out InlineUIContainer diagram)
        {
            diagram = null;

            if (para == null || para.Name == null)
            {
                return false;
            }

            bool res = false;
            if (para.Name.StartsWith(_para_diagram_))
            {
                int paraNodeId = TextUtils.GetIdFromPrefixedString(para.Name);
                foreach (Inline inl in para.Inlines)
                {
                    if (inl is InlineUIContainer)
                    {
                        string uicName = (inl as InlineUIContainer).Name;
                        int uicNodeId = TextUtils.GetIdFromPrefixedString(uicName);
                        if (paraNodeId == uicNodeId)
                        {
                            res = true;
                            diagram = (inl as InlineUIContainer);
                            break;
                        }
                    }
                }
            }

            return res;
        }

    }
}

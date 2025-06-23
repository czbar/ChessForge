using ChessPosition;
using GameTree;
using System;
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
        /// <summary>
        /// Prefix for naming paragraphs representing a diagram.
        /// </summary>
        public static readonly string DiagramParaPrefix = "para_diag_";

        /// <summary>
        /// Prefix for naming InlineUIContainer representing an inline diagram.
        /// </summary>
        public static readonly string InlineDiagramIucPrefix = "iuc_inl_diag_";

        /// <summary>
        /// Prefix for naming runs preceding an inline diagram.
        /// </summary>
        public static readonly string PreInlineDiagramRunPrefix = "run_preinl_diag_";

        /// <summary>
        /// Prefix for naming runs following an inline diagram.
        /// </summary>
        public static readonly string PostInlineDiagramRunPrefix = "run_postinl_diag_";

        /// <summary>
        /// Prefix for naming InlineUIContainer representing an inline diagram.
        /// </summary>
        public static readonly string InlineDiagramBeforeMoveIucPrefix = "iuc_inl_diag_bm_";

        /// <summary>
        /// Prefix for naming runs preceding an inline diagram.
        /// </summary>
        public static readonly string PreInlineDiagramBeforeMoveRunPrefix = "run_preinl_diag_bm_";

        /// <summary>
        /// Prefix for naming runs following an inline diagram.
        /// </summary>
        public static readonly string PostInlineDiagramBeforeMoveRunPrefix = "run_postinl_diag_bm_";

        /// <summary>
        /// Prefix for naming Text Boxes with moves in Intro. 
        /// </summary>
        public static readonly string MoveTextBoxPrefix = "tb_move_";

        /// <summary>
        /// Prefix for naming an Inline for a Move in Intro. 
        /// </summary>
        public static readonly string UicMovePrefix = "uic_move_";

        /// <summary>
        // Name of the header paragraph.
        /// </summary>
        public static readonly string HeaderParagraphName = "para_header_";

        /// <summary>
        // Name of the index paragraph in the study.
        /// </summary>
        public static readonly string StudyIndexParagraphName = "para_index_";

        /// <summary>
        // Name of the header paragraph.
        /// </summary>
        public static readonly string ExerciseUnderBoardControls = "para_underboard_";

        /// <summary>
        /// Prefix for the Run with the reference symbol
        /// </summary>
        public static readonly string ReferenceRunPrefix = "run_reference_";

        /// <summary>
        /// Prefix for naming runs representing a move in a fork table.
        /// </summary>
        public static readonly string RunForkMovePrefix = "_run_fork_move_";

        /// <summary>
        /// Prefix for naming runs representing a move.
        /// </summary>
        public static readonly string RunMovePrefix = "run_move_";

        /// <summary>
        /// Checks if the passed object is a Run representing a move.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool IsMoveRun(object o)
        {
            bool isMove = false;
            
            if (o is Run run)
            {
                return IsMoveRunName(run.Name);
            }

            return isMove;
        }

        /// <summary>
        /// Checks if the passed name is a Run representing a move.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsMoveRunName(string name)
        {
            bool isMove = false;

            if (!string.IsNullOrEmpty(name) && name.StartsWith(RunMovePrefix))
            {
                isMove = true;
            }

            return isMove;
        }

        /// <summary>
        /// Finds the first Run representing a move in the paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        public static Run FindFirstMoveRunInParagraph(Paragraph para)
        {
            Run run = null;

            if (para != null)
            {
                foreach (Inline inl in para.Inlines)
                {
                    if (inl is Run r && r.Name != null && r.Name.StartsWith(RichTextBoxUtilities.RunForkMovePrefix))
                    {
                        run = r;
                        break;
                    }
                }
            }

            return run;
        }

        /// <summary>
        /// Checks if the passed Run is the first Run in the Paragraph.
        /// Only the name of the Run is checked as it is to be used
        /// for checking move runs only and they have unique names.
        /// </summary>
        /// <param name="run"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public static bool IsFirstMoveRunInParagraph(Run run, Paragraph para)
        {
            bool isFirst = false;

            if (para != null && run != null)
            {
                foreach (Inline inl in para.Inlines)
                {
                    if (inl is Run r)
                    {
                        isFirst = (r.Name == run.Name);
                        break;
                    }
                }
            }

            return isFirst;
        }

        /// <summary>
        /// Finds the last Run representing a move in the paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        public static Run FindLastMoveRunInParagraph(Paragraph para)
        {
            Run run = null;

            if (para != null)
            {
                Inline inl = para.Inlines.LastInline;

                while (inl != null)
                {
                    if (inl is Run r && r.Name != null && r.Name.StartsWith(RichTextBoxUtilities.RunForkMovePrefix))
                    {
                        run = r;
                    }
                    inl = inl.PreviousInline;
                }
            }

            return run;
        }

        /// <summary>
        /// Determines wheter the upper or lower part of the passed Inline object was clicked.
        /// Gets the position of the mouse click and the position of the Inline object
        /// and checks if the position of he click minus half the font size would still be
        /// within the same Inline.
        /// </summary>
        /// <param name="inline"></param>
        /// <param name="ptMouse"></param>
        /// <param name="rtb"></param>
        /// <returns></returns>
        public static bool IsUpperPartClicked(Inline inline, Point ptMouse, RichTextBox rtb)
        {
            // get the TextPointer of the mouse click
            TextPointer tpMousePos = rtb.GetPositionFromPoint(ptMouse, true);

            // calculate a point half the font size below
            Point ptBelow = new Point(ptMouse.X, ptMouse.Y + inline.FontSize / 2);

            // get the TextPointer of the point below
            TextPointer tpBelow = rtb.GetPositionFromPoint(ptBelow, true);

            // if it is still the same TextPointer, we clicked the upper half
            return tpMousePos.CompareTo(tpBelow) == 0;
        }

        /// <summary>
        /// Returns true if the passed paragraph contains no inlines
        /// or only empty Runs.
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        public static bool HasNonEmptyInline(Paragraph para)
        {
            bool res = false;

            if (para != null)
            {
                foreach (Inline inl in para.Inlines)
                {
                    Run run = inl as Run;
                    // if this is not a Run we consider that this Paragraph has non-empty content (e.g. InlineUIContainer)
                    if (run == null || !string.IsNullOrEmpty(run.Text))
                    {
                        res = true;
                        break;
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Finds a paragraph with a given name in the document.
        /// Returns null if not found.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="name"></param>
        /// <param name="partialName"></param>
        /// <returns></returns>
        public static Paragraph FindParagraphByName(FlowDocument document, string name, bool partialName)
        {
            Paragraph para = null;

            foreach (Block block in document.Blocks)
            {
                if (block is Paragraph && (block as Paragraph).Name != null)
                {
                    if (partialName && (block as Paragraph).Name.StartsWith(name) || (block as Paragraph).Name == name)
                    {
                        para = block as Paragraph;
                        break;
                    }
                }
            }

            return para;
        }

        /// <summary>
        /// Checks if the passed Run is the first Run 
        /// in the Paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        public static bool IsFirstNonEmptyRunInPara(Run run, Paragraph para)
        {
            bool res = true;

            if (run != null && para != null)
            {
                foreach (Inline inl in para.Inlines)
                {
                    if (inl is Run r && !string.IsNullOrEmpty(r.Text))
                    {
                        res = (r == run);
                        break;
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Makes a copy of a Run with selected properties.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Run CopyRun(Run src)
        {
            Run runToAdd = new Run();

            runToAdd.Text = src.Text;
            runToAdd.FontWeight = src.FontWeight;
            runToAdd.FontSize = src.FontSize;
            //runToAdd.TextDecorations = run.TextDecorations;

            return runToAdd;
        }

        /// <summary>
        /// Makes a copy of a Hyperlink
        /// that can be encountered in Intros.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Hyperlink CopyHyperlink(Hyperlink src)
        {
            Run runToAdd;

            if (src.Inlines.Count > 0 && src.Inlines.FirstInline is Run run)
            {
                runToAdd = CopyRun(run);
            }
            else
            {
                runToAdd = new Run("???");
            }

            Hyperlink hlToAdd = new Hyperlink(runToAdd);
            hlToAdd.NavigateUri = src.NavigateUri;

            return hlToAdd;
        }

        /// <summary>
        /// Makes a copy of a Paragraph with selected properties.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Paragraph CopyParagraph(Paragraph src)
        {
            Paragraph paraToAdd = new Paragraph();

            paraToAdd.FontWeight = src.FontWeight;
            paraToAdd.FontSize = src.FontSize;
            paraToAdd.Margin = src.Margin;

            return paraToAdd;
        }

        /// <summary>
        /// If the caret is inside a Run, we split the Run in two
        /// and return the newly created second Run.
        /// Otherwise returns null.
        /// </summary>
        /// <param name="rtb"></param>
        /// <returns></returns>
        public static Run SplitRun(RichTextBox rtb, out double fontSize)
        {
            fontSize = 14;

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
                fontSize = currentRun.FontSize;

                Paragraph currentParagraph = caretPosition.Paragraph;

                if (caretPosition.GetOffsetToPosition(currentRun.ContentEnd) > 0)
                {
                    // Get a TextPointer to the start of the second half of the Run
                    TextPointer splitPosition = caretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);

                    // Create a new Run containing the second half of the original Run
                    newRun = new Run(currentRun.Text.Substring(-1 * splitPosition.GetOffsetToPosition(currentRun.ContentStart)));
                    newRun.FontSize = currentRun.FontSize;

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
        public static void GetMoveInsertionPlace(RichTextBox rtb, out Paragraph para, out Inline insertBefore, out double fontSize)
        {
            fontSize = 14;

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
            if (GetDiagramFromParagraph(para, out _))
            {
                para = rtb.CaretPosition.InsertParagraphBreak().Paragraph;
                para.Name = TextUtils.GenerateRandomElementName();
                insertBefore = null;
            }
            else
            {
                // if caret is inside a Run, split it and return the second part
                insertBefore = SplitRun(rtb, out fontSize);
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
        /// Checks if Paragraph's name indicates that it contains a diagram.
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        public static bool IsDiagramPara(Paragraph para)
        {
            return para != null && para.Name != null && para.Name.StartsWith(DiagramParaPrefix);
        }

        /// <summary>
        /// The diagram will be deeemed a "diagram para" if its
        /// name starts with _para_diag and it has a diagram content
        /// (the name is not enough because of how RTB can duplicate the name
        /// of a paragraph).
        /// </summary>
        /// <returns></returns>
        public static bool GetDiagramFromParagraph(Paragraph para, out InlineUIContainer diagram)
        {
            diagram = null;

            if (para == null || para.Name == null)
            {
                return false;
            }

            bool res = false;
            if (para.Name.StartsWith(DiagramParaPrefix))
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

        /// <summary>
        /// Returns text representation of the position in the node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static string GetDiagramPlainText(TreeNode node)
        {
            if (node == null)
            {
                return "";
            }
            else
            {
                return "\n" + BuildDiagramString(node.Position) + "\n";
            }
        }

        /// <summary>
        /// Gets the orientation (aka "flip state") of the diagram
        /// by checking the status of the hidden checkbox in the diagram.
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        public static bool GetDiagramFlipState(Paragraph para)
        {
            bool res = false;

            try
            {
                CheckBox cb = FindFlippedCheckBox(para);
                if (cb != null)
                {
                    res = cb.IsChecked == true;
                }
            }
            catch
            {
            }

            return res;
        }

        /// <summary>
        /// Returns the diagram's "flip state" CheckBox
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        public static CheckBox FindFlippedCheckBox(Paragraph para)
        {
            try
            {
                CheckBox cb = null;

                foreach (Inline inl in para.Inlines)
                {
                    if (inl is InlineUIContainer)
                    {
                        Viewbox vb = ((InlineUIContainer)inl).Child as Viewbox;
                        if (vb != null && vb.Child != null)
                        {
                            Canvas canvas = vb.Child as Canvas;
                            foreach (UIElement uie in canvas.Children)
                            {
                                if (uie is CheckBox)
                                {
                                    cb = uie as CheckBox;
                                    break;
                                }
                            }
                        }
                    }
                }

                return cb;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a string with plain text combined from all Runs in the Paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        public static string GetParagraphPlainText(Paragraph para)
        {
            StringBuilder plainText = new StringBuilder("");

            foreach (Inline inl in para.Inlines)
            {
                if (inl is Run)
                {
                    plainText.Append(((Run)inl).Text);
                }
            }
            plainText.Append("\n");

            return plainText.ToString();
        }

        /// <summary>
        /// Returns plain text for the embedded move.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static string GetEmbeddedElementPlainText(TreeNode node)
        {
            if (node != null)
            {
                return " " + node.LastMoveAlgebraicNotation + " ";
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Returns plain text from a Run
        /// </summary>
        /// <param name="run"></param>
        /// <returns></returns>
        public static string GetRunPlainText(Run run)
        {
            if (run != null && run.Text != null)
            {
                return run.Text;
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns plain text from a Hyperlink
        /// </summary>
        /// <param name="run"></param>
        /// <returns></returns>
        public static string GetHyperlinkPlainText(Hyperlink hl)
        {
            if (hl != null)
            {
                StringBuilder sb = new StringBuilder();
                if (hl.Inlines.Count > 0 && hl.Inlines.FirstInline is Run run)
                {
                    sb.Append(run.Text);
                }
                sb.Append(" (" + hl.NavigateUri.ToString() + ")");
                return sb.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds a string representing the current board position
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        private static string BuildDiagramString(BoardPosition board)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                for (int row = 7; row >= 0; row--)
                {
                    sb.Append("        ");
                    for (int i = 0; i <= 7; i++)
                    {
                        char ch = DebugUtils.FenPieceToChar[Constants.FlagToPiece[(byte)((board.Board[i, row] & ~Constants.Color))]];
                        char piece;
                        if (ch == DebugUtils.FenPieceToChar[PieceType.None])
                        {
                            piece = '⨯';
                        }
                        else
                        {
                            if ((board.Board[i, row] & Constants.Color) > 0)
                            {
                                Languages.WhiteFigurinesMapping.TryGetValue(char.ToUpper(ch), out piece);
                            }
                            else
                            {
                                Languages.BlackFigurinesMapping.TryGetValue(char.ToUpper(ch), out piece);
                            }
                        }

                        sb.Append(piece);
                    }
                    sb.Append("\n");
                }
            }
            catch { }

            return sb.ToString();
        }
    }
}

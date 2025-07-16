using ChessPosition;
using GameTree;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ChessForge
{
    public abstract class RichTextBuilder
    {
        /// <summary>
        /// The Flow Document this object is working with.
        /// This object will be typically associated with a RichTextBox control
        /// </summary>
        internal RichTextBox HostRtb;

        /// <summary>
        /// Styles for the document's paragraphs.
        /// The Dictionary must be implemented in derived classes.
        /// </summary>
        internal abstract Dictionary<string, RichTextPara> RichTextParas { get; }

        /// <summary>
        /// Constructs the object and sets pointer to its associated FlowDocument.
        /// </summary>
        /// <param name="doc"></param>
        public RichTextBuilder(RichTextBox rtbBox)
        {
            HostRtb = rtbBox;
        }

        /// <summary>
        /// Returns the Run where a new text should be inserted.
        /// If there is no run at the caret, or nearby a new one is created.
        /// </summary>
        /// <param name="rtb"></param>
        /// <returns></returns>
        protected Run GetRunForInsertion(RichTextBox rtb)
        {
            Run run = GetRunUnderCaret(rtb);

            if (run == null)
            {
                run = new Run();
                if (rtb.CaretPosition.Paragraph != null)
                {
                    rtb.CaretPosition.Paragraph.Inlines.Add(run);
                }
            }

            return run;
        }

        /// <summary>
        /// Returns the Run where the caret is currently placed.
        /// If the caret is not inside a Run, finds the first Run in the forward direction.
        /// If not found, returns 0.
        /// </summary>
        /// <param name="rtb"></param>
        /// <returns></returns>
        protected Run GetRunUnderCaret(RichTextBox rtb)
        {
            // Get the position of the caret
            TextPointer caretPosition = rtb.CaretPosition;

            // Find the Run containing the caret position
            Run run = caretPosition.Parent as Run;

            // If the caret is not currently inside a Run, traverse up the logical tree to find the nearest Run
            while (run == null && caretPosition != null)
            {
                caretPosition = caretPosition.GetNextContextPosition(LogicalDirection.Forward);
                if (caretPosition == null)
                {
                    break;
                }
                run = caretPosition.Parent as Run;
            }

            return run;
        }

        /// <summary>
        /// Return a set of attributes for a given style.
        /// If the style is not found in the style dictionary, 
        /// uses the default style. 
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public RichTextPara GetParaAttrs(string style, bool adjustFontSize)
        {
            if (!RichTextParas.TryGetValue(style, out RichTextPara rtAttrs))
            {
                RichTextParas.TryGetValue("default", out rtAttrs);
            }

            RichTextPara attrs = rtAttrs.CloneMe();
            if (adjustFontSize)
            {
                attrs.FontSize = GuiUtilities.AdjustFontSize(rtAttrs.FontSize);
            }
            return attrs;
        }

        /// <summary>
        /// Moves content of one document to another. 
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public void MoveDocument(FlowDocument source, FlowDocument target)
        {
            target.Blocks.Clear();
            var blockList = source.Blocks.ToList();

            foreach (Block blk in blockList)
            {
                source.Blocks.Remove(blk);
                target.Blocks.Add(blk);
            }
        }

        /// <summary>
        /// Create a paragraph for the specified style.
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public Paragraph CreateParagraph(string style, bool adjustFontSize)
        {
            RichTextPara attrs = GetParaAttrs(style, adjustFontSize);

            Paragraph para = new Paragraph
            {
                Margin = new Thickness(attrs.LeftIndent, 0, 0, attrs.BottomMargin),
                FontSize = attrs.FontSize,
                FontWeight = attrs.FontWeight,
                TextAlignment = attrs.TextAlign,
                Foreground = attrs.ForegroundColor ?? ChessForgeColors.CurrentTheme.RtbForeground,
            };

            return para;
        }

        /// <summary>
        /// Return the margin (Thickness) for the passed style.
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public Thickness GetParagraphMargin(string style)
        {
            RichTextPara attrs = GetParaAttrs(style, true);
            return new Thickness(attrs.LeftIndent, 0, 0, attrs.BottomMargin);
        }

        /// <summary>
        /// Create a table for the specified style.
        /// Sets the right margin same as the left one.
        /// Sets Foreground color to Black
        /// and FontWeight to Normal.
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public Table CreateTable(double indent)
        {
            Table table = new Table
            {
                Margin = new Thickness(indent, 0, indent, 0),
                FontSize = 14 + Configuration.FontSizeDiff,
                FontWeight = FontWeights.Normal,
                TextAlignment = TextAlignment.Left,
                Foreground = Brushes.Black
            };

            return table;
        }

        /// <summary>
        /// Create a paragraph for the specified style and sets
        /// the passed text in it.
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public Paragraph CreateParagraphWithText(string style, string text, bool adjustFontSize)
        {
            Paragraph para = CreateParagraph(style, adjustFontSize);

            if (text != null)
            {
                Run r = new Run(text);
                para.Inlines.Add(r);
            }

            return para;
        }

        /// <summary>
        /// Creates a new Run with the requested style and text.
        /// </summary>
        /// <param name="style"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public Run CreateRun(string style, string text, bool adjustFontSize)
        {
            RichTextPara attrs = GetParaAttrs(style, adjustFontSize);

            Run r = new Run
            {
                FontSize = attrs.FontSize,
                FontWeight = attrs.FontWeight,
                Foreground = attrs.ForegroundColor ?? ChessForgeColors.CurrentTheme.RtbForeground,
        };

            r.Text = text;

            return r;
        }

        /// <summary>
        /// Creates a paragraph, sets its text and inserts it
        /// into the Document.
        /// </summary>
        /// <param name="style"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public Paragraph AddNewParagraphToDoc(FlowDocument doc, string style, string text, Paragraph insertAfter = null)
        {
            Paragraph para = CreateParagraphWithText(style, text, false);
            if (insertAfter == null)
            {
                doc.Blocks.Add(para);
            }
            else
            {
                doc.Blocks.InsertAfter(insertAfter, para);
            }

            return para;
        }

        /// <summary>
        /// If the document contains no inlines or only inlines of type Run
        /// with emoty text, we consider it empty.
        /// </summary>
        /// <returns></returns>
        public bool IsDocumentEmpty(FlowDocument doc)
        {
            bool empty = true;
            
            foreach (var para in doc.Blocks)
            {
                if (para is Paragraph paragraph)
                {
                    if (paragraph.Inlines.Count > 0 && RichTextBoxUtilities.HasNonEmptyInline(paragraph as Paragraph))
                    {
                        empty = false;
                        break;
                    }
                }
            }

            return empty;
        }

        /// <summary>
        /// Removes empty paragraphs that get created when building the document
        /// or left behind after deletions.
        /// Does not remove paragraphs with the name "dummy" as they were created for a specific purpose.
        /// </summary>
        public void RemoveEmptyParagraphs(FlowDocument doc)
        {
            List<Paragraph> parasToRemove = new List<Paragraph>();

            foreach (var para in doc.Blocks)
            {
                if (para is Paragraph paragraph)
                {
                    if (para.Name != Constants.DUMMY_PARA_NAME && (paragraph.Inlines.Count == 0 || !RichTextBoxUtilities.HasNonEmptyInline(para as Paragraph)))
                    {
                        parasToRemove.Add(paragraph);
                    }
                }
            }

            foreach (Paragraph para in parasToRemove)
            {
                doc.Blocks.Remove(para);
            }
        }

        /// <summary>
        /// Removes a Block object from the Document
        /// </summary>
        /// <param name="block"></param>
        public void RemoveBlock(FlowDocument doc, Block block)
        {
            doc.Blocks.Remove(block);
        }


        /// <summary>
        /// Adds text to the referenced paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="text"></param>
        public void AddTextToParagraph(Paragraph para, string text)
        {
            Run r = new Run(text);
            para.Inlines.Add(r);
        }

        /// <summary>
        /// Builds a paragraph displaying the "stem" line
        /// i.e. moves from the first one to the first fork.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public Paragraph BuildWorkbookStemLine(TreeNode nd, bool adjustFontSize)
        {
            Paragraph para = CreateParagraph("0", adjustFontSize);
            para.Foreground = ChessForgeColors.RTB_GRAY_FOREGROUND;

            string prefix = GetStemLineText(nd);

            Run r = new Run(prefix);
            para.Inlines.Add(r);

            return para;
        }

        /// <summary>
        /// Returns id encoded in the run name.
        /// </summary>
        /// <param name="runName"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public int GetNodeIdFromRunName(string runName, string prefix)
        {
            int nodeId = -1;
            if (runName != null && runName.StartsWith(prefix))
            {
                runName = runName.Substring(prefix.Length);
                string[] tokens = runName.Split('_');
                nodeId = int.Parse(tokens[0]);
            }

            return nodeId;
        }

        /// <summary>
        /// Removes a Run from its hosting paragraph
        /// </summary>
        /// <param name="inl"></param>
        public void RemoveRunFromHostingParagraph(Inline inl)
        {
            if (inl != null)
            {
                Paragraph parent = inl.Parent as Paragraph;
                parent.Inlines.Remove(inl);
            }
        }

        /// <summary>
        /// Removes all parts of the comment for a given node.
        /// These are identified by prefixes and nodeIds coded in the name of the inline.
        /// The prefixes identify text and URL  parts of the comment and the thumbnail sign ("run_comment_"),
        /// references ("_run_comment_article_ref_")
        /// and a diagram releated components ("run_preinl_diag_", "run_postinl_diag_", "iuc_inl_diag_")
        /// </summary>
        /// <param name="block"></param>
        public void RemoveCommentRunsFromHostingParagraph(Inline inline, int nodeId)
        {
            if (inline == null)
            {
                return;
            }

            Paragraph parent = inline.Parent as Paragraph;
            if (parent == null)
            {
                return;
            }

            string commentTextName = RichTextBoxUtilities.CommentRunPrefix + nodeId.ToString();
            string preDiagTextName = RichTextBoxUtilities.PreInlineDiagramRunPrefix + nodeId.ToString();
            string postDiagTextName = RichTextBoxUtilities.PostInlineDiagramRunPrefix + nodeId.ToString();
            string iucDiagramName = RichTextBoxUtilities.InlineDiagramIucPrefix + nodeId.ToString();

            string articleRefPrefix = RichTextBoxUtilities.CommentArticleRefPrefix + nodeId.ToString() + "_";

            List<Inline> inlinesToRemove = new List<Inline>();
            // where appropriate we need to match the full name e.g. "run_comment_45"
            // (so that we don't match the above for node id 4)
            // and in the case of refs the first 2 parts ending with '_'.
            foreach (Inline inl in parent.Inlines)
            {
                if (inl.Name == commentTextName 
                    || inl.Name == preDiagTextName
                    || inl.Name == postDiagTextName
                    || inl.Name == iucDiagramName
                    || inl.Name.StartsWith(articleRefPrefix)
                    )
                {
                    inlinesToRemove.Add(inl);
                }
            }

            foreach (Inline inlToRemove in inlinesToRemove)
            {
                parent.Inlines.Remove(inlToRemove);
            }
        }

        /// <summary>
        /// Removes all parts of the "comment before move" for a given node.
        /// These are identified by prefixes and nodeIds coded in the name of the inline.
        /// </summary>
        /// <param name="inline"></param>
        /// <param name="nodeId"></param>
        public void RemoveCommentBeforeMoveRunsFromHostingParagraph(Inline inline, int nodeId)
        {
            if (inline == null)
            {
                return;
            }

            Paragraph parent = inline.Parent as Paragraph;
            if (parent == null)
            {
                return;
            }

            string commentTextName = RichTextBoxUtilities.CommentBeforeMoveRunPrefix + nodeId.ToString();
            string preDiagTextName = RichTextBoxUtilities.PreInlineDiagramBeforeMoveRunPrefix + nodeId.ToString();
            string postDiagTextName = RichTextBoxUtilities.PostInlineDiagramBeforeMoveRunPrefix + nodeId.ToString();
            string iucDiagramName = RichTextBoxUtilities.InlineDiagramBeforeMoveIucPrefix + nodeId.ToString();

            List<Inline> inlinesToRemove = new List<Inline>();
            // where appropriate we need to match the full name e.g. "run_comment_45"
            // (so that we don't match the above for node id 4)
            // and in the case of refs the first 2 parts ending with '_'.
            foreach (Inline inl in parent.Inlines)
            {
                if (inl.Name == commentTextName
                    || inl.Name == preDiagTextName
                    || inl.Name == postDiagTextName
                    || inl.Name == iucDiagramName
                    )
                {
                    inlinesToRemove.Add(inl);
                }
            }

            foreach (Inline inlToRemove in inlinesToRemove)
            {
                parent.Inlines.Remove(inlToRemove);
            }
        }

        /// <summary>
        /// Insert a Run after a specified Run.
        /// </summary>
        /// <param name="runToIsert"></param>
        /// <param name="insertAfter"></param>
        public void InsertRun(Run runToIsert, Run insertAfter)
        {
            Paragraph parent = insertAfter.Parent as Paragraph;
            parent.Inlines.InsertAfter(insertAfter, runToIsert);
        }

        /// <summary>
        /// Finds the first paragraph with the passed name.
        /// Return null if not found.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Paragraph FindParagraphByName(FlowDocument doc, string name, bool partialName)
        {
            Paragraph para = null;

            foreach (Block block in doc.Blocks)
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
        /// Finds the first Run with the passed name in the specified paragraph.
        /// Return null if not found.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public Run FindRunByName(string name, Paragraph para)
        {
            Run r = null;

            foreach (Inline inl in para.Inlines)
            {
                if (inl is Run && inl.Name == name)
                {
                    r = inl as Run;
                    break;
                }
            }

            return r;
        }

        /// <summary>
        /// Finds the first Inline with the passed name in the specified paragraph.
        /// Return null if not found.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public Inline FindInlineByName(string name, Paragraph para)
        {
            Inline ret = null;

            foreach (Inline inl in para.Inlines)
            {
                if (inl.Name == name)
                {
                    ret = inl as Inline;
                    break;
                }
            }

            return ret;
        }

        /// <summary>
        /// Finds the first Inline with the passed name in the Document.
        /// Return null if not found.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Inline FindInlineByName(FlowDocument doc, string name)
        {
            Inline inl = null;
            foreach (Block block in doc.Blocks)
            {
                if (block is Paragraph)
                {
                    inl = FindInlineByName(name, block as Paragraph);
                    if (inl != null)
                    {
                        break;
                    }
                }
            }

            return inl;
        }

        /// <summary>
        /// Assuming the format "[prefix]" + "_" + NodeId
        /// find paragrpah that hosts a run for the Node with passed
        /// NodeId.
        /// </summary>
        /// <returns></returns>
        public Run GetRunForNodeId(FlowDocument doc, int nodeId)
        {
            foreach (Block block in doc.Blocks)
            {
                if (block is Paragraph)
                {
                    foreach (var run in (block as Paragraph).Inlines)
                    {
                        if (run is Run && TextUtils.GetIdFromPrefixedString(run.Name) == nodeId)
                        {
                            return run as Run;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Builds text for the paragraph displaying the "stem" line
        /// i.e. moves from the first one to the first fork.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        protected string GetStemLineText(TreeNode nd)
        {
            StringBuilder sbPrefix = new StringBuilder();

            uint moveNumberOffset = 0;
            if (AppState.ActiveVariationTree != null)
            {
                moveNumberOffset = AppState.ActiveVariationTree.MoveNumberOffset;
            }

            while (nd != null)
            {
                if (nd.ColorToMove == PieceColor.Black)
                {
                    sbPrefix.Insert(0, (nd.MoveNumber + moveNumberOffset).ToString() + "." + nd.LastMoveAlgebraicNotation);
                }
                else
                {
                    sbPrefix.Insert(0, " " + nd.LastMoveAlgebraicNotation + " ");
                }
                nd = nd.Parent;
            }

            return sbPrefix.ToString();
        }

    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using ChessPosition;
using GameTree;

namespace ChessForge
{
    public abstract class RichTextBuilder
    {
        /// <summary>
        /// The Flow Document this object is working with.
        /// This object will be typically associated with a RichTextBox control
        /// </summary>
        internal FlowDocument Document;

        /// <summary>
        /// Styles for the document's paragraphs.
        /// The Dictionary must be implemented in derived classes.
        /// </summary>
        internal abstract Dictionary<string, RichTextPara> RichTextParas { get; }

        /// <summary>
        /// Constructs the object and sets pointer to its associated FlowDocument.
        /// </summary>
        /// <param name="doc"></param>
        public RichTextBuilder(FlowDocument doc)
        {
            Document = doc;
        }

        /// <summary>
        /// Return a set of attributes for a given style.
        /// If the style is not found in the style dictionary, 
        /// uses the default style. 
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public RichTextPara GetParaAttrs(string style)
        {
            RichTextPara para;
            if (!RichTextParas.TryGetValue(style, out para))
            {
                RichTextParas.TryGetValue("default", out para);
            }
            return para;
        }

        /// <summary>
        /// Create a paragraph for the specified style.
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public Paragraph CreateParagraph(string style)
        {
            //if (Configuration.DebugMode != 0)
            //{
            //    AppLog.Message("RTB: new paragraph of style" + style);
            //}

            RichTextPara attrs = GetParaAttrs(style);

            Paragraph para = new Paragraph();
            para.Margin = new Thickness(attrs.LeftIndent, 0, 0, attrs.BottomMargin);
            para.FontSize = attrs.FontSize;
            para.FontWeight = attrs.FontWeight;
            para.TextAlignment = attrs.TextAlign;
            para.Foreground = attrs.ForegroundColor;

            return para;
        }

        /// <summary>
        /// Create a paragraph for the specified style and sets
        /// the passed text in it.
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public Paragraph CreateParagraphWithText(string style, string text)
        {
            Paragraph para = CreateParagraph(style);

            if (text != null)
            {
                Run r = new Run(text);
                para.Inlines.Add(r);
            }

            return para;
        }

        /// <summary>
        /// Creates a paragraph, sets its text and inserts it
        /// into the Document.
        /// </summary>
        /// <param name="style"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public Paragraph AddNewParagraphToDoc(string style, string text, Paragraph insertAfter = null)
        {
            Paragraph para = CreateParagraphWithText(style, text);
            if (insertAfter == null)
            {
                Document.Blocks.Add(para);
            }
            else
            {
                Document.Blocks.InsertAfter(insertAfter, para);
            }

            return para;
        }

        /// <summary>
        /// Removes empty paragraphs that get created when building the document.
        /// </summary>
        public void RemoveEmptyParagraphs()
        {
            List<Paragraph> parasToRemove = new List<Paragraph>();

            foreach (var para in Document.Blocks)
            {
                if (para is Paragraph)
                {
                    if (((Paragraph)para).Inlines.Count == 0)
                    {
                        parasToRemove.Add((Paragraph)para);
                    }
                }
            }

            foreach (Paragraph para in parasToRemove)
            {
                Document.Blocks.Remove(para);
            }
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
        public Paragraph BuildWorkbookStemLine(TreeNode nd)
        {
            Paragraph para = CreateParagraph("0");
            para.Foreground = CHF_Colors.RTB_GRAY_FOREGROUND;

            string prefix = GetStemLineText(nd);

            Run r = new Run(prefix);
            para.Inlines.Add(r);

            return para;
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
            while (nd != null)
            {
                if (nd.ColorToMove== PieceColor.Black)
                {
                    sbPrefix.Insert(0, nd.MoveNumber.ToString() + "." + nd.LastMoveAlgebraicNotation);
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

using GameTree;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;

namespace ChessForge
{
    /// <summary>
    /// A class for elements to store in the IntroViewClipboard
    /// </summary>
    [Serializable()]
    public class IntroViewElement
    {
        /// <summary>
        /// Default constructor for serialization
        /// </summary>
        public IntroViewElement()
        {
        }

        /// <summary>
        /// Constructs an element of a specified type.
        /// </summary>
        /// <param name="type"></param>
        public IntroViewElement(IntroViewClipboard.ElementType type)
        {
            Type = type;
        }

        /// <summary>
        /// Type of the element.
        /// </summary>
        public IntroViewClipboard.ElementType Type = IntroViewClipboard.ElementType.None;

        /// <summary>
        /// Node id that will be used by some elements.
        /// </summary>
        public int NodeId = -1;

        /// <summary>
        /// Margins as individual values since Thickness class won't serialize.
        /// </summary>
        public double MarginLeft;
        public double MarginTop;
        public double MarginRight;
        public double MarginBottom;

        /// <summary>
        /// If true, set the font as Bold, otherwise Normal.
        /// </summary>
        public bool IsFontWeightBold = false;

        /// <summary>
        /// Font size to use.
        /// </summary>
        public double FontSize = 12;

        /// <summary>
        /// A TreeNode associated with the element if any.
        /// </summary>
        public TreeNode Node = null;

        /// <summary>
        /// Text to use for appropriate types.
        /// </summary>
        public string Text;

        /// <summary>
        /// Uri to use with the Hyperlink type.
        /// </summary>
        public Uri Uri;

        /// <summary>
        /// A boolean value to use for the elements that require it.
        /// </summary>
        public bool? BoolState;

        /// <summary>
        /// Sets margin values from the passed Thickness object.
        /// </summary>
        /// <param name="margin"></param>
        public void SetMargins(Thickness margin)
        {
            MarginLeft = margin.Left;
            MarginTop = margin.Top;
            MarginRight = margin.Right;
            MarginBottom = margin.Bottom;
        }

        /// <summary>
        /// Builds a Thickness object from margin values.
        /// </summary>
        /// <returns></returns>
        public Thickness GetThickness()
        {
            return new Thickness(MarginLeft, MarginTop, MarginRight, MarginBottom);
        }

        /// <summary>
        /// Configures the current object as a Run.
        /// </summary>
        /// <param name="run"></param>
        public void SetAsRun(Run run)
        {
            Type = IntroViewClipboard.ElementType.Run;
            Text = run.Text;
            IsFontWeightBold = run.FontWeight == FontWeights.Bold;
            FontSize = run.FontSize;
        }

        /// <summary>
        /// Configures the current object as a Hyperlink.
        /// </summary>
        /// <param name="hl"></param>
        public void SetAsHyperlink(Hyperlink hl)
        {
            Type = IntroViewClipboard.ElementType.Hyperlink;
            Uri = hl.NavigateUri;

            if (hl.Inlines.Count > 0 && hl.Inlines.FirstInline is Run run)
            {
                Text = run.Text;
                IsFontWeightBold = run.FontWeight == FontWeights.Bold;
                FontSize = run.FontSize;
            }
            else
            {
                Text = "???";
            }
        }

        /// <summary>
        /// Configures the current object as a Move.
        /// </summary>
        /// <param name="run"></param>
        public void SetAsMove(TreeNode node)
        {
            Type = IntroViewClipboard.ElementType.Move;
            Node = node;
        }

        /// <summary>
        /// Configures the current object as a Diagram.
        /// </summary>
        /// <param name="run"></param>
        public void SetAsDiagram(TreeNode node)
        {
            Type = IntroViewClipboard.ElementType.Diagram;
            Node = node;
        }

        /// <summary>
        /// Configures the current object as a Paragraph.
        /// </summary>
        /// <param name="run"></param>
        public void SetAsParagraph(Paragraph para)
        {
            Type = IntroViewClipboard.ElementType.Paragraph;
            IsFontWeightBold = para.FontWeight == FontWeights.Bold;
            FontSize = para.FontSize;
        }

        /// <summary>
        /// Creates a Run object from the values stored in this object.
        /// </summary>
        /// <returns></returns>
        public Run CreateRun()
        {
            Run run = new Run();

            run.Text = Text;
            run.FontWeight = IsFontWeightBold ? FontWeights.Bold : FontWeights.Normal;
            run.FontSize = FontSize;

            return run;
        }

        /// <summary>
        /// Creates a Hyperlink object from the values stored in this object.
        /// </summary>
        /// <returns></returns>
        public Hyperlink CreateHyperlink()
        {
            Run run = CreateRun();

            Hyperlink hl = new Hyperlink(run);
            hl.NavigateUri = Uri;
            hl.ToolTip = Uri.ToString();
            hl.Foreground = ChessForgeColors.CurrentTheme.HyperlinkForeground;

            return hl;
        }

        /// <summary>
        /// Creates a Paragraph object from the values stored in this object.
        /// </summary>
        /// <returns></returns>
        public Paragraph CreateParagraph()
        {
            Paragraph para = new Paragraph();

            para.FontWeight = IsFontWeightBold ? FontWeights.Bold : FontWeights.Normal;
            para.FontSize = FontSize;
            para.Margin = new Thickness(MarginLeft, MarginTop, MarginRight, MarginBottom);

            return para;
        }
    }
}

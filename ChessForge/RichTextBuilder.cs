using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using ChessPosition;
using GameTree;

namespace ChessForge
{
    public abstract class RichTextBuilder
    {
        internal FlowDocument Document;

        internal abstract Dictionary<string, RichTextPara> RichTextParas { get;}

        public RichTextBuilder(FlowDocument doc)
        {
            Document = doc;
        }

        /// <summary>
        /// Return a set of attributes for a given level.
        /// If the level is greater than the greatest defined 
        /// level, return the values for the greates defined one
        /// except for increasing the indent slightly.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public RichTextPara GetParaAttrs(string level)
        {
            RichTextPara para;
            if (!RichTextParas.TryGetValue(level, out para))
            {
                RichTextParas.TryGetValue("default", out para);
            }
            return para;
        }

        /// <summary>
        /// Create a paragraph for the specified level.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public Paragraph CreateParagraph(string level)
        {
            RichTextPara attrs = GetParaAttrs(level);

            Paragraph para = new Paragraph();
            para.Margin = new Thickness(attrs.LeftIndent, 0, 0, attrs.BottomMargin);
            para.FontSize = attrs.FontSize;
            para.FontWeight = attrs.FontWeight;

            return para;
        }

        public Paragraph CreateParagraph(int level)
        {
            return CreateParagraph(level.ToString());
        }

            public Paragraph BuildPrefixParagraph(TreeNode nd)
        {
            Paragraph para = CreateParagraph(0);
            para.Foreground = CHFRG_Colors.RTB_GRAY_FOREGROUND;

            string prefix = GetPrefixText(nd);

            Run r = new Run(prefix);
            para.Inlines.Add(r);

            return para;
        }

        public string GetPrefixText(TreeNode nd)
        {
            StringBuilder sbPrefix = new StringBuilder();
            while (nd != null)
            {
                if (nd.ColorToMove() == PieceColor.Black)
                {
                    sbPrefix.Insert(0, nd.MoveNumber().ToString() + "." + nd.LastMoveAlgebraicNotation);
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

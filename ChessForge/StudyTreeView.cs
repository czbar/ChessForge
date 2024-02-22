using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Derived from VariationTreeView manages the special layout (with variation index)
    /// for the Study View.
    /// </summary>
    public class StudyTreeView : VariationTreeView
    {
        // prefix for Runs in the index paragraph.
        private readonly string _indexrun_ = "indexrun_";

        // indent between levels in the index paragrpah.
        private readonly string _indent = "    ";

        /// <summary>
        /// Object managing the layout for this view
        /// </summary>
        public TreeViewDisplayManager DisplayManager = new TreeViewDisplayManager();

        /// <summary>
        /// Instantiates the view.
        /// </summary>
        /// <param name="rtb"></param>
        /// <param name="contentType"></param>
        /// <param name="entityIndex"></param>
        public StudyTreeView(RichTextBox rtb, GameData.ContentType contentType, int entityIndex) : base(rtb, contentType, entityIndex)
        {
        }

        /// <summary>
        /// Overrides the parent's method building the view with the layout specific
        /// to the Study view.
        /// Unlike, in the parent's view this is method does not call itself recursively.
        /// It builds the list of lines first (there is a recursion there) and only then 
        /// populates the view with the lines.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="para"></param>
        /// <param name="includeNumber"></param>
        override protected void BuildTreeLineText(TreeNode root, Paragraph para, bool includeNumber)
        {
            DisplayManager.BuildLineSectors(root);
            DisplayManager.BuildDisplaySectors();

            CreateVariationIndexPara();
            CreateParagraphs(para);
        }

        /// <summary>
        /// Creates the Index paragraph.
        /// </summary>
        private void CreateVariationIndexPara()
        {
            Paragraph para = CreateParagraph("0", true);
            para.Foreground = Brushes.Blue;
            para.FontWeight = FontWeights.Normal;

            bool first = true;
            foreach (DisplaySector sector in DisplayManager.DisplaySectors)
            {
                int level = sector.LineSector.BranchLevel;
                if (level < TreeViewDisplayManager.SECTION_TITLE_LEVELS)
                {
                    if (first)
                    {
                        Run r = new Run(Properties.Resources.VariationIndex + "\n");
                        para.Inlines.Add(r);
                        para.FontWeight = FontWeights.Bold;
                        first = false;
                    }

                    for (int i = 0; i < level; i++)
                    {
                        Run r = new Run(_indent);
                        para.Inlines.Add(r);
                    }

                    if (sector.Nodes[0].LineId == "1")
                    {
                        foreach (TreeNode nd in sector.Nodes)
                        {
                            Run rMove = BuildIndexNodeAndAddToPara(nd, false, para);
                            rMove.TextDecorations = TextDecorations.Underline;
                            rMove.FontWeight = FontWeights.Bold;
                        }
                    }
                    else
                    {
                        Run rIdTitle = BuildSectionIdTitle(sector.Nodes[0].LineId);
                        para.Inlines.Add(rIdTitle);
                        BuildIndexNodeAndAddToPara(sector.Nodes[0], true, para);
                        rIdTitle.FontWeight = FontWeights.Bold;
                    }

                    para.Inlines.Add(new Run("\n"));
                }
            }

            Document.Blocks.Add(para);
        }

        /// <summary>
        /// Creates paragraphs from the DisplaySectors.
        /// </summary>
        /// <param name="firstPara"></param>
        private void CreateParagraphs(Paragraph firstPara)
        {
            foreach (DisplaySector sector in DisplayManager.DisplaySectors)
            {
                Paragraph para;
                // TODO: redo so that we used the "firstPara" for VariationIndex.
                if (firstPara != null)
                {
                    para = firstPara;
                    firstPara = null;
                }
                else
                {
                    para = CreateParagraph(sector.DisplayLevel.ToString(), true);
                    Thickness margin = GetParagraphMargin((sector.LineSector.BranchLevel - 2).ToString());
                    para.Margin = margin;
                }

                if (sector.LineSector.BranchLevel < TreeViewDisplayManager.SECTION_TITLE_LEVELS)
                {
                    Run rIdTitle = BuildSectionIdTitle(sector.Nodes[0].LineId);
                    para.Inlines.Add(rIdTitle);

                    para.FontWeight = FontWeights.Bold;
                }

                bool includeNumber = true;
                foreach (TreeNode nd in sector.Nodes)
                {
                    BuildNodeTextAndAddToPara(nd, includeNumber, para);
                    includeNumber = false;
                }

                Document.Blocks.Add(para);
            }
        }

        /// <summary>
        /// Builds a Run for the move in the Index paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeNumber"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        protected Run BuildIndexNodeAndAddToPara(TreeNode nd, bool includeNumber, Paragraph para)
        {
            string nodeText = BuildNodeText(nd, includeNumber);

            SolidColorBrush fontColor = null;
            Run rMove = AddIndexRunToParagraph(nd, para, nodeText, fontColor);
            rMove.FontWeight = FontWeights.Normal;
            return rMove;
        }

        /// <summary>
        /// Handles a mouse click on over a move in the index paragraph.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventIndexRunClicked(object sender, MouseButtonEventArgs e)
        {
            Run r = e.Source as Run;
            if (r != null)
            {
                ClearCopySelect();
                int nodeId = TextUtils.GetIdFromPrefixedString(r.Name);
                Run target = _dictNodeToRun[nodeId];
                // we don't want any handling of letf/right button so fake it to Middle
                SelectRun(target, 1, MouseButton.Middle);
                BringSelectedRunIntoView();
            }
        }

        /// <summary>
        /// Adds a Run to the Index paragraph.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="para"></param>
        /// <param name="text"></param>
        /// <param name="fontColor"></param>
        /// <returns></returns>
        private Run AddIndexRunToParagraph(TreeNode nd, Paragraph para, string text, SolidColorBrush fontColor)
        {
            Run r = null;

            try
            {
                r = new Run(text.ToString());
                r.Name = _indexrun_ + nd.NodeId.ToString();
                r.PreviewMouseDown += EventIndexRunClicked;

                // only used the passed fontColor on the first move in the paragraph
                if (fontColor != null && para.Inlines.Count == 0)
                {
                    r.Foreground = fontColor;
                    r.FontWeight = FontWeights.Bold;
                }

                if (para.Margin.Left == 0 && nd.IsMainLine())
                {
                    r.FontWeight = FontWeights.Bold;
                    para.Inlines.Add(r);
                }
                else
                {
                    para.Inlines.Add(r);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("AddIndexRunToParagraph()", ex);
            }

            return r;
        }

        /// <summary>
        /// Builds the section level title in the form of "A.1.2".
        /// </summary>
        /// <param name="lineId"></param>
        /// <returns></returns>
        private Run BuildSectionIdTitle(string lineId)
        {
            Run r = new Run();

            if (lineId != null && lineId != "1")
            {
                StringBuilder sb = new StringBuilder();
                string[] tokens = lineId.Split('.');
                for (int i = 1; i < tokens.Length; i++)
                {
                    if (i == 1)
                    {
                        sb.Append((char)(tokens[1][0] - '1' + 'A'));
                    }
                    else
                    {
                        //if (i > 2)
                        {
                            sb.Append('.');
                        }
                        sb.Append(tokens[i]);
                    }
                }

                sb.Append(") ");
                r.Text = sb.ToString();
            }

            return r;
        }
    }
}

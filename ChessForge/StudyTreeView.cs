using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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

        // indent between levels in the index paragraph.
        private readonly string _indent = "    ";

        /// <summary>
        /// Object managing the layout for this view
        /// </summary>
        public LineSectorManager DisplayManager;

        /// <summary>
        /// Instantiates the view.
        /// </summary>
        /// <param name="rtb"></param>
        /// <param name="contentType"></param>
        /// <param name="entityIndex"></param>
        public StudyTreeView(RichTextBox rtb, GameData.ContentType contentType, int entityIndex) : base(rtb, contentType, entityIndex)
        {
            DisplayManager = new LineSectorManager(this);
        }

        /// <summary>
        /// Returns the variation index depth applicable to this Study Tree.
        /// </summary>
        /// <returns></returns>
        public int VariationIndexDepth
        {
            get
            {
                Chapter chapter = AppState.ActiveChapter;
                if (chapter == null)
                {
                    return Configuration.VariationIndexDepth;
                }
                else
                {
                    return chapter.VariationIndexDepth.Value;
                }
            }
        }

        /// <summary>
        /// Increment the index depth paying attention to limits
        /// and empty level 0 (in case of e.g. 1.e4 and 1.d4)
        /// </summary>
        public void IncrementVariationIndexDepth()
        {
            Chapter chapter = AppState.ActiveChapter;
            if (chapter != null)
            {
                int depth = VariationIndexDepth;
                if (depth == -1 && !DisplayManager.HasIndexLevelZero())
                {
                    depth = 1;
                }
                else
                {
                    if (depth + 1 < DisplayManager.MaxBranchLevel)
                    {
                        depth++;
                    }
                }
                chapter.VariationIndexDepth = depth;
            }
        }

        /// <summary>
        /// Decrement the index depth paying attention to limits
        /// and empty level 0 (in case of e.g. 1.e4 and 1.d4)
        /// </summary>
        public void DecrementVariationIndexDepth()
        {
            Chapter chapter = AppState.ActiveChapter;
            if (chapter != null)
            {
                int depth = VariationIndexDepth;
                if (depth == 1 && !DisplayManager.HasIndexLevelZero())
                {
                    depth = -1;
                }
                else
                {
                    if (depth > -1)
                    {
                        depth--;
                    }
                }
                chapter.VariationIndexDepth = depth;
            }
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

            Chapter chapter = AppState.ActiveChapter;
            if (chapter != null)
            {
                if (chapter.VariationIndexDepth > DisplayManager.MaxBranchLevel)
                {
                    chapter.VariationIndexDepth = DisplayManager.MaxBranchLevel;
                }
                if (chapter.VariationIndexDepth == 0 && !DisplayManager.HasIndexLevelZero())
                {
                    chapter.VariationIndexDepth = -1;
                }
            }

            CreateVariationIndexPara();
            CreateParagraphs(para);
        }
        /// <summary>
        /// Creates the Index paragraph.
        /// </summary>
        private void CreateVariationIndexPara()
        {
            if (VariationIndexDepth > 0 || VariationIndexDepth == 0 && DisplayManager.HasIndexLevelZero())
            {
                Paragraph para = CreateParagraph("0", true);
                para.Foreground = ChessForgeColors.VARIATION_INDEX_FORE;
                para.FontWeight = FontWeights.Normal;
                para.FontSize = para.FontSize - 1;

                bool first = true;
                foreach (LineSector sector in DisplayManager.LineSectors)
                {
                    int level = sector.BranchLevel;
                    if (DisplayManager.IsIndexLevel(level))
                    {
                        if (first)
                        {
                            Run r = new Run(Properties.Resources.VariationIndex + "  ");
                            para.Inlines.Add(r);
                            para.FontWeight = FontWeights.Bold;

                            InsertArrowRuns(para);

                            first = false;
                        }

                        for (int i = 0; i < level; i++)
                        {
                            Run r = new Run(_indent);
                            para.Inlines.Add(r);
                        }

                        if (sector.Nodes[0].LineId == "1")
                        {
                            bool validMove = false;
                            foreach (TreeNode nd in sector.Nodes)
                            {
                                Run rMove = BuildIndexNodeAndAddToPara(nd, false, para);
                                rMove.TextDecorations = TextDecorations.Underline;
                                rMove.FontWeight = FontWeights.Bold;
                                if (nd.NodeId != 0)
                                {
                                    validMove = true;
                                }
                            }

                            if (validMove)
                            {
                                para.Inlines.Add(new Run("\n"));
                            }
                        }
                        else
                        {
                            Run rIdTitle = BuildSectionIdTitle(sector.Nodes[0].LineId);
                            para.Inlines.Add(rIdTitle);
                            if (DisplayManager.IsLastIndexLine(level) || sector.Nodes[sector.Nodes.Count - 1].Children.Count == 0)
                            {
                                BuildIndexNodeAndAddToPara(sector.Nodes[0], true, para);
                            }
                            else
                            {
                                bool firstMove = true;

                                int nodeCount = sector.Nodes.Count;
                                int firstSkip = nodeCount;
                                int lastSkip = -1;

                                if (nodeCount >= 5)
                                {
                                    firstSkip = 2;
                                    lastSkip = nodeCount - 3;
                                }

                                for (int i = 0; i < sector.Nodes.Count; i++)
                                {
                                    if (i < firstSkip || i > lastSkip)
                                    {
                                        TreeNode nd = sector.Nodes[i];
                                        BuildIndexNodeAndAddToPara(nd, firstMove, para);
                                        firstMove = false;
                                    }
                                    else if (i == firstSkip)
                                    {
                                        Run rElipsis = new Run(" [...] ");
                                        rElipsis.FontWeight = FontWeights.Normal;
                                        para.Inlines.Add(rElipsis);
                                        firstMove = true;
                                    }
                                }
                            }
                            rIdTitle.FontWeight = FontWeights.Bold;

                            para.Inlines.Add(new Run("\n"));
                        }
                    }
                }
                Document.Blocks.Add(para);
            }
        }

        /// <summary>
        /// Inserts the Runs with arrow for incrementing/decrementing the Variation Index depth.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="downOnly"></param>
        private void InsertArrowRuns(Paragraph para, bool downOnly = false)
        {
            Run rPlus = new Run(Constants.CHAR_DOWN_ARROW.ToString());
            rPlus.FontWeight = FontWeights.Normal;
            rPlus.Foreground = Brushes.Black;
            rPlus.PreviewMouseDown += EventDownArrowClicked;
            para.Inlines.Add(rPlus);

            if (!downOnly)
            {
                Run rMinus = new Run(Constants.CHAR_UP_ARROW.ToString());
                rMinus.FontWeight = FontWeights.Normal;
                rMinus.Foreground = Brushes.Black;
                rMinus.PreviewMouseDown += EventUpArrowClicked;
                para.Inlines.Add(rMinus);
            }

            para.Inlines.Add(new Run("\n"));
        }

        /// <summary>
        /// Creates paragraphs from the DisplaySectors.
        /// </summary>
        /// <param name="firstPara"></param>
        private void CreateParagraphs(Paragraph firstPara)
        {
            foreach (LineSector sector in DisplayManager.LineSectors)
            {
                if (sector.Nodes.Count == 0 || sector.Nodes.Count == 1 && sector.Nodes[0].NodeId == 0)
                {
                    continue;
                }

                try
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
                        if (sector.DisplayLevel < 0)
                        {
                            sector.DisplayLevel = 0;
                        }
                        para = CreateParagraph(sector.DisplayLevel.ToString(), true);
                        Thickness margin = GetParagraphMargin((sector.DisplayLevel).ToString());
                        para.Margin = margin;
                    }

                    if (DisplayManager.IsIndexLevel(sector.BranchLevel))
                    {
                        Run rIdTitle = BuildSectionIdTitle(sector.Nodes[0].LineId);
                        rIdTitle.Foreground = ChessForgeColors.INDEX_SECTION_TITLE;
                        para.Inlines.Add(rIdTitle);

                        para.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        if (DisplayManager.IsLastIndexLine(sector.DisplayLevel + 1))
                        {
                            para.FontWeight = FontWeights.Bold;
                        }
                    }

                    bool includeNumber = true;
                    bool parenthesis = false;

                    foreach (TreeNode nd in sector.Nodes)
                    {
                        if (nd.NodeId == -100)
                        {
                            para.Inlines.Add(new Run("("));
                            parenthesis = true;
                        }
                        else if (nd.NodeId == -101)
                        {
                            para.Inlines.Add(new Run(") "));
                            parenthesis = true;
                        }
                        else
                        {
                            if (parenthesis)
                            {
                                includeNumber = true;
                            }
                            BuildNodeTextAndAddToPara(nd, includeNumber, para, sector.DisplayLevel);
                            parenthesis = false;
                        }
                        includeNumber = false;
                    }

                    Document.Blocks.Add(para);
                }
                catch
                {
                }
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

        // guid for checking single click versus double click.
        private Guid _clickPageHeaderGuid;

        /// <summary>
        /// Adds the single-click behaviour to the base function.
        /// When the header is clicked while there is no Variation Index,
        /// it will be created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        override protected void EventPageHeaderClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ClickCount == 1)
                {
                    // generate fresh guid to hold in Delay
                    Guid guid = Guid.NewGuid();
                    _clickPageHeaderGuid = guid;

                    Task.Run(async () =>
                    {
                        // Delay in case the double-click is coming.
                        await Task.Delay(300);

                        // if new guid was generated, it means that there was a second click
                        if (guid == _clickPageHeaderGuid)
                        {
                            if (VariationIndexDepth == -1)
                            {
                                IncrementVariationIndexDepth();
                            }

                            _mainWin.Dispatcher.Invoke(() =>
                            {
                                BuildFlowDocumentForVariationTree();
                            });

                            e.Handled = true;
                            return;
                        }
                    });
                }
                else if (e.ClickCount == 2)
                {
                    _clickPageHeaderGuid = Guid.NewGuid();
                    base.EventPageHeaderClicked(sender, e);
                }
            }
            catch (Exception ex)
            {
                AppLog.Message("EventPageHeaderClicked()", ex);
            }
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
        /// The Up Arrow to decrement the depth of the Variation Index was clicked. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventUpArrowClicked(object sender, MouseButtonEventArgs e)
        {
            Run r = e.Source as Run;
            if (r != null)
            {
                if (VariationIndexDepth > -1)
                {
                    Chapter chapter = AppState.ActiveChapter;
                    if (chapter != null)
                    {
                        DecrementVariationIndexDepth();
                    }
                }
                BuildFlowDocumentForVariationTree();
            }
            e.Handled = true;
        }

        /// <summary>
        /// The Down Arrow to increment the depth of the Variation Index was clicked. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventDownArrowClicked(object sender, MouseButtonEventArgs e)
        {
            Run r = e.Source as Run;
            if (r != null)
            {
                if (VariationIndexDepth < Configuration.MAX_INDEX_DEPTH && VariationIndexDepth < DisplayManager.MaxBranchLevel - 1)
                {
                    Chapter chapter = AppState.ActiveChapter;
                    if (chapter != null)
                    {
                        IncrementVariationIndexDepth();
                    }
                }
                BuildFlowDocumentForVariationTree();
            }
            e.Handled = true;
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

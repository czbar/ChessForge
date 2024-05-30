using ChessPosition;
using System;
using System.IO;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;
using System.Text;
using System.Windows.Controls;
using GameTree;
using System.Collections.Generic;

namespace ChessForge
{
    /// <summary>
    /// Manages outputting the content of the workbook's articles
    /// to an RTF file.
    /// </summary>
    public class RtfWriter
    {
        /// <summary>
        /// Writes out an RTF document representing the passed FlowDocument.
        /// </summary>
        /// <param name="guiDoc"></param>
        /// <param name="tree"></param>
        public static void WriteRtf(FlowDocument guiDoc, VariationTree tree)
        {
            if (guiDoc != null)
            {
                FlowDocument printDoc = CreateDocumentForPrint(guiDoc, tree, out List<RtfDiagram> diagrams);

                // Create a TextRange covering the entire content of the FlowDocument
                TextRange textRange = new TextRange(printDoc.ContentStart, printDoc.ContentEnd);

                string distinct = "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = DebugUtils.BuildLogFileName(App.AppPath, "rtf", distinct, "rtf");

                string rtfContent;

                using (MemoryStream stream = new MemoryStream())
                {
                    // Save the content in RTF format
                    textRange.Save(stream, DataFormats.Rtf);
                    rtfContent = Encoding.UTF8.GetString(stream.ToArray());
                }

                foreach (RtfDiagram diagram in diagrams)
                {
                    string diag = BuildTextForDiagram(diagram);
                    string ph = DiagramPlaceHolder(diagram.Node.NodeId);
                    rtfContent = rtfContent.Replace(ph, diag);
                }

                File.WriteAllText(fileName, rtfContent);
            }
        }

        /// <summary>
        /// Builds text for a diagram representing the passed TreeNode.
        /// TODO: handle "flipped".
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private static string BuildTextForDiagram(RtfDiagram diag)
        {
            StringBuilder sb = new StringBuilder();
            if (diag.Node != null)
            {
                byte[] diagram = PositionImageGenerator.GenerateImage(diag);
                string rtfImage = GetImageRtf(diagram);
                //string rtfImageTag = $@"{{\pict\pngblip\picw{ChessBoards.ChessBoardGreySmall.PixelWidth}\pich{ChessBoards.ChessBoardGreySmall.PixelHeight}\picwgoal{ChessBoards.ChessBoardGreySmall.PixelWidth * 15}\pichgoal{ChessBoards.ChessBoardGreySmall.PixelHeight * 15} {rtfImage}}}";
                string rtfImageTag = @"{\pict\pngblip\picw" + "242" + @"\pich" + "242" +
                                            @"\picwgoal" + "2800" + @"\pichgoal" + "2800" +
                                            @"\bin " + rtfImage + "}";

                sb.Append(rtfImageTag);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Codes the image byta array into an RTF format string.
        /// </summary>
        /// <param name="imageBytes"></param>
        /// <returns></returns>
        private static string GetImageRtf(byte[] imageBytes)
        {
            StringBuilder rtf = new StringBuilder();
            foreach (byte b in imageBytes)
            {
                rtf.AppendFormat("{0:X2}", b);
            }
            return rtf.ToString();
        }

        /// <summary>
        /// Generates FlowDocument suitable for dumping in the RTF format
        /// after diagram placeholders have been replaced by the caller.
        /// </summary>
        /// <param name="guiDoc"></param>
        /// <param name="diagrams"></param>
        /// <returns></returns>
        private static FlowDocument CreateDocumentForPrint(FlowDocument guiDoc, VariationTree tree, out List<RtfDiagram> diagrams)
        {
            diagrams = new List<RtfDiagram>();
            FlowDocument printDoc = new FlowDocument();

            bool lastRunWasIntroMove = false;
            foreach (Block block in guiDoc.Blocks)
            {
                lastRunWasIntroMove = false;
                if (block is Paragraph para)
                {
                    Paragraph printPara = new Paragraph();
                    CopyParaAttributes(printPara, para);
                    printDoc.Blocks.Add(printPara);

                    if (para.Name != null && para.Name.StartsWith(RichTextBoxUtilities.DiagramParaPrefix))
                    {
                        ProcessDiagram(printPara, para, tree, diagrams);
                    }
                    else
                    {
                        foreach (Inline inl in para.Inlines)
                        {
                            if (inl is Run run)
                            {
                                lastRunWasIntroMove = false;
                                // TODO: do a proper split, as this can be in the middle of a run too
                                if (run.Text == "\n")
                                {
                                    printDoc.Blocks.Add(printPara);
                                    printPara = new Paragraph();
                                }

                                ProcessTextRun(run, printPara);
                            }
                            else if (inl is InlineUIContainer uic)
                            {
                                if (inl.Name != null && inl.Name.StartsWith(RichTextBoxUtilities.UicMovePrefix))
                                {
                                    lastRunWasIntroMove = ProcessIntroMove(printPara, uic, lastRunWasIntroMove);
                                }
                                else
                                {
                                    lastRunWasIntroMove = false;
                                }
                            }
                        }
                    }
                }
                else
                {
                    AppLog.Message("RTF Write: Block element type = " + block.GetType().ToString());
                }
            }

            return printDoc;
        }

        /// <summary>
        /// Processes a single "simple text" Run. 
        /// </summary>
        /// <param name="run"></param>
        /// <param name="printPara"></param>
        private static void ProcessTextRun(Run run, Paragraph printPara)
        {
            Run printRun = new Run();
            CopyRunAttributes(printRun, run);
            printPara.Inlines.Add(printRun);
        }

        /// <summary>
        /// Processes a paragraph representing a diagram.
        /// </summary>
        /// <param name="printPara"></param>
        /// <param name="para"></param>
        /// <param name="tree"></param>
        /// <param name="diagrams"></param>
        private static void ProcessDiagram(Paragraph printPara, Paragraph para, VariationTree tree, List<RtfDiagram> diagrams)
        {
            int nodeId = TextUtils.GetIdFromPrefixedString(para.Name);
            if (tree != null)
            {
                TreeNode nd = tree.GetNodeFromNodeId(nodeId);
                if (nd != null)
                {
                    bool isFlipped = RichTextBoxUtilities.GetDiagramFlipState(para);
                    printPara.Inlines.Add(CreateDiagramPlaceholderRun(nodeId));
                    diagrams.Add(new RtfDiagram(nd, isFlipped));
                }
            }
        }

        /// <summary>
        /// Process a special Move encoding found in the Intro view.
        /// </summary>
        /// <param name="printPara"></param>
        /// <param name="uic"></param>
        /// <param name="lastRunWasIntroMove"></param>
        /// <returns></returns>
        private static bool ProcessIntroMove(Paragraph printPara, InlineUIContainer uic, bool lastRunWasIntroMove)
        {
            bool handled = false;

            // there should be just one child of type TextBlock
            var tb = uic.Child;
            if (tb != null && tb is TextBlock tbMove)
            {
                // this should have just one inline of type Run
                if (tbMove.Inlines.Count > 0)
                {
                    Run rMove = tbMove.Inlines.FirstInline as Run;
                    if (rMove != null && rMove.Text != null)
                    {
                        Run moveRun = new Run();
                        CopyRunAttributes(moveRun, rMove);
                        if (lastRunWasIntroMove && moveRun.Text != null && moveRun.Text[0] == ' ')
                        {
                            moveRun.Text = moveRun.Text.Substring(1);
                        }
                        printPara.Inlines.Add(moveRun);
                        handled = true;
                    }
                }
            }

            return handled;
        }

        /// <summary>
        /// Applies attributes of the source Paragraph to the target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        private static void CopyParaAttributes(Paragraph target, Paragraph source)
        {
            target.Foreground = Brushes.Black;
            target.Background = Brushes.White;
            target.FontWeight = source.FontWeight;
            if (source.FontWeight == FontWeights.SemiBold)
            {
                target.FontWeight = FontWeights.Bold;
            }
            target.FontSize = source.FontSize;
            target.TextDecorations = source.TextDecorations;
            target.Margin = source.Margin;
        }

        /// <summary>
        /// Applies attributes of the source Run to the target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        private static void CopyRunAttributes(Run target, Run source)
        {
            target.Text = source.Text;
            target.Foreground = Brushes.Black;
            target.Background = Brushes.White;
            target.FontWeight = source.FontWeight;
            if (source.FontWeight == FontWeights.SemiBold)
            {
                target.FontWeight = FontWeights.Bold;
            }
            target.FontSize = source.FontSize;
            target.TextDecorations = source.TextDecorations;
        }

        /// <summary>
        /// Creates a Run with the diagram placeholder text.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private static Run CreateDiagramPlaceholderRun(int nodeId)
        {
            Run run = new Run(DiagramPlaceHolder(nodeId));
            return run;
        }

        /// <summary>
        /// Text for a diagram placeholder
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private static string DiagramPlaceHolder(int nodeId)
        {
            return "<Diagram " + nodeId + ">";
        }
    }

    /// <summary>
    /// Holds attributes of the diagram to generate an image for.
    /// </summary>
    public class RtfDiagram
    {
        /// <summary>
        /// TreeNode represented by the diagram.
        /// </summary>
        public TreeNode Node;

        /// <summary>
        /// Whether the diagram should be flipped.
        /// </summary>
        public bool IsFlipped;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="isFlipped"></param>
        public RtfDiagram(TreeNode node, bool isFlipped)
        {
            Node = node;
            IsFlipped = isFlipped;
        }
    }
}

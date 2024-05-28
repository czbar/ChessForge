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
                FlowDocument printDoc = CreateDocumentForPrint(guiDoc, tree, out List<int> diagrams);

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

                foreach (int nodeId in diagrams)
                {
                    string diag = BuildTextForDiagram(tree.GetNodeFromNodeId(nodeId));
                    rtfContent = rtfContent.Replace("<Diagram " + nodeId.ToString() + ">", diag);
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
        private static string BuildTextForDiagram(TreeNode nd)
        {
            StringBuilder sb = new StringBuilder();
            if (nd != null)
            {
                byte[] diagram = PositionImageGenerator.GenerateImage(null);
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
        private static FlowDocument CreateDocumentForPrint(FlowDocument guiDoc, VariationTree tree, out List<int> diagrams)
        {
            diagrams = new List<int>();

            FlowDocument printDoc = new FlowDocument();

            foreach (Block block in guiDoc.Blocks)
            {
                if (block is Paragraph para)
                {
                    Paragraph printPara = new Paragraph();
                    CopyParaAttributes(printPara, para);
                    printDoc.Blocks.Add(printPara);

                    if (para.Name != null && para.Name.StartsWith(RichTextBoxUtilities.DiagramParaPrefix))
                    {
                        int nodeId = TextUtils.GetIdFromPrefixedString(para.Name);
                        if (tree != null)
                        {
                            TreeNode nd = tree.GetNodeFromNodeId(nodeId);
                            if (nd != null)
                            {
                                printPara.Inlines.Add(CreateDiagramPlaceholderRun(nodeId));
                                diagrams.Add(nodeId);
                            }
                        }
                    }
                    else
                    {
                        foreach (Inline inl in para.Inlines)
                        {
                            if (inl is Run run)
                            {
                                // TODO: do a proper split, as this can be in the middle of a run too
                                if (run.Text == "\n")
                                {
                                    printDoc.Blocks.Add(printPara);
                                    printPara = new Paragraph();
                                }

                                Run printRun = new Run();
                                CopyRunAttributes(printRun, run);
                                printPara.Inlines.Add(printRun);
                            }
                            else
                            {
                                AppLog.Message("RTF Write: Inline element type = " + block.GetType().ToString());
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
}

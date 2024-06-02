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
        /// Exports the passed chapter into an RTF file.
        /// </summary>
        /// <param name="chapter"></param>
        public static void WriteRtf(Chapter chapter)
        {
            List<FlowDocument> docs = new List<FlowDocument>();

            FlowDocument printDoc = new FlowDocument();
            List<RtfDiagram> diagrams = new List<RtfDiagram>();
            if (chapter != null)
            {
                if (!chapter.IsIntroEmpty())
                {
                    FlowDocument guiDoc = new FlowDocument();
                    RichTextBox rtb = new RichTextBox();
                    IntroView introView = new IntroView(guiDoc, chapter, rtb);
                    docs.Add(CreateDocumentForPrint(printDoc, rtb.Document, chapter.Intro.Tree, ref diagrams));
                }

                RichTextBox rtbStudy = new RichTextBox();
                StudyTreeView studyView = new StudyTreeView(rtbStudy, GameData.ContentType.STUDY_TREE, 0);
                studyView.BuildFlowDocumentForVariationTree(chapter.StudyTree.Tree);
                docs.Add(CreateDocumentForPrint(printDoc, rtbStudy.Document, chapter.StudyTree.Tree, ref diagrams));

                for (int i = 0; i < chapter.ModelGames.Count; i++)
                {
                    FlowDocument guiDoc = new FlowDocument();
                    VariationTreeView gameView = new VariationTreeView(guiDoc, GameData.ContentType.MODEL_GAME, i);
                    gameView.BuildFlowDocumentForVariationTree(chapter.ModelGames[i].Tree);
                    //gameView.BuildFlowDocumentForPrint(guiDoc, chapter.ModelGames[i].Tree);
                    docs.Add(CreateDocumentForPrint(printDoc, guiDoc, chapter.ModelGames[i].Tree, ref diagrams));
                }

                for (int i = 0; i < chapter.Exercises.Count; i++)
                {
                    FlowDocument guiDoc = new FlowDocument();
                    VariationTreeView exerciseView = new ExerciseTreeView(guiDoc, GameData.ContentType.EXERCISE, i);
                    exerciseView.BuildFlowDocumentForVariationTree(chapter.Exercises[i].Tree);
                    docs.Add(CreateDocumentForPrint(printDoc, guiDoc, chapter.Exercises[i].Tree, ref diagrams));
                }
            }

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
                string ph = DiagramPlaceHolder(diagram.DiagramId);
                rtfContent = rtfContent.Replace(ph, diag);
            }

            File.WriteAllText(fileName, rtfContent);
        }

        /// <summary>
        /// Builds text for a diagram representing the passed TreeNode.
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
        /// Codes the image byte array into an RTF format string.
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
        private static FlowDocument CreateDocumentForPrint(FlowDocument printDoc, FlowDocument guiDoc, VariationTree tree, ref List<RtfDiagram> diagrams)
        {
            int diagramId = 0;

            bool lastRunWasIntroMove = false;
            Paragraph lastDiagramPara = null;

            foreach (Block block in guiDoc.Blocks)
            {
                lastRunWasIntroMove = false;
                if (block is Paragraph guiPara)
                {
                    Paragraph printPara = new Paragraph();
                    CopyParaAttributes(printPara, guiPara);

                    printDoc.Blocks.Add(printPara);

                    if (guiPara.Name != null && guiPara.Name.StartsWith(RichTextBoxUtilities.DiagramParaPrefix))
                    {
                        diagramId++;
                        ProcessDiagram(diagramId, printPara, guiPara, tree, diagrams);
                        lastDiagramPara = printPara;
                    }
                    else if (guiPara.Name == RichTextBoxUtilities.ExerciseUnderBoardControls)
                    {
                        printPara.Margin = new Thickness(30, 0, 0, 0);
                        if (tree.Nodes.Count > 0)
                        {
                            Run run = new Run();
                            run.FontSize = printPara.FontSize + 4;
                            if (tree.Nodes[0].ColorToMove == PieceColor.White)
                            {
                                run.Text = " " + Constants.CHAR_WHITE_LARGE_TRIANGLE_UP.ToString();
                            }
                            else
                            {
                                run.Text = " " + Constants.CHAR_BLACK_LARGE_TRIANGLE_DOWN.ToString();
                            }
                            lastDiagramPara.Inlines.Add(run);
                            printDoc.Blocks.Remove(printPara);
                        }
                    }
                    else
                    {
                        foreach (Inline inl in guiPara.Inlines)
                        {
                            if (inl is Run run && run.Text != null)
                            {
                                lastRunWasIntroMove = false;

                                // if Run's text contains newline chars, create a new para for each (otherwise, no new line in RTF)
                                string[] tokens = run.Text.Split('\n');
                                for (int i = 0; i < tokens.Length; i++)
                                {
                                    if (i == 0)
                                    {
                                        ProcessTextRun(run, tokens[0], printPara);
                                    }
                                    else
                                    {
                                        printDoc.Blocks.Add(printPara);
                                        printPara = new Paragraph();
                                        ProcessTextRun(run, tokens[i], printPara);
                                    }
                                }
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

                    if (guiPara.Name == RichTextBoxUtilities.HeaderParagraphName)
                    {
                        printDoc.Blocks.Add(new Paragraph());
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
        private static void ProcessTextRun(Run run, string txt, Paragraph printPara)
        {
            if (!string.IsNullOrEmpty(run.Text))
            {
                Run printRun = new Run();
                CopyRunAttributes(printRun, run);
                printRun.Text = GuiUtilities.RemoveCharsFromString(txt);
                printPara.Inlines.Add(printRun);
            }
        }

        /// <summary>
        /// Processes a paragraph representing a diagram.
        /// </summary>
        /// <param name="printPara"></param>
        /// <param name="para"></param>
        /// <param name="tree"></param>
        /// <param name="diagrams"></param>
        private static void ProcessDiagram(int diagramId, Paragraph printPara, Paragraph para, VariationTree tree, List<RtfDiagram> diagrams)
        {
            int nodeId = TextUtils.GetIdFromPrefixedString(para.Name);
            if (tree != null)
            {
                TreeNode nd = tree.GetNodeFromNodeId(nodeId);
                if (nd != null)
                {
                    bool isFlipped = RichTextBoxUtilities.GetDiagramFlipState(para);
                    printPara.Inlines.Add(CreateDiagramPlaceholderRun(diagramId));
                    diagrams.Add(new RtfDiagram(diagramId, nd, isFlipped));
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
        /// <param name="diagramId"></param>
        /// <returns></returns>
        private static Run CreateDiagramPlaceholderRun(int diagramId)
        {
            Run run = new Run(DiagramPlaceHolder(diagramId));
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
        /// An ID uniquely identifying the diagram while
        /// the document is being built.
        /// </summary>
        public int DiagramId;

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
        public RtfDiagram(int diagramId, TreeNode node, bool isFlipped)
        {
            DiagramId = diagramId;
            Node = node;
            IsFlipped = isFlipped;
        }
    }
}

using ChessPosition;
using System;
using System.IO;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Manages outputting the content of the workbook's articles
    /// to an RTF file.
    /// </summary>
    public class RtfWriter
    {
        public static void WriteRtf(FlowDocument guiDoc)
        {
            if (guiDoc != null)
            {
                FlowDocument printDoc = CreateDocumentForPrint(guiDoc);

                // Create a TextRange covering the entire content of the FlowDocument
                TextRange textRange = new TextRange(printDoc.ContentStart, printDoc.ContentEnd);

                string distinct = "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = DebugUtils.BuildLogFileName(App.AppPath, "rtf", distinct, "rtf");
                // Create a file stream to save the RTF content
                using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
                {
                    // Save the content in RTF format
                    textRange.Save(fileStream, DataFormats.Rtf);
                }
            }
        }

        private static FlowDocument CreateDocumentForPrint(FlowDocument guiDoc)
        {
            FlowDocument printDoc = new FlowDocument();

            foreach (Block block in guiDoc.Blocks)
            {
                if (block is Paragraph para)
                {
                    Paragraph printPara = new Paragraph();
                    printPara.Foreground = Brushes.Black;
                    printPara.Background = Brushes.White;
                    printPara.FontWeight = para.FontWeight;
                    if (para.FontWeight == FontWeights.SemiBold)
                    {
                        printPara.FontWeight = FontWeights.Bold;
                    }
                    printPara.FontSize = para.FontSize;
                    printPara.TextDecorations = para.TextDecorations;
                    printPara.Margin = para.Margin;

                    printDoc.Blocks.Add(printPara);

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
                            printRun.Text = run.Text;
                            if (run.FontWeight == FontWeights.SemiBold)
                            {
                                printRun.FontWeight = FontWeights.Bold;
                            }
                            printRun.Foreground = Brushes.Black;
                            printRun.Background = Brushes.White;
                            printRun.FontWeight = run.FontWeight;
                            printRun.FontSize = run.FontSize;
                            printRun.TextDecorations = run.TextDecorations;

                            printPara.Inlines.Add(printRun);
                        }
                        else
                        {
                            AppLog.Message("RTF Write: Inline element type = " + block.GetType().ToString());
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
    }
}

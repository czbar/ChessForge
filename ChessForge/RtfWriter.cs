using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// The scope of Export/Print operation.
    /// </summary>
    public enum PrintScope
    {
        ARTICLE,
        CHAPTER,
        WORKBOOK,
    }

    /// <summary>
    /// Manages outputting the content of the workbook's articles
    /// to an RTF file.
    /// </summary>
    public class RtfWriter
    {
        // TODO: these fields are meant to be Configuration items.
        private static string _termForExercise = Properties.Resources.Exercise;
        private static bool _continuousArticleNumbering = true;

        // counts exported games if _continuousArticleNumbering is on 
        private static int _currentGameNumber = 0;

        // counts exported exercises if _continuousArticleNumbering is on 
        private static int _currentExerciseNumber = 0;

        // scope of the export/print operation
        private static PrintScope _printScope;

        // Chapter being printed
        private static Chapter _chapterToPrint;

        // Article being printed
        private static Article _articleToPrint;

        // running id of the diagrams in the document
        private static int diagramId = 0;


        /// <summary>
        /// Exports the passed chapter into an RTF file.
        /// </summary>
        /// <param name="chapter"></param>
        public static void WriteRtf(PrintScope scope, Chapter ch, Article article)
        {
            ResetCounters();

            _printScope = scope;
            _chapterToPrint = ch;
            _articleToPrint = article;

            // we only use Fixed Font size when "printing".  Set Configuration.UseFixedFont to true temporarily
            bool saveUseFixedFont = Configuration.UseFixedFont;
            Configuration.UseFixedFont = true;

            bool isFirstPrintPage = true;

            try
            {
                FlowDocument printDoc = new FlowDocument();
                List<RtfDiagram> diagrams = new List<RtfDiagram>();

                if (scope == PrintScope.WORKBOOK)
                {
                    FlowDocument workbookFP = PrintWorkbookFrontPage();
                    CreateDocumentForPrint(printDoc, workbookFP, null, ref diagrams);
                    isFirstPrintPage = false;
                }

                foreach (Chapter chapter in AppState.Workbook.Chapters)
                {
                    if (_printScope == PrintScope.WORKBOOK || _printScope == PrintScope.CHAPTER && ch == chapter)
                    {
                        FlowDocument chapterTitleDoc = PrintChapterTitle(chapter);
                        if (!isFirstPrintPage)
                        {
                            AddPageBreakPlaceholder(chapterTitleDoc);
                        }
                        else
                        {
                            isFirstPrintPage = false;
                        }
                        CreateDocumentForPrint(printDoc, chapterTitleDoc, null, ref diagrams);

                        if (!chapter.IsIntroEmpty())
                        {
                            FlowDocument doc = PrintIntro(chapter);
                            CreateDocumentForPrint(printDoc, doc, chapter.Intro.Tree, ref diagrams);
                        }

                        if (!chapter.IsStudyEmpty())
                        {
                            // if we are printing just this study or there was no Intro, print without the header and page break.
                            if (scope != PrintScope.ARTICLE && !chapter.IsIntroEmpty())
                            {
                                FlowDocument docStudyHeader = PrintStudyHeader();
                                AddPageBreakPlaceholder(docStudyHeader);
                                CreateDocumentForPrint(printDoc, docStudyHeader, null, ref diagrams);
                            }

                            FlowDocument doc = PrintStudy(chapter);
                            CreateDocumentForPrint(printDoc, doc, chapter.StudyTree.Tree, ref diagrams);
                        }

                        if (chapter.ModelGames.Count > 0)
                        {
                            FlowDocument docGamesHeader = PrintGamesHeader();
                            AddPageBreakPlaceholder(docGamesHeader);
                            CreateDocumentForPrint(printDoc, docGamesHeader, null, ref diagrams);

                            for (int i = 0; i < chapter.ModelGames.Count; i++)
                            {
                                FlowDocument guiDoc = PrintGame(chapter.ModelGames[i], i, ref diagrams);
                                CreateDocumentForPrint(printDoc, guiDoc, chapter.ModelGames[i].Tree, ref diagrams);
                            }
                        }

                        if (chapter.Exercises.Count > 0)
                        {
                            FlowDocument docExercisesHeader = PrintExercisesHeader();
                            AddPageBreakPlaceholder(docExercisesHeader);
                            CreateDocumentForPrint(printDoc, docExercisesHeader, null, ref diagrams);

                            for (int i = 0; i < chapter.Exercises.Count; i++)
                            {
                                FlowDocument guiDoc = PrintExercise(printDoc, chapter.Exercises[i], i, ref diagrams);
                                CreateDocumentForPrint(printDoc, guiDoc, chapter.Exercises[i].Tree, ref diagrams);
                            }
                        }
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

                rtfContent = CreateFinalRtfString(rtfContent, diagrams);

                File.WriteAllText(fileName, rtfContent);
            }
            catch (Exception ex)
            {
                AppLog.Message("WriteRtf()", ex);
            }
            finally
            {
                Configuration.UseFixedFont = saveUseFixedFont;
            }
        }

        /// <summary>
        /// Generates the string ready to be exported as RTF file.
        /// Performs the placeholder replacements and creates diagrams.
        /// </summary>
        /// <param name="rtfContent"></param>
        /// <param name="diagrams"></param>
        /// <returns></returns>
        private static string CreateFinalRtfString(string rtfContent, List<RtfDiagram> diagrams)
        {
            rtfContent = InsertColumnFormatting(rtfContent);

            foreach (RtfDiagram diagram in diagrams)
            {
                string diag = BuildTextForDiagram(diagram);
                string ph = DiagramPlaceHolder(diagram.DiagramId);
                rtfContent = rtfContent.Replace(ph, diag);
            }

            return rtfContent;
        }

        /// <summary>
        /// Creates a flow document with the Workbook's title.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static FlowDocument PrintWorkbookTitle(Chapter chapter)
        {
            FlowDocument doc = new FlowDocument();

            string title = AppState.Workbook.Title;

            Paragraph paraTitle = new Paragraph();
            paraTitle.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 6;
            paraTitle.FontWeight = FontWeights.Bold;
            paraTitle.TextAlignment = TextAlignment.Center;
            paraTitle.Inlines.Add(new Run(title));
            doc.Blocks.Add(paraTitle);

            // add a dummy paragraph a a spacer before the further content
            Paragraph paraDummy = new Paragraph();
            paraDummy.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 2;
            doc.Blocks.Add(paraDummy);

            return doc;
        }

        /// <summary>
        /// Creates FlowDocument for the front page of the Workbook.
        /// </summary>
        /// <returns></returns>
        private static FlowDocument PrintWorkbookFrontPage()
        {
            FlowDocument doc = new FlowDocument();
            
            Run cfInfo = new Run();
            StringBuilder sb = new StringBuilder();
            sb.Append(Properties.Resources.ChessForgeGenNotice);
            sb.Append(" " + Constants.CHAR_SUPER_LEFT_PARENTHESIS + Constants.CHAR_TRADE_MARK + Constants.CHAR_SUPER_RIGHT_PARENTHESIS);
            cfInfo.Text = sb.ToString();
            Paragraph paraChessForgeInfo = CreateParagraphForFrontPage(0, 60, cfInfo);
            doc.Blocks.Add(paraChessForgeInfo);

            
            Run runTitle = new Run(AppState.Workbook.Title);
            runTitle.FontWeight = FontWeights.Bold;
            Paragraph paraTitle = CreateParagraphForFrontPage(8, 10, runTitle);
            doc.Blocks.Add(paraTitle);


            Run runVersion = new Run(Properties.Resources.Version + ": " + AppState.Workbook.Version);
            Paragraph paraVersion = CreateParagraphForFrontPage(-2, 50, runVersion);
            doc.Blocks.Add(paraVersion);

            if (!string.IsNullOrEmpty(AppState.Workbook.Author))
            {
                Run runAuthorPrefix = new Run(Properties.Resources.Author + ": ");
                Run runAuthor = new Run(AppState.Workbook.Author);
                runAuthor.FontWeight = FontWeights.Bold;
                Paragraph paraAuthor = CreateParagraphForFrontPage(0, 20, runAuthorPrefix);
                paraAuthor.Inlines.Add(runAuthor);
                doc.Blocks.Add(paraAuthor);
            }

            return doc;
        }

        /// <summary>
        /// Creates a paragraph for use when building the Front Page FlowDocument.
        /// </summary>
        /// <param name="adjFontSize"></param>
        /// <param name="bottomMargin"></param>
        /// <param name="run"></param>
        /// <returns></returns>
        private static Paragraph CreateParagraphForFrontPage(int adjFontSize, double bottomMargin, Run run)
        {
            Paragraph para = new Paragraph();

            para.Margin = new Thickness(0, 0, 0, bottomMargin);
            para.TextAlignment = TextAlignment.Center;
            para.FontSize = Constants.BASE_FIXED_FONT_SIZE + adjFontSize;
            para.Inlines.Add(run);

            return para;
        }

        /// <summary>
        /// Creates a flow document with the Chapter's title.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static FlowDocument PrintChapterTitle(Chapter chapter)
        {
            FlowDocument doc = new FlowDocument();

            Paragraph paraChapterNo = new Paragraph();
            paraChapterNo.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 4;
            paraChapterNo.FontWeight = FontWeights.Bold;

            string title = chapter.Title;
            int chapterNo = chapter.Index + 1;

            Run runNo = new Run();
            runNo.Text = Properties.Resources.Chapter + " " + chapterNo.ToString();
            paraChapterNo.Inlines.Add(runNo);
            doc.Blocks.Add(paraChapterNo);

            // if title is empty, do not include the second paragraph
            if (!string.IsNullOrWhiteSpace(title))
            {
                Paragraph paraDummy1 = new Paragraph();
                paraDummy1.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
                doc.Blocks.Add(paraDummy1);

                Paragraph paraChapterTitle = new Paragraph();
                paraChapterTitle.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 2;
                paraChapterTitle.FontWeight = FontWeights.Bold;
                paraChapterTitle.TextAlignment = TextAlignment.Center;
                paraChapterTitle.Inlines.Add(new Run(title));
                doc.Blocks.Add(paraChapterTitle);
            }

            // add a dummy paragraph a a spacer before the further content
            Paragraph paraDummy2 = new Paragraph();
            paraDummy2.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
            doc.Blocks.Add(paraDummy2);

            return doc;
        }

        /// <summary>
        /// Creates a flow document with the Study header.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static FlowDocument PrintStudyHeader()
        {
            FlowDocument doc = new FlowDocument();

            // add a dummy paragraph a a spacer before the further content
            Paragraph paraDummy1 = new Paragraph();
            paraDummy1.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 4;
            doc.Blocks.Add(paraDummy1);

            Paragraph para = new Paragraph();
            para.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 2;
            para.FontWeight = FontWeights.Bold;
            para.TextAlignment = TextAlignment.Center;
            string studyTitle = Properties.Resources.Study;
            para.Inlines.Add(new Run(studyTitle));
            doc.Blocks.Add(para);

            return doc;
        }

        /// <summary>
        /// Creates a flow document with the Games header.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static FlowDocument PrintGamesHeader()
        {
            FlowDocument doc = new FlowDocument();

            // add a dummy paragraph a a spacer before the further content
            Paragraph paraDummy1 = new Paragraph();
            paraDummy1.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 8;
            doc.Blocks.Add(paraDummy1);

            Paragraph para = new Paragraph();
            para.TextAlignment = TextAlignment.Center;
            para.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 2;
            para.FontWeight = FontWeights.Bold;
            string gamesHeader = Properties.Resources.Games;
            para.Inlines.Add(new Run(gamesHeader));
            doc.Blocks.Add(para);

            Paragraph paraDummy2 = new Paragraph();
            paraDummy2.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 4;
            doc.Blocks.Add(paraDummy2);

            return doc;
        }

        /// <summary>
        /// Creates a flow document with the Games header.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static FlowDocument PrintExercisesHeader()
        {
            FlowDocument doc = new FlowDocument();

            // add a dummy paragraph a a spacer before the further content
            Paragraph paraDummy1 = new Paragraph();
            paraDummy1.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 4;
            doc.Blocks.Add(paraDummy1);

            Paragraph para = new Paragraph();
            para.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 2;
            para.FontWeight = FontWeights.Bold;
            string exercisesHeader = Properties.Resources.Exercises;
            para.Inlines.Add(new Run(exercisesHeader));
            doc.Blocks.Add(para);

            return doc;
        }

        /// <summary>
        /// Prints the intro.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static FlowDocument PrintIntro(Chapter chapter)
        {
            RichTextBox rtb = new RichTextBox();
            IntroView introView = new IntroView(new FlowDocument(), chapter, rtb);

            // the user may have placed an empty line at the start.
            // we don't want it in print so remove it
            Block first = rtb.Document.Blocks.FirstBlock;
            if (first is Paragraph para)
            {
                if (!RichTextBoxUtilities.HasNonEmptyInline(para))
                {
                    rtb.Document.Blocks.Remove(first);
                }
            }

            Paragraph paraBeginCols2 = new Paragraph();
            Run runBeginCols2 = new Run(BeginTwoColumns());
            paraBeginCols2.Inlines.Add(runBeginCols2);

            Paragraph paraEndCols2 = new Paragraph();
            Run runEndCols2 = new Run(EndTwoColumns());
            paraEndCols2.Inlines.Add(runEndCols2);

            rtb.Document.Blocks.InsertBefore(rtb.Document.Blocks.FirstBlock, paraBeginCols2);
            rtb.Document.Blocks.Add(paraEndCols2);

            return rtb.Document;
        }

        /// <summary>
        /// Prints the intro.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static FlowDocument PrintStudy(Chapter chapter)
        {
            RichTextBox rtbStudy = new RichTextBox();
            StudyTreeView studyView = new StudyTreeView(rtbStudy, GameData.ContentType.STUDY_TREE, 0);
            studyView.BuildFlowDocumentForVariationTree(chapter.StudyTree.Tree);

            Paragraph paraIndex = null;
            foreach (Block block in rtbStudy.Document.Blocks)
            {
                if (block is Paragraph para)
                {
                    if (para.Name == RichTextBoxUtilities.StudyIndexParagraphName)
                    {
                        paraIndex = para;
                        break;
                    }
                }
            }

            if (paraIndex == null)
            {
                paraIndex = rtbStudy.Document.Blocks.FirstBlock as Paragraph;
            }

            if (paraIndex != null)
            {
                Paragraph paraBeginCols2 = new Paragraph();
                Run runBeginCols2 = new Run(BeginTwoColumns());
                paraBeginCols2.Inlines.Add(runBeginCols2);

                Paragraph paraEndCols2 = new Paragraph();
                Run runEndCols2 = new Run(EndTwoColumns());
                paraEndCols2.Inlines.Add(runEndCols2);

                rtbStudy.Document.Blocks.InsertAfter(paraIndex, paraBeginCols2);
                rtbStudy.Document.Blocks.Add(paraEndCols2);
            }

            return rtbStudy.Document;
        }

        /// <summary>
        /// Print a single Game
        /// </summary>
        /// <param name="game"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static FlowDocument PrintGame(Article game, int index, ref List<RtfDiagram> diagrams)
        {
            _currentGameNumber++;
            int gameNo = _continuousArticleNumbering ? _currentGameNumber : index + 1;

            FlowDocument doc = new FlowDocument();

            Paragraph paraDummy1 = new Paragraph();
            paraDummy1.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 2;

            Paragraph para = new Paragraph();
            para.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
            para.FontWeight = FontWeights.Bold;
            string gamesHeader = Properties.Resources.Game + " " + gameNo.ToString();
            para.TextAlignment = TextAlignment.Center;
            para.TextDecorations = TextDecorations.Underline;

            para.Inlines.Add(new Run(gamesHeader));

            VariationTreeView gameView = new VariationTreeView(doc, GameData.ContentType.MODEL_GAME, gameNo);
            gameView.BuildFlowDocumentForVariationTree(game.Tree);

            AddColumnFormatPlaceholdersAfterHeader(doc);
            doc.Blocks.InsertBefore(doc.Blocks.FirstBlock, paraDummy1);
            doc.Blocks.InsertAfter(paraDummy1, para);

            return doc;
        }


        /// <summary>
        /// Print a single Exercise
        /// </summary>
        /// <param name="game"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static FlowDocument PrintExercise(FlowDocument printDoc, Article exercise, int index, ref List<RtfDiagram> diagrams)
        {
            _currentExerciseNumber++;
            int exerciseNo = _continuousArticleNumbering ? _currentExerciseNumber : index + 1;

            FlowDocument doc = new FlowDocument();

            Paragraph paraDummy1 = new Paragraph();
            paraDummy1.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 2;

            Paragraph para = new Paragraph();
            para.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
            para.FontWeight = FontWeights.Bold;
            para.TextAlignment = TextAlignment.Center;
            para.TextDecorations = TextDecorations.Underline;
            string exerciseHeader = Properties.Resources.Exercise + " " + exerciseNo.ToString();
            para.Inlines.Add(new Run(exerciseHeader));

            VariationTreeView exerciseView = new ExerciseTreeView(doc, GameData.ContentType.EXERCISE, exerciseNo);
            exerciseView.BuildFlowDocumentForVariationTree(exercise.Tree);

            AddColumnFormatPlaceholdersAfterHeader(doc);
            doc.Blocks.InsertBefore(doc.Blocks.FirstBlock, paraDummy1);
            doc.Blocks.InsertAfter(paraDummy1, para);

            return doc;
        }

        /// <summary>
        /// Inserts formatting command placeholders in the Study document 
        /// after the index paragraph.
        /// </summary>
        /// <param name="doc"></param>
        private static void AddColumnFormatPlaceholdersAfterIndex(FlowDocument doc)
        {
            Paragraph paraIndex = null;
            foreach (Block block in doc.Blocks)
            {
                if (block is Paragraph para)
                {
                    if (para.Name == RichTextBoxUtilities.StudyIndexParagraphName)
                    {
                        paraIndex = para;
                        break;
                    }
                }
            }

            if (paraIndex != null)
            {
                Paragraph paraBeginCols2 = new Paragraph();
                Run runBeginCols2 = new Run(BeginTwoColumns());
                paraBeginCols2.Inlines.Add(runBeginCols2);

                Paragraph paraEndCols2 = new Paragraph();
                Run runEndCols2 = new Run(EndTwoColumns());
                paraEndCols2.Inlines.Add(runEndCols2);

                if (paraIndex == null)
                {
                    // if there was no index, insert the command before the FirstBlock
                    doc.Blocks.InsertBefore(doc.Blocks.FirstBlock, paraBeginCols2);
                }
                else
                {
                    doc.Blocks.InsertAfter(paraIndex, paraBeginCols2);
                }
                doc.Blocks.Add(paraEndCols2);
            }
        }

        /// <summary>
        /// Puts 2-column format placeholders after the Game or Exercise header.
        /// </summary>
        /// <param name="doc"></param>
        private static void AddColumnFormatPlaceholdersAfterHeader(FlowDocument doc)
        {
            Paragraph pageHeader = null;

            foreach (Block block in doc.Blocks)
            {
                if (block is Paragraph p)
                {
                    if (p.Name == RichTextBoxUtilities.HeaderParagraphName)
                    {
                        pageHeader = p;
                        break;
                    }
                }
            }

            if (pageHeader != null)
            {
                Paragraph paraBeginCols2 = new Paragraph();
                Run runBeginCols2 = new Run(BeginTwoColumns());
                paraBeginCols2.Inlines.Add(runBeginCols2);

                doc.Blocks.InsertAfter(pageHeader, paraBeginCols2);

                Paragraph paraEndCols2 = new Paragraph();
                Run runEndCols2 = new Run(EndTwoColumns());
                paraEndCols2.Inlines.Add(runEndCols2);

                doc.Blocks.Add(paraEndCols2);
            }
        }

        /// <summary>
        /// Inserts a Page Break placeholder paragraph
        /// at the start of the document.
        /// </summary>
        /// <param name="doc"></param>
        private static void AddPageBreakPlaceholder(FlowDocument doc)
        {
            Paragraph paraPageBreak = new Paragraph();
            paraPageBreak.FontSize = 1;
            Run runPageBreak = new Run(PageBreak());
            paraPageBreak.Inlines.Add(runPageBreak);

            doc.Blocks.InsertBefore(doc.Blocks.FirstBlock, paraPageBreak);
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
        /// Replace 2-column formatting placeholders with
        /// the RTF formatting commands.
        /// </summary>
        /// <param name="rtfContent"></param>
        /// <returns></returns>
        private static string InsertColumnFormatting(string rtfContent)
        {
            string[] lines = rtfContent.Split('\n');

            StringBuilder sb = new StringBuilder();
            foreach (string line in lines)
            {
                if (line.IndexOf(BeginTwoColumns()) >= 0)
                {
                    sb.Append(@"\sect\sectd\pard\sbknone\linex0\cols2" + '\r');
                }
                else if (line.IndexOf(EndTwoColumns()) >= 0)
                {
                    sb.Append(@"\sect\sectd\par\sbknone\linex0" + '\r');
                }
                else if (line.IndexOf(PageBreak()) >= 0)
                {
                    string upd = line.Replace(PageBreak(), @" {\page} ");
                    sb.Append(upd);
                }
                else
                {
                    sb.AppendLine(line);
                }
            }
            return sb.ToString();
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
            target.TextAlignment = source.TextAlignment;
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

        /// <summary>
        /// Placeholder for the start of 2 column layout
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private static string BeginTwoColumns()
        {
            return "<BEGIN 2 COLUMNS>";
        }

        /// <summary>
        /// Placeholder for the end of 2 column layout
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private static string EndTwoColumns()
        {
            return "<END 2 COLUMNS>";
        }

        /// <summary>
        /// Placeholder for page break
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private static string PageBreak()
        {
            return "<PAGE BREAK>";
        }

        /// <summary>
        /// Resets the article counters.
        /// </summary>
        private static void ResetCounters()
        {
            _currentGameNumber = 0;
            _currentExerciseNumber = 0;
        }
    }

}

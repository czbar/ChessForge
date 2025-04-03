using ChessPosition;
using GameTree;
using Microsoft.Win32;
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
        // TODO: this field is meant to be Configuration items.
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
        private static int _diagramId = 0;

        // whether to insert FENs under diagrams
        private static bool _insertFens = false;
        
        /// <summary>
        /// Exports the scoped articles into an RTF file.
        /// </summary>
        /// <param name="chapter"></param>
        public static bool WriteRtf(string fileName)
        {
            bool result = true;
            _insertFens = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.FEN_UNDER_DIAGRAMS);

            ResetCounters();
            bool saveUseFixedFont = Configuration.UseFixedFont;
            // we only use Fixed Font size when "printing".  Set Configuration.UseFixedFont to true temporarily
            Configuration.UseFixedFont = true;

            try
            {
                // dummy write to trigger an error on access, before we take time to process.
                File.WriteAllText(fileName, "");

                PrintScope scope = ConfigurationRtfExport.GetScope();

                _printScope = scope;
                _chapterToPrint = AppState.ActiveChapter;
                _articleToPrint = AppState.Workbook.ActiveArticle;


                FlowDocument printDoc = new FlowDocument();
                List<RtfDiagram> diagrams = new List<RtfDiagram>();

                if (scope == PrintScope.ARTICLE)
                {
                    PrintActiveViewToFlowDoc(printDoc, _chapterToPrint, ref diagrams);
                }
                else
                {
                    PrintWorkbookOrChapterScopeToFlowDoc(printDoc, scope, ref diagrams);
                }

                WriteOutFile(fileName, printDoc, ref diagrams);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                result = false;
            }
            finally
            {
                Configuration.UseFixedFont = saveUseFixedFont;
            }

            return result;
        }

        /// <summary>
        /// Calls function to finalize the RTF text and writes out the content.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="printDoc"></param>
        /// <param name="diagrams"></param>
        private static void WriteOutFile(string fileName, FlowDocument printDoc, ref List<RtfDiagram> diagrams)
        {
            TextRange textRange = new TextRange(printDoc.ContentStart, printDoc.ContentEnd);
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

        /// <summary>
        /// Asks the user for the RTF file to export to.
        /// If the current Workbook has a WorkbookFilePath (i.e. it is not viewed from an online library),
        /// suggest the workbook path with the extension replaced.
        /// Otherwise, suggest a name based om the workbooks name.
        /// </summary>
        public static string SelectTargetRtfFile()
        {
            string rtfExt = ".rtf";
            string rtfFileName;

            if (string.IsNullOrEmpty(AppState.WorkbookFilePath))
            {
                rtfFileName = TextUtils.RemoveInvalidCharsFromFileName(WorkbookManager.SessionWorkbook.Title) + rtfExt;
            }
            else
            {
                rtfFileName = FileUtils.ReplacePathExtension(AppState.WorkbookFilePath, rtfExt);
            }

            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = Properties.Resources.RtfFiles + " (*.rtf)|*.rtf"
            };

            saveDlg.FileName = Path.GetFileName(rtfFileName);
            saveDlg.Title = Properties.Resources.ExportRtf;

            saveDlg.OverwritePrompt = true;
            if (saveDlg.ShowDialog() == true)
            {
                rtfFileName = saveDlg.FileName;
            }
            else
            {
                rtfFileName = "";
            }

            return rtfFileName;
        }


        //********************************************************************
        //
        //   PRINTING ARTICLES/ITEMS TO THE PASSED FLOW DOCUMENT
        //
        //********************************************************************

        /// <summary>
        /// Prints either the active chapter or the entire workbook to the passed FlowDocument.
        /// </summary>
        /// <param name="printDoc"></param>
        /// <param name="scope"></param>
        /// <param name="diagrams"></param>
        private static void PrintWorkbookOrChapterScopeToFlowDoc(FlowDocument printDoc, PrintScope scope, ref List<RtfDiagram> diagrams)
        {
            bool isFirstPrintPage = true;
            if (scope == PrintScope.WORKBOOK)
            {
                FlowDocument workbookFP = PrintWorkbookFrontPage();
                CreateDocumentForPrint(printDoc, workbookFP, null, ref diagrams);
                isFirstPrintPage = false;

                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_CONTENTS))
                {
                    FlowDocument docContents = PrintWorkbookContents(AppState.Workbook);
                    AddPageBreakPlaceholder(docContents);
                    CreateDocumentForPrint(printDoc, docContents, null, ref diagrams);
                }
            }

            PrintChaptersToFlowDoc(printDoc, scope, ref diagrams, isFirstPrintPage);

            if (scope == PrintScope.WORKBOOK)
            {
                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_GAME_INDEX))
                {
                    FlowDocument docGameIndex = PrintGameOrExerciseIndex(AppState.Workbook, true);
                    if (docGameIndex != null)
                    {
                        AddPageBreakPlaceholder(docGameIndex);
                        CreateDocumentForPrint(printDoc, docGameIndex, null, ref diagrams);
                    }
                }

                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_EXERCISE_INDEX))
                {
                    FlowDocument docExerciseIndex = PrintGameOrExerciseIndex(AppState.Workbook, false);
                    if (docExerciseIndex != null)
                    {
                        AddPageBreakPlaceholder(docExerciseIndex);
                        CreateDocumentForPrint(printDoc, docExerciseIndex, null, ref diagrams);
                    }
                }
            }
        }

        /// <summary>
        /// Process the content of the active chapter or all chapters 
        /// and places it in the printDoc.
        /// </summary>
        /// <param name="printDoc"></param>
        /// <param name="scope"></param>
        /// <param name="diagrams"></param>
        /// <param name="isFirstPrintPage"></param>
        private static void PrintChaptersToFlowDoc(FlowDocument printDoc, PrintScope scope, ref List<RtfDiagram> diagrams, bool isFirstPrintPage)
        {
            foreach (Chapter chapter in AppState.Workbook.Chapters)
            {
                try
                {
                    // if scope is CHAPTER, only act when we encounter the active chapter
                    if (_printScope == PrintScope.WORKBOOK || _printScope == PrintScope.CHAPTER && _chapterToPrint == chapter)
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

                        bool introPrinted = false;
                        if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_INTRO))
                        {
                            if (!chapter.IsIntroEmpty())
                            {
                                PrintIntroToFlowDoc(printDoc, scope, chapter, ref diagrams);
                                introPrinted = true;
                            }
                        }

                        if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_STUDY))
                        {
                            if (!chapter.IsStudyEmpty())
                            {
                                PrintStudyToFlowDoc(printDoc, scope, chapter, introPrinted, ref diagrams);
                            }
                        }

                        if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_GAMES))
                        {
                            if (chapter.ModelGames.Count > 0)
                            {
                                FlowDocument docGamesHeader = PrintGamesHeader();
                                AddPageBreakPlaceholder(docGamesHeader);
                                CreateDocumentForPrint(printDoc, docGamesHeader, null, ref diagrams);

                                // if "two-column format" is set, place the start command before the first game
                                // and the end command after the last one
                                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_GAMES))
                                {
                                    printDoc.Blocks.Add(CreateTwoColumnBeginPara());
                                }

                                for (int i = 0; i < chapter.ModelGames.Count; i++)
                                {
                                    PrintGameToFlowDoc(printDoc, chapter, i, ref diagrams);
                                }

                                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_GAMES))
                                {
                                    printDoc.Blocks.Add(CreateTwoColumnEndPara());
                                }
                            }
                        }

                        if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_EXERCISES))
                        {
                            if (chapter.Exercises.Count > 0)
                            {
                                FlowDocument docExercisesHeader = PrintExercisesHeader();
                                AddPageBreakPlaceholder(docExercisesHeader);
                                CreateDocumentForPrint(printDoc, docExercisesHeader, null, ref diagrams);

                                // if "two-column format" is set, place the start command before the first exercise
                                // and the end command after the last one
                                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_EXERCISES))
                                {
                                    printDoc.Blocks.Add(CreateTwoColumnBeginPara());
                                }

                                for (int i = 0; i < chapter.Exercises.Count; i++)
                                {
                                    PrintExerciseToFlowDoc(printDoc, chapter, i, ref diagrams);
                                }

                                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_EXERCISES))
                                {
                                    printDoc.Blocks.Add(CreateTwoColumnEndPara());
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Message("PrintChaptersToFlowDoc()" + "Chapter " + chapter.Index, ex);
                }
            }
        }

        /// <summary>
        /// Requests processing of the current view.
        /// </summary>
        /// <param name="printDoc"></param>
        /// <param name="chapter"></param>
        /// <param name="diagrams"></param>
        private static void PrintActiveViewToFlowDoc(FlowDocument printDoc, Chapter chapter, ref List<RtfDiagram> diagrams)
        {
            if (AppState.ActiveTab == TabViewType.CHAPTERS)
            {
                PrintChaptersViewToFlowDoc(printDoc, ref diagrams);
            }
            else
            {
                switch (AppState.Workbook.ActiveArticle.Tree.ContentType)
                {
                    case GameData.ContentType.INTRO:
                        PrintIntroToFlowDoc(printDoc, PrintScope.ARTICLE, chapter, ref diagrams);
                        break;
                    case GameData.ContentType.STUDY_TREE:
                        PrintStudyToFlowDoc(printDoc, PrintScope.ARTICLE, chapter, true, ref diagrams);
                        break;
                    case GameData.ContentType.MODEL_GAME:
                        PrintGameToFlowDoc(printDoc, chapter, -1, ref diagrams);
                        break;
                    case GameData.ContentType.EXERCISE:
                        PrintExerciseToFlowDoc(printDoc, chapter, -1, ref diagrams);
                        break;
                }
            }
        }

        /// <summary>
        /// Process the contents list place it in the printDoc.
        /// </summary>
        /// <param name="printDoc"></param>
        /// <param name="diagrams"></param>
        private static void PrintChaptersViewToFlowDoc(FlowDocument printDoc, ref List<RtfDiagram> diagrams)
        {
            FlowDocument workbookFP = PrintWorkbookFrontPage();
            CreateDocumentForPrint(printDoc, workbookFP, null, ref diagrams);

            FlowDocument docContents = PrintWorkbookContents(AppState.Workbook);
            AddPageBreakPlaceholder(docContents);
            CreateDocumentForPrint(printDoc, docContents, null, ref diagrams);

            FlowDocument docGameIndex = PrintGameOrExerciseIndex(AppState.Workbook, true);
            if (docGameIndex != null)
            {
                AddPageBreakPlaceholder(docGameIndex);
                CreateDocumentForPrint(printDoc, docGameIndex, null, ref diagrams);
            }
        }

        /// <summary>
        /// Process the content of the Intro and places it in the printDoc.
        /// </summary>
        /// <param name="printDoc"></param>
        /// <param name="chapter"></param>
        /// <param name="diagrams"></param>
        private static void PrintIntroToFlowDoc(FlowDocument printDoc, PrintScope scope, Chapter chapter, ref List<RtfDiagram> diagrams)
        {
            if (scope == PrintScope.ARTICLE)
            {
                FlowDocument chapterTitleDoc = PrintChapterTitle(chapter);
                CreateDocumentForPrint(printDoc, chapterTitleDoc, null, ref diagrams);
            }

            FlowDocument doc = PrintIntro(chapter);
            CreateDocumentForPrint(printDoc, doc, chapter.Intro.Tree, ref diagrams);
        }

        /// <summary>
        /// Process the content of the Study and places it in the printDoc.
        /// </summary>
        /// <param name="printDoc"></param>
        /// <param name="scope"></param>
        /// <param name="chapter"></param>
        /// <param name="introPrinted"></param>
        /// <param name="diagrams"></param>
        private static void PrintStudyToFlowDoc(FlowDocument printDoc, PrintScope scope, Chapter chapter, bool introPrinted, ref List<RtfDiagram> diagrams)
        {
            if (scope == PrintScope.ARTICLE)
            {
                FlowDocument chapterTitleDoc = PrintChapterTitle(chapter);
                CreateDocumentForPrint(printDoc, chapterTitleDoc, null, ref diagrams);
            }

            // if we are printing just this study or there was no Intro, print without the header and page break.
            if (scope != PrintScope.ARTICLE && introPrinted)
            {
                FlowDocument docStudyHeader = PrintStudyHeader();
                AddPageBreakPlaceholder(docStudyHeader);
                CreateDocumentForPrint(printDoc, docStudyHeader, null, ref diagrams);
            }

            FlowDocument doc = PrintStudy(chapter);
            CreateDocumentForPrint(printDoc, doc, chapter.StudyTree.Tree, ref diagrams);
        }

        /// <summary>
        /// Process a single Game and places it in the printDoc.
        /// </summary>
        /// <param name="printDoc"></param>
        /// <param name="chapter"></param>
        /// <param name="gameIndex"></param>
        /// <param name="diagrams"></param>
        private static void PrintGameToFlowDoc(FlowDocument printDoc, Chapter chapter, int gameIndex, ref List<RtfDiagram> diagrams)
        {
            Article game;

            if (gameIndex >= 0)
            {
                game = chapter.ModelGames[gameIndex];
            }
            else
            {
                game = chapter.ModelGames[chapter.ActiveModelGameIndex];
            }

            FlowDocument guiDoc = PrintGame(game, gameIndex, ref diagrams);
            CreateDocumentForPrint(printDoc, guiDoc, game.Tree, ref diagrams);
        }

        /// <summary>
        /// Process a single Exercise and places it in the printDoc.
        /// </summary>
        /// <param name="printDoc"></param>
        /// <param name="chapter"></param>
        /// <param name="exerciseIndex"></param>
        /// <param name="diagrams"></param>
        private static void PrintExerciseToFlowDoc(FlowDocument printDoc, Chapter chapter, int exerciseIndex, ref List<RtfDiagram> diagrams)
        {
            Article exercise;

            if (exerciseIndex >= 0)
            {
                exercise = chapter.Exercises[exerciseIndex];
            }
            else
            {
                exercise = chapter.Exercises[chapter.ActiveExerciseIndex];
            }

            FlowDocument guiDoc = PrintExercise(printDoc, exercise, exerciseIndex, ref diagrams);
            CreateDocumentForPrint(printDoc, guiDoc, exercise.Tree, ref diagrams);
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
        /// Print the Contents list for a Workbook
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static FlowDocument PrintWorkbookContents(Workbook workbook)
        {
            FlowDocument doc = new FlowDocument();

            Paragraph paraHeader = new Paragraph();
            paraHeader.Margin = new Thickness(0, 0, 0, 30);
            paraHeader.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 4;
            paraHeader.FontWeight = FontWeights.Bold;
            Run runHeader = new Run(Properties.Resources.Contents);
            paraHeader.Inlines.Add(runHeader);
            doc.Blocks.Add(paraHeader);

            foreach (Chapter chapter in workbook.Chapters)
            {
                Paragraph paraChapter = new Paragraph();
                paraChapter.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
                paraChapter.FontWeight = FontWeights.Normal;

                Run title = new Run((chapter.Index + 1).ToString() + ".\t " + chapter.Title);
                paraChapter.Inlines.Add(title);

                doc.Blocks.Add(paraChapter);
            }

            return doc;
        }

        /// <summary>
        /// Prints the index of all games in the Workbook.
        /// </summary>
        /// <param name="workbook"></param>
        /// <returns></returns>
        private static FlowDocument PrintGameOrExerciseIndex(Workbook workbook, bool gameOrExerc)
        {
            FlowDocument doc = new FlowDocument();

            Paragraph paraHeader = new Paragraph();
            paraHeader.Margin = new Thickness(0, 0, 0, 30);
            paraHeader.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 4;
            paraHeader.FontWeight = FontWeights.Bold;
            Run runHeader = new Run(gameOrExerc ? Properties.Resources.GameIndex : Properties.Resources.ExerciseIndex);
            paraHeader.Inlines.Add(runHeader);
            doc.Blocks.Add(paraHeader);

            int itemCounter = 0;
            foreach (Chapter chapter in workbook.Chapters)
            {
                int itemCount = gameOrExerc ? chapter.GetModelGameCount() : chapter.GetExerciseCount();
                if (itemCount > 0)
                {
                    Paragraph paraChapter = new Paragraph();
                    paraChapter.Margin = new Thickness(0, 20, 0, 10);
                    paraChapter.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
                    paraChapter.FontWeight = FontWeights.Bold;

                    Run chapterTitle = new Run(Properties.Resources.Chapter + " " + (chapter.Index + 1).ToString());
                    paraChapter.Inlines.Add(chapterTitle);

                    doc.Blocks.Add(paraChapter);

                    List<Article> articles = gameOrExerc ? chapter.ModelGames : chapter.Exercises;
                    foreach (Article article in articles)
                    {
                        itemCounter++;

                        Paragraph paraArticle = new Paragraph();
                        paraArticle.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
                        paraArticle.FontWeight = FontWeights.Normal;

                        string articleTitle = gameOrExerc ?
                              article.Tree.Header.BuildGameHeaderLine(false, false, true, true, false)
                            : article.Tree.Header.BuildGameHeaderLine(true, false, true, true, false);

                        Run title = new Run(itemCounter.ToString() + ".\t " + articleTitle);
                        paraArticle.Inlines.Add(title);

                        doc.Blocks.Add(paraArticle);
                    }
                }
            }

            return itemCounter == 0 ? null : doc;
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
                //Paragraph paraDummy1 = new Paragraph();
                //paraDummy1.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
                //doc.Blocks.Add(paraDummy1);

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
            //Paragraph paraDummy1 = new Paragraph();
            //paraDummy1.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 4;
            //doc.Blocks.Add(paraDummy1);

            Paragraph para = new Paragraph();
            para.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 2;
            para.FontWeight = FontWeights.Bold;
            para.TextAlignment = TextAlignment.Center;

            string studyTitle = Properties.Resources.Study;
            if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_STUDY))
            {
                studyTitle = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CUSTOM_TERM_STUDY);
            }

            if (!string.IsNullOrWhiteSpace(studyTitle))
            {
                para.Inlines.Add(new Run(studyTitle));
                doc.Blocks.Add(para);
            }

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
            //Paragraph paraDummy1 = new Paragraph();
            //paraDummy1.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 8;
            //doc.Blocks.Add(paraDummy1);

            Paragraph para = new Paragraph();
            para.TextAlignment = TextAlignment.Center;
            para.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 2;
            para.FontWeight = FontWeights.Bold;

            string gamesHeader = Properties.Resources.Games;
            if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_GAMES))
            {
                gamesHeader = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CUSTOM_TERM_GAMES);
            }

            if (!string.IsNullOrWhiteSpace(gamesHeader))
            {
                para.Inlines.Add(new Run(gamesHeader));
                doc.Blocks.Add(para);
            }

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
            //Paragraph paraDummy1 = new Paragraph();
            //paraDummy1.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 4;
            //doc.Blocks.Add(paraDummy1);

            Paragraph para = new Paragraph();
            para.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 2;
            para.TextAlignment = TextAlignment.Center;
            para.FontWeight = FontWeights.Bold;

            string exercisesHeader = Properties.Resources.Exercises;
            if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_EXERCISES))
            {
                exercisesHeader = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.USE_CUSTOM_EXERCISES);
            }

            if (!string.IsNullOrWhiteSpace(exercisesHeader))
            {
                para.Inlines.Add(new Run(exercisesHeader));
                doc.Blocks.Add(para);
            }

            Paragraph paraDummy2 = new Paragraph();
            paraDummy2.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 4;
            doc.Blocks.Add(paraDummy2);

            return doc;
        }

        /// <summary>
        /// Prints the intro.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static FlowDocument PrintIntro(Chapter chapter)
        {
            RichTextBox rtbIntro = new RichTextBox();
            IntroView introView = new IntroView(rtbIntro, chapter, true);

            RichTextBox rtb = introView.HostRtb;
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

            if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_INTRO))
            {
                Paragraph paraBeginCols2 = new Paragraph();
                Run runBeginCols2 = new Run(BeginTwoColumns());
                paraBeginCols2.Inlines.Add(runBeginCols2);

                Paragraph paraEndCols2 = new Paragraph();
                Run runEndCols2 = new Run(EndTwoColumns());
                paraEndCols2.Inlines.Add(runEndCols2);

                if (rtb.Document.Blocks.FirstBlock == null)
                {
                    rtb.Document.Blocks.Add(paraBeginCols2);
                }
                else
                {
                    rtb.Document.Blocks.InsertBefore(rtb.Document.Blocks.FirstBlock, paraBeginCols2);
                }
                rtb.Document.Blocks.Add(paraEndCols2);
            }

            return rtb.Document;
        }

        /// <summary>
        /// Prints the Study.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static FlowDocument PrintStudy(Chapter chapter)
        {
            RichTextBox rtbStudy = new RichTextBox();
            StudyTreeView studyView = new StudyTreeView(rtbStudy, GameData.ContentType.STUDY_TREE, true);
            studyView.BuildFlowDocumentForVariationTree(false, chapter.StudyTree.Tree);

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
                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_STUDY))
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
            }

            return rtbStudy.Document;
        }

        /// <summary>
        /// Print a single Game
        /// </summary>
        /// <param name="game"></param>
        /// <param name="index">If index==-1 this is printed as part of "current view"</param>
        /// <returns></returns>
        private static FlowDocument PrintGame(Article game, int index, ref List<RtfDiagram> diagrams)
        {
            _currentGameNumber++;
            int gameNo = _continuousArticleNumbering ? _currentGameNumber : index + 1;

            Paragraph paraDummy1 = new Paragraph();
            paraDummy1.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 2;

            Paragraph para = new Paragraph();

            // if index < 0 then we are printing for the CurrentView scope hence no need for a header
            if (index >= 0)
            {
                para.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
                para.FontWeight = FontWeights.Bold;

                string gameHeader = Properties.Resources.Game + " " + gameNo.ToString();
                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_GAME))
                {
                    gameHeader = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CUSTOM_TERM_GAME);
                }

                para.TextAlignment = TextAlignment.Center;
                para.TextDecorations = TextDecorations.Underline;

                if (!string.IsNullOrWhiteSpace(gameHeader))
                {
                    para.Inlines.Add(new Run(gameHeader));
                }
            }

            RichTextBox rtbGame = new RichTextBox();

            VariationTreeView gameView = new VariationTreeView(rtbGame, GameData.ContentType.MODEL_GAME, true);
            gameView.BuildFlowDocumentForVariationTree(false, game.Tree);

            FlowDocument doc = rtbGame.Document;

            // if part of "current view" print set the "two-column format, if so configured.
            // if chapter/workbook view then this is set up before/after first/last game.
            if (index < 0 && ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_GAMES))
            {
                AddColumnFormatPlaceholdersAfterHeader(doc);
            }

            if (index != 0)
            {
                if (doc.Blocks.FirstBlock == null)
                {
                    doc.Blocks.Add(paraDummy1);
                }
                else
                {
                    doc.Blocks.InsertBefore(doc.Blocks.FirstBlock, paraDummy1);
                }
                doc.Blocks.InsertAfter(paraDummy1, para);
            }
            else
            {
                if (doc.Blocks.FirstBlock == null)
                {
                    doc.Blocks.Add(para);
                }
                else
                {
                    doc.Blocks.InsertBefore(doc.Blocks.FirstBlock, para);
                }
            }

            return doc;
        }

        /// <summary>
        /// Print a single Exercise
        /// </summary>
        /// <param name="game"></param>
        /// <param name="index">If index==-1 this is printed as part of "current view"</param>
        /// <returns></returns>
        private static FlowDocument PrintExercise(FlowDocument printDoc, Article exercise, int index, ref List<RtfDiagram> diagrams)
        {
            _currentExerciseNumber++;
            int exerciseNo = _continuousArticleNumbering ? _currentExerciseNumber : index + 1;

            Paragraph paraDummy1 = new Paragraph();
            paraDummy1.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff + 2;

            Paragraph para = new Paragraph();

            // if index < 0 then we are printing for the CurrentView scope hence no need for a header
            if (index >= 0)
            {
                para.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff;
                para.FontWeight = FontWeights.Bold;
                para.TextAlignment = TextAlignment.Center;
                para.TextDecorations = TextDecorations.Underline;

                string exerciseHeader = Properties.Resources.Exercise + " " + exerciseNo.ToString();
                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_EXERCISE))
                {
                    exerciseHeader = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CUSTOM_TERM_EXERCISE);
                }

                if (!string.IsNullOrWhiteSpace(exerciseHeader))
                {
                    para.Inlines.Add(new Run(exerciseHeader));
                }
            }

            RichTextBox rtbExercise = new RichTextBox();
            VariationTreeView exerciseView = new ExerciseTreeView(rtbExercise, GameData.ContentType.EXERCISE, true);
            exerciseView.BuildFlowDocumentForVariationTree(false, exercise.Tree);

            FlowDocument doc = rtbExercise.Document;

            // if part of "current view" print set the "two-column format, if so configured.
            // if chapter/workbook view then this is set up before/after first/last exercise.
            if (index < 0 && ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.TWO_COLUMN_EXERCISES))
            {
                AddColumnFormatPlaceholdersAfterHeader(doc);
            }

            if (index != 0)
            {
                if (doc.Blocks.FirstBlock == null)
                {
                    doc.Blocks.Add(paraDummy1);
                }
                else
                {
                    doc.Blocks.InsertBefore(doc.Blocks.FirstBlock, paraDummy1);
                }

                doc.Blocks.InsertAfter(paraDummy1, para);
            }
            else
            {
                if (doc.Blocks.FirstBlock == null)
                {
                    doc.Blocks.Add(para);
                }
                else
                {
                    doc.Blocks.InsertBefore(doc.Blocks.FirstBlock, para);
                }
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
        /// Creates a paragraph with "Begin Two-Column Section" command. 
        /// </summary>
        /// <returns></returns>
        private static Paragraph CreateTwoColumnBeginPara()
        {
            Paragraph paraBeginCols2 = new Paragraph();
            Run runBeginCols2 = new Run(BeginTwoColumns());
            paraBeginCols2.Inlines.Add(runBeginCols2);

            return paraBeginCols2;
        }

        /// <summary>
        /// Creates a paragraph with "End Two-Column Section" command. 
        /// </summary>
        /// <returns></returns>
        private static Paragraph CreateTwoColumnEndPara()
        {
            Paragraph paraEndCols2 = new Paragraph();
            Run runEndCols2 = new Run(EndTwoColumns());
            paraEndCols2.Inlines.Add(runEndCols2);

            return paraEndCols2;
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

            if (doc.Blocks.FirstBlock == null)
            {
                doc.Blocks.Add(paraPageBreak);
            }
            else
            {
                doc.Blocks.InsertBefore(doc.Blocks.FirstBlock, paraPageBreak);
            }
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
                byte[] diagram = PositionImageGenerator.GenerateImage(diag.Node, diag.IsFlipped);
                string rtfImage = GetImageRtf(diagram);
                //string rtfImageTag = $@"{{\pict\pngblip\picw{ChessBoards.ChessBoardGreySmall.PixelWidth}\pich{ChessBoards.ChessBoardGreySmall.PixelHeight}\picwgoal{ChessBoards.ChessBoardGreySmall.PixelWidth * 15}\pichgoal{ChessBoards.ChessBoardGreySmall.PixelHeight * 15} {rtfImage}}}";
                string rtfImageTag = @"{\pict\pngblip\picw" + "242" + @"\pich" + "242" +
                                            @"\picwgoal" + "2800" + @"\pichgoal" + "2800" +
                                            @" " + rtfImage + "}";

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

                    // set Left margin to 0 so that centering will work correctly
                    printPara.Margin = new Thickness(0, guiPara.Margin.Top, guiPara.Margin.Right, guiPara.Margin.Bottom);

                    printDoc.Blocks.Add(printPara);

                    if (guiPara.Name != null && guiPara.Name.StartsWith(RichTextBoxUtilities.DiagramParaPrefix))
                    {
                        TreeNode nd = CreateIntroDiagramForPrint(printPara, guiPara, tree, ref diagrams);
                        lastDiagramPara = printPara;

                        if (_insertFens)
                        {
                            string fenText = "FEN " + FenParser.GenerateFenFromPosition(nd.Position);
                            Paragraph fen = new Paragraph();
                            fen.Inlines.Add(new Run(fenText));
                            fen.TextAlignment = TextAlignment.Center;
                            fen.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff - 2;
                            fen.FontFamily = new FontFamily("Courier New");
                            printDoc.Blocks.Add(fen);
                        }
                    }
                    else if (guiPara.Name == RichTextBoxUtilities.ExerciseUnderBoardControls)
                    {
                        CreateExerciseUnderBoardControls(printDoc, printPara, lastDiagramPara, tree);
                    }
                    else
                    {
                        foreach (Inline inl in guiPara.Inlines)
                        {
                            if (inl is Run run && run.Text != null)
                            {
                                lastRunWasIntroMove = false;
                                printPara = CreateTextRuns(printDoc, printPara, run);
                            }
                            else if (inl is InlineUIContainer uic)
                            {
                                if (inl.Name != null && inl.Name.StartsWith(RichTextBoxUtilities.UicMovePrefix))
                                {
                                    lastRunWasIntroMove = ProcessIntroMove(printPara, uic, lastRunWasIntroMove);
                                }
                                else if (inl.Name.StartsWith(RichTextBoxUtilities.InlineDiagramIucPrefix))
                                {
                                    printDoc.Blocks.Add(printPara);
                                    CreateInlineDiagramForPrint(printDoc, printPara, inl.Name, tree, ref diagrams);
                                    lastRunWasIntroMove = false;
                                    break;
                                }
                                else
                                {
                                    lastRunWasIntroMove = false;
                                }
                            }
                        }
                    }

                    // the very last printPara may not have been added
                    if (!printDoc.Blocks.Contains(printPara))
                    {
                        printDoc.Blocks.Add(printPara);
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
        /// Inserts placeholder for an inline diagram.
        /// </summary>
        /// <param name="printDoc"></param>
        /// <param name="printPara"></param>
        /// <param name="inlName"></param>
        /// <param name="tree"></param>
        /// <param name="diagrams"></param>
        /// <returns></returns>
        private static Paragraph CreateInlineDiagramForPrint(FlowDocument printDoc, Paragraph printPara, string inlName, VariationTree tree, ref List<RtfDiagram> diagrams)
        {
            printDoc.Blocks.Add(printPara);

            int nodeId = TextUtils.GetIdFromPrefixedString(inlName);
            // this is an inline diagram so create a new paragraph and register the diagram
            printPara = new Paragraph();
            _diagramId++;
            ProcessDiagram(_diagramId, printPara, tree, nodeId, diagrams);
            printPara.TextAlignment = TextAlignment.Center;
            printDoc.Blocks.Add(printPara);

            if (_insertFens)
            {
                TreeNode nd = tree.GetNodeFromNodeId(nodeId);
                string fenText = "FEN " + FenParser.GenerateFenFromPosition(nd.Position);
                Paragraph fen = new Paragraph();
                fen.Inlines.Add(new Run(fenText));
                fen.TextAlignment = TextAlignment.Center;
                fen.FontSize = Constants.BASE_FIXED_FONT_SIZE + Configuration.FontSizeDiff - 2;
                fen.FontFamily = new FontFamily("Courier New");
                printDoc.Blocks.Add(fen);
            }

            return printPara;
        }

        /// <summary>
        /// Inserts a diagram placeholder into the Print Doc.
        /// </summary>
        /// <param name="printPara"></param>
        /// <param name="guiPara"></param>
        /// <param name="tree"></param>
        /// <param name="diagrams"></param>
        private static TreeNode CreateIntroDiagramForPrint(Paragraph printPara, Paragraph guiPara, VariationTree tree, ref List<RtfDiagram> diagrams)
        {
            _diagramId++;
            return ProcessDiagram(_diagramId, printPara, guiPara, tree, diagrams);
        }

        /// <summary>
        /// Builds Run for the white/black triangle under the exercise diagram.
        /// </summary>
        /// <param name="printDoc"></param>
        /// <param name="printPara"></param>
        /// <param name="lastDiagramPara"></param>
        /// <param name="tree"></param>
        private static void CreateExerciseUnderBoardControls(FlowDocument printDoc, Paragraph printPara, Paragraph lastDiagramPara, VariationTree tree)
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

        /// <summary>
        /// Creates print objects for text runs.
        /// </summary>
        /// <param name="printDoc"></param>
        /// <param name="printPara"></param>
        /// <param name="run"></param>
        /// <returns></returns>
        private static Paragraph CreateTextRuns(FlowDocument printDoc, Paragraph printPara, Run run)
        {
            if (run.Name != null && run.Name.StartsWith(RichTextBoxUtilities.PreInlineDiagramRunPrefix))
            {
                // do nothing since diagram that follows will create its Paragraph
            }
            else if (run.Name != null && run.Name.StartsWith(RichTextBoxUtilities.PostInlineDiagramRunPrefix))
            {
                //// this is post inline diagram so complete the current paragraph
                //// and add a dummy one to create space after the diagram
                //printDoc.Blocks.Add(printPara);

                //printPara = new Paragraph();
                //printDoc.Blocks.Add(printPara);
                //printPara = new Paragraph();
            }
            else
            {
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

            return printPara;
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
        /// Processes a paragraph representing an Intro-style diagram.
        /// </summary>
        /// <param name="printPara"></param>
        /// <param name="para"></param>
        /// <param name="tree"></param>
        /// <param name="diagrams"></param>
        private static TreeNode ProcessDiagram(int diagramId, Paragraph printPara, Paragraph para, VariationTree tree, List<RtfDiagram> diagrams)
        {
            TreeNode nd = null;

            int nodeId = TextUtils.GetIdFromPrefixedString(para.Name);
            if (tree != null)
            {
                nd = tree.GetNodeFromNodeId(nodeId);
                if (nd != null)
                {
                    bool isFlipped = RichTextBoxUtilities.GetDiagramFlipState(para);
                    printPara.Inlines.Add(CreateDiagramPlaceholderRun(diagramId));
                    printPara.TextAlignment = TextAlignment.Center;
                    diagrams.Add(new RtfDiagram(diagramId, nd, isFlipped));
                }
            }

            return nd;
        }

        /// <summary>
        /// Creates a paragraph representing an inline diagram.
        /// </summary>
        /// <param name="diagramId"></param>
        /// <param name="printPara"></param>
        /// <param name="tree"></param>
        /// <param name="nodeId"></param>
        /// <param name="diagrams"></param>
        private static void ProcessDiagram(int diagramId, Paragraph printPara, VariationTree tree, int nodeId, List<RtfDiagram> diagrams)
        {
            if (tree != null)
            {
                TreeNode nd = tree.GetNodeFromNodeId(nodeId);
                if (nd != null)
                {
                    printPara.Inlines.Add(CreateDiagramPlaceholderRun(diagramId));
                    diagrams.Add(new RtfDiagram(diagramId, nd, false));
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
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.IndexOf(BeginTwoColumns()) >= 0)
                {
                    sb.Append(@"\sect\sectd\pard\sbknone\linex0\cols2" + '\r');
                }
                else if (line.IndexOf(EndTwoColumns()) >= 0)
                {
                    sb.Append(@"\sect\sectd\sbknone\linex0" + '\r');
                }
                else if (line.IndexOf(PageBreak()) >= 0)
                {
                    sb.Append(@"\pard\plain \pagebb{\loch}" + '\r');
                    sb.Append(@"\par \pard\plain {\loch}" + '\r');
                }
                else
                {
                    sb.AppendLine(line);
                }
            }
            return sb.ToString();
        }



        //******************************************************
        //
        //   UTILITIES
        //
        //******************************************************

        /// <summary>
        /// Resets the article counters.
        /// </summary>
        private static void ResetCounters()
        {
            _currentGameNumber = 0;
            _currentExerciseNumber = 0;
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
    }

}

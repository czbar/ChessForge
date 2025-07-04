﻿using ChessPosition;
using GameTree;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ChessForge
{
    public class TextWriter
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

        /// <summary>
        /// Exports the scoped articles into a text file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool WriteText(string fileName)
        {
            bool result = true;
            ResetCounters();

            StringBuilder sb = new StringBuilder();
            try
            {
                // dummy write to trigger an error on access, before we take time to process.
                File.WriteAllText(fileName, "");

                PrintScope scope = ConfigurationRtfExport.GetScope();

                _printScope = scope;
                _chapterToPrint = AppState.ActiveChapter;

                if (scope == PrintScope.ARTICLE)
                {
                    sb.Append(PrintActiveView(_chapterToPrint));
                }
                else
                {
                    sb.Append(PrintWorkbookOrChapterScope(scope));
                }

                string outText = ReplaceSpecialChars(sb.ToString());
                WriteOutFile(fileName, outText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Shows the dialog for the user to selecy the target file.
        /// </summary>
        /// <returns></returns>
        public static string SelectTargetTextFile()
        {
            string textExt = ".txt";
            string textFileName = Configuration.LastTextExportFile;

            if (string.IsNullOrEmpty(textFileName))
            {
                if (string.IsNullOrEmpty(AppState.WorkbookFilePath))
                {
                    textFileName = TextUtils.RemoveInvalidCharsFromFileName(WorkbookManager.SessionWorkbook.Title) + textExt;
                }
                else
                {
                    textFileName = FileUtils.ReplacePathExtension(AppState.WorkbookFilePath, textExt);
                }
            }

            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = Properties.Resources.TextFiles + " (*.txt)|*.txt",
            };

            try
            {
                saveDlg.InitialDirectory = Path.GetDirectoryName(textFileName);
            }
            catch { }

            saveDlg.FileName = Path.GetFileName(textFileName);
            saveDlg.Title = Properties.Resources.ExportText;

            saveDlg.OverwritePrompt = true;
            if (saveDlg.ShowDialog() == true)
            {
                textFileName = saveDlg.FileName;
                Configuration.LastTextExportFile = textFileName;
            }
            else
            {
                textFileName = "";
            }

            return textFileName;
        }

        /// <summary>
        /// Writes the text to a file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="content"></param>
        private static void WriteOutFile(string fileName, string content)
        {
            File.WriteAllText(fileName, content);
        }


        //********************************************
        //
        // TOP LEVEL CALLS
        //
        //********************************************

        /// <summary>
        /// Prints the workbook or chapter scope to text.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        private static string PrintWorkbookOrChapterScope(PrintScope scope)
        {
            StringBuilder sb = new StringBuilder();

            bool isFirstPrintPage = true;
            if (scope == PrintScope.WORKBOOK)
            {
                sb.Append(PrintWorkbookFrontPage());
                isFirstPrintPage = false;

                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_CONTENTS))
                {
                    sb.AppendLine();
                    sb.AppendLine(PrintWorkbookContents(AppState.Workbook));
                }
            }

            sb.Append(PrintChapters(isFirstPrintPage));

            if ((scope == PrintScope.CHAPTER || scope == PrintScope.WORKBOOK)
                 && ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_BOOKMARKS))
            {
                sb.Append(PrintBookmarks(scope));
            }

            if (scope == PrintScope.WORKBOOK)
            {
                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_GAME_INDEX))
                {
                    sb.Append(AddPageBreakPlaceholder());
                    string gameIndexText = PrintGameOrExerciseIndex(AppState.Workbook, true);
                    if (!string.IsNullOrEmpty(gameIndexText))
                    {
                        sb.Append(gameIndexText);
                    }
                }

                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_EXERCISE_INDEX))
                {
                    string exerciseIndexText = PrintGameOrExerciseIndex(AppState.Workbook, false);
                    if (!string.IsNullOrEmpty(exerciseIndexText))
                    {
                        sb.Append(AddPageBreakPlaceholder());
                        sb.Append(exerciseIndexText);
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Prints the active view to text.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static string PrintActiveView(Chapter chapter)
        {
            string txt = "";
            if (AppState.ActiveTab == TabViewType.CHAPTERS)
            {
                txt = PrintChaptersViewToText();
            }
            else
            {
                switch (AppState.Workbook.ActiveArticle.Tree.ContentType)
                {
                    case GameData.ContentType.INTRO:
                        txt = PrintIntro(chapter);
                        break;
                    case GameData.ContentType.STUDY_TREE:
                    case GameData.ContentType.MODEL_GAME:
                    case GameData.ContentType.EXERCISE:
                        txt = PrintArticle(AppState.Workbook.ActiveArticle.Tree);
                        break;
                }
            }

            return txt;
        }

        /// <summary>
        /// Prints the chapter's content to text.
        /// </summary>
        /// <param name="isFirstPrintPage"></param>
        /// <returns></returns>
        private static string PrintChapters(bool isFirstPrintPage)
        {
            StringBuilder sb = new StringBuilder();

            foreach (Chapter chapter in AppState.Workbook.Chapters)
            {
                try
                {
                    // if scope is CHAPTER, only act when we encounter the active chapter
                    if (_printScope == PrintScope.WORKBOOK || _printScope == PrintScope.CHAPTER && _chapterToPrint == chapter)
                    {
                        sb.Append(PrintChapterTitle(chapter));
                        if (!isFirstPrintPage)
                        {
                            sb.AppendLine();
                        }
                        else
                        {
                            isFirstPrintPage = false;
                        }

                        bool introPrinted = false;
                        if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_INTRO))
                        {
                            if (!chapter.IsIntroEmpty())
                            {
                                sb.Append(PrintIntroToText(_printScope, chapter));
                                introPrinted = true;
                            }
                        }

                        if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_STUDY))
                        {
                            if (!chapter.IsStudyEmpty())
                            {
                                sb.Append(PrintStudyToText(_printScope, chapter, introPrinted));
                            }
                        }

                        if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_GAMES))
                        {
                            if (chapter.ModelGames.Count > 0)
                            {
                                sb.Append(AddPageBreakPlaceholder());
                                sb.Append(PrintGamesHeader());

                                for (int i = 0; i < chapter.ModelGames.Count; i++)
                                {
                                    sb.Append(PrintGameToText(chapter, i));
                                }
                            }
                        }

                        if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_EXERCISES))
                        {
                            if (chapter.Exercises.Count > 0)
                            {
                                sb.Append(AddPageBreakPlaceholder());
                                sb.Append(PrintExercisesHeader());

                                for (int i = 0; i < chapter.Exercises.Count; i++)
                                {
                                    sb.Append(PrintExerciseToText(chapter, i));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Message("PrintChaptersToText()" + "Chapter " + chapter.Index, ex);
                }
            }

            return sb.ToString();
        }


        //********************************************
        //
        // INDICES AND HEADERS
        //
        //********************************************


        /// <summary>
        /// Prints the workbook front page to text.
        /// </summary>
        /// <returns></returns>
        private static string PrintWorkbookFrontPage()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Properties.Resources.ChessForgeGenNotice);
            sb.AppendLine(" (" + Constants.CHAR_TRADE_MARK + ")");
            sb.AppendLine();

            sb.AppendLine(Properties.Resources.Title + ": " + AppState.Workbook.Title);
            sb.AppendLine();

            sb.AppendLine(Properties.Resources.Version + ": " + AppState.Workbook.Version);

            if (!string.IsNullOrEmpty(AppState.Workbook.Author))
            {
                sb.Append(Properties.Resources.Author + ": ");
                sb.AppendLine(AppState.Workbook.Author);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Prints the contents of the workbook to text.
        /// </summary>
        /// <param name="workbook"></param>
        /// <returns></returns>
        private static string PrintWorkbookContents(Workbook workbook)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(Properties.Resources.Contents);
            sb.AppendLine(CreateUnderscoreLine(Properties.Resources.Contents));

            foreach (Chapter chapter in workbook.Chapters)
            {
                sb.AppendLine((chapter.Index + 1).ToString() + ".\t " + chapter.Title);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Prints the bookmarks, if any.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        private static string PrintBookmarks(PrintScope scope)
        {
            StringBuilder sb = new StringBuilder();

            List<BookmarkWrapper> bookMarkList = new List<BookmarkWrapper>();
            if (scope == PrintScope.CHAPTER)
            {
                BookmarkManager.BuildBookmarkList(bookMarkList, _chapterToPrint, GameData.ContentType.NONE);
            }
            else
            {
                BookmarkManager.BuildBookmarkList(bookMarkList, null, GameData.ContentType.NONE);
            }

            if (bookMarkList.Count > 0)
            {
                BookmarkManager.SortBookmarks(bookMarkList);

                sb.Append(AddPageBreakPlaceholder());
                sb.AppendLine(PrintBookmarksHeader());
                sb.AppendLine();

                for (int i = 0; i < bookMarkList.Count; i++)
                {
                    sb.Append(PrintBookmarkToText(bookMarkList[i]));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Prints the index of games or exercises to text.
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="gameOrExerc"></param>
        /// <returns></returns>
        private static string PrintGameOrExerciseIndex(Workbook workbook, bool gameOrExerc)
        {
            StringBuilder sb = new StringBuilder();

            string indexTitle = gameOrExerc ? Properties.Resources.Games : Properties.Resources.Exercises;
            sb.AppendLine(indexTitle);
            sb.Append(CreateUnderscoreLine(indexTitle));

            int itemCounter = 0;
            foreach (Chapter chapter in workbook.Chapters)
            {
                int itemCount = gameOrExerc ? chapter.GetModelGameCount() : chapter.GetExerciseCount();
                if (itemCount > 0)
                {
                    sb.AppendLine();

                    string chapterTitle = Properties.Resources.Chapter + " " + (chapter.Index + 1).ToString();
                    sb.AppendLine(chapterTitle);

                    List<Article> articles = gameOrExerc ? chapter.ModelGames : chapter.Exercises;
                    foreach (Article article in articles)
                    {
                        itemCounter++;

                        string articleTitle = gameOrExerc ?
                              article.Tree.Header.BuildGameHeaderLine(false, false, true, true, false)
                            : article.Tree.Header.BuildGameHeaderLine(true, false, true, true, false);

                        string title = itemCounter.ToString() + ". " + articleTitle;
                        sb.AppendLine(title);
                    }
                }
            }

            if (itemCounter == 0)
            {
                return "";
            }
            else
            {
                return sb.ToString();
            }
        }

        /// <summary>
        /// Prints the chapter title to text.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static string PrintChapterTitle(Chapter chapter)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(AddPageBreakPlaceholder());

            string title = chapter.Title;
            int chapterNo = chapter.Index + 1;
            string sNo = Properties.Resources.Chapter + " " + chapterNo.ToString() + ":";
            sb.Append(sNo + " ");

            sb.AppendLine(title);
            sb.AppendLine(CreateUnderscoreLine(sNo));

            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Prints the header for the study.
        /// </summary>
        /// <returns></returns>
        private static string PrintStudyHeader()
        {
            StringBuilder sb = new StringBuilder();

            string studyTitle = Properties.Resources.Study;
            if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_STUDY))
            {
                studyTitle = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CUSTOM_TERM_STUDY);
            }

            if (!string.IsNullOrWhiteSpace(studyTitle))
            {
                sb.AppendLine(studyTitle);
                sb.AppendLine(CreateUnderscoreLine(studyTitle));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Prints the header before the games part.
        /// </summary>
        /// <returns></returns>
        private static string PrintGamesHeader()
        {
            StringBuilder sb = new StringBuilder();

            string gamesHeader = Properties.Resources.Games;
            if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_GAMES))
            {
                gamesHeader = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CUSTOM_TERM_GAMES);
            }

            if (!string.IsNullOrWhiteSpace(gamesHeader))
            {
                sb.AppendLine(gamesHeader);
                sb.AppendLine(CreateUnderscoreLine(gamesHeader));
            }

            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Prints the header before the exercises part.
        /// </summary>
        /// <returns></returns>
        private static string PrintExercisesHeader()
        {
            StringBuilder sb = new StringBuilder();

            string exercisesHeader = Properties.Resources.Exercises;
            if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_EXERCISES))
            {
                exercisesHeader = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.USE_CUSTOM_EXERCISES);
            }

            if (!string.IsNullOrWhiteSpace(exercisesHeader))
            {
                sb.AppendLine(exercisesHeader);
                sb.AppendLine(CreateUnderscoreLine(exercisesHeader));
            }

            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Prints the header before the bookmarks part.
        /// </summary>
        /// <returns></returns>
        private static string PrintBookmarksHeader()
        {
            StringBuilder sb = new StringBuilder();

            string bookmarksHeader = Properties.Resources.Bookmarks;
            if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_BOOKMARKS))
            {
                bookmarksHeader = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CUSTOM_TERM_BOOKMARKS);
            }

            if (!string.IsNullOrWhiteSpace(bookmarksHeader))
            {
                sb.AppendLine(bookmarksHeader);
                sb.AppendLine(CreateUnderscoreLine(bookmarksHeader));
            }

            return sb.ToString();
        }


        //********************************************
        //
        // PREPARE ARTICLE PRINTING
        //
        //********************************************

        /// <summary>
        /// Prints the chapters view to text.
        /// </summary>
        /// <returns></returns>
        private static string PrintChaptersViewToText()
        {
            RichTextBox rtb = new RichTextBox();
            var chaptersView = new ChaptersView(rtb, null, true);
            chaptersView.BuildFlowDocumentForChaptersView(false);

            return FlowDocumentToText(rtb.Document, null);
        }

        /// <summary>
        /// Prints the intro to text.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="chapter"></param>
        private static string PrintIntroToText(PrintScope scope, Chapter chapter)
        {
            StringBuilder sb = new StringBuilder();

            if (scope == PrintScope.ARTICLE)
            {
                sb.Append(PrintChapterTitle(chapter));
            }

            sb.Append(PrintIntro(chapter));

            return sb.ToString();
        }

        /// <summary>
        /// Prints the study to text.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="chapter"></param>
        /// <param name="introPrinted"></param>
        /// <returns></returns>
        private static string PrintStudyToText(PrintScope scope, Chapter chapter, bool introPrinted)
        {
            StringBuilder sb = new StringBuilder();

            if (scope == PrintScope.ARTICLE)
            {
                sb.Append(PrintChapterTitle(chapter));
            }

            // if we are printing just this study or there was no Intro, print without the header and page break.
            if (scope != PrintScope.ARTICLE && introPrinted)
            {
                sb.Append(AddPageBreakPlaceholder());
                sb.Append(PrintStudyHeader());
            }

            sb.Append(PrintArticle(chapter.StudyTree.Tree));

            return sb.ToString();
        }

        /// <summary>
        /// Prints the games to text.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="gameIndex"></param>
        /// <returns></returns>
        private static string PrintGameToText(Chapter chapter, int gameIndex)
        {
            StringBuilder sb = new StringBuilder();

            Article game;

            if (gameIndex >= 0)
            {
                game = chapter.ModelGames[gameIndex];
            }
            else
            {
                game = chapter.ModelGames[chapter.ActiveModelGameIndex];
            }

            _currentGameNumber++;
            int gameNo = _continuousArticleNumbering ? _currentGameNumber : gameIndex + 1;

            // if index < 0 then we are printing for the CurrentView scope hence no need for a header
            if (gameIndex >= 0)
            {
                string gameWord = Properties.Resources.Game;
                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_GAME))
                {
                    gameWord = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CUSTOM_TERM_GAME);
                }

                string gameHeader = gameWord + " " + gameNo.ToString();

                sb.AppendLine();
                sb.AppendLine(gameHeader);
            }

            sb.Append(PrintArticle(game.Tree));
            return sb.ToString();
        }

        /// <summary>
        /// Prints the exercises to text.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="exerciseIndex"></param>
        /// <returns></returns>
        private static string PrintExerciseToText(Chapter chapter, int exerciseIndex)
        {
            StringBuilder sb = new StringBuilder();

            Article exercise;

            if (exerciseIndex >= 0)
            {
                exercise = chapter.Exercises[exerciseIndex];
            }
            else
            {
                exercise = chapter.Exercises[chapter.ActiveExerciseIndex];
            }

            _currentExerciseNumber++;
            int exerciseNo = _continuousArticleNumbering ? _currentExerciseNumber : exerciseIndex + 1;

            // if index < 0 then we are printing for the CurrentView scope hence no need for a header
            if (exerciseIndex >= 0)
            {
                string exerciseWord = Properties.Resources.Exercise;
                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.USE_CUSTOM_EXERCISE))
                {
                    exerciseWord = ConfigurationRtfExport.GetStringValue(ConfigurationRtfExport.CUSTOM_TERM_EXERCISE);
                }

                string exerciseHeader = exerciseWord + " " + exerciseNo.ToString();

                sb.AppendLine();
                sb.AppendLine(exerciseHeader);
            }

            sb.Append(PrintArticle(exercise.Tree));

            return sb.ToString();
        }

        /// <summary>
        /// Generates text representing a bookmark.
        /// </summary>
        /// <param name="bookmark"></param>
        /// <returns></returns>
        private static string PrintBookmarkToText(BookmarkWrapper bookmark)
        {
            StringBuilder sb = new StringBuilder();

            if (bookmark.Node != null)
            {
                sb.AppendLine();
                sb.AppendLine(Properties.Resources.Chapter + " " + (bookmark.ChapterIndex + 1).ToString());
                sb.AppendLine(BookmarkUtils.BuildArticleLabelText(bookmark));
                sb.AppendLine("[" + FenParser.GenerateFenFromPosition(bookmark.Node.Position) + "]");
            }

            return sb.ToString();
        }

        //********************************************
        //
        // PRINT INIDIVIDUAL ARTICLES
        //
        //********************************************

        /// <summary>
        /// Prints the intro of the chapter to text.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static string PrintIntro(Chapter chapter)
        {
            if (AppState.ActiveTab == TabViewType.INTRO)
            {
                // Intro is a special case where we need to save it to update the underlying data.
                AppState.MainWin.SaveIntro();
            }

            RichTextBox rtb = new RichTextBox();

            StringBuilder sb = new StringBuilder();
            if (_printScope == PrintScope.ARTICLE)
            {
                sb.Append(PrintChapterTitle(chapter));
            }

            IntroView intro = new IntroView(rtb, chapter, true);

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

            sb.Append(FlowDocumentToText(rtb.Document, chapter.Intro.Tree));
            return sb.ToString();
        }

        /// <summary>
        /// Print an article to text.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        private static string PrintArticle(VariationTree tree)
        {
            RichTextBox rtbView = new RichTextBox();

            if (tree.ContentType == GameData.ContentType.MODEL_GAME)
            {
                VariationTreeView gameView = new VariationTreeView(rtbView, GameData.ContentType.MODEL_GAME, true);
                gameView.BuildFlowDocumentForVariationTree(false, tree);
            }
            else if (tree.ContentType == GameData.ContentType.EXERCISE)
            {
                ExerciseTreeView exerciseView = new ExerciseTreeView(rtbView, GameData.ContentType.EXERCISE, true);
                exerciseView.BuildFlowDocumentForVariationTree(false, tree);
                RemoveExerciseUnderBoardControls(rtbView.Document);
            }
            else if (tree.ContentType == GameData.ContentType.STUDY_TREE)
            {
                StudyTreeView studyView = new StudyTreeView(rtbView, GameData.ContentType.STUDY_TREE, true);
                studyView.BuildFlowDocumentForVariationTree(false, tree);
            }

            return FlowDocumentToText(rtbView.Document, tree);
        }

        /// <summary>
        /// Removes controls under the board in the exercise view.
        /// </summary>
        /// <param name="doc"></param>
        private static void RemoveExerciseUnderBoardControls(FlowDocument doc)
        {
            Paragraph paraUnderBoard = null;

            foreach (Block block in doc.Blocks)
            {
                if (block is Paragraph para)
                {
                    if (para.Name == RichTextBoxUtilities.ExerciseUnderBoardControls)
                    {
                        paraUnderBoard = para;
                        break;
                    }
                }
            }

            if (paraUnderBoard != null)
            {
                doc.Blocks.Remove(paraUnderBoard);
            }
        }

        //********************************************
        //
        // UTILITIES
        //
        //********************************************


        /// <summary>
        /// Converts a FlowDocument to text.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        private static string FlowDocumentToText(FlowDocument doc, VariationTree tree)
        {
            if (tree != null)
            {
                ReplaceNonTextInFlowDocument(doc, tree);
            }
            InsertIndents(doc);

            StringBuilder sb = new StringBuilder();
            TextRange textRange = new TextRange(doc.ContentStart, doc.ContentEnd);
            string text = textRange.Text;
            sb.Append(text);
            return sb.ToString();
        }

        /// <summary>
        /// Inserts spaces as indents.
        /// </summary>
        /// <param name="doc"></param>
        private static void InsertIndents(FlowDocument doc)
        {
            List<Block> blocksToModify = new List<Block>();

            // Iterate over the blocks and add them to the list
            foreach (Block block in doc.Blocks)
            {
                blocksToModify.Add(block);
            }

            foreach (Block block in blocksToModify)
            {
                if (block is Paragraph para)
                {
                    if (block.Margin.Left > 0)
                    {
                        string indent = new string(' ', (int)(block.Margin.Left / 10));

                        Run rIndent = new Run(indent);
                        if (para.Inlines.Count == 0)
                        {
                            para.Inlines.Add(rIndent);
                        }
                        else
                        {
                            para.Inlines.InsertBefore(para.Inlines.FirstInline, rIndent);
                        }

                        // for each inline with 'n' insert indent after '\n'  
                        List<Inline> inlinesToModify = new List<Inline>();
                        foreach (Inline inl in para.Inlines)
                        {
                            inlinesToModify.Add(inl);
                        }
                        foreach (Inline inl in inlinesToModify)
                        {
                            if (inl is Run run && !string.IsNullOrEmpty(run.Text))
                            {
                                run.Text = run.Text.Replace("\n", "\n" + indent);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Replaces diagrams in the FlowDocument with FEN strings. 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="tree"></param>
        private static void ReplaceNonTextInFlowDocument(FlowDocument doc, VariationTree tree)
        {
            List<Block> blocksToModify = new List<Block>();

            foreach (Block block in doc.Blocks)
            {
                blocksToModify.Add(block);
            }

            foreach (Block block in blocksToModify)
            {
                if (block is Paragraph para)
                {
                    if (para.Name != null && para.Name.StartsWith(RichTextBoxUtilities.DiagramParaPrefix))
                    {
                        int nodeId = TextUtils.GetIdFromPrefixedString(para.Name);
                        TreeNode nd = tree.GetNodeFromNodeId(nodeId);
                        para.Inlines.Clear();

                        if (nd != null)
                        {
                            string fenText = "[" + FenParser.GenerateFenFromPosition(nd.Position) + "]";

                            if (tree.ContentType == GameData.ContentType.EXERCISE && nd.NodeId == 0)
                            {
                                if (nd.ColorToMove == PieceColor.White)
                                {
                                    fenText += " " + Constants.CHAR_WHITE_LARGE_TRIANGLE_UP.ToString();
                                }
                                else
                                {
                                    fenText += " " + Constants.CHAR_BLACK_LARGE_TRIANGLE_DOWN.ToString();
                                }
                            }
                            para.Inlines.Add(new Run(fenText + '\n'));
                        }
                    }
                    else
                    {
                        ProcessInlines(para, tree);
                    }
                }
                else
                {
                    AppLog.Message("RTF Write: Block element type = " + block.GetType().ToString());
                }
            }
        }

        /// <summary>
        /// Processes the inlines of a paragraph.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="tree"></param>
        private static void ProcessInlines(Paragraph para, VariationTree tree)
        {
            List<Inline> inlinesToModify = new List<Inline>();
            foreach (Inline inl in para.Inlines)
            {
                inlinesToModify.Add(inl);
            }

            bool lastRunWasIntroMove = false;
            para.Inlines.Clear();
            foreach (Inline inl in inlinesToModify)
            {
                if (inl is Run run)
                {
                    lastRunWasIntroMove = false;
                    para.Inlines.Add(run);
                }
                else if (inl is InlineUIContainer iuc)
                {
                    if (inl.Name != null && 
                        (inl.Name.StartsWith(RichTextBoxUtilities.InlineDiagramIucPrefix) || inl.Name.StartsWith(RichTextBoxUtilities.InlineDiagramBeforeMoveIucPrefix)))
                    {
                        lastRunWasIntroMove = false;

                        int nodeId = TextUtils.GetIdFromPrefixedString(inl.Name);
                        TreeNode nd = tree.GetNodeFromNodeId(nodeId);

                        string fenText;
                        if (nd.IsDiagramBeforeMove)
                        {
                            fenText = "[" + FenParser.GenerateFenFromPosition(nd.Parent.Position) + "]";
                        }
                        else
                        {
                            fenText = "[" + FenParser.GenerateFenFromPosition(nd.Position) + "]";
                        }

                        para.Inlines.Add(new Run(fenText));
                    }
                    else if (inl.Name != null && inl.Name.StartsWith(RichTextBoxUtilities.UicMovePrefix))
                    {
                        lastRunWasIntroMove = ProcessIntroMove(para, iuc, lastRunWasIntroMove);
                    }
                    else
                    {
                        lastRunWasIntroMove = false;
                    }
                }
            }
        }

        /// <summary>
        /// Replace the special Move encoding found in the Intro view
        /// witht the text of the move.
        /// </summary>
        /// <param name="para"></param>
        /// <param name="iuc"></param>
        /// <param name="lastRunWasIntroMove"></param>
        /// <returns></returns>
        private static bool ProcessIntroMove(Paragraph para, InlineUIContainer iuc, bool lastRunWasIntroMove)
        {
            bool handled = false;

            // there should be just one child of type TextBlock
            var tb = iuc.Child;
            if (tb != null && tb is TextBlock tbMove)
            {
                // this should have just one inline of type Run
                if (tbMove.Inlines.Count > 0)
                {
                    Run rMove = tbMove.Inlines.FirstInline as Run;
                    if (rMove != null && rMove.Text != null)
                    {
                        Run moveRun = new Run();
                        moveRun.Text = rMove.Text;

                        // if previous text was also an IntroMove, remove the leading space so we don't have too many!
                        if (lastRunWasIntroMove && !string.IsNullOrEmpty(moveRun.Text) && moveRun.Text[0] == ' ')
                        {
                            moveRun.Text = moveRun.Text.Substring(1);
                        }
                        para.Inlines.Add(moveRun);
                        handled = true;
                    }
                }
            }

            return handled;
        }

        /// <summary>
        /// Replaces the NAG characters that may not render well in text.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string ReplaceSpecialChars(string text)
        {
            string outText = text.Replace(Constants.NagsDict[14].ToString(), "+/=");
            outText = outText.Replace(Constants.NagsDict[15].ToString(), "=/+");
            outText = outText.Replace(Constants.NagsDict[16].ToString(), "+/-");
            outText = outText.Replace(Constants.NagsDict[17].ToString(), "-/+");

            return outText;
        }

        /// <summary>
        /// Resets the article counters.
        /// </summary>
        private static void ResetCounters()
        {
            _currentGameNumber = 0;
            _currentExerciseNumber = 0;
        }

        /// <summary>
        /// Adds a "page break" placeholder.
        /// </summary>
        /// <returns></returns>
        private static string AddPageBreakPlaceholder()
        {
            return "\n\n\n";
        }

        /// <summary>
        /// Creates a line of underscores.
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        private static string CreateUnderscoreLine(string txt)
        {
            if (string.IsNullOrEmpty(txt))
            {
                return "";
            }
            else
            {
                return new string('=', txt.Length);
            }
        }
    }
}

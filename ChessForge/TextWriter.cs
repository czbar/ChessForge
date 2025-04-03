using ChessForge.TreeViewManagement;
using ChessPosition;
using GameTree;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace ChessForge
{
    public class TextWriter
    {
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
                    PrintWorkbookOrChapterScope(scope);
                }

                WriteOutFile(fileName, sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Prints the workbook front page to text.
        /// </summary>
        /// <returns></returns>
        private static string PrintWorkbookFrontPage()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Properties.Resources.ChessForgeGenNotice);
            sb.Append(" " + Constants.CHAR_SUPER_LEFT_PARENTHESIS + Constants.CHAR_TRADE_MARK + Constants.CHAR_SUPER_RIGHT_PARENTHESIS);

            sb.AppendLine();

            sb.AppendLine(AppState.Workbook.Title);

            sb.AppendLine(Properties.Resources.Version + ": " + AppState.Workbook.Version);

            if (!string.IsNullOrEmpty(AppState.Workbook.Author))
            {
                sb.Append(Properties.Resources.Author + ": ");
                sb.AppendLine(AppState.Workbook.Author);
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

            foreach (Chapter chapter in workbook.Chapters)
            {
                sb.AppendLine((chapter.Index + 1).ToString() + ".\t " + chapter.Title);
            }

            return sb.ToString();
        }

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
                    sb.AppendLine(PrintWorkbookContents(AppState.Workbook));
                    sb.Append(AddPageBreakPlaceholder());
                }
            }

            PrintChapters(isFirstPrintPage);

            if (scope == PrintScope.WORKBOOK)
            {
                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_GAME_INDEX))
                {
                    string gameIndexText = PrintGameOrExerciseIndex(AppState.Workbook, true);
                    if (!string.IsNullOrEmpty(gameIndexText))
                    {
                        sb.Append(gameIndexText);
                        sb.Append(AddPageBreakPlaceholder());
                    }
                }

                if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_EXERCISE_INDEX))
                {
                    string exerciseIndexText = PrintGameOrExerciseIndex(AppState.Workbook, false);
                    if (!string.IsNullOrEmpty(exerciseIndexText))
                    {
                        sb.Append(exerciseIndexText);
                        sb.Append(AddPageBreakPlaceholder());
                    }
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

            sb.AppendLine(gameOrExerc ? Properties.Resources.GameIndex : Properties.Resources.ExerciseIndex);

            int itemCounter = 0;
            foreach (Chapter chapter in workbook.Chapters)
            {
                int itemCount = gameOrExerc ? chapter.GetModelGameCount() : chapter.GetExerciseCount();
                if (itemCount > 0)
                {
                    string chapterTitle = Properties.Resources.Chapter + " " + (chapter.Index + 1).ToString();
                    sb.AppendLine(chapterTitle);

                    List<Article> articles = gameOrExerc ? chapter.ModelGames : chapter.Exercises;
                    foreach (Article article in articles)
                    {
                        itemCounter++;

                        string articleTitle = gameOrExerc ?
                              article.Tree.Header.BuildGameHeaderLine(false, false, true, true, false)
                            : article.Tree.Header.BuildGameHeaderLine(true, false, true, true, false);

                        string title = itemCounter.ToString() + ".\t " + articleTitle;
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

            string title = chapter.Title;
            int chapterNo = chapter.Index + 1;
            string runNo = Properties.Resources.Chapter + " " + chapterNo.ToString();

            sb.AppendLine(runNo);

            // if title is empty, do not include the second paragraph
            if (!string.IsNullOrWhiteSpace(title))
            {
                sb.AppendLine(title);
            }

            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Prints the intro of the chapter to text.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static string PrintIntro(Chapter chapter)
        {
            // TODO: use CopySelectionToClipboard()

            return "";
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
                            sb.Append(AddPageBreakPlaceholder());
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
                                PrintIntroToText(_printScope, chapter);
                                introPrinted = true;
                            }
                        }

                        if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_STUDY))
                        {
                            if (!chapter.IsStudyEmpty())
                            {
                                PrintStudyToText(_printScope, chapter, introPrinted);
                            }
                        }

                        if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_GAMES))
                        {
                            if (chapter.ModelGames.Count > 0)
                            {
                                sb.Append(PrintGamesHeader());
                                sb.Append(AddPageBreakPlaceholder());

                                for (int i = 0; i < chapter.ModelGames.Count; i++)
                                {
                                    PrintGameToText(chapter, i);
                                }
                            }
                        }

                        if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_EXERCISES))
                        {
                            if (chapter.Exercises.Count > 0)
                            {
                                sb.Append( PrintExercisesHeader());
                                sb.Append(AddPageBreakPlaceholder());

                                for (int i = 0; i < chapter.Exercises.Count; i++)
                                {
                                    PrintExerciseToText(chapter, i);
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
        // PRINT INIDIVIDUAL ARTICLES
        //
        //********************************************

        /// <summary>
        /// Prints as text the view of the passed tree.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        private static string PrintTreeView(VariationTree tree)
        {
            StringBuilder sb = new StringBuilder();

            tree = AppState.ActiveVariationTree;
            sb.AppendLine(BuildPageHeader(tree, tree.ContentType));
            LineSectorsBuilder builder = new LineSectorsBuilder();
            builder.BuildLineSectors(tree.Nodes[0], false);
            foreach (LineSector sector in builder.LineSectors)
            {
                string lineText = WriteLineSector(sector, tree);
                sb.AppendLine(lineText);
            }

            return sb.ToString();
        }


        //********************************************
        //
        // HEADER LINES
        //
        //********************************************

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
                sb.Append(studyTitle);
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
                sb.Append(gamesHeader);
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
                sb.Append(exercisesHeader);
            }

            sb.AppendLine();

            return sb.ToString();
        }


        //********************************************
        //
        // "PRINT" TO TEXT
        //
        //********************************************

        /// <summary>
        /// Prints the intro to text.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="chapter"></param>
        private static void PrintIntroToText(PrintScope scope, Chapter chapter)
        {
            StringBuilder sb = new StringBuilder();

            if (scope == PrintScope.ARTICLE)
            {
                sb.Append(PrintChapterTitle(chapter));
            }

            sb.Append(PrintIntro(chapter));
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
                sb.Append(PrintStudyHeader());
                sb.Append(AddPageBreakPlaceholder());
            }

            sb.Append(PrintTreeView(chapter.StudyTree.Tree));

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

            sb.Append(PrintTreeView(game.Tree));

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

            sb.Append(PrintTreeView(exercise.Tree));

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
                //PrintChaptersViewToText();
            }
            else
            {
                switch (AppState.Workbook.ActiveArticle.Tree.ContentType)
                {
                    case GameData.ContentType.INTRO:
                        //PrintIntroToText(chapter);
                        break;
                    case GameData.ContentType.STUDY_TREE:
                        txt = PrintTreeView(AppState.Workbook.ActiveArticle.Tree);
                        break;
                    case GameData.ContentType.MODEL_GAME:
                        txt = PrintTreeView(AppState.Workbook.ActiveArticle.Tree);
                        break;
                    case GameData.ContentType.EXERCISE:
                        txt = PrintTreeView(AppState.Workbook.ActiveArticle.Tree);
                        break;
                }
            }

            return txt;
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

        /// <summary>
        /// Shows the dialog for the user to selecy the target file.
        /// </summary>
        /// <returns></returns>
        public static string SelectTargetTextFile()
        {
            string textExt = ".txt";
            string textFileName;

            if (string.IsNullOrEmpty(AppState.WorkbookFilePath))
            {
                textFileName = TextUtils.RemoveInvalidCharsFromFileName(WorkbookManager.SessionWorkbook.Title) + textExt;
            }
            else
            {
                textFileName = FileUtils.ReplacePathExtension(AppState.WorkbookFilePath, textExt);
            }

            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = Properties.Resources.TextFiles + " (*.txt)|*.txt"
            };

            saveDlg.FileName = Path.GetFileName(textFileName);
            saveDlg.Title = Properties.Resources.ExportText;

            saveDlg.OverwritePrompt = true;
            if (saveDlg.ShowDialog() == true)
            {
                textFileName = saveDlg.FileName;
            }
            else
            {
                textFileName = "";
            }

            return textFileName;
        }

        /// <summary>
        /// Writes a line sector to a string.
        /// </summary>
        /// <param name="sector"></param>
        /// <returns></returns>
        private static string WriteLineSector(LineSector sector, VariationTree tree)
        {
            StringBuilder sb = new StringBuilder();

            bool firstMove = true;

            string indent = "";

            for (int i = 0; i < sector.DisplayLevel; i++)
            {
                indent += "  ";
            }

            for (int i = 0; i < sector.Nodes.Count; i++)
            {
                if (firstMove)
                {
                    sb.Append(indent);
                }

                TreeNode nd = sector.Nodes[i];
                if (nd.NodeId != 0)
                {
                    if (nd.NodeId == LineSector.OPEN_BRACKET)
                    {
                        sb.Append("(");
                    }
                    else if (nd.NodeId == LineSector.CLOSE_BRACKET)
                    {
                        sb.Append(") ");
                    }
                    else
                    {
                        string textForNode = GetTextForNode(tree, nd, ref firstMove, indent);
                        sb.Append(textForNode);
                        if (i < sector.Nodes.Count - 1 && sector.Nodes[i + 1].NodeId != LineSector.CLOSE_BRACKET)
                        {
                            sb.Append(' ');
                        }
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds the header for the page.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private static string BuildPageHeader(VariationTree tree, GameData.ContentType contentType)
        {
            StringBuilder sb = new StringBuilder();

            if (tree != null)
            {
                switch (contentType)
                {
                    case GameData.ContentType.MODEL_GAME:
                    case GameData.ContentType.EXERCISE:
                        string whitePlayer = tree.Header.GetWhitePlayer(out _);
                        string blackPlayer = tree.Header.GetBlackPlayer(out _);

                        string whitePlayerElo = tree.Header.GetWhitePlayerElo(out _);
                        string blackPlayerElo = tree.Header.GetBlackPlayerElo(out _);

                        bool hasPlayerNames = !(string.IsNullOrWhiteSpace(whitePlayer) && string.IsNullOrWhiteSpace(blackPlayer));

                        if (hasPlayerNames)
                        {
                            sb.AppendLine(BuildPlayerLine(whitePlayer, whitePlayerElo));
                            sb.AppendLine(BuildPlayerLine(blackPlayer, blackPlayerElo));
                        }

                        if (!string.IsNullOrEmpty(tree.Header.GetEventName(out _)))
                        {
                            if (hasPlayerNames)
                            {
                                string round = tree.Header.GetRound(out _);
                                if (!string.IsNullOrWhiteSpace(round))
                                {
                                    round = " (" + round + ")";
                                }
                                else
                                {
                                    round = "";
                                }
                                sb.AppendLine(tree.Header.GetEventName(out _) + round);
                            }
                            else
                            {
                                sb.AppendLine(tree.Header.GetEventName(out _));
                            }
                        }

                        string annotator = tree.Header.GetAnnotator(out _);
                        if (!string.IsNullOrWhiteSpace(annotator))
                        {
                            annotator = "      " + Properties.Resources.Annotator + ": " + annotator;
                            sb.AppendLine(annotator);
                        }

                        string dateForDisplay = TextUtils.BuildDateFromDisplayFromPgnString(tree.Header.GetDate(out _));
                        if (!string.IsNullOrEmpty(dateForDisplay))
                        {
                            sb.AppendLine("      " + Properties.Resources.Date + ": " + dateForDisplay);
                        }

                        string eco = tree.Header.GetECO(out _);
                        string result = tree.Header.GetResult(out _);
                        BuildResultAndEcoLine(eco, result, out string rEco, out string rResult);
                        if (rEco != null || rResult != null)
                        {
                            sb.Append("      ");

                            if (rEco != null)
                            {
                                sb.Append(rEco);
                            }
                            if (rResult != null)
                            {
                                sb.Append(rResult);
                            }

                            sb.AppendLine();
                        }
                        break;
                    case GameData.ContentType.STUDY_TREE:
                        sb.AppendLine(BuildChapterTitle());
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds the chapter title.
        /// </summary>
        /// <returns></returns>
        private static string BuildChapterTitle()
        {
            StringBuilder sb = new StringBuilder();
            Chapter chapter = WorkbookManager.SessionWorkbook.ActiveChapter;
            if (chapter != null)
            {
                sb.AppendLine(chapter.GetTitle());

                if (!string.IsNullOrWhiteSpace(chapter.GetAuthor()))
                {
                    sb.AppendLine();
                    string rAuthor = "    " + Properties.Resources.Author + ": " + chapter.GetAuthor();
                    sb.AppendLine(rAuthor);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds the result and eco line.
        /// </summary>
        /// <param name="eco"></param>
        /// <param name="result"></param>
        /// <param name="rEco"></param>
        /// <param name="rResult"></param>
        private static void BuildResultAndEcoLine(string eco, string result, out string rEco, out string rResult)
        {
            rEco = null;
            rResult = null;
            if (!string.IsNullOrWhiteSpace(eco))
            {
                rEco = eco + "  ";
            }

            if (!string.IsNullOrWhiteSpace(result) && result != "*")
            {
                rResult = "(" + result + ")";
            }
        }

        /// <summary>
        /// Gets the text for a node.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="isFirstNode"></param>
        /// <param name="indent"></param>
        /// <returns></returns>
        private static string GetTextForNode(VariationTree tree, TreeNode nd, ref bool isFirstNode, string indent)
        {
            StringBuilder sb = new StringBuilder();

            bool includeNumber = isFirstNode;
            isFirstNode = false;

            if (!string.IsNullOrWhiteSpace(nd.CommentBeforeMove))
            {
                sb.Append(Constants.START_COMMENT + nd.CommentBeforeMove + Constants.END_COMMENT + " ");
                includeNumber = true;
            }

            sb.Append(MoveUtils.BuildSingleMoveText(nd, includeNumber, true, tree.MoveNumberOffset));

            string fen = "";
            if (nd.IsDiagram)
            {
                fen = "[" + FenParser.GenerateFenFromPosition(nd.Position, tree.MoveNumberOffset) + "]";
            }

            if (fen.Length > 0 && nd.IsDiagramPreComment)
            {
                sb.Append(Environment.NewLine + indent + fen + Environment.NewLine);
                isFirstNode = true;
            }
            if (!string.IsNullOrWhiteSpace(nd.Comment))
            {
                sb.Append(" " + Constants.START_COMMENT + nd.Comment + Constants.END_COMMENT);
            }
            if (fen.Length > 0 && !nd.IsDiagramPreComment)
            {
                sb.Append(Environment.NewLine + indent + fen + Environment.NewLine);
                isFirstNode = true;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds a player line.
        /// </summary>
        /// <param name="playerName"></param>
        /// <param name="playerElo"></param>
        /// <returns></returns>
        private static string BuildPlayerLine(string playerName, string playerElo)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return "NN";
            }

            if (string.IsNullOrWhiteSpace(playerElo))
            {
                return playerName;
            }
            else
            {
                return playerName + " (" + playerElo + ")";
            }
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
            return "\n\n\n\n";
        }
    }
}

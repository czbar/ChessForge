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
    /// <summary>
    /// Builds text of the PGN Export file that will be written out.
    /// This is similar but substantially different to a full Workbook output 
    /// handled in the WorkbookFileTextBuilder class.
    /// </summary>
    public class PgnWriter
    {
        // scope of the export/print operation
        private static PrintScope _printScope;

        // Chapter being printed
        private static Chapter _chapterToPrint;

        // keeps output text as it is being built
        private static StringBuilder _fileText;

        /// <summary>
        /// Exports the scoped articles into a PGN text file.
        /// The exact scope is determined by the ConfigurationRtfExport parameters.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool WritePgn(string fileName)
        {
            bool result = true;

            try
            {
                // dummy write to trigger an error on access, before we take time to process.
                File.WriteAllText(fileName, "");

                PrintScope scope = ConfigurationRtfExport.GetScope();

                _printScope = scope;
                _chapterToPrint = AppState.ActiveChapter;

                StringBuilder sbOut = new StringBuilder();

                if (scope == PrintScope.ARTICLE)
                {
                    sbOut.Append(BuildActiveViewText(_chapterToPrint));
                }
                else
                {
                    sbOut.Append(BuildChaptersText());
                }

                WriteOutFile(fileName, sbOut.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Shows the dialog for the user to select the target file.
        /// </summary>
        /// <returns></returns>
        public static string SelectTargetPgnFile()
        {
            // we need more than just the extension to prevent overwriting the actual workbook!
            string pgnExt = "_exp.pgn";
            string pgnFileName = Configuration.LastPgnExportFile;

            if (string.IsNullOrEmpty(pgnFileName))
            {
                if (string.IsNullOrEmpty(AppState.WorkbookFilePath))
                {
                    pgnFileName = TextUtils.RemoveInvalidCharsFromFileName(WorkbookManager.SessionWorkbook.Title) + pgnExt;
                }
                else
                {
                    pgnFileName = FileUtils.ReplacePathExtension(AppState.WorkbookFilePath, pgnExt);
                }
            }

            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = Properties.Resources.PgnFiles + " (*.pgn)|*.pgn",
            };

            try
            {
                saveDlg.InitialDirectory = Path.GetDirectoryName(pgnFileName);
            }
            catch { }

            saveDlg.FileName = Path.GetFileName(pgnFileName);
            saveDlg.Title = Properties.Resources.ExportPgn;

            saveDlg.OverwritePrompt = true;
            if (saveDlg.ShowDialog() == true)
            {
                pgnFileName = saveDlg.FileName;
                Configuration.LastPgnExportFile = pgnFileName;
            }
            else
            {
                // user cancelled
                pgnFileName = "";
            }

            if (!IsTargetFileValid(pgnFileName))
            {
                pgnFileName = null;
            }

            return pgnFileName;
        }

        /// <summary>
        /// Checks if the target file is valid i.e. not a Workbook file.
        /// </summary>
        /// <param name="pgnFileName"></param>
        /// <returns></returns>
        private static bool IsTargetFileValid(string pgnFileName)
        {
            bool isValid = true;

            if (pgnFileName == AppState.WorkbookFilePath || WorkbookManager.IsChessForgeWorkbook(pgnFileName))
            {
                // do not allow overwriting a workbook file
                MessageBox.Show(Properties.Resources.ErrDontOverwriteWorkbook, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Builds text for all elements in all chapters
        /// selected per the _printScope.
        /// </summary>
        /// <returns></returns>
        private static string BuildChaptersText()
        {
            StringBuilder sb = new StringBuilder();

            foreach (Chapter chapter in AppState.Workbook.Chapters)
            {
                try
                {
                    // if scope is CHAPTER, only act when we encounter the active chapter
                    if (_printScope == PrintScope.WORKBOOK || _printScope == PrintScope.CHAPTER && _chapterToPrint == chapter)
                    {
                        sb.AppendLine();

                        if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_INTRO))
                        {
                            if (!chapter.IsIntroEmpty())
                            {
                                sb.Append(BuildIntroText(chapter));
                            }
                        }

                        if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_STUDY))
                        {
                            if (!chapter.IsStudyEmpty())
                            {
                                sb.Append(BuildArticleText(chapter.StudyTree));
                            }
                        }

                        if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_GAMES))
                        {
                            if (chapter.ModelGames.Count > 0)
                            {
                                for (int i = 0; i < chapter.ModelGames.Count; i++)
                                {
                                    sb.Append(BuildArticleText(chapter.ModelGames[i]));
                                }
                            }
                        }

                        if (ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.INCLUDE_EXERCISES))
                        {
                            if (chapter.Exercises.Count > 0)
                            {
                                for (int i = 0; i < chapter.Exercises.Count; i++)
                                {
                                    sb.Append(BuildArticleText(chapter.Exercises[i]));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Message("PrintChaptersToText()" + "Chapter " + chapter.Index, ex);
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds the PGN text for the active view.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static string BuildActiveViewText(Chapter chapter)
        {
            string txt = "";
            switch (AppState.Workbook.ActiveArticle.Tree.ContentType)
            {
                case GameData.ContentType.INTRO:
                    txt = BuildIntroText(chapter);
                    break;
                case GameData.ContentType.STUDY_TREE:
                case GameData.ContentType.MODEL_GAME:
                case GameData.ContentType.EXERCISE:
                    txt = BuildArticleText(AppState.Workbook.ActiveArticle);
                    break;
            }

            return txt;
        }

        /// <summary>
        /// Builds the PGN text for the Intro of a chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static string BuildIntroText(Chapter chapter)
        {
            if (!string.IsNullOrEmpty(chapter.Intro.CodedContent))
            {
                string headerText = WorkbookFileTextBuilder.BuildIntroHeaderText(chapter);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(headerText);

                // textual version of the content first
                string introPlainText = "";
                if (chapter.Intro.Tree.RootNode != null)
                {
                    introPlainText = WorkbookFileTextBuilder.BuildCommentText(chapter.Intro.Tree.RootNode.Comment) + "\n\n";
                }

                sb.Append(introPlainText);
                sb.AppendLine();

                // add terminating character
                sb.Append(" *");
                sb.AppendLine();
                sb.AppendLine();

                return WorkbookFileTextBuilder.DivideLine(sb.ToString(), 80);
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Builds the PGN text for an Article.
        /// </summary>
        /// <param name="article"></param>
        /// <returns></returns>
        private static string BuildArticleText(Article article)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder sbOutput = new StringBuilder();

            _fileText = new StringBuilder();

            try
            {
                string headerText = BuildArticleHeaderText(article.Tree);
                TreeNode root = article.Tree.RootNode;

                if (root != null)
                {
                    // There may be a comment or command before the first move. Add if so.
                    sb.Append(BuildCommandAndCommentText(root));
                    sb.Append(BuildTreeLineText(root));
                }

                sbOutput.Append(headerText + WorkbookFileTextBuilder.DivideLine(sb.ToString(), 80));

                // add result
                sbOutput.Append(" " + article.Tree.Header.GetResult(out _));
                sbOutput.AppendLine();
                sbOutput.AppendLine();
            }
            catch (Exception ex)
            {
                AppLog.Message("PrintArticle()", ex);
            }

            return sbOutput.ToString();
        }

        /// <summary>
        /// Builds PGN text for the article header.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        private static string BuildArticleHeaderText(VariationTree tree)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_EVENT, tree.Header.GetEventName(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_ROUND, tree.Header.GetRound(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_ECO, tree.Header.GetECO(out _)));
            AppendHeaderLine(ref sb, PgnHeaders.KEY_LICHESS_ID, tree.Header.GetLichessId(out _));
            AppendHeaderLine(ref sb, PgnHeaders.KEY_CHESSCOM_ID, tree.Header.GetChessComId(out _));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_DATE, tree.Header.GetDate(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_WHITE, tree.Header.GetWhitePlayer(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_BLACK, tree.Header.GetBlackPlayer(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_WHITE_ELO, tree.Header.GetWhitePlayerElo(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_BLACK_ELO, tree.Header.GetBlackPlayerElo(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_ANNOTATOR, tree.Header.GetAnnotator(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_RESULT, tree.Header.GetResult(out _)));

            if (tree.ContentType == GameData.ContentType.EXERCISE)
            {
                // FEN, if required, must be last for compatibility with ChesBase.
                if (tree.RootNode != null)
                {
                    BoardPosition pos = new BoardPosition(tree.Nodes[0].Position);
                    WorkbookFileTextBuilder.UpShiftOnePly(ref pos);
                    string fen = FenParser.GenerateFenFromPosition(pos, tree.MoveNumberOffset);
                    sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_FEN_STRING, fen));
                }
            }

            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Builds header lines for the Preamble.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        private static string BuildPreambleText(VariationTree tree)
        {
            StringBuilder sb = new StringBuilder();
            List<string> preamble = tree.Header.GetPreamble();
            foreach (string line in preamble)
            {
                sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_PREAMBLE, line));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Appends a header line to the passed StringBuilder object, unless the value is empty.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private static void AppendHeaderLine(ref StringBuilder sb, string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            else
            {
                sb.AppendLine(PgnHeaders.BuildHeaderLine(key, value));
            }
        }

        /// <summary>
        /// Each invoked instance builds text of a single Line in the tree. 
        /// Calls itself recursively and returns the text of the complete
        /// Variation Tree.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeNumber"></param>
        private static string BuildTreeLineText(TreeNode nd, bool includeNumber = false)
        {
            while (true)
            {
                // if the node has no children,
                // print it and return 
                if (nd.Children.Count == 0)
                {
                    return _fileText.ToString();
                }

                // if the node has 1 child, print it,
                // keep the same lavel and sublevel as the parent
                // call this method on the child
                if (nd.Children.Count == 1)
                {
                    TreeNode child = nd.Children[0];
                    BuildNodeText(child, includeNumber);
                    BuildTreeLineText(child);
                    return _fileText.ToString();
                }

                // if the node has more than 1 child
                // call this method on each sibling except
                // the first one, before calling it on the 
                // first one.
                if (nd.Children.Count > 1)
                {
                    // the first child remains at the same level as the parent
                    BuildNodeText(nd.Children[0], includeNumber);
                    for (int i = 1; i < nd.Children.Count; i++)
                    {
                        // if there is more than 2 children, create a new para,
                        // otherwise just use parenthesis

                        _fileText.Append(" (");
                        BuildNodeText(nd.Children[i], true);
                        BuildTreeLineText(nd.Children[i]);
                        _fileText.Append(") ");
                    }

                    BuildTreeLineText(nd.Children[0], true);
                    return _fileText.ToString();
                }
            }
        }

        /// <summary>
        /// Builds text of an individual node (ply).
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeNumber"></param>
        private static void BuildNodeText(TreeNode nd, bool includeNumber)
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(nd.CommentBeforeMove) && ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.KEEP_COMMENTS))
            {
                sb.Append(" {" + CleanupCommentText(nd.CommentBeforeMove) + "}");
            }

            if (nd.Position.ColorToMove == PieceColor.Black)
            {
                if (!includeNumber && nd.Position.MoveNumber != 1)
                {
                    sb.Append(" ");
                }
                sb.Append(nd.Position.MoveNumber.ToString() + ".");
            }

            if (nd.Position.ColorToMove == PieceColor.White && includeNumber)
            {
                sb.Append(nd.Position.MoveNumber.ToString() + "...");
            }

            sb.Append(" " + nd.LastMoveAlgebraicNotation);
            if (nd.Position.IsCheckmate)
            {
                sb.Append('#');
            }
            else if (nd.Position.IsCheck)
            {
                sb.Append('+');
            }

            sb.Append(nd.Nags);

            sb.Append(BuildCommandAndCommentText(nd));

            _fileText.Append(sb.ToString());
        }

        /// <summary>
        /// Builds PGN text combining the commands and comments.
        /// Chess Forge specific commands are ignored.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="nd"></param>
        /// <returns></returns>
        private static string BuildCommandAndCommentText(TreeNode nd)
        {
            bool keepComments = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.KEEP_COMMENTS);
            bool keepEvaluations = ConfigurationRtfExport.GetBoolValue(ConfigurationRtfExport.KEEP_EVALUATIONS);

            if (keepComments &&
                (!string.IsNullOrEmpty(nd.Comment)
                || !string.IsNullOrEmpty(nd.Arrows)
                || !string.IsNullOrEmpty(nd.Circles))
                || !string.IsNullOrEmpty(nd.EngineEvaluation) && keepEvaluations)
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(" {");

                // Process an Evaluation ChfCommand
                if (keepEvaluations && !string.IsNullOrEmpty(nd.EngineEvaluation))
                {
                    string sCmd = ChfCommands.GetStringForCommand(ChfCommands.Command.ENGINE_EVALUATION_V2) + " " + nd.EngineEvaluation;
                    sb.Append("[" + sCmd + "]");
                }

                if (keepComments)
                {
                    // Process the Arrows string
                    if (!string.IsNullOrEmpty(nd.Arrows))
                    {
                        string sCmd = ChfCommands.GetStringForCommand(ChfCommands.Command.ARROWS) + " " + nd.Arrows;
                        sb.Append("[" + sCmd + "]");
                    }

                    // Process the Circles string
                    if (!string.IsNullOrEmpty(nd.Circles))
                    {
                        string sCmd = ChfCommands.GetStringForCommand(ChfCommands.Command.CIRCLES) + " " + nd.Circles;
                        sb.Append("[" + sCmd + "]");
                    }

                    // Comment, if any
                    if (!string.IsNullOrEmpty(nd.Comment))
                    {
                        // replace '[', '{', '}', and ']'
                        nd.Comment = CleanupCommentText(nd.Comment);
                        sb.Append(nd.Comment);
                    }
                }

                sb.Append("} ");

                return sb.ToString();
            }
            else
            {
                return "";
            }
        }

        private static string CleanupCommentText(string comment)
        {
            if (string.IsNullOrEmpty(comment))
            {
                return comment;
            }
            else
            {
                return comment.Replace('[', '(').Replace(']', ')').Replace('{', '(').Replace('}', ')');
            }
        }

        /// <summary>
        /// Writes the PGN text to a file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="content"></param>
        private static void WriteOutFile(string fileName, string content)
        {
            File.WriteAllText(fileName, content);
        }

    }
}

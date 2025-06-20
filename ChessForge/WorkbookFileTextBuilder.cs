using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChessForge
{
    /// <summary>
    /// Builds the text of the PGN file that will be written out.
    /// </summary>
    public class WorkbookFileTextBuilder
    {
        // keeps output text as it is being built
        private static StringBuilder _fileText;

        // convenience reference to the Workbook
        private static Workbook _workbook;

        /// <summary>
        /// Builds text to write out to the PGN file for the entire Workbook.
        /// The file consists of:
        /// 1. Dummy "game" to indicate that this is a Chess Forge file
        ///    with headers containing the Workbook title, Training Side
        ///    and other attributes if needed.
        /// 2. Chapters, where each chapter incudes:
        ///    a) Mandatory Study Tree
        ///    b) optional Model Games
        ///    c) optional Exercises
        /// </summary>
        /// <returns></returns>
        public static string BuildWorkbookText()
        {
            _workbook = WorkbookManager.SessionWorkbook;

            StringBuilder sbOut = new StringBuilder();
            sbOut.Append(BuildWorkbookPrefaceText());

            _fileText = new StringBuilder();
            for (int i = 0; i < _workbook.Chapters.Count; i++)
            {
                sbOut.Append(BuildChapterText(_workbook.Chapters[i], i + 1));
                _fileText.Clear();
            }

            return sbOut.ToString();
        }

        /// <summary>
        /// Builds a dummy game with Workbook headers,
        /// a comment if applicable and the game body consisting of
        /// the '*' character.
        /// </summary>
        /// <returns></returns>
        private static string BuildWorkbookPrefaceText()
        {
            StringBuilder sb = new StringBuilder();

            // workbook headers
            sb.AppendLine(PgnHeaders.GetWorkbookTitleText(_workbook.Title));
            sb.AppendLine(PgnHeaders.GetAuthorText(_workbook.Author));
            sb.AppendLine(PgnHeaders.GetWorkbookVersionText(_workbook.Version.ToString()));
            sb.AppendLine(PgnHeaders.GetWorkbookGuidText(_workbook.Guid));
            sb.AppendLine(PgnHeaders.GetTrainingSideText(_workbook.TrainingSideConfig));

            sb.AppendLine(PgnHeaders.GetStudyBoardOrientationText(_workbook.StudyBoardOrientationConfig));
            sb.AppendLine(PgnHeaders.GetGameBoardOrientationText(_workbook.GameBoardOrientationConfig));
            sb.AppendLine(PgnHeaders.GetExerciseBoardOrientationText(_workbook.ExerciseBoardOrientationConfig));

            if (_workbook.LastUpdate != null)
            {
                sb.AppendLine(PgnHeaders.GetDateText(_workbook.LastUpdate));
            }
            sb.AppendLine(PgnHeaders.GetWorkbookWhiteText());
            sb.AppendLine(PgnHeaders.GetWorkbookBlackText());
            sb.AppendLine(PgnHeaders.GetLineResultHeader());

            _workbook.Description = Properties.Resources.AboutWorkbook;
            if (!string.IsNullOrWhiteSpace(_workbook.Description))
            {
                sb.AppendLine(BuildCommentText(_workbook.Description));
            }
            sb.AppendLine();
            sb.AppendLine("*");
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Formats comment text.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public static string BuildCommentText(string comment)
        {
            if (string.IsNullOrEmpty(comment))
            {
                return "";
            }
            else
            {
                return "{" + comment + "}";
            }
        }

        /// <summary>
        /// Builds text for the entire chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="chapterIndex"></param>
        /// <returns></returns>
        private static string BuildChapterText(Chapter chapter, int chapterIndex)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(BuildStudyTreeText(chapter));
            sb.Append(BuildIntroText(chapter));
            sb.Append(BuildModelGamesText(chapter));
            sb.Append(BuildExercisesText(chapter));

            sb.AppendLine();
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Builds text for the Study Tree in the chapter.
        /// It must be present in the output as it indicates the start
        /// of a chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static string BuildStudyTreeText(Chapter chapter)
        {
            VariationTree tree = chapter.StudyTree.Tree;

            string headerText = BuildStudyTreeHeaderText(chapter);

            StringBuilder sbOutput = new StringBuilder();
            sbOutput.Append(headerText);

            if (tree != null && tree.Nodes.Count >= 1)
            {
                StringBuilder sb = new StringBuilder();
                TreeNode root = tree.RootNode;
                if (root != null)
                {

                    // There may be a comment or command before the first move. Add if so.
                    _fileText.Append(BuildCommandAndCommentText(root));

                    sb.Append(BuildTreeLineText(root));
                }

                sbOutput.Append(DivideLine(sb.ToString(), 80));
            }

            // add terminating character
            sbOutput.Append(" *");
            sbOutput.AppendLine();
            sbOutput.AppendLine();

            return sbOutput.ToString();
        }

        /// <summary>
        /// Builds text for the Intro section.
        /// Intro is optional so if there is no content, it will be skipped.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        public static string BuildIntroText(Chapter chapter)
        {
            string xaml = chapter.Intro.CodedContent;

            if (!string.IsNullOrEmpty(xaml))
            {
                string headerText = BuildIntroHeaderText(chapter);

                StringBuilder sbOutput = new StringBuilder();

                // textual version of the content first
                string introPlainText = "";
                if (chapter.Intro.Tree.RootNode != null)
                {
                    introPlainText = BuildCommentText(chapter.Intro.Tree.RootNode.Comment) + "\n\n";
                }

                string xamlCmdText = BuildXamlCommandText(chapter.Intro.CodedContent);
                string positionsText = BuildPositionsCommandText(chapter.Intro);

                string introContent = BuildCommentText("[" + xamlCmdText + " " + positionsText + "]");

                sbOutput.Append(headerText + introPlainText + DivideLineForced(introContent, 80));
                sbOutput.AppendLine();

                // add terminating character
                sbOutput.Append(" *");
                sbOutput.AppendLine();
                sbOutput.AppendLine();

                return sbOutput.ToString();
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Build text for all Model Games in the chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="chapterNo"></param>
        /// <returns></returns>
        private static string BuildModelGamesText(Chapter chapter)
        {
            _fileText.Clear();

            StringBuilder sb = new StringBuilder();
            StringBuilder sbOutput = new StringBuilder();

            for (int i = 0; i < chapter.ModelGames.Count; i++)
            {
                try
                {
                    Article article = chapter.ModelGames[i];
                    string headerText = BuildModelGameHeaderText(chapter, article.Tree);

                    TreeNode root = article.Tree.RootNode;
                    if (root != null)
                    {
                        // There may be a comment or command before the first move. Add if so.
                        _fileText.Append(BuildCommandAndCommentText(root));

                        sb.Append(BuildTreeLineText(root));
                    }

                    sbOutput.Append(headerText + DivideLine(sb.ToString(), 80));

                    // add result
                    sbOutput.Append(" " + article.Tree.Header.GetResult(out _));
                    sbOutput.AppendLine();
                    sbOutput.AppendLine();

                    sb.Clear();
                    _fileText.Clear();
                }
                catch (Exception ex)
                {
                    AppLog.Message("BuildModelGamesText() index=" + i.ToString(), ex);
                }
            }

            return sbOutput.ToString();
        }

        /// <summary>
        /// Build text for all Exercises in the chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="chapterNo"></param>
        /// <returns></returns>
        private static string BuildExercisesText(Chapter chapter)
        {
            _fileText.Clear();

            StringBuilder sb = new StringBuilder();
            StringBuilder sbOutput = new StringBuilder();

            for (int i = 0; i < chapter.Exercises.Count; i++)
            {
                Article article = chapter.Exercises[i];

                try
                {
                    string headerText = BuildExercisesHeaderText(chapter, article.Tree);

                    TreeNode root = article.Tree.RootNode;
                    if (root != null)
                    {
                        // There may be a comment or command before the first move. Add if so.
                        _fileText.Append(BuildCommandAndCommentText(root));

                        sb.Append(BuildTreeLineText(root));
                    }

                    sbOutput.Append(headerText + DivideLine(sb.ToString(), 80));

                    // add result
                    sbOutput.Append(" " + article.Tree.Header.GetResult(out _));
                    sbOutput.AppendLine();
                    sbOutput.AppendLine();

                    sb.Clear();
                    _fileText.Clear();
                }
                catch (Exception ex)
                {
                    AppLog.Message("BuildExercisesText() index=" + i.ToString(), ex);
                }
            }

            return sbOutput.ToString();
        }

        /// <summary>
        /// Builds header for the Intro.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        public static string BuildIntroHeaderText(Chapter chapter)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_GUID, chapter.Guid));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_CONTENT_TYPE, PgnHeaders.VALUE_INTRO));

            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_WHITE, "Chapter"));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_BLACK, "Introduction"));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_RESULT, "*"));
            sb.AppendLine("");

            return sb.ToString();
        }

        /// <summary>
        /// Builds the header for a Study Tree.
        /// </summary>
        /// <param name="chapter"></param>
        /// <returns></returns>
        private static string BuildStudyTreeHeaderText(Chapter chapter)
        {
            StringBuilder sb = new StringBuilder();

            string key;
            string value;

            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_CHAPTER_TITLE, chapter.GetTitle()));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_GUID, chapter.Guid));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_ANNOTATOR, chapter.GetAuthor()));

            VariationTree tree = chapter.StudyTree.Tree;

            value = tree.Header.GetContentTypeString(out key, PgnHeaders.VALUE_STUDY_TREE);
            sb.AppendLine(PgnHeaders.BuildHeaderLine(key, value));

            value = tree.Header.GetWhitePlayer(out key, "CHAPTER");
            sb.AppendLine(PgnHeaders.BuildHeaderLine(key, value));

            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_BLACK, chapter.GetTitle()));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_INDEX_DEPTH, chapter.VariationIndexDepth == null ? "" : chapter.VariationIndexDepth.Value.ToString()));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_RESULT, "*"));
            sb.AppendLine(BuildPreamble(tree));

            sb.AppendLine("");

            return sb.ToString();
        }

        /// <summary>
        /// Builds the header for a Model Game.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="tree"></param>
        /// <returns></returns>
        private static string BuildModelGameHeaderText(Chapter chapter, VariationTree tree)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_CONTENT_TYPE, PgnHeaders.VALUE_MODEL_GAME));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_EVENT, tree.Header.GetEventName(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_ROUND, tree.Header.GetRound(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_ECO, tree.Header.GetECO(out _)));
            AppendHeaderLine(ref sb, PgnHeaders.KEY_LICHESS_ID, tree.Header.GetLichessId(out _));
            AppendHeaderLine(ref sb, PgnHeaders.KEY_CHESSCOM_ID, tree.Header.GetChessComId(out _));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_GUID, tree.Header.GetGuid(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_DATE, tree.Header.GetDate(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_WHITE, tree.Header.GetWhitePlayer(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_BLACK, tree.Header.GetBlackPlayer(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_WHITE_ELO, tree.Header.GetWhitePlayerElo(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_BLACK_ELO, tree.Header.GetBlackPlayerElo(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_ANNOTATOR, tree.Header.GetAnnotator(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_RESULT, tree.Header.GetResult(out _)));
            sb.AppendLine(BuildPreamble(tree));

            sb.AppendLine();

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
        /// Builds the header for an Exercise.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="chapterNo"></param>
        /// <returns></returns>
        private static string BuildExercisesHeaderText(Chapter chapter, VariationTree tree)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_CONTENT_TYPE, PgnHeaders.VALUE_EXERCISE));

            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_EVENT, tree.Header.GetEventName(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_ROUND, tree.Header.GetRound(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_ECO, tree.Header.GetECO(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_GUID, tree.Header.GetGuid(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_DATE, tree.Header.GetDate(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_WHITE, tree.Header.GetWhitePlayer(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_BLACK, tree.Header.GetBlackPlayer(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_WHITE_ELO, tree.Header.GetWhitePlayerElo(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_BLACK_ELO, tree.Header.GetBlackPlayerElo(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_ANNOTATOR, tree.Header.GetAnnotator(out _)));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_RESULT, tree.Header.GetResult(out _)));

            // FEN, if required, must be last for compatibility with ChesBase.
            // Here though, seems that we can put it in front of Preamble which is safer.
            if (tree.RootNode != null)
            {
                BoardPosition pos = new BoardPosition(tree.Nodes[0].Position);
                UpShiftOnePly(ref pos);

                string fen = FenParser.GenerateFenFromPosition(pos, tree.MoveNumberOffset);
                sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.KEY_FEN_STRING, fen));
            }
            sb.AppendLine(BuildPreamble(tree));

            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// Builds header lines for the Preamble.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        private static string BuildPreamble(VariationTree tree)
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
        /// Command text for xaml text.
        /// </summary>
        /// <param name="xaml"></param>
        /// <returns></returns>
        private static string BuildXamlCommandText(string xaml)
        {
            return ChfCommands.GetStringForCommand(ChfCommands.Command.XAML) + " " + xaml;
        }

        /// <summary>
        /// Builds command text for positions in the Intro
        /// </summary>
        /// <param name="intro"></param>
        /// <returns></returns>
        private static string BuildPositionsCommandText(Article intro)
        {
            StringBuilder sb = new StringBuilder();
            foreach (TreeNode node in intro.Tree.Nodes)
            {
                sb.Append(BuildPositionCommandText(node));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Builds command text for a single position.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private static string BuildPositionCommandText(TreeNode nd)
        {
            if (nd != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(ChfCommands.XAML_NODE_ID + "=" + nd.NodeId.ToString() + " ");
                sb.Append(ChfCommands.XAML_MOVE_TEXT + "=" + EncodingUtils.Base64Encode(nd.LastMoveAlgebraicNotation) + " ");

                string fen = EncodingUtils.Base64Encode(FenParser.GenerateFenFromPosition(nd.Position));
                sb.Append(ChfCommands.XAML_FEN + "=" + fen + " ");

                if (!string.IsNullOrEmpty(nd.Arrows))
                {
                    sb.Append(ChfCommands.XAML_ARROWS + "=" + nd.Arrows + " ");
                }

                if (!string.IsNullOrEmpty(nd.Circles))
                {
                    sb.Append(ChfCommands.XAML_CIRCLES + "=" + nd.Circles + " ");
                }

                if (nd.IsDiagram)
                {
                    sb.Append(ChfCommands.XAML_IS_DIAGRAM + "=1" + " ");
                }

                if (nd.IsDiagramFlipped)
                {
                    sb.Append(ChfCommands.XAML_IS_DIAGRAM_FLIPPED + "=1" + " ");
                }

                return sb.ToString();
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Shifts the move number up if White is on move.
        /// This is required when saving the position as FEN.
        /// </summary>
        /// <param name="pos"></param>
        private static void UpShiftOnePly(ref BoardPosition pos)
        {
            if (pos.ColorToMove == PieceColor.White)
            {
                pos.MoveNumber += 1;
            }
        }

        /// <summary>
        /// Divides a line into multiple lines no longer than maxChars
        /// </summary>
        /// <param name="inp"></param>
        /// <param name="maxChars"></param>
        /// <returns></returns>
        public static string DivideLine(string inp, int maxChars)
        {
            StringBuilder sb = new StringBuilder();
            int startIdx = 0;
            int lastSpaceIdx;

            // loop subline by subline
            while (true)
            {
                string nextLine = "";

                // is this the last subline
                if (inp.Length <= startIdx + maxChars)
                {
                    nextLine = inp.Substring(startIdx);
                    sb.Append(nextLine);
                    break;
                }

                // If we don't want to to introduce spurious \r\n characters in the comment
                // we need to extend the line if the using maxchar would break the comment up.
                // Check if we need to adjust maxchar.
                int adjustedMaxChars = -1;
                int commentStart = inp.LastIndexOf('{', Math.Min(startIdx + maxChars, inp.Length - 1), maxChars);
                if (commentStart > 0)
                {
                    // there is a comment starting in the candidate substring, check if there is a matching end
                    int commentEnd = inp.LastIndexOf('}', Math.Min(startIdx + maxChars, inp.Length - 1), maxChars);
                    if (commentEnd < commentStart)
                    {
                        // there was no comment end following the start, so extend the candidate substring,
                        // find where the comment ends
                        int nextCommentEnd = inp.IndexOf('}', commentStart);
                        // +2 in order to capture the space that always follows '}'
                        adjustedMaxChars = (nextCommentEnd - startIdx) + 2;
                    }
                }

                adjustedMaxChars = Math.Max(maxChars, adjustedMaxChars);

                // find the last space before the maxChars limit
                //lastSpaceIdx = inp.LastIndexOf(' ', Math.Min(startIdx + maxChars, inp.Length - 1), maxChars);
                lastSpaceIdx = inp.LastIndexOf(' ', Math.Min(startIdx + adjustedMaxChars, inp.Length - 1), adjustedMaxChars);

                if (lastSpaceIdx == -1)
                {
                    // no more spaces so save and exit
                    nextLine = inp.Substring(startIdx);
                    sb.Append(nextLine);
                    break;
                }
                else if (lastSpaceIdx - startIdx == 0)
                {
                    // advance 1 char to avoid getting stuck on a leading space
                    lastSpaceIdx++; // becomes startIdx at the bottom of this loop which is what we need.
                }
                else
                {
                    // all normal, get the subline
                    nextLine = inp.Substring(startIdx, (lastSpaceIdx - startIdx) + 1);
                }

                if (nextLine.Length > 0) // skip the leading space case
                {
                    sb.Append(nextLine);
                    sb.AppendLine();
                }

                startIdx = lastSpaceIdx + 1;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Breaks a line into sublines of length on greater thanmaxCjars
        /// </summary>
        /// <param name="inp"></param>
        /// <param name="maxChars"></param>
        /// <returns></returns>
        private static string DivideLineForced(string inp, int maxChars)
        {
            StringBuilder sb = new StringBuilder();
            int startIdx = 0;

            while (true)
            {
                string nextLine;

                // is this the last subline
                if (inp.Length <= startIdx + maxChars)
                {
                    nextLine = inp.Substring(startIdx);
                    sb.Append(nextLine);
                    break;
                }
                else
                {
                    nextLine = inp.Substring(startIdx, maxChars);

                    sb.Append(nextLine);
                    sb.AppendLine();
                    startIdx += maxChars;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Each invoked instance builds text of a single Line in the Workbook. 
        /// Calls itself recursively and returns the text of the complete
        /// Workbook.
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
            nd.TextStart = _fileText.Length;

            StringBuilder sb = new StringBuilder();
            if (nd.Position.ColorToMove == PieceColor.Black)
            {
                if (!includeNumber && nd.Position.MoveNumber != 1)
                {
                    sb.Append(" ");
                }
                sb.Append(nd.Position.MoveNumber.ToString() + ".");
                if (includeNumber)
                    nd.TextStart--;
            }

            if (nd.Position.ColorToMove == PieceColor.White && includeNumber)
            {
                sb.Append(nd.Position.MoveNumber.ToString() + "...");
                nd.TextStart--;
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

            nd.TextEnd = _fileText.Length - 1;
        }

        /// <summary>
        /// Builds text for the comment and ChessForge commands if any
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        private static string BuildCommandAndCommentText(TreeNode nd)
        {
            if (nd.IsBookmark
                || nd.IsThumbnail
                || nd.IsDiagram
                || !string.IsNullOrEmpty(nd.References)
                || !string.IsNullOrEmpty(nd.Comment)
                || !string.IsNullOrEmpty(nd.CommentBeforeMove)
                || !string.IsNullOrEmpty(nd.EngineEvaluation)
                || nd.QuizPoints != 0
                || !string.IsNullOrEmpty(nd.Arrows)
                || !string.IsNullOrEmpty(nd.Circles)
                || nd.UnprocessedChfCommands.Count > 0)
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(" {");

                // Process a Bookmark ChfCommand
                if (nd.IsBookmark)
                {
                    string sCmd = ChfCommands.GetStringForCommand(ChfCommands.Command.BOOKMARK_V2);
                    sb.Append("[" + sCmd + "]");
                }

                // Process a Thumbnail command
                if (nd.IsThumbnail)
                {
                    string sCmd = ChfCommands.GetStringForCommand(ChfCommands.Command.THUMBNAIL);
                    sb.Append("[" + sCmd + "]");
                }

                // Process a Diagram command
                if (nd.IsDiagram)
                {
                    string sCmd = ChfCommands.GetStringForCommand(ChfCommands.Command.DIAGRAM) + " " 
                        + ChfCommands.CodeDiagramAttrs(nd.IsDiagramFlipped, nd.IsDiagramPreComment, nd.IsDiagramBeforeMove).ToString();
                    sb.Append("[" + sCmd + "]");
                }

                // Process an Article References command
                if (!string.IsNullOrEmpty(nd.References))
                {
                    string sCmd = ChfCommands.GetStringForCommand(ChfCommands.Command.ARTICLE_REFS) + " " + nd.References;
                    sb.Append("[" + sCmd + "]");
                }

                // Process an Evaluation ChfCommand
                if (!string.IsNullOrEmpty(nd.EngineEvaluation))
                {
                    string sCmd = ChfCommands.GetStringForCommand(ChfCommands.Command.ENGINE_EVALUATION_V2) + " " + nd.EngineEvaluation;
                    sb.Append("[" + sCmd + "]");
                }

                // Process the Quiz command
                if (nd.QuizPoints != 0)
                {
                    string sCmd = ChfCommands.GetStringForCommand(ChfCommands.Command.QUIZ_POINTS) + " " + nd.QuizPoints;
                    sb.Append("[" + sCmd + "]");
                }

                // Process the Assessment command
                if (nd.Assessment != 0)
                {
                    string sCmd = ChfCommands.GetStringForCommand(ChfCommands.Command.ASSESSMENT) + " " + nd.Assessment;
                    sb.Append("[" + sCmd + "]");
                }

                // Process the BestResponse command
                if (!string.IsNullOrEmpty(nd.BestResponse))
                {
                    string sCmd = ChfCommands.GetStringForCommand(ChfCommands.Command.BEST_RESPONSE) + " " + nd.BestResponse;
                    sb.Append("[" + sCmd + "]");
                }

                // Process the CommentBeforeMove
                if (!string.IsNullOrEmpty(nd.CommentBeforeMove))
                {
                    string sCmd = ChfCommands.GetStringForCommand(ChfCommands.Command.COMMENT_BEFORE_MOVE) + " " + nd.CommentBeforeMove;
                    // replace '[' and ']'
                    sCmd = sCmd.Replace('[', '(').Replace(']', ')');
                    sb.Append("[" + sCmd + "]");
                }

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

                // Write out commands that we did not recognize but do not want to lose.
                // E.g. we may be running an earlier version of Chess Forge.
                foreach (string cmd in nd.UnprocessedChfCommands)
                {
                    sb.Append("[" + cmd + "]");
                }

                // Comment, if any
                if (!string.IsNullOrEmpty(nd.Comment))
                {
                    // replace '[', '{', '}', and ']'
                    nd.Comment = nd.Comment.Replace('[', '(').Replace(']', ')').Replace('{','(').Replace('}', ')');
                    sb.Append(nd.Comment);
                }

                sb.Append("} ");

                return sb.ToString();
            }
            else
            {
                return "";
            }
        }
    }
}

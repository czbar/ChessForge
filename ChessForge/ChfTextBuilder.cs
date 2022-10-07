using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using GameTree;
using ChessPosition;
using System.Windows.Controls;
using ChessPosition.GameTree;

namespace ChessForge
{
    /// <summary>
    /// Builds the text of the CHF file that will be written out.
    /// </summary>
    public class ChfTextBuilder
    {
        // keeps output text as it is being built
        private static StringBuilder _fileText;

        // convenience reference to the Workbook
        private static VariationTree _workbook;

        // convenience reference to the Workbook
        private static Workbook _wb;

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
            _wb = WorkbookManager.SessionWorkbook;

            StringBuilder sbOut = new StringBuilder();
            sbOut.Append(BuildWorkbookPrefaceText());

            _fileText = new StringBuilder();
            for (int i = 0; i < _wb.Chapters.Count; i++)
            {
                sbOut.Append(BuildChapterText(_wb.Chapters[i], i + 1));
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
            sb.AppendLine(PgnHeaders.GetWorkbookTitleText(_wb.Title));
            sb.AppendLine(PgnHeaders.GetTrainingSideText(_wb.TrainingSide));
            if (_wb.LastUpdate != null)
            {
                sb.AppendLine(PgnHeaders.GetDateText(_wb.LastUpdate));
            }
            sb.AppendLine(PgnHeaders.GetWorkbookWhiteText());
            sb.AppendLine(PgnHeaders.GetWorkbookBlackText());
            sb.AppendLine(PgnHeaders.GetLineResultHeader());

            if (!string.IsNullOrWhiteSpace(_wb.Description))
            {
                sb.AppendLine(BuildCommentText(_wb.Description));
            }
            sb.AppendLine();
            sb.AppendLine("*");
            sb.AppendLine();

            return sb.ToString();
        }

        private static string BuildCommentText(string comment)
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
        /// <param name="chapterNo"></param>
        /// <returns></returns>
        private static string BuildChapterText(Chapter chapter, int chapterNo)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(BuildStudyTreeText(chapter, chapterNo));
            sb.Append(BuildModelGamesText(chapter, chapterNo));
            sb.Append(BuildExercisesText(chapter, chapterNo));

            sb.AppendLine();
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Builds text for the Study Tree in the chapter.
        /// A study tree is optional in a chapter.
        /// If it is not initialized or only contains Node 0,
        /// it will not be included in the output at all.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="chapterNo"></param>
        /// <returns></returns>
        private static string BuildStudyTreeText(Chapter chapter, int chapterNo)
        {
            VariationTree tree = chapter.StudyTree;
            if (tree != null && tree.Nodes.Count >= 1)
            {
                string headerText = BuildStudyTreeHeaderText(chapter, chapterNo);

                TreeNode root = tree.Nodes[0];

                // There may be a comment or command before the first move. Add if so.
                _fileText.Append(BuildCommandAndCommentText(root));

                StringBuilder sb = new StringBuilder();
                sb.Append(BuildTreeLineText(root));

                StringBuilder sbOutput = new StringBuilder();
                sbOutput.Append(headerText + DivideLine(sb.ToString(), 80));

                // add terminating character
                sbOutput.Append(" *");
                sbOutput.AppendLine();
                return sbOutput.ToString();
            }
            else
            {
                return "";
            }
        }

        private static string BuildStudyTreeHeaderText(Chapter chapter, int chapterNo)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(BuildCommonGameHeaderText(chapter, chapterNo));
            
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.NAME_CONTENT_TYPE, PgnHeaders.VALUE_STUDY_TREE));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.NAME_WHITE, "Chess Forge"));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.NAME_BLACK, "Study Tree"));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.NAME_RESULT, "*"));
            sb.AppendLine("");

            return sb.ToString();
        }

        /// <summary>
        /// Build text for all Model Games in the chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="chapterNo"></param>
        /// <returns></returns>
        private static string BuildModelGamesText(Chapter chapter, int chapterNo)
        {
            StringBuilder sb = new StringBuilder();


            return sb.ToString();
        }

        private static string BuildModelGameText(Chapter chapter, int chapterNo, GameMetadata gm)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(BuildCommonGameHeaderText(chapter, chapterNo));

            return sb.ToString();
        }

        private static string BuildModelGameHeaderText(Chapter chapter, int chapterNo)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(BuildCommonGameHeaderText(chapter, chapterNo));

            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.NAME_CONTENT_TYPE, PgnHeaders.VALUE_STUDY_TREE));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.NAME_WHITE, "Chess Forge"));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.NAME_BLACK, "Study Tree"));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.NAME_RESULT, "*"));

            return sb.ToString();
        }


        /// <summary>
        /// Builds text for all the exercises in the chapter.
        /// </summary>
        /// <param name="chapter"></param>
        /// <param name="chapterNo"></param>
        /// <returns></returns>
        private static string BuildExercisesText(Chapter chapter, int chapterNo)
        {
            StringBuilder sb = new StringBuilder();

            return sb.ToString();
        }


        private static string BuildCommonGameHeaderText(Chapter chapter, int chapterNo)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.NAME_CHAPTER_ID, chapterNo.ToString()));
            sb.AppendLine(PgnHeaders.BuildHeaderLine(PgnHeaders.NAME_CHAPTER_TITLE, chapter.Title));

            return sb.ToString();
        }


        /// <summary>
        /// Builds text of the complete Workbook.
        /// </summary>
        public static string BuildText(VariationTree workbook)
        {
            _workbook = workbook;
            _fileText = new StringBuilder();

            BuildHeaders();

            StringBuilder sbOutput = new StringBuilder(_fileText.ToString());

            _fileText.Clear();
            if (workbook.Nodes.Count > 0)
            {
                TreeNode root = workbook.Nodes[0];

                // There may be a comment or command before the first move. Add if so.
                _fileText.Append(BuildCommandAndCommentText(root));

                BuildTreeLineText(root);
            }

            sbOutput.Append(DivideLine(_fileText.ToString(), 80));

            // add terminating character
            sbOutput.Append(" *");
            sbOutput.AppendLine();

            return sbOutput.ToString();
        }

        /// <summary>
        /// Divides a line into multiple lines no longer than maxChars
        /// </summary>
        /// <param name="inp"></param>
        /// <param name="maxChars"></param>
        /// <returns></returns>
        private static string DivideLine(string inp, int maxChars)
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

                // find the last space before the maxChars limit
                lastSpaceIdx = inp.LastIndexOf(' ', Math.Min(startIdx + maxChars, inp.Length - 1), maxChars);

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
        /// Build text for the headers
        /// </summary>
        private static void BuildHeaders()
        {
            BuildHeader(_workbook.HEADER_TITLE);
            BuildHeader(_workbook.HEADER_DATE, DateTime.Now.ToString("yyyy.MM.dd"));
            BuildHeader(_workbook.HEADER_TRAINING_SIDE);
            BuildHeader(_workbook.HEADER_WHITE, "Chess Forge");
            BuildHeader(_workbook.HEADER_BLACK, "Workbook File");
            BuildHeader(_workbook.HEADER_RESULT, "*");
            _fileText.AppendLine();
        }

        /// <summary>
        /// Build text for a single header.
        /// </summary>
        /// <param name="key"></param>
        private static void BuildHeader(string key, string value = null)
        {
            string val;
            if (value == null)
            {
                _workbook.Headers.TryGetValue(key, out val);
            }
            else
            {
                val = value;
            }
            _fileText.Append("[" + key + " \"");
            _fileText.Append(val ?? "");
            _fileText.Append("\"]");
            _fileText.AppendLine();
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
                || !string.IsNullOrEmpty(nd.Comment)
                || !string.IsNullOrEmpty(nd.EngineEvaluation)
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

                // Process an Evaluation ChfCommand
                if (!string.IsNullOrEmpty(nd.EngineEvaluation))
                {
                    string sCmd = ChfCommands.GetStringForCommand(ChfCommands.Command.ENGINE_EVALUATION_V2) + " " + nd.EngineEvaluation;
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
                // E.g. we may ne running an earlier version of Chess Forge.
                foreach (string cmd in nd.UnprocessedChfCommands)
                {
                    sb.Append("[" + cmd + "]");
                }

                // Comment, if any
                if (!string.IsNullOrEmpty(nd.Comment))
                {
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

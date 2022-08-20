using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using GameTree;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Builds the text of the CHF file that will be written out.
    /// </summary>
    public class ChfTextBuilder
    {
        private static StringBuilder _fileText;

        private static WorkbookTree _workbook;

        /// <summary>
        /// Builds text of the complete Workbook.
        /// </summary>
        public static string BuildText(WorkbookTree workbook)
        {
            _workbook = workbook;
            _fileText = new StringBuilder();

            BuildHeaders();

            StringBuilder sbOutput = new StringBuilder(_fileText.ToString());

            _fileText.Clear();
            if (workbook.Nodes.Count > 0)
            {
                TreeNode root = workbook.Nodes[0];
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
                lastSpaceIdx = inp.LastIndexOf(' ', Math.Min(startIdx + maxChars, inp.Length -1), maxChars);

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
            BuildHeader(_workbook.HEADER_TRAINING_SIDE);
            BuildHeader(_workbook.HEADER_WHITE, "Chess Forge");
            BuildHeader(_workbook.HEADER_BLACK, "Workbook File");
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
        private static void BuildTreeLineText(TreeNode nd, bool includeNumber = false)
        {
            while (true)
            {
                // if the node has no children,
                // print it and return 
                if (nd.Children.Count == 0)
                {
                    return;
                }

                // if the node has 1 child, print it,
                // keep the same lavel and sublevel as the parent
                // call this method on the child
                if (nd.Children.Count == 1)
                {
                    TreeNode child = nd.Children[0];
                    BuildNodeText(child, includeNumber);
                    BuildTreeLineText(child);
                    return;
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
                    return;
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
            sb.Append(nd.Nags);

            if (nd.IsBookmark || !string.IsNullOrEmpty(nd.Comment) || !string.IsNullOrEmpty(nd.EngineEvaluation) || nd.UnprocessedChfCommands.Count > 0)
            {
                sb.Append(" {");

                if (nd.IsBookmark)
                {
                    string sCmd = ChfCommands.GetStringForCommand(ChfCommands.Command.BOOKMARK);
                    sb.Append("[" + sCmd + "]");
                }

                if (!string.IsNullOrEmpty(nd.EngineEvaluation))
                {
                    string sCmd = ChfCommands.GetStringForCommand(ChfCommands.Command.ENGINE_EVALUATION) + " " + nd.EngineEvaluation;
                    sb.Append("[" + sCmd + "]");
                }

                foreach (string cmd in nd.UnprocessedChfCommands)
                {
                    sb.Append("[" + cmd + "]");
                }

                if (!string.IsNullOrEmpty(nd.Comment))
                {
                    sb.Append(nd.Comment);
                }

                sb.Append("} ");
            }

            _fileText.Append(sb.ToString());

            nd.TextEnd = _fileText.Length - 1;
        }
    }
}

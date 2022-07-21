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

            if (workbook.Nodes.Count > 0)
            {
                TreeNode root = workbook.Nodes[0];
                BuildTreeLineText(root);
            }

            // add terminating character
            _fileText.Append(" *");
            _fileText.AppendLine();

            return _fileText.ToString();
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

            if (nd.IsBookmark || !string.IsNullOrEmpty(nd.Comment) || nd.UnprocessedChfCommands.Count > 0)
            {
                sb.Append(" {");

                if (nd.IsBookmark)
                {
                    string sCmd = CfhCommands.GetStringForCommand(CfhCommands.Command.BOOKMARK);
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

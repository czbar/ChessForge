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
    /// Manages the text of the CHF file that will be writtent out.
    /// </summary>
    public class ChfFileBuilder
    {
        private StringBuilder _fileText;

        /// <summary>
        /// Builds text of the complete Workbook.
        /// </summary>
        public void BuildTreeText()
        {
            _fileText = new StringBuilder();

            WorkbookTree workbook = AppState.MainWin.Workbook;
            if (workbook.Nodes.Count == 0)
            {
                return;
            }

            TreeNode root = workbook.Nodes[0];
            BuildTreeLineText(root);
        }

        /// <summary>
        /// Each invoked instance builds text of a single Line in the Workbook. 
        /// Calls itself recursively and returns the text of the complete
        /// Workbook.
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeNumber"></param>
        private void BuildTreeLineText(TreeNode nd, bool includeNumber = false)
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
                        // ViewLevel increases, ViewSubLevel resets

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
        private void BuildNodeText(TreeNode nd, bool includeNumber)
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
            _fileText.Append(sb.ToString());

            nd.TextEnd = _fileText.Length - 1;
        }
    }
}

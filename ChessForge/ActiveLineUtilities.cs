using GameTree;
using ChessPosition;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChessForge
{
    public class ActiveLineUtilities
    {
        /// <summary>
        /// Gets the next line to switch to.
        /// First looks for a fork following the passed node and gets
        /// the previous/next node at it, based on the current active line.
        /// If no prev/next line is available, repeats the process looking
        /// for the nearest fork back.
        /// </summary>
        /// <param name="currNode"></param>
        /// <param name="prevNext"></param>
        /// <returns></returns>
        public static TreeNode GetNextLineNode(TreeNode currNode, bool prevNext)
        {
            TreeNode nd = null;

            nd = FindNextLineNode(currNode, prevNext, false);
            if (nd == null && prevNext)
            {
                nd = FindNextLineNode(currNode, prevNext, true);
            }

            return nd;
        }

        /// <summary>
        /// Finds next line from the nearest fork at or after the current node.
        /// </summary>
        /// <param name="currNode"></param>
        /// <param name="prevNext"></param>
        /// <returns></returns>
        private static TreeNode FindNextLineNode(TreeNode currNode, bool prevNext, bool backForth)
        {
            TreeNode nd = null;

            try
            {
                int currNodeIndex = AppState.MainWin.ActiveLine.GetIndexForNode(currNode);

                if (currNodeIndex > 0)
                {
                    // find the next fork going forward
                    int idx = FindNextForkIndex(currNodeIndex, backForth);
                    if (idx > 0)
                    {
                        TreeNode activeChild = AppState.MainWin.ActiveLine.GetNodeAtIndex(idx + 1);
                        TreeNode sibNode = TreeUtils.GetNextSibling(activeChild, prevNext, false);
                        if (sibNode != null && sibNode != activeChild)
                        {
                            nd = sibNode;
                        }
                    }
                }
            }
            catch { }

            return nd;
        }

        /// <summary>
        /// Find previous/next fork in the ActiveLine
        /// including the starting node.
        /// </summary>
        /// <param name="currIndex"></param>
        /// <param name="backForth"></param>
        /// <returns></returns>
        private static int FindNextForkIndex(int currIndex, bool backForth)
        {
            int forkIndex = -1;

            try
            {
                if (backForth)
                {
                    for (int i = currIndex; i > 0; i--)
                    {
                        if (AppState.MainWin.ActiveLine.GetNodeAtIndex(i).Children.Count > 1)
                        {
                            forkIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = currIndex; i < AppState.MainWin.ActiveLine.GetNodeCount(); i++)
                    {
                        if (AppState.MainWin.ActiveLine.GetNodeAtIndex(i).Children.Count > 1)
                        {
                            forkIndex = i;
                            break;
                        }
                    }
                }
            }
            catch { }

            return forkIndex;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameTree;

namespace ChessPosition
{
    public class TextUtils
    {
        /// <summary>
        /// Builds text for variation line passed as a list
        /// of nodes. Depending on the fromIndex and toIndex arguments,
        /// an entire list or only a part of it will be included.
        /// </summary>
        /// <param name="line">The variation line to process</param>
        /// <param name="withNAG">Whether to include NAG symbols</param>
        /// <param name="fromIndex">The index to start from.</param>
        /// <param name="toIndex">The index of the last included ply.  
        /// If -1, the whole line starting as fromIndex will be included.</param>
        /// <returns></returns>
        public static string BuildTextForLine(List<TreeNode> line, bool withNAG = false, int fromIndex = 0, int toIndex = -1)
        {
            StringBuilder sb = new StringBuilder();

            if (toIndex < 0 || toIndex >= line.Count - 1)
            {
                toIndex = line.Count - 1;
            }

            bool isFirstPly = true;
            for (int i = fromIndex; i <= toIndex; i++)
            {
                TreeNode nd = line[i];

                // if NodeId is 0 this the starting position Node and we must not process it
                if (nd.NodeId != 0)
                {
                    if (nd.Position.ColorToMove == PieceColor.Black)
                    {
                        if (nd.Position.MoveNumber != 1)
                        {
                            sb.Append(" ");
                        }
                        sb.Append(nd.Position.MoveNumber.ToString() + ".");
                    }
                    else if (isFirstPly)
                    {
                        sb.Append(nd.Position.MoveNumber.ToString() + "...");
                    }

                    isFirstPly = false;

                    if (withNAG)
                    {
                        sb.Append(" " + nd.LastMoveAlgebraicNotationWithNag);
                    }
                    else
                    {
                        sb.Append(" " + nd.LastMoveAlgebraicNotation);
                    }
                }
            }

            return sb.ToString();
        }
    }
}

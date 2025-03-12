using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    public class SearchPosition
    {
        /// <summary>
        /// Finds nodes featuring the same Position as the passed node.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="node"></param>
        /// <param name="checkSideToMove"></param>
        /// <param name="checkEnpassant"></param>
        /// <param name="checkCastleRights"></param>
        /// <returns></returns>
        public static List<TreeNode> FindIdenticalNodes(VariationTree tree, TreeNode node, bool checkDynamic)
        {
            return FindNodesWithPosition(tree, node.Position, checkDynamic, checkDynamic, checkDynamic);
        }

        /// <summary>
        /// Finds nodes featuring the passed Position.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="refBoard"></param>
        /// <param name="checkSideToMove"></param>
        /// <param name="checkEnpassant"></param>
        /// <param name="checkCastleRights"></param>
        /// <returns></returns>
        public static List<TreeNode> FindNodesWithPosition(VariationTree tree, BoardPosition refBoard, bool checkSideToMove, bool checkEnpassant, bool checkCastleRights)
        {
            List<TreeNode> nodeList = new List<TreeNode>();

            foreach (TreeNode nd in tree.Nodes)
            {
                if (refBoard.Board.Cast<byte>().SequenceEqual(nd.Position.Board.Cast<byte>()))
                {
                    if ((!checkEnpassant || IsSameEnpassantPossibilities(refBoard, nd.Position))
                        && (!checkSideToMove || refBoard.ColorToMove == nd.Position.ColorToMove)
                        && (!checkCastleRights || refBoard.CastlingRights == nd.Position.CastlingRights))
                    {
                        if (nodeList == null)
                        {
                            nodeList = new List<TreeNode>();
                        }
                        nodeList.Add(nd);
                    }
                }
            }

            return nodeList;
        }

        /// <summary>
        /// Checks if possible enpassant moves are same in both positions. 
        /// </summary>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        /// <returns></returns>
        private static bool IsSameEnpassantPossibilities(BoardPosition pos1, BoardPosition pos2)
        {
            if (pos1.EnPassantSquare == pos2.EnPassantSquare)
            {
                return true;
            }

            int epCount1 = PossibleEnpassantCapturesCount(pos1);
            int epCount2 = PossibleEnpassantCapturesCount(pos2);

            // since we know the enpassant squares are different, we will true only if there are 0 pawns available to perform the capture
            return epCount1 == 0 && epCount2 == 0;
        }

        /// <summary>
        /// How many pawns are there to take advantage of enpassant.
        /// The result will be between 0 and 2.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="epSquare"></param>
        /// <returns></returns>
        private static int PossibleEnpassantCapturesCount(BoardPosition pos)
        {
            if (pos.EnPassantSquare == 0)
            {
                return 0;
            }

            int xPos = pos.EnPassantSquare >> 4;
            int yPos = pos.EnPassantSquare & 0x0F;

            int count = 0;

            int yIncrement = pos.ColorToMove == PieceColor.White ? -1 : 1;
            if (xPos - 1 >= 0)
            {
                if (PositionUtils.GetPieceType(pos.Board[xPos - 1, yPos + yIncrement]) == PieceType.Pawn
                  && PositionUtils.GetPieceColor(pos.Board[xPos - 1, yPos + yIncrement]) == pos.ColorToMove)
                {
                    count++;
                }
            }

            if (xPos + 1 <= 7)
            {
                if (PositionUtils.GetPieceType(pos.Board[xPos + 1, yPos + yIncrement]) == PieceType.Pawn
                  && PositionUtils.GetPieceColor(pos.Board[xPos + 1, yPos + yIncrement]) == pos.ColorToMove)
                {
                    count++;
                }
            }

            return count;
        }

    }
}

using ChessPosition;
using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessForge
{
    public class SearchPosition
    {
        /// <summary>
        /// The last searched position.
        /// </summary>
        public static BoardPosition LastSearchPosition = null;

        /// <summary>
        /// Finds nodes featuring the same Position as the passed node.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="node"></param>
        /// <param name="checkSideToMove"></param>
        /// <param name="checkEnpassant"></param>
        /// <param name="checkCastleRights"></param>
        /// <returns></returns>
        public static List<TreeNode> FindIdenticalNodes(VariationTree tree, SearchPositionCriteria crits)
        {
            if (crits.IsPartialSearch)
            {
                return FilterPositions(tree, crits);
            }
            else
            {
                return FindNodesWithPosition(tree, crits);
            }
        }

        /// <summary>
        /// Finds nodes featuring the passed Position.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="crits"></param>
        /// <returns></returns>
        public static List<TreeNode> FindNodesWithPosition(VariationTree tree, SearchPositionCriteria crits)
        {
            List<TreeNode> nodeList = new List<TreeNode>();

            foreach (TreeNode nd in tree.Nodes)
            {
                if (crits.SearchPosition.Board.Cast<byte>().SequenceEqual(nd.Position.Board.Cast<byte>()))
                {
                    if ((!crits.CheckEnpassant || IsSameEnpassantPossibilities(crits.SearchPosition, nd.Position))
                        && (!crits.CheckSideToMove || crits.SearchPosition.ColorToMove == nd.Position.ColorToMove)
                        && (!crits.CheckCastleRights || crits.SearchPosition.CastlingRights == nd.Position.CastlingRights))
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
        /// Finds nodes matching the passed "filter" position.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="crits"></param>
        /// <returns></returns>
        public static List<TreeNode> FilterPositions(VariationTree tree, SearchPositionCriteria crits)
        {
            List<TreeNode> nodeList = new List<TreeNode>();

            foreach (TreeNode nd in tree.Nodes)
            {
                if (CompareByteArrays(crits.SearchPosition.Board, nd.Position.Board))
                {
                    if ((!crits.CheckEnpassant || IsSameEnpassantPossibilities(crits.SearchPosition, nd.Position))
                        && (!crits.CheckSideToMove || crits.SearchPosition.ColorToMove == nd.Position.ColorToMove)
                        && (!crits.CheckCastleRights || crits.SearchPosition.CastlingRights == nd.Position.CastlingRights))
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
        /// Determines the position to use for search.
        /// If there is a selected node its position will be used for search.
        /// If not, the clipboard content will be tested if it contains a valid FEN.
        /// If so, it will be used, otherwise we will set the starting position.
        /// </summary>
        /// <returns></returns>
        public static BoardPosition PreparePositionForSearch()
        {
            BoardPosition position = null;

            if (LastSearchPosition != null)
            {
                position = LastSearchPosition;
            }
            else
            {
                VariationTree tree = AppState.ActiveVariationTree;
                TreeNode nd = tree == null ? null : tree.SelectedNode;
                if (nd == null)
                {
                    string fen = PositionUtils.GetFenFromClipboard();
                    if (string.IsNullOrEmpty(fen))
                    {
                        try
                        {
                            FenParser.ParseFenIntoBoard(fen, ref position);
                        }
                        catch
                        {
                            position = null;
                            position = PositionUtils.SetupStartingPosition();
                        }
                    }
                }
                else
                {
                    position = nd.Position;
                }
            }

            return position;
        }

        /// <summary>
        /// Compares two byte arrays representing chess positions.
        /// The position argument is the one that is being checked.
        /// The filter argument is the one that is used as a filter.
        /// If the filter has a non-zero value in a square, the position must have the same piece in that square.
        /// If the filter has a 0xFF value in a square, the position must have an empty square in that position.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static bool CompareByteArrays(byte[,] filter, byte[,] position)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (filter[i, j] == 0xFF)
                    {
                        if (position[i, j] != 0)
                        {
                            return false;
                        }
                    }
                    else

                    if (filter[i, j] != 0 && filter[i, j] != position[i, j])
                    {
                        return false;
                    }
                }
            }
            return true;
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

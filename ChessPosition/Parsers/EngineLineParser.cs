using ChessPosition;
using System.Collections.Generic;

namespace GameTree
{
    public class EngineLineParser
    {
        /// <summary>
        /// Creates a list of TreeNodes given a string with moves in engine notation.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="parentEx"></param>
        /// <param name="engineLine"></param>
        /// <returns></returns>
        public static List<TreeNode> ParseEngineLine(VariationTree tree, TreeNode parentEx, string engineLine)
        {
            List<TreeNode> nodeList = new List<TreeNode>();

            TreeNode parent = parentEx.CloneJustMe();
            int nodeId = tree.GetNewNodeId();

            string[] moves = engineLine.Split(' ');
            foreach (string move in moves)
            {
                TreeNode node = new TreeNode(parent, "", nodeId);
                nodeId++;
                node.Position.Board = (byte[,])parent.Position.Board.Clone();

                node.LastMoveAlgebraicNotation = MoveUtils.EngineNotationToAlgebraic(move, ref node.Position, out _);
                node.LastMoveEngineNotation = move;

                node.Position.ColorToMove = parent.ColorToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
                node.Position.MoveNumber = parent.ColorToMove == PieceColor.Black ? parent.MoveNumber : (parent.MoveNumber + 1);
                parent.Children.Add(node);

                nodeList.Add(node);
                parent = node;
            }
            return nodeList;
        }
    }
}

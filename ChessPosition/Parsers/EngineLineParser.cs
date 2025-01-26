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
        public static List<TreeNode> ParseEngineLine(VariationTree tree, TreeNode insertAtNode, string engineLine)
        {
            List<TreeNode> nodeList = new List<TreeNode>();

            TreeNode parent = insertAtNode;
            string[] moves = engineLine.Split(' ');
            bool firstMove = true;
            foreach (string move in moves)
            {
                // create a new node, pass nodeId as -1 as it does not matter, it will be set later on in the process
                TreeNode node = new TreeNode(parent, "", -1);

                node.Position.Board = (byte[,])parent.Position.Board.Clone();

                node.LastMoveAlgebraicNotation = MoveUtils.EngineNotationToAlgebraic(move, ref node.Position, out _);
                node.LastMoveEngineNotation = move;

                node.Position.ColorToMove = parent.ColorToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
                node.Position.MoveNumber = parent.ColorToMove == PieceColor.Black ? parent.MoveNumber : (parent.MoveNumber + 1);

                // !do not add the child if the parent is the insertaAtNode!
                // it will be done later in the insertion process
                if (!firstMove)
                {
                    parent.Children.Add(node);
                }
                else
                {
                    firstMove = false;
                }

                nodeList.Add(node);
                parent = node;
            }
            return nodeList;
        }
    }
}

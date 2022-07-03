using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameTree;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Represents a single line in a tree.
    /// This will be used as backing data for the
    /// ActiveLine and EngineGame Data Grids. 
    /// </summary>
    public class ScoreSheet
    {
        /// <summary>
        /// The list of TreeNodes for the currently selected line
        /// (a line is a single list, not a tree).
        /// The number of nodes equals the number of plies (half moves)
        /// plus 1, because we store the starting position at index 0.
        /// </summary>
        public ObservableCollection<TreeNode> NodeList;

        /// <summary>
        /// The list of full moves. Each object contains both the
        /// white and black moves.
        /// If the line finishes on a white move, the value of BlackPly 
        /// will be null in the last object.
        /// </summary>
        public ObservableCollection<MoveWithEval> MoveList;

        public void SetLineToNode(TreeNode targetNode)
        {
            NodeList = new ObservableCollection<TreeNode>();
            
            TreeNode nd = targetNode;
            while (nd != null)
            {
                NodeList.Insert(0,nd);
                nd = nd.Parent;
            }
        }

        /// <summary>
        /// A new ply is to be added to the line
        /// e.g. because we just processed an engine
        /// or user move.
        /// </summary>
        /// <param name="nd"></param>
        public void AddPly(TreeNode nd)
        {
            // if it was Black's move, update the last object in the MoveList,
            // otherwise create a new object
            if (nd.Position.ColorToMove == PieceColor.White)
            {
                // previous move was by White
                MoveWithEval move = MoveList[MoveList.Count - 1];
                move.BlackPly = nd.LastMoveAlgebraicNotationWithNag;
            }
            else
            {
                MoveWithEval move = new MoveWithEval();
                move.WhitePly = nd.LastMoveAlgebraicNotationWithNag;
                move.Number = (MoveList.Count + 1).ToString() + ".";
                MoveList.Add(move);
            }

        }
    }
}

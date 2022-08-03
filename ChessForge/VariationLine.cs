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
        public ObservableCollection<TreeNode> NodeList = new ObservableCollection<TreeNode>();

        /// <summary>
        /// The list of full moves. Each object contains both the
        /// white and black moves.
        /// If the line finishes on a white move, the value of BlackPly 
        /// will be null in the last object.
        /// </summary>
        public ObservableCollection<MoveWithEval> MoveList = new ObservableCollection<MoveWithEval>();

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

        public void AddPlyAndMove(TreeNode nd)
        {
            AppStateManager.MainWin.Dispatcher.Invoke(() =>
            {
                NodeList.Add(nd);
                AddPly(nd);
            });
        }

        public void BuildMoveListFromPlyList()
        {
            MoveList = PositionUtils.BuildViewListFromLine(NodeList);
        }

        /// <summary>
        /// Removes all moves and plies trailing
        /// the specified Node.
        /// </summary>
        /// <param name="nd"></param>
        public void RollbackToNode(TreeNode nd)
        {
            for (int i = NodeList.Count - 1; i >= 0; i--)
            {
                if (NodeList[i].NodeId == nd.NodeId)
                {
                    break;
                }
                else
                {
                    RemoveLastPly();
                }
            }
        }

        /// <summary>
        /// Removes all moves and plies trailing
        /// the node for a ply identified by the move number
        /// and color to move.
        /// </summary>
        /// <param name="moveNumber"></param>
        /// <param name="colorToMove"></param>
        public void RollbackToPly(uint moveNumber, PieceColor colorToMove)
        {
            for (int i = NodeList.Count - 1; i >= 0; i--)
            {
                if (NodeList[i].MoveNumber == moveNumber && NodeList[i].ColorToMove == colorToMove)
                {
                    break;
                }
                else
                {
                    RemoveLastPly();
                }
            }
        }

        /// <summary>
        /// Returns the last Node of the game.
        /// </summary>
        /// <returns></returns>
        public TreeNode GetCurrentNode()
        {
            if (NodeList.Count == 0)
            {
                return null;
            }
            else
            {
                return NodeList[NodeList.Count - 1];
            }
        }

        /// <summary>
        /// Gets the Node object from the Line
        /// given its index on the Node list.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public TreeNode GetNodeAtIndex(int idx)
        {
            return NodeList[idx];
        }

        /// <summary>
        /// Gets the Move object from the Line
        /// given its index in the Move list.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public MoveWithEval GetMoveAtIndex(int idx)
        {
            return MoveList[idx];
        }

        /// <summary>
        /// Gets the Node object from the line
        /// given its id.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public TreeNode GetNodeFromId(int nodeId)
        {
            return NodeList.First(x => x.NodeId == nodeId);
        }

        /// <summary>
        /// Sets a new Node list and builds the move list.
        /// </summary>
        /// <param name="line"></param>
        public void SetNodeList(ObservableCollection<TreeNode> line)
        {
            NodeList = line;
            BuildMoveListFromPlyList();
        }

        /// <summary>
        /// Gets the number of plies in the Line.
        /// </summary>
        /// <returns></returns>
        public int GetPlyCount()
        {
            return NodeList.Count;
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

        /// <summary>
        /// Removes the last ply from both
        /// the list of plies and the scoresheet.
        /// </summary>
        public void RemoveLastPly()
        {
            NodeList.RemoveAt(NodeList.Count - 1);

            MoveWithEval lastMove = MoveList[MoveList.Count - 1];
            if (!string.IsNullOrEmpty(lastMove.BlackPly))
            {
                lastMove.BlackPly = null;
            }
            else
            {
                MoveList.RemoveAt(MoveList.Count - 1);
            }
        }

        public void ReplaceLastPly(TreeNode nd)
        {
            NodeList[NodeList.Count - 1] = nd;
            
            MoveWithEval move = MoveList[MoveList.Count - 1];

            if (nd.Position.ColorToMove == PieceColor.White)
            {
                // we are replacing Black's move
                move.BlackPly = nd.LastMoveAlgebraicNotationWithNag;
            }
            else
            {
                move.WhitePly = nd.LastMoveAlgebraicNotationWithNag;
            }
        }
    }
}

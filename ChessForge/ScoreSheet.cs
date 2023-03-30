﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameTree;
using ChessPosition;
using System.Windows;
using System.Windows.Media;

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
        /// The number of nodes equals the number of plies (half moves) plus 1, 
        /// because we store the starting position BEFORE first move at index 0.
        /// </summary>
        public ObservableCollection<TreeNode> NodeList = new ObservableCollection<TreeNode>();

        /// <summary>
        /// The list of full moves. Each object contains both the
        /// white and black moves.
        /// If the line finishes on a white move, the value of BlackPly 
        /// will be null in the last object.
        /// Move number 1 is stored at index 0.
        /// </summary>
        public ObservableCollection<MoveWithEval> MoveList = new ObservableCollection<MoveWithEval>();

        /// <summary>
        /// Creates a line for the starting position to the passed Node.
        /// This is needed e.g. when a game is about to start from a position
        /// selected by the user.
        /// </summary>
        /// <param name="targetNode"></param>
        public void SetLineToNode(TreeNode targetNode)
        {
            NodeList = new ObservableCollection<TreeNode>();

            TreeNode nd = targetNode;
            while (nd != null)
            {
                NodeList.Insert(0, nd);
                nd = nd.Parent;
            }
        }

        /// <summary>
        /// Adds the passed Node to the list of Nodes (plies)
        /// and to the list of moves (ScoreSheet)
        /// </summary>
        /// <param name="nd"></param>
        public void AddPlyAndMove(TreeNode nd)
        {
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                NodeList.Add(nd);
                AddPly(nd);
            });
        }

        /// <summary>
        /// Builds the list of moves from the list of Nodes.
        /// </summary>
        public void BuildMoveListFromPlyList()
        {
            MoveList = PositionUtils.BuildMoveListFromLine(NodeList);
        }

        /// <summary>
        /// Used to refresh move notation when we change to/from "figurines".
        /// </summary>
        public void RefreshMovesText()
        {
            ObservableCollection<MoveWithEval> tmp = PositionUtils.BuildMoveListFromLine(NodeList);
            if (tmp.Count == MoveList.Count)
            {
                for (int i = 0; i < tmp.Count; i++)
                {
                    MoveList[i].WhitePly = tmp[i].WhitePly;
                    MoveList[i].BlackPly = tmp[i].BlackPly;
                }
            }
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
        public TreeNode GetLastNode()
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
        /// Returns the id of the line represented by this object.
        /// This is LineId of the last node in the list.
        /// </summary>
        /// <returns></returns>
        public string GetLineId()
        {
            if (NodeList.Count > 0)
            {
                return NodeList[NodeList.Count - 1].LineId;
            }
            else
            {
                return "";
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
            if (idx < 0 || idx >= NodeList.Count)
            {
                string msg = "ScoreSheet:GetNodeAtIndex bad index = " + idx.ToString();
                DebugUtils.ShowDebugMessage(msg);
                AppLog.Message(msg);
                AppLog.Message("    Context: Active Content Type = " + AppState.ActiveContentType.ToString());
                AppLog.Message("             Active Chapter Idx  = " + AppState.ActiveChapter.Index.ToString());
                AppLog.Message("             Active Game Idx     = " + WorkbookManager.SessionWorkbook.ActiveChapter.ActiveModelGameIndex.ToString());
                AppLog.Message("             Active Exercise Idx = " + WorkbookManager.SessionWorkbook.ActiveChapter.ActiveExerciseIndex.ToString());
                return null;
            }
            return idx >= 0 ? NodeList[idx] : null;
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
            return NodeList.FirstOrDefault(x => x.NodeId == nodeId);
        }

        /// <summary>
        /// Gets the move (ply) from the Move List and its color.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public MoveWithEval GetMoveFromNodeId(int nodeId, out PieceColor color)
        {
            color = PieceColor.None;

            // note that we may get 0 passed here in some context (meaning the starting position)
            // we don't want it to be "found" as the black ply past the last white ply! 
            if (nodeId <= 0)
            {
                return null;
            }

            foreach (MoveWithEval mv in MoveList)
            {
                if (mv.WhiteNodeId == nodeId)
                {
                    color = PieceColor.White;
                    return mv;
                }
                else if (mv.BlackNodeId == nodeId)
                {
                    color = PieceColor.Black;
                    return mv;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds index of a Node in the Node/Ply list.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public int GetIndexForNode(TreeNode nd)
        {
            if (nd != null)
            {
                int index = -1;
                for (int i = 0; i < NodeList.Count; i++)
                {
                    if (NodeList[i].NodeId == nd.NodeId)
                    {
                        index = i;
                        break;
                    }
                }
                return index;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Finds index of a Node with the given NodeId in the Node/Ply list.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public int GetIndexForNode(int nodeId)
        {
            int index = -1;
            for (int i = 0; i < NodeList.Count; i++)
            {
                if (NodeList[i].NodeId == nodeId)
                {
                    index = i;
                    break;
                }
            }
            return index;
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
        /// Returns the list of nodes in the line.
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<TreeNode> GetNodeList()
        {
            return NodeList;
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
                // If we are in exercise that starts fronm a Black move there will be no moves yet
                // and we need to create one
                MoveWithEval move;
                if (MoveList.Count == 0)
                {
                    move = new MoveWithEval();
                    MoveList.Add(move);
                }
                else
                {
                    move = MoveList[MoveList.Count - 1];
                }
                move.BlackPly = nd.GetGuiPlyText(true);
                move.BlackEval = nd.EngineEvaluation;
                move.BlackNodeId = nd.NodeId;
            }
            else
            {
                MoveWithEval move = new MoveWithEval();
                move.WhitePly = nd.GetGuiPlyText(true);
                move.WhiteEval = nd.EngineEvaluation;
                move.WhiteNodeId = nd.NodeId;
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
            AppState.MainWin.Dispatcher.Invoke(() =>
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
            });
        }

        /// <summary>
        /// Replaces the last play with the passed node.
        /// </summary>
        /// <param name="nd"></param>
        public void ReplaceLastPly(TreeNode nd)
        {
            NodeList[NodeList.Count - 1] = nd;

            MoveWithEval move = MoveList[MoveList.Count - 1];

            if (nd.Position.ColorToMove == PieceColor.White)
            {
                // we are replacing Black's move
                move.BlackPly = nd.GetGuiPlyText(true);
            }
            else
            {
                move.WhitePly = nd.GetGuiPlyText(true);
            }
        }
    }
}

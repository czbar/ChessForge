using System;
using System.Collections.Generic;
using System.Timers;
using System.Text;
using System.Threading.Tasks;
using GameTree;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Controls the game between the user and the engine.
    /// There can only be one such game in progress at a time.
    /// </summary>
    public class EngineGame
    {
        public enum GameState
        {
            IDLE = 0x00,
            USER_THINKING = 0x01,
            ENGINE_THINKING = 0x02,
            PAUSED = 0x04
        }

        /// <summary>
        /// The line of the game played between the user and
        /// the computer.
        /// The first TreeNode must have GameStartPosition as its parent.
        /// </summary>
        public static ScoreSheet Line = new ScoreSheet();

        /// <summary>
        /// Current state of the game.
        /// </summary>
        public static GameState State;

        /// <summary>
        /// Position from which the game started.
        /// This will be a reference to a Node in the 
        /// Workbook Tree.
        /// </summary>
        private static TreeNode GameStartPosition;

        private static bool _trainingWorkbookMoveMade;

        /// <summary>
        /// Flag indicating that a workbook move was selected during a training session
        /// </summary>
        public static bool TrainingWorkbookMoveMade { get => _trainingWorkbookMoveMade; set => _trainingWorkbookMoveMade = value; }

        public static void CheckForTrainingWorkbookMoveMade(object source, ElapsedEventArgs e)
        {
            if (TrainingWorkbookMoveMade)
            {
                TrainingWorkbookMoveMade = false;
                // stop polling for the workbook move
                AppState.MainWin.Timers.Stop(AppTimers.TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE);

                // TODO: show appropriate notifications in the GUI
                // start polling for the user move
                AppState.MainWin.Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
                TrainingState.CurrentMode = TrainingState.Mode.AWAITING_USER_MOVE;
            }
        }

        /// <summary>
        /// Initializes the EngineGame structures before the game starts.
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="IsEngineMove"></param>
        public static void PrepareGame(TreeNode startNode, bool IsEngineMove, bool IsTraining)
        {
            State = IsEngineMove ? GameState.ENGINE_THINKING : GameState.USER_THINKING;
            GameStartPosition = startNode;

            if (!IsTraining)
            {
                Line.SetLineToNode(startNode);
                Line.MoveList = PositionUtils.BuildViewListFromLine(Line.NodeList);
            }
        }

        public static BoardPosition ProcessEngineGameMove(out TreeNode nd)
        {
            string engMove = SelectMove(false);
            TreeNode curr = GetCurrentNode();

            BoardPosition pos = new BoardPosition(curr.Position);

            bool isCastle;
            string algMove = MoveUtils.EngineNotationToAlgebraic(engMove, ref pos, out isCastle);

            // note: we don't care about NodeId here
            nd = new TreeNode(curr, algMove, -1);
            nd.Position = pos;
            nd.Position.ColorToMove = pos.ColorToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
            nd.MoveNumber = nd.Position.ColorToMove == PieceColor.White ? nd.MoveNumber : nd.MoveNumber += 1;
            Line.NodeList.Add(nd);
            Line.AddPly(nd);

            return pos;
        }

        /// <summary>
        /// Processes a move made manually by the user on the board.
        /// Sets appropriate flags so that ProcessUserGameMoveEvent will determine
        /// what actions to take when its associated timer picks it up.
        /// TODO: do we need a lock here so ProcessUserGameMoveEvent does not start before
        /// we finish this?
        /// 
        /// Returns true if it is a valid move.
        /// </summary>
        /// <returns></returns>
        public static bool ProcessUserGameMove(string move, out TreeNode nd, out bool isCastle)
        {
            isCastle = false;

            nd = EngineGame.CreateNextNode();
            string algMove = "";
            try
            {
                algMove = MoveUtils.EngineNotationToAlgebraic(move, ref nd.Position, out isCastle);
            }
            catch
            {
                algMove = "";
            }

            // check that it starts with a letter as it may be something invalid like "???"
            if (!string.IsNullOrEmpty(algMove) && char.IsLetter(algMove[0]))
            {
                nd.Position.ColorToMove = nd.Position.ColorToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
                nd.MoveNumber = nd.Position.ColorToMove == PieceColor.White ? nd.MoveNumber : nd.MoveNumber += 1;
                nd.LastMoveAlgebraicNotation = algMove;
                AddNode(nd);
                if (TrainingState.IsTrainingInProgress)
                {
                    TrainingState.CurrentMode = TrainingState.Mode.USER_MOVE_COMPLETED;
                    AppState.MainWin.Timers.Start(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
                }
                else
                {
                    State = GameState.ENGINE_THINKING;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a node for the move made in the current (last)
        /// node.
        /// The caller will then check the validity of the move, update
        /// the position and add it to the GameLine
        /// </summary>
        /// <returns></returns>
        public static TreeNode CreateNextNode()
        {
            TreeNode curr = GetCurrentNode();

            BoardPosition pos = new BoardPosition(curr.Position);

            // note: we don't care about NodeId here
            TreeNode nd = new TreeNode(curr, "", -1);
            nd.Position = pos;

            return nd;
        }

        private static void AddNode(TreeNode nd)
        {
            Line.NodeList.Add(nd);
        }

        /// <summary>
        /// Returns the current position i.e. the one being under consideration
        /// by either the user or the engine.
        /// It this is the first move of the game it will be the StartPosition,
        /// otherwise the last position in the GameLine.
        /// </summary>
        /// <returns></returns>
        public static TreeNode GetCurrentNode()
        {
            if (Line.NodeList.Count == 0)
            {
                return GameStartPosition;
            }
            else
            {
                return Line.NodeList[Line.NodeList.Count - 1];
            }
        }

        public static void ReplaceCurrentWithWorkbookMove(TreeNode nd)
        {
            Line.NodeList[Line.NodeList.Count - 1] = nd;
            Line.ReplaceLastPly(nd);
            AppState.MainWin.DisplayPosition(nd.Position);
        }

        public static void ReplaceCurrentWithWorkbookMove(int nodeId)
        {
            TreeNode nd = AppState.MainWin.Workbook.GetNodeFromNodeId(nodeId);
            ReplaceCurrentWithWorkbookMove(nd);
        }

        public static BoardPosition GetCurrentPosition()
        {
            return GetCurrentNode().Position;
        }

        /// <summary>
        /// As part of processing of a user move we insert it to the list of nodes
        /// and then need to reflect it in the list of plies.
        /// That is when this method will be called
        /// </summary>
        public static void AddLastNodeToPlies()
        {
            Line.AddPly(GetCurrentNode());
        }

        public static void AddPlyToGame(TreeNode nd)
        {
            AddNode(nd);
            AddLastNodeToPlies();
        }

        /// <summary>
        /// Once the engine processing has finished,
        /// the GUI will call this method to select
        /// the move.
        /// Depending on the context, we want to return 
        /// the best move or a randomly selected,
        /// "reasonable" move.
        /// </summary>
        /// <param name="getBest"></param>
        /// <returns>The selected move in the engine notation
        /// e.g. c4d6 / c7c8Q / O-O </returns>
        private static string SelectMove(bool getBest)
        {
            if (EngineMessageProcessor.MoveCandidates.Count == 0)
            {
                return "";
            }

            if (getBest)
            {
                return EngineMessageProcessor.MoveCandidates[0].GetCandidateMove();
            }
            else
            {
                int viableMoveCount = 1;
                int highestEval = EngineMessageProcessor.MoveCandidates[0].ScoreCp;
                for (int i = 1; i < EngineMessageProcessor.MoveCandidates.Count; i++)
                {
                    if (EngineMessageProcessor.MoveCandidates[i].ScoreCp < highestEval - Configuration.ViableMoveCpDiff)
                    {
                        break;
                    }
                    viableMoveCount++;
                }
                if (viableMoveCount == 1)
                {
                    return EngineMessageProcessor.MoveCandidates[0].GetCandidateMove();
                }
                else
                {
                    int sel = PositionUtils.GlobalRnd.Next(0, viableMoveCount);
                    // defensive check
                    if (sel >= EngineMessageProcessor.MoveCandidates.Count)
                    {
                        return EngineMessageProcessor.MoveCandidates[0].GetCandidateMove();
                    }
                    else
                    {
                        return EngineMessageProcessor.MoveCandidates[sel].GetCandidateMove();
                    }
                }
            }
        }

    }
}

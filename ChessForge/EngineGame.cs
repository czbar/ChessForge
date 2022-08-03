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
        public static GameState CurrentState;

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

        private static MainWindow _mainWin;

        public static void SetMainWin(MainWindow mainWin)
        {
            _mainWin = mainWin;
        }

        public static void CheckForTrainingWorkbookMoveMade(object source, ElapsedEventArgs e)
        {
            if (TrainingWorkbookMoveMade)
            {
                TrainingWorkbookMoveMade = false;
                // stop polling for the workbook move
                _mainWin.Timers.Stop(AppTimers.TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE);

                _mainWin.DisplayPosition(GetCurrentPosition());
                SoundPlayer.PlayMoveSound(GetCurrentNode().LastMoveAlgebraicNotation);

                _mainWin._trainingView.WorkbookMoveMade();

                // TODO: show appropriate notifications in the GUI
                // start polling for the user move
                _mainWin.Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
                TrainingState.CurrentMode = TrainingState.Mode.AWAITING_USER_TRAINING_MOVE;
            }
        }

        /// <summary>
        /// Initializes the EngineGame structures before the game starts.
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="IsEngineMove"></param>
        public static void PrepareGame(TreeNode startNode, bool IsEngineMove, bool IsTraining)
        {
            CurrentState = IsEngineMove ? GameState.ENGINE_THINKING : GameState.USER_THINKING;
            GameStartPosition = startNode;

            if (!IsTraining)
            {
                Line.SetLineToNode(startNode);
                Line.BuildMoveListFromPlyList();
            }
        }

        /// <summary>
        /// Processes engine move during a game.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public static BoardPosition ProcessEngineGameMove(out TreeNode nd)
        {
            // debug exception
            if (LearningMode.CurrentMode != LearningMode.Mode.GAME_VS_COMPUTER)
                throw (new Exception("ProcessEngineGameMove() called NOT during a game"));

            string engMove = SelectMove(false);
            TreeNode curr = GetCurrentNode();

            BoardPosition pos = new BoardPosition(curr.Position);

            bool isCastle;
            string algMove = MoveUtils.EngineNotationToAlgebraic(engMove, ref pos, out isCastle);

            nd = new TreeNode(curr, algMove, _mainWin.Workbook.GetNewNodeId());
            nd.IsNewTrainingMove = curr.IsNewTrainingMove;
            nd.Position = pos;
            nd.Position.ColorToMove = pos.ColorToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
            nd.MoveNumber = nd.Position.ColorToMove == PieceColor.White ? nd.MoveNumber : nd.MoveNumber += 1;
            Line.AddPlyAndMove(nd);
            _mainWin.Workbook.AddNodeToParent(nd);

            return pos;
        }

        /// <summary>
        /// Processes a move made manually by the user on the board.
        /// This could be in any App Mode i.e. during a game against thje engine in
        /// Manual Review or Game Play mode, or in Training responding to "coach's" move.
        /// Sets appropriate flags so that ProcessUserGameMoveEvent will determine
        /// what actions to take when its associated timer picks it up.
        /// TODO: do we need a lock here so ProcessUserGameMoveEvent does not start before
        /// we finish this?
        /// 
        /// Returns true if it is a valid move.
        /// </summary>
        /// <returns></returns>
        public static bool ProcessUserMove(string move, out TreeNode nd, out bool isCastle)
        {
            isCastle = false;

            nd = CreateNextNode();
            string algMove;
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
                TreeNode sib = _mainWin.Workbook.GetIdenticalSibling(nd);
                if (sib == null)
                {
                    // if this is a new move, mark as such and add to Workbook
                    nd.IsNewTrainingMove = true;
                    _mainWin.Workbook.AddNodeToParent(nd);
                }
                else
                {
                    nd = sib;
                }
                Line.AddPlyAndMove(nd);
                SwitchToAwaitEngineMove(nd);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// This method will be invoked after the engine's move was processed,
        /// or when we restart a game vs engine from an eralier move.
        /// </summary>
        /// <param name="nd"></param>
        private static void SwitchToAwaitEngineMove(TreeNode nd)
        {
            if (TrainingState.IsTrainingInProgress && LearningMode.CurrentMode != LearningMode.Mode.GAME_VS_COMPUTER)
            {
                TrainingState.CurrentMode = TrainingState.Mode.USER_MOVE_COMPLETED;
                _mainWin.Timers.Start(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
            }
            else
            {
                if (TrainingState.IsTrainingInProgress)
                {
                    // this is a game during Training triggered by the user making a move not in Workbook.
                    // We know, therefore, that this is a new move.
                    nd.IsNewTrainingMove = true;
                    nd.NodeId = _mainWin.Workbook.GetNewNodeId();
                    _mainWin._trainingView.UserMoveMade();
                }
                CurrentState = GameState.ENGINE_THINKING;
            }
        }

        private static void SwitchToAwaitUserMove(TreeNode nd)
        {
            CurrentState = GameState.USER_THINKING;
            _mainWin.Timers.Start(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
            _mainWin.Timers.Stop(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
        }

        /// <summary>
        /// A request to restart the engine game at the specified
        /// user move was received.
        /// We remove all nodes created after the last node.
        /// </summary>
        /// <param name="nd"></param>
        public static void RestartAtUserMove(TreeNode nd)
        {
            _mainWin.Workbook.RemoveTailAfter(nd);
            Line.RollbackToNode(nd);
            SwitchToAwaitEngineMove(nd);
        }

        /// <summary>
        /// A request to restart the engine game at the specified
        /// engine move was received.
        /// We remove all nodes created after the last node.
        /// </summary>
        /// <param name="nd"></param>
        public static void RestartAtEngineMove(TreeNode nd)
        {
            _mainWin.Workbook.RemoveTailAfter(nd);
            Line.RollbackToNode(nd);
            SwitchToAwaitUserMove(nd);
        }

        /// <summary>
        /// Rolls back the game to the position with 
        /// the same move number and color-to-move as 
        /// the passed Node.
        /// Replaces the found Node with the one passed.
        /// </summary>
        /// <param name="nd"></param>
        public static void RollbackGame(TreeNode nd)
        {
            Line.RollbackToPly(nd.MoveNumber, nd.ColorToMove);
            Line.ReplaceLastPly(nd);
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

            TreeNode nd = new TreeNode(curr, "", _mainWin.Workbook.GetNewNodeId());
            nd.Position = pos;

            return nd;
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
            TreeNode nd = Line.GetCurrentNode();
            if (nd == null)
            {
                nd = GameStartPosition;
            }

            return nd;
        }

        /// <summary>
        /// Returns the type of the piece on a given square
        /// in the current position.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static PieceType GetPieceType(SquareCoords sq)
        {
            return PositionUtils.GetPieceType(GetCurrentNode().Position.Board[sq.Xcoord, sq.Ycoord]);
        }

        /// <summary>
        /// Returns the color of the piece on a given square
        /// in the current position.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static PieceColor GetPieceColor(SquareCoords sq)
        {
            TreeNode nd = GetCurrentNode();
            return nd != null ? PositionUtils.GetPieceColor(GetCurrentNode().Position.Board[sq.Xcoord, sq.Ycoord]) : PieceColor.None;
        }

        /// <summary>
        /// Accessor to ColorToMove property
        /// of the current position
        /// </summary>
        public static PieceColor ColorToMove
        {
            get { return GetCurrentPosition().ColorToMove; }
        }

        public static void ReplaceCurrentWithWorkbookMove(TreeNode nd)
        {
            Line.ReplaceLastPly(nd);
            _mainWin.DisplayPosition(nd.Position);
        }

        public static void ReplaceCurrentWithWorkbookMove(int nodeId)
        {
            TreeNode nd = _mainWin.Workbook.GetNodeFromNodeId(nodeId);
            ReplaceCurrentWithWorkbookMove(nd);
        }

        public static BoardPosition GetCurrentPosition()
        {
            TreeNode nd = GetCurrentNode();
            return nd != null ?  GetCurrentNode().Position : null;
        }

        /// <summary>
        /// As part of processing of a user move we insert it to the list of nodes
        /// and then need to reflect it in the list of plies.
        /// That is when this method will be called
        /// </summary>
        public static void AddLastNodeToPlies()
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                Line.AddPly(GetCurrentNode());
            });
        }

        public static void AddPlyToGame(TreeNode nd)
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                Line.AddPlyAndMove(nd);
            });
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

using System;
using System.Collections.Generic;
using System.Timers;
using System.Text;
using System.Threading.Tasks;
using GameTree;
using ChessPosition;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Controls the game between the user and the engine.
    /// There can only be one such game in progress at a time.
    /// </summary>
    public class EngineGame
    {
        /// <summary>
        /// Possible states of the game.
        /// </summary>
        public enum GameState
        {
            IDLE = 0x00,
            USER_THINKING = 0x01,
            ENGINE_THINKING = 0x02,
        }

        /// <summary>
        /// The line of the game played between the user and the computer.
        /// The first TreeNode must have GameStartPosition as its parent.
        /// </summary>
        public static ScoreSheet Line = new ScoreSheet();

        /// <summary>
        /// Current state of the game. This property is read only.
        /// To set the value, clients need to call ChangeCurrentState.
        /// </summary>
        public static GameState CurrentState { get => _gameState; }

        // color that the engine plays with
        private static PieceColor _engineColor = PieceColor.None;

        /// <summary>
        /// The color with which the engine plays.
        /// This is not controlled here but is just a convenient
        /// place holder for use by clients.
        /// In particular it may not be set at all.
        /// </summary>
        public static PieceColor EngineColor
        {
            get => _engineColor;
            set => _engineColor = value;
        }


        /// <summary>
        /// Position from which the game started.
        /// This will be a reference to a Node in the Workbook Tree.
        /// </summary>
        private static TreeNode GameStartPosition;

        // Current game state
        private static GameState _gameState;

        // Flags if a training workbook move was made.
        private static bool _isTrainingWorkbookMoveMade;

        /// <summary>
        /// Flag indicating that a workbook move was selected during a training session and made by the program (a.k.a. coach).
        /// </summary>
        public static bool IsTrainingWorkbookMoveMade { get => _isTrainingWorkbookMoveMade; set => _isTrainingWorkbookMoveMade = value; }

        /// <summary>
        /// Reference to the main application window.
        /// </summary>
        private static MainWindow _mainWin { get => AppState.MainWin; }

        /// <summary>
        /// Changes the current state of the game.
        /// </summary>
        /// <param name="state"></param>
        public static void ChangeCurrentState(GameState state)
        {
            AppLog.Message(2, "Game: ChangeCurrentState() to " + state.ToString());
            _gameState = state;
        }

        /// <summary>
        /// Invoked by the CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE timer
        /// checks if the Workbook move has been made by the "coach".
        /// If so, performs appropriate actions.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static void CheckForTrainingWorkbookMoveMade(object source, ElapsedEventArgs e)
        {
            if (IsTrainingWorkbookMoveMade)
            {
                IsTrainingWorkbookMoveMade = false;
                // stop polling for the workbook move
                _mainWin.Timers.Stop(AppTimers.TimerId.CHECK_FOR_TRAINING_WORKBOOK_MOVE_MADE);

                _mainWin.DisplayPosition(GetLastGameNode());
                SoundPlayer.PlayMoveSound(GetLastGameNode().LastMoveAlgebraicNotation);
                _mainWin.ColorMoveSquares(GetLastGameNode().LastMoveEngineNotation);


                _mainWin.UiTrainingView.WorkbookMoveMade();

                // start polling for the user move
                _mainWin.Timers.Stop(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
                TrainingSession.ChangeCurrentState(TrainingSession.State.AWAITING_USER_TRAINING_MOVE);
            }
        }

        /// <summary>
        /// Initializes the EngineGame structures before the game starts.
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="IsEngineMove"></param>
        public static void InitializeGameObject(TreeNode startNode, bool IsEngineMove, bool IsTraining)
        {
            ChangeCurrentState(IsEngineMove ? GameState.ENGINE_THINKING : GameState.USER_THINKING);
            GameStartPosition = startNode;

            if (!IsTraining)
            {
                Line.SetLineToNode(startNode);
                Line.BuildMoveListFromPlyList();

                Line.CopyNodeListToTree();
            }
        }

        /// <summary>
        /// Processes an engine move during a game.
        /// Selects the move from the candidate moves supplied by the engine,
        /// processes it, builds a new ply (TreeNode) and adds it to the
        /// game line.
        /// Also adds the move to the Workbook.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public static BoardPosition ProcessEngineMove(out TreeNode nd, string evalStr)
        {
            string engMove = SelectMoveFromCandidates(false);
            EngineMessageProcessor.ClearMoveCandidates(true);

            TreeNode curr = GetLastGameNode();
            BoardPosition pos = new BoardPosition(curr.Position);
            pos.InheritedEnPassantSquare = curr.Position.EnPassantSquare;
            pos.EnPassantSquare = 0;

            string algMove = MoveUtils.EngineNotationToAlgebraic(engMove, ref pos, out bool isCastle);

            if (string.IsNullOrEmpty(algMove) || algMove.StartsWith("?"))
            {
                MessageBox.Show(Properties.Resources.FailedProcessEngineMove, Properties.Resources.UnexpectedError, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            nd = new TreeNode(curr, algMove, _mainWin.ActiveVariationTree.GetNewNodeId());

            if (AppState.EngineEvaluationsUpdateble)
            {
                nd.SetEngineEvaluation(evalStr);
            }

            TreeNode sib = AppState.MainWin.ActiveVariationTree.GetIdenticalSibling(nd, engMove);
            if (sib == null)
            {
                nd.IsNewTrainingMove = curr.IsNewTrainingMove;
                nd.Position = pos;
                nd.Position.ColorToMove = MoveUtils.ReverseColor(pos.ColorToMove);
                PositionUtils.SetCheckStaleMateFlags(ref nd.Position);
                nd.MoveNumber = nd.Position.ColorToMove == PieceColor.White ? nd.MoveNumber : nd.MoveNumber += 1;
                _mainWin.ActiveVariationTree.AddNodeToParent(nd);
                Line.AddPlyAndMove(nd);
            }
            else
            {
                nd = sib;
                Line.AddPlyAndMove(nd);
            }

            return pos;
        }

        /// <summary>
        /// This method will be invoked after the user's move was processed,
        /// or when we restart a game vs engine from an earlier move.
        /// </summary>
        /// <param name="nd"></param>
        public static void SwitchToAwaitEngineMove(TreeNode nd, bool endOfGame)
        {
            if (TrainingSession.IsTrainingInProgress && LearningMode.CurrentMode != LearningMode.Mode.ENGINE_GAME)
            {
                AppLog.Message(2, "SwitchToAwaitEngineMove() in Training in mode " + LearningMode.CurrentMode.ToString());
                TrainingSession.ChangeCurrentState(TrainingSession.State.USER_MOVE_COMPLETED);
                // here we are in the play against Workbook mode so even if endOfGame == true
                // start the CHECK_FOR_USER_MOVE timer so that we can report the mate properly in response
                _mainWin.Timers.Start(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
            }
            else
            {
                if (TrainingSession.IsTrainingInProgress)
                {
                    AppLog.Message(2, "SwitchToAwaitEngineMove() in Training Game" );
                    // this is a game during Training triggered by the user making a move not in Workbook.
                    // We know, therefore, that this is a new move.
                    nd.IsNewTrainingMove = true;
                    nd.NodeId = _mainWin.ActiveVariationTree.GetNewNodeId();
                    _mainWin.UiTrainingView.UserGameMoveMade();
                    _mainWin.Timers.Start(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
                }
                if (endOfGame)
                {
                    ChangeCurrentState(GameState.IDLE);
                }
                else
                {
                    ChangeCurrentState(GameState.ENGINE_THINKING);
                }
            }
        }

        /// <summary>
        /// A request to restart the engine game at the specified
        /// user move was received.
        /// We remove all nodes created after the last node.
        /// </summary>
        /// <param name="nd"></param>
        public static void RestartAtUserMove(TreeNode nd)
        {
            _mainWin.ActiveVariationTree.RemoveTailAfter(nd);
            Line.RollbackToNode(nd);
            SwitchToAwaitEngineMove(nd, false);
        }

        /// <summary>
        /// A request to restart the engine game at the specified
        /// engine move was received.
        /// We remove all nodes created after the last node.
        /// </summary>
        /// <param name="nd"></param>
        public static void RestartAtEngineMove(TreeNode nd)
        {
            _mainWin.ActiveVariationTree.RemoveTailAfter(nd);
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
        /// Returns the current position i.e. the one being under consideration
        /// by either the user or the engine.
        /// It this is the first move of the game it will be the StartPosition,
        /// otherwise the last position in the GameLine.
        /// </summary>
        /// <returns></returns>
        public static TreeNode GetLastGameNode()
        {
            TreeNode nd = Line.GetLastNode();
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
            return PositionUtils.GetPieceType(GetLastGameNode().Position.Board[sq.Xcoord, sq.Ycoord]);
        }

        /// <summary>
        /// Returns the color of the piece on a given square
        /// in the last position of the game.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static PieceColor GetPieceColor(SquareCoords sq)
        {
            TreeNode nd = GetLastGameNode();
            return nd != null ? PositionUtils.GetPieceColor(GetLastGameNode().Position.Board[sq.Xcoord, sq.Ycoord]) : PieceColor.None;
        }

        /// <summary>
        /// Accessor to the ColorToMove property
        /// of the last position in the game.
        /// </summary>
        public static PieceColor ColorToMove
        {
            get
            {
                BoardPosition pos = GetLastPosition();
                return pos != null ? GetLastPosition().ColorToMove : PieceColor.None;
            }
        }

        /// <summary>
        /// Replaces the last ply in the game with the passed node (ply).
        /// </summary>
        /// <param name="nd"></param>
        public static void ReplaceLastPly(TreeNode nd)
        {
            Line.ReplaceLastPly(nd);
            _mainWin.DisplayPosition(nd);
            _mainWin.ColorMoveSquares(nd.LastMoveEngineNotation);
        }

        /// <summary>
        /// Replaces the last ply in the game with the node
        /// with the passed NodeId.
        /// </summary>
        /// <param name="nd"></param>
        public static void ReplaceLastPly(int nodeId)
        {
            TreeNode nd = _mainWin.ActiveVariationTree.GetNodeFromNodeId(nodeId);
            ReplaceLastPly(nd);
        }

        /// <summary>
        /// Obtains the latest position in the current game. 
        /// </summary>
        /// <returns></returns>
        public static BoardPosition GetLastPosition()
        {
            TreeNode nd = GetLastGameNode();
            return nd != null ? GetLastGameNode().Position : null;
        }

        /// <summary>
        /// Adds the passed Node to the list of Nodes (plies)
        /// and to the ScoreSheet (moves)
        /// </summary>
        /// <param name="nd"></param>
        public static void AddPlyToGame(TreeNode nd)
        {
            _mainWin.Dispatcher.Invoke(() =>
            {
                Line.AddPlyAndMove(nd);
            });
        }

        /// <summary>
        /// Switches mode to awaiting for the user move
        /// </summary>
        /// <param name="nd"></param>
        public static void SwitchToAwaitUserMove(TreeNode nd)
        {
            AppLog.Message(2, "SwitchToAwaitUserMove()");
            ChangeCurrentState(GameState.USER_THINKING);
            _mainWin.Timers.Start(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
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
        private static string SelectMoveFromCandidates(bool getBest)
        {
            if (EngineMessageProcessor.EngineMoveCandidates.Lines.Count == 0)
            {
                return "";
            }

            if (getBest || EngineMessageProcessor.EngineMoveCandidates.Lines[0].IsMateDetected)
            {
                return EngineMessageProcessor.EngineMoveCandidates.Lines[0].GetCandidateMove();
            }
            else
            {
                int viableMoveCount = 1;
                int highestEval = EngineMessageProcessor.EngineMoveCandidates.Lines[0].ScoreCp;
                for (int i = 1; i < EngineMessageProcessor.EngineMoveCandidates.Lines.Count; i++)
                {
                    if (EngineMessageProcessor.EngineMoveCandidates.Lines[i].IsMateDetected 
                        || EngineMessageProcessor.EngineMoveCandidates.Lines[i].ScoreCp < highestEval - Configuration.ViableMoveCpDiff)
                    {
                        break;
                    }
                    viableMoveCount++;
                }
                if (viableMoveCount == 1)
                {
                    return EngineMessageProcessor.EngineMoveCandidates.Lines[0].GetCandidateMove();
                }
                else
                {
                    int sel = PositionUtils.GlobalRnd.Next(0, viableMoveCount);
                    // defensive check
                    if (sel >= EngineMessageProcessor.EngineMoveCandidates.Lines.Count)
                    {
                        return EngineMessageProcessor.EngineMoveCandidates.Lines[0].GetCandidateMove();
                    }
                    else
                    {
                        return EngineMessageProcessor.EngineMoveCandidates.Lines[sel].GetCandidateMove();
                    }
                }
            }
        }

    }
}

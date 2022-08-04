using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameTree;
using ChessPosition;

namespace ChessForge
{
    /// <summary>
    /// Handles messages received from the chess engine
    /// and requests for evaluation in different contexts.
    /// There will be one instance of this class created 
    /// in the Main Window lasting the whole session. 
    /// </summary>
    public class EngineMessageProcessor
    {
        // an instance of the engine service
        public static EngineService.EngineProcess ChessEngineService;

        /// <summary>
        /// The list of candidates returned by the engine.
        /// </summary>
        public static List<MoveEvaluation> MoveCandidates = new List<MoveEvaluation>();

        /// <summary>
        /// Flags whether message processing is in progress.
        /// We won't process another message untile this flag
        /// is reset.
        /// </summary>
        public static bool IsInfoMessageProcessing = false;

        /// <summary>
        /// Lock object to use when accessing the list of candidate move.
        /// </summary>
        public static object MoveCandidatesLock = new object();

        /// <summary>
        /// Lock object to use when accessing the "info message processing" flag.
        /// </summary>
        private static object InfoMessageProcessLock = new object();

        private static MainWindow _mainWin;

        /// <summary>
        /// Creates an instance of the Engine service.
        /// </summary>
        /// <param name="win"></param>
        /// <param name="debugMode"></param>
        public static void CreateEngineService(MainWindow mainWin, bool debugMode)
        {
            _mainWin = mainWin;

            ChessEngineService = new EngineService.EngineProcess(debugMode);
            ChessEngineService.EngineMessage += EngineMessageReceived;
        }

        /// <summary>
        /// Returns true if the engine service is running.
        /// </summary>
        /// <returns></returns>
        public static bool IsEngineAvailable()
        {
            return ChessEngineService.IsEngineReady;
        }

        /// <summary>
        /// Sends a command to the engine.
        /// </summary>
        /// <param name="cmd"></param>
        public static void SendCommand(string cmd)
        {
            AppLog.Message("Tx Command: " + cmd);
            ChessEngineService.SendCommand(cmd);
        }

        /// <summary>
        /// Starts the engine service.
        /// </summary>
        /// <returns></returns>
        public static bool Start()
        {
            return ChessEngineService.StartEngine();
        }

        /// <summary>
        /// Evaluation has finished. Tidy up and reset to prepare for the next evaluation request.
        /// This is called in the evaluation mode as well as during user vs engine game. 
        /// 
        /// This method may be invoked to evaluate a single move whether there is or there is not
        /// a game in progress i.e. AppState.Mode is or is not equal to GAME_VS_COMPUTER.
        /// To distinguish we need to check Evaluation.Mode.
        /// </summary>
        public static void MoveEvaluationFinished()
        {
            // if (AppState.CurrentMode == AppState.Mode.GAME_VS_COMPUTER && AppState.MainWin.Evaluation.Mode == EvaluationState.EvaluationMode.IN_GAME_PLAY)
            if (_mainWin.Evaluation.CurrentMode == EvaluationState.EvaluationMode.ENGINE_GAME)
            {
                ProcessEngineGameMoveEvent();
                _mainWin.Evaluation.PrepareToContinue();

                _mainWin.Timers.Stop(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
                _mainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                _mainWin.ResetEvaluationProgressBar();
                if (TrainingState.IsTrainingInProgress)
                {
                    _mainWin.EngineTrainingGameMoveMade();
                }
            }
            // else if (AppState.CurrentMode == AppState.Mode.TRAINING)
            else if (TrainingState.IsTrainingInProgress)
            {
                // stop the timer, apply training mode specific handling 
                // NOTE do not reset Evaluation.CurrentMode as this will be done 
                // later down the chain
                _mainWin.Timers.Stop(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
                _mainWin.Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                _mainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                _mainWin.ResetEvaluationProgressBar();

                _mainWin.MoveEvaluationFinishedInTraining();
            }
            else
            {
                lock (LearningMode.EvalLock)
                {
                    AppLog.Message("Move evaluation finished for index " + _mainWin.Evaluation.PositionIndex.ToString());

                    string eval = "";
                    if (!string.IsNullOrEmpty(_mainWin.Evaluation.PositionEvaluation))
                    {
                        eval = (_mainWin.Evaluation.PositionEvaluation[0] == '-' ? "" : "+") + _mainWin.Evaluation.PositionEvaluation;
                    }

                    bool isWhiteEval = (_mainWin.Evaluation.PositionIndex - 1) % 2 == 0;
                    int moveIndex = (_mainWin.Evaluation.PositionIndex - 1) / 2;
                    if (isWhiteEval)
                    {
                        _mainWin.ActiveLine.GetMoveAtIndex(moveIndex).WhiteEval = eval;
                    }
                    else
                    {
                        _mainWin.ActiveLine.GetMoveAtIndex(moveIndex).BlackEval = eval;
                    }

                    _mainWin.Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                    _mainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);

                    // if the mode is not FULL_LINE or this is the last move in FULL_LINE
                    // evaluation we stop here
                    // otherwise we start the next move's evaluation
                    if (_mainWin.Evaluation.CurrentMode != EvaluationState.EvaluationMode.MANUAL_LINE
                        || _mainWin.Evaluation.PositionIndex == _mainWin.ActiveLine.GetPlyCount() - 1)
                    {
                        _mainWin.Evaluation.Reset();

                        AppStateManager.ResetEvaluationControls();
                        AppStateManager.ShowMoveEvaluationControls(false, false);
                    }
                    else
                    {
                        AppLog.Message("Continue eval next move after index " + _mainWin.Evaluation.PositionIndex.ToString());
                        _mainWin.Evaluation.PrepareToContinue();

                        _mainWin.Evaluation.PositionIndex++;
                        RequestMoveEvaluation(_mainWin.Evaluation.PositionIndex);

                        _mainWin.Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                    }
                }
            }
        }

        /// <summary>
        /// We have a response from the engine so need to choose
        /// the move from the list of candidates, show it on the board
        /// and display in the Engine Game Line view.
        /// </summary>
        public static void ProcessEngineGameMoveEvent()
        {
            BoardPosition pos = null;

            // NOTE: need to invoke from the Dispatcher here or the program
            // will crash when engine makes a White move (because
            // it will attempt to add an element to the MoveList ObservableCollection
            // from the "wrong" thread)
            _mainWin.Dispatcher.Invoke(() =>
            {
                TreeNode nd;
                pos = EngineGame.ProcessEngineGameMove(out nd);
                SoundPlayer.PlayMoveSound(nd.LastMoveAlgebraicNotation);
                _mainWin.CommentBox.GameMoveMade(nd, false);
            });


            // update the GUI and finish
            // (the app will wait for the user's move)
            _mainWin.DisplayPosition(pos);
            EngineGame.CurrentState = EngineGame.GameState.USER_THINKING;
            _mainWin.Timers.Start(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
            _mainWin.Timers.Stop(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
        }

        /// <summary>
        /// Starts move evaluation on user's request or when continuing line evaluation.
        /// NOTE: does not start evaluation when making a move during a user vs engine game.
        /// </summary>
        /// <param name="posIndex"></param>
        public static void RequestMoveEvaluation(int posIndex)
        {
            _mainWin.Evaluation.PositionIndex = posIndex;
            if (_mainWin.Evaluation.CurrentMode == EvaluationState.EvaluationMode.IDLE)
            {
                _mainWin.Evaluation.CurrentMode = EvaluationState.EvaluationMode.MANUAL_SINGLE_MOVE;
            }
            _mainWin.Evaluation.Position = _mainWin.ActiveLine.GetNodeAtIndex(posIndex).Position;
            _mainWin.DisplayPosition(_mainWin.Evaluation.Position);

            AppStateManager.ShowMoveEvaluationControls(true);
            _mainWin.UpdateLastMoveTextBox(posIndex);

            _mainWin.Timers.Start(AppTimers.TimerId.ENGINE_MESSAGE_POLL);

            string fen = AppStateManager.PrepareMoveEvaluation(_mainWin.Evaluation.Position);
            RequestEngineEvaluation(fen, Configuration.EngineMpv, Configuration.EngineEvaluationTime);
        }

        /// <summary>
        /// This method will be called when in Training mode to evaluate
        /// user's move or moves from the Workbook.
        /// </summary>
        /// <param name="nodeId"></param>
        public static void RequestMoveEvaluationInTraining(int nodeId)
        {
            TreeNode nd = _mainWin.Workbook.GetNodeFromNodeId(nodeId);
            RequestMoveEvaluationInTraining(nd);
        }

        /// <summary>
        /// Requests evaluation while in Training mode.
        /// </summary>
        /// <param name="nd"></param>
        public static void RequestMoveEvaluationInTraining(TreeNode nd)
        {
            _mainWin.Evaluation.Position = nd.Position;
            _mainWin.UpdateLastMoveTextBox(nd);
            AppStateManager.ShowMoveEvaluationControls(true, false);

            _mainWin.Timers.Start(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
            string fen = AppStateManager.PrepareMoveEvaluation(_mainWin.Evaluation.Position);
            RequestEngineEvaluation(fen, Configuration.EngineMpv, Configuration.EngineEvaluationTime);
        }

        /// <summary>
        /// Prepares controls, timers 
        /// and requests the engine to make move.
        /// </summary>
        /// <param name="position"></param>
        public static void RequestEngineMove(BoardPosition position)
        {
            AppStateManager.ChangeEvaluationState(EvaluationState.EvaluationMode.ENGINE_GAME);
            string fen = AppStateManager.PrepareMoveEvaluation(position);
            RequestEngineEvaluation(fen, Configuration.EngineMpv, Configuration.EngineEvaluationTime);
        }


        /// <summary>
        /// Clears the list of candidate moves.
        /// </summary>
        public static void Clear()
        {
            lock (MoveCandidatesLock)
            {
                MoveCandidates.Clear();
            }
        }

        /// <summary>
        /// Starts the timer controlling polling
        /// for engine messages.
        /// </summary>
        public static void StartMessagePollTimer()
        {
            _mainWin.Timers.Start(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
        }

        /// <summary>
        /// Stops the timer controlling polling
        /// for engine messages.
        /// </summary>
        public static void StopMessagePollTimer()
        {
            _mainWin.Timers.Stop(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
        }

        /// <summary>
        /// Sends a sequence of commands to the engine to request evaluation
        /// of the position.
        /// </summary>
        /// <param name="fen">The position to analyze in the FEN format.</param>
        /// <param name="mpv">Number of options to return.</param>
        /// <param name="movetime">Time to think per move (milliseconds)</param>
        public static void RequestEngineEvaluation(string fen, int mpv, int movetime)
        {
            Clear();
            StartMessagePollTimer();
            SendCommand("setoption name multipv value " + mpv.ToString());
            SendCommand("position fen " + fen);
            SendCommand("go movetime " + movetime.ToString());
        }

        /// <summary>
        /// Processes a response from the engine.
        /// We will be interested in messages of these forms:
        /// 1. ongoing evaluation update e.g.: 
        ///     "info depth 10 seldepth 15 multipv 1 score cp -59 nodes 60598 nps 126509 tbhits 0 time 479 pv d7d5 e4d5 d8d5 b1c3 d5a5 d2d4 e7e5 d4d5 a5b4"
        ///     where "multipv" indicates which move, in order of best to worst this is, "score cp" is score in centipawns and "pv" indicates the variation
        ///     examined.
        /// 2. best move message e.g. 
        ///    "bestmove d7d5 ponder e4d5"
        ///    where we will grab the "bestmove" value as the final answer and ignore the "ponder" part (for now, anyway)
        /// </summary>
        /// <param name="message"></param>
        private static void EngineMessageReceived(string message)
        {
            if (message == null)
                return;

            AppLog.Message("Rx Msg: " + message);
            if (message.StartsWith("info"))
            {
                ProcessInfoMessage(message);
            }
            else if (message.StartsWith("bestmove"))
            {
                ProcessBestMoveMessage(message);
            }
        }

        /// <summary>
        /// In response to the "bestmove" message,
        /// flags the completion of evaluation.
        /// </summary>
        /// <param name="message"></param>
        private static void ProcessBestMoveMessage(string message)
        {
            // tell the app that the evaluation has finished
            MoveEvaluationFinished();
        }

        /// <summary>
        /// Parses a text message from the engine and sets properties
        /// of the MoveEvaluation object stored in MoveCandidates list
        /// under the index corresponding to the value of multipv.
        /// </summary>
        /// <param name="message"></param>
        private static void ProcessInfoMessage(string message)
        {
            lock (InfoMessageProcessLock)
            {
                if (IsInfoMessageProcessing)
                    return;
                else
                    IsInfoMessageProcessing = true;
            }

            string[] tokens = message.Split(' ');
            int idx = 0;
            int? multipv = null;
            int? score = null;
            int? movesToMate = null;
            string moves = "";

            string STRING_PV = " pv ";

            while (idx < tokens.Length)
            {
                switch (tokens[idx])
                {
                    case "multipv":
                        idx++;
                        multipv = int.Parse(tokens[idx]);
                        break;
                    case "score":
                        // next token can be "cp" or "mate" followed by an int value
                        ProcessScoreTokens(tokens[idx + 1], tokens[idx + 2], out score, out movesToMate);
                        idx += 2;
                        break;
                    case "pv":
                        int pvIndex = message.IndexOf(STRING_PV);
                        moves = message.Substring(pvIndex + STRING_PV.Length);

                        // force loop exit
                        idx = tokens.Length;
                        break;
                }
                idx++;
            }

            if (multipv != null && (score != null || movesToMate != null))
            {
                // we have updated evaluation
                lock (MoveCandidatesLock)
                {
                    // make sure we have the object to set
                    while (MoveCandidates.Count < multipv)
                    {
                        MoveCandidates.Add(new MoveEvaluation());
                    }

                    MoveCandidates[multipv.Value - 1].Line = moves;
                    if (score != null)
                    {
                        MoveCandidates[multipv.Value - 1].ScoreCp = score.Value;
                    }
                    else
                    {
                        MoveCandidates[multipv.Value - 1].IsMateDetected = true;
                        MoveCandidates[multipv.Value - 1].MovesToMate = movesToMate.Value;
                    }
                }
            }

            lock (InfoMessageProcessLock)
            {
                IsInfoMessageProcessing = false;
            }
        }

        /// <summary>
        /// Processes the 2 tokens following the "score" token.
        /// We will only process it if the value of the first token is "cp" or "mate".
        /// </summary>
        private static void ProcessScoreTokens(string firstToken, string secondToken, out int? score, out int? movesToMate)
        {
            score = null;
            movesToMate = null;

            int iVal;

            if (firstToken == "cp")
            {
                if (int.TryParse(secondToken, out iVal))
                {
                    score = iVal;
                }
            }
            else if (firstToken == "mate")
            {
                if (int.TryParse(secondToken, out iVal))
                {
                    movesToMate = iVal;
                }
            }
        }

    }
}

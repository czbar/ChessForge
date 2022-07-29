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

        /// <summary>
        /// Creates an instance of the Engine service.
        /// </summary>
        /// <param name="win"></param>
        /// <param name="debugMode"></param>
        public static void CreateEngineService(bool debugMode)
        {
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
            if (AppState.MainWin.Evaluation.Mode == EvaluationState.EvaluationMode.IN_GAME_PLAY)
            {
                ProcessEngineGameMoveEvent();
                AppState.MainWin.Evaluation.PrepareToContinue();

                AppState.MainWin.Timers.Stop(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
                AppState.MainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                AppState.MainWin.ResetEvaluationProgressBar();
                if (TrainingState.IsTrainingInProgress)
                {
                    AppState.MainWin.EngineTrainingGameMoveMade();
                }
            }
            // else if (AppState.CurrentMode == AppState.Mode.TRAINING)
            else if (TrainingState.IsTrainingInProgress)
            {
                // stop the timer, reset mode and apply training mode specific handling 
                AppState.MainWin.Evaluation.Mode = EvaluationState.EvaluationMode.IDLE;
                AppState.MainWin.Timers.Stop(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
                AppState.MainWin.Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                AppState.MainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);
                AppState.MainWin.ResetEvaluationProgressBar();

                AppState.MainWin.MoveEvaluationFinishedInTraining();
            }
            else
            {
                lock (AppState.EvalLock)
                {
                    AppLog.Message("Move evaluation finished for index " + AppState.MainWin.Evaluation.PositionIndex.ToString());

                    string eval = "";
                    if (!string.IsNullOrEmpty(AppState.MainWin.Evaluation.PositionEvaluation))
                    {
                        eval = (AppState.MainWin.Evaluation.PositionEvaluation[0] == '-' ? "" : "+") + AppState.MainWin.Evaluation.PositionEvaluation;
                    }

                    bool isWhiteEval = (AppState.MainWin.Evaluation.PositionIndex - 1) % 2 == 0;
                    int moveIndex = (AppState.MainWin.Evaluation.PositionIndex - 1) / 2;
                    if (isWhiteEval)
                    {
                        AppState.MainWin.ActiveLine.GetMoveAtIndex(moveIndex).WhiteEval = eval;
                    }
                    else
                    {
                        AppState.MainWin.ActiveLine.GetMoveAtIndex(moveIndex).BlackEval = eval;
                    }

                    AppState.MainWin.Timers.Stop(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
                    AppState.MainWin.Timers.Stop(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);

                    // if the mode is not FULL_LINE or this is the last move in FULL_LINE
                    // evaluation we stop here
                    // otherwise we start the next move's evaluation
                    if (AppState.MainWin.Evaluation.Mode != EvaluationState.EvaluationMode.FULL_LINE
                        || AppState.MainWin.Evaluation.PositionIndex == AppState.MainWin.ActiveLine.GetPlyCount() - 1)
                    {
                        AppState.MainWin.Evaluation.Reset();

                        AppState.MainWin.ResetEvaluationControls();
                        AppState.MainWin.ShowMoveEvaluationControls(false, false);
                    }
                    else
                    {
                        AppLog.Message("Continue eval next move after index " + AppState.MainWin.Evaluation.PositionIndex.ToString());
                        AppState.MainWin.Evaluation.PrepareToContinue();

                        AppState.MainWin.Evaluation.PositionIndex++;
                        RequestMoveEvaluation(AppState.MainWin.Evaluation.PositionIndex, AppState.MainWin.Evaluation.Mode, false);

                        AppState.MainWin.Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
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
            AppState.MainWin.Dispatcher.Invoke(() =>
            {
                TreeNode nd;
                pos = EngineGame.ProcessEngineGameMove(out nd);
                SoundPlayer.PlayMoveSound(nd.LastMoveAlgebraicNotation);
                AppState.MainWin.CommentBox.GameMoveMade(nd, false);
            });


            // update the GUI and finish
            // (the app will wait for the user's move)
            AppState.MainWin.DisplayPosition(pos);
            EngineGame.State = EngineGame.GameState.USER_THINKING;
            AppState.MainWin.Timers.Start(AppTimers.TimerId.CHECK_FOR_USER_MOVE);
            AppState.MainWin.Timers.Stop(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
        }

        /// <summary>
        /// Starts move evaluation on user's request or when continuing line evaluation.
        /// NOTE: does not start evaluation when making a move during a user vs engine game.
        /// </summary>
        /// <param name="posIndex"></param>
        /// <param name="mode"></param>
        /// <param name="isLineStart"></param>
        public static void RequestMoveEvaluation(int posIndex, EvaluationState.EvaluationMode mode, bool isLineStart)
        {
            AppState.MainWin.Evaluation.PositionIndex = posIndex;
            AppState.MainWin.Evaluation.Position = AppState.MainWin.ActiveLine.GetNodeAtIndex(posIndex).Position;
            AppState.MainWin.DisplayPosition(AppState.MainWin.Evaluation.Position);

            AppState.MainWin.ShowMoveEvaluationControls(true, isLineStart);
            AppState.MainWin.UpdateLastMoveTextBox(posIndex, isLineStart);

            AppState.MainWin.Timers.Start(AppTimers.TimerId.ENGINE_MESSAGE_POLL);

            PrepareMoveEvaluation(mode, AppState.MainWin.Evaluation.Position);
        }

        /// <summary>
        /// This method will be called when in Training mode to evaluate
        /// user's move or moves from the Workbook.
        /// </summary>
        /// <param name="nodeId"></param>
        public static void RequestMoveEvaluationInTraining(int nodeId)
        {
            TreeNode nd = AppState.MainWin.Workbook.GetNodeFromNodeId(nodeId);
            RequestMoveEvaluationInTraining(nd);
        }

        /// <summary>
        /// Requests evaluation while in Training mode.
        /// </summary>
        /// <param name="nd"></param>
        public static void RequestMoveEvaluationInTraining(TreeNode nd)
        {
            AppState.MainWin.Evaluation.Position = nd.Position;
            AppState.MainWin.Evaluation.Mode = EvaluationState.EvaluationMode.SINGLE_MOVE;
            AppState.MainWin.UpdateLastMoveTextBox(nd, true);
            AppState.MainWin.ShowMoveEvaluationControls(true, false);

            AppState.MainWin.Timers.Start(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
            PrepareMoveEvaluation(EvaluationState.EvaluationMode.IN_TRAINING, AppState.MainWin.Evaluation.Position);
        }

        /// <summary>
        /// Prepares controls, timers 
        /// and requests the engine to make move.
        /// </summary>
        /// <param name="position"></param>
        public static void RequestEngineMove(BoardPosition position)
        {
            PrepareMoveEvaluation(EvaluationState.EvaluationMode.IN_GAME_PLAY, position);
        }


        public static void Clear()
        {
            lock (MoveCandidatesLock)
            {
                MoveCandidates.Clear();
            }
        }

        public static void StartMessagePollTimer()
        {
            AppState.MainWin.Timers.Start(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
        }

        public static void StopMessagePollTimer()
        {
            AppState.MainWin.Timers.Stop(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
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

        /// <summary>
        /// Preparations for move evaluation that are common for Position/Line 
        /// evaluation as well as requesting engine move in a game.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="position"></param>
        private static void PrepareMoveEvaluation(EvaluationState.EvaluationMode mode, BoardPosition position)
        {
            AppState.MainWin.Evaluation.Mode = mode;

            AppState.MainWin.PrepareEvaluationControls();

            AppState.MainWin.Timers.Start(AppTimers.TimerId.EVALUATION_LINE_DISPLAY);
            AppState.MainWin.Timers.Start(AppTimers.StopwatchId.EVALUATION_ELAPSED_TIME);

            string fen = FenParser.GenerateFenFromPosition(position);
            RequestEngineEvaluation(fen, Configuration.EngineMpv, Configuration.EngineEvaluationTime);
        }

    }
}

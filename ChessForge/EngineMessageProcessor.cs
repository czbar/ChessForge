using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Handles messages received from the chess engine.
    /// There will be one instance of this class created 
    /// in the Main Window lasting the whole session. 
    /// </summary>
    public class EngineMessageProcessor
    {

        public static MainWindow MainWin;

        public static void CreateEngineService(MainWindow win, bool debugMode)
        {
            MainWin = win;
            ChessEngineService = new EngineService.EngineProcess(debugMode);
            ChessEngineService.EngineMessage += EngineMessageReceived;
        }

        // an instance of the engine service
        public static EngineService.EngineProcess ChessEngineService;

        /// <summary>
        /// The list of candidates returned by the engine.
        /// </summary>
        public static List<MoveEvaluation> MoveCandidates = new List<MoveEvaluation>();

        public static object MoveCandidatesLock = new object();
        public static object InfoMessageProcessLock = new object();

        public static bool IsInfoMessageProcessing = false;

        public static void Clear()
        {
            lock (MoveCandidatesLock)
            {
                MoveCandidates.Clear();
            }
        }

        public static void StartMessagePollTimer()
        {
            MainWin.Timers.Start(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
        }

        public static void StopMessagePollTimer()
        {
            MainWin.Timers.Stop(AppTimers.TimerId.ENGINE_MESSAGE_POLL);
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

        public static bool IsEngineAvailable()
        {
            return ChessEngineService.IsEngineReady;
        }

        public static void SendCommand(string cmd)
        {
            AppLog.Message("Tx Command: " + cmd);
            ChessEngineService.SendCommand(cmd);
        }

        public static bool Start()
        {
            return ChessEngineService.StartEngine();
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
            MainWin.MoveEvaluationFinished();
        }

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
                        // next token should be cp and only then the value
                        idx += 2;
                        score = int.Parse(tokens[idx]);
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

            if (multipv != null && score != null)
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
                    MoveCandidates[multipv.Value - 1].ScoreCp = score.Value;
                }
            }

            lock (InfoMessageProcessLock)
            {
                IsInfoMessageProcessing = false;
            }
        }

    }
}

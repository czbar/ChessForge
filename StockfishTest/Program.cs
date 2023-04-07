using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ChessPosition;
using GameTree;
using EngineService;

namespace StockfishTest
{
    /// <summary>
    /// Use this to test engine behavior under different circumstances. 
    /// </summary>
    class Program
    {
        private static EngineService.EngineProcess engine;
        private static bool IsEngineReady = false;
        private static bool IsMultiPvMode = true;

        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("Starting engine...");
            engine = new EngineService.EngineProcess();
            engine.EngineMessage += EngineMessage;

            // start the engine
            StartStockfishEngine(engine);

            Console.WriteLine("Engine Ready");

            // Send a few requests one after another and check the responses in the log files
            AnalyzePosition("5rk1/2p1bppp/p1p1pn2/PQ6/3P4/6P1/NP2PP1P/2BR2K1 b k - 0 18");
            Console.WriteLine("StartsSleep");
            Thread.Sleep(500);
            Console.WriteLine("EndSleep");

            AnalyzePosition("rnbq1rk1/pp2bppp/5n2/2pp2B1/3P4/3B1Q1P/PPP2PP1/RN2K1NR w KQ c6 0 7");
            Console.WriteLine("StartsSleep");
            Thread.Sleep(500);
            Console.WriteLine("EndSleep");

            AnalyzePosition("rnbq1rk1/pp2bppp/5n2/2Pp2B1/8/3B1Q1P/PPP2PP1/RN2K1NR b KQ - 0 8");
            Console.WriteLine("StartsSleep");
            Thread.Sleep(500);
            Console.WriteLine("EndSleep");

            AnalyzePosition("5rk1/2p1bppp/p1p1pn2/PQ6/3P4/6P1/NP2PP1P/2BR2K1 b k - 0 18");
            Console.WriteLine("StartsSleep");
            Thread.Sleep(500);
            Console.WriteLine("EndSleep");

            AnalyzePosition("r1bq1rk1/pp1nbppp/5n2/2Pp2B1/8/3B1Q1P/PPP2PP1/RN2K1NR w KQ - 1 8");
            Console.WriteLine("StartsSleep");
            Thread.Sleep(500);
            Console.WriteLine("EndSleep");

            AnalyzePosition("5rk1/2p1bppp/p1p1pn2/PQ6/3P4/6P1/NP2PP1P/2BR2K1 b k - 0 18");
            Console.WriteLine("StartsSleep");
            Thread.Sleep(500);

            Console.WriteLine("Final Sleep");
            Thread.Sleep(1000);

            EngineLog.Dump("enginelog.txt");

            engine.StopEngine();

            Console.WriteLine("Finished");

            Environment.Exit(0);
        }

        /// <summary>
        /// Sends FEN for analysis
        /// </summary>
        /// <param name="fen"></param>
        private static void AnalyzePosition(string fen)
        {
            //engine.DebugSendCommand("stop");
            //engine.SendCommand("position fen " + fen);
            //engine.SendCommand("go");
        }

        /// <summary>
        /// Starts the engine and waits for the reaydok message
        /// </summary>
        /// <param name="engine"></param>
        private static void StartStockfishEngine(EngineService.EngineProcess engine)
        {
            engine.StartEngine("stockfish_15_x86_64.exe", null);

            // wait for "readyok"
            while (true)
            {
                Thread.Sleep(100);
                if (engine.IsEngineReady)
                    break;
            }

            if (IsMultiPvMode)
            {
//                engine.SendCommand("setoption name multipv value 5");
            }
            else
            {
//                engine.SendCommand("setoption name multipv value 1");
            }
        }

        /// <summary>
        /// Sets IsEngineReady if the message is "readyok"
        /// </summary>
        /// <param name="message"></param>
        private static void EngineMessage(string message)
        {
            if (!IsEngineReady)
            {
                if (message.Contains("readyok"))
                {
                    IsEngineReady = true;
                }
            }
        }
    }

}
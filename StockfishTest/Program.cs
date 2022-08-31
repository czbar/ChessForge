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
    class Program
    {
        private static StringBuilder txtMessageFile = new StringBuilder();

        private static EngineService.EngineProcess engine;
        private static MoveEval[] mev = new MoveEval[5];
        private static bool IsEngineReady = false;

        private static bool IsMultiPvMode = true;
        private static bool AnalysisInProgress = false;

        private static StringBuilder sbAllText = new StringBuilder();

        private static string BestMove;

        static void Main(string[] args)
        {
            engine = new EngineService.EngineProcess(true, "");
            engine.EngineMessage += EngineMessage;

            StartStockfishEngine(engine);

            AnalyzePosition("5rk1/2p1bppp/p1p1pn2/PQ6/3P4/6P1/NP2PP1P/2BR2K1 b k - 0 18");

            //EvaluatePgnGame();

            EngineLog.Dump(null);

            //Console.WriteLine(sbAllText.ToString());

            //File.WriteAllText("EngineMessages.txt", txtMessageFile.ToString());

        }

        private static void StartStockfishEngine(EngineService.EngineProcess engine)
        {
            engine.StartEngine("stockfish_15_x64_avx2.exe");

            // wait for "readyok"
            while (true)
            {
                Thread.Sleep(100);
                if (engine.IsEngineReady)
                    break;
            }

            if (IsMultiPvMode)
            {
                engine.SendCommand("setoption name multipv value 5");
            }
            else
            {
                engine.SendCommand("setoption name multipv value 1");
            }

            

        }

        private static void EvaluatePgnGame()
        {
            WorkbookTree variationTree = new WorkbookTree();
            string gameText = File.ReadAllText("../../../ChessPositionTest/TestData/GameShort.pgn");
            PgnGameParser pgnGame = new PgnGameParser(gameText, variationTree, out bool multi, false);
            EvaluateVariationTree(variationTree);
        }

        private static void EvaluateVariationTree(WorkbookTree tree)
        {
            TreeNode root = tree.Nodes[0];
            EvaluateTreeLine(root);
        }

        private static void EvaluateTreeLine(TreeNode nd, bool includeNumber = false)
        {
            while (true)
            {
                // if the node has no children,
                // print it and return 
                if (nd.Children.Count == 0)
                {
                    return;
                }

                // if the node has 1 child, print it,
                // call this method on the child
                if (nd.Children.Count == 1)
                {
                    EvaluateNode(nd.Children[0], includeNumber);
                    EvaluateTreeLine(nd.Children[0]);
                    return;
                }

                // if the node has more than 1 child
                // call this method on each sibling except
                // the first one, before calling it on the 
                // first one.
                if (nd.Children.Count > 1)
                {
                    EvaluateNode(nd.Children[0], includeNumber);
                    for (int i = 1; i < nd.Children.Count; i++)
                    {
                        sbAllText.Append(" (");
                        EvaluateNode(nd.Children[i], true);
                        EvaluateTreeLine(nd.Children[i]);
                        sbAllText.Append(") ");
                    }
                    EvaluateTreeLine(nd.Children[0], true);
                    return;
                }
            }
        }
        private static void EvaluateNode(TreeNode nd, bool includeNumber)
        {
            if (nd.Position.ColorToMove == PieceColor.Black)
            {
                if (!includeNumber && nd.Position.MoveNumber != 1)
                {
                    sbAllText.Append(" ");
                }
                sbAllText.Append(nd.Position.MoveNumber.ToString() + ".");
            }

            if (nd.Position.ColorToMove == PieceColor.White && includeNumber)
            {
                sbAllText.Append(nd.Position.MoveNumber.ToString() + "...");
            }

            sbAllText.Append(" " + nd.LastMoveAlgebraicNotation);

            AnalyzePosition(FenParser.GenerateFenFromPosition(nd.Position));
        }

        private static void AnalyzePosition(string fen)
        {
            AnalysisInProgress = true;
            //fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            txtMessageFile.Append(Environment.NewLine);
            txtMessageFile.Append("FEN: " + fen + Environment.NewLine);
            engine.SendCommand("position fen " + fen);
            engine.SendCommand("go movetime 1000");

            Console.WriteLine("Position: " + fen);

            while (AnalysisInProgress)
            {
                Thread.Sleep(10);
            }

            sbAllText.Append(" [eval: " + mev[0].CpEval.ToString() + " best move: " + mev[0].Move + "]");

            //Console.WriteLine(" [eval: " + mev[0].CpEval.ToString() + " best move: " + mev[0].Move + "]");
            //Console.WriteLine("");

 //           engine.Kill();
 //           engine.StartEngine();

        }

        private static void CheckEngineReady()
        {
            IsEngineReady = false;
            engine.SendCommand("isready");
            while (true)
            {
                if (IsEngineReady)
                    break;

                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Process messages giving us the scores
        /// </summary>
        /// <param name="message"></param>
        private static void EngineMessage(string message)
        {
            int pvIndex = 0;
            int cpScore = 0;
            string move = "";

            if (message != null)
            {
                txtMessageFile.Append(Environment.NewLine);
                txtMessageFile.Append("Engine: " + message + Environment.NewLine);
            }

            if (!IsEngineReady)
            {
                if (message.Contains("readyok"))
                {
                    IsEngineReady = true;
                }
            }

            if (message.Contains("score")) // Message is in the form: "info depth 1 seldepth 1 multipv 1 score cp 127 nodes 169 nps 84500 tbhits 0 time 2 pv d1d4"
            {
                string[] tokens = message.Split(' ');
                if (IsMultiPvMode)
                {
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        if (tokens[i] == "multipv")
                        {
                            pvIndex = int.Parse(tokens[i + 1]);
                        }
                        else if (tokens[i] == "score")
                        {
                            cpScore = int.Parse(tokens[i + 2]);
                        }
                        else if (tokens[i] == "pv")
                        {
                            move = tokens[i + 1];
                            mev[pvIndex - 1].Move = move;
                            mev[pvIndex - 1].CpEval = cpScore;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        if (tokens[i] == "score")
                        {
                            // the next token MUST be cp
                            // TODO handle other tokens esp. "mate"!
                            if (tokens[i + 1] == "cp")
                            {
                                cpScore = int.Parse(tokens[i + 2]);
                            }
                        }
                        else if (tokens[i] == "pv")
                        {
                            move = tokens[i + 1];
                            mev[0].Move = move;
                            mev[0].CpEval = -1 * cpScore;
                        }
                    }
                }
            }
            else if (message.Contains("bestmove"))
            {
                // we are done
                string[] tokens = message.Split(' ');
                BestMove = tokens[1];

                if (BestMove != mev[0].Move)
                {
//                    throw new Exception("best move != pv move");
                }

                AnalysisInProgress = false;
            }
        }

        static public void SendMovesToEngine(string moves, short time)
        {
            var command = UciCommands.ENG_POSITION_STARTPOS + " " + moves;
            engine.SendCommand(command);
            command = UciCommands.ENG_GO_MOVE_TIME + " " + time.ToString();
            engine.SendCommand(command);
        }


    }

    public struct MoveEval
    {
        //public MoveEval(string move, int cpEval)
        //{
        //    Move = move;
        //    CpEval = cpEval;
        //}

        public string Move;
        public int CpEval;
    }

}
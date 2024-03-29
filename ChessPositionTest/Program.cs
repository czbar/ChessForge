﻿using System;
using System.IO;
using System.Collections.Generic;
using ChessPosition;
using GameTree;
using System.Xml;
using ChessPosition.GameTree;
using System.Threading.Tasks;
using WebAccess;
using System.Diagnostics;
using System.Linq;
using System.Collections;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;

namespace ChessPositionTest
{
    class Program
    {
        /// <summary>
        /// Blunder detection tests.
        /// </summary>
        private static List<PositionEvaluation> positionEvaluations = new List<PositionEvaluation>()
        {
             new PositionEvaluation(1, 5,  false, true),
             new PositionEvaluation(5, 1,  true, false),
             new PositionEvaluation(1, -5, true, false),
             new PositionEvaluation(0, 5,  false, true),
             new PositionEvaluation(0, -5, true, false),
             new PositionEvaluation(-1, -5, true, false),
             new PositionEvaluation(-5, -1, false, true),
             new PositionEvaluation(-1, 5, false, true),
             new PositionEvaluation(+7, +10, false, false),
             new PositionEvaluation(+10, +7, false, false),
             new PositionEvaluation(-7, -10, false, false),
             new PositionEvaluation(-10, -7, false, false),
             new PositionEvaluation(-7, -4, false, true),
             new PositionEvaluation(7, 4, true, false),
        };

        /// <summary>
        /// Positions to test
        /// </summary>
        private static List<TestPosition> TestPositions = new List<TestPosition>()
        {
             new TestPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1","")
            ,new TestPosition("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1","")
            ,new TestPosition("r1r3k1/p2bppbp/3p1np1/q2Nn2P/1p1NP1P1/1B2BP2/PPPQ4/2KR3R b - - 1 15", "c6")
        };

        // high threshold for blunder detection
        private static double HIGH_EVAL_THRESH = 5;

        /// <summary>
        /// Blunder detection logic
        /// </summary>
        /// <param name="prevEval"></param>
        /// <param name="currEval"></param>
        /// <param name="colorToMove"></param>
        /// <returns></returns>
        private static bool IsBlunder(double prevEval, double currEval, PieceColor colorToMove)
        {
            bool res = false;

            if (colorToMove == PieceColor.White)
            {
                if ((currEval - prevEval < -2) && ((Math.Abs(prevEval) <= HIGH_EVAL_THRESH) || Math.Abs(currEval) <= HIGH_EVAL_THRESH))
                {
                    res = true;
                }
            }
            else
            {
                if ((currEval - prevEval > 2) && ((Math.Abs(prevEval) <= HIGH_EVAL_THRESH) || Math.Abs(currEval) <= HIGH_EVAL_THRESH))
                {
                    res = true;
                }
            }

            return res;
        }

        /// <summary>
        /// Executes blunder detection tests.
        /// </summary>
        private static void RunEvaluationTest()
        {
            Console.WriteLine("#### EVALUATION TEST ####");

            for(int i = 0; i < positionEvaluations.Count; i++)
            {
                Console.WriteLine("@TEST " + (i+1).ToString() + ": ");
                var posEval = positionEvaluations[i];
                Console.WriteLine("Previous Position Eval = " + posEval.PreviousPositionEvaluation);
                Console.WriteLine("Current  Position Eval = " + posEval.CurrentPositionEvaluation);
                bool isWhiteBlunder = IsBlunder(posEval.PreviousPositionEvaluation, posEval.CurrentPositionEvaluation, PieceColor.White);
                bool isBlackBlunder = IsBlunder(posEval.PreviousPositionEvaluation, posEval.CurrentPositionEvaluation, PieceColor.Black);

                if (isWhiteBlunder == posEval.BlunderIfWhiteMove)
                {
                    Console.WriteLine("PASS for WHITE on move: " + (isWhiteBlunder ? "[??]" : "[--]"));
                }
                else
                {
                    Console.WriteLine("FAIL for WHITE on move: " + (isWhiteBlunder ? "[??]" : "[--]") + "  EXPECTED " + (posEval.BlunderIfWhiteMove ? "[??]" : "[--]"));
                }

                if (isBlackBlunder == posEval.BlunderIfBlackMove)
                {
                    Console.WriteLine("PASS for BLACK on move: " + (isBlackBlunder ? "[??]" : "[--]"));
                }
                else
                {
                    Console.WriteLine("FAIL for BLACK on move: " + (isBlackBlunder ? "[??]" : "[--]") + "  EXPECTED " + (posEval.BlunderIfBlackMove ? "[??]" : "[--]"));
                }
                Console.WriteLine("");
            }
        }

        private static bool IsChessMove(string move)
        {
            Regex urlRegex = new Regex(@"^(\d+\.|(\d+\.\.\.)|) ?((([NBRQK][a-h1-8]?|[a-h])?[1-8]?)|(O-O(-O)?))(=[NBRQ])?([+#])?$", RegexOptions.Compiled);

            MatchCollection matches = urlRegex.Matches(move);
            return matches.Count > 0;
        }

        /// <summary>
        /// Tree to build.
        /// </summary>
        private static VariationTree _treeOut;

        static Stopwatch watch = new System.Diagnostics.Stopwatch();

        /// <summary>
        /// Tests aspects of variation trees and text parsing.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            RunEvaluationTest();

            bool moveFound = IsChessMove("Nf3");
            moveFound = IsChessMove("1. Nf3");
            moveFound = IsChessMove("2... e5");
            moveFound = IsChessMove("e4");
            moveFound = IsChessMove("O-O");
            moveFound = IsChessMove("O-O-O");

            TreeNode nd1 = new TreeNode(null, "", 0);
            FenParser.ParseFenIntoBoard("rnbqkb1r/ppp1pp1p/5np1/3p4/2PP4/2N5/PP2PPPP/R1BQKBNR w KQkq - 0 3", ref nd1.Position);

            TreeNode nd2 = new TreeNode(null, "", 0);
            FenParser.ParseFenIntoBoard("rnbqkb1r/ppp1pp1p/5np1/3p4/2PP4/2N5/PP2PPPP/R1BQKBNR w KQkq - 0 4", ref nd2.Position);

            string fen = "";
            int hash1 = 0;
            int hash2 = 0;

            for (int i = 0; i < 10000; i++)
            {
                fen = FenParser.GenerateFenFromPosition(nd1.Position);
                hash1 = fen.GetHashCode();
                fen = FenParser.GenerateFenFromPosition(nd2.Position);
                hash2 = fen.GetHashCode();
            }


            var data1 = nd1.Position.Board;
            var data2 = nd2.Position.Board;

            for (int i = 0; i < 100000; i++)
            {
                var equal = data1.Cast<byte>().SequenceEqual(data2.Cast<byte>());
            }

            Console.WriteLine("Sending Request");
            watch.Start();

            WebAccess.OpeningExplorer.RequestOpeningStats(0, nd1);

            //TestTreeMerge();
            //TestPgnGameParser();
            //TestFenParser();
            //            Console.ReadLine();
        }

        public static void OpeningStatsReceived(object sender, WebAccessEventArgs e)
        {
            watch.Stop();
            Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");
            LichessOpeningsStats stats = e.OpeningStats;
            Console.WriteLine(stats.Opening.Name);
            foreach (WebAccess.LichessMoveStats move in stats.Moves)
            {
                Console.WriteLine(move.San + "   :   " + move.White.ToString() + " : " + move.Draws.ToString() + " : " + move.Black.ToString());
            }
        }

        /// <summary>
        /// Reads in 2 PGN files, parses them into a tree,
        /// performs a merge and writes the result out.
        /// </summary>
        private static void TestTreeMerge()
        {
            VariationTree tree1 = BuildTreeFromFile("../../../ChessPositionTest/TestData/TestMerge_1.pgn");
            VariationTree tree2 = BuildTreeFromFile("../../../ChessPositionTest/TestData/TestMerge_2.pgn");

            _treeOut = new VariationTree(GameData.ContentType.STUDY_TREE);
            _treeOut.AddNode(new TreeNode(null, "", 0));

            MergeTrees(tree1.Nodes[0], tree2.Nodes[0], _treeOut.Nodes[0]);
            PrintVariationTree(_treeOut);
        }

        /// <summary>
        /// Build a WorkbookTree object from a PGN file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static VariationTree BuildTreeFromFile(string fileName)
        {
            VariationTree tree = new VariationTree(GameData.ContentType.STUDY_TREE);
            string gameText = File.ReadAllText(fileName);
            PgnGameParser pgnGame = new PgnGameParser(gameText, tree, "", false);

            return tree;
        }

        private static Dictionary<string, TreeNode> _dict1 = new Dictionary<string, TreeNode>();
        private static Dictionary<string, TreeNode> _dict2 = new Dictionary<string, TreeNode>();

        /// <summary>
        /// Get all children of each node and add them the output tree
        /// making sure we don't add duplicates.
        /// Where the node is found in only one of the trees, add the entire subtree,
        /// otherwise add the node and call this method recursively on all such nodes.
        /// </summary>
        /// <param name="tn1"></param>
        /// <param name="tn2"></param>
        private static void MergeTrees(TreeNode tn1, TreeNode tn2, TreeNode outParent)
        {
            // node tn1 and tn2 are already added (as one node since they were matched)

            _dict1.Clear();
            _dict2.Clear();

            // add all children of tn1 to the dictionary
            foreach (TreeNode nd in tn1.Children)
            {
                _dict1[FenParser.GenerateFenFromPosition(nd.Position)] = nd;
            }

            // add all children of tn2 to the dictionary
            foreach (TreeNode nd in tn2.Children)
            {
                _dict2[FenParser.GenerateFenFromPosition(nd.Position)] = nd;
            }

            // place the ones in both dictionaries to one side and remove from dictionaries
            List<TreeNode> dupes1 = new List<TreeNode>();
            List<TreeNode> dupes2 = new List<TreeNode>();
            List<string> dupeFens = new List<string>();

            foreach (string fen in _dict1.Keys)
            {
                if (_dict2.ContainsKey(fen))
                {
                    dupes1.Add(_dict1[fen]);
                    dupes2.Add(_dict2[fen]);
                    dupeFens.Add(fen);
                }
            }
            foreach (string fen in dupeFens)
            {
                _dict1.Remove(fen);
                _dict2.Remove(fen);
            }

            // now the dictionaries contain unique nodes
            foreach (TreeNode nd in _dict1.Values)
            {
                TreeNode outNode = InsertNode(nd, outParent);
                _treeOut.InsertSubtree(outNode, nd);
            }
            foreach (TreeNode nd in _dict2.Values)
            {
                TreeNode outNode = InsertNode(nd, outParent);
                _treeOut.InsertSubtree(outNode, nd);
            }

            // for those in dupes, and a new node to the output tree 
            // and call this method recursively
            for (int i = 0; i < dupes1.Count; i++)
            {
                TreeNode outNode = InsertNode(dupes1[i], outParent);

                MergeTrees(dupes1[i], dupes2[i], outNode);
            }
        }

        private static TreeNode InsertNode(TreeNode nd, TreeNode outParent)
        {
            TreeNode outNode = nd.CloneMe(true);
            outNode.Parent = outParent;
            outParent.AddChild(outNode);
            outNode.NodeId = _treeOut.GetNewNodeId();

            _treeOut.AddNode(outNode);

            return outNode;
        }

        /// <summary>
        /// Tests parsing a game text. Writes readable output to STDOUT
        /// </summary>
        private static void TestPgnGameParser()
        {
            VariationTree variationTree = new VariationTree(GameData.ContentType.STUDY_TREE);
            string gameText = File.ReadAllText("../../../ChessPositionTest/TestData/GameTreeTest_1.pgn");
            PgnGameParser pgnGame = new PgnGameParser(gameText, variationTree, "", false);
            PrintVariationTree(variationTree);
        }

        /// <summary>
        /// Tests the FEN parser by reading in FEN strings converting them
        /// to ChessForge position format and then back out to FEN.
        /// Compares the input and output FEN.
        /// </summary>
        private static void TestFenParser()
        {
            foreach (TestPosition s in TestPositions)
            {
                BoardPosition pos = new BoardPosition();
                FenParser.ParseFenIntoBoard(s.Fen, ref pos);
                DebugUtils.PrintPosition(pos, s.Fen);

                string fen = FenParser.GenerateFenFromPosition(pos);

                Console.WriteLine("Original FEN");
                Console.WriteLine("   " + s.Fen);
                Console.WriteLine("");
                Console.Write("Regenerated FEN ");
                if (s.Fen == fen)
                {
                    Console.WriteLine(" identical to the original: TEST PASS");
                }
                else
                {
                    Console.WriteLine(" different from the original: TEST FAIL !!!");
                }
                Console.WriteLine("   " + fen);
                Console.WriteLine("");

                if (s.Square.Length == 2)
                {
                    GetSquareAttackers("c6", PositionUtils.SideToMove(pos), ref pos);
                }
            }
        }

        /// <summary>
        /// Tests identification of pieces attacking a certain square in a certain position.
        /// </summary>
        /// <param name="algSquare"></param>
        /// <param name="col"></param>
        /// <param name="testBoard"></param>
        private static void GetSquareAttackers(string algSquare, PieceColor col, ref BoardPosition testBoard)
        {
            SquareCoords coords = PositionUtils.ConvertAlgebraicToXY(algSquare);
            PiecesTargetingSquare sa = new PiecesTargetingSquare((byte)coords.Xcoord, (byte)coords.Ycoord, -1, -1, col, ref testBoard);

            DebugUtils.PrintAttackers(algSquare, col, sa.Candidates);
        }

        /// <summary>
        /// Prints the entire tree to STDOUT
        /// </summary>
        /// <param name="tree"></param>
        private static void PrintVariationTree(VariationTree tree)
        {
            Console.WriteLine("");
            Console.WriteLine("VARIATION TREE");
            Console.WriteLine("==============");

            if (tree.Nodes.Count == 0)
            {
                Console.WriteLine("The Variation Tree is empty!");
                return;
            }

            TreeNode root = tree.Nodes[0];
            PrintTreeLine(root);
            Console.WriteLine("");
            Console.WriteLine("==============");
            Console.WriteLine("");
        }

        /// <summary>
        /// Prints a line to STDOUT
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeNumber"></param>
        private static void PrintTreeLine(TreeNode nd, bool includeNumber = false)
        {
            while (true)
            {
                // if the node has no children,
                // print it and return 
                if (nd.Children.Count == 0)
                {
                    //                    Console.Write(")");
                    return;
                }

                // if the node has 1 child, print it,
                // call this method on the child
                if (nd.Children.Count == 1)
                {
                    PrintNode(nd.Children[0], includeNumber);
                    PrintTreeLine(nd.Children[0]);
                    return;
                }

                // if the node has more than 1 child
                // call this method on each sibling except
                // the first one, before calling it on the 
                // first one.
                if (nd.Children.Count > 1)
                {
                    PrintNode(nd.Children[0], includeNumber);
                    for (int i = 1; i < nd.Children.Count; i++)
                    {
                        Console.Write(" (");
                        PrintNode(nd.Children[i], true);
                        PrintTreeLine(nd.Children[i]);
                        Console.Write(") ");
                    }
                    PrintTreeLine(nd.Children[0], true);
                    return;
                }
            }
        }

        /// <summary>
        /// Prints a single node to STDOUT
        /// </summary>
        /// <param name="nd"></param>
        /// <param name="includeNumber"></param>
        private static void PrintNode(TreeNode nd, bool includeNumber)
        {
            if (nd.Position.ColorToMove == PieceColor.Black)
            {
                if (!includeNumber && nd.Position.MoveNumber != 1)
                {
                    Console.Write(" ");
                }
                Console.Write(nd.Position.MoveNumber.ToString() + ".");
            }

            if (nd.Position.ColorToMove == PieceColor.White && includeNumber)
            {
                Console.Write(nd.Position.MoveNumber.ToString() + "...");
            }

            Console.Write(" " + nd.LastMoveAlgebraicNotation);
        }

    }

    /// <summary>
    /// A structure holding FEN and square to test together
    /// </summary>
    class TestPosition
    {
        public TestPosition(string fen, string square)
        {
            Fen = fen;
            Square = square;
        }
        public string Fen;
        public string Square;
    }

    class PositionEvaluation
    {
        public PositionEvaluation(double previousPositionEvaluation, double currentPositionEvaluation, bool blunderIfWhiteMove, bool blunderIfBlackMove)
        {
            CurrentPositionEvaluation = currentPositionEvaluation;
            PreviousPositionEvaluation = previousPositionEvaluation;

            BlunderIfWhiteMove = blunderIfWhiteMove;
            BlunderIfBlackMove = blunderIfBlackMove;
        }

        public double PreviousPositionEvaluation;
        public double CurrentPositionEvaluation;
        public bool BlunderIfWhiteMove;
        public bool BlunderIfBlackMove;
    }
}

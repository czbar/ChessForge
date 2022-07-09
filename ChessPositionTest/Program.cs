using System;
using System.IO;
using System.Collections.Generic;
using ChessPosition;
using GameTree;

namespace ChessPositionTest
{
    class Program
    {

        private static List<TestPosition> TestPositions = new List<TestPosition>()
        {
             new TestPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1","")
            ,new TestPosition("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1","")
            ,new TestPosition("r1r3k1/p2bppbp/3p1np1/q2Nn2P/1p1NP1P1/1B2BP2/PPPQ4/2KR3R b - - 1 15", "c6")
        };

        static void Main(string[] args)
        {
            TestPgnGameParser();
            TestFenParser();
        }

        private static void TestPgnGameParser()
        {
            WorkbookTree variationTree = new WorkbookTree();
            string gameText = File.ReadAllText("../../../ChessPositionTest/TestData/GameTreeTest_1.pgn");
            PgnGameParser pgnGame = new PgnGameParser(gameText, variationTree, true);
            PrintVariationTree(variationTree);
        }

        private static void TestFenParser()
        {
            foreach (TestPosition s in TestPositions)
            {
                BoardPosition pos = new BoardPosition();
                FenParser.ParseFenIntoBoard(s.Fen, ref pos);
                DebugUtils.PrintPosition(pos, s.Fen);

                string fen =FenParser.GenerateFenFromPosition(pos);

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

        private static void GetSquareAttackers(string algSquare, PieceColor col, ref BoardPosition testBoard)
        {
            SquareCoords coords = PositionUtils.ConvertAlgebraicToXY(algSquare);
            PiecesTargetingSquare sa = new PiecesTargetingSquare((byte)coords.Xcoord, (byte)coords.Ycoord, -1, -1, col, ref testBoard);

            DebugUtils.PrintAttackers(algSquare, col, sa.Candidates);
        }

        private static void PrintVariationTree(WorkbookTree tree)
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

                //for (int i = 0; i < nd.Children.Count; i++)
                //{
                //    PrintNode(nd.Children[i]);
                //    PrintTreeLine(nd.Children[i]);
                //}
                //if (nd.Children.Count == 0)
                //{
                //    Console.WriteLine(" :LINE END:");
                //}
                //return;
            }
        }
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
}

using System;
using System.Collections.Generic;
using ChessPosition;
using System.Text;
using System.IO;
using GameTree;

namespace GenerateOpeningsDictionary
{
    /// <summary>
    /// Generates the Openings FEN / ECO + Name dictionary
    /// from lichess's data file.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The key if is a "short" FEN i.e. FEN without move counters.
        /// The value is the ECO code followed by a space and the Opening name e.g "A00	Polish Opening". 
        /// </summary>
        private static Dictionary<string, string> _dictFenToName = new Dictionary<string, string>();

        /// <summary>
        /// Reads the lichess data file and processes all lines
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            string[] lines = File.ReadAllLines("../../../Documentation/OpeningClassification.txt");
            foreach (string line in lines)
            {
                ProcessLine(line);
            }
            // ouput file to be added into ChessForge  as a resorce file 
            StringBuilder sb = new StringBuilder();
            foreach (string key in _dictFenToName.Keys)
            {
                sb.Append(key);
                sb.Append("=");
                sb.AppendLine(_dictFenToName[key]);
            }

            File.WriteAllText("../../../ChessForge/Properties/OpeningExplorer.txt", sb.ToString());
        }

        /// <summary>
        /// A valid line comprises a 3 char ECO code followed by a space
        /// followed by the opening name, followed by a tab character and 
        /// th emove sequence.
        /// </summary>
        /// <param name="line"></param>
        static private void ProcessLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            string[] tokens = line.Split('\t');
            if (tokens.Length == 3)
            {
                // while generating catch any duplicates (there should not be any)
                string duplicateEntry = "";

                string fen = GetLastPositionFen(tokens[2]);
                if (_dictFenToName.ContainsKey(fen))
                {
                    duplicateEntry = _dictFenToName[fen];
                }
                string eco = tokens[0];
                string openingName = tokens[1];
                _dictFenToName[fen] = eco + " " + openingName;

                if (duplicateEntry.Length > 0)
                {
                    // if we have a duplicate, write it out to the standard output
                    Console.WriteLine("Dupe for FEN: " + fen);
                    Console.WriteLine(duplicateEntry);
                    Console.WriteLine(_dictFenToName[fen]);
                }
            }
        }

        /// <summary>
        /// Calculates FEN of the last position in the supplied
        /// sequence of moves.
        /// </summary>
        /// <param name="moves"></param>
        /// <returns></returns>
        static private string GetLastPositionFen(string moves)
        {
            VariationTree tree = new VariationTree(GameData.ContentType.NONE);
            new PgnGameParser(moves, tree);
            TreeNode lastNode = tree.Nodes[tree.Nodes.Count - 1];
            return FenParser.GenerateShortFen(lastNode.Position);
        }
    }
}

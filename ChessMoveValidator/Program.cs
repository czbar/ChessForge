using System;
using System.IO;
using System.Text;

namespace ChessMoveValidator
{
    /// <summary>
    /// Main class of the program for testing move validation.
    /// </summary>
    class Program
    {
        static string inputFile = "MoveValidationTests.txt";
        static string outputFile = "MoveValidationResults.txt";

        /// <summary>
        /// Main entry point of the program.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                inputFile = args[0];
            }

            string[] lines = File.ReadAllLines(inputFile);

            StringBuilder output = new StringBuilder();

            int passCount = 0;
            int failCount = 0;

            foreach (string line in lines)
            {
                bool? result;
                output.AppendLine(ProcessTestLine(line, out result));
                if (result.HasValue)
                {
                    if (result.Value)
                    {
                        passCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
            }

            File.WriteAllText(outputFile, output.ToString());
            
            Console.WriteLine("Tests complete. Results written to " + outputFile);
            Console.WriteLine("Pass count: " + passCount);
            Console.WriteLine("Fail count: " + failCount);
        }

        /// <summary>
        /// Processes a single line of the input file.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        static string ProcessTestLine(string line ,out bool? result)
        {
            result = null;

            StringBuilder sb = new StringBuilder();

            string[] parts = line.Split(',');
            if (parts.Length >= 3)
            {
                string fen = parts[0].Trim();
                string move = parts[1].Trim();
                bool expectedResult = IsTrue(parts[2].Trim());
                string comment = "";
                if (parts.Length > 3)
                {
                    comment = parts[3].Trim();
                }

                bool isValid = ChessMoveValidator.ValidateChessMove(fen, move);

                bool isPass = isValid == expectedResult;
                result = isPass;

                sb.Append(isValid == isPass ? "PASS: " : "FAIL: ");
                sb.Append("fen=" + fen);
                sb.Append(", move=" + move);
                sb.Append(", expected result=" + (expectedResult ? "valid" : "invalid"));
                sb.Append(", actual=" + (isValid ? "valid" : "invalid"));
                if (!string.IsNullOrEmpty(comment))
                {
                    sb.Append(", comment=" + comment);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts a string to a boolean value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static bool IsTrue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            else
            {
                return value == "1" || value.ToLower()[0] == 't' || value.ToLower()[0] == 'y';
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace MasterTranslator
{
    /// <summary>
    /// Program to 
    /// - generate empty translator templates
    /// - compare content of 2 translation files
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// List of actions performed by this program
        /// </summary>
        private enum Action
        {
            GENERATE,
            COMPARE,
        }

        // output lines created by the Translator file generator
        private static List<string> generatedLines = new List<string>();

        // name of the master file when generating a translator file
        private static string masterFile;

        // name of the generated translator file
        private static string translatorFile;

        // default name for the comparison results file
        private static string compareResults = "Comparison.txt";

        // name of the first file in the comparison action
        private static string firstTranslatorFile;

        // name of the second file in the comparison action
        private static string secondTranslatorFile;

        // action being performed
        private static Action action;

        /// <summary>
        /// Checks the command line arguments and invokes appropriate actions.
        /// 
        /// For running from here use the following command line parameters
        ///       masterFile = "../../../../Translation/MasterFile.txt";
        ///       translatorFile = "../../../../Translation/TranslatorFile.empty.txt";
        ///       
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // set default values that may be overwritten by the command line arguments
            masterFile = "MasterFile.txt";
            translatorFile = "TranslatorFile.empty.txt";

            if (!ProcessCommandLine(args))
            {
                return;
            }

            switch (action)
            {
                case Action.GENERATE:
                    GenerateTranslatorFile();
                    break;
                case Action.COMPARE:
                    CompareTranslatorFiles();
                    break;
            }
        }

        /// <summary>
        /// If the command line is empty, we will grab 
        /// the default MasterFile from ../../../../Translation/MasterFile.txt
        /// and produce TranslatorFile.empty.txt for the translator to fill out.
        /// 
        /// (The ".empty." part in the file will then be replaced by location e.g. ".pl-PL."
        /// The TranslatorFile.xx-XX.txt will then be given to resgen to compile into 
        /// resources file.)
        /// 
        /// If a file name is the argument then it will be read in instead of the MasterFile.
        /// If there is a second argument, it will be considered the name of the output file.
        /// 
        /// If the first argument is given with a '/' or '-' in front of it, the program 
        /// will perform some other function.
        /// </summary>
        /// <param name="args"></param>
        static bool ProcessCommandLine(string[] args)
        {
            if (args.Length == 0)
            {
                return true;
            }

            if (args[0][0] == '-' || args[0][0] == '/')
            {
                int n = 0;
                while (n < args.Length)
                {
                    string arg = args[n].Substring(1);
                    switch (arg.ToLower())
                    {
                        case "help":
                        case "?":
                            // we only honor the request to show help if this is the first argument.
                            if (n == 0)
                            {
                                PrintHelp();
                                return false;
                            }
                            break;
                        case "c":
                        case "comp":
                        case "compare":
                            n++;
                            // there must be 2 file names following this one to compare the content.
                            if (args.Length < 3)
                            {
                                return false;
                            }

                            firstTranslatorFile = args[n];
                            n++;
                            secondTranslatorFile = args[n];
                            action = Action.COMPARE;
                            break;
                    }
                    n++;
                }

            }
            else
            {
                masterFile = args[0];
                if (args.Length > 1)
                {
                    translatorFile = args[1];
                }
                action = Action.GENERATE;
            }

            return true;
        }

        /// <summary>
        /// Generates a Translator text and writes it out.
        /// </summary>
        private static void GenerateTranslatorFile()
        {
            string[] lines = File.ReadAllLines(masterFile);
            List<string> block = new List<string>();
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    // process current block
                    ProcessBlock(block);
                    // clear the block lines list
                    block.Clear();

                    generatedLines.Add("");
                }
                else
                {
                    block.Add(line);
                }
            }

            File.WriteAllLines(translatorFile, generatedLines.ToArray());
        }

        /// <summary>
        /// Compares the content of the firstTranslatorFile with secondTranslatorFile.
        /// We are only interested in not commented lines with an '=' sign in them.
        /// We look for keys missing on both sides and for duplicates.
        /// 
        /// Outputs the comaparison file.
        /// </summary>
        private static void CompareTranslatorFiles()
        {
            List<string> first= GetKeyStrings(firstTranslatorFile, out List<string> firstDupes, out List<string> firstMissingValues);
            List<string> second = GetKeyStrings(secondTranslatorFile, out List<string> secondDupes, out List<string> secondMissingValues);

            IEnumerable<string> inFirstOnly = first.Except(second);
            IEnumerable<string> inSecondOnly = second.Except(first);

            StringBuilder sb = new StringBuilder();
            if (inSecondOnly.Count() == 0)
            {
                sb.AppendLine("No keys missing in the first file");
            }
            else
            {
                sb.AppendLine("Keys missing in the first file");
                foreach (string s in inSecondOnly)
                {
                    sb.AppendLine("    " + s);
                }
                sb.AppendLine();
            }

            if (firstDupes.Count > 0)
            {
                sb.AppendLine("Duplicates in the first file");
                foreach (string s in firstDupes)
                {
                    sb.AppendLine("    " + s);
                }
                sb.AppendLine();
            }

            if (firstMissingValues.Count > 0)
            {
                sb.AppendLine("Keys with missing values in the first file");
                foreach (string s in firstMissingValues)
                {
                    sb.AppendLine("    " + s);
                }
                sb.AppendLine();
            }

            // the second file

            if (inFirstOnly.Count() == 0)
            {
                sb.AppendLine("Nothing missing in the second file");
            }
            else
            {
                sb.AppendLine("Keys missing in the second file");
                foreach (string s in inFirstOnly)
                {
                    sb.AppendLine("    " + s);
                }
                sb.AppendLine();
            }

            if (secondDupes.Count > 0)
            {
                sb.AppendLine("Duplicates in the second file");
                foreach (string s in secondDupes)
                {
                    sb.AppendLine("    " + s);
                }
                sb.AppendLine();
            }

            if (secondMissingValues.Count > 0)
            {
                sb.AppendLine("Keys with missing values in the second file");
                foreach (string s in secondMissingValues)
                {
                    sb.AppendLine("    " + s);
                }
                sb.AppendLine();
            }

            File.WriteAllText(compareResults, sb.ToString());
        }

        /// <summary>
        /// Retrieves Key strings from a file.
        /// Key strings are the first strings in a line 
        /// that is not commented out and contains a '=' character.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dupes"></param>
        /// <returns></returns>
        private static List<string> GetKeyStrings(string fileName, out List<string> dupes, out List<string> missingValues)
        {
            dupes = new List<string>();
            missingValues = new List<string>();
            List<string> keys = new List<string>();
            string[] lines = File.ReadAllLines(fileName);
            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line) && line[0] != '#')
                {
                    int index = line.IndexOf('=');
                    if (index > 0)
                    {
                        string key = line.Substring(0, index);
                        if (keys.Find(x => x == key) != null)
                        {
                            dupes.Add(key);
                        }
                        keys.Add(key);

                        if (index == line.Length - 1 || string.IsNullOrWhiteSpace(line.Substring(index)))
                        {
                            missingValues.Add(key);
                        }
                    }
                }
            }

            return keys;
        }

        /// <summary>
        /// Prints help to the console.
        /// </summary>
        static void PrintHelp()
        {
            Console.WriteLine("Master Translator command  line arguments: ");
            Console.WriteLine("   /help or /?    - print this info");
            Console.WriteLine("   file           - name of the input Master file to be translated");
            Console.WriteLine("                    the output file will be named TranslatorFile.empty.txt");
            Console.WriteLine("   file1 file2    - name of the input Master file followed by the name of the output file");
            Console.WriteLine("   /c file1 file2 - compare two translator files");
            Console.WriteLine("");
        }

        /// <summary>
        /// The passed block will start and end with a non-empty line 
        /// and there will be no empty lines in between.
        /// </summary>
        /// <param name="block"></param>
        static void ProcessBlock(List<string> block)
        {
            if (block.Count == 0)
            {
                return;
            }

            if (IsValidBlock(block))
            {
                int lineCount = block.Count;
                // insert #item: in the first line
                // create new line consisting of the pre '=' part of the last line
                // insert #en in the last line
                // append the new line
                string lineToTranslate = ExtractPartToTranslate(block[lineCount - 1]);
                for (int i = 0; i < lineCount; i++)
                {
                    if (i == 0)
                    {
                        string line = block[0].Insert(1, "description: ");
                        generatedLines.Add(line);
                    }
                    else if (i == lineCount - 1)
                    {
                        string line = block[i].Insert(0, "#english: ");
                        generatedLines.Add(line);
                    }
                    else
                    {
                        generatedLines.Add(block[i]);
                    }
                }
                generatedLines.Add(lineToTranslate);
            }
            else
            {
                foreach (string s in block)
                {
                    generatedLines.Add(s);
                }
            }
        }

        /// <summary>
        /// Extracts the key word to add to the translation file.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        static string ExtractPartToTranslate(string line)
        {
            string[] parts = line.Split('=');
            return parts[0] + "=";
        }

        /// <summary>
        /// If all lines but the last one start with a # and the last one does not
        /// but has an '=' in it, this is a legitimate block
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        static bool IsValidBlock(List<string> block)
        {
            bool valid = true;

            for (int i = 0; i < block.Count - 1; i++)
            {
                if (block[i].Length == 0 || block[i][0] != '#')
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                if (block[block.Count - 1].Length == 0
                    || block[block.Count - 1][0] == '#'
                    || block[block.Count - 1].IndexOf('=') <= 0)
                {
                    valid = false;
                }
            }

            return valid;
        }
    }
}

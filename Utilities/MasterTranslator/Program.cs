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
    /// This program generates translation templates from 
    /// the master file and existing translation files if exist
    /// for a given culture.
    /// It also compares the Master file against a localized file. 
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

        // first part of the template file names
        private static readonly string TEMPLATE_NAME = "Template";

        // first part of the localized file names
        private static readonly string LOCALIZED_NAME = "Localized";

        // first part of the comparison file names
        private static readonly string COMPARISON_NAME = "Comparison";

        // culture code
        private static string _cultureInfo = "";

        // output lines created by the Translator file generator
        private static List<string> _generatedLines = new List<string>();

        // folder with data files
        private static string _workingfolder;

        // name of the generated translator file
        private static string _outputTemplateFile;

        // input localized file
        private static string _inputLocalizedFile;

        // name of the master file when generating a translator file
        private static string _inputMasterFile = "Master.txt";

        // default name for the comparison results file
        private static string _compareResultsFile;

        // action being performed
        private static Action _action;

        /// <summary>
        /// Checks the command line arguments and invokes appropriate actions.
        /// If the action is GENERATE a new template file will be generated.
        /// If there is a Localized file used as an input, the program will compare
        /// it against the Master file.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            if (!ProcessCommandLine(args))
            {
                return;
            }

            if (_workingfolder != null)
            {
                _inputMasterFile = Path.Combine(_workingfolder, _inputMasterFile);
            }

            if (!File.Exists(_inputMasterFile))
            {
                Console.WriteLine("ERROR: file " + _inputMasterFile + " not found");
                return;
            }

            // prepare file names that may be needed
            if (string.IsNullOrEmpty(_cultureInfo))
            {
                _inputLocalizedFile = LOCALIZED_NAME + ".txt";
                _outputTemplateFile = TEMPLATE_NAME + ".txt";
                _compareResultsFile = COMPARISON_NAME + ".txt";
            }
            else
            {
                _inputLocalizedFile = string.Format(LOCALIZED_NAME + ".{0}.txt", _cultureInfo);
                _outputTemplateFile = string.Format(TEMPLATE_NAME + ".{0}.txt", _cultureInfo);
                _compareResultsFile = string.Format(COMPARISON_NAME + ".{0}.txt", _cultureInfo);
            }

            if (_workingfolder != null)
            {
                _inputLocalizedFile = Path.Combine(_workingfolder, _inputLocalizedFile);
                _outputTemplateFile = Path.Combine(_workingfolder, _outputTemplateFile);
                _compareResultsFile = Path.Combine(_workingfolder, _compareResultsFile);
            }

            // take the requested action
            switch (_action)
            {
                case Action.GENERATE:
                    CompareMasterAndLocalizedFiles();
                    GenerateTemplateFile();
                    break;
                case Action.COMPARE:
                    CompareMasterAndLocalizedFiles();
                    break;
            }
        }

        /// <summary>
        /// The first command line argument will determine the action, the optional
        /// second will be the culture code, then file names may follow.
        /// </summary>
        /// <param name="args"></param>
        static bool ProcessCommandLine(string[] args)
        {
            if (args.Length == 0)
            {
                return false;
            }

            string firstArg = args[0];
            if (firstArg[0] != '-' && firstArg[0] != '/')
            {
                return false;
            }

            switch (firstArg.ToLower())
            {
                case "h":
                case "?":
                    PrintHelp();
                    return false;
                case "c":
                    _action = Action.COMPARE;
                    break;
                case "g":
                    _action = Action.GENERATE;
                    break;
            }

            int n = 1;
            while (n < args.Length)
            {
                switch (args[n].ToLower())
                {
                    case "/f":
                    case "-f":
                        n++;
                        _workingfolder = args[n];
                        break;
                    default:
                        _cultureInfo = args[n];
                        break;
                }
                n++;
            }

            return true;
        }

        /// <summary>
        /// Generates a Localized file text and writes it out.
        /// </summary>
        private static void GenerateTemplateFile()
        {
            Dictionary<string, string> dictLocalized = new Dictionary<string, string>();
            string[] lines = File.ReadAllLines(_inputMasterFile);
            if (!string.IsNullOrWhiteSpace(_cultureInfo))
            {
                if (!File.Exists(_inputLocalizedFile))
                {
                    Console.WriteLine("ERROR: file " + _inputLocalizedFile + " not found");
                    return;
                }
                string[] localizedLines = File.ReadAllLines(_inputLocalizedFile);
                foreach (string line in localizedLines)
                {
                    UpdateLocalizedDictionary(line, ref dictLocalized);
                }
            }

            List<string> block = new List<string>();
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    // process current block
                    ProcessBlock(block, ref dictLocalized);
                    // clear the block lines list
                    block.Clear();

                    _generatedLines.Add("");
                }
                else
                {
                    block.Add(line);
                }
            }

            // we may be missing the last block
            ProcessBlock(block, ref dictLocalized);

            File.WriteAllLines(_outputTemplateFile, _generatedLines.ToArray());
        }

        /// <summary>
        /// Adds a key/value to the dictionary, if the line contains a translation.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="dictLocalized"></param>
        private static void UpdateLocalizedDictionary(string line, ref Dictionary<string, string> dictLocalized)
        {
            if (line.Length > 0 && line[0] != '#' && line.IndexOf('=') > 0)
            {
                string[] tokens = line.Split('=');
                if (tokens.Length > 1)
                {
                    dictLocalized[tokens[0]] = tokens[1];
                }
            }
        }

        /// <summary>
        /// Compares the content of the firstTranslatorFile with secondTranslatorFile.
        /// We are only interested in not commented lines with an '=' sign in them.
        /// We look for keys missing on both sides and for duplicates.
        /// 
        /// Outputs the comparison file.
        /// </summary>
        private static void CompareMasterAndLocalizedFiles()
        {
            if (string.IsNullOrEmpty(_cultureInfo) || string.IsNullOrEmpty(_inputLocalizedFile) || !File.Exists(_inputLocalizedFile))
            {
                return;
            }

            List<string> first = GetKeyStrings(_inputMasterFile, out List<string> firstDupes, out List<string> firstMissingValues);
            List<string> second = GetKeyStrings(_inputLocalizedFile, out List<string> secondDupes, out List<string> secondMissingValues);

            IEnumerable<string> inFirstOnly = first.Except(second);
            IEnumerable<string> inSecondOnly = second.Except(first);

            StringBuilder sb = new StringBuilder();
            if (inSecondOnly.Count() == 0)
            {
                sb.AppendLine("No keys missing in " + _inputMasterFile);
            }
            else
            {
                sb.AppendLine("Keys missing in " + _inputMasterFile);
                foreach (string s in inSecondOnly)
                {
                    sb.AppendLine("    " + s);
                }
                sb.AppendLine();
            }

            if (firstDupes.Count > 0)
            {
                sb.AppendLine("Duplicates in " + _inputMasterFile);
                foreach (string s in firstDupes)
                {
                    sb.AppendLine("    " + s);
                }
                sb.AppendLine();
            }

            if (firstMissingValues.Count > 0)
            {
                sb.AppendLine("Keys with missing values in " + _inputMasterFile);
                foreach (string s in firstMissingValues)
                {
                    sb.AppendLine("    " + s);
                }
                sb.AppendLine();
            }

            // the second file

            if (inFirstOnly.Count() == 0)
            {
                sb.AppendLine("No keys missing in " + _inputLocalizedFile);
            }
            else
            {
                sb.AppendLine("Keys missing in " + _inputLocalizedFile);
                foreach (string s in inFirstOnly)
                {
                    sb.AppendLine("    " + s);
                }
                sb.AppendLine();
            }

            if (secondDupes.Count > 0)
            {
                sb.AppendLine("Duplicates in " + _inputLocalizedFile);
                foreach (string s in secondDupes)
                {
                    sb.AppendLine("    " + s);
                }
                sb.AppendLine();
            }

            if (secondMissingValues.Count > 0)
            {
                sb.AppendLine("Keys with missing values in " + _inputLocalizedFile);
                foreach (string s in secondMissingValues)
                {
                    sb.AppendLine("    " + s);
                }
                sb.AppendLine();
            }

            File.WriteAllText(_compareResultsFile, sb.ToString());

            Console.WriteLine(sb.ToString());
            Console.WriteLine("See " + _compareResultsFile);
            Console.WriteLine("");
        }

        /// <summary>
        /// Retrieves key strings from a file.
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
        static void ProcessBlock(List<string> block, ref Dictionary<string, string> dictLocalized)
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
                        _generatedLines.Add(line);
                    }
                    else if (i == lineCount - 1)
                    {
                        string line = block[i].Insert(0, "#english: ");
                        _generatedLines.Add(line);
                    }
                    else
                    {
                        _generatedLines.Add(block[i]);
                    }
                }

                string val;
                bool res = dictLocalized.TryGetValue(lineToTranslate, out val);
                if (res)
                {
                    lineToTranslate = lineToTranslate + "=" + val;
                }
                else
                {
                    lineToTranslate = lineToTranslate + "=";
                }
                _generatedLines.Add(lineToTranslate);
            }
            else
            {
                foreach (string s in block)
                {
                    _generatedLines.Add(s);
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
            return parts[0];
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
                if (string.IsNullOrWhiteSpace(block[i]) || block[i][0] != '#')
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                if (string.IsNullOrWhiteSpace(block[block.Count - 1])
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

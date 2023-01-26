using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace MasterTranslator
{
    internal class Program
    {

        private static List<string> outputLines = new List<string>();

        /// <summary>
        /// Generates a Translator File from the Master File
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            string masterFile = "../../../../Translation/MasterFile.txt";
            if (args.Length > 0)
            {
                masterFile = args[0];
            }
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

                    outputLines.Add("");
                }
                else
                {
                    block.Add(line);
                }
            }

            File.WriteAllLines("TranslatorFile.empty.txt", outputLines.ToArray());
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
                        outputLines.Add(line);
                    }
                    else if (i == lineCount - 1)
                    {
                        string line = block[i].Insert(0, "#english: ");
                        outputLines.Add(line);
                    }
                    else
                    {
                        outputLines.Add(block[i]);
                    }
                }
                outputLines.Add(lineToTranslate);
            }
            else
            {
                foreach (string s in block)
                {
                    outputLines.Add(s);
                }
            }
        }

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

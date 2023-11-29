using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;

namespace OnlineLibraryCheck
{
    /// <summary>
    /// Reads the library listings and checks against the files in library
    /// subdirectories.
    /// </summary>
    internal class Program
    {
        // list of library content files in different languages
        private static List<string> _contentFiles = new List<string>();

        // root folder of the library
        private static string _libraryRoot;

        // dictionary where the key is the file path and the value is the list of library listings containing it.
        private static Dictionary<string, List<string>> _dictFilesInLibraries = new Dictionary<string, List<string>>();

        /// <summary>
        /// The first argument is the root directory of the library.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // hard coded list of library content files
            _contentFiles.Add("LibraryContent.txt");
            _contentFiles.Add("LibraryContent.pl.txt");

            // default library root
            _libraryRoot = "C:/Users/rober/Documents/ChessForge/WebSite/Library";

            // update the library root if listed
            if (args.Length > 0)
            {
                _libraryRoot = args[0];
            }

            // create list of content files with full paths
            for (int i = 0; i < _contentFiles.Count; i++)
            {
                _contentFiles[i] = Path.Combine(_libraryRoot, _contentFiles[i]);
            }

            // process each content file
            foreach (string contentFile in _contentFiles)
            {
                ProcessLibraryContentFile(contentFile);
            }

            // check if all specified files exist, and report omissions
            bool hasIssues = CheckFilesExist();

            if (!hasIssues)
            {
                Console.WriteLine("Content OK");
            }
        }

        /// <summary>
        /// Checks if all files specified in the online library content files exist.
        /// Report those that do not.
        /// </summary>
        private static bool CheckFilesExist()
        {
            bool hasIssues = false;

            foreach (string file in _dictFilesInLibraries.Keys)
            {
                string path = Path.Combine(_libraryRoot, file);
                if (!File.Exists(path))
                {
                    Console.WriteLine("ERROR file does not exists: " + file);
                    Console.WriteLine("    Listed in: ");
                    foreach (string contentFile in _dictFilesInLibraries[file])
                    {
                        Console.WriteLine("        " + contentFile);
                        hasIssues = true;
                    }
                }
            }

            return hasIssues;
        }

        /// <summary>
        /// Processes each library content file.
        /// </summary>
        /// <param name="contentFile"></param>
        private static void ProcessLibraryContentFile(string contentFile)
        {
            string[] lines = File.ReadAllLines(contentFile);
            foreach (string line in lines)
            {
                ParseLine(contentFile,line);
            }
        }

        /// <summary>
        /// Parses a single line in the content file.
        /// Returns the key (type) and the value.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static void ParseLine(string contentFile, string line)
        {
            if (!string.IsNullOrEmpty(line))
            {
                string[] tokens = line.Split(':');
                if (tokens.Length > 1)
                {
                    if (tokens[0].Trim() == "File")
                    {
                        string path = tokens[1].Trim();
                        if (!_dictFilesInLibraries.Keys.Contains(path))
                        {
                            _dictFilesInLibraries[path] = new List<string>();
                        }
                        _dictFilesInLibraries[path].Add(contentFile);
                    }
                }
            }
        }

    }
}

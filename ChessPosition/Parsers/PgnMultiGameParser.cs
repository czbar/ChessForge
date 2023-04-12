using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace GameTree
{
    /// <summary>
    /// Utilities for parsing a file or string with multiple PGN games.
    /// </summary>
    public class PgnMultiGameParser
    {
        /// <summary>
        /// Reads the passed text that is expected to represent one or more PGN games.
        /// Builds the list of games and returnes their number
        /// </summary>
        /// <param name="text"></param>
        /// <param name="games"></param>
        /// <returns></returns>
        public static int ParsePgnMultiGameText(string text,
                                                ref ObservableCollection<GameData> games)
        {
            games.Clear();

            // read line by line, fishing for lines with PGN headers i.e. beginning with "[" followed by a keyword.
            // Note we may accidentally hit a comment formatted that way, so make sure that the last char on the line is "]".
            GameData gm = new GameData();
            gm.FirstLineInFile = 1;

            using (StringReader reader = new StringReader(text))
            {
                StringBuilder gameText = new StringBuilder();
                string line;
                int lineNo = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    ProcessMultiPgnTextLine(line, lineNo, ref gameText, ref gm, ref games);
                }
            }

            return games.Count;
        }

        /// <summary>
        /// Processes a single line of text from a multi pgn file/text
        /// and updates the objects passed by refernce:
        /// current game's text, current game's GameData object and games
        /// collection as required.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="lineNo"></param>
        /// <param name="sbGameText"></param>
        /// <param name="gm"></param>
        /// <param name="games"></param>
        private static void ProcessMultiPgnTextLine(string line,
                                         int lineNo,
                                         ref StringBuilder sbGameText,
                                         ref GameData gm,
                                         ref ObservableCollection<GameData> games)
        {
            bool headerLine = true;

            string header = PgnHeaders.ParsePgnHeaderLine(line, out string val);
            if (header != null)
            {
                // ignore headers with no name
                if (header.Length > 0)
                {
                    gm.Header.SetHeaderValue(header, val);
                }
            }
            else
            {
                headerLine = false;
                // if no header then this is the end of the header lines
                // if we do have any header data we add a new game to the list
                if (gm.HasAnyHeader())
                {
                    gm.Header.DetermineContentType();
                    games.Add(gm);
                    gm = new GameData();
                }
            }

            // If this was the first header line, the gameText variable
            // holds the complete text of the previous game
            if (headerLine == true && gm.FirstLineInFile == 0)
            {
                gm.FirstLineInFile = lineNo - 1;
                // add game text to the previous game object 
                games[games.Count - 1].GameText = sbGameText.ToString();
                sbGameText.Clear();
            }

            if (!headerLine)
            {
                sbGameText.AppendLine(line);
            }
        }

    }
}

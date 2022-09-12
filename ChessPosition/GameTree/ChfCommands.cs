using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameTree
{
    /// <summary>
    /// Encapsulates special Chess Forge commands extending
    /// the PGN format.
    /// </summary>
    public class ChfCommands
    {
        /// <summary>
        /// Command IDs
        /// </summary>
        public enum Command
        {
            NONE,
            BOOKMARK,
            ENGINE_EVALUATION,
            COACH_ASSESSMENT,
            COACH_COMMENT
        }

        /// <summary>
        /// ID's of coach's assessments
        /// </summary>
        public enum Assessment
        {
            NONE,
            BEST,
            ONLY,
            BRILLIANT,
            DUBIOUS,
            MISTAKE,
            BLUNDER
        }

        /// <summary>
        /// Map of PGN strings to Command IDs
        /// </summary>
        private static Dictionary<string, Command> _dictCommands = new Dictionary<string, Command>()
        {
            ["%chf-bkm"] = Command.BOOKMARK,
            ["%chf-eev"] = Command.ENGINE_EVALUATION,
            ["%chf-coa"] = Command.COACH_ASSESSMENT
        };

        /// <summary>
        /// Map of assessment strings to assessment commands.
        /// </summary>
        private static Dictionary<string, Assessment> _dictAssessments = new Dictionary<string, Assessment>()
        {
            ["best"] = Assessment.BEST,
            ["only"] = Assessment.ONLY,
            ["brilliant"] = Assessment.BRILLIANT,
            ["dubious"] = Assessment.DUBIOUS,
            ["mistake"] = Assessment.MISTAKE,
            ["blunder"] = Assessment.BLUNDER
        };

        /// <summary>
        /// Returns the command id given a string.
        /// </summary>
        /// <param name="sCmd"></param>
        /// <returns></returns>
        public static Command GetCommand(string sCmd)
        {
            if (sCmd == null)
                return Command.NONE;

            Command cmd;
            if (_dictCommands.TryGetValue(sCmd, out cmd))
                return cmd;
            else
                return Command.NONE;
        }

        /// <summary>
        /// Returns a string for a given Command Id.
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public static string GetStringForCommand(Command cmd)
        {
            return _dictCommands.FirstOrDefault(x => x.Value == cmd).Key;
        }

        /// <summary>
        /// Returns the Assessment id given a string.
        /// </summary>
        /// <param name="sCmd"></param>
        /// <returns></returns>
        public static Assessment GetAssessment(string sAss)
        {
            if (sAss == null)
                return ChfCommands.Assessment.NONE;

            Assessment ass;
            if (_dictAssessments.TryGetValue(sAss, out ass))
                return ass;
            else
                return Assessment.NONE;
        }

        /// <summary>
        /// Returns a string for a given Assessment Id.
        /// </summary>
        /// <param name="ass"></param>
        /// <returns></returns>
        public static string GetStringForAssessment(Assessment ass)
        {
            return _dictAssessments.FirstOrDefault(x => x.Value == ass).Key;
        }

    }
}

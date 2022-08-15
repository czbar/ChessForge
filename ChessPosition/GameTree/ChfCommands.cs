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
            ENGINE_EVALUATION
        }

        /// <summary>
        /// Map of PGN strings to Command IDs
        /// </summary>
        private static Dictionary<string, Command> _dictCommands = new Dictionary<string, Command>()
        {
            ["%chf-bkm"] = Command.BOOKMARK,
            ["%chf-eev"] = Command.ENGINE_EVALUATION,
        };

        /// <summary>
        /// Returns the command id given a string.
        /// </summary>
        /// <param name="sCmd"></param>
        /// <returns></returns>
        public static Command GetCommand(string sCmd)
        {
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

    }
}

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
        /// Strings to use when encoding the XAML view.
        /// </summary>
        public const string XAML_NODE_ID = "node_id";
        public const string XAML_MOVE_TEXT = "text";
        public const string XAML_FEN = "fen";

        /// <summary>
        /// Command IDs
        /// </summary>
        public enum Command
        {
            NONE,
            BOOKMARK,
            BOOKMARK_V2,
            ENGINE_EVALUATION,
            ENGINE_EVALUATION_V2,
            QUIZ_POINTS,
            THUMBNAIL,
            ARTICLE_REFS,
            XAML,

            ARROWS,
            CIRCLES
        }

        /// <summary>
        /// Map of PGN strings to Command IDs
        /// </summary>
        private static Dictionary<string, Command> _dictCommands = new Dictionary<string, Command>()
        {
            ["%chf-bkm"] = Command.BOOKMARK,           // DEPRECATED
            ["%bkm"] = Command.BOOKMARK_V2,
            ["%chf-eev"] = Command.ENGINE_EVALUATION,  // DEPRECATED
            ["%eval"] = Command.ENGINE_EVALUATION_V2,
            ["%quiz"] = Command.QUIZ_POINTS,
            ["%thmb"] = Command.THUMBNAIL,
            ["%ref"] = Command.ARTICLE_REFS,
            ["%xaml"] = Command.XAML,

            ["%csl"] = Command.CIRCLES,
            ["%cal"] = Command.ARROWS
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

    }
}

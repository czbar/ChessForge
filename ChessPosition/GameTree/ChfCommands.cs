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
        public const string XAML_CIRCLES = "csl";
        public const string XAML_ARROWS = "cal";
        public const string XAML_IS_DIAGRAM = "diag";
        public const string XAML_IS_DIAGRAM_FLIPPED = "diag_flip";

        // ChessBase diagram command
        public const string CHESS_BASE_DIAGRAM = "[#]";
        public const string CHESS_BASE_DIAGRAM_LONG = "Diagram [#]";

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
            COMMENT_BEFORE_MOVE,
            QUIZ_POINTS,
            ASSESSMENT,
            BEST_RESPONSE,
            THUMBNAIL,
            DIAGRAM,
            ARTICLE_REFS,
            XAML,
            BINARY,

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
            ["%assm"] = Command.ASSESSMENT,
            ["%brsp"] = Command.BEST_RESPONSE,
            ["%cbm"] = Command.COMMENT_BEFORE_MOVE,
            ["%thmb"] = Command.THUMBNAIL,
            ["%diag"] = Command.DIAGRAM,
            ["%ref"] = Command.ARTICLE_REFS,
            ["%xaml"] = Command.XAML,
            ["%bin"] = Command.BINARY,

            ["%csl"] = Command.CIRCLES,
            ["%cal"] = Command.ARROWS
        };

        /// <summary>
        /// Coded assessment values.
        /// </summary>
        public enum Assessment
        {
            NONE = 0,
            BLUNDER = 1,
            MISTAKE = 2,
        }

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
        /// The LSB determines if the diagram is flipped.
        /// The second bit determines if the diagram is placed
        /// before the comment.
        /// The third bit determines whether the diagram is to be shown before
        /// the move, reflecting the position from the parent node.
        /// </summary>
        /// <param name="attrs"></param>
        /// <param name="isFlipped"></param>
        /// <param name="isPreComment"></param>
        public static void DecodeDiagramAttrs(uint attrs, out bool isFlipped, out bool isPreComment, out bool isBeforeMove)
        {
            isFlipped = false;
            isPreComment = false;
            isBeforeMove = false;

            if ((attrs & 0x01) != 0)
            {
                isFlipped = true;
            }
            if ((attrs & 0x02) != 0)
            {
                isPreComment = true;
            }
            if ((attrs & 0x04) != 0)
            {
                isBeforeMove = true;
            }
        }

        /// <summary>
        /// The LSB determines if the diagram is flipped.
        /// The second bit determines if the diagram is placed
        /// before the comment.
        /// The third bit determines whether the diagram is to be shown before
        /// the move, reflecting the position from the parent node.
        /// </summary>
        /// <param name="attrs"></param>
        /// <param name="isFlipped"></param>
        /// <param name="isPreComment"></param>
        public static uint CodeDiagramAttrs(bool isFlipped, bool isPreComment, bool isBeforeMove)
        {
            uint coded = 0;

            if (isFlipped)
            {
                coded |= 0x01;
            }
            if (isPreComment)
            {
                coded |= 0x02;
            }
            if (isBeforeMove)
            {
                coded |= 0x04;
            }

            return coded;
        }
    }
}

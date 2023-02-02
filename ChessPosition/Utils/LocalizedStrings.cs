using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessPosition.Utils
{
    /// <summary>
    /// Dictionary of localized for use with the library.
    /// The main program populates it upon startup.
    /// </summary>
    public class LocalizedStrings
    {
        /// <summary>
        /// Keys to the localized values.
        /// </summary>
        public enum StringId
        {
            None = 0,

            Move,
            Game,
            Exercise,
            PGN,

            PgnMissingMoveAfter,

            FenTooFewFields,
            FenInvalidEnpassant,
            FenColorNotSpecified,
            FenTooFewRows,
            FenRowIncomplete,

            InvalidEngineMoveReceived,
            CannotIdentifyPiece,
            IllegalCastling,
            AmbiguousMove,
        }

        /// <summary>
        /// Dictionary initialized from the main program.
        /// </summary>
        public static Dictionary<StringId, string> Values= new Dictionary<StringId, string>();
    }
}

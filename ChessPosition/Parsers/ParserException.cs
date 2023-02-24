using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameTree
{
    /// <summary>
    /// EventArgs 
    /// </summary>
    public class ParserException : Exception
    {
        /// <summary>
        /// Types of errors for the caller to translate
        /// into a message to the user.
        /// </summary>
        public enum ParseErrorType
        {
            PGN_GAME_EXPECTED_MOVE_NUMBER,
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="err"></param>
        /// <param name="token"></param>
        public ParserException(ParseErrorType err, string token)
        {
            ParseError = err;
            CurrentToken = token;
        }

        /// <summary>
        /// Type of the error.
        /// </summary>
        public ParseErrorType ParseError;

        /// <summary>
        /// Algebraic notation for the parent move.
        /// </summary>
        public string PreviousMove;

        /// <summary>
        /// Current token being processed.
        /// </summary>
        public string CurrentToken;
    }
}



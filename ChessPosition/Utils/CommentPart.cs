using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessPosition.Utils
{
    /// <summary>
    /// Types of comment parts
    /// </summary>
    public enum CommentPartType
    {
        ASSESSMENT,
        TEXT,
        URL,
        THUMBNAIL_SYMBOL,
        QUIZ_POINTS,
        REFERENCE,
    }

    /// <summary>
    /// Represents a single part of a comment
    /// </summary>
    public class CommentPart
    {
        /// <summary>
        /// Type of the part.
        /// </summary>
        public CommentPartType Type;

        /// <summary>
        /// Text of the part.
        /// </summary>
        public string Text;

        /// <summary>
        /// Guid of the part if applicable.
        /// </summary>
        public string Guid;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="text"></param>
        public CommentPart(CommentPartType type, string text, string guid = null)
        {
            Type = type;
            Text = text;
            Guid = guid;
        }
    }
}

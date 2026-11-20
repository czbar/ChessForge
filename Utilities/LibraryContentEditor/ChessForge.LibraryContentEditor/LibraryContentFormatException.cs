using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessForge.LibraryContentEditor
{
    public sealed class LibraryContentFormatException : FormatException
    {
        public LibraryContentFormatException(IEnumerable<string> errors)
            : base(CreateMessage(errors))
        {
            Errors = errors.ToArray();
        }

        public IReadOnlyList<string> Errors { get; private set; }

        private static string CreateMessage(IEnumerable<string> errors)
        {
            string[] errorArray = errors.ToArray();
            return "The library-content file is invalid:" + Environment.NewLine +
                   string.Join(Environment.NewLine, errorArray.Select(error => "• " + error));
        }
    }
}


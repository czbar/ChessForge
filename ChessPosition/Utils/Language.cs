using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessPosition
{
    public class Language : IComparable<Language>
    {
        /// <summary>
        /// Marks the selected language if any.
        /// It could be that no language is specified in the config
        /// and we are using default.
        /// </summary>
        public bool IsSelected;

        /// <summary>
        /// Culture code of the language
        /// </summary>
        public string Code;

        /// <summary>
        /// Name of the language
        /// </summary>
        public string Name;

        /// <summary>
        /// Compares 2 language objects alphabetically.
        /// </summary>
        /// <param name="lang"></param>
        /// <returns></returns>
        public int CompareTo(Language lang)
        {
            return string.Compare(this.Name, lang.Name, true);
        }

        /// <summary>
        /// Show name when in ListBox etc.
        /// </summary>
        /// <returns></returns>
        override public string ToString()
        {
            return this.Name;
        }
    }
}

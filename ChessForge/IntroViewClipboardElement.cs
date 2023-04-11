using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// A class for elements to store in the IntroViewClipboard
    /// </summary>
    public class IntroViewClipboardElement
    {
        /// <summary>
        /// Constructs an element of a specified type.
        /// </summary>
        /// <param name="type"></param>
        public IntroViewClipboardElement(IntroViewClipboard.ElementType type)
        {
            Type = type;
        }

        /// <summary>
        /// Type of the element.
        /// </summary>
        public IntroViewClipboard.ElementType Type = IntroViewClipboard.ElementType.None;

        /// <summary>
        /// Node id that will be used by some elements.
        /// </summary>
        public int NodeId = -1;

        /// <summary>
        /// Margins of the this paragraph or the parent paragraph.
        /// </summary>
        public Thickness? Margins;

        /// <summary>
        /// An object with data of the type appropriate for the type of the element.
        /// </summary>
        public object DataObject = null;

        /// <summary>
        /// A boolean value to use for the elements that require it.
        /// </summary>
        public bool? BoolState;
    }
}

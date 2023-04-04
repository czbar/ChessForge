using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace ChessForge
{
    /// <summary>
    /// Holds the last selection that the user sent a request to Copy on
    /// </summary>
    public class IntroViewClipboard
    {
        /// <summary>
        /// Types of elements that can be found in the view
        /// </summary>
        public enum ElementType
        {
            None,
            Paragraph,
            Run,
            Move,
            Diagram
        }

        /// <summary>
        /// The list of elements in the Clipboard.
        /// </summary>
        public static List<IntroViewClipboardElement> Elements = new List<IntroViewClipboardElement>();

        /// <summary>
        /// Clears the clipboard.
        /// </summary>
        public static void Clear()
        {
            Elements.Clear();
        }

        /// <summary>
        /// Adds a Run element to the Clipboard.
        /// </summary>
        /// <param name="run"></param>
        public static void AddRun(Run run)
        {
            IntroViewClipboardElement element = new IntroViewClipboardElement(ElementType.Run);

            // make a copy of the run
            Run runToAdd = RichTextBoxUtilities.CopyRun(run);
            element.DataObject = runToAdd;

            Elements.Add(element);
        }

        /// <summary>
        /// Adds a paragraph element to the Clipboard.
        /// </summary>
        /// <param name="para"></param>
        public static void AddParagraph(Paragraph para, bool? flipped)
        {
            IntroViewClipboardElement element = new IntroViewClipboardElement(ElementType.Paragraph);

            // make a copy of the paragraph (without content)
            Paragraph paraToAdd = new Paragraph();
            paraToAdd.Name = para.Name;
            paraToAdd.FontWeight = para.FontWeight;
            paraToAdd.FontSize = para.FontSize;
            //paraToAdd.TextDecorations = para.TextDecorations;

            element.DataObject = paraToAdd;

            if (flipped != null)
            {
                element.BoolState = flipped;
            }

            Elements.Add(element);
        }

        /// <summary>
        /// Adds a Move element to the Clipboard.
        /// </summary>
        /// <param name="node"></param>
        public static void AddMove(TreeNode node)
        {
            IntroViewClipboardElement element = new IntroViewClipboardElement(ElementType.Move);
            element.DataObject = node;
            Elements.Add(element);
        }

        /// <summary>
        /// Adds a diagram element to the clipboard.
        /// </summary>
        /// <param name="diagram"></param>
        public static void AddDiagram(IntroViewDiagram diagram)
        {
            IntroViewClipboardElement element = new IntroViewClipboardElement(ElementType.Diagram);
            element.DataObject = diagram;
            Elements.Add(element);
        }
    }

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
        /// An object with data of the type appropriate for the type of the element.
        /// </summary>
        public object DataObject = null;

        /// <summary>
        /// A boolean value to use for the elements that require it.
        /// </summary>
        public bool? BoolState;
    }

}

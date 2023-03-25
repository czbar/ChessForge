using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Clipboard for copying view items. 
    /// </summary>
    public class ChfClipboard
    {
        /// <summary>
        /// Types of objects that can be held in the clipboard.
        /// </summary>
        public enum ItemType
        {
            EMPTY,
            NODE_LIST
        }

        /// <summary>
        /// Type of the object currently in the clipboard
        /// </summary>
        public static ItemType Type { get; set; }

        /// <summary>
        /// Object currently held in the clipboard.
        /// </summary>
        public static object Value { get; set; }

        /// <summary>
        /// Saves a node list in the clipboard.
        /// </summary>
        /// <param name="lst"></param>
        public static void HoldNodeList(List<TreeNode> lst)
        {
            if (lst == null)
            {
                return;
            }

            Type = ItemType.NODE_LIST;
            Value = lst;
        }
    }
}

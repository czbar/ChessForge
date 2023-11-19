using ChessPosition;
using GameTree;
using System.Collections.Generic;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Manages interactions with the system clipborad
    /// </summary>
    public class SystemClipboard
    {
        /// <summary>
        /// Clear the clipboard.
        /// </summary>
        public static void Clear()
        {
            try
            {
                Clipboard.Clear();
            }
            catch { };
        }

        /// <summary>
        /// Returns true if the clipboard has a list of Nodes.
        /// </summary>
        /// <returns></returns>
        public static bool HasSerializedData()
        {
            bool res = false;
            try
            {
                if (Clipboard.ContainsData(DataFormats.Serializable))
                {
                    IDataObject dataObject = Clipboard.GetDataObject();
                    if (dataObject != null)
                    {
                        res = (dataObject.GetData(DataFormats.Serializable) as List<TreeNode>) != null;
                    }
                }
            }
            catch
            {
            }

            return res;
        }

        /// <summary>
        /// Sets text in the clipboard.
        /// </summary>
        public static void SetText(string txt)
        {
            try
            {
                Clipboard.SetText(txt);
            }
            catch { };
        }

        /// <summary>
        /// Gets text from the clipboard.
        /// </summary>
        public static string GetText()
        {
            string txt = "";
            try
            {
                txt = Clipboard.GetText();
            }
            catch { };

            return txt;
        }

        /// <summary>
        /// Saves a node list in the clipboard.
        /// </summary>
        /// <param name="lst"></param>
        public static void CopyMoveList(List<TreeNode> lst, uint moveNumberOffset)
        {
            try
            {
                IDataObject dataObject = new DataObject();
                dataObject.SetData(DataFormats.UnicodeText, TextUtils.BuildLineText(lst, moveNumberOffset));
                dataObject.SetData(DataFormats.Serializable, lst);
                Clipboard.SetDataObject(dataObject);
            }
            catch { }
        }

    }
}

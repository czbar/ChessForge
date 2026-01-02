using GameTree;
using System.Collections.Generic;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Manages interactions with the system clipboard.
    /// Due to some issues with interop, calls to the clipboard can throw an exception.
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
        /// Direct call Clipboard.GetText() can throw an exception.
        /// </summary>
        /// <returns></returns>
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
        /// Gets IDataObject from the clipboard.
        /// Direct call Clipboard.GetDataObject() can throw an exception.
        /// </summary>
        /// <returns></returns>
        public static IDataObject GetDataObject()
        {
            IDataObject dataObject = null;

            try
            {
                dataObject = Clipboard.GetDataObject();
            }
            catch { }

            return dataObject;
        }

        /// <summary>
        /// Places an IDataObject in the clipboard.
        /// Direct call Clipboard.SetDataObject() can throw an exception.
        /// </summary>
        /// <returns></returns>
        public static void SetDataObject(IDataObject dataObject)
        {
            try
            {
                Clipboard.SetDataObject(dataObject);
            }
            catch { }
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
                dataObject.SetData(DataFormats.UnicodeText, PgnWriter.BuildSubtreeText(lst[0], moveNumberOffset));
                dataObject.SetData(DataFormats.Serializable, lst);
                Clipboard.SetDataObject(dataObject);
            }
            catch { }
        }

    }
}

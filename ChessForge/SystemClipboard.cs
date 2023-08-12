using System;
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
        /// Determines if the text in the clipboard is empty.
        /// </summary>
        /// <returns></returns>
        public static bool IsEmpty()
        {
            string txt = GetText();
            return string.IsNullOrEmpty(txt);
        }
    }
}

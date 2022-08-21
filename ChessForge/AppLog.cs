using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ChessForge
{
    /// <summary>
    /// Logging class to be used in debug mode.
    /// </summary>
    public class AppLog
    {
        /// <summary>
        /// Lock object for accessing log the log
        /// data.
        /// </summary>
        public static object AppLogLock = new object();

        /// <summary>
        /// List of logged messages.
        /// </summary>
        private static List<string> Log = new List<string>();

        /// <summary>
        /// Logs a message adding a time stamp.
        /// </summary>
        /// <param name="msg"></param>
        public static void Message(string msg)
        {
            if (Configuration.DebugMode == 0)
                return;

            lock (AppLogLock)
            {
                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  ";
                Log.Add(timeStamp + msg);
            }
        }

        /// <summary>
        /// Writes the logged messages out to a file.
        /// </summary>
        public static void Dump()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in Log)
            {
                sb.Append(s + Environment.NewLine);
            }
            try
            {
                // this may fail if we try to write to the system folder e.g. because the app was invoked via menu association.
                string fileName = Path.Combine(App.AppPath, "log.txt");
                File.WriteAllText(fileName, sb.ToString());
            }
            catch { };
        }
    }
}

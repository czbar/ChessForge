using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace EngineService
{
    /// <summary>
    /// Collects and logs commands to, and messages from the engine.
    /// </summary>
    public class EngineLog
    {
        // lock object to use when logging
        public static object AppLogLock = new object();

        // list of logged messages
        public static List<string> Log = new List<string>();

        /// <summary>
        /// Logs a single message
        /// </summary>
        /// <param name="msg"></param>
        [Conditional("DEBUG")]
        public static void Message(string msg)
        {
            lock (AppLogLock)
            {
                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  ";
                Log.Add(timeStamp + msg);
            }
        }

        /// <summary>
        /// Dumps all logged messages to a file
        /// </summary>
        [Conditional("DEBUG")]
        public static void Dump(string filePath)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (string s in Log)
                {
                    sb.Append(s + Environment.NewLine);
                }
                File.WriteAllText(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                throw ex;
            };
            Log.Clear();
        }
    }
}

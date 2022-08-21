using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace EngineService
{
    public class EngineLog
    {
        // lock object to use when logging
        public static object AppLogLock = new object();

        // list of logged messages
        public static List<string> Log = new List<string>();

        // debug mode flag
        private static bool _isDebugMode = false;

        // file to save the log file to
        private static string _outputFile = "";

        /// <summary>
        /// Sets the debug mode and path to save the log to.
        /// </summary>
        /// <param name="appPath"></param>
        public static void SetDebugMode(string appPath)
        {
            _isDebugMode = true;
            _outputFile = Path.Combine(appPath, "engine.txt");
        }

        /// <summary>
        /// Logs a single message
        /// </summary>
        /// <param name="msg"></param>
        public static void Message(string msg)
        {
            if (!_isDebugMode)
            {
                return;
            }

            lock (AppLogLock)
            {
                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  ";
                Log.Add(timeStamp + msg);
            }
        }

        /// <summary>
        /// Dumps all loogged messages to a file
        /// </summary>
        public static void Dump()
        {
            if (!_isDebugMode)
            {
                return;
            }

            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (string s in Log)
                {
                    sb.Append(s + Environment.NewLine);
                }
                File.WriteAllText(_outputFile, sb.ToString());
            }
            catch { };
        }
    }
}

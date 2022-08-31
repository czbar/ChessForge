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
        private static string _outputPath = "";

        /// <summary>
        /// Sets the debug mode and path to save the log to.
        /// </summary>
        /// <param name="appPath"></param>
        public static void SetDebugMode(string appPath)
        {
            _isDebugMode = true;
            _outputPath = appPath;
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
        /// Dumps all logged messages to a file
        /// </summary>
        public static void Dump(string logFileDistinct)
        {
            if (!_isDebugMode)
            {
                return;
            }

            try
            {
                if (logFileDistinct != null)
                {
                    _outputPath = Path.Combine(_outputPath, "enginelog" + logFileDistinct);
                }
                else 
                {
                    _outputPath = Path.Combine(_outputPath, "enginelog.txt");
                }

                StringBuilder sb = new StringBuilder();
                foreach (string s in Log)
                {
                    sb.Append(s + Environment.NewLine);
                }
                File.WriteAllText(_outputPath, sb.ToString());
            }
            catch { };
        }
    }
}

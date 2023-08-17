using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ChessForge
{
    /// <summary>
    /// This class must not be used in software releases.
    /// It provides a primitive logging feature to use in cases when 
    /// issues only happen in release builds so the AppLog cannot be used
    /// to help with debugging.
    /// </summary>
    public class SpecialLogs
    {
        // flags whether the log has been initialized
        private static bool _isLogInitialized;

        // name of the log file
        private static string _fileName = "SpecialLog.txt";

        // path to the log file
        private static string _path;

        /// <summary>
        /// Initializes the log file by creating or overwriting it with empty content.
        /// </summary>
        private static void Initialize()
        {
            _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Chess Forge", _fileName);
            try
            {
                using (FileStream fs = File.Create(_path))
                {
                }
                _isLogInitialized = true;
            }
            catch { }
        }

        /// <summary>
        /// Logs a message to the special log file
        /// </summary>
        /// <param name="txt"></param>
        public static void LogMessage(string txt)
        {
            if (!_isLogInitialized)
            {
                Initialize();
            }

            try
            {
                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  ";
                string msg = timeStamp + txt;

                using (StreamWriter sw = File.AppendText(_path))
                {
                    sw.WriteLine(msg);
                }
            }
            catch { }
        }
    }
}

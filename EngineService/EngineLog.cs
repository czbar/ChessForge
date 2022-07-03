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
        public static object AppLogLock = new object();

        public static List<string> Log = new List<string>();

        private static bool _isDebugMode = false;

        public static void SetDebugMode()
        {
            _isDebugMode = true;
        }

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

        public static void Dump()
        {
            if (!_isDebugMode)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            foreach (string s in Log)
            {
                sb.Append(s + Environment.NewLine);
            }
            File.WriteAllText("engine.txt", sb.ToString());
        }
    }
}

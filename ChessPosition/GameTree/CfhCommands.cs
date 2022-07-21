using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameTree
{
    public class CfhCommands
    {
        public enum Command
        {
            NONE,
            BOOKMARK
        }

        private static Dictionary<string, Command> _dictCommands = new Dictionary<string, Command>()
        {
            ["%chf-bkm"] = Command.BOOKMARK,
        };

        public static Command GetCommand(string sCmd)
        {
            Command cmd;
            if (_dictCommands.TryGetValue(sCmd, out cmd))
                return cmd;
            else
                return Command.NONE;
        }
        public static string GetStringForCommand(Command cmd)
        {
            return _dictCommands.FirstOrDefault(x => x.Value == cmd).Key;
        }

    }
}

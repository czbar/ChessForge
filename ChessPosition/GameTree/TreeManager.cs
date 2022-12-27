using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Manages Variation Trees within a Workbook.
    /// </summary>
    public class TreeManager
    {
        private static int _maxTreeId = 0;

        public static int GetNewTreeId()
        {
            _maxTreeId++;
            return _maxTreeId;
        }

        public static void Reset()
        {
            _maxTreeId = 0;
        }
    }
}

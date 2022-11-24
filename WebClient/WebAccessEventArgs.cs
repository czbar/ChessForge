using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAccess
{
    /// <summary>
    /// EventArgs for the Web Access events
    /// </summary>
    public class WebAccessEventArgs : EventArgs
    {
        public bool Sucess { get; set; }
    }
}

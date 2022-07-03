using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    public class TreeTableCell
    {

        public string Ply { get => _ply; set => _ply = value; }

        public string ToolTip { get => _toolTip; set => _toolTip = value; }

        public int PlyAttrs { get => _plyAttrs; set => _plyAttrs = value; }

        public int NodeId { get => _nodeId; set => _nodeId = value; }

        private string _ply;
        private string _toolTip;
        private int _plyAttrs;
        private int _nodeId;

    }
}

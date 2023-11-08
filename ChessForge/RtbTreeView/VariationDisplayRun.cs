using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace ChessForge
{
    /// <summary>
    /// Combines a Run shown in the Variation view with associated objects.
    /// </summary>
    public class VariationDisplayRun
    {
        // the Run to show
        Run _run;

        // the Node that this object represents.
        TreeNode _node;
    }
}

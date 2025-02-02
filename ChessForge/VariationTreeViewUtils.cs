using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    public class VariationTreeViewUtils
    {
        /// <summary>
        /// Safe accessor to the chapter's variation index depth.
        /// </summary>
        /// <returns></returns>
        public static int VariationIndexDepth
        {
            get { return AppState.ActiveChapter == null ? Configuration.DefaultIndexDepth : AppState.ActiveChapter.VariationIndexDepth.Value; }
        }

    }
}

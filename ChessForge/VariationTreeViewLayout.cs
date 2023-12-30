using GameTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Layout related methods of the VariationTreeView
    /// </summary>
    public partial class VariationTreeView : RichTextBuilder
    {
        /// <summary>
        /// Builds the Document from the LineSectorTree
        /// </summary>
        private void BuildFlowDocument()
        {
            LineSectorsTree tree = DisplayManager.SectorsTree;

            if (tree.LineSectors.Count <= 1)
            {
                return;
            }

            // LineSector 0 contains just the root node and needs to be added, as the first node, invisible, in sector 1
            TreeNode root = tree.LineSectors[1].Nodes[0];

            for (int i = 1; i < tree.LineSectors.Count; i++)
            {
                LineSector sector = tree.LineSectors[i];
                Paragraph para = CreateParagraph((sector.DisplayLevel - 1).ToString(), true);
                if (i == 1)
                {
                    CreateRunForStartingNode(para, root);
                }

                bool firstNode = true;
                foreach (TreeNode node in sector.Nodes)
                {
                    bool includeNumber = firstNode || node.ColorToMove == ChessPosition.PieceColor.Black;
                    BuildNodeTextAndAddToPara(node, includeNumber, para);
                    firstNode = false;
                }

                Document.Blocks.Add(para);
            }

        }
    }
}

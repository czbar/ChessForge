using GameTree;

namespace ChessForge
{
    public class VariationTreeViewUtils
    {
        /// <summary>
        /// Identifies the collapsed ancestor if any.
        /// </summary>
        /// <param name="nd"></param>
        /// <returns></returns>
        public static TreeNode FindCollapsedAncestor(TreeNode nd)
        {
            TreeNode collapsedAncestor = null;

            if (nd != null)
            {
                while (nd.Parent != null)
                {
                    if (nd.Parent.IsCollapsed)
                    {
                        collapsedAncestor = nd.Parent;
                        break;
                    }
                    nd = nd.Parent;
                }
            }

            return collapsedAncestor;
        }

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

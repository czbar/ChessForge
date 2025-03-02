using GameTree;

namespace ChessForge
{
    public class VariationTreeViewUtils
    {
        /// <summary>
        /// Finds out if the move has a collapsed ancestor
        /// </summary>
        /// <param name="nd"></param>
        /// <returns>true if the uncollapse was required</returns>
        public static bool HasCollapsedAncestor(TreeNode nd)
        {
            bool result = false;

            if (nd != null)
            {
                while (nd.Parent != null)
                {
                    if (nd.Parent.IsCollapsed)
                    {
                        result = true;
                        break;
                    }
                    nd = nd.Parent;
                }
            }

            return result;
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

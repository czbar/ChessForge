using GameTree;

namespace ChessForge
{
    /// <summary>
    /// Utilities for handling the variation tree view.
    /// </summary>
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

        /// <summary>
        /// Sets the selections in the variation tree view and the workbook views for the given node. 
        /// If the node is in a study tree, also sets up the GUI for that.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="node"></param>
        public static void SetSelectionsForNode(GameData.ContentType contentType, TreeNode node)
        {
            if (node != null)
            {
                if (contentType == GameData.ContentType.STUDY_TREE)
                {
                    AppState.MainWin.SetupGuiForActiveStudyTree(true);
                }

                if (AppState.MainWin.ActiveTreeView != null)
                {
                    AppState.MainWin.ActiveTreeView.UnhighlightActiveLine();
                    AppState.MainWin.SetActiveLine(node.LineId, node.NodeId);
                    if (AppState.MainWin.ActiveTreeView is StudyTreeView study)
                    {
                        if (study.UncollapseMove(node))
                        {
                            study.BuildFlowDocumentForVariationTree(false);
                        }
                    }
                    AppState.MainWin.ActiveTreeView
                        .SelectLineAndMoveInWorkbookViews(node.LineId, AppState.MainWin.ActiveLine.GetSelectedPlyNodeIndex(false), true);
                }
            }
        }
    }
}

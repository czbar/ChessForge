using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Utilities for context menus.
    /// </summary>
    public partial class ContextMenus
    {
        /// <summary>
        /// Enables or disables the menu items in the References context menu.
        /// The state of the menu items depends on how many references of different types are
        /// there on the clicked node and the article overall.
        /// </summary>
        /// <param name="cm"></param>
        /// <param name="clickedArticleRef"></param>
        /// <param name="treeGameExerciseRefCount"></param>
        /// <param name="treeChapterRefCount"></param>
        /// <param name="nodeGameExerciseRefCount"></param>
        /// <param name="nodeChapterRefCount"></param>
        public static void EnableReferencesMenuItems(ContextMenu cm, Article clickedArticleRef,
                                                        int treeGameExerciseRefCount, int treeChapterRefCount,
                                                        int nodeGameExerciseRefCount, int nodeChapterRefCount)
        {
            try
            {
                bool isGameExerciseRef = false;
                if (clickedArticleRef.ContentType == GameTree.GameData.ContentType.MODEL_GAME || clickedArticleRef.ContentType == GameTree.GameData.ContentType.EXERCISE)
                {
                    isGameExerciseRef = true;
                }

                foreach (var item in cm.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        switch (menuItem.Name)
                        {
                            case "UiMnciRemoveReference":
                                menuItem.IsEnabled = true;
                                break;
                            case "UiMnciAutoPlaceReference":
                                // enabled if we clicked the game or exercise references
                                menuItem.IsEnabled = isGameExerciseRef;
                                break;
                            case "UiMnciAutoPlaceMoveReferences":
                                // enabled if there is more than one game or exercise reference on the clicked node
                                menuItem.IsEnabled = nodeGameExerciseRefCount > 1;
                                break;
                            case "UiMnciAutoPlaceAllReferences":
                                // enabled if there is more than one game or exercise reference
                                menuItem.IsEnabled = treeGameExerciseRefCount > 1;
                                break;
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Sets the font size of the context menus
        /// </summary>
        /// <param name="size"></param>
        public static void SetMenuFontSize(double size)
        {
            ContextMenu expCollapse = AppState.MainWin.Resources["CmIndexExpandCollapse"] as ContextMenu;
            if (expCollapse != null)
            {
                expCollapse.FontSize = size;
            }

            ContextMenu cmRefs = AppState.MainWin.Resources["CmReferences"] as ContextMenu;
            if (cmRefs != null)
            {
                cmRefs.FontSize = size;
            }
        }


    }
}

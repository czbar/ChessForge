using System.Windows.Controls;

namespace ChessForge
{
    /// <summary>
    /// Utilities for context menus.
    /// </summary>
    public partial class ContextMenus
    {
        /// <summary>
        /// Enables or disables the menu items in the References context menu
        /// </summary>
        /// <param name="cm"></param>
        /// <param name="article"></param>
        /// <param name="gameExerciseRefCount"></param>
        /// <param name="chapterRefCount"></param>
        public static void EnableReferencesMenuItems(ContextMenu cm, Article article, int gameExerciseRefCount, int chapterRefCount)
        {
            try
            {
                bool isGameExerciseRef = false;
                if (article.ContentType == GameTree.GameData.ContentType.MODEL_GAME || article.ContentType == GameTree.GameData.ContentType.EXERCISE)
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
                            case "UiMnciAutoPlaceAllReferences":
                                // enabled if there is more than one game or exercise reference
                                menuItem.IsEnabled = gameExerciseRefCount > 1;
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

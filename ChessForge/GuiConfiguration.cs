using System;
using System.Collections.Generic;
using ChessPosition;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Utilities for configuring GUI objects' visibility and IsEnabled state.
    /// </summary>
    public class GuiConfiguration
    {
        /// <summary>
        /// Sets up visibility of font buttons in the application bar.
        /// </summary>
        public static void ConfigureAppBarFontButtons()
        {
            AppState.MainWin.UiBtnFontSizeFixed.Visibility = (AppState.IsTreeViewTabActive(true) && AppState.ActiveVariationTree != null) 
                                                            ? Visibility.Visible : Visibility.Hidden;
            AppState.MainWin.UiBtnFontSizeVariable.Visibility = (AppState.IsTreeViewTabActive(true) && AppState.ActiveVariationTree != null) 
                                                            ? Visibility.Visible : Visibility.Hidden;

            AppState.MainWin.UiBtnFontSizeUp.Visibility = (AppState.IsTreeViewTabActive(true) && AppState.ActiveVariationTree != null || AppState.ActiveTab == TabViewType.INTRO) 
                                                            ? Visibility.Visible : Visibility.Hidden;
            AppState.MainWin.UiBtnFontSizeDown.Visibility = (AppState.IsTreeViewTabActive(true) && AppState.ActiveVariationTree != null || AppState.ActiveTab == TabViewType.INTRO) 
                                                            ? Visibility.Visible : Visibility.Hidden;
        }
    }
}

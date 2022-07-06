using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Defines state of an UIElement depending
    /// on the current state of the application
    /// or game (if in progress).
    /// </summary>
    internal class UIEelementState
    {
        public UIEelementState(UIElement element, uint modeVis, uint modeEnabled, uint submodeVis, uint submodeEnabled)
        {
            Element = element;

            ModeVisibilityFlags = modeVis;
            ModeEnabledFlags = modeEnabled;

            SubmodeVisibilityFlags = submodeVis;
            SubmodeEnabledFlags = submodeEnabled;
        }

        /// <summary>
        /// UI Element 
        /// </summary>
        public UIElement Element;
        
        /// <summary>
        /// Indicates App modes in which the element will be visible
        /// </summary>
        public uint ModeVisibilityFlags;

        /// <summary>
        /// Indicates App modes in which the element will be enabled.
        /// If 0, the element is always enabled whether visible or not.
        /// </summary>
        public uint ModeEnabledFlags;

        /// <summary>
        /// Indicates App submodes in which the element will be visible.
        /// The app will combine these with the current mode to determine
        /// the required visibility state.
        /// 0 indicates that the submode should be ignored.
        /// </summary>
        public uint SubmodeVisibilityFlags;

        /// <summary>
        /// Indicates App submodes in which the element will be enabled.
        /// If 0, the element is always enabled whether visible or not.
        /// The app will combine these with the current mode to determine
        /// the required enabled state.
        /// 0 indicates that the submode should be ignored.
        /// </summary>
        public uint SubmodeEnabledFlags;
    }
}

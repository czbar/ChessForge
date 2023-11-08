using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessForge
{
    /// <summary>
    /// Visibility status of the line.
    /// It can be fully visible, completely hidden
    /// or partialy shown indicating to the user that there is something to show.
    /// </summary>
    public enum DisplayLineVisibility
    {
        VISIBLE,
        PARTIAL,
        HIDDEN,
    }

    /// <summary>
    /// Types of Runs that can be used in the view.
    /// </summary>
    public enum DisplayRunType
    {
        MOVE,
        COMMENT,
        PREMOVE_COMMENT,
        REFERENCE,
        HYPERLINK,
        ASSESSMENT,
        THUMBNAIL,
    }

    /// <summary>
    /// A variation to be displayed in a single paragraph.
    /// This is either a line without any branches, or with one or more
    /// branchless branches e.g. "15.e4 e5 (15...Nf6 16.Nf3; 15...f6 16.Bc2) 16.Qe4"
    /// Therefor, it represents one or more VariationSingleLine objects.
    /// </summary>
    public class VariationDisplayLine
    {
        // single lines represented by this object
        private List<VariationSingleLine> _lines;

        // how the line is currently shown in the view
        private DisplayLineVisibility _visibility;

        // which level in the line hierarchy is this line at
        private int _level;

        // list of display objects
        private List<VariationDisplayRun> _runList;

        /// <summary>
        /// List of display objects to show in a single paragrapph.
        /// They could represent one or more VariationSingleLine objects.
        /// </summary>
        public List<VariationDisplayRun> RunList
        {
            get => _runList;
            set => _runList = value;
        }
    }
}

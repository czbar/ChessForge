using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Manages color attributes of a LineSector object.
    /// per Display Level
    /// </summary>
    public class LineSectorRunColors
    {
        /// <summary>
        /// Resets the last move color selection.
        /// </summary>
        public void ResetLastMoveBrush()
        {
            _lastMoveBrushIndex = -1;
            _lastLevelCombo = 0;
        }

        // last move color selection index
        private int _lastMoveBrushIndex = -1;

        // last level + levelGroup combination for which color was requested
        private int _lastLevelCombo = 0;

        /// <summary>
        /// Color for the last node at the given level. 
        /// </summary>
        /// <param name="level"></param>
        /// <param name="levelGroup"></param>
        /// <returns></returns>
        public Brush GetBrushForLastMove(int level, int levelGroup)
        {
            Brush brush;

            // increment the index if this is not the last combination.
            if (level + levelGroup != _lastLevelCombo)
            {
                _lastLevelCombo = level + levelGroup;
                _lastMoveBrushIndex++;
            }
            else
            {
                if (_lastMoveBrushIndex < 0)
                {
                    _lastMoveBrushIndex = 0;
                }
            }

            int modLevel = _lastMoveBrushIndex % 4;

            switch (modLevel)
            {
                case 0:
                    brush = ChessForgeColors.CurrentTheme.ModuloColor_0;
                    break;
                case 1:
                    brush = ChessForgeColors.CurrentTheme.ModuloColor_1;
                    break;
                case 2:
                    brush = ChessForgeColors.CurrentTheme.ModuloColor_2;
                    break;
                case 3:
                    brush = ChessForgeColors.CurrentTheme.ModuloColor_3;
                    break;
                default:
                    brush = ChessForgeColors.CurrentTheme.RtbForeground;
                    break;
            }

            return brush;
        }
    }
}

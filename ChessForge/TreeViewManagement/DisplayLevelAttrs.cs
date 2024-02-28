using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace ChessForge
{
    /// <summary>
    /// Manages various attributes of the Study layout
    /// per Display Level
    /// </summary>
    public class DisplayLevelAttrs
    {
        /// <summary>
        /// Color for the last node at the given level. 
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static Brush GetBrushForLastMove(int level)
        {
            Brush brush;

            int modLevel = level % 4;

            switch (modLevel)
            {
                case 0:
                    brush = Brushes.Blue;
                    break;
                case 1:
                    brush = Brushes.Green;
                    break;
                case 2:
                    brush = Brushes.Purple;
                    break;
                case 3:
                    brush = Brushes.Chocolate;
                    break;
                default:
                    brush = Brushes.Black;
                    break;
            }

            return brush;
        }
    }
}

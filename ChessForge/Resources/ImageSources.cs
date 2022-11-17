using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ChessForge
{
    /// <summary>
    /// Image Sources for various GUI elements
    /// </summary>
    public class ImageSources
    {
        public static BitmapImage FontSizeVariable = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/FontSizeVariable.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage FontSizeFixed = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/FontSizeFixed.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage SolvingExit = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/exit.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage SolvingComplete = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/solve_complete.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage SolveAnalysis = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/solve.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage SolveGuess = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/guess_move.png", UriKind.RelativeOrAbsolute));
    }
}

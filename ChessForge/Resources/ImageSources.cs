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
        public static BitmapImage ChevronLeft = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/chevron-left.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChevronRight = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/chevron-right.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChevronUp = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/chevron-up.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChevronDown = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/chevron-down.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage LichessLogo = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/lichess_logo.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage FlipBoard = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/flip_board.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ResetDates = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ResetDates.png", UriKind.RelativeOrAbsolute));

        public static BitmapImage ChaptersUpArrow = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChapterUpArrow.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChaptersDnArrow = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChapterDnArrow.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChaptersUpArrowDarkMode = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChapterUpArrowDarkMode.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChaptersDnArrowDarkMode = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChapterDnArrowDarkMode.png", UriKind.RelativeOrAbsolute));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ChessForge
{
    /// <summary>
    /// Images for the main chess boards
    /// </summary>
    public class ChessBoards
    {
        public static BitmapImage ChessBoardBlue = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardBlue.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardBrown = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardBrown.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage ChessBoardGreen = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ChessBoardGreen.png", UriKind.RelativeOrAbsolute));
    }

    /// <summary>
    /// Images for the arrows to be drawn on the board.
    /// </summary>
    public class ChessBoardArrows
    {
        public static BitmapImage OrangeTriangle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowTriangleYellow.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage OrangeStem = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowStemYellow.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage OrangeCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowCircleYellow.png", UriKind.RelativeOrAbsolute));
        public static BitmapImage OrangeHalfCircle = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/ArrowHalfCircleYellow.png", UriKind.RelativeOrAbsolute));
    }
}
